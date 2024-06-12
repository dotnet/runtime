// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Extensions.Logging
{
    [DebuggerDisplay("{DebuggerToString(),nq}")]
    [DebuggerTypeProxy(typeof(LoggerDebugView))]
    internal sealed class Logger : ILogger
    {
        private readonly string _categoryName;

        public Logger(string categoryName, LoggerInformation[] loggers)
        {
            _categoryName = categoryName;
            Loggers = loggers;
        }

        public LoggerInformation[] Loggers { get; set; }
        public MessageLogger[]? MessageLoggers { get; set; }
        public ScopeLogger[]? ScopeLoggers { get; set; }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            MessageLogger[]? loggers = MessageLoggers;
            if (loggers == null)
            {
                return;
            }

            List<Exception>? exceptions = null;
            for (int i = 0; i < loggers.Length; i++)
            {
                ref readonly MessageLogger loggerInfo = ref loggers[i];
                if (!loggerInfo.IsEnabled(logLevel))
                {
                    continue;
                }

                LoggerLog(logLevel, eventId, loggerInfo.Logger, exception, formatter, ref exceptions, state);
            }

            if (exceptions != null && exceptions.Count > 0)
            {
                ThrowLoggingError(exceptions);
            }

            static void LoggerLog(LogLevel logLevel, EventId eventId, ILogger logger, Exception? exception, Func<TState, Exception?, string> formatter, ref List<Exception>? exceptions, in TState state)
            {
                try
                {
                    logger.Log(logLevel, eventId, state, exception, formatter);
                }
                catch (Exception ex)
                {
                    exceptions ??= new List<Exception>();
                    exceptions.Add(ex);
                }
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            MessageLogger[]? loggers = MessageLoggers;
            if (loggers == null)
            {
                return false;
            }

            List<Exception>? exceptions = null;
            int i = 0;
            for (; i < loggers.Length; i++)
            {
                ref readonly MessageLogger loggerInfo = ref loggers[i];
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
                ThrowLoggingError(exceptions);
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

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            ScopeLogger[]? loggers = ScopeLoggers;

            if (loggers == null)
            {
                return NullScope.Instance;
            }

            if (loggers.Length == 1)
            {
                return loggers[0].CreateScope(state);
            }

            var scope = new Scope(loggers.Length);
            List<Exception>? exceptions = null;
            for (int i = 0; i < loggers.Length; i++)
            {
                ref readonly ScopeLogger scopeLogger = ref loggers[i];

                try
                {
                    scope.SetDisposable(i, scopeLogger.CreateScope(state));
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

        internal string DebuggerToString()
        {
            return DebuggerDisplayFormatting.DebuggerToString(_categoryName, this);
        }

        private sealed class LoggerDebugView(Logger logger)
        {
            public string Name => logger._categoryName;

            // The list of providers includes the full list of configured providers from the logger factory.
            // It then mentions the min level and enable status of each provider for this logger.
            public List<LoggerProviderDebugView> Providers
            {
                get
                {
                    List<LoggerProviderDebugView> providers = new List<LoggerProviderDebugView>();
                    for (int i = 0; i < logger.Loggers.Length; i++)
                    {
                        LoggerInformation loggerInfo = logger.Loggers[i];
                        string providerName = ProviderAliasUtilities.GetAlias(loggerInfo.ProviderType) ?? loggerInfo.ProviderType.Name;
                        MessageLogger? messageLogger = FirstOrNull(logger.MessageLoggers, loggerInfo.Logger);

                        providers.Add(new LoggerProviderDebugView(providerName, messageLogger));
                    }

                    return providers;

                    // Find message logger or return null if there is no match. FirstOrDefault isn't used because MessageLogger is a struct.
                    static MessageLogger? FirstOrNull(MessageLogger[]? messageLoggers, ILogger logger)
                    {
                        if (messageLoggers is null || messageLoggers.Length == 0)
                        {
                            return null;
                        }

                        foreach (MessageLogger item in messageLoggers)
                        {
                            if (item.Logger == logger)
                            {
                                return item;
                            }
                        }

                        return null;
                    }
                }
            }

            public List<object?>? Scopes
            {
                get
                {
                    var scopeProvider = logger.ScopeLoggers?.FirstOrDefault().ExternalScopeProvider;
                    if (scopeProvider == null)
                    {
                        return null;
                    }

                    List<object?> scopes = new List<object?>();
                    scopeProvider.ForEachScope((scope, scopes) => scopes!.Add(scope), scopes);
                    return scopes;
                }
            }
            public LogLevel? MinLevel => DebuggerDisplayFormatting.CalculateEnabledLogLevel(logger);
            public bool Enabled => DebuggerDisplayFormatting.CalculateEnabledLogLevel(logger) != null;
        }

        [DebuggerDisplay("{DebuggerToString(),nq}")]
        private sealed class LoggerProviderDebugView(string providerName, MessageLogger? messageLogger)
        {
            public string Name => providerName;
            public LogLevel LogLevel => CalculateEnabledLogLevel(messageLogger) ?? LogLevel.None;

            private static LogLevel? CalculateEnabledLogLevel(MessageLogger? logger)
            {
                if (logger == null)
                {
                    return null;
                }

                ReadOnlySpan<LogLevel> logLevels = stackalloc LogLevel[]
                {
                    LogLevel.Critical,
                    LogLevel.Error,
                    LogLevel.Warning,
                    LogLevel.Information,
                    LogLevel.Debug,
                    LogLevel.Trace,
                };

                LogLevel? minimumLevel = null;

                // Check log level from highest to lowest. Report the lowest log level.
                foreach (LogLevel logLevel in logLevels)
                {
                    if (!logger.Value.IsEnabled(logLevel))
                    {
                        break;
                    }

                    minimumLevel = logLevel;
                }

                return minimumLevel;
            }

            private string DebuggerToString()
            {
                return $@"Name = ""{providerName}"", LogLevel = {LogLevel}";
            }
        }

        private sealed class Scope : IDisposable
        {
            private bool _isDisposed;

            private IDisposable? _disposable0;
            private IDisposable? _disposable1;
            private readonly IDisposable?[]? _disposable;

            public Scope(int count)
            {
                if (count > 2)
                {
                    _disposable = new IDisposable[count - 2];
                }
            }

            public void SetDisposable(int index, IDisposable? disposable)
            {
                switch (index)
                {
                    case 0:
                        _disposable0 = disposable;
                        break;
                    case 1:
                        _disposable1 = disposable;
                        break;
                    default:
                        _disposable![index - 2] = disposable;
                        break;
                }
            }

            public void Dispose()
            {
                if (!_isDisposed)
                {
                    _disposable0?.Dispose();
                    _disposable1?.Dispose();

                    if (_disposable != null)
                    {
                        int count = _disposable.Length;
                        for (int index = 0; index != count; ++index)
                        {
                            _disposable[index]?.Dispose();
                        }
                    }

                    _isDisposed = true;
                }
            }
        }
    }
}
