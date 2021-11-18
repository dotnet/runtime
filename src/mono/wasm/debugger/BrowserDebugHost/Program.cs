// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.WebAssembly.Diagnostics
{
    public class ProxyOptions
    {
        public Uri DevToolsUrl { get; set; } = new Uri("http://localhost:9222");

        public int? OwnerPid { get; set; }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
                builder.AddSimpleConsole(options => options.SingleLine = true)
                       .AddFilter(null, LogLevel.Information)
            );

            FirefoxProxyServer proxyFirefox = new FirefoxProxyServer(loggerFactory, 6000);
            proxyFirefox.Run();

            IWebHost host = new WebHostBuilder()
                .UseSetting("UseIISIntegration", false.ToString())
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddCommandLine(args);
                })
                .UseUrls("http://127.0.0.1:0")
                .Build();

            host.Run();
        }
}
}
