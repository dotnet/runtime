// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging.Console
{
    /// <summary>
    /// Determines when color can be enabled log messages
    /// </summary>
    public enum LoggerColorBehavior
    {
        /// <summary>
        /// Disabled when output is redirected
        /// </summary>
        Default,
        /// <summary>
        /// Enable color for logging
        /// </summary>
        Enabled,
        /// <summary>
        /// Disable color for logging
        /// </summary>
        Disabled,
    }
}
