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
        /// Gets or sets a value that indicates whether scopes are included.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if scopes are included.
        /// </value>
        public bool IncludeScopes { get; set; }

        /// <summary>
        /// Gets or sets the format string used to format timestamp in logging messages.
        /// </summary>
        /// <value>
        /// The default is <see langword="null" />.
        /// </value>
        [StringSyntax(StringSyntaxAttribute.DateTimeFormat)]
        public string? TimestampFormat { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether or not UTC timezone should be used to format timestamps in logging messages.
        /// </summary>
        /// <value>
        /// The default is <see langword="false" />.
        /// </value>
        public bool UseUtcTimestamp { get; set; }

        internal virtual void Configure(IConfiguration configuration) => configuration.Bind(this);
    }
}
