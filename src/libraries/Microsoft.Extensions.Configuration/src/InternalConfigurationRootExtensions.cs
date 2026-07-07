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

            // The aggregated child keys, already ordered and de-duplicated, projected into sections below.
            SortedChildKeys? keys = AggregateChildKeys(providers, path);
            if (keys is null || keys.Count == 0)
            {
                return Array.Empty<IConfigurationSection>();
            }

            if (reference is null)
            {
                // Deferred projection via a static local iterator (no captured closure/delegate allocation).
                return Project(root, path, keys);
            }

            // ConfigurationManager: eagerly materialize before releasing the reference so we don't iterate over
            // disposed providers. The child count is known, so the list is sized exactly.
            var children = new List<IConfigurationSection>(keys.Count);
            foreach (string key in keys)
            {
                children.Add(CreateSection(root, path, key));
            }
            return children;

            static IEnumerable<IConfigurationSection> Project(IConfigurationRoot root, string? path, IEnumerable<string> keys)
            {
                foreach (string key in keys)
                {
                    yield return CreateSection(root, path, key);
                }
            }
        }

        private static IConfigurationSection CreateSection(IConfigurationRoot root, string? path, string key)
        {
            string fullPath = path is null ? key : path + ConfigurationPath.KeyDelimiter + key;
            return root.GetSection(fullPath);
        }

        /// <summary>
        /// Gets the immediate child keys for a path, aggregated and de-duplicated across all providers, without
        /// projecting them into <see cref="IConfigurationSection"/> instances. This lets a
        /// <see cref="ChainedConfigurationProvider"/> read the keys of a chained <see cref="IConfigurationRoot"/>
        /// without allocating a section per child.
        /// </summary>
        internal static IEnumerable<string> GetChildKeysImplementation(this IConfigurationRoot root, string? path, SortedChildKeys? seed = null)
        {
            using ReferenceCountedProviders? reference = (root as ConfigurationManager)?.GetProvidersReference();
            IEnumerable<IConfigurationProvider> providers = reference?.Providers ?? root.Providers;
            return (IEnumerable<string>?)AggregateChildKeys(providers, path, seed) ?? Array.Empty<string>();
        }

        private static SortedChildKeys? AggregateChildKeys(IEnumerable<IConfigurationProvider> providers, string? path, SortedChildKeys? seed = null)
        {
            if (providers is List<IConfigurationProvider> list)
            {
                int count = list.Count;
                if (count == 0)
                {
                    return null;
                }

                SortedChildKeys accumulator = seed ?? new SortedChildKeys();
                for (int i = 0; i < count; i++)
                {
                    ProcessProvider(list[i], accumulator, path);
                }

                return accumulator;

            }
            else
            {
                SortedChildKeys accumulator = seed ?? new SortedChildKeys();
                foreach (IConfigurationProvider provider in providers)
                {
                    ProcessProvider(provider, accumulator, path);
                }

                return accumulator;
            }
        }

        private static void ProcessProvider(IConfigurationProvider provider, SortedChildKeys accumulator, string? path)
        {
            IEnumerable<string> returned = provider.GetChildKeys(accumulator, path);
            if (!ReferenceEquals(returned, accumulator))
            {
                accumulator.Overwrite(returned);
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
