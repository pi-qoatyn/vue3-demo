using Microsoft.Extensions.Logging;
using RabbitMQEventSourceStore.Core.Models;
using RabbitMQEventSourceStore.Core.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQEventSourceStore.Core.Consuming
{
    /// <summary>
    /// Consumer for processing events from streams
    /// </summary>
    public class EventConsumer : IDisposable
    {
        private readonly ILogger<EventConsumer> _logger;
        private readonly IEventStore _eventStore;
        private readonly List<IEventHandler> _handlers;
        private readonly Dictionary<string, string> _subscriptions;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private bool _disposed;

        public EventConsumer(ILogger<EventConsumer> logger, IEventStore eventStore)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
            _handlers = new List<IEventHandler>();
            _subscriptions = new Dictionary<string, string>();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Registers an event handler
        /// </summary>
        /// <param name="handler">Handler to register</param>
        public void RegisterHandler(IEventHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _handlers.Add(handler);
            _logger.LogInformation("Registered event handler: {HandlerType}", handler.GetType().Name);
        }

        /// <summary>
        /// Starts consuming events from a specific stream
        /// </summary>
        /// <param name="streamName">Name of the stream to consume</param>
        /// <param name="fromVersion">Starting version</param>
        /// <returns>Task</returns>
        public async Task StartConsumingStreamAsync(string streamName, long? fromVersion = null)
        {
            if (string.IsNullOrEmpty(streamName))
                throw new ArgumentNullException(nameof(streamName));

            if (_subscriptions.ContainsKey(streamName))
            {
                _logger.LogWarning("Already consuming stream {StreamName}", streamName);
                return;
            }

            try
            {
                var subscriptionId = await _eventStore.SubscribeToStreamAsync(
                    streamName, 
                    HandleEventAsync, 
                    fromVersion);

                _subscriptions[streamName] = subscriptionId;
                _logger.LogInformation("Started consuming stream {StreamName} with subscription {SubscriptionId}", 
                    streamName, subscriptionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start consuming stream {StreamName}", streamName);
                throw;
            }
        }

        /// <summary>
        /// Starts consuming all events
        /// </summary>
        /// <param name="fromPosition">Starting position</param>
        /// <returns>Task</returns>
        public async Task StartConsumingAllAsync(long? fromPosition = null)
        {
            try
            {
                var subscriptionId = await _eventStore.SubscribeToAllAsync(
                    HandleEventAsync, 
                    fromPosition);

                _subscriptions["$all"] = subscriptionId;
                _logger.LogInformation("Started consuming all events with subscription {SubscriptionId}", 
                    subscriptionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start consuming all events");
                throw;
            }
        }

        /// <summary>
        /// Stops consuming events from a specific stream
        /// </summary>
        /// <param name="streamName">Name of the stream</param>
        /// <returns>Task</returns>
        public async Task StopConsumingStreamAsync(string streamName)
        {
            if (string.IsNullOrEmpty(streamName))
                throw new ArgumentNullException(nameof(streamName));

            if (_subscriptions.TryGetValue(streamName, out var subscriptionId))
            {
                await _eventStore.UnsubscribeAsync(subscriptionId);
                _subscriptions.Remove(streamName);
                _logger.LogInformation("Stopped consuming stream {StreamName}", streamName);
            }
        }

        /// <summary>
        /// Stops all consumption
        /// </summary>
        /// <returns>Task</returns>
        public async Task StopAllAsync()
        {
            foreach (var subscription in _subscriptions.ToList())
            {
                await _eventStore.UnsubscribeAsync(subscription.Value);
            }
            _subscriptions.Clear();
            _logger.LogInformation("Stopped all event consumption");
        }

        private async Task HandleEventAsync(EventEnvelope envelope)
        {
            if (envelope?.Event == null)
            {
                _logger.LogWarning("Received null event envelope");
                return;
            }

            var @event = envelope.Event;
            var eventType = @event.EventType;

            _logger.LogDebug("Processing event {EventType} from stream {StreamName}", 
                eventType, envelope.StreamName);

            try
            {
                var handlers = _handlers.Where(h => h.CanHandle(eventType)).ToList();
                
                if (!handlers.Any())
                {
                    _logger.LogWarning("No handlers found for event type {EventType}", eventType);
                    return;
                }

                var tasks = handlers.Select(handler => ExecuteHandlerAsync(handler, @event));
                await Task.WhenAll(tasks);

                _logger.LogDebug("Successfully processed event {EventType}", eventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event {EventType}", eventType);
                throw;
            }
        }

        private async Task ExecuteHandlerAsync(IEventHandler handler, IEvent @event)
        {
            try
            {
                await handler.HandleAsync(@event);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Handler {HandlerType} failed to process event {EventType}",
                    handler.GetType().Name, @event.EventType);
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _cancellationTokenSource.Cancel();
            
            try
            {
                StopAllAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing EventConsumer");
            }
            finally
            {
                _cancellationTokenSource.Dispose();
            }
        }
    }
}