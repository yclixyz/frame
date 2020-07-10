using Gugubao.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;

namespace Gugubao.Main
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = CreateHostBuilder(args).Build();

            using (var scope = builder.Services.CreateScope())
            {
                var serviceProvider = scope.ServiceProvider;

                try
                {
                    var context = serviceProvider.GetRequiredService<MySqlDbContext>();

                    SeedData seedData = new SeedData();

                    seedData.Initialize(serviceProvider);
                }
                catch (Exception ex)
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred seeding the DB.");
                }
            }

            builder.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .UseSerilog((host, logging) =>
                {
                    logging.ReadFrom.Configuration(host.Configuration).Enrich.FromLogContext();

                    if (host.HostingEnvironment.EnvironmentName == Environments.Development)
                    {
                        logging.WriteTo.Console();
                    }
                    else
                    {
                        logging.WriteTo.File(Path.Combine("logs", "log.txt"), fileSizeLimitBytes: 1_000_000, rollOnFileSizeLimit: true, shared: true, rollingInterval: RollingInterval.Day, flushToDiskInterval: TimeSpan.FromSeconds(1));

                        //logging.WriteTo.Http("http://192.168.248.135:5000"); //容僕logstash
                        //logging.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(host.Configuration["Elasticsearch"]))// 容僕elasticsearch
                        //{
                        //    AutoRegisterTemplate = true,
                        //});
                    }
                });
    }
}
