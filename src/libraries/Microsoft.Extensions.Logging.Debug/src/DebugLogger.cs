// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.Extensions.Logging.Debug
{
    /// <summary>
    /// A logger that writes messages in the debug output window only when a debugger is attached.
    /// </summary>
    internal sealed partial class DebugLogger : ILogger
    {
        private readonly string _name;

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugLogger"/> class.
        /// </summary>
        /// <param name="name">The name of the logger.</param>
        public DebugLogger(string name)
        {
            _name = name;
        }

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return NullScope.Instance;
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            // Everything is enabled unless the debugger is not attached
            return Debugger.IsAttached && logLevel != LogLevel.None;
        }

        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            ArgumentNullException.ThrowIfNull(formatter);

            string formatted = formatter(state, exception);

            if (string.IsNullOrEmpty(formatted) && exception == null)
            {
                // With no formatted message or exception, there's nothing to print.
                return;
            }

            string message;
            if (string.IsNullOrEmpty(formatted))
            {
                System.Diagnostics.Debug.Assert(exception != null);
                message = $"{logLevel}: {exception}";
            }
            else if (exception == null)
            {
                message = $"{logLevel}: {formatted}";
            }
            else
            {
                message = $"{logLevel}: {formatted}{Environment.NewLine}{Environment.NewLine}{exception}";
            }

            DebugWriteLine(message, _name);
        }
    }
}
