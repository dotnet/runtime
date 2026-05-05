// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETFRAMEWORK || NET
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
            private long _lastEvictedTimestamp; // Stopwatch timestamp of the latest eviction operation.
            private readonly TimeSpan _evictionInterval; // min duration needed to trigger a new evict operation.
            private readonly TimeSpan _slidingExpiration; // max duration allowed for cache entries to remain inactive.
            private readonly ConcurrentDictionary<TKey, CacheEntry> _cache = new();

            public Cache(TimeSpan slidingExpiration, TimeSpan evictionInterval)
            {
                _slidingExpiration = slidingExpiration;
                _evictionInterval = evictionInterval;
                _lastEvictedTimestamp = Stopwatch.GetTimestamp();
            }

            public TValue GetOrAdd<TValue>(TKey key, Func<TKey, TValue> valueFactory) where TValue : class?
            {
                CacheEntry entry = _cache.GetOrAdd(
                    key,
#if NET
                    static (TKey key, Func<TKey, TValue> valueFactory) => new(valueFactory(key)),
                    valueFactory);
#else
                    key => new(valueFactory(key)));
#endif
                long nowTimestamp = Stopwatch.GetTimestamp();
                Volatile.Write(ref entry.LastUsedTimestamp, nowTimestamp);

                if (Stopwatch.GetElapsedTime(Volatile.Read(ref _lastEvictedTimestamp), nowTimestamp) >= _evictionInterval)
                {
                    if (Interlocked.CompareExchange(ref _evictLock, 1, 0) == 0)
                    {
                        if (Stopwatch.GetElapsedTime(_lastEvictedTimestamp, nowTimestamp) >= _evictionInterval)
                        {
                            EvictStaleCacheEntries(nowTimestamp);
                            Volatile.Write(ref _lastEvictedTimestamp, nowTimestamp);
                        }

                        Volatile.Write(ref _evictLock, 0);
                    }
                }

                return (TValue)entry.Value!;
            }

            public void Clear()
            {
                _cache.Clear();
                _lastEvictedTimestamp = Stopwatch.GetTimestamp();
            }

            private void EvictStaleCacheEntries(long nowTimestamp)
            {
                foreach (KeyValuePair<TKey, CacheEntry> kvp in _cache)
                {
                    if (Stopwatch.GetElapsedTime(Volatile.Read(ref kvp.Value.LastUsedTimestamp), nowTimestamp) >= _slidingExpiration)
                    {
                        _cache.TryRemove(kvp.Key, out _);
                    }
                }
            }

            private sealed class CacheEntry
            {
                public readonly object? Value;
                public long LastUsedTimestamp;

                public CacheEntry(object? value)
                {
                    Value = value;
                }
            }
        }
    }
}
#endif
