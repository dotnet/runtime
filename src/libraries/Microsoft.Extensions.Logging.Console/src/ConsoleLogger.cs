// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Extensions.Logging.Console
{
    [UnsupportedOSPlatform("browser")]
    internal sealed class ConsoleLogger : ILogger
    {
        private readonly string _name;
        private readonly ConsoleLoggerProcessor _queueProcessor;

        internal ConsoleLogger(
            string name,
            ConsoleLoggerProcessor loggerProcessor,
            ConsoleFormatter formatter,
            IExternalScopeProvider? scopeProvider,
            ConsoleLoggerOptions options)
        {
            ThrowHelper.ThrowIfNull(name);

            _name = name;
            _queueProcessor = loggerProcessor;
            Formatter = formatter;
            ScopeProvider = scopeProvider;
            Options = options;
        }

        internal ConsoleFormatter Formatter { get; set; }
        internal IExternalScopeProvider? ScopeProvider { get; set; }
        internal ConsoleLoggerOptions Options { get; set; }

        [ThreadStatic]
        private static StringWriter? t_stringWriter;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            ThrowHelper.ThrowIfNull(formatter);

            t_stringWriter ??= new StringWriter();
            LogEntry<TState> logEntry = new LogEntry<TState>(logLevel, _name, eventId, state, exception, formatter);
            Formatter.Write(in logEntry, ScopeProvider, t_stringWriter);

            var sb = t_stringWriter.GetStringBuilder();
            if (sb.Length == 0)
            {
                return;
            }
            string computedAnsiString = sb.ToString();
            sb.Clear();
            if (sb.Capacity > 1024)
            {
                sb.Capacity = 1024;
            }
            _queueProcessor.EnqueueMessage(new LogMessageEntry(computedAnsiString, logAsError: logLevel >= Options.LogToStandardErrorThreshold));
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => ScopeProvider?.Push(state) ?? NullScope.Instance;
    }
}
