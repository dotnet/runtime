// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETFRAMEWORK || NETCOREAPP
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace System.Text.Json.Serialization.Metadata
{
    internal sealed partial class ReflectionEmitCachingMemberAccessor
    {
        private sealed class Cache<TKey> where TKey : notnull
        {
            private int _evictLock;
            private long _lastEvictedTicks; // timestamp of latest eviction operation.
            private readonly long _evictionIntervalTicks; // min timespan needed to trigger a new evict operation.
            private readonly long _slidingExpirationTicks; // max timespan allowed for cache entries to remain inactive.
            private readonly ConcurrentDictionary<TKey, CacheEntry> _cache = new();

            public Cache(TimeSpan slidingExpiration, TimeSpan evictionInterval)
            {
                _slidingExpirationTicks = slidingExpiration.Ticks;
                _evictionIntervalTicks = evictionInterval.Ticks;
                _lastEvictedTicks = DateTime.UtcNow.Ticks;
            }

            public TValue GetOrAdd<TValue>(TKey key, Func<TKey, TValue> valueFactory) where TValue : class?
            {
                CacheEntry entry = _cache.GetOrAdd(
                    key,
#if NETCOREAPP
                    static (TKey key, Func<TKey, TValue> valueFactory) => new(valueFactory(key)),
                    valueFactory);
#else
                    key => new(valueFactory(key)));
#endif
                long utcNowTicks = DateTime.UtcNow.Ticks;
                Volatile.Write(ref entry.LastUsedTicks, utcNowTicks);

                if (utcNowTicks - Volatile.Read(ref _lastEvictedTicks) >= _evictionIntervalTicks)
                {
                    if (Interlocked.CompareExchange(ref _evictLock, 1, 0) == 0)
                    {
                        if (utcNowTicks - _lastEvictedTicks >= _evictionIntervalTicks)
                        {
                            EvictStaleCacheEntries(utcNowTicks);
                            Volatile.Write(ref _lastEvictedTicks, utcNowTicks);
                        }

                        Volatile.Write(ref _evictLock, 0);
                    }
                }

                return (TValue)entry.Value!;
            }

            public void Clear()
            {
                _cache.Clear();
                _lastEvictedTicks = DateTime.UtcNow.Ticks;
            }

            private void EvictStaleCacheEntries(long utcNowTicks)
            {
                foreach (KeyValuePair<TKey, CacheEntry> kvp in _cache)
                {
                    if (utcNowTicks - Volatile.Read(ref kvp.Value.LastUsedTicks) >= _slidingExpirationTicks)
                    {
                        _cache.TryRemove(kvp.Key, out _);
                    }
                }
            }

            private sealed class CacheEntry
            {
                public readonly object? Value;
                public long LastUsedTicks;

                public CacheEntry(object? value)
                {
                    Value = value;
                }
            }
        }
    }
}
#endif
