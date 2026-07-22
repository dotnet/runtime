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
            // For ConfigurationManager the engine is obtained from the pinned provider generation, so the engine and the
            // providers it resolves against are always the same generation. A plain ConfigurationRoot exposes its engine
            // directly (its provider list is fixed).
            using ReferenceCountedProviders? reference = (root as ConfigurationManager)?.GetProvidersReference();
            ReferenceEngine? engine = reference?.ReferenceEngine ?? (root as ConfigurationRoot)?.ReferenceEngine;

            IEnumerable<string> childKeys;
            if (ReferenceEngine.Disabled || engine is null)
            {
                // No engine (references disabled, or a third-party root): enumerate children the plain way, with nothing
                // to resolve or merge.
                IEnumerable<IConfigurationProvider> providers = reference?.Providers ?? root.Providers;
                childKeys = providers
                    .Aggregate(Enumerable.Empty<string>(), (seed, source) => source.GetChildKeys(seed, path))
                    .Distinct(StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                // References merge into the children: a redirected section lists its target's children unioned with any
                // keys a higher provider defines under the reference, at every hop of the chain.
                childKeys = engine.ChildKeys(path);
            }

            IEnumerable<IConfigurationSection> children = childKeys
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
            // For ConfigurationManager, take a counted reference and use the engine from that pinned generation
            // so a concurrent source mutation can neither dispose the providers mid-read nor pair them with a different
            // generation's index. The resolved key is then read like any other.
            if (!ReferenceEngine.Disabled)
            {
                if (root is ConfigurationManager cm)
                {
                    using ReferenceCountedProviders reference = cm.GetProvidersReference();
                    return reference.ReferenceEngine!.TryRead(key, out value);
                }
                else if (root is ConfigurationRoot cr)
                {
                    return cr.ReferenceEngine!.TryRead(key, out value);
                }
            }

            // Plain path: references globally disabled, no opted-in provider, or a third-party root implementation.
            // Commonly Providers is already IList<IConfigurationProvider> in ConfigurationRoot.
            IList<IConfigurationProvider> providers = root.Providers is IList<IConfigurationProvider> list
                ? list
                : root.Providers.ToList();
            return TryGet(providers, key, out value);
        }

        private static bool TryGet(IList<IConfigurationProvider> providers, string key, out string? value)
        {
            for (int i = providers.Count - 1; i >= 0; i--)
            {
                try
                {
                    if (providers[i].TryGet(key, out value))
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
