// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.Caching.Memory
{
    public class MemoryCacheHasStatisticsTests
    {
        [Fact]
        public void GetCurrentStatistics_Basic()
        {
            var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });
            MemoryCacheStatistics stats = cache.GetCurrentStatistics();

            Assert.Equal(0, stats.CurrentSize);
            Assert.Equal(0, stats.CurrentEntryCount);
            Assert.Equal(0, stats.TotalRequests);
            Assert.Equal(0, stats.TotalHits);

            cache.Set("key", "value", new MemoryCacheEntryOptions { Size = 2 });
            Assert.Equal("value", cache.Get("key"));
            stats = cache.GetCurrentStatistics();

            Assert.Equal(2, stats.CurrentSize);
            Assert.Equal(1, stats.CurrentEntryCount);
            Assert.Equal(1, stats.TotalRequests);
            Assert.Equal(1, stats.TotalHits);

            cache.Set("key", "value1", new MemoryCacheEntryOptions { Size = 3 });
            Assert.Equal("value1", cache.Get("key"));
            stats = cache.GetCurrentStatistics();

            Assert.Equal(3, stats.CurrentSize);
            Assert.Equal(1, stats.CurrentEntryCount);
            Assert.Equal(2, stats.TotalRequests);
            Assert.Equal(2, stats.TotalHits);
        }

        // TODO: add more tests
    }
}
