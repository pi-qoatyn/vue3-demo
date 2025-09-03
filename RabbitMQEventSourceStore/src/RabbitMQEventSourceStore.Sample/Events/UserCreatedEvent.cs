using RabbitMQEventSourceStore.Core.Models;
using System;

namespace RabbitMQEventSourceStore.Sample.Events
{
    public class UserCreatedEvent : EventBase
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime CreatedAt { get; set; }

        public UserCreatedEvent() : base() 
        {
            CreatedAt = DateTime.UtcNow;
        }

        public UserCreatedEvent(Guid aggregateId, string username, string email, 
            string firstName, string lastName, string userId = null) 
            : base(aggregateId, userId)
        {
            Username = username;
            Email = email;
            FirstName = firstName;
            LastName = lastName;
            CreatedAt = DateTime.UtcNow;
        }
    }
}