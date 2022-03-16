// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Caching.Memory.Infrastructure;
using Xunit;

namespace Microsoft.Extensions.Caching.Memory
{
    public class MemoryCacheHasStatisticsTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetCurrentStatistics_GetCache_UpdatesStatistics(bool sizeLimitIsSet)
        {
            var cache = sizeLimitIsSet ? 
                new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 }) :
                new MemoryCache(new MemoryCacheOptions { });

            cache.Set("key", "value", new MemoryCacheEntryOptions { Size = 2 });
            for (int i = 0; i < 100; i++)
            {
                Assert.Equal("value", cache.Get("key"));
                Assert.Null(cache.Get("missingKey1"));            
                Assert.Null(cache.Get("missingKey2"));            
            }

            MemoryCacheStatistics stats = cache.GetCurrentStatistics();

            Assert.Equal(200, stats.TotalMisses);
            Assert.Equal(100, stats.TotalHits);
            Assert.Equal(1, stats.CurrentEntryCount);
            VerifyCurrentEstimatedSize(2, sizeLimitIsSet, stats);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetCurrentStatistics_UpdateExistingCache_UpdatesStatistics(bool sizeLimitIsSet)
        {
            var cache = sizeLimitIsSet ? 
                new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 }) :
                new MemoryCache(new MemoryCacheOptions { });

            cache.Set("key", "value", new MemoryCacheEntryOptions { Size = 2 });
            Assert.Equal("value", cache.Get("key"));

            cache.Set("key", "updated value", new MemoryCacheEntryOptions { Size = 3 });
            Assert.Equal("updated value", cache.Get("key"));

            MemoryCacheStatistics stats = cache.GetCurrentStatistics();

            Assert.Equal(1, stats.CurrentEntryCount);
            Assert.Equal(0, stats.TotalMisses);
            Assert.Equal(2, stats.TotalHits);
            VerifyCurrentEstimatedSize(3, sizeLimitIsSet, stats);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetCurrentStatistics_UpdateAfterExistingItemExpired_CurrentEstimatedSizeResets(bool sizeLimitIsSet)
        {
            const string Key = "myKey";

            var cache = new MemoryCache(sizeLimitIsSet ?
                new MemoryCacheOptions { Clock = new SystemClock(), SizeLimit = 10 } :
                new MemoryCacheOptions { Clock = new SystemClock() }
            );

            ICacheEntry entry;
            using (entry = cache.CreateEntry(Key))
            {
                var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };
                var mc = new MemoryCacheEntryOptions { Size = 5 };
                cache.Set(Key, new object(), mc.AddExpirationToken(expirationToken));
                MemoryCacheStatistics stats = cache.GetCurrentStatistics();
                Assert.Equal(1,  cache.Count);
                Assert.Equal(1,  stats.CurrentEntryCount);
                VerifyCurrentEstimatedSize(5, sizeLimitIsSet, stats);

                expirationToken.HasChanged = true;
                cache.Set(Key, new object(), mc.AddExpirationToken(expirationToken));
                stats = cache.GetCurrentStatistics();
                Assert.Equal(0,  cache.Count);
                Assert.Equal(0,  stats.CurrentEntryCount);
                VerifyCurrentEstimatedSize(0, sizeLimitIsSet, stats);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetCurrentStatistics_EntrySizesAreOne_MultithreadedCacheUpdates_CountAndSizeRemainInSync(bool sizeLimitIsSet)
        {
            bool statsGetInaccurateDuringHeavyLoad = false;
            const int numEntries = 100;
            Random random = new Random();

            var cache = new MemoryCache(sizeLimitIsSet ?
                new MemoryCacheOptions { Clock = new SystemClock(), SizeLimit = 2000 } :
                new MemoryCacheOptions { Clock = new SystemClock() }
            );

            void FillCache()
            {
                for (int i = 0; i < numEntries; i++)
                {
                    var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };
                    cache.Set($"key{i}", $"value{i}",
                        new MemoryCacheEntryOptions { Size = 1 }
                            .AddExpirationToken(expirationToken));

                    if (random.Next(numEntries) < numEntries * 0.25)
                    {
                        // Set to expired 25% of the time
                        expirationToken.HasChanged = true;
                    }
                }
            }

            // start a few tasks to access entries in the background
            Task[] backgroundAccessTasks = new Task[Environment.ProcessorCount];
            bool done = false;

            for (int i = 0; i < backgroundAccessTasks.Length; i++)
            {
                backgroundAccessTasks[i] = Task.Run(async () =>
                {
                    while (!done)
                    {
                        cache.TryGetValue($"key{random.Next(numEntries)}", out _);
                        var stats = cache.GetCurrentStatistics();

                        // set flag when stats go out of sync
                        if ((stats.CurrentEntryCount != cache.Count)
                            || (sizeLimitIsSet && stats.CurrentEstimatedSize != stats.CurrentEntryCount))
                        {
                            statsGetInaccurateDuringHeavyLoad = true;
                        }

                        await Task.Yield();
                    }
                });
            }

            for (int i = 0; i < 1000; i++)
            {
                cache.Compact(1);
                FillCache();
            }

            done = true;

            Task.WaitAll(backgroundAccessTasks);

            // even if the values are not exact during heavy multithreaded operations, 
            // once done, the count and size eventually become fixed to expected value
            var finalStats = cache.GetCurrentStatistics();
            Assert.True(statsGetInaccurateDuringHeavyLoad);
            Assert.Equal(cache.Count, finalStats.CurrentEntryCount);
            VerifyCurrentEstimatedSize(finalStats.CurrentEntryCount, sizeLimitIsSet, finalStats);
        }

        private void VerifyCurrentEstimatedSize (long expected, bool sizeLimitIsSet, MemoryCacheStatistics stats)
        {
            if (sizeLimitIsSet)
            {
                Assert.Equal(expected, stats.CurrentEstimatedSize );
            }
            else
            {
                Assert.Null(stats.CurrentEstimatedSize );
            }
        }
    }
}
