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
    /// Applies configuration to <see cref="EventLogSettings" /> via <see cref="EventLogSettings.Configure(IConfiguration)" />,
    /// which uses the ConfigurationBinder APIs under the hood.
    /// </remarks>
    internal sealed class EventLogConfigureSettings : IConfigureOptions<EventLogSettings>
    {
        private readonly IConfiguration _configuration;

        [UnsupportedOSPlatform("browser")]
        public EventLogConfigureSettings(ILoggerProviderConfiguration<EventLogLoggerProvider> providerConfiguration)
        {
            _configuration = providerConfiguration.Configuration;
        }

        public void Configure(EventLogSettings options) => options.Configure(_configuration);
    }
}
