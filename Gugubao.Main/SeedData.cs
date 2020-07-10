using Gugubao.Data;
using Gugubao.Entity;
using Gugubao.Utility;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace Gugubao.Main
{
    internal class SeedData
    {
        internal void Initialize(IServiceProvider serviceProvider)
        {
            var mySqlDbContext = serviceProvider.GetRequiredService<MySqlDbContext>();

            var users = mySqlDbContext.Set<Customer>();

            if (users.Count() == 0)
            {
                var user = new Customer
                {
                    Phone = "13344556677"
                };

                mySqlDbContext.Add(user);
                mySqlDbContext.SaveChanges();
            }
        }
    }
}