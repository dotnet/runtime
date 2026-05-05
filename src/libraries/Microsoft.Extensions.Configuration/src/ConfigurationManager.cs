// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Represents a mutable configuration object.
    /// </summary>
    /// <remarks>
    /// It is both an <see cref="IConfigurationBuilder"/> and an <see cref="IConfigurationRoot"/>.
    /// As sources are added, it updates its current view of configuration.
    /// </remarks>
    [DebuggerDisplay("{DebuggerToString(),nq}")]
    [DebuggerTypeProxy(typeof(ConfigurationManagerDebugView))]
    public sealed class ConfigurationManager : IConfigurationManager, IConfigurationRoot, IDisposable
    {
        // Concurrently modifying config sources or properties is not thread-safe. However, it is thread-safe to read config while modifying sources or properties.
        private readonly ConfigurationSources _sources;
        private readonly ConfigurationBuilderProperties _properties;

        // ReferenceCountedProviderManager manages copy-on-write references to support concurrently reading config while modifying sources.
        // It waits for readers to unreference the providers before disposing them without blocking on any concurrent operations.
        private readonly ReferenceCountedProviderManager _providerManager = new();

        // _changeTokenRegistrations is only modified when config sources are modified. It is not referenced by any read operations.
        // Because modify config sources is not thread-safe, modifying _changeTokenRegistrations does not need to be thread-safe either.
        private readonly List<IDisposable> _changeTokenRegistrations = new();
        private ConfigurationReloadToken _changeToken = new();

        // Non-null when the builder opted into reference resolution via UseReferences.
        // Rebuilt on every source mutation (AddSource/ReloadSources) so it always reflects the
        // current provider set. Reads are unsynchronized; in-flight reads that observe a stale
        // engine still see a consistent (old) provider snapshot held by that engine.
        private ReferenceResolutionEngine? _engine;

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
                using ReferenceCountedProviders reference = _providerManager.GetReference();
                ReferenceResolutionEngine? engine = _engine;
                if (engine is not null)
                {
                    return engine.TryGet(key, out string? resolved) ? resolved : null;
                }

                return ConfigurationRoot.GetConfiguration(reference.Providers, key);
            }
            set
            {
                using ReferenceCountedProviders reference = _providerManager.GetReference();
                ConfigurationRoot.SetConfiguration(reference.Providers, key, value);
            }
        }

        internal ReferenceResolutionEngine? Engine => _engine;

        /// <inheritdoc/>
        public IConfigurationSection GetSection(string key) => new ConfigurationSection(this, key);

        /// <inheritdoc/>
        public IEnumerable<IConfigurationSection> GetChildren() => this.GetChildrenImplementation(null);

        IDictionary<string, object> IConfigurationBuilder.Properties => _properties;

        /// <inheritdoc />
        public IList<IConfigurationSource> Sources => _sources;

        // We cannot track the duration of the reference to the providers if this property is used.
        // If a configuration source is removed after this is accessed but before it's completely enumerated,
        // this may allow access to a disposed provider.
        IEnumerable<IConfigurationProvider> IConfigurationRoot.Providers => _providerManager.NonReferenceCountedProviders;

        /// <inheritdoc/>
        public void Dispose()
        {
            DisposeRegistrations();
            Interlocked.Exchange(ref _engine, null)?.Dispose();
            _providerManager.Dispose();
        }

        IConfigurationBuilder IConfigurationBuilder.Add(IConfigurationSource source)
        {
            ArgumentNullException.ThrowIfNull(source);

            _sources.Add(source);
            return this;
        }

        IConfigurationRoot IConfigurationBuilder.Build() => this;

        IChangeToken IConfiguration.GetReloadToken() => _changeToken;

        void IConfigurationRoot.Reload()
        {
            using (ReferenceCountedProviders reference = _providerManager.GetReference())
            {
                foreach (IConfigurationProvider provider in reference.Providers)
                {
                    provider.Load();
                }
            }

            _engine?.Invalidate();
            RaiseChanged();
        }

        internal ReferenceCountedProviders GetProvidersReference() => _providerManager.GetReference();

        private void RaiseChanged()
        {
            var previousToken = Interlocked.Exchange(ref _changeToken, new ConfigurationReloadToken());
            previousToken.OnReload();
        }

        // Don't rebuild and reload all providers in the common case when a source is simply added to the IList.
        private void AddSource(IConfigurationSource source)
        {
            IConfigurationProvider provider = source.Build(this);

            provider.Load();
            _changeTokenRegistrations.Add(ChangeToken.OnChange(provider.GetReloadToken, RaiseChanged));

            _providerManager.AddProvider(provider);
            SwapEngine();
            RaiseChanged();
        }

        // Something other than Add was called on IConfigurationBuilder.Sources or IConfigurationBuilder.Properties has changed.
        private void ReloadSources()
        {
            DisposeRegistrations();

            _changeTokenRegistrations.Clear();

            var newProvidersList = new List<IConfigurationProvider>();

            foreach (IConfigurationSource source in _sources)
            {
                newProvidersList.Add(source.Build(this));
            }

            foreach (IConfigurationProvider p in newProvidersList)
            {
                p.Load();
                _changeTokenRegistrations.Add(ChangeToken.OnChange(p.GetReloadToken, RaiseChanged));
            }

            _providerManager.ReplaceProviders(newProvidersList);
            SwapEngine();
            RaiseChanged();
        }

        // Rebuild the engine against the current provider set when resolution is enabled.
        // The old engine's providers are the previous snapshot, and any in-flight read that
        // already captured the old engine will complete against that snapshot; a subsequent
        // read will pick up the new engine. The old engine is disposed to drop its reload-
        // token subscription against the old providers.
        private void SwapEngine()
        {
            ReferenceResolutionEngine? newEngine = null;
            if (ReferenceResolutionConfigurationBuilderExtensions.IsEnabled(_properties))
            {
                newEngine = new ReferenceResolutionEngine(_providerManager.NonReferenceCountedProviders);
            }

            ReferenceResolutionEngine? previous = Interlocked.Exchange(ref _engine, newEngine);
            previous?.Dispose();
        }

        private void DisposeRegistrations()
        {
            // dispose change token registrations
            foreach (IDisposable registration in _changeTokenRegistrations)
            {
                registration.Dispose();
            }
        }

        private string DebuggerToString()
        {
            return $"Sections = {ConfigurationSectionDebugView.FromConfiguration(this, this).Count}";
        }

        private sealed class ConfigurationManagerDebugView
        {
            private readonly ConfigurationManager _current;

            public ConfigurationManagerDebugView(ConfigurationManager current)
            {
                _current = current;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public ConfigurationSectionDebugView[] Items => ConfigurationSectionDebugView.FromConfiguration(_current, _current).ToArray();
        }

        private sealed class ConfigurationSources : IList<IConfigurationSource>
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

            public List<IConfigurationSource>.Enumerator GetEnumerator() => _sources.GetEnumerator();

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

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            IEnumerator<IConfigurationSource> IEnumerable<IConfigurationSource>.GetEnumerator() => GetEnumerator();
        }

        private sealed class ConfigurationBuilderProperties : IDictionary<string, object>
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
                    if (IsReferenceResolutionProperty(key))
                    {
                        _config.SwapEngine();
                    }
                    else
                    {
                        _config.ReloadSources();
                    }
                }
            }

            public ICollection<string> Keys => _properties.Keys;

            public ICollection<object> Values => _properties.Values;

            public int Count => _properties.Count;

            public bool IsReadOnly => false;

            public void Add(string key, object value)
            {
                _properties.Add(key, value);
                if (IsReferenceResolutionProperty(key))
                {
                    _config.SwapEngine();
                }
                else
                {
                    _config.ReloadSources();
                }
            }

            public void Add(KeyValuePair<string, object> item)
            {
                ((IDictionary<string, object>)_properties).Add(item);
                if (IsReferenceResolutionProperty(item.Key))
                {
                    _config.SwapEngine();
                }
                else
                {
                    _config.ReloadSources();
                }
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
                if (IsReferenceResolutionProperty(key))
                {
                    _config.SwapEngine();
                }
                else
                {
                    _config.ReloadSources();
                }

                return wasRemoved;
            }

            public bool Remove(KeyValuePair<string, object> item)
            {
                var wasRemoved = ((IDictionary<string, object>)_properties).Remove(item);
                if (IsReferenceResolutionProperty(item.Key))
                {
                    _config.SwapEngine();
                }
                else
                {
                    _config.ReloadSources();
                }

                return wasRemoved;
            }

            // Reference-resolution property writes only affect how the root interprets the existing
            // provider set; they never change which providers exist or what they hold. Handling them
            // through SwapEngine avoids the O(n) provider rebuild/Load cost that a general property
            // write triggers via ReloadSources, so enabling or reconfiguring resolution mid-setup
            // does not penalize callers using slow-to-load sources (e.g. Azure App Configuration).
            private static bool IsReferenceResolutionProperty(string key)
            {
                return key == ReferenceResolutionConfigurationBuilderExtensions.UseReferencesPropertyName;
            }

            public bool TryGetValue(string key, [NotNullWhen(true)] out object? value)
            {
                return _properties.TryGetValue(key, out value);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _properties.GetEnumerator();
            }

            // Direct accessor used by the ConfigurationManager build/reload loop to mutate
            // well-known properties (e.g. PreviouslyBuiltProviders) without triggering another
            // ReloadSources via the IDictionary interface this class exposes publicly.
            internal IDictionary<string, object> Raw => _properties;
        }
    }
}
