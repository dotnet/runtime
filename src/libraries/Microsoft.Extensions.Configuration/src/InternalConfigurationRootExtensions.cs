// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            IList<IConfigurationProvider> providers = AsList(reference?.Providers ?? root.Providers);

            IEnumerable<string> keys = ConfigurationEngine.Default.GetChildKeys(providers, path);
            if (reference is not null)
            {
                keys = keys.ToList();
            }

            return keys.Select(key => root.GetSection(path == null ? key : path + ConfigurationPath.KeyDelimiter + key));
        }

        internal static bool TryGetConfiguration(this IConfigurationRoot root, string key, out string? value)
        {
            if (root is ConfigurationManager manager)
            {
                // Hold the reference for the whole read: resolving a reference reads several keys, and they all have to
                // come from the same provider generation.
                using ReferenceCountedProviders reference = manager.GetProvidersReference();
                return ConfigurationEngine.Default.Get(reference.Providers, key, out value, out _);
            }

            return ConfigurationEngine.Default.Get(AsList(root.Providers), key, out value, out _);
        }

        // Providers is IList<IConfigurationProvider> for both of the roots in this library, and for the pinned
        // generation a ConfigurationManager hands out.
        private static IList<IConfigurationProvider> AsList(IEnumerable<IConfigurationProvider> providers) =>
            providers as IList<IConfigurationProvider> ?? providers.ToList();
    }
}
