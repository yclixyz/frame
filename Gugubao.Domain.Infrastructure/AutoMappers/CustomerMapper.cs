using AutoMapper;
using Gugubao.Entity;
using Gugubao.Utility;

namespace Gugubao.Domain.Infrastructure
{
    public class CustomerMapper : Profile
    {
        public CustomerMapper()
        {
            CreateMap<CreateCustomerCommand, Customer>()
            .ForMember(c => c.Id, opt =>
            {
                opt.MapFrom((x, y) => { return SnowflakeId.Default().NextId(); });
            });

            CreateMap<Customer, CustomerViewModel>().ReverseMap();
        }
    }
}
