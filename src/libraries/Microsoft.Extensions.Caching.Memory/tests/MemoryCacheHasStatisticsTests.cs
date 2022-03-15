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
        public void GetCurrentStatistics_Basic(bool sizeLimitIsSet)
        {
            var cache = sizeLimitIsSet ? 
                new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 }) :
                new MemoryCache(new MemoryCacheOptions { });

            MemoryCacheStatistics stats = cache.GetCurrentStatistics();

            Assert.Equal(0, stats.CurrentEntryCount);
            Assert.Equal(0, stats.TotalRequests);
            Assert.Equal(0, stats.TotalHits);
            VerifyCurrentSize(0, sizeLimitIsSet, stats);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetCurrentStatistics_GetCache_UpdatesStatistics(bool sizeLimitIsSet)
        {
            var cache = sizeLimitIsSet ? 
                new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 }) :
                new MemoryCache(new MemoryCacheOptions { });

            cache.Set("key", "value", new MemoryCacheEntryOptions { Size = 2 });
            Assert.Equal("value", cache.Get("key"));
            MemoryCacheStatistics stats = cache.GetCurrentStatistics();

            Assert.Equal(1, stats.CurrentEntryCount);
            Assert.Equal(1, stats.TotalRequests);
            Assert.Equal(1, stats.TotalHits);
            VerifyCurrentSize(2, sizeLimitIsSet, stats);
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
            Assert.Equal(2, stats.TotalRequests);
            Assert.Equal(2, stats.TotalHits);
            VerifyCurrentSize(3, sizeLimitIsSet, stats);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetCurrentStatistics_UpdateAfterExistingItemExpired_CurrentSizeResets(bool sizeLimitIsSet)
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
                VerifyCurrentSize(5, sizeLimitIsSet, stats);

                expirationToken.HasChanged = true;
                cache.Set(Key, new object(), mc.AddExpirationToken(expirationToken));
                stats = cache.GetCurrentStatistics();
                Assert.Equal(0,  cache.Count);
                Assert.Equal(0,  stats.CurrentEntryCount);
                VerifyCurrentSize(0, sizeLimitIsSet, stats);
            }
        }

        // TODO: add more tests

        private void VerifyCurrentSize(long expected, bool sizeLimitIsSet, MemoryCacheStatistics stats)
        {
            if (sizeLimitIsSet)
            {
                Assert.Equal(expected, stats.CurrentSize);
            }
            else
            {
                Assert.Null(stats.CurrentSize);
            }
        }
    }
}
