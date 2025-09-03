using RabbitMQEventSourceStore.Core.Models;
using System;

namespace RabbitMQEventSourceStore.Sample.Events
{
    public class UserUpdatedEvent : EventBase
    {
        public string UpdatedField { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public DateTime UpdatedAt { get; set; }

        public UserUpdatedEvent() : base() 
        {
            UpdatedAt = DateTime.UtcNow;
        }

        public UserUpdatedEvent(Guid aggregateId, string updatedField, 
            string oldValue, string newValue, string userId = null) 
            : base(aggregateId, userId)
        {
            UpdatedField = updatedField;
            OldValue = oldValue;
            NewValue = newValue;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}