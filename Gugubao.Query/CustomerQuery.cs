using AutoMapper;
using Gugubao.Data;
using Gugubao.Domain.Infrastructure;
using Gugubao.Entity;
using Gugubao.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace Gugubao.Query
{
    public class CustomerQuery
    {
        private readonly MySqlDbContext _dbContext;
        private readonly IMapper _mapper;

        public CustomerQuery(MySqlDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<PagingResult<CustomerViewModel>> GetAllAsync(CustomerPaingModel model)
        {
            var queryableRecords = _dbContext.Set<Customer>();

            var totalCount = await queryableRecords.CountAsync();

            var records = await queryableRecords.PageToListAsync(model.CurPage, model.PageSize);

            var result = _mapper.Map<List<CustomerViewModel>>(records);

            return result.ToPaging(totalCount, model.PageSize);
        }

        public async Task<CustomerViewModel> GetByPhoneAsync(string phone)
        {
            var customer = await _dbContext.Set<Customer>().FirstOrDefaultAsync(c => c.Phone == phone);

            return _mapper.Map<CustomerViewModel>(customer);
        }
    }
}