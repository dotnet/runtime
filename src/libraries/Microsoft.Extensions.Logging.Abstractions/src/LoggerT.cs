// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Internal;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Delegates to a new <see cref="ILogger"/> instance using the full name of the given type, created by the
    /// provided <see cref="ILoggerFactory"/>.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    public class Logger<T> : ILogger<T>, ILogEntryProcessorFactory
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a new <see cref="Logger{T}"/>.
        /// </summary>
        /// <param name="factory">The factory.</param>
        public Logger(ILoggerFactory factory)
        {
            ThrowHelper.ThrowIfNull(factory);

            _logger = factory.CreateLogger(TypeNameHelper.GetTypeDisplayName(typeof(T), includeGenericParameters: false, nestedTypeDelimiter: '.'));
        }

        private class Processor : ILogEntryProcessor
        {
            private ILogger _logger;
            public Processor(ILogger logger)
            {
                _logger = logger;
            }
            public LogEntryHandler<TState> GetLogEntryHandler<TState>(ILogMetadata<TState>? metadata, out bool enabled, out bool dynamicEnabledCheckRequired)
            {
                enabled = true;
                dynamicEnabledCheckRequired = true;
                return new InvokeLoggerLogHandler<TState>(_logger);
            }

            public ScopeHandler<TState> GetScopeHandler<TState>(ILogMetadata<TState>? metadata, out bool enabled) where TState : notnull
            {
                enabled = true;
                return new InvokeLoggerScopeHandler<TState>(_logger);
            }

            public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);
        }

        public ProcessorContext GetProcessor()
        {
            if(_logger is ILogEntryProcessorFactory factory)
            {
                return factory.GetProcessor();
            }
            return new ProcessorContext(new Processor(_logger), CancellationToken.None);
        }

        /// <inheritdoc />
        IDisposable? ILogger.BeginScope<TState>(TState state)
        {
            return _logger.BeginScope(state);
        }

        /// <inheritdoc />
        bool ILogger.IsEnabled(LogLevel logLevel)
        {
            return _logger.IsEnabled(logLevel);
        }

        /// <inheritdoc />
        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            _logger.Log(logLevel, eventId, state, exception, formatter);
        }
    }

}
