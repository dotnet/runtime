// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Provides option flags for <see cref="Internal.ConsoleLifetime"/>.
    /// </summary>
    public class ConsoleLifetimeOptions
    {
        /// <summary>
        /// Gets or sets a value that indicates if host lifetime status messages should be suppressed, such as on startup.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if host lifetime status messages should be suppressed.
        /// The default is <see langword="false"/>.
        /// </value>
        public bool SuppressStatusMessages { get; set; }
    }
}
