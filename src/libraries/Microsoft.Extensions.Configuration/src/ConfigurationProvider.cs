// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Defines the core behavior of configuration providers and provides a base for derived classes.
    /// </summary>
    public abstract class ConfigurationProvider : IConfigurationProvider
    {
        private ConfigurationReloadToken _reloadToken = new ConfigurationReloadToken();

        /// <summary>
        /// Initializes a new <see cref="IConfigurationProvider"/>.
        /// </summary>
        protected ConfigurationProvider()
        {
            Data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets or sets the configuration key-value pairs for this provider.
        /// </summary>
        protected IDictionary<string, string?> Data { get; set; }

        /// <summary>
        /// Attempts to find a value with the given key.
        /// </summary>
        /// <param name="key">The key to lookup.</param>
        /// <param name="value">When this method returns, contains the value if one is found.</param>
        /// <returns><see langword="true" /> if <paramref name="key" /> has a value; otherwise <see langword="false" />.</returns>
        public virtual bool TryGet(string key, out string? value)
            => Data.TryGetValue(key, out value);

        /// <summary>
        /// Sets a value for a given key.
        /// </summary>
        /// <param name="key">The configuration key to set.</param>
        /// <param name="value">The value to set.</param>
        public virtual void Set(string key, string? value)
            => Data[key] = value;

        /// <summary>
        /// Loads (or reloads) the data for this provider.
        /// </summary>
        public virtual void Load()
        { }

        /// <summary>
        /// Returns the list of keys that this provider has.
        /// </summary>
        /// <param name="earlierKeys">The earlier keys that other providers contain.</param>
        /// <param name="parentPath">The path for the parent IConfiguration.</param>
        /// <returns>The list of keys for this provider.</returns>
        public virtual IEnumerable<string> GetChildKeys(
            IEnumerable<string> earlierKeys,
            string? parentPath)
        {
            ArgumentNullException.ThrowIfNull(earlierKeys);
            Debug.Assert(ConfigurationPath.KeyDelimiter == ":");

            ChildKeysAggregator? accumulator = earlierKeys as ChildKeysAggregator;
            ChildKeysBag bag = new ChildKeysBag(accumulator, parentPath);
            if (Data is Dictionary<string, string?> dictionary)
            {
                foreach (string key in dictionary.Keys)
                {
                    bag.AddChildKey(key);
                }
            }
            else
            {
                foreach (KeyValuePair<string, string?> kv in Data)
                {
                    bag.AddChildKey(kv.Key);
                }
            }

            if (accumulator is not null)
            {
                // this is the shared accumulator
                return accumulator;
            }

            accumulator = bag.Accumulator;
            if (accumulator is null)
            {
                return earlierKeys;
            }

            foreach (string key in earlierKeys)
            {
                accumulator.Add(key);
            }

            ChildKeySorter.Sort(accumulator.Items, accumulator.Count);
            return accumulator;
        }

        /// <summary>
        /// Returns a <see cref="IChangeToken"/> that can be used to listen when this provider is reloaded.
        /// </summary>
        /// <returns>The <see cref="IChangeToken"/>.</returns>
        public IChangeToken GetReloadToken()
        {
            return _reloadToken;
        }

        /// <summary>
        /// Triggers the reload change token and creates a new one.
        /// </summary>
        protected void OnReload()
        {
            ConfigurationReloadToken previousToken = Interlocked.Exchange(ref _reloadToken, new ConfigurationReloadToken());
            previousToken.OnReload();
        }

        /// <summary>
        /// Generates a string representing this provider name and relevant details.
        /// </summary>
        /// <returns>The configuration name.</returns>
        public override string ToString() => GetType().Name;

        private ref struct ChildKeysBag
        {
            private readonly string? _parentPath;
            private readonly int _prefixLength;
            private readonly char _lastPrefixChar;
            private ReadOnlySpan<char> _last;
            private int _divergence;

            public ChildKeysBag(ChildKeysAggregator? accumulator, string? parentPath)
            {
                Accumulator = accumulator;
                _parentPath = parentPath;
                _prefixLength = parentPath?.Length ?? -1;
                _lastPrefixChar = _prefixLength > 0 ? parentPath![_prefixLength - 1] : '\0';
                _divergence = _prefixLength;
            }

            public ChildKeysAggregator? Accumulator { get; private set; }

            public void AddChildKey(string key)
            {
                if (_prefixLength > -1)
                {
                    if (key.Length <= _prefixLength || key[_prefixLength] != ':')
                    {
                        return;
                    }

                    if (_prefixLength != 0 && !MayMatch(key[_prefixLength - 1], _lastPrefixChar))
                    {
                        return;
                    }

                    if (_divergence < _prefixLength && !MayMatch(key[_divergence], _parentPath![_divergence]))
                    {
                        return;
                    }

                    if (!StartsWithParent(key))
                    {
                        return;
                    }
                }

                Add(key, _prefixLength + 1);
            }

            private bool StartsWithParent(string key)
            {
                if (key.StartsWith(_parentPath!, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
#if NET
                _divergence = key.AsSpan(0, _prefixLength).CommonPrefixLength(_parentPath.AsSpan());
#else
                int common = 0;
                while (common < _prefixLength && key[common] == _parentPath![common])
                {
                    common++;
                }

                _divergence = common;
#endif
                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool MayMatch(char a, char b)
            {
                if (a == b || (a | b) >= 0x80)
                {
                    return true;
                }

                uint fold = (uint)(a | 0x20);
                return fold == (uint)(b | 0x20) && fold - 'a' <= 'z' - 'a';
            }

            private void Add(string key, int start)
            {
                int delimiter = key.IndexOf(':', start);
                int length = delimiter < 0 ? key.Length - start : delimiter - start;
                ReadOnlySpan<char> segment = key.AsSpan(start, length);
                if (length != 0 && segment.Equals(_last, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                _last = segment;
                ChildKeysAggregator accumulator = Accumulator ??= new ChildKeysAggregator();
                if (start == 0 && length == key.Length)
                {
                    accumulator.Add(key);
                }
                else
                {
                    accumulator.Add(key.AsSpan(start, length));
                }
            }
        }
    }
}
