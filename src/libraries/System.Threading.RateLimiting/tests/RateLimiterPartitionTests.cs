// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.Threading.RateLimiting.Tests
{
    public class RateLimiterPartitionTests
    {
        [Fact]
        public void Create_Concurrency()
        {
            var options = new ConcurrencyLimiterOptions
            {
                PermitLimit = 10,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            };
            var partition = RateLimitPartition.GetConcurrencyLimiter(1, key => options);

            var limiter = partition.Factory(1);
            var concurrencyLimiter = Assert.IsType<ConcurrencyLimiter>(limiter);
            Assert.Equal(options.PermitLimit, concurrencyLimiter.GetStatistics().CurrentAvailablePermits);
        }

        [Fact]
        public void Create_TokenBucket()
        {
            var options = new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                TokensPerPeriod = 1,
                AutoReplenishment = true
            };
            var partition = RateLimitPartition.GetTokenBucketLimiter(1, key => options);

            var limiter = partition.Factory(1);
            var tokenBucketLimiter = Assert.IsType<TokenBucketRateLimiter>(limiter);
            Assert.Equal(options.TokenLimit, tokenBucketLimiter.GetStatistics().CurrentAvailablePermits);
            Assert.Equal(options.ReplenishmentPeriod, tokenBucketLimiter.ReplenishmentPeriod);
            Assert.False(tokenBucketLimiter.IsAutoReplenishing);
        }

        [Fact]
        public async Task Create_NoLimiter()
        {
            var partition = RateLimitPartition.GetNoLimiter(1);

            var limiter = partition.Factory(1);

            // How do we test an internal implementation of a limiter that doesn't limit? Just try some stuff that normal limiters would probably block on and see if it works.
            var available = limiter.GetStatistics().CurrentAvailablePermits;
            var lease = limiter.AttemptAcquire(int.MaxValue);
            Assert.True(lease.IsAcquired);
            Assert.Equal(available, limiter.GetStatistics().CurrentAvailablePermits);

            lease = limiter.AttemptAcquire(int.MaxValue);
            Assert.True(lease.IsAcquired);

            var wait = limiter.AcquireAsync(int.MaxValue);
            Assert.True(wait.IsCompletedSuccessfully);
            lease = await wait;
            Assert.True(lease.IsAcquired);

            lease.Dispose();
        }

        [Fact]
        public async Task NoLimiter_GetStatistics()
        {
            var partition = RateLimitPartition.GetNoLimiter(1);

            var limiter = partition.Factory(1);

            var stats = limiter.GetStatistics();
            Assert.NotSame(stats, limiter.GetStatistics());
            Assert.Equal(long.MaxValue, stats.CurrentAvailablePermits);
            Assert.Equal(0, stats.CurrentQueuedCount);
            Assert.Equal(0, stats.TotalFailedLeases);
            Assert.Equal(0, stats.TotalSuccessfulLeases);

            var leaseCount = 0;
            for (var i = 0; i < 134; i++)
            {
                var lease = limiter.AttemptAcquire(i);
                Assert.True(lease.IsAcquired);
                ++leaseCount;
            }

            stats = limiter.GetStatistics();
            Assert.Equal(long.MaxValue, stats.CurrentAvailablePermits);
            Assert.Equal(0, stats.CurrentQueuedCount);
            Assert.Equal(0, stats.TotalFailedLeases);
            Assert.Equal(leaseCount, stats.TotalSuccessfulLeases);

            for (var i = 0; i < 165; i++)
            {
                var wait = limiter.AcquireAsync(int.MaxValue);
                Assert.True(wait.IsCompletedSuccessfully);
                var lease = await wait;
                Assert.True(lease.IsAcquired);
                ++leaseCount;
            }

            stats = limiter.GetStatistics();
            Assert.Equal(long.MaxValue, stats.CurrentAvailablePermits);
            Assert.Equal(0, stats.CurrentQueuedCount);
            Assert.Equal(0, stats.TotalFailedLeases);
            Assert.Equal(leaseCount, stats.TotalSuccessfulLeases);
        }

        [Fact]
        public void Create_AnyLimiter()
        {
            var partition = RateLimitPartition.Get(1, key => new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 10
            }));

            var limiter = partition.Factory(1);
            var concurrencyLimiter = Assert.IsType<ConcurrencyLimiter>(limiter);
            Assert.Equal(1, concurrencyLimiter.GetStatistics().CurrentAvailablePermits);

            var partition2 = RateLimitPartition.Get(1, key => new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 10,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(100),
                TokensPerPeriod = 1,
                AutoReplenishment = false
            }));
            limiter = partition2.Factory(1);
            var tokenBucketLimiter = Assert.IsType<TokenBucketRateLimiter>(limiter);
            Assert.Equal(1, tokenBucketLimiter.GetStatistics().CurrentAvailablePermits);
        }

        [Fact]
        public void Create_FixedWindow()
        {
            var options = new FixedWindowRateLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                AutoReplenishment = true
            };
            var partition = RateLimitPartition.GetFixedWindowLimiter(1, key => options);

            var limiter = partition.Factory(1);
            var fixedWindowLimiter = Assert.IsType<FixedWindowRateLimiter>(limiter);
            Assert.Equal(options.PermitLimit, fixedWindowLimiter.GetStatistics().CurrentAvailablePermits);
            Assert.Equal(options.Window, fixedWindowLimiter.ReplenishmentPeriod);
            Assert.False(fixedWindowLimiter.IsAutoReplenishing);
        }

        [Fact]
        public void Create_SlidingWindow()
        {
            var options = new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10,
                Window = TimeSpan.FromSeconds(33),
                SegmentsPerWindow = 3,
                AutoReplenishment = true
            };
            var partition = RateLimitPartition.GetSlidingWindowLimiter(1, key => options);

            var limiter = partition.Factory(1);
            var slidingWindowLimiter = Assert.IsType<SlidingWindowRateLimiter>(limiter);
            Assert.Equal(options.PermitLimit, slidingWindowLimiter.GetStatistics().CurrentAvailablePermits);
            Assert.Equal(TimeSpan.FromSeconds(11), slidingWindowLimiter.ReplenishmentPeriod);
            Assert.False(slidingWindowLimiter.IsAutoReplenishing);
        }
    }
}
