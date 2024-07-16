// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Logging.Abstractions
{
    /// <summary>
    /// Logging providers can implement this interface to indicate they support buffered logging.
    /// </summary>
    /// <remarks>
    /// A logging provider normally exposes an <see cref="ILogger" /> interface that gets invoked by the
    /// logging infrastructure whenever it’s time to log a piece of state.
    ///
    /// The logging infrastructure will type-test the <c>ILogger</c> object to determine if
    /// it supports the <c>IBufferedLogger</c> interface also. If it does, that tells the
    /// logging infrastructure that the logging provider supports buffering. Whenever log
    /// buffering is enabled, buffered log records will be delivered to the logging provider
    /// via the <c>IBufferedLogger</c> interface.
    ///
    /// If a logging provider does not support log buffering, then it will always be given
    /// unbuffered log records. In other words, whether or not buffering is requested by
    /// the user, it will not happen for those log providers.
    /// </remarks>
    public interface IBufferedLogger
    {
        /// <summary>
        /// Delivers a batch of buffered log records to a logging provider.
        /// </summary>
        /// <param name="records">The buffered log records to log.</param>
        /// <remarks>
        /// Once this function returns, the implementation should no longer access the records
        /// or state referenced by these records since they will get recycled.
        /// </remarks>
        void LogRecords(IEnumerable<BufferedLogRecord> records);
    }
}
