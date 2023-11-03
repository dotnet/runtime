// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Logging.Console
{
    /// <summary>
    /// This type is retained only for compatibility. The recommended alternative is ConsoleLoggerOptions.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("This type is retained only for compatibility. The recommended alternative is ConsoleLoggerOptions.", error: true)]
    public interface IConsoleLoggerSettings
    {
        /// <summary>
        /// This property is retained only for compatibility.
        /// </summary>
        bool IncludeScopes { get; }

        /// <summary>
        /// This property is retained only for compatibility.
        /// </summary>
        IChangeToken? ChangeToken { get; }

        /// <summary>
        /// This property is retained only for compatibility.
        /// </summary>
        /// <param name="name">This property is retained only for compatibility.</param>
        /// <param name="level">This property is retained only for compatibility.</param>
        /// <returns>This property is retained only for compatibility.</returns>
        bool TryGetSwitch(string name, out LogLevel level);

        /// <summary>
        /// This method is retained only for compatibility.
        /// </summary>
        /// <returns>This method is retained only for compatibility.</returns>
        IConsoleLoggerSettings Reload();
    }
}
