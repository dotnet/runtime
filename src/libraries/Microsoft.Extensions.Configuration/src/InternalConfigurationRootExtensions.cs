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

            // todo: steve: if each provider exposes their delimiter, we can then use it

            // todo: ConfigurationProvider now does expose 'GetDelimiter' (not IConfigurationProvider as that would be a breaking change)
            // so, we need to get not just the child key strings, but also the delimiter used by the provider.
            // then, when we get the 'ConfigurationSection', we can provide that with the correct delimiter.
            // We can likely then get rid of all the overloads (Load, Stream etc.) that take the 'separator' string.

            Dictionary<string, List<string>> l = new Dictionary<string, List<string>>();

            HashSet<string> delims = new HashSet<string>();

            foreach (var provider in providers)
            {
                string delim = (provider as ConfigurationProvider)?.GetDelimiter() ?? ":";
                delims.Add(delim);

                if (!l.ContainsKey(delim))
                {
                    l[delim] = new List<string>();
                }

                List<string> strings = l[delim];
                l[delim] = provider.GetChildKeys(strings, path).Distinct().ToList();
            }

            List<IConfigurationSection> cs = new List<IConfigurationSection>();

            foreach (string delim in delims)
            {
                foreach (string key in l[delim])
                {
                    cs.Add(root.GetSection(path == null ? key : ConfigurationPath.CombineWith(delim, path, key)));
                }
            }

            IEnumerable<IConfigurationSection> children = providers
                .Aggregate(Enumerable.Empty<string>(), (seed, source) => source.GetChildKeys(seed, path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(key => root.GetSection(path == null ? key : ConfigurationPath.CombineWith("`", path, key)));

            if (reference is null)
            {
                return cs;
            }
            else
            {
                // Eagerly evaluate the IEnumerable before releasing the reference so we don't allow iteration over disposed providers.
                return cs.ToList(); // todo: this is now, by default, eagerly evaluated - need to think about this...
            }

            //if (reference is null)
            //{
            //    return children;
            //}
            //else
            //{
            //    // Eagerly evaluate the IEnumerable before releasing the reference so we don't allow iteration over disposed providers.
            //    return children.ToList();
            //}
        }
    }
}
