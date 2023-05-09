// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging
{
    public interface ILogEntryPipelineFactory
    {
        public LogEntryPipeline<TState>? GetPipeline<TState>(ILogMetadata<TState>? metadata, object? userState);
    }

    public class LogEntryPipeline
    {
        public LogEntryPipeline(object? userState, bool isEnabled, bool isDynamicLevelCheckRequired)
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

    public class LogEntryPipeline<TState> : LogEntryPipeline
    {
        public LogEntryPipeline(LogEntryHandler<TState, EmptyEnrichmentPropertyValues> handler, object? userState, bool isEnabled, bool isDynamicLevelCheckRequired) :
            base(userState, isEnabled, isDynamicLevelCheckRequired)
        {
            _firstHandler = handler;
        }

        private LogEntryHandler<TState, EmptyEnrichmentPropertyValues> _firstHandler;

        public bool IsEnabledDynamic(LogLevel level) => _firstHandler.IsEnabled(level);
        public void HandleLogEntry(ref LogEntry<TState, EmptyEnrichmentPropertyValues> logEntry) => _firstHandler.HandleLogEntry(ref logEntry);
    }

    public struct EmptyEnrichmentPropertyValues
    {
    }
}
