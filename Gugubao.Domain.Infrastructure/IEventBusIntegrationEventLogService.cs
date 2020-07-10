using Gugubao.Rabbitmq;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Gugubao.Domain.Infrastructure
{
    public interface IEventBusIntegrationEventLogService
    {
        Task PublishEventsAsync(Guid transactionId);

        Task AddAndSaveEventAsync(IntegrationEvent evt);
    }
}
