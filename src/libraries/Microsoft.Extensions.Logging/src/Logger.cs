// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Extensions.Logging
{
    internal sealed class Logger : ILogger, ILogEntryPipelineFactory
    {
        private readonly LoggerFactory _loggerFactory;

        public Logger(LoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public VersionedLoggerState VersionedState { get; set; } = VersionedLoggerState.Default;

        public Action ProcessorInvalidated => () => _loggerFactory.OnProcessorInvalidated(this);

        public LogEntryPipeline<TState>? GetLoggingPipeline<TState>(ILogMetadata<TState>? metadata, object? userState)
        {
            return VersionedState.GetLoggingPipeline(metadata, userState);
        }

        public ScopePipeline<TState>? GetScopePipeline<TState>(ILogMetadata<TState>? metadata, object? userState) where TState : notnull
        {
            return VersionedState.GetScopePipeline(metadata, userState);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            ILogMetadata<TState>? metadata = null;
            if (state is ILoggerStateWithMetadata<TState>)
            {
                metadata = ((ILoggerStateWithMetadata<TState>)state).Metadata;
            }
            LogEntryPipeline<TState> pipeline = GetLoggingPipeline<TState>(metadata, this)!;
            if (!pipeline.IsEnabled || (pipeline.IsDynamicLevelCheckRequired && !pipeline.IsEnabledDynamic(logLevel)))
            {
                return;
            }
            LogEntry<TState> logEntry = new LogEntry<TState>(logLevel, category: null!, eventId, state, exception, formatter);
            pipeline.HandleLogEntry(ref logEntry);
        }

        public bool IsEnabled(LogLevel level)
        {
            ILogEntryProcessor processor = VersionedState.Processor;
            if (processor != null)
            {
                return processor.IsEnabled(level);
            }
            return false;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            ILogMetadata<TState>? metadata = null;
            if (state is ILoggerStateWithMetadata<TState>)
            {
                metadata = ((ILoggerStateWithMetadata<TState>)state).Metadata;
            }
            ScopePipeline<TState>? pipeline = GetScopePipeline<TState>(metadata, this);
            if (pipeline is null || !pipeline.IsEnabled)
            {
                return null;
            }
            return pipeline.HandleScope(ref state);
        }

        internal static void ThrowLoggingError(List<Exception> exceptions)
        {
            throw new AggregateException(
                message: "An error occurred while writing to logger(s).", innerExceptions: exceptions);
        }
    }

    internal sealed class Scope : IDisposable
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

    internal sealed class VersionedLoggerState
    {
        public static readonly VersionedLoggerState Default = new VersionedLoggerState();

        public VersionedLoggerState()
        {
            Loggers = Array.Empty<LoggerInformation>();
            Processor = NullLogProcessor.Instance;
            FilterOptions = new LoggerFilterOptions();
        }

        public VersionedLoggerState(LoggerInformation[] loggers, ILogEntryProcessor processor, LoggerFilterOptions filterOptions)
        {
            Loggers = loggers;
            Processor = processor;
            FilterOptions = filterOptions;
            _isUpToDate = true;
        }

        private bool _isUpToDate;
        public LoggerInformation[] Loggers { get; }
        public ILogEntryProcessor Processor { get; }
        public LoggerFilterOptions FilterOptions { get; }
        public Dictionary<PipelineKey, Pipeline> Pipelines { get; } = new Dictionary<PipelineKey, Pipeline>();

        public LogEntryPipeline<TState>? GetLoggingPipeline<TState>(ILogMetadata<TState>? metadata, object? userState)
        {
            // The default versioned state should never be used to create pipelines, it is a shared singleton
            // that exists just to satisfy nullability checks
            Debug.Assert(this != Default);

            Pipeline? pipeline;
            PipelineKey key = new PipelineKey(isLoggingPipeline: true, (metadata == null) ? typeof(TState) : metadata, terminalProcessor: null, userState: userState);
            lock (Pipelines)
            {
                if (!Pipelines.TryGetValue(key, out pipeline))
                {
                    LogEntryHandler<TState> handler = Processor.GetLogEntryHandler<TState>(metadata, out bool enabled, out bool dynamicCheckRequired);
                    pipeline = new LogEntryPipeline<TState>(handler, userState, enabled, dynamicCheckRequired);
                    // in a multi-threaded race it is possible to create new pipelines after the versioned state is already disposed
                    // if this happens the pipeline is immediately marked as being not up-to-date.
                    pipeline.IsUpToDate = _isUpToDate;
                    Pipelines[key] = pipeline;
                }
            }
            return (LogEntryPipeline<TState>?)pipeline;
        }

        public ScopePipeline<TState>? GetScopePipeline<TState>(ILogMetadata<TState>? metadata, object? userState) where TState : notnull
        {
            // The default versioned state should never be used to create pipelines, it is a shared singleton
            // that exists just to satisfy nullability checks
            Debug.Assert(this != Default);

            if (!FilterOptions.CaptureScopes)
            {
                return null;
            }

            Pipeline? pipeline;
            PipelineKey key = new PipelineKey(isLoggingPipeline: false, (metadata == null) ? typeof(TState) : metadata, terminalProcessor: null, userState: userState);
            lock (Pipelines)
            {
                if (!Pipelines.TryGetValue(key, out pipeline))
                {
                    ScopeHandler<TState> handler = Processor.GetScopeHandler<TState>(metadata, out bool enabled, out bool dynamicCheckRequired);
                    pipeline = new ScopePipeline<TState>(handler, userState, enabled, dynamicCheckRequired);
                    // in a multi-threaded race it is possible to create new pipelines after the versioned state is already disposed
                    // if this happens the pipeline is immediately marked as being not up-to-date.
                    pipeline.IsUpToDate = _isUpToDate;
                    Pipelines[key] = pipeline;
                }
            }
            return (ScopePipeline<TState>?)pipeline;
        }

        public void MarkNotUpToDate()
        {
            lock (Pipelines)
            {
                _isUpToDate = false;
                foreach (Pipeline pipeline in Pipelines.Values)
                {
                    pipeline.IsUpToDate = false;
                }
            }
        }

        private sealed class NullLogProcessor : ILogEntryProcessor
        {
            public static readonly NullLogProcessor Instance = new NullLogProcessor();

            public LogEntryHandler<TState> GetLogEntryHandler<TState>(ILogMetadata<TState>? metadata, out bool enabled, out bool dynamicEnabledCheckRequired)
            {
                throw new NotImplementedException();
            }

            public ScopeHandler<TState> GetScopeHandler<TState>(ILogMetadata<TState>? metadata, out bool enabled, out bool dynamicEnabledCheckRequired) where TState : notnull
            {
                throw new NotImplementedException();
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return false;
            }
        }
    }
}
