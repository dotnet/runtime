// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Threading;
using System.Diagnostics;
using System.Security;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

namespace System.Runtime.Caching
{
    internal sealed class MemoryCacheStore : IDisposable
    {
        private const int INSERT_BLOCK_WAIT = 10000;
        private const int MAX_COUNT = int.MaxValue / 2;

        private readonly Hashtable _entries;
        private readonly object _entriesLock;
        private readonly CacheExpires _expires;
        private readonly CacheUsage _usage;
        private int _disposed;
        private ManualResetEvent _insertBlock;
        private volatile bool _useInsertBlock;
        private readonly MemoryCache _cache;
        private readonly Counters _perfCounters;
#if NETCOREAPP
        [UnsupportedOSPlatformGuard("browser")]
        private static bool _countersSupported => !OperatingSystem.IsBrowser();
#else
        private static bool _countersSupported => true;
#endif

        internal MemoryCacheStore(MemoryCache cache, Counters perfCounters)
        {
            _cache = cache;
            _perfCounters = perfCounters;
            _entries = new Hashtable(new MemoryCacheEqualityComparer());
            _entriesLock = new object();
            _expires = new CacheExpires(this);
            _usage = new CacheUsage(this);
            InitDisposableMembers();
        }

        // private members

        private void AddToCache(MemoryCacheEntry entry)
        {
            // add outside of lock
            if (entry == null)
            {
                return;
            }

            if (entry.HasExpiration())
            {
                _expires.Add(entry);
            }

            if (entry.HasUsage()
                && (!entry.HasExpiration() || entry.UtcAbsExp - DateTime.UtcNow >= CacheUsage.MIN_LIFETIME_FOR_USAGE))
            {
                _usage.Add(entry);
            }

            // One last sanity check to be sure we didn't fall victim to an Add concurrency
            if (!entry.CompareExchangeState(EntryState.AddedToCache, EntryState.AddingToCache))
            {
                if (entry.InExpires())
                {
                    _expires.Remove(entry);
                }

                if (entry.InUsage())
                {
                    _usage.Remove(entry);
                }
            }

            entry.CallNotifyOnChanged();
            if (_perfCounters != null && _countersSupported)
            {
                _perfCounters.Increment(CounterName.Entries);
                _perfCounters.Increment(CounterName.Turnover);
            }
        }

        private void InitDisposableMembers()
        {
            _insertBlock = new ManualResetEvent(true);
            _expires.EnableExpirationTimer(true);
        }

        private void RemoveFromCache(MemoryCacheEntry entry, CacheEntryRemovedReason reason, bool delayRelease = false)
        {
            // release outside of lock
            if (entry != null)
            {
                if (entry.InExpires())
                {
                    _expires.Remove(entry);
                }

                if (entry.InUsage())
                {
                    _usage.Remove(entry);
                }

                Debug.Assert(entry.State == EntryState.RemovingFromCache, "entry.State = EntryState.RemovingFromCache");

                entry.State = EntryState.RemovedFromCache;
                if (!delayRelease)
                {
                    entry.Release(_cache, reason);
                }
                if (_perfCounters != null && _countersSupported)
                {
                    _perfCounters.Decrement(CounterName.Entries);
                    _perfCounters.Increment(CounterName.Turnover);
                }
            }
        }

        // 'updatePerfCounters' defaults to true since this method is called by all Get() operations
        // to update both the performance counters and the sliding expiration. Callers that perform
        // nested sliding expiration updates (like a MemoryCacheEntry touching its update sentinel)
        // can pass false to prevent these from unintentionally showing up in the perf counters.
        internal void UpdateExpAndUsage(MemoryCacheEntry entry, bool updatePerfCounters = true)
        {
            if (entry != null)
            {
                if (entry.InUsage() || entry.SlidingExp > TimeSpan.Zero)
                {
                    DateTime utcNow = DateTime.UtcNow;
                    entry.UpdateSlidingExp(utcNow, _expires);
                    entry.UpdateUsage(utcNow, _usage);
                }

                // If this entry has an update sentinel, the sliding expiration is actually associated
                // with that sentinel, not with this entry. We need to update the sentinel's sliding expiration to
                // keep the sentinel from expiring, which in turn would force a removal of this entry from the cache.
                entry.UpdateSlidingExpForUpdateSentinel();

                if (updatePerfCounters && _perfCounters != null && _countersSupported)
                {
                    _perfCounters.Increment(CounterName.Hits);
                    _perfCounters.Increment(CounterName.HitRatio);
                    _perfCounters.Increment(CounterName.HitRatioBase);
                }
            }
            else
            {
                if (updatePerfCounters && _perfCounters != null && _countersSupported)
                {
                    _perfCounters.Increment(CounterName.Misses);
                    _perfCounters.Increment(CounterName.HitRatioBase);
                }
            }
        }

        private void WaitInsertBlock()
        {
            _insertBlock.WaitOne(INSERT_BLOCK_WAIT, false);
        }

        // public/internal members

        internal CacheUsage Usage { get { return _usage; } }

        internal MemoryCacheEntry AddOrGetExisting(MemoryCacheKey key, MemoryCacheEntry entry)
        {
            if (_useInsertBlock && entry.HasUsage())
            {
                WaitInsertBlock();
            }
            MemoryCacheEntry existingEntry = null;
            MemoryCacheEntry toBeReleasedEntry = null;
            bool added = false;
            lock (_entriesLock)
            {
                if (_disposed == 0)
                {
                    existingEntry = _entries[key] as MemoryCacheEntry;
                    // has it expired?
                    if (existingEntry != null && existingEntry.UtcAbsExp <= DateTime.UtcNow)
                    {
                        toBeReleasedEntry = existingEntry;
                        toBeReleasedEntry.State = EntryState.RemovingFromCache;
                        existingEntry = null;
                    }
                    // can we add entry to the cache?
                    if (existingEntry == null)
                    {
                        entry.State = EntryState.AddingToCache;
                        added = true;
                        _entries[key] = entry;
                    }
                }
            }
            // release outside of lock
            RemoveFromCache(toBeReleasedEntry, CacheEntryRemovedReason.Expired, delayRelease: true);
            if (added)
            {
                // add outside of lock
                AddToCache(entry);
            }
            // update outside of lock
            UpdateExpAndUsage(existingEntry);

            // Call Release after the new entry has been completely added so
            // that the CacheItemRemovedCallback can take a dependency on the newly inserted item.
            toBeReleasedEntry?.Release(_cache, CacheEntryRemovedReason.Expired);
            return existingEntry;
        }

        internal void BlockInsert()
        {
            _insertBlock.Reset();
            _useInsertBlock = true;
        }

        internal void CopyTo(IDictionary h)
        {
            lock (_entriesLock)
            {
                if (_disposed == 0)
                {
                    foreach (DictionaryEntry e in _entries)
                    {
                        MemoryCacheKey key = e.Key as MemoryCacheKey;
                        MemoryCacheEntry entry = e.Value as MemoryCacheEntry;
                        if (entry.UtcAbsExp > DateTime.UtcNow)
                        {
                            h[key.Key] = entry.Value;
                        }
                    }
                }
            }
        }

        internal int Count
        {
            get
            {
                return _entries.Count;
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                // disable CacheExpires timer
                _expires.EnableExpirationTimer(false);
                // build array list of entries
                ArrayList entries = new ArrayList(_entries.Count);
                lock (_entriesLock)
                {
                    foreach (DictionaryEntry e in _entries)
                    {
                        MemoryCacheEntry entry = e.Value as MemoryCacheEntry;
                        entries.Add(entry);
                    }
                    foreach (MemoryCacheEntry entry in entries)
                    {
                        MemoryCacheKey key = entry as MemoryCacheKey;
                        entry.State = EntryState.RemovingFromCache;
                        _entries.Remove(key);
                    }
                }
                // release entries outside of lock
                foreach (MemoryCacheEntry entry in entries)
                {
                    RemoveFromCache(entry, CacheEntryRemovedReason.CacheSpecificEviction);
                }

                // MemoryCacheStatistics has been disposed, and therefore nobody should be using
                // _insertBlock except for potential threads in WaitInsertBlock (which won't care if we call Close).
                Debug.Assert(_useInsertBlock == false, "_useInsertBlock == false");
                _insertBlock.Close();

                // Don't need to call GC.SuppressFinalize(this) for sealed types without finalizers.
            }
        }

        internal MemoryCacheEntry Get(MemoryCacheKey key)
        {
            MemoryCacheEntry entry = _entries[key] as MemoryCacheEntry;
            // has it expired?
            if (entry != null && entry.UtcAbsExp <= DateTime.UtcNow)
            {
                Remove(key, entry, CacheEntryRemovedReason.Expired);
                entry = null;
            }
            // update outside of lock
            UpdateExpAndUsage(entry);
            return entry;
        }

        internal MemoryCacheEntry Remove(MemoryCacheKey key, MemoryCacheEntry entryToRemove, CacheEntryRemovedReason reason)
        {
            MemoryCacheEntry entry = null;
            lock (_entriesLock)
            {
                if (_disposed == 0)
                {
                    // get current entry
                    entry = _entries[key] as MemoryCacheEntry;
                    // remove if it matches the entry to be removed (but always remove if entryToRemove is null)
                    if (entryToRemove == null || object.ReferenceEquals(entry, entryToRemove))
                    {
                        if (entry != null)
                        {
                            entry.State = EntryState.RemovingFromCache;
                            _entries.Remove(key);
                        }
                    }
                    else
                    {
                        entry = null;
                    }
                }
            }
            // release outside of lock
            RemoveFromCache(entry, reason);
            return entry;
        }

        internal void Set(MemoryCacheKey key, MemoryCacheEntry entry)
        {
            if (_useInsertBlock && entry.HasUsage())
            {
                WaitInsertBlock();
            }
            MemoryCacheEntry existingEntry = null;
            bool added = false;
            lock (_entriesLock)
            {
                if (_disposed == 0)
                {
                    existingEntry = _entries[key] as MemoryCacheEntry;
                    if (existingEntry != null)
                    {
                        existingEntry.State = EntryState.RemovingFromCache;
                    }
                    entry.State = EntryState.AddingToCache;
                    added = true;
                    _entries[key] = entry;
                }
            }

            CacheEntryRemovedReason reason = CacheEntryRemovedReason.Removed;
            if (existingEntry != null)
            {
                if (existingEntry.UtcAbsExp <= DateTime.UtcNow)
                {
                    reason = CacheEntryRemovedReason.Expired;
                }
                RemoveFromCache(existingEntry, reason, delayRelease: true);
            }
            if (added)
            {
                AddToCache(entry);
            }

            // Call Release after the new entry has been completely added so
            // that the CacheItemRemovedCallback can take a dependency on the newly inserted item.
            existingEntry?.Release(_cache, reason);
        }

        internal long TrimInternal(int percent)
        {
            Debug.Assert(percent <= 100, "percent <= 100");

            int count = Count;
            int toTrim = 0;
            // do we need to drop a percentage of entries?
            if (percent > 0)
            {
                toTrim = (int)Math.Ceiling(((long)count * (long)percent) / 100D);
                // would this leave us above MAX_COUNT?
                int minTrim = count - MAX_COUNT;
                if (toTrim < minTrim)
                {
                    toTrim = minTrim;
                }
            }
            // do we need to trim?
            if (toTrim <= 0 || _disposed == 1)
            {
                return 0;
            }
            int trimmed = 0; // total number of entries trimmed
            int trimmedOrExpired = 0;
#if DEBUG
            int beginTotalCount = count;
#endif

            trimmedOrExpired = _expires.FlushExpiredItems(true);
            if (trimmedOrExpired < toTrim)
            {
                trimmed = _usage.FlushUnderUsedItems(toTrim - trimmedOrExpired);
                trimmedOrExpired += trimmed;
            }

            if (trimmed > 0 && _perfCounters != null && _countersSupported)
            {
                // Update values for perfcounters
                _perfCounters.IncrementBy(CounterName.Trims, trimmed);
            }

#if DEBUG
            Dbg.Trace("MemoryCacheStore", "TrimInternal:"
                        + " beginTotalCount=" + beginTotalCount
                        + ", endTotalCount=" + count
                        + ", percent=" + percent
                        + ", trimmed=" + trimmed);
#endif
            return trimmedOrExpired;
        }

        internal void UnblockInsert()
        {
            _useInsertBlock = false;
            _insertBlock.Set();
        }
    }
}
