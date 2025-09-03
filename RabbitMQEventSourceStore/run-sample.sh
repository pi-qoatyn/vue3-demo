#!/bin/bash

echo "Starting RabbitMQ Event Source Store Sample"
echo "==========================================="
echo ""

# Check if RabbitMQ is running
if ! docker ps | grep -q rabbitmq-eventsource; then
    echo "Starting RabbitMQ using docker-compose..."
    docker-compose up -d
    echo "Waiting for RabbitMQ to be ready..."
    sleep 10
else
    echo "RabbitMQ is already running."
fi

echo ""
echo "Building the solution..."
dotnet build

echo ""
echo "Running the sample application..."
echo "Note: The application will generate random events every 1-5 seconds."
echo "Press Ctrl+C to stop."
echo ""

cd src/RabbitMQEventSourceStore.Sample
dotnet run