// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Represents a section of application configuration values.
    /// </summary>
    [DebuggerDisplay("{DebuggerToString(),nq}")]
    [DebuggerTypeProxy(typeof(ConfigurationSectionDebugView))]
    public class ConfigurationSection : IConfigurationSection
    {
        private readonly IConfigurationRoot _root;
        private readonly string _path;
        private string? _key;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="root">The configuration root.</param>
        /// <param name="path">The path to this section.</param>
        public ConfigurationSection(IConfigurationRoot root, string path)
        {
            ThrowHelper.ThrowIfNull(root);
            ThrowHelper.ThrowIfNull(path);

            _root = root;
            _path = path;
        }

        /// <summary>
        /// Gets the full path to this section from the <see cref="IConfigurationRoot"/>.
        /// </summary>
        public string Path => _path;

        /// <summary>
        /// Gets the key this section occupies in its parent.
        /// </summary>
        public string Key =>
            // Key is calculated lazily as last portion of Path
            _key ??= ConfigurationPath.GetSectionKey(_path);

        /// <summary>
        /// Gets or sets the section value.
        /// </summary>
        public string? Value
        {
            get
            {
                return _root[Path];
            }
            set
            {
                _root[Path] = value;
            }
        }

        /// <summary>
        /// Gets or sets the value corresponding to a configuration key.
        /// </summary>
        /// <param name="key">The configuration key.</param>
        /// <returns>The configuration value.</returns>
        public string? this[string key]
        {
            get
            {
                return _root[ConfigurationPath.Combine(Path, key)];
            }
            set
            {
                _root[ConfigurationPath.Combine(Path, key)] = value;
            }
        }

        /// <summary>
        /// Gets a configuration sub-section with the specified key.
        /// </summary>
        /// <param name="key">The key of the configuration section.</param>
        /// <returns>The <see cref="IConfigurationSection"/>.</returns>
        /// <remarks>
        ///     This method will never return <c>null</c>. If no matching sub-section is found with the specified key,
        ///     an empty <see cref="IConfigurationSection"/> will be returned.
        /// </remarks>
        public IConfigurationSection GetSection(string key) => _root.GetSection(ConfigurationPath.Combine(Path, key));

        /// <summary>
        /// Gets the immediate descendant configuration sub-sections.
        /// </summary>
        /// <returns>The configuration sub-sections.</returns>
        public IEnumerable<IConfigurationSection> GetChildren() => _root.GetChildrenImplementation(Path);

        /// <summary>
        /// Returns a <see cref="IChangeToken"/> that can be used to observe when this configuration is reloaded.
        /// </summary>
        /// <returns>The <see cref="IChangeToken"/>.</returns>
        public IChangeToken GetReloadToken() => _root.GetReloadToken();

        private string DebuggerToString()
        {
            var s = $"Path = {Path}";
            var childCount = Configuration.ConfigurationSectionDebugView.FromConfiguration(this, _root).Count;
            if (childCount > 0)
            {
                s += $", Sections = {childCount}";
            }
            if (Value is not null)
            {
                s += $", Value = {Value}";
                IConfigurationProvider? provider = Configuration.ConfigurationSectionDebugView.GetValueProvider(_root, Path);
                if (provider != null)
                {
                    s += $", Provider = {provider}";
                }
            }
            return s;
        }

        private sealed class ConfigurationSectionDebugView
        {
            private readonly ConfigurationSection _current;
            private readonly IConfigurationProvider? _provider;

            public ConfigurationSectionDebugView(ConfigurationSection current)
            {
                _current = current;
                _provider = Configuration.ConfigurationSectionDebugView.GetValueProvider(_current._root, _current.Path);
            }

            public string Path => _current.Path;
            public string Key => _current.Key;
            public string? Value => _current.Value;
            public IConfigurationProvider? Provider => _provider;
            public List<Configuration.ConfigurationSectionDebugView> Sections => Configuration.ConfigurationSectionDebugView.FromConfiguration(_current, _current._root);
        }
    }
}
