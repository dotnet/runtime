// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging.Test
{
    internal sealed class TestLogEntryProcessor : ILogEntryProcessor
    {
        private readonly ILogEntryProcessor _nextProcessor;
        private readonly Action<string> _handleLogEntryCallback;

        public TestLogEntryProcessor(ILogEntryProcessor nextProcessor, Action<string> handleLogEntryCallback)
        {
            _nextProcessor = nextProcessor;
            _handleLogEntryCallback = handleLogEntryCallback;
        }

        public LogEntryHandler<TState> GetLogEntryHandler<TState>(ILogMetadata<TState>? metadata, out bool enabled, out bool dynamicEnabledCheckRequired)
        {
            var nextHandler = _nextProcessor.GetLogEntryHandler<TState>(metadata, out enabled, out dynamicEnabledCheckRequired);
            return new TestLogEntryHandler<TState>(nextHandler, _handleLogEntryCallback);
        }

        public ScopeHandler<TState> GetScopeHandler<TState>(ILogMetadata<TState>? metadata, out bool enabled) where TState : notnull
        {
            var nextHandler = _nextProcessor.GetScopeHandler<TState>(metadata, out enabled);
            return new TestScopeHandler<TState>(nextHandler, _handleLogEntryCallback);
        }

        public bool IsEnabled(LogLevel logLevel) => _nextProcessor.IsEnabled(logLevel);
    }
}
