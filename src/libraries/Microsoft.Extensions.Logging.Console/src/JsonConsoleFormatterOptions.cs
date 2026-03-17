// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Logging.Console
{
    /// <summary>
    /// Options for the built-in JSON console log formatter.
    /// </summary>
    public class JsonConsoleFormatterOptions : ConsoleFormatterOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonConsoleFormatterOptions"/> class.
        /// </summary>
        public JsonConsoleFormatterOptions() { }

        /// <summary>
        /// Gets or sets JsonWriterOptions.
        /// </summary>
        public JsonWriterOptions JsonWriterOptions { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="System.Text.Json.JsonNamingPolicy"/> used to convert the built-in
        /// property names (<c>Timestamp</c>, <c>EventId</c>, <c>LogLevel</c>, <c>Category</c>,
        /// <c>Message</c>, <c>Exception</c>, <c>State</c>, <c>Scopes</c>) when writing JSON output.
        /// </summary>
        /// <value>
        /// The naming policy, or <see langword="null"/> to preserve the default PascalCase names.
        /// For example, setting this to <see cref="JsonNamingPolicy.CamelCase"/> produces
        /// <c>timestamp</c>, <c>eventId</c>, <c>logLevel</c>, etc.
        /// </value>
        /// <remarks>
        /// This property only affects the built-in property names emitted by the formatter.
        /// Property names from structured log state and scope data are written as-is.
        /// </remarks>
        public JsonNamingPolicy? JsonNamingPolicy { get; set; }

#pragma warning disable SYSLIB1100
#pragma warning disable SYSLIB1101
        internal override void Configure(IConfiguration configuration) => configuration.Bind(this);
#pragma warning restore
    }
}
