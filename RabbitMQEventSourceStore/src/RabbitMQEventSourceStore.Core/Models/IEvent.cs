using System;

namespace RabbitMQEventSourceStore.Core.Models
{
    /// <summary>
    /// Base interface for all events
    /// </summary>
    public interface IEvent
    {
        /// <summary>
        /// Unique identifier for the event
        /// </summary>
        Guid EventId { get; }

        /// <summary>
        /// Timestamp when the event occurred
        /// </summary>
        DateTime Timestamp { get; }

        /// <summary>
        /// Type of the event
        /// </summary>
        string EventType { get; }

        /// <summary>
        /// Version of the event schema
        /// </summary>
        int Version { get; }

        /// <summary>
        /// Aggregate ID this event belongs to
        /// </summary>
        Guid AggregateId { get; }

        /// <summary>
        /// User who triggered the event
        /// </summary>
        string UserId { get; }

        /// <summary>
        /// Correlation ID for tracking related events
        /// </summary>
        string CorrelationId { get; }
    }
}