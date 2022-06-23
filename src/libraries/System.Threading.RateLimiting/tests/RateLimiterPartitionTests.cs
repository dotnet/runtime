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
            var options = new ConcurrencyLimiterOptions(10, QueueProcessingOrder.OldestFirst, 10);
            var partition = RateLimitPartition.CreateConcurrencyLimiter(1, key => options);

            var factoryProperty = typeof(RateLimitPartition<int>).GetField("Factory", Reflection.BindingFlags.NonPublic | Reflection.BindingFlags.Instance)!;
            var factory = (Func<int, RateLimiter>)factoryProperty.GetValue(partition);
            var limiter = factory(1);
            var concurrencyLimiter = Assert.IsType<ConcurrencyLimiter>(limiter);
            Assert.Equal(options.PermitLimit, concurrencyLimiter.GetAvailablePermits());
        }

        [Fact]
        public void Create_TokenBucket()
        {
            var options = new TokenBucketRateLimiterOptions(1, QueueProcessingOrder.OldestFirst, 10, TimeSpan.FromMinutes(1), 1, true);
            var partition = RateLimitPartition.CreateTokenBucketLimiter(1, key => options);

            var factoryProperty = typeof(RateLimitPartition<int>).GetField("Factory", Reflection.BindingFlags.NonPublic | Reflection.BindingFlags.Instance)!;
            var factory = (Func<int, RateLimiter>)factoryProperty.GetValue(partition);
            var limiter = factory(1);
            var tokenBucketLimiter = Assert.IsType<TokenBucketRateLimiter>(limiter);
            Assert.Equal(options.TokenLimit, tokenBucketLimiter.GetAvailablePermits());
            Assert.Equal(options.ReplenishmentPeriod, tokenBucketLimiter.ReplenishmentPeriod);
            Assert.False(tokenBucketLimiter.IsAutoReplenishing);
        }

        [Fact]
        public async Task Create_NoLimiter()
        {
            var partition = RateLimitPartition.CreateNoLimiter(1);

            var factoryProperty = typeof(RateLimitPartition<int>).GetField("Factory", Reflection.BindingFlags.NonPublic | Reflection.BindingFlags.Instance)!;
            var factory = (Func<int, RateLimiter>)factoryProperty.GetValue(partition);
            var limiter = factory(1);

            // How do we test an internal implementation of a limiter that doesn't limit? Just try some stuff that normal limiters would probably block on and see if it works.
            var available = limiter.GetAvailablePermits();
            var lease = limiter.Acquire(int.MaxValue);
            Assert.True(lease.IsAcquired);
            Assert.Equal(available, limiter.GetAvailablePermits());

            lease = limiter.Acquire(int.MaxValue);
            Assert.True(lease.IsAcquired);

            var wait = limiter.WaitAsync(int.MaxValue);
            Assert.True(wait.IsCompletedSuccessfully);
            lease = await wait;
            Assert.True(lease.IsAcquired);

            lease.Dispose();
        }

        [Fact]
        public void Create_AnyLimiter()
        {
            var partition = RateLimitPartition.Create(1, key => new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.NewestFirst, 10)));

            var factoryProperty = typeof(RateLimitPartition<int>).GetField("Factory", Reflection.BindingFlags.NonPublic | Reflection.BindingFlags.Instance)!;
            var factory = (Func<int, RateLimiter>)factoryProperty.GetValue(partition);
            var limiter = factory(1);
            var concurrencyLimiter = Assert.IsType<ConcurrencyLimiter>(limiter);
            Assert.Equal(1, concurrencyLimiter.GetAvailablePermits());

            var partition2 = RateLimitPartition.Create(1, key => new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions(1, QueueProcessingOrder.NewestFirst, 10, TimeSpan.FromMilliseconds(100), 1, autoReplenishment: false)));
            factory = (Func<int, RateLimiter>)factoryProperty.GetValue(partition2);
            limiter = factory(1);
            var tokenBucketLimiter = Assert.IsType<TokenBucketRateLimiter>(limiter);
            Assert.Equal(1, tokenBucketLimiter.GetAvailablePermits());
        }

        [Fact]
        public void Create_FixedWindow()
        {
            var options = new FixedWindowRateLimiterOptions(1, QueueProcessingOrder.OldestFirst, 10, TimeSpan.FromMinutes(1), true);
            var partition = RateLimitPartition.CreateFixedWindowLimiter(1, key => options);

            var factoryProperty = typeof(RateLimitPartition<int>).GetField("Factory", Reflection.BindingFlags.NonPublic | Reflection.BindingFlags.Instance)!;
            var factory = (Func<int, RateLimiter>)factoryProperty.GetValue(partition);
            var limiter = factory(1);
            var fixedWindowLimiter = Assert.IsType<FixedWindowRateLimiter>(limiter);
            Assert.Equal(options.PermitLimit, fixedWindowLimiter.GetAvailablePermits());
            Assert.Equal(options.Window, fixedWindowLimiter.ReplenishmentPeriod);
            Assert.False(fixedWindowLimiter.IsAutoReplenishing);
        }

        [Fact]
        public void Create_SlidingWindow()
        {
            var options = new SlidingWindowRateLimiterOptions(1, QueueProcessingOrder.OldestFirst, 10, TimeSpan.FromSeconds(33), 3, true);
            var partition = RateLimitPartition.CreateSlidingWindowLimiter(1, key => options);

            var factoryProperty = typeof(RateLimitPartition<int>).GetField("Factory", Reflection.BindingFlags.NonPublic | Reflection.BindingFlags.Instance)!;
            var factory = (Func<int, RateLimiter>)factoryProperty.GetValue(partition);
            var limiter = factory(1);
            var slidingWindowLimiter = Assert.IsType<SlidingWindowRateLimiter>(limiter);
            Assert.Equal(options.PermitLimit, slidingWindowLimiter.GetAvailablePermits());
            Assert.Equal(TimeSpan.FromSeconds(11), slidingWindowLimiter.ReplenishmentPeriod);
            Assert.False(slidingWindowLimiter.IsAutoReplenishing);
        }
    }
}
