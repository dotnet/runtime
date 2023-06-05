// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Extensions.Logging
{
    internal sealed class Logger : ILogger, ILogEntryProcessorFactory
    {
        private readonly LoggerFactory _loggerFactory;

        public Logger(LoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public VersionedLoggerState VersionedState { get; set; } = VersionedLoggerState.Default;

        public Action ProcessorInvalidated => () => _loggerFactory.OnProcessorInvalidated(this);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            LogEntryHandler<TState> handler = VersionedState.GetLogEntryHandler<TState>(null, out bool enabled, out bool dynamicCheckRequired);
            if(!enabled || (dynamicCheckRequired && !handler.IsEnabled(logLevel)))
            {
                return;
            }
            LogEntry<TState> logEntry = new LogEntry<TState>(logLevel, category: null!, eventId, state, exception, formatter);
            handler.HandleLogEntry(ref logEntry);
        }

        public bool IsEnabled(LogLevel level) => VersionedState.IsEnabled(level);

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            ScopeHandler<TState> handler = VersionedState.GetScopeHandler<TState>(null, out bool _);
            return handler.HandleBeginScope(ref state);
        }

        internal static void ThrowLoggingError(List<Exception> exceptions)
        {
            throw new AggregateException(
                message: "An error occurred while writing to logger(s).", innerExceptions: exceptions);
        }

        public ProcessorContext GetProcessor() => new ProcessorContext(VersionedState, VersionedState.CancelToken);
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

    internal sealed class VersionedLoggerState : ILogEntryProcessor, IDisposable
    {
        internal readonly struct HandlerKey
        {
            public HandlerKey(bool isLoggingHandler, object typeOrMetadata)
            {
                IsLoggingPipeline = isLoggingHandler;
                TypeOrMetadata = typeOrMetadata;
            }

            public bool IsLoggingPipeline { get; }
            public object TypeOrMetadata { get; }
        }

        internal readonly struct LogEntryHandlerState<TState>
        {
            public LogEntryHandlerState(LogEntryHandler<TState> handler, bool enabled, bool dynamicEnableCheckRequired)
            {
                Handler = handler;
                IsEnabled = enabled;
                IsDynamicEnableCheckRequired = dynamicEnableCheckRequired;
            }
            public LogEntryHandler<TState> Handler { get; }
            public bool IsEnabled { get; }
            public bool IsDynamicEnableCheckRequired { get; }
        }

        internal readonly struct ScopeHandlerState<TState> where TState : notnull
        {
            public ScopeHandlerState(ScopeHandler<TState> handler, bool enabled)
            {
                Handler = handler;
                IsEnabled = enabled;
            }
            public ScopeHandler<TState> Handler { get; }
            public bool IsEnabled { get; }
        }

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
            _cancelSource = new CancellationTokenSource();
        }

        private CancellationTokenSource _cancelSource = new CancellationTokenSource();
        public CancellationToken CancelToken => _cancelSource.Token;
        public LoggerInformation[] Loggers { get; }
        public LoggerFilterOptions FilterOptions { get; }
        private ILogEntryProcessor Processor { get; }

        private Dictionary<HandlerKey, object> Handlers { get; } = new Dictionary<HandlerKey, object>();

        public LogEntryHandler<TState> GetLogEntryHandler<TState>(ILogMetadata<TState>? metadata, out bool enabled, out bool dynamicEnableCheckRequired)
        {
            // The default versioned state should never be used to create pipelines, it is a shared singleton
            // that exists just to satisfy nullability checks
            Debug.Assert(this != Default);

            object? handlerObject;
            LogEntryHandlerState<TState> handlerState;
            HandlerKey key = new HandlerKey(isLoggingHandler: true, (metadata == null) ? typeof(TState) : metadata);
            lock (Handlers)
            {
                if (!Handlers.TryGetValue(key, out handlerObject))
                {
                    LogEntryHandler<TState> handler = Processor.GetLogEntryHandler(metadata, out enabled, out dynamicEnableCheckRequired);
                    handlerState = new LogEntryHandlerState<TState>(handler, enabled, dynamicEnableCheckRequired);
                    Handlers[key] = handlerState;
                }
                else
                {
                    handlerState = (LogEntryHandlerState<TState>)handlerObject;
                }
            }
            enabled = handlerState.IsEnabled;
            dynamicEnableCheckRequired = handlerState.IsDynamicEnableCheckRequired;
            return handlerState.Handler;
        }

        public ScopeHandler<TState> GetScopeHandler<TState>(ILogMetadata<TState>? metadata, out bool enabled) where TState : notnull
        {
            // The default versioned state should never be used to create pipelines, it is a shared singleton
            // that exists just to satisfy nullability checks
            Debug.Assert(this != Default);

            object? handlerObject;
            ScopeHandlerState<TState> handlerState;
            HandlerKey key = new HandlerKey(isLoggingHandler: true, (metadata == null) ? typeof(TState) : metadata);
            lock (Handlers)
            {
                if (!Handlers.TryGetValue(key, out handlerObject))
                {
                    if (FilterOptions.CaptureScopes)
                    {
                        ScopeHandler<TState> handler = Processor.GetScopeHandler(metadata, out enabled);
                        handlerState = new ScopeHandlerState<TState>(handler, enabled);
                    }
                    else
                    {
                        handlerState = new ScopeHandlerState<TState>(new NullScopeHandler<TState>(), false);
                    }
                    Handlers[key] = handlerState;
                }
                else
                {
                    handlerState = (ScopeHandlerState<TState>)handlerObject;
                }
            }
            enabled = handlerState.IsEnabled;
            return handlerState.Handler;
        }

        public bool IsEnabled(LogLevel logLevel) => Processor.IsEnabled(logLevel);
        public void Dispose()
        {
            // the default versioned state is never disposed
            if (this != Default)
            {
                _cancelSource.Cancel();
                _cancelSource.Dispose();
            }
        }

        private sealed class NullLogProcessor : ILogEntryProcessor
        {
            public static readonly NullLogProcessor Instance = new NullLogProcessor();

            public LogEntryHandler<TState> GetLogEntryHandler<TState>(ILogMetadata<TState>? metadata, out bool enabled, out bool dynamicEnabledCheckRequired)
            {
                throw new NotImplementedException();
            }

            public ScopeHandler<TState> GetScopeHandler<TState>(ILogMetadata<TState>? metadata, out bool enabled) where TState : notnull
            {
                throw new NotImplementedException();
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return false;
            }
        }

        private sealed class NullScopeHandler<TState> : ScopeHandler<TState> where TState : notnull
        {
            public override IDisposable? HandleBeginScope(ref TState state) => null;
            public override bool IsEnabled(LogLevel level) => false;
        }
    }
}
