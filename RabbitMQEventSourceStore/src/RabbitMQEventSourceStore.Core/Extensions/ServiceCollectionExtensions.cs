using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using RabbitMQEventSourceStore.Core.Configuration;
using RabbitMQEventSourceStore.Core.Connection;
using RabbitMQEventSourceStore.Core.Consuming;
using RabbitMQEventSourceStore.Core.Publishing;
using RabbitMQEventSourceStore.Core.Serialization;
using RabbitMQEventSourceStore.Core.Store;
using System;

namespace RabbitMQEventSourceStore.Core.Extensions
{
    /// <summary>
    /// Extension methods for dependency injection
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds RabbitMQ Event Source Store services
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddRabbitMQEventSourceStore(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            return services.AddRabbitMQEventSourceStore(configuration.GetSection("RabbitMQ"));
        }

        /// <summary>
        /// Adds RabbitMQ Event Source Store services
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configurationSection">Configuration section</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddRabbitMQEventSourceStore(
            this IServiceCollection services, 
            IConfigurationSection configurationSection)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configurationSection == null)
                throw new ArgumentNullException(nameof(configurationSection));

            // Register configuration
            services.Configure<RabbitMQConfiguration>(configurationSection);

            // Register core services
            RegisterCoreServices(services);

            return services;
        }

        /// <summary>
        /// Adds RabbitMQ Event Source Store services with custom configuration
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configureOptions">Configuration action</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddRabbitMQEventSourceStore(
            this IServiceCollection services,
            Action<RabbitMQConfiguration> configureOptions)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));

            // Register configuration
            services.Configure(configureOptions);

            // Register core services
            RegisterCoreServices(services);

            return services;
        }

        /// <summary>
        /// Adds an event handler
        /// </summary>
        /// <typeparam name="THandler">Type of handler</typeparam>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddEventHandler<THandler>(this IServiceCollection services)
            where THandler : class, IEventHandler
        {
            services.AddTransient<IEventHandler, THandler>();
            return services;
        }

        private static void RegisterCoreServices(IServiceCollection services)
        {
            services.AddSingleton<IRabbitMQConnectionManager, RabbitMQConnectionManager>();
            services.AddSingleton<IEventSerializer, JsonEventSerializer>();
            services.AddSingleton<IEventStore, RabbitMQEventStore>();
            services.AddTransient<IEventPublisher, EventPublisher>();
            services.AddTransient<EventConsumer>();
        }
    }
}