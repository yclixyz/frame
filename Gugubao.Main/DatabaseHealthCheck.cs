using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Gugubao.Main
{
    public class DatabaseHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken =
         default)
        {
            using (var connection = new SqlConnection("Server=(localdb)\\mssqllocaldb;Database=aspnet;Trusted_Connection=True;MultipleActiveResultSets=true"))
            {
                try
                {
                    connection.Open();
                }
                catch (SqlException)
                {
                    return Task.FromResult(HealthCheckResult.Unhealthy());
                }
            }

            return Task.FromResult(HealthCheckResult.Healthy());

        }
    }
}
