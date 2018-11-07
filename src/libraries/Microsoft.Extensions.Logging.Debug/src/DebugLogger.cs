// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.Extensions.Logging.Debug
{
    /// <summary>
    /// A logger that writes messages in the debug output window only when a debugger is attached.
    /// </summary>
    public partial class DebugLogger : ILogger
    {
        private readonly Func<string, LogLevel, bool> _filter;
        private readonly string _name;

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugLogger"/> class.
        /// </summary>
        /// <param name="name">The name of the logger.</param>
        public DebugLogger(string name) : this(name, filter: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugLogger"/> class.
        /// </summary>
        /// <param name="name">The name of the logger.</param>
        /// <param name="filter">The function used to filter events based on the log level.</param>
        public DebugLogger(string name, Func<string, LogLevel, bool> filter)
        {
            _name = string.IsNullOrEmpty(name) ? nameof(DebugLogger) : name;
            _filter = filter;
        }


        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state)
        {
            return NoopDisposable.Instance;
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            // If the filter is null, everything is enabled
            // unless the debugger is not attached
            return Debugger.IsAttached &&
                logLevel != LogLevel.None &&
                (_filter == null || _filter(_name, logLevel));
        }

        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            var message = formatter(state, exception);

            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            message = $"{ logLevel }: {message}";

            if (exception != null)
            {
                message += Environment.NewLine + Environment.NewLine + exception.ToString();
            }

            DebugWriteLine(message, _name);
        }

        private class NoopDisposable : IDisposable
        {
            public static NoopDisposable Instance = new NoopDisposable();

            public void Dispose()
            {
            }
        }
    }
}
