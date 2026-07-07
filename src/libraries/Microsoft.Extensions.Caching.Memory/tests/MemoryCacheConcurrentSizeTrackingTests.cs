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
        //
        // The workers are LongRunning tasks, which the default scheduler backs with dedicated threads
        // rather than the shared ThreadPool. This prevents the storm from starving timing-sensitive
        // post-eviction callbacks in sibling tests. It runs as OuterLoop because it is a long-running
        // stress test, and ConditionalFact skips platforms without real thread support (e.g. browser/wasm).
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
        [OuterLoop]
        public void ConcurrentSetReplaceAndRemove_DoesNotDriftSizeNegative_NorLatch()
        {
            using MemoryCache cache = new(new MemoryCacheOptions
            {
                SizeLimit = 200L * 1024 * 1024, // far larger than the working set below
                TrackStatistics = true
            });

            const int KeyCount = 16;
            const int ValueSize = 4096;
            const int IterationsPerThread = 200_000;
            const int SampleMask = 1023; // sample CurrentEstimatedSize roughly every 1024 iterations
            byte[] payload = new byte[ValueSize];
            int threadCount = Math.Min(Math.Max(4, Environment.ProcessorCount), 16);

            long observedMinSize = 0;

            Task[] workers = new Task[threadCount];
            for (int t = 0; t < threadCount; t++)
            {
                int seed = t + 1;
                workers[t] = Task.Factory.StartNew(
                    () =>
                    {
                        Random rnd = new(seed);
                        for (int i = 0; i < IterationsPerThread; i++)
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

                            if ((i & SampleMask) == 0)
                            {
                                long? size = cache.GetCurrentStatistics()?.CurrentEstimatedSize;
                                if (size.HasValue && size.Value < Interlocked.Read(ref observedMinSize))
                                {
                                    Interlocked.Exchange(ref observedMinSize, size.Value);
                                }
                            }
                        }
                    },
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);
            }

            Task.WaitAll(workers);

            // Drain the working set so the cache is logically empty.
            for (int k = 0; k < KeyCount; k++)
            {
                cache.Remove("k" + k);
            }
            Thread.Sleep(100);

            Assert.True(observedMinSize >= 0, $"CurrentEstimatedSize drifted negative to {observedMinSize}.");

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
    }
}
