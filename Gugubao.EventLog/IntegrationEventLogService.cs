using Gugubao.Rabbitmq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace Gugubao.IntegrationEventLog
{
    public class IntegrationEventLogService : IIntegrationEventLogService
    {
        private readonly IntegrationEventLogContext _dbContext;

        public IntegrationEventLogService(DbConnection dbConnection)
        {
            _dbContext = new IntegrationEventLogContext(new DbContextOptionsBuilder<IntegrationEventLogContext>()
                                                            .UseMySql(dbConnection).Options);
        }

        public async Task<IEnumerable<IIntegrationEventLogEntity>> PendingsToPublishAsync(Guid transactionId, Assembly assembly)
        {
            var _eventTypes = assembly.GetTypes().Where(c => !c.IsNested && c.IsClass && c.IsSubclassOf(typeof(IntegrationEvent))).ToList();

            var tid = transactionId.ToString();

            var pendings = await _dbContext.IntegrationEventLogs
                .Where(c => c.TransactionId == tid && c.State == EventStateEnum.NotPublished)
                .ToListAsync();

            if (pendings != null && pendings.Any())
            {
                return pendings.OrderBy(o => o.CreateTime)
                    .Select(e => e.DeserializeJsonContent(_eventTypes.Find(t => t.FullName == e.EventTypeName)));
            }

            return new List<IIntegrationEventLogEntity>();
        }

        public async Task SaveAsync(IntegrationEvent integrationEvent, IDbContextTransaction transaction)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            var eventLogEntry = new IIntegrationEventLogEntity(integrationEvent, transaction.TransactionId);

            _dbContext.Database.UseTransaction(transaction.GetDbTransaction());

            await _dbContext.IntegrationEventLogs.AddAsync(eventLogEntry);

            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateEventStateAsync(long eventId, EventStateEnum eventState)
        {
            var eventLogEntry = _dbContext.IntegrationEventLogs.Single(ie => ie.EventId == eventId);

            eventLogEntry.State = eventState;

            if (eventState == EventStateEnum.InProgress)
            {
                eventLogEntry.TimesSent++;
            }

            _dbContext.IntegrationEventLogs.Update(eventLogEntry);

            await _dbContext.SaveChangesAsync();
        }
    }
}
