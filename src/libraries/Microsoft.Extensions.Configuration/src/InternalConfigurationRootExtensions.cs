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
            var optimizedProviders = new List<ConfigurationProvider>();
            bool allProvidersAreOptimized = true;
            foreach (IConfigurationProvider provider in providers)
            {
                // If a IConfigurationProvider inherit from ConfigurationProvider, but override the implementation GetChildKeys
                // To make sure we get the expected keys, we should honor the override and go the legacy path.
                Type type = provider.GetType();
                if (provider is not ConfigurationProvider optimizedProvider
                    || type.GetMethod(nameof(provider.GetChildKeys))?.DeclaringType == type)
                {
                    allProvidersAreOptimized = false;
                    break;
                }

                optimizedProviders.Add(optimizedProvider);
            }

            IEnumerable<string> allKeys;
            if (allProvidersAreOptimized)
            {
                // Optimized implementation: each provider return keys via yield return
                // No re-create the accumulated list, No sort,
                // Note P = number of providers, K = max number of keys per provider,
                // The time complexity is O(K*P*log(K*P))
                allKeys = optimizedProviders
                    .SelectMany(p => p.GetChildKeysInternal(path))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(key => key, ConfigurationKeyComparer.Instance);
            }
            else
            {
                // Legacy implementation: accumulate keys by passing the partial keys from one provider to the next,
                // If each provider re-create the accumulated list and sort inside,
                // the time complexity is O(K*P^2*log(K*P))
                allKeys = providers
                    .Aggregate(Enumerable.Empty<string>(),
                        (seed, source) => source.GetChildKeys(seed, path))
                    .Distinct(StringComparer.OrdinalIgnoreCase);
            }

            IEnumerable<IConfigurationSection> children = allKeys
                .Select(key => root.GetSection(path == null ? key : ConfigurationPath.Combine(path, key)));
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
    }
}
