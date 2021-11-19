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

        // _providerManager provides copy-on-write references. It waits until all readers unreference before disposing any providers.
        private readonly ProviderManager _providerManager = new();
        private readonly List<IDisposable> _changeTokenRegistrations = new();
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
            get
            {
                using var refCounter = _providerManager.GetReference();
                return ConfigurationRoot.GetConfiguration(refCounter.Providers, key);
            }
            set
            {
                using var refCounter = _providerManager.GetReference();
                ConfigurationRoot.SetConfiguration(refCounter.Providers, key, value);
            }
        }

        /// <inheritdoc/>
        public IConfigurationSection GetSection(string key) => new ConfigurationSection(this, key);

        /// <inheritdoc/>
        public IEnumerable<IConfigurationSection> GetChildren()
        {
            using var refCounter = _providerManager.GetReference();
            return this.GetChildrenImplementation(refCounter.Providers, path: null);
        }

        IDictionary<string, object> IConfigurationBuilder.Properties => _properties;

        IList<IConfigurationSource> IConfigurationBuilder.Sources => _sources;

        // We cannot track the duration of the reference to the providers if this property is used.
        // If a configuration source is removed after this is accessed but before it's completely enumerated,
        // this may allow access to a disposed provider.
        IEnumerable<IConfigurationProvider> IConfigurationRoot.Providers => _providerManager.Providers;

        /// <inheritdoc/>
        public void Dispose()
        {
            DisposeRegistrations();
            _providerManager.Dispose();
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
            using (var refCounter = _providerManager.GetReference())
            {
                foreach (var provider in refCounter.Providers)
                {
                    provider.Load();
                }
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

            provider.Load();
            _changeTokenRegistrations.Add(ChangeToken.OnChange(() => provider.GetReloadToken(), () => RaiseChanged()));

            _providerManager.AddProvider(provider);
            RaiseChanged();
        }

        // Something other than Add was called on IConfigurationBuilder.Sources or IConfigurationBuilder.Properties has changed.
        private void ReloadSources()
        {
            DisposeRegistrations();

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

            _providerManager.ReplaceProviders(newProvidersList);
            RaiseChanged();
        }

        private void DisposeRegistrations()
        {
            // dispose change token registrations
            foreach (var registration in _changeTokenRegistrations)
            {
                registration.Dispose();
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

        private class ProviderManager : IDisposable
        {
            private readonly object _replaceProvidersLock = new object();
            private RefCountedProviders _refCountedProviders = new(new List<IConfigurationProvider>());

            public IEnumerable<IConfigurationProvider> Providers => _refCountedProviders.Providers;

            public RefCountedProviders GetReference()
            {
                // Lock to ensure oldRefCountedProviders.Dispose() in ReplaceProviders() doesn't decrement ref count to zero
                // before calling _refCountedProviders.AddRef().
                lock (_replaceProvidersLock)
                {
                    _refCountedProviders.AddRef();
                    return _refCountedProviders;
                }
            }

            // Providers should never be concurrently modified. Reading during modification is allowed.
            public void ReplaceProviders(List<IConfigurationProvider> providers)
            {
                RefCountedProviders oldRefCountedProviders = _refCountedProviders;

                lock (_replaceProvidersLock)
                {
                    _refCountedProviders = new RefCountedProviders(providers);
                }

                oldRefCountedProviders.Dispose();
            }

            public void AddProvider(IConfigurationProvider provider)
            {
                // Maintain existing references, but replace list with copy containing new item.
                _refCountedProviders.Providers = new List<IConfigurationProvider>(_refCountedProviders.Providers)
                {
                    provider
                };
            }

            public void Dispose() => _refCountedProviders.Dispose();
        }

        private class RefCountedProviders : IDisposable
        {
            private long _refCount = 1;

            public RefCountedProviders(List<IConfigurationProvider> providers)
            {
                Providers = providers;
            }

            public List<IConfigurationProvider> Providers { get; set; }

            public void AddRef()
            {
                Interlocked.Increment(ref _refCount);
            }

            public void Dispose()
            {
                if (Interlocked.Decrement(ref _refCount) == 0)
                {
                    foreach (var provider in Providers)
                    {
                        (provider as IDisposable)?.Dispose();
                    }
                }
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
