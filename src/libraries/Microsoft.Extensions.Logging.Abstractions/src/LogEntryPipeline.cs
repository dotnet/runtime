// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging
{
    public interface ILogEntryPipelineFactory
    {
        public LogEntryPipeline<TState>? GetLoggingPipeline<TState>(ILogMetadata<TState>? metadata, object? userState);
        public ScopePipeline<TState>? GetScopePipeline<TState>(ILogMetadata<TState>? metadata, object? userState) where TState : notnull;
    }

    public class Pipeline
    {
        public Pipeline(object? userState, bool isEnabled, bool isDynamicLevelCheckRequired)
        {
            UserState = userState;
            IsEnabled = isEnabled;
            IsDynamicLevelCheckRequired = isDynamicLevelCheckRequired;
            IsUpToDate = true;
        }

        public object? UserState { get; }
        public bool IsEnabled { get; }
        public bool IsDynamicLevelCheckRequired { get; }
        public bool IsUpToDate { get; set; }
    }

    //public class ScopePipeline
    //{
    //    public ScopePipeline(object? userState, bool isEnabled, bool isDynamicLevelCheckRequired)
    //    {
    //        UserState = userState;
    //        IsEnabled = isEnabled;
    //        IsDynamicLevelCheckRequired = isDynamicLevelCheckRequired;
    //        IsUpToDate = true;
    //    }

    //    public object? UserState { get; }
    //    public bool IsEnabled { get; }
    //    public bool IsDynamicLevelCheckRequired { get; }
    //    public bool IsUpToDate { get; set; }
    //}

    public class LogEntryPipeline<TState> : Pipeline
    {
        public LogEntryPipeline(LogEntryHandler<TState, EmptyEnrichmentPropertyValues> handler, object? userState, bool isEnabled, bool isDynamicLevelCheckRequired) :
            base(userState, isEnabled, isDynamicLevelCheckRequired)
        {
            _firstHandler = handler;
        }

        private readonly LogEntryHandler<TState, EmptyEnrichmentPropertyValues> _firstHandler;

        public bool IsEnabledDynamic(LogLevel level) => _firstHandler.IsEnabled(level);
        public void HandleLogEntry(ref LogEntry<TState, EmptyEnrichmentPropertyValues> logEntry) => _firstHandler.HandleLogEntry(ref logEntry);
    }

    public class ScopePipeline<TState> : Pipeline where TState : notnull
    {
        public ScopePipeline(ScopeHandler<TState> handler, object? userState, bool isEnabled, bool isDynamicLevelCheckRequired) :
            base(userState, isEnabled, isDynamicLevelCheckRequired)
        {
            _firstHandler = handler;
        }

        private readonly ScopeHandler<TState> _firstHandler;

        public bool IsEnabledDynamic(LogLevel level) => _firstHandler.IsEnabled(level);
        public IDisposable? HandleScope(ref TState scope) => _firstHandler.HandleBeginScope(ref scope);
    }

    public struct EmptyEnrichmentPropertyValues
    {
    }
}
