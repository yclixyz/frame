using Gugubao.Data;
using Gugubao.Utility;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Gugubao.Extensions
{
    public class TransactionBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly ILogger<TransactionBehaviour<TRequest, TResponse>> _logger;
        private readonly MySqlDbContext _dbContext;
        //private readonly IEventBusService _eventBusService;

        public TransactionBehaviour(MySqlDbContext dbContext,
            //IEventBusService eventBusService,
            ILogger<TransactionBehaviour<TRequest, TResponse>> logger)
        {
            _dbContext = dbContext;
            //_eventBusService = eventBusService;
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            var response = default(TResponse);

            try
            {
                if (_dbContext.HasActiveTransaction)
                {
                    return await next();
                }

                var strategy = _dbContext.Database.CreateExecutionStrategy();

                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _dbContext.BeginTransactionAsync();

                    try
                    {
                        response = await next();
                    }
                    catch (Exception ex)
                    {
                        // 释放数据库事务
                        _dbContext.RollbackTransaction();

                        throw ex;
                    }

                    await _dbContext.CommitTransactionAsync(transaction);

                    var transactionId = transaction.TransactionId;

                    //await _eventBusService.PublishEventsAsync(transactionId);
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR Handling transaction for {CommandName} ({@Command})", request.GetGenericTypeName(), request);

                throw ex;
            }
        }
    }
}
