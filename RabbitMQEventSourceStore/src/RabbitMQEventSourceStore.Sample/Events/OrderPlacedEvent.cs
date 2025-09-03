using RabbitMQEventSourceStore.Core.Models;
using System;
using System.Collections.Generic;

namespace RabbitMQEventSourceStore.Sample.Events
{
    public class OrderPlacedEvent : EventBase
    {
        public string OrderNumber { get; set; }
        public Guid CustomerId { get; set; }
        public decimal TotalAmount { get; set; }
        public List<OrderItem> Items { get; set; }
        public DateTime OrderDate { get; set; }

        public OrderPlacedEvent() : base() 
        {
            Items = new List<OrderItem>();
            OrderDate = DateTime.UtcNow;
        }

        public OrderPlacedEvent(Guid aggregateId, string orderNumber, 
            Guid customerId, decimal totalAmount, List<OrderItem> items, string userId = null) 
            : base(aggregateId, userId)
        {
            OrderNumber = orderNumber;
            CustomerId = customerId;
            TotalAmount = totalAmount;
            Items = items ?? new List<OrderItem>();
            OrderDate = DateTime.UtcNow;
        }
    }

    public class OrderItem
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;
    }
}