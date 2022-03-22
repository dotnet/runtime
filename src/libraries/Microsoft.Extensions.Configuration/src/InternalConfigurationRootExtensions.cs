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

            Dictionary<string, List<string>> lookupOfSeparatorToKeys = new Dictionary<string, List<string>>();

            HashSet<string> separators = new HashSet<string>();

            foreach (var provider in providers)
            {
                string delim = (provider as ConfigurationProvider)?.GetDelimiter() ?? ":";
                separators.Add(delim);

                if (!lookupOfSeparatorToKeys.ContainsKey(delim))
                {
                    lookupOfSeparatorToKeys[delim] = new List<string>();
                }

                List<string> previousKeys = lookupOfSeparatorToKeys[delim];
                lookupOfSeparatorToKeys[delim] = provider.GetChildKeys(previousKeys, path).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            }

            List<IConfigurationSection> configurationSources = new List<IConfigurationSection>();

            foreach (string eachSeparator in separators)
            {
                foreach (string key in lookupOfSeparatorToKeys[eachSeparator])
                {
                    configurationSources.Add(root.GetSection(path == null ? key : ConfigurationPath.CombineWith(eachSeparator, path, key)));
                }
            }

            if (reference is null)
            {
                return configurationSources;
            }
            else
            {
                // Eagerly evaluate the IEnumerable before releasing the reference so we don't allow iteration over disposed providers.
                return configurationSources.ToList(); // todo: this is now, by default, eagerly evaluated - need to think about this...
            }
        }
    }
}
