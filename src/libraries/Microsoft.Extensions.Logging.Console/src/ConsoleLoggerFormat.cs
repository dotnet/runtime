// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging.Console
{
    /// <summary>
    /// Describes the format of <see cref="ConsoleLogger" /> messages.
    /// </summary>
    [System.ObsoleteAttribute("ConsoleLoggerFormat has been deprecated.")]
    public enum ConsoleLoggerFormat
    {
        /// <summary>
        /// Produce messages in the default console format.
        /// </summary>
        Default,
        /// <summary>
        /// Produce messages in a format suitable for console output to the systemd journal.
        /// </summary>
        Systemd,
    }
}
