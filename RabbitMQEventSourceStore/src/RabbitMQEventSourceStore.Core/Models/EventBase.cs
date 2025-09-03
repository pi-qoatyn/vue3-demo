using System;

namespace RabbitMQEventSourceStore.Core.Models
{
    /// <summary>
    /// Base class for all events
    /// </summary>
    public abstract class EventBase : IEvent
    {
        public Guid EventId { get; set; }
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; }
        public int Version { get; set; }
        public Guid AggregateId { get; set; }
        public string UserId { get; set; }
        public string CorrelationId { get; set; }

        protected EventBase()
        {
            EventId = Guid.NewGuid();
            Timestamp = DateTime.UtcNow;
            EventType = GetType().Name;
            Version = 1;
        }

        protected EventBase(Guid aggregateId, string userId = null, string correlationId = null) : this()
        {
            AggregateId = aggregateId;
            UserId = userId;
            CorrelationId = correlationId ?? Guid.NewGuid().ToString();
        }
    }
}