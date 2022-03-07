// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.WebAssembly.Diagnostics
{
    public class TestHarnessProxy
    {
        static IWebHost host;
        static Task hostTask;
        static CancellationTokenSource cts = new CancellationTokenSource();
        static object proxyLock = new object();

        public static readonly Uri Endpoint = new Uri("http://localhost:9400");

        public static Task Start(string chromePath, string appPath, string pagePath)
        {
            lock (proxyLock)
            {
                if (host != null)
                    return hostTask;

                host = WebHost.CreateDefaultBuilder()
                    .UseSetting("UseIISIntegration", false.ToString())
                    .ConfigureAppConfiguration((hostingContext, config) =>
                    {
                        config.AddEnvironmentVariables(prefix: "WASM_TESTS_");
                    })
                    .ConfigureLogging(logging =>
                    {
                        logging.AddSimpleConsole(options =>
                                {
                                    options.SingleLine = true;
                                    options.TimestampFormat = "[HH:mm:ss] ";
                                })
                               .AddFilter(null, LogLevel.Information);
                    })
                    .ConfigureServices((ctx, services) =>
                    {
                        services.Configure<TestHarnessOptions>(ctx.Configuration);
                        services.Configure<TestHarnessOptions>(options =>
                        {
                            options.ChromePath = options.ChromePath ?? chromePath;
                            options.AppPath = appPath;
                            options.PagePath = pagePath;
                            options.DevToolsUrl = new Uri("http://localhost:0");
                        });
                    })
                    .UseStartup<TestHarnessStartup>()
                    .UseUrls(Endpoint.ToString())
                    .Build();
                hostTask = host.StartAsync(cts.Token);
            }

            Console.WriteLine("WebServer Ready!");
            return hostTask;
        }
    }
}
