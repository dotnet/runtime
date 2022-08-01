// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

#nullable enable

namespace Microsoft.WebAssembly.Diagnostics
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            IConfigurationRoot config = new ConfigurationBuilder().AddCommandLine(args).Build();
            ProxyOptions options = new();
            config.Bind(options);
            options.RunningForBlazor = true;

            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSimpleConsole(options =>
                        {
                            options.TimestampFormat = "[HH:mm:ss] ";
                        })
                        .AddFilter("DevToolsProxy", LogLevel.Information)
                        .AddFilter("FirefoxMonoProxy", LogLevel.Information)
                        .AddFilter(null, LogLevel.Warning);
            });

            await DebugProxyHost.RunDebugProxyAsync(options, args, loggerFactory, CancellationToken.None);
        }
    }
}
