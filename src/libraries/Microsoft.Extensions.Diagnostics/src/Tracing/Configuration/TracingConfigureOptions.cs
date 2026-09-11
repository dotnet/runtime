// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Tracing
{
    internal sealed class TracingConfigureOptions : IConfigureOptions<TracingOptions>
    {
        private const string EnabledTracingKey = "EnabledTracing";
        private const string EnabledGlobalTracingKey = "EnabledGlobalTracing";
        private const string EnabledLocalTracingKey = "EnabledLocalTracing";
        private const string DefaultKey = "Default";
        private readonly IConfiguration _configuration;

        public TracingConfigureOptions(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public void Configure(TracingOptions options) => LoadConfig(options);

        private void LoadConfig(TracingOptions options)
        {
            foreach (var configurationSection in _configuration.GetChildren())
            {
                if (configurationSection.Key.Equals(EnabledTracingKey, StringComparison.OrdinalIgnoreCase))
                {
                    LoadActivitySourceRules(options, configurationSection, ActivitySourceScopes.Global | ActivitySourceScopes.Local, listenerName: null);
                }
                else if (configurationSection.Key.Equals(EnabledGlobalTracingKey, StringComparison.OrdinalIgnoreCase))
                {
                    LoadActivitySourceRules(options, configurationSection, ActivitySourceScopes.Global, listenerName: null);
                }
                else if (configurationSection.Key.Equals(EnabledLocalTracingKey, StringComparison.OrdinalIgnoreCase))
                {
                    LoadActivitySourceRules(options, configurationSection, ActivitySourceScopes.Local, listenerName: null);
                }
                else
                {
                    var listenerName = configurationSection.Key;
                    var enabledTracingSection = configurationSection.GetSection(EnabledTracingKey);
                    if (enabledTracingSection.Exists())
                    {
                        LoadActivitySourceRules(options, enabledTracingSection, ActivitySourceScopes.Global | ActivitySourceScopes.Local, listenerName);
                    }

                    var enabledGlobalTracingSection = configurationSection.GetSection(EnabledGlobalTracingKey);
                    if (enabledGlobalTracingSection.Exists())
                    {
                        LoadActivitySourceRules(options, enabledGlobalTracingSection, ActivitySourceScopes.Global, listenerName);
                    }

                    var enabledLocalTracingSection = configurationSection.GetSection(EnabledLocalTracingKey);
                    if (enabledLocalTracingSection.Exists())
                    {
                        LoadActivitySourceRules(options, enabledLocalTracingSection, ActivitySourceScopes.Local, listenerName);
                    }
                }
            }
        }

        internal static void LoadActivitySourceRules(TracingOptions options, IConfigurationSection configurationSection, ActivitySourceScopes scopes, string? listenerName)
        {
            foreach (var activitySourceSection in configurationSection.GetChildren())
            {
                if (activitySourceSection.GetChildren().Any())
                {
                    LoadActivityRules(options, activitySourceSection, scopes, listenerName);
                }
                else if (TryGetEnabledValue(activitySourceSection, out var enabled))
                {
                    var sourceName = activitySourceSection.Key;
                    if (string.Equals(DefaultKey, sourceName, StringComparison.OrdinalIgnoreCase))
                    {
                        sourceName = null;
                    }

                    options.Rules.Add(new TracingRule(sourceName, operationName: null, listenerName, scopes, enabled));
                }
            }
        }

        internal static void LoadActivityRules(TracingOptions options, IConfigurationSection activitySourceSection, ActivitySourceScopes scopes, string? listenerName)
        {
            foreach (var activityPair in activitySourceSection.AsEnumerable(makePathsRelative: true))
            {
                if (bool.TryParse(activityPair.Value, out var enabled))
                {
                    var operationName = activityPair.Key;
                    if (string.Equals(DefaultKey, operationName, StringComparison.OrdinalIgnoreCase))
                    {
                        operationName = null;
                    }

                    options.Rules.Add(new TracingRule(activitySourceSection.Key, operationName, listenerName, scopes, enabled));
                }
            }
        }

        private static bool TryGetEnabledValue(IConfigurationSection activitySourceSection, out bool enabled)
        {
            if (bool.TryParse(activitySourceSection.Value, out enabled))
            {
                return true;
            }

            enabled = default;
            return false;
        }
    }
}
