// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Extension methods for configuration classes./>.
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Adds a new configuration source.
        /// </summary>
        /// <param name="builder">The builder to add to.</param>
        /// <param name="configureSource">Configures the source secrets.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder Add<TSource>(this IConfigurationBuilder builder, Action<TSource> configureSource) where TSource : IConfigurationSource, new()
        {
            var source = new TSource();
            configureSource?.Invoke(source);
            return builder.Add(source);
        }

        /// <summary>
        /// Shorthand for GetSection("ConnectionStrings")[name].
        /// </summary>
        /// <param name="configuration">The configuration to enumerate.</param>
        /// <param name="name">The connection string key.</param>
        /// <returns>The connection string.</returns>
        public static string GetConnectionString(this IConfiguration configuration, string name)
        {
            return configuration?.GetSection("ConnectionStrings")?[name];
        }

        /// <summary>
        /// Get the enumeration of key value pairs within the <see cref="IConfiguration" />
        /// </summary>
        /// <param name="configuration">The configuration to enumerate.</param>
        /// <returns>An enumeration of key value pairs.</returns>
        public static IEnumerable<KeyValuePair<string, string>> AsEnumerable(this IConfiguration configuration) => configuration.AsEnumerable(makePathsRelative: false);

        /// <summary>
        /// Get the enumeration of key value pairs within the <see cref="IConfiguration" />
        /// </summary>
        /// <param name="configuration">The configuration to enumerate.</param>
        /// <param name="makePathsRelative">If true, the child keys returned will have the current configuration's Path trimmed from the front.</param>
        /// <returns>An enumeration of key value pairs.</returns>
        public static IEnumerable<KeyValuePair<string, string>> AsEnumerable(this IConfiguration configuration, bool makePathsRelative)
        {
            var stack = new Stack<IConfiguration>();
            stack.Push(configuration);
            var rootSection = configuration as IConfigurationSection;
            int prefixLength = (makePathsRelative && rootSection != null) ? rootSection.Path.Length + 1 : 0;
            while (stack.Count > 0)
            {
                IConfiguration config = stack.Pop();
                // Don't include the sections value if we are removing paths, since it will be an empty key
                if (config is IConfigurationSection section && (!makePathsRelative || config != configuration))
                {
                    yield return new KeyValuePair<string, string>(section.Path.Substring(prefixLength), section.Value);
                }
                foreach (IConfigurationSection child in config.GetChildren())
                {
                    stack.Push(child);
                }
            }
        }

        /// <summary>
        /// Determines whether the section has a <see cref="IConfigurationSection.Value"/> or has children
        /// </summary>
        /// <param name="section">The section to enumerate.</param>
        /// <returns><see langword="true" /> if the section has values or children; otherwise, <see langword="false" />.</returns>
        public static bool Exists(this IConfigurationSection section)
        {
            if (section == null)
            {
                return false;
            }
            return section.Value != null || section.GetChildren().Any();
        }

        /// <summary>
        /// Gets a configuration sub-section with the specified key.
        /// </summary>
        /// <param name="configuration">The configuration to enumerate.</param>
        /// <param name="key">The key of the configuration section.</param>
        /// <returns>The <see cref="IConfigurationSection"/>.</returns>
        /// <remarks>
        ///     If no matching sub-section is found with the specified key, an exception is raised.
        /// </remarks>
        /// <exception cref="System.InvalidOperationException">There is no section with key <paramref name="key"/>.</exception>
        public static IConfigurationSection GetRequiredSection(this IConfiguration configuration, string key)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            IConfigurationSection section = configuration.GetSection(key);
            if (section.Exists())
            {
                return section;
            }

            throw new InvalidOperationException(SR.Format(SR.InvalidSectionName, key));
        }
    }
}
