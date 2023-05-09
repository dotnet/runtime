// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.Logging
{
    internal sealed class DispatchProcessor : ILogEntryProcessor
    {
        private readonly LoggerInformation[] _loggers;

        public DispatchProcessor(LoggerInformation[] loggers)
        {
            _loggers = loggers;
        }

        public LogEntryHandler<TState, TEnrichmentProperties> GetLogEntryHandler<TState, TEnrichmentProperties>(ILogMetadata<TState>? metadata, out bool enabled, out bool dynamicCheckRequired)
        {
            if (metadata != null)
            {
                LoggerInformation[] filteredLoggers = _loggers.Where(l => l.IsEnabled(metadata.LogLevel)).ToArray();
                if (filteredLoggers.Length == 0)
                {
                    enabled = false;
                    dynamicCheckRequired = false;
                    return NullHandler<TState, TEnrichmentProperties>.Instance;
                }
                else if (filteredLoggers.Length == 1)
                {
                    LoggerInformation loggerInfo = filteredLoggers[0];
                    LogEntryHandler<TState, TEnrichmentProperties>? handler = null;
                    if (loggerInfo.Processor != null)
                    {
                        handler = loggerInfo.Processor.GetLogEntryHandler<TState, TEnrichmentProperties>(metadata, out enabled, out dynamicCheckRequired);
                        if (handler != null)
                        {
                            return new DispatchViaHandler<TState, TEnrichmentProperties>(handler);
                        }
                    }
                }
                //TODO: add a more general purpose case that dispatches to N handlers and M loggers
            }

            enabled = true;
            dynamicCheckRequired = true;
            return new DynamicDispatchToLoggers<TState, TEnrichmentProperties>(this, metadata?.GetStringMessageFormatter());
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            // We could potentially pre-process loggers to remove those with
            // MinLevel = None at the cost of another array for every category
            LoggerInformation[]? loggers = _loggers;
            if (loggers == null)
            {
                return false;
            }

            List<Exception>? exceptions = null;
            int i = 0;
            for (; i < loggers.Length; i++)
            {
                ref readonly LoggerInformation loggerInfo = ref loggers[i];
                if (!loggerInfo.IsEnabled(logLevel))
                {
                    continue;
                }

                if (LoggerIsEnabled(logLevel, loggerInfo.Logger, ref exceptions))
                {
                    break;
                }
            }

            if (exceptions != null && exceptions.Count > 0)
            {
                Logger.ThrowLoggingError(exceptions);
            }

            return i < loggers.Length ? true : false;

            static bool LoggerIsEnabled(LogLevel logLevel, ILogger logger, ref List<Exception>? exceptions)
            {
                try
                {
                    if (logger.IsEnabled(logLevel))
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    exceptions ??= new List<Exception>();
                    exceptions.Add(ex);
                }

                return false;
            }
        }

        private sealed class NullHandler<TState, TEnrichmentProperties> : LogEntryHandler<TState, TEnrichmentProperties>
        {
            public static NullHandler<TState, TEnrichmentProperties> Instance = new NullHandler<TState, TEnrichmentProperties>();
            public override void HandleLogEntry(ref LogEntry<TState, TEnrichmentProperties> logEntry)
            {
            }
            public override bool IsEnabled(LogLevel level) => false;
        }

        private sealed class DispatchViaHandler<TState, TEnrichmentProperties> : LogEntryHandler<TState, TEnrichmentProperties>
        {
            private LogEntryHandler<TState, TEnrichmentProperties> _nestedHandler;

            public DispatchViaHandler(LogEntryHandler<TState, TEnrichmentProperties> handler)
            {
                _nestedHandler = handler;
            }

            public override void HandleLogEntry(ref LogEntry<TState, TEnrichmentProperties> logEntry)
            {
                try
                {
                    _nestedHandler.HandleLogEntry(ref logEntry);
                }
                catch (Exception ex)
                {
                    Logger.ThrowLoggingError(new List<Exception>(new Exception[] { ex }));
                }
            }

            public override bool IsEnabled(LogLevel level) => _nestedHandler.IsEnabled(level);
        }

        private sealed class DynamicDispatchToLoggers<TState, TEnrichmentProperties> : LogEntryHandler<TState, TEnrichmentProperties>
        {
            private DispatchProcessor _processor;
            private Func<TState, Exception?, string>? _formatter;

            public DynamicDispatchToLoggers(DispatchProcessor processor, Func<TState, Exception?, string>? formatter)
            {
                _processor = processor;
                _formatter = formatter;
            }
            public override void HandleLogEntry(ref LogEntry<TState, TEnrichmentProperties> logEntry)
            {
                Func<TState, Exception?, string>? formatter = logEntry.Formatter ?? _formatter;
                formatter ??= (TState s, Exception? _) => s == null ? "" : s.ToString() ?? "";
                LoggerInformation[] loggers = _processor._loggers!;
                List<Exception>? exceptions = null;
                for (int i = 0; i < loggers.Length; i++)
                {
                    ref readonly LoggerInformation loggerInfo = ref loggers[i];
                    if (!loggerInfo.IsEnabled(logEntry.LogLevel))
                    {
                        continue;
                    }

                    try
                    {
                        loggerInfo.Logger.Log(logEntry.LogLevel, logEntry.EventId, logEntry.State, logEntry.Exception, formatter);
                    }
                    catch (Exception ex)
                    {
                        exceptions ??= new List<Exception>();
                        exceptions.Add(ex);
                    }
                }

                if (exceptions != null && exceptions.Count > 0)
                {
                    Logger.ThrowLoggingError(exceptions);
                }
            }

            public override bool IsEnabled(LogLevel level) => _processor.IsEnabled(level);
        }
    }
}
