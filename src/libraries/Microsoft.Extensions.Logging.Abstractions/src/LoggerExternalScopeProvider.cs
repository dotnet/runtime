// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Default implemenation of <see cref="IExternalScopeProvider"/>
    /// </summary>
    public class LoggerExternalScopeProvider : IExternalScopeProvider
    {
        private readonly AsyncLocal<Scope> _currentScope = new AsyncLocal<Scope>();

        /// <inheritdoc />
        public void ForEachScope<TState>(Action<object, TState> callback, TState state)
        {
            void Report(Scope current)
            {
                if (current == null)
                {
                    return;
                }
                Report(current.Parent);
                callback(current.State, state);
            }
            Report(_currentScope.Value);
        }

        /// <inheritdoc />
        public IDisposable Push(object state)
        {
            var parent = _currentScope.Value;
            var newScope = new Scope(this, state, parent);
            _currentScope.Value = newScope;

            return newScope;
        }

        private class Scope : IDisposable
        {
            private readonly LoggerExternalScopeProvider _provider;
            private bool _isDisposed;

            internal Scope(LoggerExternalScopeProvider provider, object state, Scope parent)
            {
                _provider = provider;
                State = state;
                Parent = parent;
            }

            public Scope Parent { get; }

            public object State { get; }

            public override string ToString()
            {
                return State?.ToString();
            }

            public void Dispose()
            {
                if (!_isDisposed)
                {
                    _provider._currentScope.Value = Parent;
                    _isDisposed = true;
                }
            }
        }
    }
}
