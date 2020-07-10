using Gugubao.Rabbitmq;

namespace Gugubao.Domain.Infrastructure
{
    public class CreateCustomerEvent : IntegrationEvent
    {
        public string OldCellPhone { get; set; }

        public string NewCellPhone { get; set; }
    }
}
