// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.Extensions.Logging.Console
{
    /// <summary>
    /// Options for the built-in json console log formatter.
    /// </summary>
    public class JsonConsoleFormatterOptions : ConsoleFormatterOptions
    {
        public JsonConsoleFormatterOptions() { }

        /// <summary>
        /// Gets or sets JsonWriterOptions.
        /// </summary>
        public JsonWriterOptions JsonWriterOptions { get; set; }
    }
}
