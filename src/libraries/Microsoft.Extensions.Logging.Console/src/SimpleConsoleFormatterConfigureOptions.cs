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
    /// Configures a SimpleConsoleFormatterOptions object from an IConfiguration.
    /// </summary>
    /// <remarks>
    /// Doesn't use ConfigurationBinder in order to allow ConfigurationBinder, and all its dependencies,
    /// to be trimmed. This improves app size and startup.
    /// </remarks>
    [UnsupportedOSPlatform("browser")]
    internal sealed class SimpleConsoleFormatterConfigureOptions : IConfigureOptions<SimpleConsoleFormatterOptions>
    {
        private readonly IConfiguration _configuration;

        public SimpleConsoleFormatterConfigureOptions(ILoggerProviderConfiguration<ConsoleLoggerProvider> providerConfiguration)
        {
            _configuration = providerConfiguration.GetFormatterOptionsSection();
        }

        public void Configure(SimpleConsoleFormatterOptions options)
        {
            ConsoleFormatterConfigureOptions.Bind(_configuration, options);

            if (_configuration["ColorBehavior"] is string colorBehavior)
            {
                options.ColorBehavior = ConsoleLoggerConfigureOptions.ParseEnum<LoggerColorBehavior>(colorBehavior);
            }

            if (_configuration["SingleLine"] is string singleLine)
            {
                options.SingleLine = bool.Parse(singleLine);
            }
        }
    }
}
