using Gugubao.Rabbitmq;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Gugubao.Domain.Infrastructure
{
    public interface IEventBusService
    {
        Task PublishEventsAsync(Guid transactionId);

        Task AddAndSaveEventAsync(IntegrationEvent evt);
    }
}
