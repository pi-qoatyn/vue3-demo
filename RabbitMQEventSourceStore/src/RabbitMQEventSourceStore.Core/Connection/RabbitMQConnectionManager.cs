using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQEventSourceStore.Core.Configuration;
using System;
using System.Threading;

namespace RabbitMQEventSourceStore.Core.Connection
{
    /// <summary>
    /// Manages RabbitMQ connections with retry logic and automatic recovery
    /// </summary>
    public class RabbitMQConnectionManager : IRabbitMQConnectionManager
    {
        private readonly ILogger<RabbitMQConnectionManager> _logger;
        private readonly RabbitMQConfiguration _configuration;
        private readonly IConnectionFactory _connectionFactory;
        private IConnection _connection;
        private readonly object _connectionLock = new object();
        private bool _disposed;

        public RabbitMQConnectionManager(
            ILogger<RabbitMQConnectionManager> logger,
            IOptions<RabbitMQConfiguration> configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));

            _connectionFactory = new ConnectionFactory
            {
                HostName = _configuration.HostName,
                Port = _configuration.Port,
                UserName = _configuration.UserName,
                Password = _configuration.Password,
                VirtualHost = _configuration.VirtualHost,
                AutomaticRecoveryEnabled = _configuration.AutomaticRecoveryEnabled,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(_configuration.NetworkRecoveryIntervalSeconds),
                RequestedHeartbeat = TimeSpan.FromSeconds(60),
                DispatchConsumersAsync = true
            };
        }

        public IConnection Connection
        {
            get
            {
                if (!IsConnected)
                {
                    TryConnect();
                }
                return _connection;
            }
        }

        public bool IsConnected => _connection != null && _connection.IsOpen && !_disposed;

        public IModel CreateChannel()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("No RabbitMQ connection is available to perform this action");
            }

            return _connection.CreateModel();
        }

        public bool TryConnect()
        {
            _logger.LogInformation("Attempting to connect to RabbitMQ...");

            lock (_connectionLock)
            {
                if (IsConnected)
                {
                    return true;
                }

                var retryCount = 0;
                while (retryCount < _configuration.RetryCount)
                {
                    try
                    {
                        _connection = _connectionFactory.CreateConnection();

                        if (IsConnected)
                        {
                            _connection.ConnectionShutdown += OnConnectionShutdown;
                            _connection.CallbackException += OnCallbackException;
                            _connection.ConnectionBlocked += OnConnectionBlocked;

                            _logger.LogInformation("RabbitMQ connection established successfully");
                            return true;
                        }
                    }
                    catch (BrokerUnreachableException ex)
                    {
                        _logger.LogWarning(ex, "Failed to connect to RabbitMQ (attempt {RetryCount}/{MaxRetry})", 
                            retryCount + 1, _configuration.RetryCount);
                        
                        retryCount++;
                        if (retryCount < _configuration.RetryCount)
                        {
                            Thread.Sleep(_configuration.RetryDelaySeconds * 1000);
                        }
                    }
                }

                _logger.LogError("Failed to establish RabbitMQ connection after {RetryCount} attempts", 
                    _configuration.RetryCount);
                return false;
            }
        }

        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            if (_disposed) return;
            _logger.LogWarning("RabbitMQ connection is blocked. Reason: {Reason}", e.Reason);
        }

        private void OnCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            if (_disposed) return;
            _logger.LogWarning(e.Exception, "RabbitMQ connection callback exception");
        }

        private void OnConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            if (_disposed) return;
            _logger.LogWarning("RabbitMQ connection was shut down. Reply: {ReplyText}", e.ReplyText);
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            try
            {
                if (_connection != null)
                {
                    _connection.ConnectionShutdown -= OnConnectionShutdown;
                    _connection.CallbackException -= OnCallbackException;
                    _connection.ConnectionBlocked -= OnConnectionBlocked;
                    _connection.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while disposing RabbitMQ connection");
            }
        }
    }
}