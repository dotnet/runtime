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
            // common cases Providers is IList<IConfigurationProvider> in ConfigurationRoot
            IList<IConfigurationProvider> providers = root.Providers is IList<IConfigurationProvider> list
                ? list
                : root.Providers.ToList();

            // ensure looping in the reverse order
            for (int i = providers.Count - 1; i >= 0; i--)
            {
                IConfigurationProvider provider = providers[i];

                try
                {
                    if (provider.TryGet(key, out value))
                    {
                        return true;
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Skip disposed providers to avoid exceptions during access.
                    // This is especially relevant for cases like ConfigurationManager,
                    // which implements IConfigurationRoot and may dispose providers
                    // if configuration sources are concurrently modified. A new collection
                    // is created in this case, so it's still safe to iterate over it.
                    //
                    // If we want to avoid this possible exception altogether, we could update
                    // ConfigurationSection.TryGetValue to be virtual and have ConfigurationManager
                    // implement it with reference counting like it does for the indexer.
                }
            }

            value = null;
            return false;
        }

    }
}
