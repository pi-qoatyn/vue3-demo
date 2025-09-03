# RabbitMQ Event Source Store

A .NET Core library for implementing Event Sourcing with RabbitMQ as the message broker. This library provides a simple and scalable way to publish and consume events using RabbitMQ.

## Features

- **Event Publishing**: Publish events to RabbitMQ exchanges with configurable routing
- **Event Consumption**: Subscribe to event streams with automatic message handling
- **Connection Management**: Robust connection management with retry logic and automatic recovery
- **Serialization**: Built-in JSON serialization with support for custom serializers
- **Event Handlers**: Simple interface for implementing event handlers
- **Configuration**: Flexible configuration options via appsettings.json or code
- **Dead Letter Exchange**: Automatic DLX configuration for failed messages
- **Stream-based Organization**: Events organized by streams (aggregates)

## Installation

```bash
# Clone the repository
git clone https://github.com/yourusername/RabbitMQEventSourceStore.git

# Build the solution
cd RabbitMQEventSourceStore
dotnet build
```

## Quick Start

### 1. Configure RabbitMQ Connection

Add the following to your `appsettings.json`:

```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "ExchangeName": "event-source-exchange",
    "ExchangeType": "topic",
    "DeadLetterExchange": "event-source-dlx",
    "RetryCount": 3,
    "RetryDelaySeconds": 5,
    "AutomaticRecoveryEnabled": true,
    "NetworkRecoveryIntervalSeconds": 10
  }
}
```

### 2. Register Services

```csharp
services.AddRabbitMQEventSourceStore(Configuration);
```

### 3. Define Events

```csharp
public class UserCreatedEvent : EventBase
{
    public string Username { get; set; }
    public string Email { get; set; }

    public UserCreatedEvent(Guid aggregateId, string username, string email) 
        : base(aggregateId)
    {
        Username = username;
        Email = email;
    }
}
```

### 4. Publish Events

```csharp
public class UserService
{
    private readonly IEventPublisher _eventPublisher;

    public UserService(IEventPublisher eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }

    public async Task CreateUser(string username, string email)
    {
        var userId = Guid.NewGuid();
        var @event = new UserCreatedEvent(userId, username, email);
        
        await _eventPublisher.PublishAsync("users", @event);
    }
}
```

### 5. Handle Events

```csharp
public class UserEventHandler : IEventHandler
{
    private readonly ILogger<UserEventHandler> _logger;

    public UserEventHandler(ILogger<UserEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(IEvent @event)
    {
        if (@event is UserCreatedEvent userCreated)
        {
            _logger.LogInformation($"User created: {userCreated.Username}");
            // Update read models, send notifications, etc.
        }
    }

    public bool CanHandle(string eventType)
    {
        return eventType == nameof(UserCreatedEvent);
    }
}
```

### 6. Start Consuming Events

```csharp
var consumer = serviceProvider.GetRequiredService<EventConsumer>();
consumer.RegisterHandler(new UserEventHandler(logger));
await consumer.StartConsumingStreamAsync("users");
```

## Running the Sample

1. **Start RabbitMQ**:
   ```bash
   docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
   ```

2. **Run the sample application**:
   ```bash
   cd src/RabbitMQEventSourceStore.Sample
   dotnet run
   ```

The sample application will:
- Automatically generate and publish random events (users and orders)
- Consume and log these events in real-time
- Demonstrate event handling patterns

## Architecture

### Core Components

- **IRabbitMQConnectionManager**: Manages RabbitMQ connections with retry logic
- **IEventStore**: Core interface for event storage operations
- **IEventPublisher**: Simplified interface for publishing events
- **EventConsumer**: Manages event consumption and handler routing
- **IEventSerializer**: Handles event serialization/deserialization

### Event Flow

1. Events are published to RabbitMQ exchanges
2. Messages are routed to queues based on routing keys (stream names)
3. Consumers subscribe to queues and receive events
4. Events are deserialized and routed to appropriate handlers
5. Failed messages are sent to dead letter exchange

## Advanced Usage

### Custom Event Serialization

```csharp
public class CustomEventSerializer : IEventSerializer
{
    // Implement custom serialization logic
}

services.AddSingleton<IEventSerializer, CustomEventSerializer>();
```

### Multiple Event Handlers

```csharp
consumer.RegisterHandler(new UserEventHandler(logger));
consumer.RegisterHandler(new OrderEventHandler(logger));
consumer.RegisterHandler(new InventoryEventHandler(logger));
```

### Subscribing to All Events

```csharp
await consumer.StartConsumingAllAsync();
```

## Best Practices

1. **Event Design**: Keep events immutable and focused on what happened
2. **Stream Names**: Use meaningful stream names (e.g., "users", "orders")
3. **Error Handling**: Implement proper error handling in event handlers
4. **Idempotency**: Design handlers to be idempotent
5. **Monitoring**: Monitor RabbitMQ queues and connection health

## Limitations

- This implementation uses RabbitMQ as a message broker, not a persistent event store
- Historical event replay requires additional persistent storage
- Event ordering is guaranteed only within a single queue

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.