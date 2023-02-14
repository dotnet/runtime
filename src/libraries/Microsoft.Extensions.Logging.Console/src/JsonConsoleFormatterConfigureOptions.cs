// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.Versioning;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Configures a JsonConsoleFormatterOptions object from an IConfiguration.
    /// </summary>
    /// <remarks>
    /// Doesn't use ConfigurationBinder in order to allow ConfigurationBinder, and all its dependencies,
    /// to be trimmed. This improves app size and startup.
    /// </remarks>
    [UnsupportedOSPlatform("browser")]
    internal sealed class JsonConsoleFormatterConfigureOptions : IConfigureOptions<JsonConsoleFormatterOptions>
    {
        private readonly IConfiguration _configuration;

        public JsonConsoleFormatterConfigureOptions(ILoggerProviderConfiguration<ConsoleLoggerProvider> providerConfiguration)
        {
            _configuration = providerConfiguration.GetFormatterOptionsSection();
        }

        public void Configure(JsonConsoleFormatterOptions options)
        {
            ConsoleFormatterConfigureOptions.Bind(_configuration, options);

            if (_configuration.GetSection("JsonWriterOptions") is IConfigurationSection jsonWriterOptionsConfig)
            {
                JsonWriterOptions jsonWriterOptions = options.JsonWriterOptions;

                if (jsonWriterOptionsConfig["Indented"] is string indented)
                {
                    jsonWriterOptions.Indented = bool.Parse(indented);
                }

                if (jsonWriterOptionsConfig["MaxDepth"] is string maxDepth)
                {
                    jsonWriterOptions.MaxDepth = int.Parse(maxDepth, NumberStyles.Integer, NumberFormatInfo.InvariantInfo);
                }

                if (jsonWriterOptionsConfig["SkipValidation"] is string skipValidation)
                {
                    jsonWriterOptions.SkipValidation = bool.Parse(skipValidation);
                }

                options.JsonWriterOptions = jsonWriterOptions;
            }
        }
    }
}
