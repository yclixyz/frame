using Gugubao.Rabbitmq;
using System.Threading.Tasks;

namespace Gugubao.Domain.Infrastructure
{
    public class CreateCustomerEventEventHandler : IIntegrationEventHandler<CreateCustomerEvent>
    {
        public async Task Handle(CreateCustomerEvent integrationEvent)
        {
            await Task.CompletedTask;
        }
    }
}
