// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Runtime.Caching
{
    internal sealed class MemoryCacheEntry : MemoryCacheKey
    {
        private readonly object _value;
        private readonly DateTime _utcCreated;
        private int _state;
        // expiration
        private DateTime _utcAbsExp;
        private TimeSpan _slidingExp;
        private ExpiresEntryRef _expiresEntryRef;
        private byte _expiresBucket; // index of the expiration list (bucket)
        // usage
        private readonly byte _usageBucket;  // index of the usage list (== priority-1)
        private UsageEntryRef _usageEntryRef;   // ref into the usage list
        private DateTime _utcLastUpdateUsage;   // time we last updated usage

        private readonly CacheEntryRemovedCallback _callback;
        private SeldomUsedFields _fields; // optimization to reduce workingset when the entry hasn't any dependencies

        private sealed class SeldomUsedFields
        {
            internal Collection<ChangeMonitor> _dependencies; // the entry's dependency needs to be disposed when the entry is released
            internal Dictionary<MemoryCacheEntryChangeMonitor, MemoryCacheEntryChangeMonitor> _dependents;  // dependents must be notified when this entry is removed
            internal MemoryCache _cache;
            internal Tuple<MemoryCacheStore, MemoryCacheEntry> _updateSentinel; // the MemoryCacheEntry (and its associated store) of the OnUpdateSentinel for this entry, if there is one
        }

        internal object Value
        {
            get { return _value; }
        }

        internal bool HasExpiration()
        {
            return _utcAbsExp < DateTime.MaxValue;
        }

        internal DateTime UtcAbsExp
        {
            get { return _utcAbsExp; }
            set { _utcAbsExp = value; }
        }

        internal DateTime UtcCreated
        {
            get { return _utcCreated; }
        }

        internal ExpiresEntryRef ExpiresEntryRef
        {
            get { return _expiresEntryRef; }
            set { _expiresEntryRef = value; }
        }

        internal byte ExpiresBucket
        {
            get { return _expiresBucket; }
            set { _expiresBucket = value; }
        }

        internal bool InExpires()
        {
            return !_expiresEntryRef.IsInvalid;
        }

        internal TimeSpan SlidingExp
        {
            get { return _slidingExp; }
        }

        internal EntryState State
        {
            get { return (EntryState)_state; }
            set { _state = (int)value; }
        }

        internal byte UsageBucket
        {
            get { return _usageBucket; }
        }

        internal UsageEntryRef UsageEntryRef
        {
            get { return _usageEntryRef; }
            set { _usageEntryRef = value; }
        }

        internal DateTime UtcLastUpdateUsage
        {
            get { return _utcLastUpdateUsage; }
            set { _utcLastUpdateUsage = value; }
        }

        internal MemoryCacheEntry(string key,
                                  object value,
                                  DateTimeOffset absExp,
                                  TimeSpan slidingExp,
                                  CacheItemPriority priority,
                                  Collection<ChangeMonitor> dependencies,
                                  CacheEntryRemovedCallback removedCallback,
                                  MemoryCache cache) : base(key)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            _utcCreated = DateTime.UtcNow;
            _value = value;

            _slidingExp = slidingExp;
            if (_slidingExp > TimeSpan.Zero)
            {
                _utcAbsExp = _utcCreated + _slidingExp;
            }
            else
            {
                _utcAbsExp = absExp.UtcDateTime;
            }

            _expiresEntryRef = ExpiresEntryRef.INVALID;
            _expiresBucket = 0xff;

            _usageEntryRef = UsageEntryRef.INVALID;
            if (priority == CacheItemPriority.NotRemovable)
            {
                _usageBucket = 0xff;
            }
            else
            {
                _usageBucket = 0;
            }

            _callback = removedCallback;

            if (dependencies != null)
            {
                _fields = new SeldomUsedFields();
                _fields._dependencies = dependencies;
                _fields._cache = cache;
            }
        }

        internal void AddDependent(MemoryCache cache, MemoryCacheEntryChangeMonitor dependent)
        {
            lock (this)
            {
                if (State > EntryState.AddedToCache)
                {
                    return;
                }
                if (_fields == null)
                {
                    _fields = new SeldomUsedFields();
                }
                if (_fields._cache == null)
                {
                    _fields._cache = cache;
                }
                if (_fields._dependents == null)
                {
                    _fields._dependents = new Dictionary<MemoryCacheEntryChangeMonitor, MemoryCacheEntryChangeMonitor>();
                }
                _fields._dependents[dependent] = dependent;
            }
        }

        private void CallCacheEntryRemovedCallback(MemoryCache cache, CacheEntryRemovedReason reason)
        {
            if (_callback == null)
            {
                return;
            }
            CacheEntryRemovedArguments args = new CacheEntryRemovedArguments(cache, reason, new CacheItem(Key, _value));
            try
            {
                _callback(args);
            }
            catch
            {
                //
            }
        }

        internal void CallNotifyOnChanged()
        {
            if (_fields != null && _fields._dependencies != null)
            {
                foreach (ChangeMonitor monitor in _fields._dependencies)
                {
                    monitor.NotifyOnChanged(new OnChangedCallback(this.OnDependencyChanged));
                }
            }
        }

        internal bool CompareExchangeState(EntryState value, EntryState comparand)
        {
            return (Interlocked.CompareExchange(ref _state, (int)value, (int)comparand) == (int)comparand);
        }

        // Associates this entry with an update sentinel. If this entry has a sliding expiration, we need to
        // touch the sentinel so that it doesn't expire.
        internal void ConfigureUpdateSentinel(MemoryCacheStore sentinelStore, MemoryCacheEntry sentinelEntry)
        {
            lock (this)
            {
                if (_fields == null)
                {
                    _fields = new SeldomUsedFields();
                }
                _fields._updateSentinel = Tuple.Create(sentinelStore, sentinelEntry);
            }
        }

        internal bool HasUsage()
        {
            return _usageBucket != 0xff;
        }

        internal bool InUsage()
        {
            return !_usageEntryRef.IsInvalid;
        }

        private void OnDependencyChanged(object state)
        {
            if (State == EntryState.AddedToCache)
            {
                _fields._cache.RemoveEntry(this.Key, this, CacheEntryRemovedReason.ChangeMonitorChanged);
            }
        }

        internal void Release(MemoryCache cache, CacheEntryRemovedReason reason)
        {
            State = EntryState.Closed;

            // Are there any cache entries that depend on this entry?
            // If so, we need to fire their dependencies.
            Dictionary<MemoryCacheEntryChangeMonitor, MemoryCacheEntryChangeMonitor>.KeyCollection deps = null;
            // clone the dependents
            lock (this)
            {
                if (_fields != null && _fields._dependents != null && _fields._dependents.Count > 0)
                {
                    deps = _fields._dependents.Keys;
                    // set to null so RemoveDependent does not attempt to access it, since we're not
                    // using a copy of the KeyCollection.
                    _fields._dependents = null;
                    Debug.Assert(_fields._dependents == null, "_fields._dependents == null");
                }
            }
            if (deps != null)
            {
                foreach (MemoryCacheEntryChangeMonitor dependent in deps)
                {
                    if (dependent != null)
                    {
                        dependent.OnCacheEntryReleased();
                    }
                }
            }

            CallCacheEntryRemovedCallback(cache, reason);

            // Dispose any dependencies
            if (_fields != null && _fields._dependencies != null)
            {
                foreach (ChangeMonitor monitor in _fields._dependencies)
                {
                    monitor.Dispose();
                }
            }
        }

        internal void RemoveDependent(MemoryCacheEntryChangeMonitor dependent)
        {
            lock (this)
            {
                if (_fields != null && _fields._dependents != null)
                {
                    _fields._dependents.Remove(dependent);
                }
            }
        }

        internal void UpdateSlidingExp(DateTime utcNow, CacheExpires expires)
        {
            if (_slidingExp > TimeSpan.Zero)
            {
                DateTime utcNewExpires = utcNow + _slidingExp;
                if (utcNewExpires - _utcAbsExp >= CacheExpires.MIN_UPDATE_DELTA || utcNewExpires < _utcAbsExp)
                {
                    expires.UtcUpdate(this, utcNewExpires);
                }
            }
        }

        internal void UpdateSlidingExpForUpdateSentinel()
        {
            // We don't need a lock to get information about the update sentinel
            SeldomUsedFields fields = _fields;
            if (fields != null)
            {
                Tuple<MemoryCacheStore, MemoryCacheEntry> sentinelInfo = fields._updateSentinel;

                // touch the update sentinel to keep it from expiring
                if (sentinelInfo != null)
                {
                    MemoryCacheStore sentinelStore = sentinelInfo.Item1;
                    MemoryCacheEntry sentinelEntry = sentinelInfo.Item2;
                    sentinelStore.UpdateExpAndUsage(sentinelEntry, updatePerfCounters: false); // perf counters shouldn't be polluted by touching update sentinel entry
                }
            }
        }

        internal void UpdateUsage(DateTime utcNow, CacheUsage usage)
        {
            // update, but not more frequently than once per second.
            if (InUsage() && _utcLastUpdateUsage < utcNow - CacheUsage.CORRELATED_REQUEST_TIMEOUT)
            {
                _utcLastUpdateUsage = utcNow;
                usage.Update(this);
                if (_fields != null && _fields._dependencies != null)
                {
                    foreach (ChangeMonitor monitor in _fields._dependencies)
                    {
                        MemoryCacheEntryChangeMonitor m = monitor as MemoryCacheEntryChangeMonitor;
                        if (m == null)
                        {
                            continue;
                        }
                        foreach (MemoryCacheEntry e in m.Dependencies)
                        {
                            MemoryCacheStore store = e._fields._cache.GetStore(e);
                            e.UpdateUsage(utcNow, store.Usage);
                        }
                    }
                }
            }
        }
    }
}
