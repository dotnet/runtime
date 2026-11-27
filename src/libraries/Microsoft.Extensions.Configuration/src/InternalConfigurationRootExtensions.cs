// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Extensions method for <see cref="IConfigurationRoot"/>
    /// </summary>
    internal static class InternalConfigurationRootExtensions
    {
        /// <summary>
        /// Gets the immediate children sub-sections of configuration root based on key.
        /// </summary>
        /// <param name="root">Configuration from which to retrieve sub-sections.</param>
        /// <param name="path">Key of a section of which children to retrieve.</param>
        /// <returns>Immediate children sub-sections of section specified by key.</returns>
        internal static IEnumerable<IConfigurationSection> GetChildrenImplementation(this IConfigurationRoot root, string? path)
        {
            using ReferenceCountedProviders? reference = (root as ConfigurationManager)?.GetProvidersReference();
            IEnumerable<IConfigurationProvider> providers = reference?.Providers ?? root.Providers;

            IEnumerable<IConfigurationSection> children = providers
                .Aggregate(Enumerable.Empty<string>(),
                    (seed, source) => source.GetChildKeys(seed, path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(key => root.GetSection(path == null ? key : path + ConfigurationPath.KeyDelimiter + key));

            if (reference is null)
            {
                return children;
            }
            else
            {
                // Eagerly evaluate the IEnumerable before releasing the reference so we don't allow iteration over disposed providers.
                return children.ToList();
            }
        }

        internal static bool TryGetConfiguration(this IConfigurationRoot root, string key, out string? value)
        {
            if (root is ConfigurationManager manager)
            {
                // Hold the reference for the whole read: resolving a reference reads several keys, and they all have to
                // come from the same provider generation.
                using ReferenceCountedProviders reference = manager.GetProvidersReference();
                return ConfigurationRoot.TryGetConfiguration(reference.Providers, key, out value);
            }

            IEnumerable<IConfigurationProvider> providers = root.Providers;
            return ConfigurationRoot.TryGetConfiguration(providers as IList<IConfigurationProvider> ?? providers.ToList(), key, out value);
        }
    }
}
