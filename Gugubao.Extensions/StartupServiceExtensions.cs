using Gugubao.Data;
using Gugubao.Domain.Infrastructure;
using Gugubao.IntegrationEventLog;
using Gugubao.Rabbitmq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Pomelo.EntityFrameworkCore.MySql.Storage;
using System;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Gugubao.Extensions
{
    public static class StartupServiceExtensions
    {
        private static readonly ILoggerFactory DbLoggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddFilter((c, level) => c == DbLoggerCategory.Database.Command.Name && level == LogLevel.Information).AddConsole();
        });

        public static IServiceCollection AddSwaggerService(this IServiceCollection services, Assembly assembly,Assembly modelAssmebly)
        {
            // 添加Swagger Api服务
            services.AddApiVersioning();

            services.AddSwaggerGen(c =>
            {
                typeof(ApiVersions).GetEnumNames().ToList().ForEach(version =>
                {
                    c.SwaggerDoc(version,
                                 new OpenApiInfo
                                 {
                                     Title = "结算中心",
                                     Version = version,
                                     Description = $"结算中心{version}",
                                     TermsOfService = new Uri("http://www.lianghuiw.com")
                                 });
                });

                c.DocInclusionPredicate((docName, apiDesc) =>
                {
                    return apiDesc.HasVersion(docName);
                });

                c.OperationFilter<AssignOperationExtensions>();
                c.OperationFilter<RemoveVersionParameters>();
                c.DocumentFilter<SetVersionInPaths>();

                var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name }.xml");
                c.IncludeXmlComments(xmlPath);

                var xmlModelPath = Path.Combine(AppContext.BaseDirectory, $"{modelAssmebly.GetName().Name }.xml");

                c.IncludeXmlComments(xmlModelPath);

                c.EnableAnnotations();
            });

            return services;
        }

        public static IServiceCollection AddDbService<T>(this IServiceCollection services, IConfiguration configuration, Assembly assembly) where T : DbContext
        {
            //内存数据库
            //services.AddDbContext<MySqlDbContext>(options =>
            //{
            //    options.UseInMemoryDatabase("test");

            //    options.UseLoggerFactory(DbLoggerFactory);
            //});

            //MySql
            services.AddDbContextPool<T>(options =>
            {
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

                options.UseMySql(configuration["mysql:connection"], mysqlOptions =>
                {
                    mysqlOptions.MigrationsAssembly(assembly.GetName().Name);

                    mysqlOptions.EnableRetryOnFailure(
                                   maxRetryCount: 3,
                                   maxRetryDelay: TimeSpan.FromSeconds(10),
                                   errorNumbersToAdd: new int[] { 2 });
                    mysqlOptions.CommandTimeout(60);

                    mysqlOptions.CharSet(CharSet.Utf8Mb4);
                    mysqlOptions.CharSetBehavior(CharSetBehavior.AppendToAllColumns);
                });

                options.UseLoggerFactory(DbLoggerFactory);
            });

            return services;
        }

        public static IServiceCollection AddRepository(this IServiceCollection services, Assembly assembly, Type iRepository)
        {
            // 获取泛型接口是IRepository<>的子类
            var repoImpls = assembly.GetTypes()
                                    .Where(c => !c.IsNested && !c.IsInterface && c.GetTypeInfo().ImplementedInterfaces
                                           .Any(d => d.IsGenericType && d.GetGenericTypeDefinition() == iRepository))
                                    .ToList();


            repoImpls.ForEach(c =>
            {
                var ifs = c.GetInterfaces().ToList();

                var genericType = ifs.Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == iRepository);

                if (genericType)
                {
                    // Repository使用IRepository接口
                    // 其他Repository使用非IRepository接口
                    var repoIf = ifs.Count == 1 ? ifs.Select(d => d.GetGenericTypeDefinition()).First() : ifs.First(x => !x.IsGenericType);
                    services.AddScoped(repoIf, c);
                }
            });

            return services;
        }

        public static IServiceCollection AddQueries(this IServiceCollection services, Assembly assembly)
        {
            var queries = assembly.GetTypes().Where(c => c.Name.EndsWith("Query")).ToList();
            queries.ForEach(c =>
            {
                services.AddScoped(c);
            });

            return services;
        }

        public static IServiceCollection AddRabbitmq(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<RabbitmqOption>(configuration.GetSection("Rabbitmq"));

            services.AddSingleton<IEventBusManager, EventBusManager>();

            services.AddSingleton<EventBusGuxiaopu>();

            services.AddSingleton<IEventBus, EventBus>();

            services.AddSingleton<IRabbitmqManager, RabbitmqManager>();

            return services;
        }

        public static IServiceCollection AddRabbitEvent(this IServiceCollection services, Assembly assembly)
        {
            var eventHandlers = assembly.GetTypes()
                                        .Where(c => !c.IsNested && !c.IsInterface && c.GetTypeInfo().ImplementedInterfaces
                                              .Any(d => d.IsGenericType && d.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>)))
                                        .ToList();

            eventHandlers.ForEach(c =>
            {
                services.AddScoped(c.GetTypeInfo().ImplementedInterfaces.First(), c);
            });

            return services;
        }

        /// <summary>
        /// 注册EventLog相关服务
        /// </summary>
        /// <param name="services">services</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddEventLogService(this IServiceCollection services, Assembly assembly)
        {
            static IIntegrationEventLogService eventLogFunc(DbConnection x, Assembly y)
            {
                return new IntegrationEventLogService(x);
            }

            services.AddTransient(sp => (Func<DbConnection, Assembly, IIntegrationEventLogService>)eventLogFunc);

            services.AddTransient<IEventBusService, EventBusService>(sp =>
            {
                var eventBus = sp.GetRequiredService<IEventBus>();
                var dbContext = sp.GetRequiredService<MySqlDbContext>();
                var logger = sp.GetRequiredService<ILogger<EventBusService>>();

                var integrationEventLogService = new IntegrationEventLogService(dbContext.Database.GetDbConnection());

                return new EventBusService(eventBus, dbContext, logger, assembly, eventLogFunc);
            });

            return services;
        }
    }
}
