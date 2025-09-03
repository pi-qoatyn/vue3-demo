using Microsoft.Extensions.Logging;
using RabbitMQEventSourceStore.Core.Consuming;
using RabbitMQEventSourceStore.Core.Models;
using RabbitMQEventSourceStore.Sample.Events;
using System;
using System.Threading.Tasks;

namespace RabbitMQEventSourceStore.Sample.Handlers
{
    public class UserEventHandler : IEventHandler
    {
        private readonly ILogger<UserEventHandler> _logger;

        public UserEventHandler(ILogger<UserEventHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task HandleAsync(IEvent @event)
        {
            switch (@event)
            {
                case UserCreatedEvent userCreated:
                    await HandleUserCreatedAsync(userCreated);
                    break;
                case UserUpdatedEvent userUpdated:
                    await HandleUserUpdatedAsync(userUpdated);
                    break;
                default:
                    _logger.LogWarning("Unhandled event type: {EventType}", @event.EventType);
                    break;
            }
        }

        public bool CanHandle(string eventType)
        {
            return eventType == nameof(UserCreatedEvent) || 
                   eventType == nameof(UserUpdatedEvent);
        }

        private async Task HandleUserCreatedAsync(UserCreatedEvent @event)
        {
            _logger.LogInformation(
                "User created: {Username} ({Email}) - Name: {FirstName} {LastName}, AggregateId: {AggregateId}", 
                @event.Username, 
                @event.Email, 
                @event.FirstName, 
                @event.LastName, 
                @event.AggregateId);
            
            // Here you would typically:
            // - Update read models
            // - Send notifications
            // - Trigger workflows
            // - Update search indexes
            
            await Task.CompletedTask;
        }

        private async Task HandleUserUpdatedAsync(UserUpdatedEvent @event)
        {
            _logger.LogInformation(
                "User updated: AggregateId: {AggregateId}, Field: {Field}, Old: {OldValue}, New: {NewValue}", 
                @event.AggregateId,
                @event.UpdatedField,
                @event.OldValue,
                @event.NewValue);
            
            await Task.CompletedTask;
        }
    }
}