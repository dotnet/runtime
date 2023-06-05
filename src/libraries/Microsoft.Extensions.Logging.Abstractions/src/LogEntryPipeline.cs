// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Extensions.Logging
{
    //TODO: Pipeline isn't the best name for this, but leaving it as-is to make the refactor clearer.
    //Really it is a single entry in a cache that maps ILogger -> Handler
    internal class Pipeline
    {
        public Pipeline(object? userState, bool isEnabled, bool isDynamicLevelCheckRequired, CancellationToken cancelToken)
        {
            UserState = userState;
            IsEnabled = isEnabled;
            IsDynamicLevelCheckRequired = isDynamicLevelCheckRequired;
            CancelToken = cancelToken;
        }

        public object? UserState { get; }
        public bool IsEnabled { get; }
        public bool IsDynamicLevelCheckRequired { get; }
        public CancellationToken CancelToken { get; }
    }

    internal class LogEntryPipeline<TState> : Pipeline
    {
        public LogEntryPipeline(LogEntryHandler<TState> handler, object? userState, bool isEnabled, bool isDynamicLevelCheckRequired, CancellationToken cancelToken) :
            base(userState, isEnabled, isDynamicLevelCheckRequired, cancelToken)
        {
            _firstHandler = handler;
        }

        private readonly LogEntryHandler<TState> _firstHandler;

        public bool IsEnabledDynamic(LogLevel level) => _firstHandler.IsEnabled(level);
        public void HandleLogEntry(ref LogEntry<TState> logEntry) => _firstHandler.HandleLogEntry(ref logEntry);
    }

    internal class ScopePipeline<TState> : Pipeline where TState : notnull
    {
        public ScopePipeline(ScopeHandler<TState> handler, object? userState, bool isEnabled, CancellationToken cancelToken) :
            base(userState, isEnabled, isDynamicLevelCheckRequired: false, cancelToken)
        {
            _firstHandler = handler;
        }

        private readonly ScopeHandler<TState> _firstHandler;

        public IDisposable? HandleScope(ref TState scope) => _firstHandler.HandleBeginScope(ref scope);
    }


    internal class InvokeLoggerLogHandler<TState> : LogEntryHandler<TState>
    {
        private ILogger _logger;
        public InvokeLoggerLogHandler(ILogger logger)
        {
            _logger = logger;
        }
        public override void HandleLogEntry(ref LogEntry<TState> logEntry) => _logger.Log(logEntry.LogLevel, logEntry.EventId, logEntry.State, logEntry.Exception, logEntry.Formatter);
        public override bool IsEnabled(LogLevel level) => _logger.IsEnabled(level);
    }

    internal class InvokeLoggerScopeHandler<TState> : ScopeHandler<TState> where TState : notnull
    {
        private ILogger _logger;
        public InvokeLoggerScopeHandler(ILogger logger)
        {
            _logger = logger;
        }
        public override IDisposable? HandleBeginScope(ref TState state) => _logger.BeginScope(state);
        public override bool IsEnabled(LogLevel level) => true;
    }
}
