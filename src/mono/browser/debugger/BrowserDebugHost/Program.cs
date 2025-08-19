// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

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
                builder
                    .AddConsole(options => options.FormatterName = "messageOnly") // Emit messages as expected by DebugProxyLauncher.cs
                    .AddConsoleFormatter<MessageOnlyFormatter, ConsoleFormatterOptions>()
                    .AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information)
                    .AddFilter("DevToolsProxy", LogLevel.Information)
                    .AddFilter("FirefoxMonoProxy", LogLevel.Information)
                    .AddFilter(null, LogLevel.Warning);
            });

            await DebugProxyHost.RunDebugProxyAsync(options, args, loggerFactory, CancellationToken.None);
        }
    }

    public class MessageOnlyFormatter : ConsoleFormatter
    {
        public MessageOnlyFormatter() : base("messageOnly") { }

        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
            => textWriter.WriteLine(logEntry.Formatter(logEntry.State, logEntry.Exception));
    }
}
