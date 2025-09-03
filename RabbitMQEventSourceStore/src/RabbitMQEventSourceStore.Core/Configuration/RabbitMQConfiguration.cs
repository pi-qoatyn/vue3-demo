namespace RabbitMQEventSourceStore.Core.Configuration
{
    /// <summary>
    /// RabbitMQ connection configuration
    /// </summary>
    public class RabbitMQConfiguration
    {
        /// <summary>
        /// RabbitMQ host name or IP address
        /// </summary>
        public string HostName { get; set; } = "localhost";

        /// <summary>
        /// RabbitMQ port number
        /// </summary>
        public int Port { get; set; } = 5672;

        /// <summary>
        /// RabbitMQ username
        /// </summary>
        public string UserName { get; set; } = "guest";

        /// <summary>
        /// RabbitMQ password
        /// </summary>
        public string Password { get; set; } = "guest";

        /// <summary>
        /// Virtual host to connect to
        /// </summary>
        public string VirtualHost { get; set; } = "/";

        /// <summary>
        /// Exchange name for events
        /// </summary>
        public string ExchangeName { get; set; } = "event-source-exchange";

        /// <summary>
        /// Exchange type (direct, topic, fanout, headers)
        /// </summary>
        public string ExchangeType { get; set; } = "topic";

        /// <summary>
        /// Dead letter exchange name
        /// </summary>
        public string DeadLetterExchange { get; set; } = "event-source-dlx";

        /// <summary>
        /// Connection retry count
        /// </summary>
        public int RetryCount { get; set; } = 3;

        /// <summary>
        /// Connection retry delay in seconds
        /// </summary>
        public int RetryDelaySeconds { get; set; } = 5;

        /// <summary>
        /// Automatic recovery enabled
        /// </summary>
        public bool AutomaticRecoveryEnabled { get; set; } = true;

        /// <summary>
        /// Network recovery interval in seconds
        /// </summary>
        public int NetworkRecoveryIntervalSeconds { get; set; } = 10;
    }
}