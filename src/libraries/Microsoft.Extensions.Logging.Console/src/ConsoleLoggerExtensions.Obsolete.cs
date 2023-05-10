// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.Console;

namespace Microsoft.Extensions.Logging
{
    public static partial class ConsoleLoggerExtensions
    {
        [Obsolete("This method is retained only for compatibility. The recommended alternative is AddConsole(this ILoggingBuilder builder).")]
        public static Logging.ILoggerFactory AddConsole(this Logging.ILoggerFactory factory, Extensions.Configuration.IConfiguration configuration)
        {
            var settings = new ConfigurationConsoleLoggerSettings(configuration);
            return factory.AddConsole(settings);
        }

        [Obsolete("This method is retained only for compatibility. The recommended alternative is AddConsole(this ILoggingBuilder builder).")]
        public static Logging.ILoggerFactory AddConsole(this Logging.ILoggerFactory factory, Console.IConsoleLoggerSettings settings)
        {
            factory.AddProvider(new ConsoleLoggerProvider(settings));
            return factory;
        }

        [Obsolete("This method is retained only for compatibility. The recommended alternative is AddConsole(this ILoggingBuilder builder).")]
        public static Logging.ILoggerFactory AddConsole(this Logging.ILoggerFactory factory, Logging.LogLevel minLevel, bool includeScopes)
        {
            factory.AddConsole((n, l) => l >= LogLevel.Information, includeScopes);
            return factory;
        }

        [Obsolete("This method is retained only for compatibility. The recommended alternative is AddConsole(this ILoggingBuilder builder).")]
        public static Logging.ILoggerFactory AddConsole(this Logging.ILoggerFactory factory, Logging.LogLevel minLevel)
        {
            factory.AddConsole(minLevel, includeScopes: false);
            return factory;
        }

        [Obsolete("This method is retained only for compatibility. The recommended alternative is AddConsole(this ILoggingBuilder builder).")]
        public static Logging.ILoggerFactory AddConsole(this Logging.ILoggerFactory factory, bool includeScopes)
        {
            factory.AddConsole((n, l) => l >= LogLevel.Information, includeScopes);
            return factory;
        }

        [Obsolete("This method is retained only for compatibility. The recommended alternative is AddConsole(this ILoggingBuilder builder).")]
        public static Logging.ILoggerFactory AddConsole(this Logging.ILoggerFactory factory, System.Func<string, Logging.LogLevel, bool> filter, bool includeScopes)
        {
            factory.AddProvider(new ConsoleLoggerProvider(filter, includeScopes));
            return factory;
        }

        [Obsolete("This method is retained only for compatibility. The recommended alternative is AddConsole(this ILoggingBuilder builder).")]
        public static Logging.ILoggerFactory AddConsole(this Logging.ILoggerFactory factory, System.Func<string, Logging.LogLevel, bool> filter)
        {
            factory.AddConsole(filter, includeScopes: false);
            return factory;
        }

        [Obsolete("This method is retained only for compatibility. The recommended alternative is AddConsole(this ILoggingBuilder builder).")]
        public static Logging.ILoggerFactory AddConsole(this Logging.ILoggerFactory factory)
        {
            return factory.AddConsole(includeScopes: false);
        }
    }
}
