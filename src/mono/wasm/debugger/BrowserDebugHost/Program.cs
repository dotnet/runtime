// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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
            IConfigurationRoot config = new ConfigurationBuilder().AddCommandLine(args).Build();
            int proxyPort = 0;
            if (config["proxy-port"] is not null && int.TryParse(config["proxy-port"], out int port))
                proxyPort = port;
            int firefoxDebugPort = 6000;
            if (config["firefox-debug-port"] is not null && int.TryParse(config["firefox-debug-port"], out int ffport))
                firefoxDebugPort = ffport;
            string? logPath = config["log-path"];


            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSimpleConsole(options =>
                        {
                            options.TimestampFormat = "[HH:mm:ss] ";
                        })
                       .AddFilter(null, LogLevel.Debug);

                if (!string.IsNullOrEmpty(logPath))
                    builder.AddFile(Path.Combine(logPath, "proxy.log"),
                                minimumLevel: LogLevel.Trace,
                                outputTemplate: "{Timestamp:o} [{Level:u3}] {SourceContext}: {Message}{NewLine}{Exception}");
            });

            ILogger logger = loggerFactory.CreateLogger("FirefoxMonoProxy");
            _ = FirefoxDebuggerProxy.Run(browserPort: firefoxDebugPort, proxyPort: proxyPort, loggerFactory, logger);

            IWebHost host = new WebHostBuilder()
                .UseSetting("UseIISIntegration", false.ToString())
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddCommandLine(args);
                })
                .UseUrls($"http://127.0.0.1:{proxyPort}")
                .Build();

            host.Run();
        }
    }
}
