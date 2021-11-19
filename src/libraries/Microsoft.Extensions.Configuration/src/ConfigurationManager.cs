// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// ConfigurationManager is a mutable configuration object. It is both an <see cref="IConfigurationBuilder"/> and an <see cref="IConfigurationRoot"/>.
    /// As sources are added, it updates its current view of configuration. Once Build is called, configuration is frozen.
    /// </summary>
    public sealed class ConfigurationManager : IConfigurationBuilder, IConfigurationRoot, IDisposable
    {
        private readonly ConfigurationSources _sources;
        private readonly ConfigurationBuilderProperties _properties;

        private readonly List<IDisposable> _changeTokenRegistrations = new();
        private List<IConfigurationProvider> _providers = new();
        private ConfigurationReloadToken _changeToken = new();

        /// <summary>
        /// Creates an empty mutable configuration object that is both an <see cref="IConfigurationBuilder"/> and an <see cref="IConfigurationRoot"/>.
        /// </summary>
        public ConfigurationManager()
        {
            _sources = new ConfigurationSources(this);
            _properties = new ConfigurationBuilderProperties(this);

            // Make sure there's some default storage since there are no default providers.
            _sources.Add(new MemoryConfigurationSource());
        }

        /// <inheritdoc/>
        public string? this[string key]
        {
            get => ConfigurationRoot.GetConfiguration(_providers, key);
            set => ConfigurationRoot.SetConfiguration(_providers, key, value);
        }

        /// <inheritdoc/>
        public IConfigurationSection GetSection(string key) => new ConfigurationSection(this, key);

        /// <inheritdoc/>
        public IEnumerable<IConfigurationSection> GetChildren() => this.GetChildrenImplementation(null);

        IDictionary<string, object> IConfigurationBuilder.Properties => _properties;

        IList<IConfigurationSource> IConfigurationBuilder.Sources => _sources;

        IEnumerable<IConfigurationProvider> IConfigurationRoot.Providers => _providers;

        /// <inheritdoc/>
        public void Dispose()
        {
            DisposeRegistrationsAndProviders();
        }

        IConfigurationBuilder IConfigurationBuilder.Add(IConfigurationSource source)
        {
            _sources.Add(source ?? throw new ArgumentNullException(nameof(source)));
            return this;
        }

        IConfigurationRoot IConfigurationBuilder.Build() => this;

        IChangeToken IConfiguration.GetReloadToken() => _changeToken;

        void IConfigurationRoot.Reload()
        {
            foreach (var provider in _providers)
            {
                provider.Load();
            }

            RaiseChanged();
        }

        private void RaiseChanged()
        {
            var previousToken = Interlocked.Exchange(ref _changeToken, new ConfigurationReloadToken());
            previousToken.OnReload();
        }

        // Don't rebuild and reload all providers in the common case when a source is simply added to the IList.
        private void AddSource(IConfigurationSource source)
        {
            var provider = source.Build(this);

            var newProvidersList = new List<IConfigurationProvider>(_providers);
            newProvidersList.Add(provider);

            provider.Load();
            _changeTokenRegistrations.Add(ChangeToken.OnChange(() => provider.GetReloadToken(), () => RaiseChanged()));

            _providers = newProvidersList;
            RaiseChanged();
        }

        // Something other than Add was called on IConfigurationBuilder.Sources or IConfigurationBuilder.Properties has changed.
        private void ReloadSources()
        {
            DisposeRegistrationsAndProviders();

            _changeTokenRegistrations.Clear();

            var newProvidersList = new List<IConfigurationProvider>();

            foreach (var source in _sources)
            {
                newProvidersList.Add(source.Build(this));
            }

            foreach (var p in newProvidersList)
            {
                p.Load();
                _changeTokenRegistrations.Add(ChangeToken.OnChange(() => p.GetReloadToken(), () => RaiseChanged()));
            }

            _providers = newProvidersList;
            RaiseChanged();
        }

        private void DisposeRegistrationsAndProviders()
        {
            // dispose change token registrations
            foreach (var registration in _changeTokenRegistrations)
            {
                registration.Dispose();
            }

            // dispose providers
            foreach (var provider in _providers)
            {
                (provider as IDisposable)?.Dispose();
            }
        }

        private class ConfigurationSources : IList<IConfigurationSource>
        {
            private readonly List<IConfigurationSource> _sources = new();
            private readonly ConfigurationManager _config;

            public ConfigurationSources(ConfigurationManager config)
            {
                _config = config;
            }

            public IConfigurationSource this[int index]
            {
                get => _sources[index];
                set
                {
                    _sources[index] = value;
                    _config.ReloadSources();
                }
            }

            public int Count => _sources.Count;

            public bool IsReadOnly => false;

            public void Add(IConfigurationSource source)
            {
                _sources.Add(source);
                _config.AddSource(source);
            }

            public void Clear()
            {
                _sources.Clear();
                _config.ReloadSources();
            }

            public bool Contains(IConfigurationSource source)
            {
                return _sources.Contains(source);
            }

            public void CopyTo(IConfigurationSource[] array, int arrayIndex)
            {
                _sources.CopyTo(array, arrayIndex);
            }

            public IEnumerator<IConfigurationSource> GetEnumerator()
            {
                return _sources.GetEnumerator();
            }

            public int IndexOf(IConfigurationSource source)
            {
                return _sources.IndexOf(source);
            }

            public void Insert(int index, IConfigurationSource source)
            {
                _sources.Insert(index, source);
                _config.ReloadSources();
            }

            public bool Remove(IConfigurationSource source)
            {
                var removed = _sources.Remove(source);
                _config.ReloadSources();
                return removed;
            }

            public void RemoveAt(int index)
            {
                _sources.RemoveAt(index);
                _config.ReloadSources();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class ConfigurationBuilderProperties : IDictionary<string, object>
        {
            private readonly Dictionary<string, object> _properties = new();
            private readonly ConfigurationManager _config;

            public ConfigurationBuilderProperties(ConfigurationManager config)
            {
                _config = config;
            }

            public object this[string key]
            {
                get => _properties[key];
                set
                {
                    _properties[key] = value;
                    _config.ReloadSources();
                }
            }

            public ICollection<string> Keys => _properties.Keys;

            public ICollection<object> Values => _properties.Values;

            public int Count => _properties.Count;

            public bool IsReadOnly => false;

            public void Add(string key, object value)
            {
                _properties.Add(key, value);
                _config.ReloadSources();
            }

            public void Add(KeyValuePair<string, object> item)
            {
                ((IDictionary<string, object>)_properties).Add(item);
                _config.ReloadSources();
            }

            public void Clear()
            {
                _properties.Clear();
                _config.ReloadSources();
            }

            public bool Contains(KeyValuePair<string, object> item)
            {
                return _properties.Contains(item);
            }

            public bool ContainsKey(string key)
            {
                return _properties.ContainsKey(key);
            }

            public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
            {
                ((IDictionary<string, object>)_properties).CopyTo(array, arrayIndex);
            }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                return _properties.GetEnumerator();
            }

            public bool Remove(string key)
            {
                var wasRemoved = _properties.Remove(key);
                _config.ReloadSources();
                return wasRemoved;
            }

            public bool Remove(KeyValuePair<string, object> item)
            {
                var wasRemoved = ((IDictionary<string, object>)_properties).Remove(item);
                _config.ReloadSources();
                return wasRemoved;
            }

            public bool TryGetValue(string key, [NotNullWhen(true)] out object? value)
            {
                return _properties.TryGetValue(key, out value);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _properties.GetEnumerator();
            }
        }
    }
}
