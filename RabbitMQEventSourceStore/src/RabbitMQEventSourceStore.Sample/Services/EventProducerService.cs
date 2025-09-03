using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQEventSourceStore.Core.Publishing;
using RabbitMQEventSourceStore.Sample.Events;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQEventSourceStore.Sample.Services
{
    /// <summary>
    /// Background service that produces sample events
    /// </summary>
    public class EventProducerService : BackgroundService
    {
        private readonly ILogger<EventProducerService> _logger;
        private readonly IEventPublisher _eventPublisher;
        private readonly Random _random = new Random();

        public EventProducerService(
            ILogger<EventProducerService> logger,
            IEventPublisher eventPublisher)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Event Producer Service started");

            // Wait a bit for everything to initialize
            await Task.Delay(2000, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Generate random events
                    var eventType = _random.Next(0, 3);
                    
                    switch (eventType)
                    {
                        case 0:
                            await PublishUserCreatedEvent();
                            break;
                        case 1:
                            await PublishUserUpdatedEvent();
                            break;
                        case 2:
                            await PublishOrderPlacedEvent();
                            break;
                    }

                    // Wait 1-5 seconds between events
                    var delay = _random.Next(1000, 5000);
                    await Task.Delay(delay, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error producing events");
                    await Task.Delay(5000, stoppingToken);
                }
            }

            _logger.LogInformation("Event Producer Service stopped");
        }

        private async Task PublishUserCreatedEvent()
        {
            var userId = Guid.NewGuid();
            var usernames = new[] { "john_doe", "jane_smith", "bob_wilson", "alice_brown", "charlie_davis" };
            var firstNames = new[] { "John", "Jane", "Bob", "Alice", "Charlie" };
            var lastNames = new[] { "Doe", "Smith", "Wilson", "Brown", "Davis" };
            
            var index = _random.Next(0, usernames.Length);
            
            var @event = new UserCreatedEvent(
                userId,
                usernames[index] + _random.Next(1000, 9999),
                $"{usernames[index]}@example.com",
                firstNames[index],
                lastNames[index],
                "system");

            await _eventPublisher.PublishAsync("users", @event);
            _logger.LogInformation("Published UserCreatedEvent for {Username}", @event.Username);
        }

        private async Task PublishUserUpdatedEvent()
        {
            var userId = Guid.NewGuid();
            var fields = new[] { "Email", "FirstName", "LastName", "Phone", "Address" };
            var field = fields[_random.Next(0, fields.Length)];
            
            var @event = new UserUpdatedEvent(
                userId,
                field,
                $"old_{field}_value",
                $"new_{field}_value",
                "system");

            await _eventPublisher.PublishAsync("users", @event);
            _logger.LogInformation("Published UserUpdatedEvent for field {Field}", field);
        }

        private async Task PublishOrderPlacedEvent()
        {
            var orderId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{_random.Next(1000, 9999)}";
            
            var products = new[]
            {
                ("Laptop", 999.99m),
                ("Mouse", 29.99m),
                ("Keyboard", 79.99m),
                ("Monitor", 299.99m),
                ("Headphones", 149.99m)
            };
            
            var items = new List<OrderItem>();
            var itemCount = _random.Next(1, 4);
            var totalAmount = 0m;
            
            for (int i = 0; i < itemCount; i++)
            {
                var product = products[_random.Next(0, products.Length)];
                var quantity = _random.Next(1, 3);
                var item = new OrderItem
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = product.Item1,
                    UnitPrice = product.Item2,
                    Quantity = quantity
                };
                items.Add(item);
                totalAmount += item.TotalPrice;
            }
            
            var @event = new OrderPlacedEvent(
                orderId,
                orderNumber,
                customerId,
                totalAmount,
                items,
                "customer");

            await _eventPublisher.PublishAsync("orders", @event);
            _logger.LogInformation("Published OrderPlacedEvent {OrderNumber} with {ItemCount} items", 
                orderNumber, items.Count);
        }
    }
}