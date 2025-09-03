using RabbitMQEventSourceStore.Core.Models;
using System.Threading.Tasks;

namespace RabbitMQEventSourceStore.Core.Consuming
{
    /// <summary>
    /// Interface for handling events
    /// </summary>
    /// <typeparam name="TEvent">Type of event to handle</typeparam>
    public interface IEventHandler<in TEvent> where TEvent : IEvent
    {
        /// <summary>
        /// Handles the event
        /// </summary>
        /// <param name="event">Event to handle</param>
        /// <returns>Task</returns>
        Task HandleAsync(TEvent @event);
    }

    /// <summary>
    /// Generic event handler for all events
    /// </summary>
    public interface IEventHandler
    {
        /// <summary>
        /// Handles any event
        /// </summary>
        /// <param name="event">Event to handle</param>
        /// <returns>Task</returns>
        Task HandleAsync(IEvent @event);

        /// <summary>
        /// Checks if this handler can handle the given event type
        /// </summary>
        /// <param name="eventType">Type of the event</param>
        /// <returns>True if can handle</returns>
        bool CanHandle(string eventType);
    }
}