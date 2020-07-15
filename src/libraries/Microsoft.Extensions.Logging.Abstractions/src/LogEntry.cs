// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging.Abstractions
{
    /// <summary>
    /// Holds the information for a single log entry.
    /// </summary>
    public readonly struct LogEntry<TState>
    {
        /// <summary>
        /// Initializes an instance of the LogEntry struct.
        /// </summary>
        /// <param name="logLevel">The log level.</param>
        /// <param name="category">The category name for the log.</param>
        /// <param name="eventId">The log event Id.</param>
        /// <param name="state">The state for which log is being written.</param>
        /// <param name="exception">The log exception.</param>
        /// <param name="formatter">The formatter.</param>
        public LogEntry(LogLevel logLevel, string category, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            LogLevel = logLevel;
            Category = category;
            EventId = eventId;
            State = state;
            Exception = exception;
            Formatter = formatter;
        }

        /// <summary>
        /// Gets the LogLevel
        /// </summary>
        public LogLevel LogLevel { get; }

        /// <summary>
        /// Gets the log category
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// Gets the log EventId
        /// </summary>
        public EventId EventId { get; }

        /// <summary>
        /// Gets the TState
        /// </summary>
        public TState State { get; }

        /// <summary>
        /// Gets the log exception
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Gets the formatter
        /// </summary>
        public Func<TState, Exception, string> Formatter { get; }
    }
}
