using Gugubao.Utility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Gugubao.Rabbitmq
{
	/// <summary>
	/// 推送消息到估小铺，历史遗留问题，无法使用新的消息框架
	/// 旧系统无法使用新框架的消息类型，故不再做对象约束
	/// 作为推送端不在构建任何的交换机和队列，请保证原始系统已经初始化交换机和对应队列
	/// 如果不再和老系统交互可以删除
	/// </summary>
	public class EventBusGuxiaopu
	{
		private readonly ILogger _logger;
		private readonly ILoggerFactory _loggerFactory;
		private readonly GxpRabbitmqManager _rabbitmqManager;
		private readonly IConfiguration _configuration;

		public EventBusGuxiaopu(ILoggerFactory loggerFactory, IConfiguration configuration)
		{
			_configuration = configuration;
			_loggerFactory = loggerFactory;
			_logger = loggerFactory.CreateLogger<EventBusGuxiaopu>();

			_rabbitmqManager = new GxpRabbitmqManager(new RabbitmqOption
			{
				UserName = _configuration.GetValue<string>("GxpRabbitmq:UserName"),
				HostName = _configuration.GetValue<string>("GxpRabbitmq:HostName"),
				Password = _configuration.GetValue<string>("GxpRabbitmq:Password"),
				RetryCount = 3
			},
			_loggerFactory); ;

			_rabbitmqManager.TryConnect();
		}

		/// <summary>
		/// 推送消息到估小铺，估估宝的消息队列和估小铺的消息队列在通一台服务器
		/// </summary>
		/// <typeparam name="T">IntegrationEvent</typeparam>
		/// <param name="event">IntegrationEvent事件对象</param>
		/// <param name="brokerName">交换机名称</param>
		/// <param name="queueName">队列名称</param>
		public void PublishToGxp<T>(T @event, string brokerName = "", string queueName = "")
			where T : IntegrationEvent
		{
			Publish(@event, brokerName, queueName);
		}

		/// <summary>
		/// 推送消息到估估宝，估估宝的消息队列和估小铺的消息队列在通一台服务器
		/// </summary>
		/// <typeparam name="T">IntegrationEvent</typeparam>
		/// <param name="event">IntegrationEvent事件对象</param>
		/// <param name="brokerName">交换机名称，估估宝的交换机和队列是固定的</param>
		/// <param name="queueName">队列名称</param>
		public void PublishToGgb<T>(T @event, string brokerName = "ggb.direct", string queueName = "ggb_queue")
			where T : IntegrationEvent
		{
			Publish(@event, brokerName, queueName);
		}

		/// <summary>
		/// 推送消息到旧系统
		/// </summary>
		/// <typeparam name="T">IntegrationEvent</typeparam>
		/// <param name="integrationEvent">IntegrationEvent对象</param>
		public void Publish<T>(T integrationEvent, string brokerName = "", string queueName = "")
			where T : IntegrationEvent
		{
			if (!_rabbitmqManager.IsConnected)
			{
				_rabbitmqManager.TryConnect();
			}

			var policy = Policy.Handle<BrokerUnreachableException>()
				.Or<SocketException>()
				.WaitAndRetry(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
				{
					_logger.LogWarning(ex, "无法发布消息到估小铺消息队");
				});

			using var channel = _rabbitmqManager.CreateModel();

			var message = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType());
			var body = Encoding.UTF8.GetBytes(message);

			policy.Execute(() =>
			{
				var properties = channel.CreateBasicProperties();
				properties.Persistent = true;

				channel.BasicPublish(brokerName, queueName, properties, body);
			});
		}

		void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
		{
			_logger.LogError($"估小铺消息队列异常{e.Reason}");
		}

		void OnCallbackException(object sender, CallbackExceptionEventArgs e)
		{
			_logger.LogError($"估小铺消息队列异常{e.Detail}");
		}

		void OnConnectionShutdown(object sender, ShutdownEventArgs reason)
		{
			_logger.LogError($"估小铺消息队列异常{reason.ReplyText}");
		}
	}

	class GxpRabbitmqManager
	{
		private readonly ILogger _logger;
		private readonly RabbitmqOption _option;
		private readonly IConnectionFactory _connectionFactory;
		private readonly object sync_root = new object();
		IConnection _connection;
		bool _disposed;

		public GxpRabbitmqManager(RabbitmqOption option, ILoggerFactory logger)
		{
			_option = option;
			_logger = logger.CreateLogger<GxpRabbitmqManager>();

			_connectionFactory = new ConnectionFactory
			{
				UserName = _option.UserName,
				Password = _option.Password,
				AutomaticRecoveryEnabled = true,// connection自动重连
				TopologyRecoveryEnabled = true, // 交换机队列自动重连
				DispatchConsumersAsync = true
			};
		}

		public bool IsConnected => _connection != null && _connection.IsOpen && !_disposed;

		public IModel CreateModel()
		{
			if (!IsConnected)
			{
				_logger.LogCritical("rabbitmq服务未启动");
			}

			return _connection.CreateModel();
		}

		public void TryConnect()
		{
			var hostNames = _option.HostName.Split(',');

			_logger.LogInformation("RabbitMQ Client is trying to connect");

			lock (sync_root)
			{
				var policy = Policy.Handle<SocketException>()
					.Or<BrokerUnreachableException>()
					.WaitAndRetry(_option.RetryCount,
								  retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
								  (ex, time, retryAttempt, context) =>
								  {
									  _logger.LogCritical($"Rabbitmq消息发送失败，重试第{retryAttempt}次");
								  }
				);

				policy.Execute(() =>
				{
					_connection = _connectionFactory.CreateConnection(hostNames);
				});

				if (IsConnected)
				{
					_connection.ConnectionShutdown += OnConnectionShutdown;
					_connection.CallbackException += OnCallbackException;
					_connection.ConnectionBlocked += OnConnectionBlocked;
				}
				else
				{
					_logger.LogCritical("rabbitmq服务未启动");
				}
			}
		}

		public void Dispose()
		{
			if (_disposed) return;

			_disposed = true;

			try
			{
				_connection.Dispose();
			}
			catch (IOException ex)
			{
				_logger.LogCritical(ex.ToString());
			}
		}

		void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
		{
			if (_disposed) return;

			_logger.LogWarning("RabbitMQ连接异常,尝试重连...");

			TryConnect();
		}

		void OnCallbackException(object sender, CallbackExceptionEventArgs e)
		{
			if (_disposed) return;

			_logger.LogWarning("RabbitMQ连接异常,尝试重连...");

			TryConnect();
		}

		void OnConnectionShutdown(object sender, ShutdownEventArgs reason)
		{
			if (_disposed) return;

			_logger.LogWarning("RabbitMQ连接异常,尝试重连...");

			TryConnect();
		}
	}

}
