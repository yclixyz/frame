using Gugubao.Rabbitmq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Gugubao.Extensions
{
    public static class ConfigureExtensions
    {
        /// <summary>
        /// rabbitmq消费者注册
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="app">IApplicationBuilder</param>
        /// <param name="iIntegrationEventHandler"></param>
        public static void UseRabbitmqSubscribe(this IApplicationBuilder app, Assembly assembly)
        {
            // 获取IEventBus实例
            var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();

            // eventBus.Subscribe方法
            MethodInfo method = eventBus.GetType().GetMethod("Subscribe", BindingFlags.Instance | BindingFlags.Public);

            // 获取所有集成IIntegrationEventHandler的子类
            var eventHandlers = assembly
                .GetTypes()
                .Where(c => c.GetTypeInfo().ImplementedInterfaces
                                           .Any(d => d.IsGenericType &&
                                                     d.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>)))
                .ToList();

            foreach (var eventHandler in eventHandlers)
            {
                // 获取IIntegrationEventHandler子类方法的参数类型
                var eventType = eventHandler.GetMethod("Handle").GetParameters().First().ParameterType;

                // 构造IIntegrationEventHandler<T>
                var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);

                // 构造Subscribe<T,IIntegrationEventHandler<T>>
                var handlerMethod = method.MakeGenericMethod(eventType, concreteType);

                // 执行eventBus.Subscribe<T,H>()
                handlerMethod.Invoke(eventBus, null);
            }
        }

        public static void UseConfigSwagger(this IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseSwagger(c =>
                {
                    c.PreSerializeFilters.Add((swagger, httpReq) =>
                    {
                        swagger.Servers = new List<OpenApiServer> { new OpenApiServer { Url = $"{httpReq.Scheme}://{httpReq.Host.Value}" } };
                    });
                });

                app.UseSwaggerUI(c =>
                {
                    typeof(ApiVersions).GetEnumNames().OrderBy(e => e).ToList().ForEach(version =>
                    {
                        c.SwaggerEndpoint($"/swagger/{version}/swagger.json", $"结算中心 {version}版本");
                        c.RoutePrefix = string.Empty;
                    });

                    c.DisplayOperationId();
                    c.DisplayRequestDuration();
                    c.DocExpansion(DocExpansion.None);
                    c.DefaultModelsExpandDepth(-1);
                });
            }
        }
    }
}
