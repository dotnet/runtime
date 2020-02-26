// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Extensions.Logging.Console
{
    /// <summary>
    /// Format of <see cref="ConsoleLogger" /> messages.
    /// </summary>
    public enum ConsoleLoggerFormat
    {
        /// <summary>
        /// Produces messages in the default console format.
        /// </summary>
        Default,
        /// <summary>
        /// Produces messages in a format suitable for console output to the systemd journal.
        /// </summary>
        Systemd,
    }
}
