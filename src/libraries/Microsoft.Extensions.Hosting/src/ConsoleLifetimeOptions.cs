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
        /// Indicates if host lifetime status messages should be suppressed such as on startup.
        /// The default is false.
        /// </summary>
        public bool SuppressStatusMessages { get; set; }
    }
}
