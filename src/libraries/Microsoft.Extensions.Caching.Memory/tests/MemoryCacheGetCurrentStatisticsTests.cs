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
        [Fact]
        public void GetCurrentStatistics_TrackStatisticsFalse_ReturnsNull()
        {
            var cache = new MemoryCache(new MemoryCacheOptions { TrackStatistics = false });
            Assert.Null(cache.GetCurrentStatistics());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetCurrentStatistics_GetCache_UpdatesStatistics(bool sizeLimitIsSet)
        {
            var cache = sizeLimitIsSet ? 
                new MemoryCache(new MemoryCacheOptions { TrackStatistics = true, SizeLimit = 10 }) :
                new MemoryCache(new MemoryCacheOptions { TrackStatistics = true });

            cache.Set("key", "value", new MemoryCacheEntryOptions { Size = 2 });
            for (int i = 0; i < 100; i++)
            {
                Assert.Equal("value", cache.Get("key"));
                Assert.Null(cache.Get("missingKey1"));            
                Assert.Null(cache.Get("missingKey2"));            
            }

            MemoryCacheStatistics? stats = cache.GetCurrentStatistics();

            Assert.NotNull(stats);
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
                new MemoryCache(new MemoryCacheOptions { TrackStatistics = true, SizeLimit = 10 }) :
                new MemoryCache(new MemoryCacheOptions { TrackStatistics = true });

            cache.Set("key", "value", new MemoryCacheEntryOptions { Size = 2 });
            Assert.Equal("value", cache.Get("key"));

            cache.Set("key", "updated value", new MemoryCacheEntryOptions { Size = 3 });
            Assert.Equal("updated value", cache.Get("key"));

            MemoryCacheStatistics? stats = cache.GetCurrentStatistics();

            Assert.NotNull(stats);
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
                new MemoryCacheOptions { TrackStatistics = true, Clock = new SystemClock(), SizeLimit = 10 } :
                new MemoryCacheOptions { TrackStatistics = true, Clock = new SystemClock() }
            );

            ICacheEntry entry;
            using (entry = cache.CreateEntry(Key))
            {
                var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };
                var mc = new MemoryCacheEntryOptions { Size = 5 };
                cache.Set(Key, new object(), mc.AddExpirationToken(expirationToken));
                MemoryCacheStatistics? stats = cache.GetCurrentStatistics();

                Assert.NotNull(stats);
                Assert.Equal(1,  cache.Count);
                Assert.Equal(1,  stats.CurrentEntryCount);
                VerifyCurrentEstimatedSize(5, sizeLimitIsSet, stats);

                expirationToken.HasChanged = true;
                cache.Set(Key, new object(), mc.AddExpirationToken(expirationToken));
                stats = cache.GetCurrentStatistics();

                Assert.NotNull(stats);
                Assert.Equal(0,  cache.Count);
                Assert.Equal(0,  stats.CurrentEntryCount);
                VerifyCurrentEstimatedSize(0, sizeLimitIsSet, stats);
            }
        }

#if NET6_0_OR_GREATER
        [Fact]
        public void GetCurrentStatistics_DIMReturnsNull()
        {
            Assert.Null((new FakeMemoryCache() as IMemoryCache).GetCurrentStatistics());
        }
#endif

        private class FakeMemoryCache : IMemoryCache
        {
            public ICacheEntry CreateEntry(object key) => throw new NotImplementedException();
            public void Dispose() => throw new NotImplementedException();
            public void Remove(object key) => throw new NotImplementedException();
            public bool TryGetValue(object key, out object? value) => throw new NotImplementedException();
        }

        private void VerifyCurrentEstimatedSize(long expected, bool sizeLimitIsSet, MemoryCacheStatistics stats)
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
