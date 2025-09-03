using Microsoft.Extensions.Logging;
using RabbitMQEventSourceStore.Core.Consuming;
using RabbitMQEventSourceStore.Core.Models;
using RabbitMQEventSourceStore.Sample.Events;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RabbitMQEventSourceStore.Sample.Handlers
{
    public class OrderEventHandler : IEventHandler
    {
        private readonly ILogger<OrderEventHandler> _logger;

        public OrderEventHandler(ILogger<OrderEventHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task HandleAsync(IEvent @event)
        {
            if (@event is OrderPlacedEvent orderPlaced)
            {
                await HandleOrderPlacedAsync(orderPlaced);
            }
        }

        public bool CanHandle(string eventType)
        {
            return eventType == nameof(OrderPlacedEvent);
        }

        private async Task HandleOrderPlacedAsync(OrderPlacedEvent @event)
        {
            _logger.LogInformation(
                "Order placed: {OrderNumber} for customer {CustomerId}, Total: ${TotalAmount:F2}", 
                @event.OrderNumber, 
                @event.CustomerId, 
                @event.TotalAmount);
            
            foreach (var item in @event.Items)
            {
                _logger.LogInformation(
                    "  - {ProductName} x{Quantity} @ ${UnitPrice:F2} = ${TotalPrice:F2}",
                    item.ProductName,
                    item.Quantity,
                    item.UnitPrice,
                    item.TotalPrice);
            }
            
            // Here you would typically:
            // - Update inventory
            // - Send order confirmation email
            // - Update customer order history
            // - Generate invoice
            // - Trigger shipping workflow
            
            await Task.CompletedTask;
        }
    }
}