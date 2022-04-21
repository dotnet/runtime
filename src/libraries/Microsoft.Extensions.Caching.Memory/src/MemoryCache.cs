// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Caching.Memory
{
    /// <summary>
    /// An implementation of <see cref="IMemoryCache"/> using a dictionary to
    /// store its entries.
    /// </summary>
    public class MemoryCache : IMemoryCache
    {
        internal readonly ILogger _logger;

        private readonly MemoryCacheOptions _options;

        private readonly List<WeakReference<Stats>>? _allStats;
        private readonly Stats? _accumulatedStats;
        private readonly ThreadLocal<Stats>? _stats;
        private CoherentState _coherentState;
        private bool _disposed;
        private DateTimeOffset _lastExpirationScan;

        /// <summary>
        /// Creates a new <see cref="MemoryCache"/> instance.
        /// </summary>
        /// <param name="optionsAccessor">The options of the cache.</param>
        public MemoryCache(IOptions<MemoryCacheOptions> optionsAccessor)
            : this(optionsAccessor, NullLoggerFactory.Instance) { }

        /// <summary>
        /// Creates a new <see cref="MemoryCache"/> instance.
        /// </summary>
        /// <param name="optionsAccessor">The options of the cache.</param>
        /// <param name="loggerFactory">The factory used to create loggers.</param>
        public MemoryCache(IOptions<MemoryCacheOptions> optionsAccessor, ILoggerFactory loggerFactory)
        {
            ThrowHelper.ThrowIfNull(optionsAccessor);
            ThrowHelper.ThrowIfNull(loggerFactory);

            _options = optionsAccessor.Value;
            _logger = loggerFactory.CreateLogger<MemoryCache>();

            _coherentState = new CoherentState();

            if (_options.Clock == null)
            {
                _options.Clock = new SystemClock();
            }

            if (_options.TrackStatistics)
            {
                _allStats = new List<WeakReference<Stats>>();
                _accumulatedStats = new Stats();
                _stats = new ThreadLocal<Stats>(() => new Stats(this));
            }

            _lastExpirationScan = _options.Clock.UtcNow;
            TrackLinkedCacheEntries = _options.TrackLinkedCacheEntries; // we store the setting now so it's consistent for entire MemoryCache lifetime
        }

        /// <summary>
        /// Cleans up the background collection events.
        /// </summary>
        ~MemoryCache() => Dispose(false);

        /// <summary>
        /// Gets the count of the current entries for diagnostic purposes.
        /// </summary>
        public int Count => _coherentState.Count;

        // internal for testing
        internal long Size => _coherentState.Size;

        internal bool TrackLinkedCacheEntries { get; }

        /// <inheritdoc />
        public ICacheEntry CreateEntry(object key)
        {
            CheckDisposed();
            ValidateCacheKey(key);

            return new CacheEntry(key, this);
        }

        internal void SetEntry(CacheEntry entry)
        {
            if (_disposed)
            {
                // No-op instead of throwing since this is called during CacheEntry.Dispose
                return;
            }

            if (_options.SizeLimit.HasValue && !entry.Size.HasValue)
            {
                throw new InvalidOperationException(SR.Format(SR.CacheEntryHasEmptySize, nameof(entry.Size), nameof(_options.SizeLimit)));
            }

            DateTimeOffset utcNow = _options.Clock!.UtcNow;

            // Applying the option's absolute expiration only if it's not already smaller.
            // This can be the case if a dependent cache entry has a smaller value, and
            // it was set by cascading it to its parent.
            if (entry.AbsoluteExpirationRelativeToNow.HasValue)
            {
                var absoluteExpiration = utcNow + entry.AbsoluteExpirationRelativeToNow.Value;
                if (!entry.AbsoluteExpiration.HasValue || absoluteExpiration < entry.AbsoluteExpiration.Value)
                {
                    entry.AbsoluteExpiration = absoluteExpiration;
                }
            }

            // Initialize the last access timestamp at the time the entry is added
            entry.LastAccessed = utcNow;

            CoherentState coherentState = _coherentState; // Clear() can update the reference in the meantime
            if (coherentState._entries.TryGetValue(entry.Key, out CacheEntry? priorEntry))
            {
                priorEntry.SetExpired(EvictionReason.Replaced);
            }

            if (entry.CheckExpired(utcNow))
            {
                entry.InvokeEvictionCallbacks();
                if (priorEntry != null)
                {
                    coherentState.RemoveEntry(priorEntry, _options);
                }
                StartScanForExpiredItemsIfNeeded(utcNow);
                return;
            }

            bool exceedsCapacity = UpdateCacheSizeExceedsCapacity(entry, coherentState);
            if (!exceedsCapacity)
            {
                bool entryAdded;

                if (priorEntry == null)
                {
                    // Try to add the new entry if no previous entries exist.
                    entryAdded = coherentState._entries.TryAdd(entry.Key, entry);
                }
                else
                {
                    // Try to update with the new entry if a previous entries exist.
                    entryAdded = coherentState._entries.TryUpdate(entry.Key, entry, priorEntry);

                    if (entryAdded)
                    {
                        if (_options.SizeLimit.HasValue)
                        {
                            // The prior entry was removed, decrease the by the prior entry's size
                            Interlocked.Add(ref coherentState._cacheSize, -priorEntry.Size!.Value);
                        }
                    }
                    else
                    {
                        // The update will fail if the previous entry was removed after retrival.
                        // Adding the new entry will succeed only if no entry has been added since.
                        // This guarantees removing an old entry does not prevent adding a new entry.
                        entryAdded = coherentState._entries.TryAdd(entry.Key, entry);
                    }
                }

                if (entryAdded)
                {
                    entry.AttachTokens();
                }
                else
                {
                    if (_options.SizeLimit.HasValue)
                    {
                        // Entry could not be added, reset cache size
                        Interlocked.Add(ref coherentState._cacheSize, -entry.Size!.Value);
                    }
                    entry.SetExpired(EvictionReason.Replaced);
                    entry.InvokeEvictionCallbacks();
                }

                if (priorEntry != null)
                {
                    priorEntry.InvokeEvictionCallbacks();
                }
            }
            else
            {
                entry.SetExpired(EvictionReason.Capacity);
                TriggerOvercapacityCompaction();
                entry.InvokeEvictionCallbacks();
                if (priorEntry != null)
                {
                    coherentState.RemoveEntry(priorEntry, _options);
                }
            }

            StartScanForExpiredItemsIfNeeded(utcNow);
        }

        /// <inheritdoc />
        public bool TryGetValue(object key, out object? result)
        {
            ThrowHelper.ThrowIfNull(key);

            CheckDisposed();

            DateTimeOffset utcNow = _options.Clock!.UtcNow;

            CoherentState coherentState = _coherentState; // Clear() can update the reference in the meantime
            if (coherentState._entries.TryGetValue(key, out CacheEntry? entry))
            {
                // Check if expired due to expiration tokens, timers, etc. and if so, remove it.
                // Allow a stale Replaced value to be returned due to concurrent calls to SetExpired during SetEntry.
                if (!entry.CheckExpired(utcNow) || entry.EvictionReason == EvictionReason.Replaced)
                {
                    entry.LastAccessed = utcNow;
                    result = entry.Value;

                    if (TrackLinkedCacheEntries && entry.CanPropagateOptions())
                    {
                        // When this entry is retrieved in the scope of creating another entry,
                        // that entry needs a copy of these expiration tokens.
                        entry.PropagateOptions(CacheEntryHelper.Current);
                    }

                    StartScanForExpiredItemsIfNeeded(utcNow);
                    // Hit
                    if (_allStats is not null)
                    {
                        if (IntPtr.Size == 4)
                            Interlocked.Increment(ref GetStats().Hits);
                        else
                            GetStats().Hits++;
                    }

                    return true;
                }
                else
                {
                    // TODO: For efficiency queue this up for batch removal
                    coherentState.RemoveEntry(entry, _options);
                }
            }

            StartScanForExpiredItemsIfNeeded(utcNow);

            result = null;
            // Miss
            if (_allStats is not null)
            {
                if (IntPtr.Size == 4)
                    Interlocked.Increment(ref GetStats().Misses);
                else
                    GetStats().Misses++;
            }

            return false;
        }

        /// <inheritdoc />
        public void Remove(object key)
        {
            ThrowHelper.ThrowIfNull(key);

            CheckDisposed();

            CoherentState coherentState = _coherentState; // Clear() can update the reference in the meantime
            if (coherentState._entries.TryRemove(key, out CacheEntry? entry))
            {
                if (_options.SizeLimit.HasValue)
                {
                    Interlocked.Add(ref coherentState._cacheSize, -entry.Size!.Value);
                }

                entry.SetExpired(EvictionReason.Removed);
                entry.InvokeEvictionCallbacks();
            }

            StartScanForExpiredItemsIfNeeded(_options.Clock!.UtcNow);
        }

        /// <summary>
        /// Removes all keys and values from the cache.
        /// </summary>
        public void Clear()
        {
            CheckDisposed();

            CoherentState oldState = Interlocked.Exchange(ref _coherentState, new CoherentState());
            foreach (var entry in oldState._entries)
            {
                entry.Value.SetExpired(EvictionReason.Removed);
                entry.Value.InvokeEvictionCallbacks();
            }
        }

        /// <summary>
        /// Gets a snapshot of the current statistics for the memory cache.
        /// </summary>
        /// <returns>Returns <see langword="null"/> if statistics are not being tracked because <see cref="MemoryCacheOptions.TrackStatistics" /> is <see langword="false"/>.</returns>
        public MemoryCacheStatistics? GetCurrentStatistics()
        {
            if (_allStats is not null)
            {
                (long hit, long miss) sumTotal = Sum();
                return new MemoryCacheStatistics()
                {
                    TotalMisses = sumTotal.miss,
                    TotalHits = sumTotal.hit,
                    CurrentEntryCount = Count,
                    CurrentEstimatedSize = _options.SizeLimit.HasValue ? Size : null
                };
            }

            return null;
        }

        internal void EntryExpired(CacheEntry entry)
        {
            // TODO: For efficiency consider processing these expirations in batches.
            _coherentState.RemoveEntry(entry, _options);
            StartScanForExpiredItemsIfNeeded(_options.Clock!.UtcNow);
        }

        // Called by multiple actions to see how long it's been since we last checked for expired items.
        // If sufficient time has elapsed then a scan is initiated on a background task.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StartScanForExpiredItemsIfNeeded(DateTimeOffset utcNow)
        {
            if (_options.ExpirationScanFrequency < utcNow - _lastExpirationScan)
            {
                ScheduleTask(utcNow);
            }

            void ScheduleTask(DateTimeOffset utcNow)
            {
                _lastExpirationScan = utcNow;
                Task.Factory.StartNew(state => ScanForExpiredItems((MemoryCache)state!), this,
                    CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }
        }

        private (long, long) Sum()
        {
            lock (_allStats!)
            {
                long hits = _accumulatedStats!.Hits;
                long misses = _accumulatedStats.Misses;

                foreach (WeakReference<Stats> wr in _allStats)
                {
                    if (wr.TryGetTarget(out Stats? stats))
                    {
                        hits += Interlocked.Read(ref stats.Hits);
                        misses += Interlocked.Read(ref stats.Misses);
                    }
                }

                return (hits, misses);
            }
        }

        private Stats GetStats() => _stats!.Value!;

        internal sealed class Stats
        {
            private readonly MemoryCache? _memoryCache;
            public long Hits;
            public long Misses;

            public Stats() { }

            public Stats(MemoryCache memoryCache)
            {
                _memoryCache = memoryCache;
                _memoryCache.AddToStats(this);
            }

            ~Stats() => _memoryCache?.RemoveFromStats(this);
        }

        private void RemoveFromStats(Stats current)
        {
            lock (_allStats!)
            {
                for (int i = 0; i < _allStats.Count; i++)
                {
                    if (_allStats[i].TryGetTarget(out Stats? stats) && stats == current)
                    {
                        _allStats.RemoveAt(i);
                        break;
                    }
                }

                _accumulatedStats!.Hits += Interlocked.Read(ref current.Hits);
                _accumulatedStats.Misses += Interlocked.Read(ref current.Misses);
                _allStats.TrimExcess();
            }
        }

        private void AddToStats(Stats current)
        {
            lock (_allStats!)
            {
                _allStats.Add(new WeakReference<Stats>(current));
            }
        }

        private static void ScanForExpiredItems(MemoryCache cache)
        {
            DateTimeOffset now = cache._lastExpirationScan = cache._options.Clock!.UtcNow;

            CoherentState coherentState = cache._coherentState; // Clear() can update the reference in the meantime
            foreach (KeyValuePair<object, CacheEntry> item in coherentState._entries)
            {
                CacheEntry entry = item.Value;

                if (entry.CheckExpired(now))
                {
                    coherentState.RemoveEntry(entry, cache._options);
                }
            }
        }

        private bool UpdateCacheSizeExceedsCapacity(CacheEntry entry, CoherentState coherentState)
        {
            if (!_options.SizeLimit.HasValue)
            {
                return false;
            }

            for (int i = 0; i < 100; i++)
            {
                long sizeRead = coherentState.Size;
                long newSize = sizeRead + entry.Size!.Value;

                if (newSize < 0 || newSize > _options.SizeLimit)
                {
                    // Overflow occurred, return true without updating the cache size
                    return true;
                }

                if (sizeRead == Interlocked.CompareExchange(ref coherentState._cacheSize, newSize, sizeRead))
                {
                    return false;
                }
            }

            return true;
        }

        private void TriggerOvercapacityCompaction()
        {
            _logger.LogDebug("Overcapacity compaction triggered");

            // Spawn background thread for compaction
            ThreadPool.QueueUserWorkItem(s => OvercapacityCompaction((MemoryCache)s!), this);
        }

        private static void OvercapacityCompaction(MemoryCache cache)
        {
            CoherentState coherentState = cache._coherentState; // Clear() can update the reference in the meantime
            long currentSize = coherentState.Size;

            cache._logger.LogDebug($"Overcapacity compaction executing. Current size {currentSize}");

            double? lowWatermark = cache._options.SizeLimit * (1 - cache._options.CompactionPercentage);
            if (currentSize > lowWatermark)
            {
                cache.Compact(currentSize - (long)lowWatermark, entry => entry.Size!.Value, coherentState);
            }

            cache._logger.LogDebug($"Overcapacity compaction executed. New size {coherentState.Size}");
        }

        /// Remove at least the given percentage (0.10 for 10%) of the total entries (or estimated memory?), according to the following policy:
        /// 1. Remove all expired items.
        /// 2. Bucket by CacheItemPriority.
        /// 3. Least recently used objects.
        /// ?. Items with the soonest absolute expiration.
        /// ?. Items with the soonest sliding expiration.
        /// ?. Larger objects - estimated by object graph size, inaccurate.
        public void Compact(double percentage)
        {
            CoherentState coherentState = _coherentState; // Clear() can update the reference in the meantime
            int removalCountTarget = (int)(coherentState.Count * percentage);
            Compact(removalCountTarget, _ => 1, coherentState);
        }

        private void Compact(long removalSizeTarget, Func<CacheEntry, long> computeEntrySize, CoherentState coherentState)
        {
            var entriesToRemove = new List<CacheEntry>();
            // cache LastAccessed outside of the CacheEntry so it is stable during compaction
            var lowPriEntries = new List<(CacheEntry entry, DateTimeOffset lastAccessed)>();
            var normalPriEntries = new List<(CacheEntry entry, DateTimeOffset lastAccessed)>();
            var highPriEntries = new List<(CacheEntry entry, DateTimeOffset lastAccessed)>();
            long removedSize = 0;

            // Sort items by expired & priority status
            DateTimeOffset now = _options.Clock!.UtcNow;
            foreach (KeyValuePair<object, CacheEntry> item in coherentState._entries)
            {
                CacheEntry entry = item.Value;
                if (entry.CheckExpired(now))
                {
                    entriesToRemove.Add(entry);
                    removedSize += computeEntrySize(entry);
                }
                else
                {
                    switch (entry.Priority)
                    {
                        case CacheItemPriority.Low:
                            lowPriEntries.Add((entry, entry.LastAccessed));
                            break;
                        case CacheItemPriority.Normal:
                            normalPriEntries.Add((entry, entry.LastAccessed));
                            break;
                        case CacheItemPriority.High:
                            highPriEntries.Add((entry, entry.LastAccessed));
                            break;
                        case CacheItemPriority.NeverRemove:
                            break;
                        default:
                            throw new NotSupportedException("Not implemented: " + entry.Priority);
                    }
                }
            }

            ExpirePriorityBucket(ref removedSize, removalSizeTarget, computeEntrySize, entriesToRemove, lowPriEntries);
            ExpirePriorityBucket(ref removedSize, removalSizeTarget, computeEntrySize, entriesToRemove, normalPriEntries);
            ExpirePriorityBucket(ref removedSize, removalSizeTarget, computeEntrySize, entriesToRemove, highPriEntries);

            foreach (CacheEntry entry in entriesToRemove)
            {
                coherentState.RemoveEntry(entry, _options);
            }

            // Policy:
            // 1. Least recently used objects.
            // ?. Items with the soonest absolute expiration.
            // ?. Items with the soonest sliding expiration.
            // ?. Larger objects - estimated by object graph size, inaccurate.
            static void ExpirePriorityBucket(ref long removedSize, long removalSizeTarget, Func<CacheEntry, long> computeEntrySize, List<CacheEntry> entriesToRemove, List<(CacheEntry Entry, DateTimeOffset LastAccessed)> priorityEntries)
            {
                // Do we meet our quota by just removing expired entries?
                if (removalSizeTarget <= removedSize)
                {
                    // No-op, we've met quota
                    return;
                }

                // Expire enough entries to reach our goal
                // TODO: Refine policy

                // LRU
                priorityEntries.Sort(static (e1, e2) => e1.LastAccessed.CompareTo(e2.LastAccessed));
                foreach ((CacheEntry entry, _) in priorityEntries)
                {
                    entry.SetExpired(EvictionReason.Capacity);
                    entriesToRemove.Add(entry);
                    removedSize += computeEntrySize(entry);

                    if (removalSizeTarget <= removedSize)
                    {
                        break;
                    }
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _stats?.Dispose();
                    GC.SuppressFinalize(this);
                }

                _disposed = true;
            }
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                Throw();
            }

            [DoesNotReturn]
            static void Throw() => throw new ObjectDisposedException(typeof(MemoryCache).FullName);
        }

        private static void ValidateCacheKey(object key)
        {
            ThrowHelper.ThrowIfNull(key);
        }

        private sealed class CoherentState
        {
            internal ConcurrentDictionary<object, CacheEntry> _entries = new ConcurrentDictionary<object, CacheEntry>();
            internal long _cacheSize;

            private ICollection<KeyValuePair<object, CacheEntry>> EntriesCollection => _entries;

            internal int Count => _entries.Count;

            internal long Size => Interlocked.Read(ref _cacheSize);

            internal void RemoveEntry(CacheEntry entry, MemoryCacheOptions options)
            {
                if (EntriesCollection.Remove(new KeyValuePair<object, CacheEntry>(entry.Key, entry)))
                {
                    if (options.SizeLimit.HasValue)
                    {
                        Interlocked.Add(ref _cacheSize, -entry.Size!.Value);
                    }
                    entry.InvokeEvictionCallbacks();
                }
            }
        }
    }
}
