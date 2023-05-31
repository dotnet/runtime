// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Extensions.Logging.Test
{
    internal sealed class TestLogEntryHandler<TState> : LogEntryHandler<TState>
    {
        private readonly LogEntryHandler<TState> _nextHandler;
        private readonly Action<string>? _handleLogEntryCallback;

        public TestLogEntryHandler(LogEntryHandler<TState> nextHandler, Action<string>? handleLogEntryCallback)
        {
            _nextHandler = nextHandler;
            _handleLogEntryCallback = handleLogEntryCallback;
        }

        public override void HandleLogEntry(ref LogEntry<TState> logEntry)
        {
            var message = logEntry.Formatter(logEntry.State, logEntry.Exception);
            _handleLogEntryCallback?.Invoke(message);

            _nextHandler.HandleLogEntry(ref logEntry);
        }

        public override bool IsEnabled(LogLevel level)
        {
            return _nextHandler.IsEnabled(level);
        }
    }
}
