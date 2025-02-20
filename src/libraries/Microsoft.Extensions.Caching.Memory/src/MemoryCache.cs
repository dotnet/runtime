// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Caching.Memory
{
    /// <summary>
    /// Implements <see cref="IMemoryCache"/> using a dictionary to
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
        private DateTime _lastExpirationScan;

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

            if (_options.TrackStatistics)
            {
                _allStats = new List<WeakReference<Stats>>();
                _accumulatedStats = new Stats();
                _stats = new ThreadLocal<Stats>(() => new Stats(this));
            }

            _lastExpirationScan = UtcNow;
            TrackLinkedCacheEntries = _options.TrackLinkedCacheEntries; // we store the setting now so it's consistent for entire MemoryCache lifetime
        }

        private DateTime UtcNow => _options.Clock?.UtcNow.UtcDateTime ?? DateTime.UtcNow;

        /// <summary>
        /// Cleans up the background collection events.
        /// </summary>
        ~MemoryCache() => Dispose(false);

        /// <summary>
        /// Gets the count of the current entries for diagnostic purposes.
        /// </summary>
        public int Count => _coherentState.Count;

        /// <summary>
        /// Gets an enumerable of the all the keys in the <see cref="MemoryCache"/>.
        /// </summary>
        public IEnumerable<object> Keys
            => _coherentState.GetAllKeys();

        /// <summary>
        /// Internal accessor for Size for testing only.
        ///
        /// Note that this is only eventually consistent with the contents of the collection.
        /// See comment on <see cref="CoherentState"/>.
        /// </summary>
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

            if (_options.HasSizeLimit && entry.Size < 0)
            {
                throw new InvalidOperationException(SR.Format(SR.CacheEntryHasEmptySize, nameof(entry.Size), nameof(_options.SizeLimit)));
            }

            DateTime utcNow = UtcNow;

            // Applying the option's absolute expiration only if it's not already smaller.
            // This can be the case if a dependent cache entry has a smaller value, and
            // it was set by cascading it to its parent.
            if (entry.AbsoluteExpirationRelativeToNow.Ticks > 0)
            {
                long absoluteExpiration = (utcNow + entry.AbsoluteExpirationRelativeToNow).Ticks;
                if ((ulong)absoluteExpiration < (ulong)entry.AbsoluteExpirationTicks)
                {
                    entry.AbsoluteExpirationTicks = absoluteExpiration;
                }
            }

            // Initialize the last access timestamp at the time the entry is added
            entry.LastAccessed = utcNow;

            CoherentState coherentState = _coherentState; // Clear() can update the reference in the meantime
            if (coherentState.TryGetValue(entry.Key, out CacheEntry? priorEntry))
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
            }
            else if (!UpdateCacheSizeExceedsCapacity(entry, priorEntry, coherentState))
            {
                bool entryAdded;
                if (priorEntry == null)
                {
                    // Try to add the new entry if no previous entries exist.
                    entryAdded = coherentState.TryAdd(entry.Key, entry);
                }
                else
                {
                    // Try to update with the new entry if a previous entries exist.
                    entryAdded = coherentState.TryUpdate(entry.Key, entry, priorEntry);

                    if (!entryAdded)
                    {
                        // The update will fail if the previous entry was removed after retrieval.
                        // Adding the new entry will succeed only if no entry has been added since.
                        // This guarantees removing an old entry does not prevent adding a new entry.
                        entryAdded = coherentState.TryAdd(entry.Key, entry);
                    }
                }

                if (entryAdded)
                {
                    entry.AttachTokens();
                }
                else
                {
                    if (_options.HasSizeLimit)
                    {
                        // Entry could not be added, reset cache size
                        Interlocked.Add(ref coherentState._cacheSize, -entry.Size + (priorEntry?.Size).GetValueOrDefault());
                    }
                    entry.SetExpired(EvictionReason.Replaced);
                    entry.InvokeEvictionCallbacks();
                }

                priorEntry?.InvokeEvictionCallbacks();
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
            CoherentState coherentState = _coherentState; // Clear() can update the reference in the meantime
            coherentState.TryGetValue(key, out CacheEntry? entry); // note we rely on documented "default when fails" contract re the out
            return PostProcessTryGetValue(coherentState, entry, out result);
        }

#if NET9_0_OR_GREATER
        /// <summary>
        /// Gets the item associated with this key if present.
        /// </summary>
        /// <param name="key">A character span corresponding to a <see cref="string"/> identifying the requested entry.</param>
        /// <param name="value">The located value or null.</param>
        /// <returns>True if the key was found.</returns>
        /// <remarks>This method allows values with <see cref="string"/> keys to be queried by content without allocating a new <see cref="string"/> instance.</remarks>
        [OverloadResolutionPriority(1)]
        public bool TryGetValue(ReadOnlySpan<char> key, out object? value)
        {
            CheckDisposed();
            CoherentState coherentState = _coherentState; // Clear() can update the reference in the meantime
            coherentState.TryGetValue(key, out CacheEntry? entry); // note we rely on documented "default when fails" contract re the out
            return PostProcessTryGetValue(coherentState, entry, out value);
        }

        /// <summary>
        /// Gets the item associated with this key if present.
        /// </summary>
        /// <param name="key">A character span corresponding to a <see cref="string"/> identifying the requested entry.</param>
        /// <param name="value">The located value or null.</param>
        /// <returns>True if the key was found.</returns>
        /// <remarks>This method allows values with <see cref="string"/> keys to be queried by content without allocating a new <see cref="string"/> instance.</remarks>
        [OverloadResolutionPriority(1)]
        public bool TryGetValue<TItem>(ReadOnlySpan<char> key, out TItem? value)
        {
            // this implementation intentionally based on (and consistent with) CacheExtensions.TryGetValue<TItem>
            if (TryGetValue(key, out object? untyped))
            {
                if (untyped == null)
                {
                    value = default;
                    return true;
                }

                if (untyped is TItem item)
                {
                    value = item;
                    return true;
                }
            }

            value = default;
            return false;

        }
#endif

        private bool PostProcessTryGetValue(CoherentState coherentState, CacheEntry? entry, out object? result)
        {
            // shared "get value" logic
            DateTime utcNow = UtcNow;
            if (entry is not null)
            {
                // Check if expired due to expiration tokens, timers, etc. and if so, remove it.
                // Allow a stale Replaced value to be returned due to concurrent calls to SetExpired during SetEntry.
                if (!entry.CheckExpired(utcNow) || entry.EvictionReason == EvictionReason.Replaced)
                {
                    entry.LastAccessed = utcNow;
                    result = entry.Value;

                    if (TrackLinkedCacheEntries)
                    {
                        // When this entry is retrieved in the scope of creating another entry,
                        // that entry needs a copy of these expiration tokens.
                        entry.PropagateOptionsToCurrent();
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

            if (coherentState.TryRemove(key, out CacheEntry? entry))
            {
                if (_options.HasSizeLimit)
                {
                    Interlocked.Add(ref coherentState._cacheSize, -entry.Size);
                }

                entry.SetExpired(EvictionReason.Removed);
                entry.InvokeEvictionCallbacks();
            }

            StartScanForExpiredItemsIfNeeded(UtcNow);
        }

        /// <summary>
        /// Removes all keys and values from the cache.
        /// </summary>
        public void Clear()
        {
            CheckDisposed();

            CoherentState oldState = Interlocked.Exchange(ref _coherentState, new CoherentState());
            foreach (CacheEntry entry in oldState.GetAllValues())
            {
                entry.SetExpired(EvictionReason.Removed);
                entry.InvokeEvictionCallbacks();
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
            StartScanForExpiredItemsIfNeeded(UtcNow);
        }

        // Called by multiple actions to see how long it's been since we last checked for expired items.
        // If sufficient time has elapsed then a scan is initiated on a background task.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StartScanForExpiredItemsIfNeeded(DateTime utcNow)
        {
            if (_options.ExpirationScanFrequency < utcNow - _lastExpirationScan)
            {
                ScheduleTask(utcNow);
            }

            void ScheduleTask(DateTime utcNow)
            {
                _lastExpirationScan = utcNow;
                Task.Factory.StartNew(state => ((MemoryCache)state!).ScanForExpiredItems(), this,
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
                    if (!_allStats[i].TryGetTarget(out Stats? stats))
                    {
                        _allStats.RemoveAt(i);
                        i--;
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

        private void ScanForExpiredItems()
        {
            DateTime utcNow = _lastExpirationScan = UtcNow;

            CoherentState coherentState = _coherentState; // Clear() can update the reference in the meantime

            foreach (CacheEntry entry in coherentState.GetAllValues())
            {
                if (entry.CheckExpired(utcNow))
                {
                    coherentState.RemoveEntry(entry, _options);
                }
            }
        }

        /// <summary>
        /// Determines if increasing the cache size by the size of the
        /// entry would cause it to exceed any size limit on the cache.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if increasing the cache size would
        /// cause it to exceed the size limit; otherwise, <see langword="false" />.
        /// </returns>
        private bool UpdateCacheSizeExceedsCapacity(CacheEntry entry, CacheEntry? priorEntry, CoherentState coherentState)
        {
            long sizeLimit = _options.SizeLimitValue;
            if (sizeLimit < 0)
            {
                return false;
            }

            long sizeRead = coherentState.Size;
            for (int i = 0; i < 100; i++)
            {
                long newSize = sizeRead + entry.Size;
                if (priorEntry != null)
                {
                    Debug.Assert(entry.Key == priorEntry.Key);
                    newSize -= priorEntry.Size;
                }

                if ((ulong)newSize > (ulong)sizeLimit)
                {
                    // Overflow occurred, return true without updating the cache size
                    return true;
                }

                long original = Interlocked.CompareExchange(ref coherentState._cacheSize, newSize, sizeRead);
                if (sizeRead == original)
                {
                    return false;
                }
                sizeRead = original;
            }

            return true;
        }

        private int lockFlag;

        private void TriggerOvercapacityCompaction()
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("Overcapacity compaction triggered");

            // If no threads are currently running compact - enter lock and start compact
            // If there is already a thread that is running compact - do nothing
            if (Interlocked.CompareExchange(ref lockFlag, 1, 0) == 0)
                // Spawn background thread for compaction
                ThreadPool.QueueUserWorkItem(s =>
                {
                    try
                    {
                        ((MemoryCache)s!).OvercapacityCompaction();
                    }
                    finally
                    {
                        lockFlag = 0; // Release the lock
                    }
                }, this);
        }

        private void OvercapacityCompaction()
        {
            CoherentState coherentState = _coherentState; // Clear() can update the reference in the meantime
            long currentSize = coherentState.Size;

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug($"Overcapacity compaction executing. Current size {currentSize}");

            long sizeLimit = _options.SizeLimitValue;
            if (sizeLimit >= 0)
            {
                long lowWatermark = sizeLimit - (long)(sizeLimit * _options.CompactionPercentage);
                if (currentSize > lowWatermark)
                {
                    Compact(currentSize - (long)lowWatermark, entry => entry.Size, coherentState);
                }
            }

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug($"Overcapacity compaction executed. New size {coherentState.Size}");
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
            DateTime utcNow = UtcNow;
            foreach (CacheEntry entry in coherentState.GetAllValues())
            {
                if (entry.CheckExpired(utcNow))
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

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Disposes the cache and clears all entries.
        /// </summary>
        /// <param name="disposing"><see langword="true" /> to dispose the object resources; <see langword="false" /> to take no action.</param>
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

        /// <summary>
        /// Wrapper for the memory cache entries collection.
        ///
        /// Entries may have various sizes. If a size limit has been set, the cache keeps track of the aggregate of all the entries' sizes
        /// in order to trigger compaction when the size limit is exceeded.
        ///
        /// For performance reasons, the size is not updated atomically with the collection, but is only made eventually consistent.
        ///
        /// When the memory cache is cleared, it replaces the backing collection entirely. This may occur in parallel with operations
        /// like add, set, remove, and compact which may modify the collection and thus its overall size.
        ///
        /// To keep the overall size eventually consistent, therefore, the collection and the overall size are wrapped in this CoherentState
        /// object. Individual operations take a local reference to this wrapper object while they work, and make size updates to this object.
        /// Clearing the cache simply replaces the object, so that any still in progress updates do not affect the overall size value for
        /// the new backing collection.
        /// </summary>
        private sealed class CoherentState
        {
            private readonly ConcurrentDictionary<string, CacheEntry> _stringEntries = new ConcurrentDictionary<string, CacheEntry>(StringKeyComparer.Instance);
            private readonly ConcurrentDictionary<object, CacheEntry> _nonStringEntries = new ConcurrentDictionary<object, CacheEntry>();

#if NET9_0_OR_GREATER
            private readonly ConcurrentDictionary<string, CacheEntry>.AlternateLookup<ReadOnlySpan<char>> _stringAltLookup;

            public CoherentState()
            {
                _stringAltLookup = _stringEntries.GetAlternateLookup<ReadOnlySpan<char>>();
            }
#endif

            internal long _cacheSize;

            internal bool TryGetValue(object key, [NotNullWhen(true)] out CacheEntry? entry)
                => key is string s ? _stringEntries.TryGetValue(s, out entry) : _nonStringEntries.TryGetValue(key, out entry);

#if NET9_0_OR_GREATER
            internal bool TryGetValue(ReadOnlySpan<char> key, [NotNullWhen(true)] out CacheEntry? entry)
                => _stringAltLookup.TryGetValue(key, out entry);
#endif


            internal bool TryRemove(object key, [NotNullWhen(true)] out CacheEntry? entry)
                => key is string s ? _stringEntries.TryRemove(s, out entry) : _nonStringEntries.TryRemove(key, out entry);

            internal bool TryAdd(object key, CacheEntry entry)
                => key is string s ? _stringEntries.TryAdd(s, entry) : _nonStringEntries.TryAdd(key, entry);

            internal bool TryUpdate(object key, CacheEntry entry, CacheEntry comparison)
                => key is string s ? _stringEntries.TryUpdate(s, entry, comparison) : _nonStringEntries.TryUpdate(key, entry, comparison);

            public IEnumerable<CacheEntry> GetAllValues()
            {
                // note this mimics the outgoing code in that we don't just access
                // .Values, which has additional overheads; this is only used for rare
                // calls - compaction, clear, etc - so the additional overhead of a
                // generated enumerator is not alarming
                foreach (KeyValuePair<string, CacheEntry> entry in _stringEntries)
                {
                    yield return entry.Value;
                }
                foreach (KeyValuePair<object, CacheEntry> entry in _nonStringEntries)
                {
                    yield return entry.Value;
                }
            }

            public IEnumerable<object> GetAllKeys()
            {
                foreach (KeyValuePair<string, CacheEntry> pairs in _stringEntries)
                {
                    yield return pairs.Key;
                }
                foreach (KeyValuePair<object, CacheEntry> pairs in _nonStringEntries)
                {
                    yield return pairs.Key;
                }
            }

            private ICollection<KeyValuePair<string, CacheEntry>> StringEntriesCollection => _stringEntries;
            private ICollection<KeyValuePair<object, CacheEntry>> NonStringEntriesCollection => _nonStringEntries;

            internal int Count => _stringEntries.Count + _nonStringEntries.Count;

            internal long Size => Volatile.Read(ref _cacheSize);

            internal void RemoveEntry(CacheEntry entry, MemoryCacheOptions options)
            {
                if (entry.Key is string s)
                {
                    if (StringEntriesCollection.Remove(new KeyValuePair<string, CacheEntry>(s, entry)))
                    {
                        if (options.SizeLimit.HasValue)
                        {
                            Interlocked.Add(ref _cacheSize, -entry.Size);
                        }
                        entry.InvokeEvictionCallbacks();
                    }
                }
                else if (NonStringEntriesCollection.Remove(new KeyValuePair<object, CacheEntry>(entry.Key, entry)))
                {
                    if (options.SizeLimit.HasValue)
                    {
                        Interlocked.Add(ref _cacheSize, -entry.Size);
                    }
                    entry.InvokeEvictionCallbacks();
                }
            }

#if NETCOREAPP
            // on .NET Core, the inbuilt comparer has Marvin built in; no need to intercept
            private static class StringKeyComparer
            {
                internal static IEqualityComparer<string> Instance => EqualityComparer<string>.Default;
            }
#else
            // otherwise, we need a custom comparer that manually implements Marvin
            private sealed class StringKeyComparer : IEqualityComparer<string>, IEqualityComparer
            {
                private StringKeyComparer() { }

                internal static readonly IEqualityComparer<string> Instance = new StringKeyComparer();

                // special-case string keys and use Marvin hashing
                public int GetHashCode(string? s) => s is null ? 0
                    : Marvin.ComputeHash32(MemoryMarshal.AsBytes(s.AsSpan()), Marvin.DefaultSeed);

                public bool Equals(string? x, string? y)
                    => string.Equals(x, y);

                bool IEqualityComparer.Equals(object x, object y)
                    => object.Equals(x, y);

                int IEqualityComparer.GetHashCode(object obj)
                    => obj is string s ? GetHashCode(s) : 0;
            }
#endif
        }
    }
}
