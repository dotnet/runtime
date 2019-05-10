// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Logging
{
    internal class Logger : ILogger
    {
        public LoggerInformation[] Loggers { get; set; }
        public MessageLogger[] MessageLoggers { get; set; }
        public ScopeLogger[] ScopeLoggers { get; set; }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var loggers = MessageLoggers;
            if (loggers == null)
            {
                return;
            }

            List<Exception> exceptions = null;
            for (var i = 0; i < loggers.Length; i++)
            {
                ref readonly var loggerInfo = ref loggers[i];
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

            static void LoggerLog(LogLevel logLevel, EventId eventId, ILogger logger, Exception exception, Func<TState, Exception, string> formatter, ref List<Exception> exceptions, in TState state)
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
            var loggers = MessageLoggers;
            if (loggers == null)
            {
                return false;
            }

            List<Exception> exceptions = null;
            var i = 0;
            for (; i < loggers.Length; i++)
            {
                ref readonly var loggerInfo = ref loggers[i];
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

            static bool LoggerIsEnabled(LogLevel logLevel, ILogger logger, ref List<Exception> exceptions)
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
            var loggers = ScopeLoggers;

            if (loggers == null)
            {
                return NullScope.Instance;
            }

            if (loggers.Length == 1)
            {
                return loggers[0].CreateScope(state);
            }

            var scope = new Scope(loggers.Length);
            List<Exception> exceptions = null;
            for (var i = 0; i < loggers.Length; i++)
            {
                ref readonly var scopeLogger = ref loggers[i];

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

        private class Scope : IDisposable
        {
            private bool _isDisposed;

            private IDisposable _disposable0;
            private IDisposable _disposable1;
            private readonly IDisposable[] _disposable;

            public Scope(int count)
            {
                if (count > 2)
                {
                    _disposable = new IDisposable[count - 2];
                }
            }

            public void SetDisposable(int index, IDisposable disposable)
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
                        _disposable[index - 2] = disposable;
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
                        var count = _disposable.Length;
                        for (var index = 0; index != count; ++index)
                        {
                            if (_disposable[index] != null)
                            {
                                _disposable[index].Dispose();
                            }
                        }
                    }

                    _isDisposed = true;
                }
            }
        }
    }
}
