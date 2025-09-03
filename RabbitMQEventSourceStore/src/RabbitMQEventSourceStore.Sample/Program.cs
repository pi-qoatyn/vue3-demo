using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQEventSourceStore.Core.Consuming;
using RabbitMQEventSourceStore.Core.Extensions;
using RabbitMQEventSourceStore.Sample.Handlers;
using RabbitMQEventSourceStore.Sample.Services;
using System;
using System.Threading.Tasks;

namespace RabbitMQEventSourceStore.Sample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("RabbitMQ Event Source Store Sample");
            Console.WriteLine("==================================");
            Console.WriteLine();

            var host = CreateHostBuilder(args).Build();

            // Setup event consumers
            using (var scope = host.Services.CreateScope())
            {
                var consumer = scope.ServiceProvider.GetRequiredService<EventConsumer>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                // Register event handlers
                consumer.RegisterHandler(new UserEventHandler(
                    scope.ServiceProvider.GetRequiredService<ILogger<UserEventHandler>>()));
                consumer.RegisterHandler(new OrderEventHandler(
                    scope.ServiceProvider.GetRequiredService<ILogger<OrderEventHandler>>()));

                // Start consuming from streams
                try
                {
                    await consumer.StartConsumingStreamAsync("users");
                    await consumer.StartConsumingStreamAsync("orders");
                    logger.LogInformation("Started consuming events from users and orders streams");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to start event consumption");
                }
            }

            // Run the host
            await host.RunAsync();
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Add RabbitMQ Event Source Store
                    services.AddRabbitMQEventSourceStore(hostContext.Configuration);

                    // Add background service for producing events
                    services.AddHostedService<EventProducerService>();

                    // Configure logging
                    services.AddLogging(config =>
                    {
                        config.AddConsole();
                        config.SetMinimumLevel(LogLevel.Information);
                    });
                })
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                });
    }
}