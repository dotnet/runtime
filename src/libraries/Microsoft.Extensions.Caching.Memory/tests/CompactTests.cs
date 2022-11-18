// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;
using Xunit;

namespace Microsoft.Extensions.Caching.Memory
{
    public class CompactTests
    {
        private MemoryCache CreateCache(ISystemClock clock = null)
        {
            return new MemoryCache(new MemoryCacheOptions()
            {
                Clock = clock,
            });
        }

        [Fact]
        public void CompactEmptyNoOps()
        {
            var cache = CreateCache();
            cache.Compact(0.10);
        }

        [Fact]
        public void Compact100PercentClearsAll()
        {
            var cache = CreateCache();
            cache.Set("key1", "value1");
            cache.Set("key2", "value2");
            Assert.Equal(2, cache.Count);
            cache.Compact(1.0);
            Assert.Equal(0, cache.Count);
        }

        [Fact]
        public void Compact100PercentClearsAllButNeverRemoveItems()
        {
            var cache = CreateCache();
            cache.Set("key1", "Value1", new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.NeverRemove));
            cache.Set("key2", "Value2", new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.NeverRemove));
            cache.Set("key3", "value3");
            cache.Set("key4", "value4");
            Assert.Equal(4, cache.Count);
            cache.Compact(1.0);
            Assert.Equal(2, cache.Count);
            Assert.Equal("Value1", cache.Get("key1"));
            Assert.Equal("Value2", cache.Get("key2"));
        }

        [Fact]
        public void CompactPrioritizesLowPriorityItems()
        {
            var cache = CreateCache();
            cache.Set("key1", "Value1", new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.Low));
            cache.Set("key2", "Value2", new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.Low));
            cache.Set("key3", "value3");
            cache.Set("key4", "value4");
            Assert.Equal(4, cache.Count);
            cache.Compact(0.5);
            Assert.Equal(2, cache.Count);
            Assert.Equal("value3", cache.Get("key3"));
            Assert.Equal("value4", cache.Get("key4"));
        }

        [Fact]
        public void CompactPrioritizesLRU()
        {
            var testClock = new TestClock();
            var cache = CreateCache(testClock);
            cache.Set("key1", "value1");
            testClock.Add(TimeSpan.FromSeconds(1));
            cache.Set("key2", "value2");
            testClock.Add(TimeSpan.FromSeconds(1));
            cache.Set("key3", "value3");
            testClock.Add(TimeSpan.FromSeconds(1));
            cache.Set("key4", "value4");
            Assert.Equal(4, cache.Count);
            cache.Compact(0.90);
            Assert.Equal(1, cache.Count);
            Assert.Equal("value4", cache.Get("key4"));
        }
    }

    [Collection(nameof(DisableParallelization))]
    public class CompactTestsDisableParallelization
    {
        /// <summary>
        /// Tests a race condition in Compact where CacheEntry.LastAccessed is getting updated
        /// by a different thread than what is doing the Compact, leading to sorting failing.
        ///
        /// See https://github.com/dotnet/runtime/issues/61032.
        /// </summary>
        [Fact]
        public void CompactLastAccessedRaceCondition()
        {
            const int numEntries = 100;
            MemoryCache cache = new MemoryCache(new MemoryCacheOptions());
            Random random = new Random();

            void FillCache()
            {
                for (int i = 0; i < numEntries; i++)
                {
                    cache.Set($"key{i}", $"value{i}");
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
                        await Task.Yield();
                    }
                });
            }

            for (int i = 0; i < 1000; i++)
            {
                FillCache();

                cache.Compact(1);
            }

            done = true;

            Task.WaitAll(backgroundAccessTasks);
        }
    }
}
