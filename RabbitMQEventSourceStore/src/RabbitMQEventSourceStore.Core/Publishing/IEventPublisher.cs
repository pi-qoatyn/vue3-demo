using RabbitMQEventSourceStore.Core.Models;
using System.Threading.Tasks;

namespace RabbitMQEventSourceStore.Core.Publishing
{
    /// <summary>
    /// Interface for publishing events
    /// </summary>
    public interface IEventPublisher
    {
        /// <summary>
        /// Publishes an event to the specified stream
        /// </summary>
        /// <param name="streamName">Name of the stream</param>
        /// <param name="event">Event to publish</param>
        /// <returns>Task</returns>
        Task PublishAsync(string streamName, IEvent @event);

        /// <summary>
        /// Publishes an event to a stream based on aggregate ID
        /// </summary>
        /// <param name="event">Event to publish</param>
        /// <returns>Task</returns>
        Task PublishAsync(IEvent @event);
    }
}