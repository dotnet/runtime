// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Logging.Console
{
    /// <summary>
    /// Options for the built-in json console log formatter.
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

#pragma warning disable SYSLIB1100
#pragma warning disable SYSLIB1101
        internal override void Configure(IConfiguration configuration) => configuration.Bind(this);
#pragma warning restore
    }
}
