// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Provides a chained implementation of <see cref="IConfigurationProvider"/>.
    /// </summary>
    public class ChainedConfigurationProvider : IConfigurationProvider, IDisposable
    {
        private readonly IConfiguration _config;
        private readonly bool _shouldDisposeConfig;
        private bool _initialLoadCompleted;

        /// <summary>
        /// Initializes a new instance from the source configuration.
        /// </summary>
        /// <param name="source">The source configuration.</param>
        public ChainedConfigurationProvider(ChainedConfigurationSource source)
        {
            ArgumentNullException.ThrowIfNull(source);

            _config = source.Configuration ?? throw new ArgumentException(SR.Format(SR.InvalidNullArgument, "source.Configuration"), nameof(source));
            _shouldDisposeConfig = source.ShouldDisposeConfiguration;
        }

        /// <summary>
        /// Gets the chained configuration.
        /// </summary>
        public IConfiguration Configuration => _config;

        /// <summary>
        /// Tries to get a configuration value for the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">When this method returns, contains the value.</param>
        /// <returns><see langword="true"/> if a value for the specified key was found, otherwise <see langword="false"/>.</returns>
        public bool TryGet(string key, out string? value)
        {
            value = _config[key];
            return !string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// Sets a configuration value for the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Set(string key, string? value) => _config[key] = value;

        /// <summary>
        /// Returns a change token if this provider supports change tracking; otherwise returns <see langword="null" />.
        /// </summary>
        /// <returns>The change token.</returns>
        public IChangeToken GetReloadToken() => _config.GetReloadToken();

        /// <summary>
        /// Loads configuration values from the source represented by this <see cref="IConfigurationProvider"/>.
        /// </summary>
        public void Load()
        {
            if (!_initialLoadCompleted)
            {
                // The initial load is a no-op since the chained configuration is expected to be already loaded by the
                // time it is used as a source for another configuration. This way we avoid unnecessary change notifications.
                _initialLoadCompleted = true;
                return;
            }

            if (_config is IConfigurationRoot root)
            {
                foreach (IConfigurationProvider provider in root.Providers)
                {
                    provider.Load();
                }
            }
        }

        /// <summary>
        /// Returns the immediate descendant configuration keys for a given parent path based on the data of this
        /// <see cref="IConfigurationProvider"/> and the set of keys returned by all the preceding
        /// <see cref="IConfigurationProvider"/> objects.
        /// </summary>
        /// <param name="earlierKeys">The child keys returned by the preceding providers for the same parent path.</param>
        /// <param name="parentPath">The parent path.</param>
        /// <returns>The child keys.</returns>
        public IEnumerable<string> GetChildKeys(
            IEnumerable<string> earlierKeys,
            string? parentPath)
        {
            SortedChildKeys accumulator = earlierKeys is SortedChildKeys existing ? existing : new(earlierKeys);
            if (_config is IConfigurationRoot root)
            {
                // Aggregate the chained root's own child keys from an empty seed, then merge them into the outer
                // accumulator. Providers inside the chained root therefore never observe the outer providers' keys and
                // cannot filter, reorder, or drop them, preserving the chaining boundary that existed before.
                accumulator.UnionWith(root.GetChildKeysImplementation(parentPath));
                return accumulator;
            }

            IConfiguration section = parentPath == null ? _config : _config.GetSection(parentPath);
            foreach (IConfigurationSection child in section.GetChildren())
            {
                accumulator.AddSegment(child.Key, 0, child.Key.Length);
            }

            return accumulator;
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
