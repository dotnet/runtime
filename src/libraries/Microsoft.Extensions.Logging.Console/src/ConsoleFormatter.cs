// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Extensions.Logging.Console
{
    /// <summary>
    /// Allows custom log messages formatting
    /// </summary>
    public abstract class ConsoleFormatter
    {
        protected ConsoleFormatter(string name)
        {
            ThrowHelper.ThrowIfNull(name);

            Name = name;
        }

        /// <summary>
        /// Gets the name associated with the console log formatter.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Writes the log message to the specified TextWriter.
        /// </summary>
        /// <remarks>
        /// if the formatter wants to write colors to the console, it can do so by embedding ANSI color codes into the string
        /// </remarks>
        /// <param name="logEntry">The log entry.</param>
        /// <param name="scopeProvider">The provider of scope data.</param>
        /// <param name="textWriter">The string writer embedding ansi code for colors.</param>
        /// <typeparam name="TState">The type of the object to be written.</typeparam>
        public abstract void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter);
    }
}
