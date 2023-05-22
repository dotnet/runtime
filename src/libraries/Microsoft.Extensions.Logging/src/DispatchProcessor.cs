// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Extensions.Logging
{
    internal sealed class DispatchProcessor : ILogEntryProcessor
    {
        private readonly LoggerInformation[] _loggers;
        private readonly IExternalScopeProvider? _externalScopeProvider;

        public DispatchProcessor(LoggerInformation[] loggers, IExternalScopeProvider? externalScopeProvider)
        {
            _loggers = loggers;
            _externalScopeProvider = externalScopeProvider;
        }

        public LogEntryHandler<TState> GetLogEntryHandler<TState>(ILogMetadata<TState>? metadata, out bool enabled, out bool dynamicCheckRequired)
        {
            if (metadata != null)
            {
                LoggerInformation[] filteredLoggers = _loggers.Where(l => l.IsEnabled(metadata.LogLevel)).ToArray();
                if (filteredLoggers.Length == 0)
                {
                    enabled = false;
                    dynamicCheckRequired = false;
                    return NullHandler<TState>.Instance;
                }
                else if (filteredLoggers.Length == 1)
                {
                    LoggerInformation loggerInfo = filteredLoggers[0];
                    LogEntryHandler<TState>? handler = null;
                    if (loggerInfo.Processor != null)
                    {
                        handler = loggerInfo.Processor.GetLogEntryHandler<TState>(metadata, out enabled, out dynamicCheckRequired);
                        if (handler != null)
                        {
                            return new DispatchViaHandler<TState>(handler);
                        }
                    }
                }
                //TODO: add a more general purpose case that dispatches to N handlers and M loggers
            }

            enabled = true;
            dynamicCheckRequired = true;
            return new DynamicDispatchToLoggers<TState>(this, metadata?.GetStringMessageFormatter());
        }

        public ScopeHandler<TState> GetScopeHandler<TState>(ILogMetadata<TState>? metadata, out bool enabled, out bool dynamicCheckRequired) where TState : notnull
        {
            enabled = true;
            dynamicCheckRequired = false;
            return new DynamicDispatchScopeToLoggers<TState>(this);
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

        private sealed class NullHandler<TState> : LogEntryHandler<TState>
        {
            public static readonly NullHandler<TState> Instance = new NullHandler<TState>();
            public override void HandleLogEntry(ref LogEntry<TState> logEntry)
            {
            }
            public override bool IsEnabled(LogLevel level) => false;
        }

        private sealed class NullScopeHandler<TState> : ScopeHandler<TState> where TState : notnull
        {
            public static readonly NullScopeHandler<TState> Instance = new NullScopeHandler<TState>();
            public override IDisposable? HandleBeginScope(ref TState state) => null;
            public override bool IsEnabled(LogLevel level) => false;
        }

        private sealed class DispatchViaHandler<TState> : LogEntryHandler<TState>
        {
            private LogEntryHandler<TState> _nestedHandler;

            public DispatchViaHandler(LogEntryHandler<TState> handler)
            {
                _nestedHandler = handler;
            }

            public override void HandleLogEntry(ref LogEntry<TState> logEntry)
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

        private sealed class DispatchViaScopeHandler<TState> : ScopeHandler<TState> where TState : notnull
        {
            private ScopeHandler<TState> _nestedHandler;

            public DispatchViaScopeHandler(ScopeHandler<TState> handler)
            {
                _nestedHandler = handler;
            }

            public override IDisposable? HandleBeginScope(ref TState state)
            {
                try
                {
                    return _nestedHandler.HandleBeginScope(ref state);
                }
                catch (Exception ex)
                {
                    Logger.ThrowLoggingError(new List<Exception>(new Exception[] { ex }));
                    return null;
                }
            }

            public override bool IsEnabled(LogLevel level) => _nestedHandler.IsEnabled(level);
        }

        private sealed class DynamicDispatchToLoggers<TState> : LogEntryHandler<TState>
        {
            private DispatchProcessor _processor;
            private Func<TState, Exception?, string>? _formatter;

            public DynamicDispatchToLoggers(DispatchProcessor processor, Func<TState, Exception?, string>? formatter)
            {
                _processor = processor;
                _formatter = formatter;
            }

            public override void HandleLogEntry(ref LogEntry<TState> logEntry)
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

        private sealed class DynamicDispatchScopeToLoggers<TState> : ScopeHandler<TState> where TState : notnull
        {
            private readonly ScopeLogger[] _scopeLoggers;
            private readonly DispatchProcessor _processor;

            public DynamicDispatchScopeToLoggers(DispatchProcessor processor)
            {
                List<ScopeLogger> scopeLoggers = new List<ScopeLogger>();

                foreach (LoggerInformation loggerInformation in processor._loggers)
                {
                    if (!loggerInformation.ExternalScope && loggerInformation.IsEnabled(LogLevel.Critical))
                    {
                        scopeLoggers.Add(new ScopeLogger(logger: loggerInformation.Logger, externalScopeProvider: null));
                    }
                }
                if (processor._externalScopeProvider is { } scopeProvider)
                {
                    scopeLoggers.Add(new ScopeLogger(logger: null, externalScopeProvider: scopeProvider));
                }

                _scopeLoggers = scopeLoggers.ToArray();
                _processor = processor;
            }

            public override IDisposable? HandleBeginScope(ref TState state)
            {
                ScopeLogger[] loggers = _scopeLoggers;

                if (loggers.Length == 0)
                {
                    return null;
                }
                if (loggers.Length == 1)
                {
                    return loggers[0].CreateScope(state);
                }

                var scope = new Scope(loggers.Length);
                List<Exception>? exceptions = null;
                for (int i = 0; i < loggers.Length; i++)
                {
                    ref readonly ScopeLogger loggerInfo = ref loggers[i];

                    try
                    {
                        scope.SetDisposable(i, loggerInfo.CreateScope(state));
                    }
                    catch (Exception ex)
                    {
                        exceptions ??= new List<Exception>();
                        exceptions.Add(ex);
                    }
                }

                if (exceptions != null && exceptions.Count > 0)
                {
                    ThrowLoggingError(exceptions);
                }

                return scope;
            }

            private static void ThrowLoggingError(List<Exception> exceptions)
            {
                throw new AggregateException(
                    message: "An error occurred while writing to logger(s).", innerExceptions: exceptions);
            }



            //Processors should also handle scopes
            //ScopeLogger[]? loggers = ScopeLoggers;
            //ScopeLogger[]? loggers = null;

            //if (loggers == null)
            //{
            //    return NullScope.Instance;
            //}

            //if (loggers.Length == 1)
            //{
            //    return loggers[0].CreateScope(state);
            //}

            //var scope = new Scope(loggers.Length);
            //List<Exception>? exceptions = null;
            //for (int i = 0; i < loggers.Length; i++)
            //{
            //    ref readonly ScopeLogger scopeLogger = ref loggers[i];

            //    try
            //    {
            //        scope.SetDisposable(i, scopeLogger.CreateScope(state));
            //    }
            //    catch (Exception ex)
            //    {
            //        exceptions ??= new List<Exception>();
            //        exceptions.Add(ex);
            //    }
            //}

            //if (exceptions != null && exceptions.Count > 0)
            //{
            //    ThrowLoggingError(exceptions);
            //}

            //return scope;

            public override bool IsEnabled(LogLevel level) => _processor.IsEnabled(level);
        }
    }
}
