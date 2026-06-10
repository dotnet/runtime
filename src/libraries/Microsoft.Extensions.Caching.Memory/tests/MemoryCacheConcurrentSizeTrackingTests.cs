// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.Caching.Memory
{
    public class MemoryCacheConcurrentSizeTrackingTests
    {
        // Regression test for the size-tracking double-decrement that drives _cacheSize negative and
        // permanently latches a size-limited cache into rejecting all inserts. Many threads
        // concurrently Set (replace), Get, and Remove a small set of string keys with short
        // expirations under a generous SizeLimit. The working set is a tiny fraction of the limit, so
        // no legitimate capacity rejection can occur. After the storm the tracked size must not be
        // negative and the cache must still retain fresh, non-expiring entries.
        [Fact]
        public void ConcurrentSetReplaceAndRemove_DoesNotDriftSizeNegative_NorLatch()
        {
            using MemoryCache cache = new(new MemoryCacheOptions
            {
                SizeLimit = 200L * 1024 * 1024, // far larger than the working set below
                TrackStatistics = true
            });

            const int KeyCount = 16;
            const int ValueSize = 4096;
            byte[] payload = new byte[ValueSize];
            int threadCount = Math.Max(8, Environment.ProcessorCount * 4);

            long observedNegative = 0;
            using (CancellationTokenSource cts = new(TimeSpan.FromSeconds(3)))
            {
                Task monitor = Task.Run(() =>
                {
                    while (!cts.IsCancellationRequested)
                    {
                        long? size = cache.GetCurrentStatistics()?.CurrentEstimatedSize;
                        if (size.HasValue && size.Value < Interlocked.Read(ref observedNegative))
                        {
                            Interlocked.Exchange(ref observedNegative, size.Value);
                        }
                    }
                });

                Task[] workers = new Task[threadCount];
                for (int t = 0; t < threadCount; t++)
                {
                    workers[t] = Task.Run(() =>
                    {
                        Random rnd = new(Environment.CurrentManagedThreadId);
                        while (!cts.IsCancellationRequested)
                        {
                            string key = "k" + rnd.Next(KeyCount);
                            int roll = rnd.Next(100);
                            if (roll < 65)
                            {
                                using ICacheEntry entry = cache.CreateEntry(key);
                                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(15);
                                entry.Size = ValueSize;
                                entry.Value = payload;
                            }
                            else if (roll < 85)
                            {
                                cache.TryGetValue(key, out _);
                            }
                            else
                            {
                                cache.Remove(key);
                            }
                        }
                    });
                }

                Task.WaitAll(workers);
                cts.Cancel();
                monitor.Wait();
            }

            // Drain the working set so the cache is logically empty.
            for (int k = 0; k < KeyCount; k++)
            {
                cache.Remove("k" + k);
            }
            Thread.Sleep(100);

            Assert.True(observedNegative >= 0, $"CurrentEstimatedSize drifted negative to {observedNegative}.");

            long drainedSize = cache.GetCurrentStatistics().CurrentEstimatedSize ?? 0;
            Assert.True(drainedSize >= 0, $"CurrentEstimatedSize is negative after drain: {drainedSize}.");

            // The cache must still retain fresh, non-expiring entries (i.e., it is not latched).
            const int Probe = 512;
            int retained = 0;
            for (int i = 0; i < Probe; i++)
            {
                string key = "fresh-" + i;
                using (ICacheEntry entry = cache.CreateEntry(key))
                {
                    entry.Size = ValueSize;
                    entry.Value = payload;
                }

                if (cache.TryGetValue(key, out _))
                {
                    retained++;
                }
            }

            Assert.Equal(Probe, retained);
        }

        // Guards that the fix does not disable SizeLimit eviction: a tiny limit must keep the cache
        // bounded even when far more entries (in aggregate size) are inserted.
        [Fact]
        public void SizeLimit_StillEnforced_AfterReplaceAccountingFix()
        {
            using MemoryCache cache = new(new MemoryCacheOptions
            {
                SizeLimit = 40, // room for ~10 entries of size 4
                TrackStatistics = true
            });

            for (int i = 0; i < 1000; i++)
            {
                using ICacheEntry entry = cache.CreateEntry("cap-" + i);
                entry.Size = 4;
                entry.Value = i;
            }

            MemoryCacheStatistics stats = cache.GetCurrentStatistics();
            Assert.True(stats.CurrentEstimatedSize >= 0);
            Assert.True(stats.CurrentEstimatedSize <= 40, $"size {stats.CurrentEstimatedSize} exceeds limit 40");
            Assert.True(stats.CurrentEntryCount <= 10, $"count {stats.CurrentEntryCount} exceeds capacity");
        }
    }
}
