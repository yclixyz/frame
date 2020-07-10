using AutoMapper;
using Gugubao.Data;
using Gugubao.Domain.Infrastructure;
using Gugubao.Entity;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Gugubao.Handler
{
    /// <summary>
    /// 添加客户Handler
    /// </summary>
    public class CreateCustomerHandler : IRequestHandler<CreateCustomerCommand, bool>
    {
        private readonly MySqlDbContext _dbContext;
        private readonly IMapper _mapper;

        public CreateCustomerHandler(MySqlDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<bool> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
        {
            var customer = _mapper.Map<Customer>(request);

            await _dbContext.AddAsync(customer);

            return true;
        }
    }
}
