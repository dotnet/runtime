// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Logging.Console
{
    /// <summary>
    /// This type is retained only for compatibility. The recommended alternative is ConsoleLoggerOptions.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("This type is retained only for compatibility. The recommended alternative is ConsoleLoggerOptions.", error: true)]
    public class ConsoleLoggerSettings : IConsoleLoggerSettings
    {
        /// <inheritdoc/>
        public IChangeToken? ChangeToken { get; set; }

        /// <inheritdoc/>
        public bool IncludeScopes { get; set; }

        /// <summary>
        /// This property is retained only for compatibility.
        /// </summary>
        public bool DisableColors { get; set; }

        /// <summary>
        /// This property is retained only for compatibility.
        /// </summary>
        public IDictionary<string, LogLevel> Switches { get; set; } = new Dictionary<string, LogLevel>();

        /// <inheritdoc/>
        public IConsoleLoggerSettings Reload()
        {
            return this;
        }

        /// <inheritdoc/>
        public bool TryGetSwitch(string name, out LogLevel level)
        {
            return Switches.TryGetValue(name, out level);
        }
    }
}
