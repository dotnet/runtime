// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging.Console
{
    public class ConsoleLoggerOptions
    {
        private ConsoleLoggerFormat _format = ConsoleLoggerFormat.Default;

        public bool IncludeScopes { get; set; }
        public bool DisableColors { get; set; }

        /// <summary>
        /// Gets or sets log message format. Defaults to <see cref="ConsoleLoggerFormat.Default" />.
        /// </summary>
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
        }

        /// <summary>
        /// Gets or sets value indicating the minimum level of messaged that would get written to <c>Console.Error</c>.
        /// </summary>
        public LogLevel LogToStandardErrorThreshold { get; set; } = LogLevel.None;

        /// <summary>
        /// Gets or sets format string used to format timestamp in logging messages. Defaults to <c>null</c>.
        /// </summary>
        public string TimestampFormat { get; set; }
    }
}