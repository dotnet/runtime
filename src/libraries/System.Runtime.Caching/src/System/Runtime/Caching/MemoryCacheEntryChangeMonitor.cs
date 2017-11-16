// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Caching.Resources;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace System.Runtime.Caching
{
    internal sealed class MemoryCacheEntryChangeMonitor : CacheEntryChangeMonitor
    {
        // use UTC minimum DateTime for error free conversions to DateTimeOffset
        private static readonly DateTime s_DATETIME_MINVALUE_UTC = new DateTime(0, DateTimeKind.Utc);
        private const int MAX_CHAR_COUNT_OF_LONG_CONVERTED_TO_HEXADECIMAL_STRING = 16;
        private ReadOnlyCollection<String> _keys;
        private String _regionName;
        private String _uniqueId;
        private DateTimeOffset _lastModified;
        private List<MemoryCacheEntry> _dependencies;

        private MemoryCacheEntryChangeMonitor() { } // hide default .ctor

        private void InitDisposableMembers(MemoryCache cache)
        {
            bool dispose = true;
            try
            {
                bool hasChanged = false;
                string uniqueId = null;
                _dependencies = new List<MemoryCacheEntry>(_keys.Count);
                if (_keys.Count == 1)
                {
                    string k = _keys[0];
                    MemoryCacheEntry entry = cache.GetEntry(k);
                    DateTime utcCreated = s_DATETIME_MINVALUE_UTC;
                    StartMonitoring(cache, entry, ref hasChanged, ref utcCreated);
                    uniqueId = k + utcCreated.Ticks.ToString("X", CultureInfo.InvariantCulture);
                    _lastModified = utcCreated;
                }
                else
                {
                    int capacity = 0;
                    foreach (string key in _keys)
                    {
                        capacity += key.Length + MAX_CHAR_COUNT_OF_LONG_CONVERTED_TO_HEXADECIMAL_STRING;
                    }
                    StringBuilder sb = new StringBuilder(capacity);
                    foreach (string key in _keys)
                    {
                        MemoryCacheEntry entry = cache.GetEntry(key);
                        DateTime utcCreated = s_DATETIME_MINVALUE_UTC;
                        StartMonitoring(cache, entry, ref hasChanged, ref utcCreated);
                        sb.Append(key);
                        sb.Append(utcCreated.Ticks.ToString("X", CultureInfo.InvariantCulture));
                        if (utcCreated > _lastModified)
                        {
                            _lastModified = utcCreated;
                        }
                    }
                    uniqueId = sb.ToString();
                }
                _uniqueId = uniqueId;
                if (hasChanged)
                {
                    OnChanged(null);
                }
                dispose = false;
            }
            finally
            {
                InitializationComplete();
                if (dispose)
                {
                    Dispose();
                }
            }
        }

        private void StartMonitoring(MemoryCache cache, MemoryCacheEntry entry, ref bool hasChanged, ref DateTime utcCreated)
        {
            if (entry != null)
            {
                // pass reference to self so the dependency can notify us when it changes
                entry.AddDependent(cache, this);
                // add dependency to collection so we can dispose it later
                _dependencies.Add(entry);
                // has the entry already changed?
                if (entry.State != EntryState.AddedToCache)
                {
                    hasChanged = true;
                }
                utcCreated = entry.UtcCreated;
            }
            else
            {
                // the entry does not exist--set hasChanged to true so the user is notified
                hasChanged = true;
            }
        }

        //
        // protected members
        //

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_dependencies != null)
                {
                    foreach (MemoryCacheEntry entry in _dependencies)
                    {
                        if (entry != null)
                        {
                            entry.RemoveDependent(this);
                        }
                    }
                }
            }
        }

        //
        // public and internal members
        //

        public override ReadOnlyCollection<string> CacheKeys { get { return new ReadOnlyCollection<string>(_keys); } }
        public override string RegionName { get { return _regionName; } }
        public override string UniqueId { get { return _uniqueId; } }
        public override DateTimeOffset LastModified { get { return _lastModified; } }
        internal List<MemoryCacheEntry> Dependencies { get { return _dependencies; } }

        internal MemoryCacheEntryChangeMonitor(ReadOnlyCollection<String> keys, String regionName, MemoryCache cache)
        {
            Dbg.Assert(keys != null && keys.Count > 0, "keys != null && keys.Count > 0");
            _keys = keys;
            _regionName = regionName;
            InitDisposableMembers(cache);
        }

        // invoked by a cache entry dependency when it is released from the cache
        internal void OnCacheEntryReleased()
        {
            OnChanged(null);
        }
    }
}
