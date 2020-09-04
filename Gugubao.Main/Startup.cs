using AutoMapper;
using Gugubao.Data;
using Gugubao.Domain.Infrastructure;
using Gugubao.Extensions;
using Gugubao.Handler;
using Gugubao.Query;
using Gugubao.Rabbitmq;
using Gugubao.Utility;
using HealthChecks.UI.Client;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using StackExchange.Redis;
using System;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using OpenTelemetry.Trace;

namespace Gugubao.Main
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors()
                    .AddMemoryCache()
                    .AddMediatR(Assembly.GetAssembly(typeof(CreateCustomerHandler)))
                    .AddAutoMapper(Assembly.GetAssembly(typeof(CustomerMapper)))
                    .AddDbService<MySqlDbContext>(Configuration, Assembly.GetExecutingAssembly())
                    //.AddDbService<IntegrationEventLogContext>(Configuration, Assembly.GetExecutingAssembly())
                    .AddQueries(Assembly.GetAssembly(typeof(CustomerQuery)))
                    .AddSwaggerService(Assembly.GetExecutingAssembly().GetName().Name)
                    .AddRabbitmq(Configuration)
                    //.AddRabbitEvent(Assembly.GetAssembly(typeof(UpdatePhoneEvent)))
                    //.AddEventLogService(Assembly.GetAssembly(typeof(UpdatePhoneEvent)))
                    .AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehaviour<,>))
                    .AddResponseCaching();

            services.AddOpenTelemetryTracerProvider(options =>
            {
                options.AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddGrpcClientInstrumentation()
                .AddZipkinExporter(zipkin =>
                {
                    zipkin.ServiceName = "test";
                    zipkin.Endpoint = new Uri("http://ip:9411/api/v2/spans");
                });
            });

            services.AddHttpClient();

            services.AddApplicationInsightsTelemetry(Configuration);

            // services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("sql");
            // services.AddHealthChecks().AddDbContextCheck<MySqlDbContext>();

            var rabbitmqOption = new RabbitmqOption();

            Configuration.GetSection("Rabbitmq").Bind(rabbitmqOption);

            var factory = new ConnectionFactory()
            {
                HostName = rabbitmqOption.HostName,
                UserName = rabbitmqOption.UserName,
                Password = rabbitmqOption.Password,
                DispatchConsumersAsync = true
            };
            var connection = factory.CreateConnection();

            services.AddHealthChecks()
            .AddMySql(Configuration["mysql:connection"])
            .AddRedis("192.168.248.135")
            .AddRabbitMQ(sp => connection, name: "basket-rabbitmqbus-check", tags: new string[] { "rabbitmqbus" })
            .AddUrlGroup(new Uri("http://localhost:5002/identity"), name: "identityapi -check", tags: new string[] { "identityapi" });

            services.AddHealthChecksUI();

            services.AddDataProtection(opts =>
            {
                opts.ApplicationDiscriminator = "eshop.webmvc";
            })
            .PersistKeysToStackExchangeRedis(ConnectionMultiplexer.Connect("192.168.248.135"), "DataProtection-Keys");


            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddTransient<HttpClientAuthorizationDelegatingHandler>();
            services.AddTransient<HttpClientRequestIdDelegatingHandler>();

            services.AddHttpClient<IAccountService, AccountService>()
            .SetHandlerLifetime(TimeSpan.FromMinutes(5))
            .AddHttpMessageHandler<HttpClientAuthorizationDelegatingHandler>()
            .AddHttpMessageHandler<HttpClientRequestIdDelegatingHandler>();

            services.AddControllers(options =>
            {
                options.Filters.Add(typeof(HttpGlobalExceptionFilter));
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
                options.JsonSerializerOptions.Converters.Add(new DatetimeJsonConverter());
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMiddleware<AuthencateMiddleware>();

            // Cors中间件，现默认所有
            app.UseCors(c =>
            {
                c.SetIsOriginAllowed(origin => true)
                 .AllowAnyHeader()
                 .AllowAnyMethod()
                 .AllowCredentials();
            });

            app.UseRouting();

            app.UseHealthChecksUI();

            app.UseResponseCaching();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health", new HealthCheckOptions
                {
                    ResultStatusCodes =
                    {
                      [HealthStatus.Healthy] = StatusCodes.Status200OK,
                      [HealthStatus.Degraded] = StatusCodes.Status200OK,
                      [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
                    },
                    AllowCachingResponses = false,
                    Predicate = _ => true,
                    //Predicate = r => r.Name.Contains("sql"),
                    //ResponseWriter = CustomResponseWriter
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
                });

                endpoints.MapControllers();
            });

            // 配置消息队列消费者
            // app.UseRabbitmqSubscribe(Assembly.GetAssembly(typeof(UpdatePhoneEvent)));

            // 配置SwaggerApi
            app.UseConfigSwagger(env);
        }
    }
}
