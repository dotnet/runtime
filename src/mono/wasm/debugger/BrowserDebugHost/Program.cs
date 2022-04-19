// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

#nullable enable

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
                builder.AddSimpleConsole(options =>
                        {
                            options.SingleLine = true;
                            options.TimestampFormat = "[HH:mm:ss] ";
                        })
                       .AddFilter(null, LogLevel.Information)
            );

            IConfigurationRoot config = new ConfigurationBuilder().AddCommandLine(args).Build();
            int proxyPort = 0;
            if (config["proxy-port"] is not null && int.TryParse(config["proxy-port"], out int port))
                proxyPort = port;
            int firefoxDebugPort = 6000;
            if (config["firefox-debug-port"] is not null && int.TryParse(config["firefox-debug-port"], out int ffport))
                firefoxDebugPort = ffport;

            ILogger logger = loggerFactory.CreateLogger("FirefoxMonoProxy");
            _ = FirefoxProxyServer.Run(browserPort: firefoxDebugPort, proxyPort: proxyPort, loggerFactory, logger);

            IWebHost host = new WebHostBuilder()
                .UseSetting("UseIISIntegration", false.ToString())
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddCommandLine(args);
                })
                .UseUrls($"http://127.0.0.1:{proxyPort+1}")
                .Build();

            host.Run();
        }
    }
}
