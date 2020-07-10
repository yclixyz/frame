using System;
using System.Collections.Generic;
using System.Text;

namespace Gugubao.Rabbitmq
{
    public interface IEventBusManager
    {
        bool IsEmpty { get; }

        void AddSubscription<T, TH>()
           where T : IntegrationEvent
           where TH : IIntegrationEventHandler<T>;

        bool HasSubscriptionsForEvent<T>() where T : IntegrationEvent;

        bool HasSubscriptionsForEvent(string eventName);

        Type GetEventTypeByName(string eventName);

        void Clear();

        IEnumerable<Type> GetHandlersForEvent<T>() where T : IntegrationEvent;

        IEnumerable<Type> GetHandlersForEvent(string eventName);

        string GetEventKey<T>();
    }
}