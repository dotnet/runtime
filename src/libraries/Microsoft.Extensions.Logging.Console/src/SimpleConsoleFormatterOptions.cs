// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Logging.Console
{
    /// <summary>
    /// Options for the built-in default console log formatter.
    /// </summary>
    public class SimpleConsoleFormatterOptions : ConsoleFormatterOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleConsoleFormatterOptions"/> class.
        /// </summary>
        public SimpleConsoleFormatterOptions() { }

        /// <summary>
        /// Determines when to use color when logging messages.
        /// </summary>
        public LoggerColorBehavior ColorBehavior { get; set; }

        /// <summary>
        /// When <see langword="true" />, the entire message gets logged in a single line.
        /// </summary>
        public bool SingleLine { get; set; }

        internal override void Configure(IConfiguration configuration)
        {
            base.Configure(configuration);

            if (ConsoleLoggerOptions.ParseEnum(configuration, nameof(ColorBehavior), out LoggerColorBehavior colorBehavior))
            {
                ColorBehavior = colorBehavior;
            }

            if (ConsoleLoggerOptions.ParseBool(configuration, nameof(SingleLine), out bool singleLine))
            {
                SingleLine = singleLine;
            }
        }
    }
}
