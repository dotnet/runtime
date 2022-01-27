// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Threading;

namespace ServerComparison.TestSites
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            //var builder = Host.CreateApplicationBuilder(args);

            //builder.Configuration.AddJsonFile("myconfig.json");
            //Console.WriteLine($"MyConfigVariable = ${builder.Configuration["MyConfigVariable"]}");

            //builder.Services.AddHostedService<MyHostedService>();

            //var minConsoleLogLevel = builder.Environment.IsDevelopment() ? LogLevel.Trace : LogLevel.Warning;
            //builder.Logging.AddFilter<ConsoleLoggerProvider>(level => level >= minConsoleLogLevel);

            //builder.Build().Run();

            //var builder = Host.CreateDefaultBuilder(args)
            //    .ConfigureAppConfiguration(hostConfig =>
            //    {
            //        hostConfig.AddJsonFile("myconfig.json");
            //    })
            //    .ConfigureServices((context, services) =>
            //    {
            //        Console.WriteLine($"MyConfigVariable = ${context.Configuration["MyConfigVariable"]}");
            //        services.AddHostedService<MyHostedService>();
            //    })
            //    .ConfigureLogging((context, logging) =>
            //    {
            //        var minConsoleLogLevel = context.HostingEnvironment.IsDevelopment() ? LogLevel.Trace : LogLevel.Warning;
            //        logging.AddFilter<ConsoleLoggerProvider>(level => level >= minConsoleLogLevel);
            //    });

            //builder.Build().Run();

            var builder = new HostBuilder()
                .ConfigureHostConfiguration(configBuilder =>
                {
                    configBuilder.AddCommandLine(args)
                        .AddEnvironmentVariables(prefix: "DOTNET_");
                })
                .ConfigureLogging((_, factory) =>
                {
                    factory.AddConsole();
                    factory.AddFilter<ConsoleLoggerProvider>(level => level >= LogLevel.Warning);
                });
            using (var host = builder.Build())
            {
                var config = host.Services.GetRequiredService<IConfiguration>();
                var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

                lifetime.ApplicationStarted.Register(() =>
                {
                    Console.WriteLine("Started");
                });
                lifetime.ApplicationStopping.Register(() =>
                {
                    Console.WriteLine("Stopping firing");
                    Console.WriteLine("Stopping end");
                });
                lifetime.ApplicationStopped.Register(() =>
                {
                    Console.WriteLine("Stopped firing");
                    Console.WriteLine("Stopped end");
                });

                if (config["STARTMECHANIC"] == "Run")
                {
                    host.Run();
                }
                else if (config["STARTMECHANIC"] == "WaitForShutdown")
                {
                    host.Start();
                    host.WaitForShutdown();
                }
                else
                {
                    throw new InvalidOperationException("Starting mechanic not specified");
                }
            }
        }

        private class MyHostedService : IHostedService
        {
            public Task StartAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
            public Task StopAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
        }
    }
}

