// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Configures an EventLogSettings object from an IConfiguration.
    /// </summary>
    /// <remarks>
    /// Uses source-generated configuration binding to allow ConfigurationBinder, and all its dependencies,
    /// to be trimmed. This improves app size and startup.
    /// </remarks>
    internal sealed class EventLogConfigureOptions : IConfigureOptions<EventLogSettings>
    {
        private readonly IConfiguration _configuration;

        [UnsupportedOSPlatform("browser")]
        public EventLogConfigureOptions(ILoggerProviderConfiguration<EventLogLoggerProvider> providerConfiguration)
        {
            _configuration = providerConfiguration.Configuration;
        }

        public void Configure(EventLogSettings options) => _configuration.Bind(options);
    }
}
