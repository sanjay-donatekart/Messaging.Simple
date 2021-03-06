﻿namespace Messaging.Simple
{
    public interface IHandlerFactory
    {
        void Release(IMessageHandler messageHandler);
        IMessageHandler Resolve(string routingKey);
    }
}