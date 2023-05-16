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

        internal override void Configure(IConfiguration configuration)
        {
            base.Configure(configuration);

            if (configuration.GetSection(nameof(JsonWriterOptions)) is IConfigurationSection jsonWriterOptionsConfig)
            {
                JsonWriterOptions jsonWriterOptions = JsonWriterOptions;

                if (ConsoleLoggerOptions.ParseBool(jsonWriterOptionsConfig, nameof(JsonWriterOptions.Indented), out bool indented))
                {
                    jsonWriterOptions.Indented = indented;
                }

                if (ConsoleLoggerOptions.ParseInt(jsonWriterOptionsConfig, nameof(JsonWriterOptions.MaxDepth), out int maxDepth))
                {
                    jsonWriterOptions.MaxDepth = maxDepth;
                }

                if (ConsoleLoggerOptions.ParseBool(jsonWriterOptionsConfig, nameof(JsonWriterOptions.SkipValidation), out bool skipValidation))
                {
                    jsonWriterOptions.SkipValidation = skipValidation;
                }

                JsonWriterOptions = jsonWriterOptions;
            }
        }
    }
}
