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
        /// Gets or sets a value that indicates whether colors are disabled.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if colors are disabled.
        /// </value>
        [System.ObsoleteAttribute("ConsoleLoggerOptions.DisableColors has been deprecated. Use SimpleConsoleFormatterOptions.ColorBehavior instead.")]
        public bool DisableColors { get; set; }

#pragma warning disable CS0618
        private ConsoleLoggerFormat _format = ConsoleLoggerFormat.Default;
        /// <summary>
        /// Gets or sets the log message format.
        /// </summary>
        /// <value>
        /// The default value is <see cref="ConsoleLoggerFormat.Default" />.
        /// </value>
        [System.ObsoleteAttribute("ConsoleLoggerOptions.Format has been deprecated. Use ConsoleLoggerOptions.FormatterName instead.")]
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
        /// Gets or sets the name of the log message formatter to use.
        /// </summary>
        /// <value>
        /// The default value is <see langword="simple" />.
        /// </value>
        public string? FormatterName { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether scopes are included.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if scopes are included.
        /// </value>
        [System.ObsoleteAttribute("ConsoleLoggerOptions.IncludeScopes has been deprecated. Use ConsoleFormatterOptions.IncludeScopes instead.")]
        public bool IncludeScopes { get; set; }

        /// <summary>
        /// Gets or sets value indicating the minimum level of messages that get written to <c>Console.Error</c>.
        /// </summary>
        public LogLevel LogToStandardErrorThreshold { get; set; } = LogLevel.None;

        /// <summary>
        /// Gets or sets the format string used to format timestamp in logging messages.
        /// </summary>
        /// <value>
        /// The default value is <see langword="null" />.
        /// </value>
        [System.ObsoleteAttribute("ConsoleLoggerOptions.TimestampFormat has been deprecated. Use ConsoleFormatterOptions.TimestampFormat instead.")]
        public string? TimestampFormat { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether UTC timezone should be used to format timestamps in logging messages.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the UTC timezone should be used to format timestamps. The default value is <see langword="false" />.
        /// </value>
        [System.ObsoleteAttribute("ConsoleLoggerOptions.UseUtcTimestamp has been deprecated. Use ConsoleFormatterOptions.UseUtcTimestamp instead.")]
        public bool UseUtcTimestamp { get; set; }

        private ConsoleLoggerQueueFullMode _queueFullMode = ConsoleLoggerQueueFullMode.Wait;
        /// <summary>
        /// Gets or sets the desired console logger behavior when the queue becomes full.
        /// </summary>
        /// <value>
        /// The default value is <see langword="wait" />.
        /// </value>
        public ConsoleLoggerQueueFullMode QueueFullMode
        {
            get => _queueFullMode;
            set
            {
                if (value != ConsoleLoggerQueueFullMode.Wait && value != ConsoleLoggerQueueFullMode.DropWrite)
                {
                    throw new ArgumentOutOfRangeException(SR.Format(SR.QueueModeNotSupported, nameof(value)));
                }
                _queueFullMode = value;
            }
        }

        internal const int DefaultMaxQueueLengthValue = 2500;
        private int _maxQueuedMessages = DefaultMaxQueueLengthValue;

        /// <summary>
        /// Gets or sets the maximum number of enqueued messages.
        /// </summary>
        /// <value>
        /// The default value is 2500.
        /// </value>
        public int MaxQueueLength
        {
            get => _maxQueuedMessages;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(SR.Format(SR.MaxQueueLengthBadValue, nameof(value)));
                }

                _maxQueuedMessages = value;
            }
        }
    }
}
