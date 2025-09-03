using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQEventSourceStore.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace RabbitMQEventSourceStore.Core.Serialization
{
    /// <summary>
    /// JSON-based event serializer implementation
    /// </summary>
    public class JsonEventSerializer : IEventSerializer
    {
        private readonly ILogger<JsonEventSerializer> _logger;
        private readonly JsonSerializerSettings _settings;
        private readonly Dictionary<string, Type> _eventTypes;

        public JsonEventSerializer(ILogger<JsonEventSerializer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                NullValueHandling = NullValueHandling.Ignore
            };

            _eventTypes = new Dictionary<string, Type>();
            LoadEventTypes();
        }

        public byte[] Serialize(IEvent @event)
        {
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));

            try
            {
                var json = JsonConvert.SerializeObject(@event, _settings);
                return Encoding.UTF8.GetBytes(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to serialize event of type {EventType}", @event.GetType().Name);
                throw;
            }
        }

        public IEvent Deserialize(byte[] data, string eventType)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentNullException(nameof(data));
            
            if (string.IsNullOrEmpty(eventType))
                throw new ArgumentNullException(nameof(eventType));

            try
            {
                var json = Encoding.UTF8.GetString(data);
                
                if (_eventTypes.TryGetValue(eventType, out var type))
                {
                    return (IEvent)JsonConvert.DeserializeObject(json, type, _settings);
                }
                else
                {
                    _logger.LogWarning("Unknown event type: {EventType}. Deserializing as dynamic object.", eventType);
                    return JsonConvert.DeserializeObject<DynamicEvent>(json, _settings);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize event of type {EventType}", eventType);
                throw;
            }
        }

        public byte[] SerializeEnvelope(EventEnvelope envelope)
        {
            if (envelope == null)
                throw new ArgumentNullException(nameof(envelope));

            try
            {
                var envelopeData = new
                {
                    Event = JsonConvert.SerializeObject(envelope.Event, _settings),
                    EventType = envelope.Event.GetType().Name,
                    envelope.Metadata,
                    envelope.SequenceNumber,
                    envelope.StreamName,
                    envelope.StreamPosition
                };

                var json = JsonConvert.SerializeObject(envelopeData, _settings);
                return Encoding.UTF8.GetBytes(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to serialize event envelope");
                throw;
            }
        }

        public EventEnvelope DeserializeEnvelope(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentNullException(nameof(data));

            try
            {
                var json = Encoding.UTF8.GetString(data);
                dynamic envelopeData = JsonConvert.DeserializeObject(json, _settings);
                
                var eventJson = (string)envelopeData.Event;
                var eventType = (string)envelopeData.EventType;
                var eventBytes = Encoding.UTF8.GetBytes(eventJson);
                
                var @event = Deserialize(eventBytes, eventType);
                
                return new EventEnvelope
                {
                    Event = @event,
                    Metadata = envelopeData.Metadata?.ToObject<Dictionary<string, string>>() ?? new Dictionary<string, string>(),
                    SequenceNumber = (long)envelopeData.SequenceNumber,
                    StreamName = (string)envelopeData.StreamName,
                    StreamPosition = (long)envelopeData.StreamPosition
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize event envelope");
                throw;
            }
        }

        private void LoadEventTypes()
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                var eventTypes = assemblies
                    .SelectMany(a => a.GetTypes())
                    .Where(t => !t.IsAbstract && !t.IsInterface && typeof(IEvent).IsAssignableFrom(t))
                    .ToList();

                foreach (var type in eventTypes)
                {
                    _eventTypes[type.Name] = type;
                    _logger.LogDebug("Registered event type: {EventType}", type.Name);
                }

                _logger.LogInformation("Loaded {Count} event types", _eventTypes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load event types");
            }
        }

        /// <summary>
        /// Dynamic event class for unknown event types
        /// </summary>
        private class DynamicEvent : EventBase
        {
            public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        }
    }
}