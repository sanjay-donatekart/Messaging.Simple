﻿using System;
using RabbitMQ.Client;

namespace Messaging.Simple
{
    public abstract class Connection : IDisposable
    {
        private readonly IMessageLogger messageLogger;
        protected readonly ConnectionConfiguration connectionConfiguration;
        private readonly IConnection connection;
        protected readonly IModel Channel;

        protected Connection(IMessageLogger messageLogger,
            ConnectionConfiguration configuration)
        {
            this.messageLogger = messageLogger;
            connectionConfiguration = configuration;

            var factory = new ConnectionFactory
            {
                HostName = connectionConfiguration.HostName,
                AutomaticRecoveryEnabled = true
            };
            if (!string.IsNullOrEmpty(connectionConfiguration.UserName))
            {
                factory.Uri = new Uri(connectionConfiguration.Uri);
            }

            connection = factory.CreateConnection();
            Channel = connection.CreateModel();
            Channel.BasicReturn += Channel_BasicReturn;

            Channel.ExchangeDeclare(exchange: connectionConfiguration.Exchange, type: "direct");
            Channel.ExchangeDeclare(exchange: connectionConfiguration.PoisionExchange, type: "topic");
            Channel.ExchangeDeclare(exchange: connectionConfiguration.UndeliveredExchange, type: "topic");

            Bind(connectionConfiguration.PoisionQueueName, "#", connectionConfiguration.PoisionExchange);
            Bind(connectionConfiguration.UndeliveredQueueName, "#", connectionConfiguration.UndeliveredExchange);
        }

        private void Channel_BasicReturn(object sender, RabbitMQ.Client.Events.BasicReturnEventArgs e)
        {
            Channel.BasicPublish(exchange: connectionConfiguration.UndeliveredExchange,
                routingKey: e.RoutingKey,
                basicProperties: e.BasicProperties,
                body: e.Body);
        }

        public void Bind(string queue, string routingKey, string exchange)
        {
            Channel.QueueDeclare(queue: queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            Channel.QueueBind(queue: queue,
                exchange: exchange,
                routingKey: routingKey);
        }

        public void Dispose()
        {
            Channel?.Close();
            connection?.Close();
            Channel?.Dispose();
            connection?.Dispose();
            messageLogger.Info($"Disconnected from RabbitMQ {connectionConfiguration.HostName}");
        }
    }
}