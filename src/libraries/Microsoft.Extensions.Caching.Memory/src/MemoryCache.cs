// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
        private readonly ConcurrentDictionary<string, CacheEntry> _stringKeyEntries;
        private readonly ConcurrentDictionary<object, CacheEntry> _nonStringKeyEntries;

        private long _cacheSize;
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
            if (optionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _options = optionsAccessor.Value;
            _logger = loggerFactory.CreateLogger<MemoryCache>();

            _stringKeyEntries = new ConcurrentDictionary<string, CacheEntry>(StringKeyComparer.Instance);
            _nonStringKeyEntries = new ConcurrentDictionary<object, CacheEntry>();

            if (_options.Clock == null)
            {
                _options.Clock = new SystemClock();
            }

            _lastExpirationScan = _options.Clock.UtcNow;
        }

        /// <summary>
        /// Cleans up the background collection events.
        /// </summary>
        ~MemoryCache() => Dispose(false);

        /// <summary>
        /// Gets the count of the current entries for diagnostic purposes.
        /// </summary>
        public int Count => _stringKeyEntries.Count + _nonStringKeyEntries.Count;

        // internal for testing
        internal long Size { get => Interlocked.Read(ref _cacheSize); }

        private ICollection<KeyValuePair<string, CacheEntry>> StringKeyEntriesCollection => _stringKeyEntries;

        private ICollection<KeyValuePair<object, CacheEntry>> NonStringKeyEntriesCollection => _nonStringKeyEntries;

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

            DateTimeOffset utcNow = _options.Clock.UtcNow;

            DateTimeOffset? absoluteExpiration = null;
            if (entry.AbsoluteExpirationRelativeToNow.HasValue)
            {
                absoluteExpiration = utcNow + entry.AbsoluteExpirationRelativeToNow;
            }
            else if (entry.AbsoluteExpiration.HasValue)
            {
                absoluteExpiration = entry.AbsoluteExpiration;
            }

            // Applying the option's absolute expiration only if it's not already smaller.
            // This can be the case if a dependent cache entry has a smaller value, and
            // it was set by cascading it to its parent.
            if (absoluteExpiration.HasValue)
            {
                if (!entry.AbsoluteExpiration.HasValue || absoluteExpiration.Value < entry.AbsoluteExpiration.Value)
                {
                    entry.AbsoluteExpiration = absoluteExpiration;
                }
            }

            // Initialize the last access timestamp at the time the entry is added
            entry.LastAccessed = utcNow;

            CacheEntry priorEntry = null;
            string s = entry.Key as string;
            if (s != null)
            {
                if (_stringKeyEntries.TryGetValue(s, out priorEntry))
                {
                    priorEntry.SetExpired(EvictionReason.Replaced);
                }
            }
            else if (_nonStringKeyEntries.TryGetValue(entry.Key, out priorEntry))
            {
                priorEntry.SetExpired(EvictionReason.Replaced);
            }

            bool exceedsCapacity = UpdateCacheSizeExceedsCapacity(entry);

            if (!entry.CheckExpired(utcNow) && !exceedsCapacity)
            {
                bool entryAdded = false;

                if (priorEntry == null)
                {
                    // Try to add the new entry if no previous entries exist.
                    if (s != null)
                    {
                        entryAdded = _stringKeyEntries.TryAdd(s, entry);
                    }
                    else
                    {
                        entryAdded = _nonStringKeyEntries.TryAdd(entry.Key, entry);
                    }
                }
                else
                {
                    // Try to update with the new entry if a previous entries exist.
                    if (s != null)
                    {
                        entryAdded = _stringKeyEntries.TryUpdate(s, entry, priorEntry);
                    }
                    else
                    {
                        entryAdded = _nonStringKeyEntries.TryUpdate(entry.Key, entry, priorEntry);
                    }

                    if (entryAdded)
                    {
                        if (_options.SizeLimit.HasValue)
                        {
                            // The prior entry was removed, decrease the by the prior entry's size
                            Interlocked.Add(ref _cacheSize, -priorEntry.Size.Value);
                        }
                    }
                    else
                    {
                        // The update will fail if the previous entry was removed after retrival.
                        // Adding the new entry will succeed only if no entry has been added since.
                        // This guarantees removing an old entry does not prevent adding a new entry.
                        if (s != null)
                        {
                            entryAdded = _stringKeyEntries.TryAdd(s, entry);
                        }
                        else
                        {
                            entryAdded = _nonStringKeyEntries.TryAdd(entry.Key, entry);
                        }
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
                        Interlocked.Add(ref _cacheSize, -entry.Size.Value);
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
                if (exceedsCapacity)
                {
                    // The entry was not added due to overcapacity
                    entry.SetExpired(EvictionReason.Capacity);

                    TriggerOvercapacityCompaction();
                }
                else
                {
                    if (_options.SizeLimit.HasValue)
                    {
                        // Entry could not be added due to being expired, reset cache size
                        Interlocked.Add(ref _cacheSize, -entry.Size.Value);
                    }
                }

                entry.InvokeEvictionCallbacks();
                if (priorEntry != null)
                {
                    RemoveEntry(priorEntry);
                }
            }

            StartScanForExpiredItemsIfNeeded(utcNow);
        }

        /// <inheritdoc />
        public bool TryGetValue(object key, out object result)
        {
            ValidateCacheKey(key);
            CheckDisposed();

            DateTimeOffset utcNow = _options.Clock.UtcNow;

            bool found;
            CacheEntry entry;
            if (key is string s)
            {
                found = _stringKeyEntries.TryGetValue(s, out entry);
            }
            else
            {
                found = _nonStringKeyEntries.TryGetValue(key, out entry);
            }

            if (found)
            {
                // Check if expired due to expiration tokens, timers, etc. and if so, remove it.
                // Allow a stale Replaced value to be returned due to concurrent calls to SetExpired during SetEntry.
                if (!entry.CheckExpired(utcNow) || entry.EvictionReason == EvictionReason.Replaced)
                {
                    entry.LastAccessed = utcNow;
                    result = entry.Value;

                    if (entry.CanPropagateOptions())
                    {
                        // When this entry is retrieved in the scope of creating another entry,
                        // that entry needs a copy of these expiration tokens.
                        entry.PropagateOptions(CacheEntryHelper.Current);
                    }

                    StartScanForExpiredItemsIfNeeded(utcNow);

                    return true;
                }
                else
                {
                    // TODO: For efficiency queue this up for batch removal
                    RemoveEntry(entry);
                }
            }

            StartScanForExpiredItemsIfNeeded(utcNow);

            result = null;
            return false;
        }

        /// <inheritdoc />
        public void Remove(object key)
        {
            ValidateCacheKey(key);

            CheckDisposed();
            bool removed;
            CacheEntry entry;
            if (key is string s)
            {
                removed = _stringKeyEntries.TryRemove(s, out entry);
            }
            else
            {
                removed = _nonStringKeyEntries.TryRemove(key, out entry);
            }

            if (removed)
            {
                if (_options.SizeLimit.HasValue)
                {
                    Interlocked.Add(ref _cacheSize, -entry.Size.Value);
                }

                entry.SetExpired(EvictionReason.Removed);
                entry.InvokeEvictionCallbacks();
            }

            StartScanForExpiredItemsIfNeeded(_options.Clock.UtcNow);
        }

        private void RemoveEntry(CacheEntry entry)
        {
            bool removed;
            if (entry.Key is string s)
            {
                removed = StringKeyEntriesCollection.Remove(new KeyValuePair<string, CacheEntry>(s, entry));
            }
            else
            {
                removed = NonStringKeyEntriesCollection.Remove(new KeyValuePair<object, CacheEntry>(entry.Key, entry));
            }

            if (removed)
            {
                if (_options.SizeLimit.HasValue)
                {
                    Interlocked.Add(ref _cacheSize, -entry.Size.Value);
                }
                entry.InvokeEvictionCallbacks();
            }
        }

        internal void EntryExpired(CacheEntry entry)
        {
            // TODO: For efficiency consider processing these expirations in batches.
            RemoveEntry(entry);
            StartScanForExpiredItemsIfNeeded(_options.Clock.UtcNow);
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
                Task.Factory.StartNew(state => ScanForExpiredItems((MemoryCache)state), this,
                    CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }
        }

        private static void ScanForExpiredItems(MemoryCache cache)
        {
            DateTimeOffset now = cache._lastExpirationScan = cache._options.Clock.UtcNow;

            foreach (CacheEntry entry in cache.GetCacheEntries())
            {
                if (entry.CheckExpired(now))
                {
                    cache.RemoveEntry(entry);
                }
            }
        }

        private bool UpdateCacheSizeExceedsCapacity(CacheEntry entry)
        {
            if (!_options.SizeLimit.HasValue)
            {
                return false;
            }

            long newSize = 0L;
            for (int i = 0; i < 100; i++)
            {
                long sizeRead = Interlocked.Read(ref _cacheSize);
                newSize = sizeRead + entry.Size.Value;

                if (newSize < 0 || newSize > _options.SizeLimit)
                {
                    // Overflow occurred, return true without updating the cache size
                    return true;
                }

                if (sizeRead == Interlocked.CompareExchange(ref _cacheSize, newSize, sizeRead))
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
            ThreadPool.QueueUserWorkItem(s => OvercapacityCompaction((MemoryCache)s), this);
        }

        private static void OvercapacityCompaction(MemoryCache cache)
        {
            long currentSize = Interlocked.Read(ref cache._cacheSize);

            cache._logger.LogDebug($"Overcapacity compaction executing. Current size {currentSize}");

            double? lowWatermark = cache._options.SizeLimit * (1 - cache._options.CompactionPercentage);
            if (currentSize > lowWatermark)
            {
                cache.Compact(currentSize - (long)lowWatermark, entry => entry.Size.Value);
            }

            cache._logger.LogDebug($"Overcapacity compaction executed. New size {Interlocked.Read(ref cache._cacheSize)}");
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
            int removalCountTarget = (int)(Count * percentage);
            Compact(removalCountTarget, _ => 1);
        }

        private IEnumerable<CacheEntry> GetCacheEntries()
        {
            // note this mimics the outgoing code in that we don't just access
            // .Values, which has additional overheads; this is only used for rare
            // calls - compaction, clear, etc - so the additional overhead of a
            // generated enumerator is not alarming
            foreach (KeyValuePair<string, CacheEntry> item in _stringKeyEntries)
            {
                yield return item.Value;
            }
            foreach (KeyValuePair<object, CacheEntry> item in _nonStringKeyEntries)
            {
                yield return item.Value;
            }
        }

        private void Compact(long removalSizeTarget, Func<CacheEntry, long> computeEntrySize)
        {
            var entriesToRemove = new List<CacheEntry>();
            // cache LastAccessed outside of the CacheEntry so it is stable during compaction
            var lowPriEntries = new List<CompactPriorityEntry>();
            var normalPriEntries = new List<CompactPriorityEntry>();
            var highPriEntries = new List<CompactPriorityEntry>();
            long removedSize = 0;

            // Sort items by expired & priority status
            DateTimeOffset now = _options.Clock.UtcNow;
            foreach (CacheEntry entry in GetCacheEntries())
            {
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
                            lowPriEntries.Add(new CompactPriorityEntry(entry, entry.LastAccessed));
                            break;
                        case CacheItemPriority.Normal:
                            normalPriEntries.Add(new CompactPriorityEntry(entry, entry.LastAccessed));
                            break;
                        case CacheItemPriority.High:
                            highPriEntries.Add(new CompactPriorityEntry(entry, entry.LastAccessed));
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
                RemoveEntry(entry);
            }

            // Policy:
            // 1. Least recently used objects.
            // ?. Items with the soonest absolute expiration.
            // ?. Items with the soonest sliding expiration.
            // ?. Larger objects - estimated by object graph size, inaccurate.
            static void ExpirePriorityBucket(ref long removedSize, long removalSizeTarget, Func<CacheEntry, long> computeEntrySize, List<CacheEntry> entriesToRemove, List<CompactPriorityEntry> priorityEntries)
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
                foreach (CompactPriorityEntry priorityEntry in priorityEntries)
                {
                    CacheEntry entry = priorityEntry.Entry;
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

        // use a struct instead of a ValueTuple to avoid adding a new dependency
        // on System.ValueTuple on .NET Framework in a servicing release
        private readonly struct CompactPriorityEntry
        {
            public readonly CacheEntry Entry;
            public readonly DateTimeOffset LastAccessed;

            public CompactPriorityEntry(CacheEntry entry, DateTimeOffset lastAccessed)
            {
                Entry = entry;
                LastAccessed = lastAccessed;
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

            static void Throw() => throw new ObjectDisposedException(typeof(MemoryCache).FullName);
        }

        private static void ValidateCacheKey(object key)
        {
            if (key == null)
            {
                Throw();
            }

            static void Throw() => throw new ArgumentNullException(nameof(key));
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
                => obj is string s ? Marvin.ComputeHash32(MemoryMarshal.AsBytes(s.AsSpan()), Marvin.DefaultSeed) : 0;
        }
#endif
    }
}
