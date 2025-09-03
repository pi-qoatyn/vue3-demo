using RabbitMQEventSourceStore.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RabbitMQEventSourceStore.Core.Store
{
    /// <summary>
    /// Interface for event store operations
    /// </summary>
    public interface IEventStore
    {
        /// <summary>
        /// Appends events to a stream
        /// </summary>
        /// <param name="streamName">Name of the stream</param>
        /// <param name="events">Events to append</param>
        /// <param name="expectedVersion">Expected version for optimistic concurrency</param>
        /// <returns>Task</returns>
        Task AppendToStreamAsync(string streamName, IEnumerable<IEvent> events, long? expectedVersion = null);

        /// <summary>
        /// Appends a single event to a stream
        /// </summary>
        /// <param name="streamName">Name of the stream</param>
        /// <param name="event">Event to append</param>
        /// <param name="expectedVersion">Expected version for optimistic concurrency</param>
        /// <returns>Task</returns>
        Task AppendToStreamAsync(string streamName, IEvent @event, long? expectedVersion = null);

        /// <summary>
        /// Reads events from a stream
        /// </summary>
        /// <param name="streamName">Name of the stream</param>
        /// <param name="fromVersion">Starting version (inclusive)</param>
        /// <param name="toVersion">Ending version (inclusive)</param>
        /// <returns>List of event envelopes</returns>
        Task<IEnumerable<EventEnvelope>> ReadStreamAsync(string streamName, long fromVersion = 0, long? toVersion = null);

        /// <summary>
        /// Reads all events from all streams
        /// </summary>
        /// <param name="fromPosition">Starting position</param>
        /// <param name="maxCount">Maximum number of events to read</param>
        /// <returns>List of event envelopes</returns>
        Task<IEnumerable<EventEnvelope>> ReadAllAsync(long fromPosition = 0, int maxCount = 1000);

        /// <summary>
        /// Subscribes to a stream for real-time updates
        /// </summary>
        /// <param name="streamName">Name of the stream</param>
        /// <param name="onEventReceived">Callback when event is received</param>
        /// <param name="fromVersion">Starting version</param>
        /// <returns>Subscription ID</returns>
        Task<string> SubscribeToStreamAsync(string streamName, Func<EventEnvelope, Task> onEventReceived, long? fromVersion = null);

        /// <summary>
        /// Subscribes to all streams
        /// </summary>
        /// <param name="onEventReceived">Callback when event is received</param>
        /// <param name="fromPosition">Starting position</param>
        /// <returns>Subscription ID</returns>
        Task<string> SubscribeToAllAsync(Func<EventEnvelope, Task> onEventReceived, long? fromPosition = null);

        /// <summary>
        /// Unsubscribes from a stream
        /// </summary>
        /// <param name="subscriptionId">Subscription ID to cancel</param>
        /// <returns>Task</returns>
        Task UnsubscribeAsync(string subscriptionId);

        /// <summary>
        /// Gets the current version of a stream
        /// </summary>
        /// <param name="streamName">Name of the stream</param>
        /// <returns>Current version or null if stream doesn't exist</returns>
        Task<long?> GetStreamVersionAsync(string streamName);

        /// <summary>
        /// Deletes a stream
        /// </summary>
        /// <param name="streamName">Name of the stream to delete</param>
        /// <returns>Task</returns>
        Task DeleteStreamAsync(string streamName);
    }
}