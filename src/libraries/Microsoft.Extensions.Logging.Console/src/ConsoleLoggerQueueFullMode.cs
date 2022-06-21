// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging.Console
{
    /// <summary>
    /// Determines the console logger behavior when buffer becomes full.
    /// </summary>
    public enum ConsoleLoggerQueueFullMode
    {
        /// <summary>
        /// Blocks the logging threads once the buffer limit is reached.
        /// </summary>
        Wait,
        /// <summary>
        /// Drops new log messages when the buffer is full
        /// </summary>
        DropWrite
    }
}
