using Gugubao.Data;
using Gugubao.IntegrationEventLog;
using Gugubao.Rabbitmq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Data.Common;
using System.Reflection;
using System.Threading.Tasks;

namespace Gugubao.Domain.Infrastructure
{
    public class EventBusService : IEventBusService
    {
        private readonly IEventBus _eventBus;
        private readonly MySqlDbContext _dbContext;
        private readonly ILogger<EventBusService> _logger;
        private readonly IIntegrationEventLogService _eventLogService;
        private readonly Assembly _assembly;

        public EventBusService(IEventBus eventBus,
            MySqlDbContext dbContext,
            ILogger<EventBusService> logger,
            Assembly assembly,
            Func<DbConnection, Assembly, IIntegrationEventLogService> eventLogFunc)
        {
            _eventBus = eventBus;
            _dbContext = dbContext;
            _logger = logger;
            _assembly = assembly;
            _eventLogService = eventLogFunc(dbContext.Database.GetDbConnection(), assembly);
        }

        public async Task AddAndSaveEventAsync(IntegrationEvent evt)
        {
            await _eventLogService.SaveAsync(evt, _dbContext.GetCurrentTransaction());
        }

        public async Task PublishEventsAsync(Guid transactionId)
        {
            var pendingLogEvents = await _eventLogService.PendingsToPublishAsync(transactionId, _assembly);

            foreach (var logEvt in pendingLogEvents)
            {
                try
                {
                    await _eventLogService.UpdateEventStateAsync(logEvt.EventId, EventStateEnum.InProgress);
                    _eventBus.Publish(logEvt.IntegrationEvent);
                    await _eventLogService.UpdateEventStateAsync(logEvt.EventId, EventStateEnum.Published);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "发布消息队列失败");

                    await _eventLogService.UpdateEventStateAsync(logEvt.EventId, EventStateEnum.Published);
                }
            }
        }
    }
}
