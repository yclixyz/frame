using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Gugubao.Rabbitmq
{
    /// <summary>
    /// 消息队列发布订阅中心
    /// </summary>
    public class EventBus : IEventBus
    {
        private readonly RabbitmqOption _option;
        private readonly IRabbitmqManager _rabbitmqManager;
        private readonly ILogger<EventBus> _logger;
        private readonly IMemoryCache _cache;
        private readonly IServiceProvider _serviceProvider;
        private readonly IEventBusManager _subsManager;
        private IModel _consumerChannel;

        public EventBus(IOptions<RabbitmqOption> option,
            IRabbitmqManager manager,
            ILoggerFactory factory,
            IMemoryCache cache,
            IServiceProvider serviceProvider,
            IEventBusManager subsManager)
        {
            _option = option.Value;
            _rabbitmqManager = manager;
            _logger = factory.CreateLogger<EventBus>();
            _cache = cache;
            _serviceProvider = serviceProvider;
            _subsManager = subsManager;
            _consumerChannel = CreateConsumerChannel();
        }

        public void Publish<T>(T integrationEvent) where T : IntegrationEvent
        {
            if (!_rabbitmqManager.IsConnected)
            {
                _rabbitmqManager.TryConnect();
            }

            var policy = Policy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(_option.RetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                    _logger.LogWarning(ex, "无法发布消息到Rabbitmq服务");
                });

            using var channel = _rabbitmqManager.CreateModel();
            channel.ExchangeDeclare(_option.BrokerName, ExchangeType.Direct, true, false, null);

            var message = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType());
            var body = Encoding.UTF8.GetBytes(message);

            policy.Execute(() =>
            {
                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;

                // 没有对应的消费者则消息不进入队列
                channel.BasicPublish(_option.BrokerName, integrationEvent.GetType().Name, true, properties, body);
            });
        }

        public void Subscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = _subsManager.GetEventKey<T>();
            DoInternalSubscription(eventName);

            _subsManager.AddSubscription<T, TH>();
            StartBasicConsume();
        }

        private void DoInternalSubscription(string eventName)
        {
            var containsKey = _subsManager.HasSubscriptionsForEvent(eventName);
            if (!containsKey)
            {
                if (!_rabbitmqManager.IsConnected)
                {
                    _rabbitmqManager.TryConnect();
                }

                using var channel = _rabbitmqManager.CreateModel();
                channel.QueueBind(_option.QueueName, _option.BrokerName, eventName);
            }
        }

        public void Dispose()
        {
            if (_consumerChannel != null)
            {
                _consumerChannel.Dispose();
            }

            _subsManager.Clear();
        }

        private void StartBasicConsume()
        {
            if (_consumerChannel != null)
            {
                var consumer = new AsyncEventingBasicConsumer(_consumerChannel);

                consumer.Received += Consumer_Received;

                _consumerChannel.BasicConsume(_option.QueueName, false, consumer);
            }
            else
            {
                _logger.LogError("rabbitmq服务异常，无法消费队列消息");
            }
        }

        private async Task Consumer_Received(object sender, BasicDeliverEventArgs eventArgs)
        {
            var eventId = 0L;
            var excuteCount = 0;

            try
            {
                var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

                eventId = JsonSerializer.Deserialize<IntegrationEvent>(message).Id;

                _cache.TryGetValue(eventId, out excuteCount);

                if (excuteCount == _option.RetryCount)
                {
                    _cache.Remove(eventId);
                    _consumerChannel.BasicReject(eventArgs.DeliveryTag, false);
                    return;
                }

                await ProcessEvent(eventArgs);
            }
            catch (JsonException ex)
            {
                _logger.LogCritical($"不支持的数据类型，消息Id:{eventId},{ex}");

                _cache.Set(eventId, Interlocked.Increment(ref excuteCount), DateTimeOffset.Now.AddDays(7));

                _consumerChannel.BasicReject(eventArgs.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"Rabbitmq消费错误，消息Id:{eventId},{ex}");

                _cache.Set(eventId, Interlocked.Increment(ref excuteCount), DateTimeOffset.Now.AddDays(7));

                _consumerChannel.BasicReject(eventArgs.DeliveryTag, true);
            }
        }

        private IModel CreateConsumerChannel()
        {
            if (!_rabbitmqManager.IsConnected)
            {
                _rabbitmqManager.TryConnect();
            }

            var channel = _rabbitmqManager.CreateModel();

            channel.ExchangeDeclare("deadletter.direct", ExchangeType.Direct, false, false, null);

            channel.QueueDeclare("deadletter", true, false, false, null);

            channel.QueueBind("deadletter", "deadletter.direct", "deadletter", null);

            channel.ExchangeDeclare(_option.BrokerName, ExchangeType.Direct, true, false, null);

            channel.QueueDeclare(_option.QueueName, true, false, false, new Dictionary<string, object>
            {
                {"x-max-length",100 },
                { "x-message-ttl",60000 },// 60s
                { "x-dead-letter-exchange","deadletter.direct" },
                { "x-dead-letter-routing-key","deadletter"}
            });

            channel.CallbackException += (sender, ea) =>
            {
                _logger.LogWarning(ea.Exception, "Rabbit服务异常，正在重启服务");

                _consumerChannel.Dispose();
                _consumerChannel = CreateConsumerChannel();
                StartBasicConsume();
            };

            return channel;
        }

        private async Task ProcessEvent(BasicDeliverEventArgs eventArgs)
        {
            var eventName = eventArgs.RoutingKey;

            var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

            if (_subsManager.HasSubscriptionsForEvent(eventName))
            {
                using var scope = _serviceProvider.CreateScope();

                var subscriptions = _subsManager.GetHandlersForEvent(eventName);
                foreach (var subscription in subscriptions)
                {
                    var handler = scope.ServiceProvider.GetService(subscription);
                    if (handler == null) continue;
                    var eventType = _subsManager.GetEventTypeByName(eventName);
                    var integrationEvent = JsonSerializer.Deserialize(message, eventType);
                    var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);

                    await Task.Yield();
                    await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { integrationEvent });

                    _consumerChannel.BasicAck(eventArgs.DeliveryTag, multiple: false);
                }
            }
            else
            {
                _logger.LogWarning("匹配不到对应的消息消费Handler", eventName);
            }
        }
    }
}
