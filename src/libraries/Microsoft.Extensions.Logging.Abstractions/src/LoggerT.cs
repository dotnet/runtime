// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Internal;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Delegates to a new <see cref="ILogger"/> instance using the full name of the given type, created by the
    /// provided <see cref="ILoggerFactory"/>.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    public class Logger<T> : ILogger<T>, ILogEntryPipelineFactory
    {
        //private Dictionary<object, object> _metadataPipelines = new Dictionary<object, object>(); // metadata -> LogEntryPipeline<TState>
        //private Dictionary<Type, object> _noMetadataPipelines = new Dictionary<Type, object>();
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

        ScopePipeline<TState>? ILogEntryPipelineFactory.GetScopePipeline<TState>(ILogMetadata<TState>? metadata, object? userState)
        {
            if (_logger is ILogEntryPipelineFactory factory)
            {
                return factory.GetScopePipeline(metadata, userState);
            }
            else
            {
                return null;
            }
        }

        LogEntryPipeline<TState>? ILogEntryPipelineFactory.GetLoggingPipeline<TState>(ILogMetadata<TState>? metadata, object? userState)
        {
            if (_logger is ILogEntryPipelineFactory factory)
            {
                return factory.GetLoggingPipeline(metadata, userState);
            }
            else
            {
                return null;
            }

            /*
            if (_logger is not ILogEntryProcessor)
                return null;
            object pipeline;
            if (metadata != null)
            {
                lock (_metadataPipelines)
                {
                    if (!_metadataPipelines.TryGetValue(metadata, out pipeline))
                    {
                        pipeline = BuildPipeline<TState>(metadata);
                        _metadataPipelines[metadata] = pipeline;
                    }
                }
            }
            else
            {
                lock (_noMetadataPipelines)
                {
                    if (!_noMetadataPipelines.TryGetValue(typeof(TState), out pipeline))
                    {
                        pipeline = BuildPipeline<TState>(null);
                        _noMetadataPipelines[typeof(TState)] = pipeline;
                    }
                }
            }
            return (LogEntryPipeline<TState>)pipeline;
            */
        }

        /*
        private LogEntryPipeline<TState> BuildPipeline<TState>(ILogMetadata<TState>? metadata)
        {
            return new LogEntryPipeline<TState>(((ILogEntryProcessor)_logger).GetLogEntryHandler(metadata), this, true, false);
        }*/
    }

}
