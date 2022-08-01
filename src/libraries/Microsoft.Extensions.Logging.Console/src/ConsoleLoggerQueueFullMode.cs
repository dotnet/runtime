// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging.Console
{
    /// <summary>
    /// Determines the console logger behavior when the queue becomes full.
    /// </summary>
    public enum ConsoleLoggerQueueFullMode
    {
        /// <summary>
        /// Blocks the logging threads once the queue limit is reached.
        /// </summary>
        Wait,
        /// <summary>
        /// Drops new log messages when the queue is full.
        /// </summary>
        DropWrite
    }
}
