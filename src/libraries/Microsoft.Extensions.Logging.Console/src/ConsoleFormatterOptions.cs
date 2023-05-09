// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Logging.Console
{
    /// <summary>
    /// Options for the built-in console log formatter.
    /// </summary>
    public class ConsoleFormatterOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleFormatterOptions"/> class.
        /// </summary>
        public ConsoleFormatterOptions() { }

        /// <summary>
        /// Includes scopes when <see langword="true" />.
        /// </summary>
        public bool IncludeScopes { get; set; }

        /// <summary>
        /// Gets or sets format string used to format timestamp in logging messages. Defaults to <c>null</c>.
        /// </summary>
        [StringSyntax(StringSyntaxAttribute.DateTimeFormat)]
        public string? TimestampFormat { get; set; }

        /// <summary>
        /// Gets or sets indication whether or not UTC timezone should be used to format timestamps in logging messages. Defaults to <c>false</c>.
        /// </summary>
        public bool UseUtcTimestamp { get; set; }

        internal virtual void Configure(IConfiguration configuration)
        {
            if (ConsoleLoggerOptions.ParseBool(configuration, nameof(IncludeScopes), out bool includeScopes))
            {
                IncludeScopes = includeScopes;
            }

            if (configuration[nameof(TimestampFormat)] is string timestampFormat)
            {
                TimestampFormat = timestampFormat;
            }

            if (ConsoleLoggerOptions.ParseBool(configuration, nameof(UseUtcTimestamp), out bool useUtcTimestamp))
            {
                UseUtcTimestamp = useUtcTimestamp;
            }
        }
    }
}
