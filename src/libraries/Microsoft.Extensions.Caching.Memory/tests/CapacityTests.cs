// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory.Infrastructure;
using Microsoft.Extensions.Internal;
using Xunit;

namespace Microsoft.Extensions.Caching.Memory
{
    public class CapacityTests
    {
        [Fact]
        public void MemoryDistributedCacheOptionsDefaultsTo200MBSizeLimit()
        {
            Assert.Equal(200 * 1024 * 1024, new MemoryDistributedCacheOptions().SizeLimit);
        }

        [Fact]
        public void NegativeSizeOnMemoryCacheEntryOptionsThrows()
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions();

            Assert.Throws<ArgumentOutOfRangeException>(() => { cacheEntryOptions.Size = -1; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { cacheEntryOptions.SetSize(-1); });
        }

        [Fact]
        public void NegativeSizeOnMemoryCacheEntryThrows()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());

            using (var cacheEntry = cache.CreateEntry(new object()))
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => { cacheEntry.Size = -1; });
                Assert.Throws<ArgumentOutOfRangeException>(() => { cacheEntry.SetSize(-1); });
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData(10L)]
        public void SettingSizeAfterEntryIsDisposedThrows(long? sizeLimit)
        {
            var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = sizeLimit });

            ICacheEntry cacheEntry = cache.CreateEntry("key");
            cacheEntry.Size = 5;
            cacheEntry.Value = "value";
            cacheEntry.Dispose();

            Assert.Throws<InvalidOperationException>(() => { cacheEntry.Size = 6; });
            Assert.Throws<InvalidOperationException>(() => { cacheEntry.SetSize(6); });
            Assert.Throws<InvalidOperationException>(() => { cacheEntry.Size = null; });
            Assert.Throws<InvalidOperationException>(() => cacheEntry.SetOptions(new MemoryCacheEntryOptions { Size = 6 }));
            Assert.Equal(5L, cacheEntry.Size);
        }

        [Fact]
        public void SettingOptionsAfterEntryIsDisposedDoesNotChangeOtherOptions()
        {
            var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });

            ICacheEntry cacheEntry = cache.CreateEntry("key");
            cacheEntry.AbsoluteExpiration = DateTimeOffset.MaxValue;
            cacheEntry.SlidingExpiration = TimeSpan.FromMinutes(5);
            cacheEntry.Priority = CacheItemPriority.NeverRemove;
            cacheEntry.Size = 5;
            cacheEntry.Value = "value";
            cacheEntry.Dispose();

            DateTimeOffset? absoluteExpiration = cacheEntry.AbsoluteExpiration;
            TimeSpan? absoluteExpirationRelativeToNow = cacheEntry.AbsoluteExpirationRelativeToNow;
            TimeSpan? slidingExpiration = cacheEntry.SlidingExpiration;
            CacheItemPriority priority = cacheEntry.Priority;

            var replacementOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.MaxValue.AddDays(-1),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2),
                SlidingExpiration = TimeSpan.FromMinutes(1),
                Priority = CacheItemPriority.Low,
                Size = 6,
            };

            Assert.Throws<InvalidOperationException>(() => cacheEntry.SetOptions(replacementOptions));
            Assert.Equal(absoluteExpiration, cacheEntry.AbsoluteExpiration);
            Assert.Equal(absoluteExpirationRelativeToNow, cacheEntry.AbsoluteExpirationRelativeToNow);
            Assert.Equal(slidingExpiration, cacheEntry.SlidingExpiration);
            Assert.Equal(priority, cacheEntry.Priority);
            Assert.Equal(5L, cacheEntry.Size);
        }

        [Fact]
        public void SettingSizeCapturedByGetOrCreateAfterEntryIsDisposedThrows()
        {
            var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });
            ICacheEntry? capturedEntry = null;

            string? value = cache.GetOrCreate("key", entry =>
            {
                capturedEntry = entry;
                entry.Size = 4;
                return "value";
            });

            Assert.Equal("value", value);

            ICacheEntry cacheEntry = Assert.IsAssignableFrom<ICacheEntry>(capturedEntry);
            Assert.Throws<InvalidOperationException>(() => { cacheEntry.Size = 2; });
            AssertCacheSize(4, cache);

            cache.Remove("key");
            AssertCacheSize(0, cache);
        }

        [Fact]
        public void SettingSizeAfterNestedGetOrCreateWithLinkedTrackingThrows()
        {
            var cache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 10,
                TrackLinkedCacheEntries = true,
            });
            ICacheEntry? capturedEntry = null;

            string? value = cache.GetOrCreate("outer", outerEntry =>
            {
                outerEntry.Size = 4;
                return cache.GetOrCreate("inner", innerEntry =>
                {
                    capturedEntry = innerEntry;
                    innerEntry.Size = 3;
                    return "value";
                });
            });

            Assert.Equal("value", value);
            Assert.Equal("value", cache.Get("outer"));
            Assert.Equal("value", cache.Get("inner"));

            ICacheEntry cacheEntry = Assert.IsAssignableFrom<ICacheEntry>(capturedEntry);
            Assert.Throws<InvalidOperationException>(() => { cacheEntry.Size = 2; });
            AssertCacheSize(7, cache);

            cache.Remove("inner");
            AssertCacheSize(4, cache);
            cache.Remove("outer");
            AssertCacheSize(0, cache);
        }

        [Fact]
        public void SettingSizeAfterEntryIsDisposedValidatesArgumentBeforeState()
        {
            var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });

            ICacheEntry cacheEntry = cache.CreateEntry("key");
            cacheEntry.Size = 5;
            cacheEntry.Value = "value";
            cacheEntry.Dispose();

            // A negative size is reported as an argument problem whichever route is used, so the property
            // and the SetSize extension agree rather than differing on which check runs first.
            Assert.Throws<ArgumentOutOfRangeException>(() => { cacheEntry.Size = -1; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { cacheEntry.SetSize(-1); });
        }

        [Fact]
        public void SettingSizeAfterEntryIsDisposedWithoutValueThrows()
        {
            var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });

            // Disposing without setting Value never commits the entry, but the size is frozen regardless.
            ICacheEntry cacheEntry = cache.CreateEntry("key");
            cacheEntry.Size = 5;
            cacheEntry.Dispose();

            Assert.Throws<InvalidOperationException>(() => { cacheEntry.Size = 6; });
            Assert.Equal(0, cache.Count);
            AssertCacheSize(0, cache);
        }

        [Fact]
        public void SettingSizeAfterEntryIsDisposedDoesNotSkewCacheSize()
        {
            var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });

            ICacheEntry cacheEntry = cache.CreateEntry("key");
            cacheEntry.Size = 4;
            cacheEntry.Value = "value";
            cacheEntry.Dispose();

            AssertCacheSize(4, cache);

            Assert.Throws<InvalidOperationException>(() => { cacheEntry.Size = 2; });
            AssertCacheSize(4, cache);

            cache.Remove("key");
            AssertCacheSize(0, cache);

            // The cache is not latched: an entry needing the whole limit is still admitted, which a skewed
            // total would have refused.
            cache.Set("key2", "value2", new MemoryCacheEntryOptions { Size = 10 });

            Assert.Equal("value2", cache.Get("key2"));
            AssertCacheSize(10, cache);
        }

        [Fact]
        public void CacheWithSizeLimitAddingEntryWithoutSizeThrows()
        {
            var cache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 10
            });

            Assert.Throws<InvalidOperationException>(() => cache.Set(new object(), new object()));
        }

        [Fact]
        public void NonPositiveCacheSizeLimitThrows()
        {
            var options = new MemoryCacheOptions();

            Assert.Throws<ArgumentOutOfRangeException>(() => options.SizeLimit = -1);
        }

        [Fact]
        public void InvalidRemovalPercentageOnOvercapacityCompactionThrows()
        {
            var options = new MemoryCacheOptions();

            Assert.Throws<ArgumentOutOfRangeException>(() => options.CompactionPercentage = 1.1);
            Assert.Throws<ArgumentOutOfRangeException>(() => options.CompactionPercentage = -0.1);
        }

        [Fact]
        public void AddingEntryIncreasesCacheSizeWhenEnforcingSizeLimit()
        {
            var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });

            AssertCacheSize(0, cache);

            cache.Set("key", "value", new MemoryCacheEntryOptions { Size = 5 });

            AssertCacheSize(5, cache);
        }

        [Fact]
        public void AddingEntryDoesNotIncreasesCacheSizeWhenNotEnforcingSizeLimit()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());

            AssertCacheSize(0, cache);

            cache.Set("key", "value", new MemoryCacheEntryOptions { Size = 5 });

            AssertCacheSize(0, cache);
        }

        [Fact]
        public void DoNotAddEntryIfItExceedsCapacity()
        {
            var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });

            AssertCacheSize(0, cache);

            cache.Set("key", "value", new MemoryCacheEntryOptions { Size = 4 });

            Assert.Equal("value", cache.Get("key"));
            AssertCacheSize(4, cache);

            cache.Set("key2", "value2", new MemoryCacheEntryOptions { Size = 7 });

            Assert.Null(cache.Get("key2"));
            AssertCacheSize(4, cache);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/72912")]
        public async Task DoNotAddIfSizeOverflows()
        {
            var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = long.MaxValue });

            var entryOptions = new MemoryCacheEntryOptions { Size = long.MaxValue };
            var sem = new SemaphoreSlim(0, 1);
            entryOptions.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration
            {
                EvictionCallback = (k, v, r, s) => sem.Release(),
                State = null
            });

            AssertCacheSize(0, cache);

            cache.Set("key", "value", entryOptions);

            Assert.Equal("value", cache.Get("key"));
            AssertCacheSize(long.MaxValue, cache);

            cache.Set("key1", "value1", new MemoryCacheEntryOptions { Size = long.MaxValue });
            // Do not add the new item
            Assert.Null(cache.Get("key1"));

            // Wait for compaction to complete
            Assert.True(await sem.WaitAsync(TimeSpan.FromSeconds(10)));

            // Compaction removes old item
            Assert.Null(cache.Get("key"));
            AssertCacheSize(0, cache);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/72912")]
        public async Task ExceedsCapacityCompacts()
        {
            var cache = new MemoryCache(new MemoryCacheOptions
            {
                ExpirationScanFrequency = TimeSpan.Zero,
                SizeLimit = 10,
                CompactionPercentage = 0.5
            });

            var entryOptions = new MemoryCacheEntryOptions { Size = 6 };
            var sem = new SemaphoreSlim(0, 1);
            entryOptions.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration
            {
                EvictionCallback = (k, v, r, s) => sem.Release(),
                State = null
            });

            AssertCacheSize(0, cache);

            cache.Set("key", "value", entryOptions);

            Assert.Equal("value", cache.Get("key"));
            AssertCacheSize(6, cache);

            cache.Set("key2", "value2", new MemoryCacheEntryOptions { Size = 5 });

            // Wait for compaction to complete
            Assert.True(await sem.WaitAsync(TimeSpan.FromSeconds(10)));

            Assert.Null(cache.Get("key"));
            Assert.Null(cache.Get("key2"));
            AssertCacheSize(0, cache);
        }

        [Fact]
        public void AddingReplacementWithSizeIncreaseUpdates()
        {
            var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });

            AssertCacheSize(0, cache);

            cache.Set("key", "value", new MemoryCacheEntryOptions { Size = 2 });

            Assert.Equal("value", cache.Get("key"));
            AssertCacheSize(2, cache);

            cache.Set("key", "value1", new MemoryCacheEntryOptions { Size = 3 });

            Assert.Equal("value1", cache.Get("key"));
            AssertCacheSize(3, cache);
        }

        [Fact]
        public void AddingReplacementWithSizeDecreaseUpdates()
        {
            var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });

            AssertCacheSize(0, cache);

            cache.Set("key", "value", new MemoryCacheEntryOptions { Size = 2 });

            Assert.Equal("value", cache.Get("key"));
            AssertCacheSize(2, cache);

            cache.Set("key", "value1", new MemoryCacheEntryOptions { Size = 1 });

            Assert.Equal("value1", cache.Get("key"));
            AssertCacheSize(1, cache);
        }

        [Fact]
        public void AddingReplacementWhenTotalSizeExceedsCapacityDoesNotUpdateAndRemovesOldEntry()
        {
            var cache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 5,
                CompactionPercentage = 0.5
            });

            AssertCacheSize(0, cache);

            cache.Set("key", "value", new MemoryCacheEntryOptions { Size = 5 });

            Assert.Equal("value", cache.Get("key"));
            AssertCacheSize(5, cache);

            cache.Set("key", "value1", new MemoryCacheEntryOptions { Size = 6 });

            Assert.Null(cache.Get("key"));
            AssertCacheSize(0, cache);
        }

        [Theory]
        [InlineData(6)]
        [InlineData(5)]
        [InlineData(2)]
        public void ReplaceOldEntryWithSameSizeOrLessNewEntryAtSizeLimitCapacity(int newValueSize)
        {
            var cache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 6
            });

            AssertCacheSize(0, cache);

            cache.Set("key", "oldValue", new MemoryCacheEntryOptions { Size = 6 });

            Assert.Equal("oldValue", cache.Get("key"));

            AssertCacheSize(6, cache);

            cache.Set("key", "newValue", new MemoryCacheEntryOptions { Size = newValueSize });

            Assert.Equal("newValue", cache.Get("key"));
            AssertCacheSize(newValueSize, cache);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/72912")]
        public async Task AddingReplacementWhenTotalSizeExceedsCapacityDoesNotUpdateRemovesOldEntryAndTriggersCompaction()
        {
            var cache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 10,
                CompactionPercentage = 0.5
            });

            var entryOptions = new MemoryCacheEntryOptions { Size = 6 };
            var sem = new SemaphoreSlim(0, 1);
            entryOptions.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration
            {
                EvictionCallback = (k, v, r, s) => sem.Release(),
                State = null
            });

            AssertCacheSize(0, cache);

            cache.Set("key", "value", entryOptions);

            Assert.Equal("value", cache.Get("key"));
            AssertCacheSize(6, cache);

            cache.Set("key", "value1", new MemoryCacheEntryOptions { Size = 5 });

            // Wait for compaction to complete
            Assert.True(await sem.WaitAsync(TimeSpan.FromSeconds(10)));

            Assert.Null(cache.Get("key"));
            AssertCacheSize(0, cache);
        }

        [Fact]
        public void AddingReplacementExceedsCapacityRemovesOldEntry()
        {
            var cache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 10,
                CompactionPercentage = 0.5
            });

            AssertCacheSize(0, cache);

            cache.Set("key", "value", new MemoryCacheEntryOptions { Size = 6 });

            Assert.Equal("value", cache.Get("key"));

            AssertCacheSize(6, cache);

            cache.Set("key", "value1", new MemoryCacheEntryOptions { Size = 11 });

            Assert.Null(cache.Get("key"));
            AssertCacheSize(0, cache); // addition was rejected due to size, and previous item with the same key removed
        }

        [Fact]
        public void RemovingEntryDecreasesCacheSize()
        {
            var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });

            cache.Set("key", "value", new MemoryCacheEntryOptions { Size = 5 });

            AssertCacheSize(5, cache);

            cache.Remove("key");

            AssertCacheSize(0, cache);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/72912")]
        public async Task ExpiringEntryDecreasesCacheSize()
        {
            var cache = new MemoryCache(new MemoryCacheOptions
            {
                ExpirationScanFrequency = TimeSpan.Zero,
                SizeLimit = 10
            });

            var entryOptions = new MemoryCacheEntryOptions { Size = 5 };
            var changeToken = new TestExpirationToken();
            var sem = new SemaphoreSlim(0, 1);
            entryOptions.ExpirationTokens.Add(changeToken);
            entryOptions.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration
            {
                EvictionCallback = (k, v, r, s) => sem.Release(),
                State = null
            });

            cache.Set("key", "value", entryOptions);

            AssertCacheSize(5, cache);

            // Expire entry
            changeToken.Fire();

            // Trigger compaction
            Assert.Null(cache.Get("key"));

            // Wait for compaction to complete
            Assert.True(await sem.WaitAsync(TimeSpan.FromSeconds(10)));

            AssertCacheSize(0, cache);
        }

        [Fact]
        public void TryingToAddExpiredEntryDoesNotIncreaseCacheSize()
        {
            var testClock = new TestClock();
            var cache = new MemoryCache(new MemoryCacheOptions { Clock = testClock, SizeLimit = 10 });

            var entryOptions = new MemoryCacheEntryOptions
            {
                Size = 5,
                AbsoluteExpiration = testClock.UtcNow.Add(TimeSpan.FromMinutes(-1))
            };

            cache.Set("key", "value", entryOptions);

            Assert.Null(cache.Get("key"));
            AssertCacheSize(0, cache);
        }

        [Fact]
        public void TryingToAddEntryWithExpiredTokenDoesNotIncreaseCacheSize()
        {
            var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });
            var testExpirationToken = new TestExpirationToken { HasChanged = true };
            var entryOptions = new MemoryCacheEntryOptions
            {
                Size = 5,
                ExpirationTokens = { testExpirationToken }
            };

            cache.Set("key", "value", entryOptions);

            Assert.Null(cache.Get("key"));
            AssertCacheSize(0, cache);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/72912")]
        public async Task CompactsToLessThanLowWatermarkUsingLRUWhenHighWatermarkExceeded()
        {
            var testClock = new TestClock();
            var cache = new MemoryCache(new MemoryCacheOptions
            {
                Clock = testClock,
                SizeLimit = 10,
                CompactionPercentage = 0.3
            });

            var numEntries = 5;
            var sem = new SemaphoreSlim(0, numEntries);

            for (var i = 0; i < numEntries; i++)
            {
                var entryOptions = new MemoryCacheEntryOptions { Size = i };
                entryOptions.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration
                {
                    EvictionCallback = (k, v, r, s) => sem.Release(),
                    State = null
                });
                cache.Set($"key{i}", $"value{i}", entryOptions);
                testClock.Add(TimeSpan.FromSeconds(1));
            }

            // There should be 5 items in the cache
            Assert.Equal(numEntries, cache.Count);

            cache.Set($"key{numEntries}", $"value{numEntries}", new MemoryCacheEntryOptions { Size = 1 });
            testClock.Add(TimeSpan.FromSeconds(10));

            // Wait for compaction to complete
            for (var i = 0; i < 3; i++)
            {
                Assert.True(await sem.WaitAsync(TimeSpan.FromSeconds(10)));
            }

            // There should be 2 items in the cache
            Assert.Equal(2, cache.Count);
            Assert.Null(cache.Get("key0"));
            Assert.Null(cache.Get("key1"));
            Assert.Null(cache.Get("key2"));
            Assert.NotNull(cache.Get("key3"));
            Assert.NotNull(cache.Get("key4"));
            Assert.Null(cache.Get("key5"));
        }

        [Fact]
        public void NoCompactionWhenNoMaximumEntriesCountSpecified()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());

            for (var i = 0; i < 5; i++)
            {
                cache.Set($"key{i}", $"value{i}", new MemoryCacheEntryOptions { Size = 1 });
            }

            // There should be 5 items in the cache
            Assert.Equal(5, cache.Count);

            cache.Set("key5", "value5", new MemoryCacheEntryOptions { Size = 1 });

            // There should be 6 items in the cache
            Assert.Equal(6, cache.Count);
        }

        [Fact]
        public void ClearZeroesTheSize()
        {
            var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });
            AssertCacheSize(0, cache);

            cache.Set("key", "value", new MemoryCacheEntryOptions { Size = 5 });
            AssertCacheSize(5, cache);

            cache.Clear();
            AssertCacheSize(0, cache);
            Assert.Equal(0, cache.Count);
        }

        internal static void AssertCacheSize(long size, MemoryCache cache)
        {
            // Size is only eventually consistent, so retry a few times. Note that the expected size must
            // be a constant. Reading it from the cache instead produces a stale snapshot that a
            // concurrent overcapacity compaction can move away from, and no number of retries will then
            // converge; use AssertEventually and re-read both sides inside the callback for that case.
            AssertEventually(() => Assert.Equal(size, cache.Size));
        }

        /// <summary>
        /// Retries <paramref name="assert"/> until the cache state it inspects settles. Every value the
        /// assertion depends on must be read inside the callback.
        /// </summary>
        internal static void AssertEventually(Action assert, [CallerMemberName] string? testName = null) =>
            RetryHelper.Execute(assert, maxAttempts: 12, (iteration) => (int)Math.Pow(2, iteration), testName: testName); // 2ms, 4ms.. 2048ms. In practice, retries are rarely needed.
    }
}
