using RabbitMQEventSourceStore.Core.Models;

namespace RabbitMQEventSourceStore.Core.Serialization
{
    /// <summary>
    /// Interface for event serialization
    /// </summary>
    public interface IEventSerializer
    {
        /// <summary>
        /// Serializes an event to bytes
        /// </summary>
        /// <param name="event">Event to serialize</param>
        /// <returns>Serialized event as byte array</returns>
        byte[] Serialize(IEvent @event);

        /// <summary>
        /// Deserializes bytes to an event
        /// </summary>
        /// <param name="data">Byte array to deserialize</param>
        /// <param name="eventType">Type of the event</param>
        /// <returns>Deserialized event</returns>
        IEvent Deserialize(byte[] data, string eventType);

        /// <summary>
        /// Serializes an event envelope to bytes
        /// </summary>
        /// <param name="envelope">Event envelope to serialize</param>
        /// <returns>Serialized envelope as byte array</returns>
        byte[] SerializeEnvelope(EventEnvelope envelope);

        /// <summary>
        /// Deserializes bytes to an event envelope
        /// </summary>
        /// <param name="data">Byte array to deserialize</param>
        /// <returns>Deserialized event envelope</returns>
        EventEnvelope DeserializeEnvelope(byte[] data);
    }
}