// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using System.IO;

class Program
{
    private static int ConstructorCallCount = 0;
    private static int WriteCallCount = 0;

    static int Main(string[] args)
    {
        IServiceCollection descriptors = new ServiceCollection();
        descriptors.AddLogging(builder =>
        {
            builder.AddConsoleFormatter<CustomFormatter, CustomOptions>();
            builder.AddConsole(o => { o.FormatterName = "custom"; });
        });

        ServiceProvider provider = descriptors.BuildServiceProvider();

        ILoggerProvider logger = provider.GetRequiredService<ILoggerProvider>();
        logger.CreateLogger("log").LogError("Hello");

        if (ConstructorCallCount != 1 ||
            WriteCallCount != 1)
        {
            return -1;
        }

        return 100;
    }

    private class CustomFormatter : ConsoleFormatter
    {
        public CustomFormatter() : base("Custom")
        {
            ConstructorCallCount++;
        }
        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter)
        {
            WriteCallCount++;
        }
    }

    private class CustomOptions : ConsoleFormatterOptions { }
}
