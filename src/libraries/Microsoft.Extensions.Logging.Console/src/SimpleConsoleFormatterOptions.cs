// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging.Console
{
    /// <summary>
    /// Options for the built-in default console log formatter.
    /// </summary>
    public class SimpleConsoleFormatterOptions : ConsoleFormatterOptions
    {
        public SimpleConsoleFormatterOptions() { }

        /// <summary>
        /// Determines when to use color when logging messages.
        /// </summary>
        public LoggerColorBehavior ColorBehavior { get; set; }

        /// <summary>
        /// When <see langword="false" />, the entire message gets logged in a single line.
        /// </summary>
        public bool SingleLine { get; set; }
    }
}
