// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging.Test
{
    internal sealed class TestScopeHandler<TState> : ScopeHandler<TState>
    {
        private readonly ScopeHandler<TState> _nextHandler;
        private readonly Action<string> _handleLogEntryCallback;

        public TestScopeHandler(ScopeHandler<TState> nextHandler, Action<string> handleLogEntryCallback)
        {
            _nextHandler = nextHandler;
            _handleLogEntryCallback = handleLogEntryCallback;
        }

        public override IDisposable? HandleBeginScope(ref TState state)
        {
            _handleLogEntryCallback(state.ToString());

            return _nextHandler.HandleBeginScope(ref state);
        }

        public override bool IsEnabled(LogLevel level)
        {
            return _nextHandler.IsEnabled(level);
        }
    }
}
