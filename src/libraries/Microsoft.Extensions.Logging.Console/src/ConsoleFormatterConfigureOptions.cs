// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Configures a ConsoleFormatterOptions object from an IConfiguration.
    /// </summary>
    /// <remarks>
    /// Doesn't use ConfigurationBinder in order to allow ConfigurationBinder, and all its dependencies,
    /// to be trimmed. This improves app size and startup.
    /// </remarks>
    [UnsupportedOSPlatform("browser")]
    internal sealed class ConsoleFormatterConfigureOptions : IConfigureOptions<ConsoleFormatterOptions>
    {
        private readonly IConfiguration _configuration;

        public ConsoleFormatterConfigureOptions(ILoggerProviderConfiguration<ConsoleLoggerProvider> providerConfiguration)
        {
            _configuration = providerConfiguration.GetFormatterOptionsSection();
        }

        public void Configure(ConsoleFormatterOptions options) => Bind(_configuration, options);

        public static void Bind(IConfiguration configuration, ConsoleFormatterOptions options)
        {
            if (configuration["IncludeScopes"] is string includeScopes)
            {
                options.IncludeScopes = bool.Parse(includeScopes);
            }

            if (configuration["TimestampFormat"] is string timestampFormat)
            {
                options.TimestampFormat = timestampFormat;
            }

            if (configuration["UseUtcTimestamp"] is string useUtcTimestamp)
            {
                options.UseUtcTimestamp = bool.Parse(useUtcTimestamp);
            }
        }
    }
}
