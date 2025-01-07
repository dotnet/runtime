// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Metrics.Configuration
{
    internal sealed class MetricsConfigureOptions : IConfigureOptions<MetricsOptions>
    {
        private const string EnabledMetricsKey = "EnabledMetrics";
        private const string EnabledGlobalMetricsKey = "EnabledGlobalMetrics";
        private const string EnabledLocalMetricsKey = "EnabledLocalMetrics";
        private const string DefaultKey = "Default";
        private readonly IConfiguration _configuration;

        public MetricsConfigureOptions(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public void Configure(MetricsOptions options) => LoadConfig(options);

        private void LoadConfig(MetricsOptions options)
        {
            foreach (var configurationSection in _configuration.GetChildren())
            {
                if (configurationSection.Key.Equals(EnabledMetricsKey, StringComparison.OrdinalIgnoreCase))
                {
                    // Load listener defaults
                    LoadMeterRules(options, configurationSection, MeterScope.Global | MeterScope.Local, null);
                }
                else if (configurationSection.Key.Equals(EnabledGlobalMetricsKey, StringComparison.OrdinalIgnoreCase))
                {
                    // Load global listener defaults
                    LoadMeterRules(options, configurationSection, MeterScope.Global, null);
                }
                else if (configurationSection.Key.Equals(EnabledLocalMetricsKey, StringComparison.OrdinalIgnoreCase))
                {
                    // Load local listener defaults
                    LoadMeterRules(options, configurationSection, MeterScope.Local, null);
                }
                else
                {
                    // Load listener specific rules
                    var listenerName = configurationSection.Key;
                    var enabledMetricsSection = configurationSection.GetSection(EnabledMetricsKey);
                    if (enabledMetricsSection != null)
                    {
                        LoadMeterRules(options, enabledMetricsSection, MeterScope.Global | MeterScope.Local, listenerName);
                    }
                    var enabledGlobalMetricsSection = configurationSection.GetSection(EnabledGlobalMetricsKey);
                    if (enabledGlobalMetricsSection != null)
                    {
                        LoadMeterRules(options, enabledGlobalMetricsSection, MeterScope.Global, listenerName);
                    }
                    var enabledLocalMetricsSection = configurationSection.GetSection(EnabledLocalMetricsKey);
                    if (enabledLocalMetricsSection != null)
                    {
                        LoadMeterRules(options, enabledLocalMetricsSection, MeterScope.Local, listenerName);
                    }
                }
            }
        }

        // Internal for testing
        internal static void LoadMeterRules(MetricsOptions options, IConfigurationSection configurationSection, MeterScope scopes, string? listenerName)
        {
            foreach (var meterSection in configurationSection.GetChildren())
            {
                // Is the meter a simple on/off bool or is it an object listing individual instruments?
                if (meterSection.GetChildren().Any())
                {
                    // It's an object, load individual instruments
                    LoadInstrumentRules(options, meterSection, scopes, listenerName);
                }
                // Otherwise, it's a simple bool
                else if (bool.TryParse(meterSection.Value, out var meterEnabled))
                {
                    var meterName = meterSection.Key;
                    if (string.Equals(DefaultKey, meterName, StringComparison.OrdinalIgnoreCase))
                    {
                        // "Default" is a special key that applies to all meters
                        meterName = null;
                    }
                    // Simple bool, enable/disable all instruments for this meter
                    options.Rules.Add(new InstrumentRule(meterName, instrumentName: null, listenerName, scopes, meterEnabled));
                }
            }
        }

        // Internal for testing
        internal static void LoadInstrumentRules(MetricsOptions options, IConfigurationSection meterSection, MeterScope scopes, string? listenerName)
        {
            foreach (var instrumentPair in meterSection.AsEnumerable(makePathsRelative: true))
            {
                if (bool.TryParse(instrumentPair.Value, out var instrumentEnabled))
                {
                    var instrumentName = instrumentPair.Key;
                    if (string.Equals(DefaultKey, instrumentName, StringComparison.OrdinalIgnoreCase))
                    {
                        // "Default" is a special key that applies to all instruments
                        instrumentName = null;
                    }
                    // Simple bool, enable/disable all instruments for this meter
                    options.Rules.Add(new InstrumentRule(meterSection.Key, instrumentName, listenerName, scopes, instrumentEnabled));
                }
            }
        }
    }
}
