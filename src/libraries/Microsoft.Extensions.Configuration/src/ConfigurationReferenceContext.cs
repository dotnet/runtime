// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// The input a reference recogniser sees: the raw value to recognise and the upstream providers it may
    /// inspect to decide which keys to name. Providers are exposed rather than an <see cref="IConfiguration"/> so
    /// recognisers probe keys directly (<see cref="IConfigurationProvider.TryGet"/>,
    /// <see cref="IConfigurationProvider.GetChildKeys"/>) without the section-tree allocations and reload-token
    /// semantics of the higher-level surface. Inspection is raw: the reference rules still gate any key named.
    /// </summary>
    public readonly struct ConfigurationReferenceContext
    {
        internal ConfigurationReferenceContext(string key, string value, IReadOnlyList<IConfigurationProvider> providers)
        {
            Key = key;
            Value = value;
            Providers = providers;
        }

        /// <summary>Gets the configuration key whose value is being recognised.</summary>
        public string Key { get; }

        /// <summary>Gets the raw configuration value to recognise.</summary>
        public string Value { get; }

        /// <summary>Gets the upstream providers, in declaration order, the reference provider resolves against.</summary>
        public IReadOnlyList<IConfigurationProvider> Providers { get; }
    }
}
