using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQEventSourceStore.Core.Configuration;
using RabbitMQEventSourceStore.Core.Connection;
using RabbitMQEventSourceStore.Core.Models;
using RabbitMQEventSourceStore.Core.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQEventSourceStore.Core.Store
{
    /// <summary>
    /// RabbitMQ-based event store implementation
    /// </summary>
    public class RabbitMQEventStore : IEventStore, IDisposable
    {
        private readonly ILogger<RabbitMQEventStore> _logger;
        private readonly IRabbitMQConnectionManager _connectionManager;
        private readonly IEventSerializer _serializer;
        private readonly RabbitMQConfiguration _configuration;
        private readonly ConcurrentDictionary<string, Subscription> _subscriptions;
        private readonly ConcurrentDictionary<string, long> _streamVersions;
        private IModel _channel;
        private readonly object _channelLock = new object();
        private bool _disposed;

        public RabbitMQEventStore(
            ILogger<RabbitMQEventStore> logger,
            IRabbitMQConnectionManager connectionManager,
            IEventSerializer serializer,
            IOptions<RabbitMQConfiguration> configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
            
            _subscriptions = new ConcurrentDictionary<string, Subscription>();
            _streamVersions = new ConcurrentDictionary<string, long>();
            
            InitializeExchanges();
        }

        private void InitializeExchanges()
        {
            try
            {
                EnsureChannel();
                
                // Declare main exchange
                _channel.ExchangeDeclare(
                    exchange: _configuration.ExchangeName,
                    type: _configuration.ExchangeType,
                    durable: true,
                    autoDelete: false);

                // Declare dead letter exchange
                _channel.ExchangeDeclare(
                    exchange: _configuration.DeadLetterExchange,
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false);

                _logger.LogInformation("RabbitMQ exchanges initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize RabbitMQ exchanges");
                throw;
            }
        }

        private void EnsureChannel()
        {
            lock (_channelLock)
            {
                if (_channel == null || !_channel.IsOpen)
                {
                    _channel?.Dispose();
                    _channel = _connectionManager.CreateChannel();
                    _channel.BasicQos(0, 100, false); // Prefetch 100 messages
                }
            }
        }

        public async Task AppendToStreamAsync(string streamName, IEvent @event, long? expectedVersion = null)
        {
            await AppendToStreamAsync(streamName, new[] { @event }, expectedVersion);
        }

        public async Task AppendToStreamAsync(string streamName, IEnumerable<IEvent> events, long? expectedVersion = null)
        {
            if (string.IsNullOrEmpty(streamName))
                throw new ArgumentNullException(nameof(streamName));
            
            var eventList = events?.ToList() ?? throw new ArgumentNullException(nameof(events));
            if (!eventList.Any())
                return;

            await Task.Run(() =>
            {
                try
                {
                    EnsureChannel();

                    // Check expected version
                    var currentVersion = _streamVersions.GetOrAdd(streamName, -1);
                    if (expectedVersion.HasValue && currentVersion != expectedVersion.Value)
                    {
                        throw new InvalidOperationException(
                            $"Expected version {expectedVersion.Value} but current version is {currentVersion}");
                    }

                    var routingKey = GetRoutingKey(streamName);
                    var properties = _channel.CreateBasicProperties();
                    properties.Persistent = true;
                    properties.ContentType = "application/json";
                    properties.Headers = new Dictionary<string, object>
                    {
                        ["stream-name"] = streamName
                    };

                    foreach (var @event in eventList)
                    {
                        currentVersion++;
                        
                        var envelope = new EventEnvelope(@event, streamName)
                        {
                            StreamPosition = currentVersion,
                            SequenceNumber = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                        };

                        var body = _serializer.SerializeEnvelope(envelope);
                        
                        _channel.BasicPublish(
                            exchange: _configuration.ExchangeName,
                            routingKey: routingKey,
                            basicProperties: properties,
                            body: body);

                        _logger.LogDebug("Published event {EventType} to stream {StreamName} at position {Position}",
                            @event.EventType, streamName, currentVersion);
                    }

                    _streamVersions[streamName] = currentVersion;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to append events to stream {StreamName}", streamName);
                    throw;
                }
            });
        }

        public async Task<IEnumerable<EventEnvelope>> ReadStreamAsync(string streamName, long fromVersion = 0, long? toVersion = null)
        {
            if (string.IsNullOrEmpty(streamName))
                throw new ArgumentNullException(nameof(streamName));

            // Note: In a real implementation, you would typically read from a persistent store
            // RabbitMQ is primarily a message broker, not a persistent event store
            // This is a simplified implementation that doesn't support historical reads
            _logger.LogWarning("ReadStreamAsync is not fully implemented for RabbitMQ. Consider using a persistent store for event sourcing.");
            
            return await Task.FromResult(Enumerable.Empty<EventEnvelope>());
        }

        public async Task<IEnumerable<EventEnvelope>> ReadAllAsync(long fromPosition = 0, int maxCount = 1000)
        {
            // Note: Same limitation as ReadStreamAsync
            _logger.LogWarning("ReadAllAsync is not fully implemented for RabbitMQ. Consider using a persistent store for event sourcing.");
            
            return await Task.FromResult(Enumerable.Empty<EventEnvelope>());
        }

        public async Task<string> SubscribeToStreamAsync(string streamName, Func<EventEnvelope, Task> onEventReceived, long? fromVersion = null)
        {
            if (string.IsNullOrEmpty(streamName))
                throw new ArgumentNullException(nameof(streamName));
            if (onEventReceived == null)
                throw new ArgumentNullException(nameof(onEventReceived));

            return await Task.Run(() =>
            {
                try
                {
                    EnsureChannel();

                    var subscriptionId = Guid.NewGuid().ToString();
                    var queueName = $"{streamName}-{subscriptionId}";
                    var routingKey = GetRoutingKey(streamName);

                    // Declare queue with dead letter exchange
                    var arguments = new Dictionary<string, object>
                    {
                        ["x-dead-letter-exchange"] = _configuration.DeadLetterExchange
                    };

                    _channel.QueueDeclare(
                        queue: queueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: true,
                        arguments: arguments);

                    _channel.QueueBind(
                        queue: queueName,
                        exchange: _configuration.ExchangeName,
                        routingKey: routingKey);

                    var consumer = new AsyncEventingBasicConsumer(_channel);
                    consumer.Received += async (sender, args) =>
                    {
                        try
                        {
                            var envelope = _serializer.DeserializeEnvelope(args.Body.ToArray());
                            
                            if (!fromVersion.HasValue || envelope.StreamPosition > fromVersion.Value)
                            {
                                await onEventReceived(envelope);
                            }

                            _channel.BasicAck(args.DeliveryTag, false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing message in subscription {SubscriptionId}", subscriptionId);
                            _channel.BasicNack(args.DeliveryTag, false, false); // Send to DLX
                        }
                    };

                    var consumerTag = _channel.BasicConsume(
                        queue: queueName,
                        autoAck: false,
                        consumer: consumer);

                    _subscriptions[subscriptionId] = new Subscription
                    {
                        Id = subscriptionId,
                        QueueName = queueName,
                        ConsumerTag = consumerTag,
                        StreamName = streamName
                    };

                    _logger.LogInformation("Created subscription {SubscriptionId} for stream {StreamName}", 
                        subscriptionId, streamName);

                    return subscriptionId;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create subscription for stream {StreamName}", streamName);
                    throw;
                }
            });
        }

        public async Task<string> SubscribeToAllAsync(Func<EventEnvelope, Task> onEventReceived, long? fromPosition = null)
        {
            // Subscribe to all events using wildcard routing key
            return await SubscribeToStreamAsync("*", onEventReceived, fromPosition);
        }

        public async Task UnsubscribeAsync(string subscriptionId)
        {
            if (string.IsNullOrEmpty(subscriptionId))
                throw new ArgumentNullException(nameof(subscriptionId));

            await Task.Run(() =>
            {
                if (_subscriptions.TryRemove(subscriptionId, out var subscription))
                {
                    try
                    {
                        EnsureChannel();
                        _channel.BasicCancel(subscription.ConsumerTag);
                        _channel.QueueDelete(subscription.QueueName, false, false);
                        
                        _logger.LogInformation("Unsubscribed from {SubscriptionId}", subscriptionId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error unsubscribing from {SubscriptionId}", subscriptionId);
                        throw;
                    }
                }
            });
        }

        public async Task<long?> GetStreamVersionAsync(string streamName)
        {
            if (string.IsNullOrEmpty(streamName))
                throw new ArgumentNullException(nameof(streamName));

            return await Task.FromResult(
                _streamVersions.TryGetValue(streamName, out var version) ? version : (long?)null);
        }

        public async Task DeleteStreamAsync(string streamName)
        {
            if (string.IsNullOrEmpty(streamName))
                throw new ArgumentNullException(nameof(streamName));

            await Task.Run(() =>
            {
                _streamVersions.TryRemove(streamName, out _);
                _logger.LogInformation("Deleted stream {StreamName}", streamName);
            });
        }

        private string GetRoutingKey(string streamName)
        {
            return $"events.{streamName}";
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            try
            {
                // Unsubscribe all
                foreach (var subscription in _subscriptions.Values)
                {
                    try
                    {
                        _channel?.BasicCancel(subscription.ConsumerTag);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error canceling subscription {SubscriptionId}", subscription.Id);
                    }
                }

                _subscriptions.Clear();
                _channel?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing RabbitMQEventStore");
            }
        }

        private class Subscription
        {
            public string Id { get; set; }
            public string QueueName { get; set; }
            public string ConsumerTag { get; set; }
            public string StreamName { get; set; }
        }
    }
}