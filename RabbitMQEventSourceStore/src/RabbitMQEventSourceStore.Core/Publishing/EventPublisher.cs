using Microsoft.Extensions.Logging;
using RabbitMQEventSourceStore.Core.Models;
using RabbitMQEventSourceStore.Core.Store;
using System;
using System.Threading.Tasks;

namespace RabbitMQEventSourceStore.Core.Publishing
{
    /// <summary>
    /// Default implementation of event publisher
    /// </summary>
    public class EventPublisher : IEventPublisher
    {
        private readonly ILogger<EventPublisher> _logger;
        private readonly IEventStore _eventStore;

        public EventPublisher(ILogger<EventPublisher> logger, IEventStore eventStore)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        }

        public async Task PublishAsync(string streamName, IEvent @event)
        {
            if (string.IsNullOrEmpty(streamName))
                throw new ArgumentNullException(nameof(streamName));
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));

            try
            {
                await _eventStore.AppendToStreamAsync(streamName, @event);
                _logger.LogInformation("Published event {EventType} to stream {StreamName}", 
                    @event.EventType, streamName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish event {EventType} to stream {StreamName}", 
                    @event.EventType, streamName);
                throw;
            }
        }

        public async Task PublishAsync(IEvent @event)
        {
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));

            var streamName = $"aggregate-{@event.AggregateId}";
            await PublishAsync(streamName, @event);
        }
    }
}