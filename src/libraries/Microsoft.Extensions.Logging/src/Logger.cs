// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions.Internal;

namespace Microsoft.Extensions.Logging
{
    internal class Logger : ILogger
    {
        private readonly LoggerFactory _loggerFactory;

        private LoggerInformation[] _loggers;

        private int _scopeCount;

        public Logger(LoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public LoggerInformation[] Loggers
        {
            get { return _loggers; }
            set
            {
                var scopeSize = 0;
                foreach (var loggerInformation in value)
                {
                    if (loggerInformation.CreateScopes)
                    {
                        scopeSize++;
                    }
                }
                _scopeCount = scopeSize;
                _loggers = value;
            }
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var loggers = Loggers;
            if (loggers == null)
            {
                return;
            }

            List<Exception> exceptions = null;
            foreach (var loggerInfo in loggers)
            {
                if (!loggerInfo.IsEnabled(logLevel))
                {
                    continue;
                }

                try
                {
                    loggerInfo.Logger.Log(logLevel, eventId, state, exception, formatter);
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
                throw new AggregateException(
                    message: "An error occurred while writing to logger(s).", innerExceptions: exceptions);
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            var loggers = Loggers;
            if (loggers == null)
            {
                return false;
            }

            List<Exception> exceptions = null;
            foreach (var loggerInfo in loggers)
            {
                if (!loggerInfo.IsEnabled(logLevel))
                {
                    continue;
                }

                try
                {
                    if (loggerInfo.Logger.IsEnabled(logLevel))
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
            }

            if (exceptions != null && exceptions.Count > 0)
            {
                throw new AggregateException(
                    message: "An error occurred while writing to logger(s).",
                    innerExceptions: exceptions);
            }

            return false;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            var loggers = Loggers;

            if (loggers == null)
            {
                return NullScope.Instance;
            }

            var scopeProvider = _loggerFactory.ScopeProvider;
            var scopeCount = _scopeCount;

            if (scopeProvider != null)
            {
                // if external scope is used for all providers
                // we can return it's IDisposable directly
                // without wrapping and saving on allocation
                if (scopeCount == 0)
                {
                    return scopeProvider.Push(state);
                }
                else
                {
                    scopeCount++;
                }

            }

            var scope = new Scope(scopeCount);
            List<Exception> exceptions = null;
            for (var index = 0; index < loggers.Length; index++)
            {
                var loggerInformation = loggers[index];
                if (!loggerInformation.CreateScopes)
                {
                    continue;
                }

                try
                {
                    scopeCount--;
                    // _loggers and _scopeCount are not updated atomically
                    // there might be a situation when count was updated with
                    // lower value then we have loggers
                    // This is small race that happens only on configuraiton reload
                    // and we are protecting from it by checkig that there is enough space
                    // in Scope
                    if (scopeCount >= 0)
                    {
                        var disposable = loggerInformation.Logger.BeginScope(state);
                        scope.SetDisposable(scopeCount, disposable);
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
            }

            if (scopeProvider != null)
            {
                scope.SetDisposable(0, scopeProvider.Push(state));
            }

            if (exceptions != null && exceptions.Count > 0)
            {
                throw new AggregateException(
                    message: "An error occurred while writing to logger(s).", innerExceptions: exceptions);
            }

            return scope;
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