// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Logging.Abstractions
{
    /// <summary>
    /// Represents the ability of a logging provider to support buffered logging.
    /// </summary>
    /// <remarks>
    /// A logging provider implements the <see cref="ILogger" /> interface that gets invoked by the
    /// logging infrastructure whenever it’s time to log a piece of state.
    ///
    /// A logging provider may also optionally implement the <see cref="IBufferedLogger" /> interface.
    /// The logging infrastructure may type-test the <see cref="ILogger" /> object to determine if
    /// it supports the <see cref="IBufferedLogger" /> interface. If it does, that indicates to the
    /// logging infrastructure that the logging provider supports buffering. Whenever log
    /// buffering is enabled, buffered log records may be delivered to the logging provider
    /// in a batch via <see cref="IBufferedLogger.LogRecords" />.
    ///
    /// If a logging provider does not support log buffering, then it will always be given
    /// unbuffered log records. If a logging provider does support log buffering, whether its
    /// <see cref="ILogger" /> or <see cref="IBufferedLogger" /> implementation is used is
    /// determined by the log producer.
    /// </remarks>
    public interface IBufferedLogger
    {
        /// <summary>
        /// Delivers a batch of buffered log records to a logging provider.
        /// </summary>
        /// <param name="records">The buffered log records to log.</param>
        /// <remarks>
        /// Once this function returns, the implementation should no longer access the records
        /// or state referenced by these records since the instances may be reused to represent other logs.
        /// </remarks>
        void LogRecords(IEnumerable<BufferedLogRecord> records);
    }
}
