using Gugubao.Rabbitmq;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Gugubao.IntegrationEventLog
{
    public interface IIntegrationEventLogService
    {
        Task<IEnumerable<IIntegrationEventLogEntity>> PendingsToPublishAsync(Guid transactionId, Assembly assembly);

        Task SaveAsync(IntegrationEvent integrationEvent, IDbContextTransaction transaction);

        Task UpdateEventStateAsync(long eventId, EventStateEnum eventState);
    }
}
