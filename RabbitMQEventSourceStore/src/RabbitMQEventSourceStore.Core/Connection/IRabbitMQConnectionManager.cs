using RabbitMQ.Client;
using System;

namespace RabbitMQEventSourceStore.Core.Connection
{
    /// <summary>
    /// Interface for managing RabbitMQ connections
    /// </summary>
    public interface IRabbitMQConnectionManager : IDisposable
    {
        /// <summary>
        /// Gets the RabbitMQ connection
        /// </summary>
        IConnection Connection { get; }

        /// <summary>
        /// Creates a new channel
        /// </summary>
        /// <returns>A new IModel (channel)</returns>
        IModel CreateChannel();

        /// <summary>
        /// Checks if the connection is open
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Attempts to connect to RabbitMQ
        /// </summary>
        /// <returns>True if connection successful</returns>
        bool TryConnect();
    }
}