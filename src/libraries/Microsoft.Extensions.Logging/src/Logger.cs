// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Logging
{
    internal sealed class Logger : ILogger
    {
        public Logger(LoggerInformation[] loggers) => Loggers = loggers;

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
                    if (exceptions == null)
                    {
                        exceptions = new List<Exception>();
                    }

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
                    if (exceptions == null)
                    {
                        exceptions = new List<Exception>();
                    }

                    exceptions.Add(ex);
                }

                return false;
            }
        }

        public IDisposable BeginScope<TState>(TState state)
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
                    if (exceptions == null)
                    {
                        exceptions = new List<Exception>();
                    }

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
