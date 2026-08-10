// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Diagnostics.Tracing
{
    internal sealed class DefaultActivityListenerConfigurationFactory : ActivityListenerConfigurationFactory
    {
        private readonly IEnumerable<TracingConfiguration> _configurations;

        public DefaultActivityListenerConfigurationFactory(IEnumerable<TracingConfiguration> configurations)
        {
            _configurations = configurations ?? throw new ArgumentNullException(nameof(configurations));
        }

        public override IConfiguration GetConfiguration(string listenerName)
        {
            ArgumentNullException.ThrowIfNull(listenerName);

            var configurationBuilder = new ConfigurationBuilder();
            foreach (TracingConfiguration configuration in _configurations)
            {
                IConfigurationSection section = configuration.Configuration.GetSection(listenerName);
                configurationBuilder.AddConfiguration(section);
            }

            return configurationBuilder.Build();
        }
    }
}
