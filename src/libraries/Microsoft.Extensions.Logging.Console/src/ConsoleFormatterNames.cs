// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging.Console
{
    /// <summary>
    /// Reserved formatter names for the built-in console formatters.
    /// </summary>
    public static class ConsoleFormatterNames
    {
        /// <summary>
        /// Reserved name for simple console formatter
        /// </summary>
        public const string Simple = "simple";

        /// <summary>
        /// Reserved name for json console formatter
        /// </summary>
        public const string Json = "json";

        /// <summary>
        /// Reserved name for systemd console formatter
        /// </summary>
        public const string Systemd = "systemd";
    }
}
