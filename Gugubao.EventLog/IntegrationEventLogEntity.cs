using Gugubao.Rabbitmq;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Gugubao.IntegrationEventLog
{
    public class IIntegrationEventLogEntity
    {
        public IIntegrationEventLogEntity()
        {
        }

        public IIntegrationEventLogEntity(IntegrationEvent @event, Guid transactionId)
        {
            EventId = @event.Id;
            CreateTime = @event.CreateTime;
            EventTypeName = @event.GetType().FullName;
            Content = JsonSerializer.Serialize(@event, @event.GetType());
            State = EventStateEnum.NotPublished;
            TimesSent = 0;
            TransactionId = transactionId.ToString();
        }

        [NotMapped]
        public IntegrationEvent IntegrationEvent { get; set; }

        public long EventId { get; private set; }

        public string EventTypeName { get; private set; }

        public EventStateEnum State { get; set; }

        public int TimesSent { get; set; }

        public DateTime CreateTime { get; private set; }

        public string Content { get; private set; }

        public string TransactionId { get; private set; }

        public IIntegrationEventLogEntity DeserializeJsonContent(Type type)
        {
            IntegrationEvent = JsonSerializer.Deserialize(Content, type) as IntegrationEvent;
            return this;
        }
    }
}
