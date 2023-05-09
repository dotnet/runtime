// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

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

        public LogEntryPipeline<TState>? GetPipeline<TState>(ILogMetadata<TState>? metadata, object? userState)
        {
            return VersionedState.GetPipeline(metadata, userState);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            ILogMetadata<TState>? metadata = null;
            if (state is ILoggerStateWithMetadata<TState>)
            {
                metadata = ((ILoggerStateWithMetadata<TState>)state).Metadata;
            }
            LogEntryPipeline<TState> pipeline = GetPipeline<TState>(metadata, this)!;
            if (!pipeline.IsEnabled || (pipeline.IsDynamicLevelCheckRequired && !pipeline.IsEnabledDynamic(logLevel)))
                return;
            EmptyEnrichmentPropertyValues props = default;
            LogEntry<TState, EmptyEnrichmentPropertyValues> logEntry = new LogEntry<TState, EmptyEnrichmentPropertyValues>(logLevel, eventId, ref state, ref props, exception, formatter);
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
            throw new NotImplementedException();
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
        }

        internal static void ThrowLoggingError(List<Exception> exceptions)
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

    internal sealed class VersionedLoggerState
    {
        public static readonly VersionedLoggerState Default = new VersionedLoggerState();

        public VersionedLoggerState()
        {
            Loggers = Array.Empty<LoggerInformation>();
            Processor = NullLogProcessor.Instance;
        }

        public VersionedLoggerState(LoggerInformation[] loggers, ILogEntryProcessor processor)
        {
            Loggers = loggers;
            Processor = processor;
            _isUpToDate = true;
        }

        private bool _isUpToDate;
        public LoggerInformation[] Loggers { get; }
        public ILogEntryProcessor Processor { get; }
        public Dictionary<PipelineKey, LogEntryPipeline> Pipelines { get; } = new Dictionary<PipelineKey, LogEntryPipeline>();

        public LogEntryPipeline<TState>? GetPipeline<TState>(ILogMetadata<TState>? metadata, object? userState)
        {
            // The default versioned state should never be used to create pipelines, it is a shared singleton
            // that exists just to satisfy nullability checks
            Debug.Assert(this != Default);

            LogEntryPipeline? pipeline;
            PipelineKey key = new PipelineKey((metadata == null) ? typeof(TState) : metadata, terminalProcessor: null, userState: userState);
            lock (Pipelines)
            {
                if (!Pipelines.TryGetValue(key, out pipeline))
                {
                    LogEntryHandler<TState, EmptyEnrichmentPropertyValues> handler = Processor.GetLogEntryHandler<TState, EmptyEnrichmentPropertyValues>(metadata, out bool enabled, out bool dynamicCheckRequired);
                    pipeline = new LogEntryPipeline<TState>(handler, userState, enabled, dynamicCheckRequired);
                    // in a multi-threaded race it is possible to create new pipelines after the versioned state is already disposed
                    // if this happens the pipeline is immediately marked as being not up-to-date.
                    pipeline.IsUpToDate = _isUpToDate;
                    Pipelines[key] = pipeline;
                }
            }
            return (LogEntryPipeline<TState>?)pipeline;
        }

        public void MarkNotUpToDate()
        {
            lock (Pipelines)
            {
                _isUpToDate = false;
                foreach (LogEntryPipeline pipeline in Pipelines.Values)
                {
                    pipeline.IsUpToDate = false;
                }
            }
        }

        private sealed class NullLogProcessor : ILogEntryProcessor
        {
            public static readonly NullLogProcessor Instance = new NullLogProcessor();

            public LogEntryHandler<TState, TEnrichmentProperties> GetLogEntryHandler<TState, TEnrichmentProperties>(ILogMetadata<TState>? metadata, out bool enabled, out bool dynamicEnabledCheckRequired)
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
