// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Chained implementation of <see cref="IConfigurationProvider"/>
    /// </summary>
    public class ChainedConfigurationProvider : IConfigurationProvider, IDisposable
    {
        private readonly IConfiguration _config;
        private readonly bool _shouldDisposeConfig;

        /// <summary>
        /// Initialize a new instance from the source configuration.
        /// </summary>
        /// <param name="source">The source configuration.</param>
        public ChainedConfigurationProvider(ChainedConfigurationSource source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (source.Configuration == null)
            {
                throw new ArgumentNullException(nameof(source.Configuration));
            }

            _config = source.Configuration;
            _shouldDisposeConfig = source.ShouldDisposeConfiguration;
        }

        /// <summary>
        /// Tries to get a configuration value for the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>True</c> if a value for the specified key was found, otherwise <c>false</c>.</returns>
        public bool TryGet(string key, out string value)
        {
            value = _config[key];
            return !string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// Sets a configuration value for the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Set(string key, string value) => _config[key] = value;

        /// <summary>
        /// Returns a change token if this provider supports change tracking, null otherwise.
        /// </summary>
        /// <returns></returns>
        public IChangeToken GetReloadToken() => _config.GetReloadToken();

        /// <summary>
        /// Loads configuration values from the source represented by this <see cref="IConfigurationProvider"/>.
        /// </summary>
        public void Load() { }

        /// <summary>
        /// Returns the immediate descendant configuration keys for a given parent path based on this
        /// <see cref="IConfigurationProvider"/>'s data and the set of keys returned by all the preceding
        /// <see cref="IConfigurationProvider"/>s.
        /// </summary>
        /// <param name="earlierKeys">The child keys returned by the preceding providers for the same parent path.</param>
        /// <param name="parentPath">The parent path.</param>
        /// <returns>The child keys.</returns>
        public IEnumerable<string> GetChildKeys(
            IEnumerable<string> earlierKeys,
            string parentPath)
        {
            var section = parentPath == null ? _config : _config.GetSection(parentPath);
            var children = section.GetChildren();
            var keys = new List<string>();
            keys.AddRange(children.Select(c => c.Key));
            return keys.Concat(earlierKeys)
                .OrderBy(k => k, ConfigurationKeyComparer.Instance);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_shouldDisposeConfig)
            {
                (_config as IDisposable)?.Dispose();
            }
        }
    }
}
