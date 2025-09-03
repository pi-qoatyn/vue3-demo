using System;
using System.Collections.Generic;

namespace RabbitMQEventSourceStore.Core.Models
{
    /// <summary>
    /// Wrapper for events with metadata
    /// </summary>
    public class EventEnvelope
    {
        /// <summary>
        /// The actual event data
        /// </summary>
        public IEvent Event { get; set; }

        /// <summary>
        /// Metadata associated with the event
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; }

        /// <summary>
        /// Sequence number for ordering
        /// </summary>
        public long SequenceNumber { get; set; }

        /// <summary>
        /// Stream name where this event belongs
        /// </summary>
        public string StreamName { get; set; }

        /// <summary>
        /// Position in the stream
        /// </summary>
        public long StreamPosition { get; set; }

        public EventEnvelope()
        {
            Metadata = new Dictionary<string, string>();
        }

        public EventEnvelope(IEvent @event, string streamName) : this()
        {
            Event = @event ?? throw new ArgumentNullException(nameof(@event));
            StreamName = streamName ?? throw new ArgumentNullException(nameof(streamName));
        }
    }
}