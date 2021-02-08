// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private readonly ConcurrentDictionary<object, CacheEntry> _entries;

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

            _entries = new ConcurrentDictionary<object, CacheEntry>();

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
        public int Count => _entries.Count;

        // internal for testing
        internal long Size { get => Interlocked.Read(ref _cacheSize); }

        private ICollection<KeyValuePair<object, CacheEntry>> EntriesCollection => _entries;

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

            if (_entries.TryGetValue(entry.Key, out CacheEntry priorEntry))
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
                    entryAdded = _entries.TryAdd(entry.Key, entry);
                }
                else
                {
                    // Try to update with the new entry if a previous entries exist.
                    entryAdded = _entries.TryUpdate(entry.Key, entry, priorEntry);

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
                        entryAdded = _entries.TryAdd(entry.Key, entry);
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

            if (_entries.TryGetValue(key, out CacheEntry entry))
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
            if (_entries.TryRemove(key, out CacheEntry entry))
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
            if (EntriesCollection.Remove(new KeyValuePair<object, CacheEntry>(entry.Key, entry)))
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
            foreach (CacheEntry entry in cache._entries.Values)
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
            int removalCountTarget = (int)(_entries.Count * percentage);
            Compact(removalCountTarget, _ => 1);
        }

        private void Compact(long removalSizeTarget, Func<CacheEntry, long> computeEntrySize)
        {
            var entriesToRemove = new List<CacheEntry>();
            var lowPriEntries = new List<CacheEntry>();
            var normalPriEntries = new List<CacheEntry>();
            var highPriEntries = new List<CacheEntry>();
            long removedSize = 0;

            // Sort items by expired & priority status
            DateTimeOffset now = _options.Clock.UtcNow;
            foreach (CacheEntry entry in _entries.Values)
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
                            lowPriEntries.Add(entry);
                            break;
                        case CacheItemPriority.Normal:
                            normalPriEntries.Add(entry);
                            break;
                        case CacheItemPriority.High:
                            highPriEntries.Add(entry);
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
            static void ExpirePriorityBucket(ref long removedSize, long removalSizeTarget, Func<CacheEntry, long> computeEntrySize, List<CacheEntry> entriesToRemove, List<CacheEntry> priorityEntries)
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
                priorityEntries.Sort((e1, e2) => e1.LastAccessed.CompareTo(e2.LastAccessed));
                foreach (CacheEntry entry in priorityEntries)
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
    }
}
