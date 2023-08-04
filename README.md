# Event Driven Microservices with RabbitMQ and C#
This project demonstrates an event driven microservice architecture using RabbitMQ and C#/.NET Core.

## Overview
The repo contains a basic implementation of:

- An Post service
- A User service
- RabbitMQ for message queuing
The services publish events to RabbitMQ on certain actions. Other services consume these events and react accordingly.

Some key concepts:

- Building independently deployable microservices with .NET Core
- Using RabbitMQ for event streaming between services
- Event-driven architecture with producers and consumers
- C# implementation of each service
  
Services

Users

Manages User activities and status. Publishes events on new user and status changes.

Post

Manages Post data. Reacts to new users by creating a user record.

Running the Services
The services can be started independently. RabbitMQ needs to be installed and running.


Notes
This provides a simple starting point for an event driven C# microservice architecture with RabbitMQ.
