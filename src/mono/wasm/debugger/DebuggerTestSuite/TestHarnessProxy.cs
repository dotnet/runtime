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

        public static Task Start(string browserPath, string appPath, string pagePath, string browserParms, string url, Func<string, ILogger<TestHarnessProxy>, Task<string>> extractConnUrl)
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
                            options.BrowserPath = options.BrowserPath ?? browserPath;
                            options.BrowserParms = browserParms;
                            options.AppPath = appPath;
                            options.PagePath = pagePath;
                            options.DevToolsUrl = new Uri(url);
                            options.ExtractConnUrl = extractConnUrl;
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
