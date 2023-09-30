// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Diagnostics.Metrics.Configuration
{
    internal sealed class MetricListenerConfigurationFactory : IMetricListenerConfigurationFactory
    {
        private readonly IEnumerable<MetricsConfiguration> _configurations;

        public MetricListenerConfigurationFactory(IEnumerable<MetricsConfiguration> configurations)
        {
            _configurations = configurations ?? throw new ArgumentNullException(nameof(configurations));
        }

        public IConfiguration GetConfiguration(string listenerName)
        {
            ThrowHelper.ThrowIfNull(listenerName);

            var configurationBuilder = new ConfigurationBuilder();
            foreach (MetricsConfiguration configuration in _configurations)
            {
                var section = configuration.Configuration.GetSection(listenerName);
                configurationBuilder.AddConfiguration(section);
            }
            return configurationBuilder.Build();
        }
    }
}
