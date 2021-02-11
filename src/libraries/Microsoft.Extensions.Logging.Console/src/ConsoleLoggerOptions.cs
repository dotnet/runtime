// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging.Console
{
    /// <summary>
    /// Options for a <see cref="ConsoleLogger"/>.
    /// </summary>
    public class ConsoleLoggerOptions
    {
        /// <summary>
        /// Disables colors when <see langword="true" />.
        /// </summary>
        [System.ObsoleteAttribute("ConsoleLoggerOptions.DisableColors has been deprecated. Please use SimpleConsoleFormatterOptions.ColorBehavior instead.", false)]
        public bool DisableColors { get; set; }

#pragma warning disable CS0618
        private ConsoleLoggerFormat _format = ConsoleLoggerFormat.Default;
        /// <summary>
        /// Gets or sets log message format. Defaults to <see cref="ConsoleLoggerFormat.Default" />.
        /// </summary>
        [System.ObsoleteAttribute("ConsoleLoggerOptions.Format has been deprecated. Please use ConsoleLoggerOptions.FormatterName instead.", false)]
        public ConsoleLoggerFormat Format
        {
            get => _format;
            set
            {
                if (value < ConsoleLoggerFormat.Default || value > ConsoleLoggerFormat.Systemd)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _format = value;
            }
#pragma warning restore CS0618
        }

        /// <summary>
        /// Name of the log message formatter to use. Defaults to "simple" />.
        /// </summary>
        public string FormatterName { get; set; }

        /// <summary>
        /// Includes scopes when <see langword="true" />.
        /// </summary>
        [System.ObsoleteAttribute("ConsoleLoggerOptions.IncludeScopes has been deprecated. Please use ConsoleFormatterOptions.IncludeScopes instead.", false)]
        public bool IncludeScopes { get; set; }

        /// <summary>
        /// Gets or sets value indicating the minimum level of messages that would get written to <c>Console.Error</c>.
        /// </summary>
        public LogLevel LogToStandardErrorThreshold { get; set; } = LogLevel.None;

        /// <summary>
        /// Gets or sets format string used to format timestamp in logging messages. Defaults to <c>null</c>.
        /// </summary>
        [System.ObsoleteAttribute("ConsoleLoggerOptions.TimestampFormat has been deprecated. Please use ConsoleFormatterOptions.TimestampFormat instead.", false)]
        public string TimestampFormat { get; set; }

        /// <summary>
        /// Gets or sets indication whether or not UTC timezone should be used to for timestamps in logging messages. Defaults to <c>false</c>.
        /// </summary>
        [System.ObsoleteAttribute("ConsoleLoggerOptions.UseUtcTimestamp has been deprecated. Please use ConsoleFormatterOptions.UseUtcTimestamp instead.", false)]
        public bool UseUtcTimestamp { get; set; }
    }
}