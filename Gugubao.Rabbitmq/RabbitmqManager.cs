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

namespace Gugubao.Rabbitmq
{
    /// <summary>
    /// rabbitmq管理
    /// </summary>
    public class RabbitmqManager : IRabbitmqManager
    {
        private readonly ILogger<RabbitmqManager> _logger;
        private readonly RabbitmqOption _option;
        private readonly IConnectionFactory _connectionFactory;
        private readonly object sync_root = new object();
        IConnection _connection;
        bool _disposed;

        public RabbitmqManager(IOptions<RabbitmqOption> option, ILogger<RabbitmqManager> logger)
        {
            _option = option.Value;
            _logger = logger;

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