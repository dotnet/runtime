// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// State representing a buffered log record.
    /// </summary>
    /// <remarks>
    /// Objects of this type are reused over time to reduce
    /// allocations.
    /// </remarks>
    public abstract class BufferedLogRecord
    {
        /// <summary>
        /// Gets the time when the log record was first created.
        /// </summary>
        public abstract DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Gets the record's log level, indicating it rough importance
        /// </summary>
        public abstract LogLevel LogLevel { get; }

        /// <summary>
        /// Gets the records event id.
        /// </summary>
        public abstract EventId EventId { get; }

        /// <summary>
        /// Gets an optional exception string for this record.
        /// </summary>
        public abstract string? Exception { get => null; }

        /// <summary>
        /// Gets an activity span id for this record, representing the state of the thread that created the record.
        /// </summary>
        public abstract ActivitySpanId? ActivitySpanId { get => null; }

        /// <summary>
        /// Gets an activity trace id for this record, representing the state of the thread that created the record.
        /// </summary>
        public abstract ActivityTraceId? ActivityTraceId { get => null; }

        /// <summary>
        /// Gets the ID of the thread that created the log record.
        /// </summary>
        public abstract int? ManagedThreadId { get => null; }

        /// <summary>
        /// Gets the formatted log message.
        /// </summary>
        public abstract string? FormattedMessage { get => null; }

        /// <summary>
        /// Gets the original log message template.
        /// </summary>
        public abstract string? MessageTemplate { get => null; }

        /// <summary>
        /// Gets the variable set of name/value pairs associated with the record.
        /// </summary>
        public abstract IReadOnlyList<KeyValuePair<string, object?>> Attributes { get => Array.Empty<KeyValuePair<string, object?>>(); }
    }
}
