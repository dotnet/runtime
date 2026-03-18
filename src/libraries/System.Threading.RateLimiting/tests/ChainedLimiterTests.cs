// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.RateLimiting.Tests
{
    public class ChainedLimiterTests
    {
        [Fact]
        public void ThrowsWhenNoLimitersProvided()
        {
            Assert.Throws<ArgumentException>(() => RateLimiter.CreateChained());
            Assert.Throws<ArgumentException>(() => RateLimiter.CreateChained(new RateLimiter[0]));
        }

        [Fact]
        public void ThrowsWhenNullPassedIn()
        {
            Assert.Throws<ArgumentNullException>(() => RateLimiter.CreateChained(null));
        }

        [Fact]
        public async Task DisposeMakesMethodsThrow()
        {
            using var limiter1 = new CustomizableLimiter();
            using var limiter2 = new CustomizableLimiter();
            var chainedLimiter = RateLimiter.CreateChained(limiter1, limiter2);

            chainedLimiter.Dispose();

            Assert.Throws<ObjectDisposedException>(() => chainedLimiter.GetStatistics());
            Assert.Throws<ObjectDisposedException>(() => chainedLimiter.IdleDuration);
            Assert.Throws<ObjectDisposedException>(() => chainedLimiter.AttemptAcquire());
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await chainedLimiter.AcquireAsync());
        }

        [Fact]
        public async Task DisposeAsyncMakesMethodsThrow()
        {
            using var limiter1 = new CustomizableLimiter();
            using var limiter2 = new CustomizableLimiter();
            var chainedLimiter = RateLimiter.CreateChained(limiter1, limiter2);

            await chainedLimiter.DisposeAsync();

            Assert.Throws<ObjectDisposedException>(() => chainedLimiter.GetStatistics());
            Assert.Throws<ObjectDisposedException>(() => chainedLimiter.IdleDuration);
            Assert.Throws<ObjectDisposedException>(() => chainedLimiter.AttemptAcquire());
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await chainedLimiter.AcquireAsync());
        }

        [Fact]
        public void ArrayChangesAreIgnored()
        {
            using var limiter1 = new CustomizableLimiter { IdleDurationImpl = () => TimeSpan.FromMilliseconds(1) };
            using var limiter2 = new CustomizableLimiter { IdleDurationImpl = () => TimeSpan.FromMilliseconds(2) };
            var limiters = new RateLimiter[] { limiter1 };
            var chainedLimiter = RateLimiter.CreateChained(limiters);

            limiters[0] = limiter2;

            var idleDuration = chainedLimiter.IdleDuration;
            Assert.Equal(1, idleDuration.Value.TotalMilliseconds);
        }

        [Fact]
        public void GetStatisticsReturnsLowestOrAggregateValues()
        {
            using var limiter1 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 34,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 4
            });
            using var limiter2 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 22,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 2
            });
            using var limiter3 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 13,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 10
            });

            using var chainedLimiter = RateLimiter.CreateChained(limiter1, limiter2, limiter3);

            var stats = chainedLimiter.GetStatistics();
            Assert.Equal(13, stats.CurrentAvailablePermits);
            Assert.Equal(0, stats.CurrentQueuedCount);
            Assert.Equal(0, stats.TotalFailedLeases);
            Assert.Equal(0, stats.TotalSuccessfulLeases);
        }

        [Fact]
        public void GetStatisticsWithSingleLimiterWorks()
        {
            using var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 34,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 4
            });

            using var chainedLimiter = RateLimiter.CreateChained(limiter);

            var stats = chainedLimiter.GetStatistics();
            Assert.Equal(34, stats.CurrentAvailablePermits);
            Assert.Equal(0, stats.CurrentQueuedCount);
            Assert.Equal(0, stats.TotalFailedLeases);
            Assert.Equal(0, stats.TotalSuccessfulLeases);
        }

        [Fact]
        public void GetStatisticsReturnsNewInstances()
        {
            using var limiter1 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 34,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 4
            });
            using var limiter2 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 22,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 2
            });
            using var limiter3 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 13,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 10
            });

            using var chainedLimiter = RateLimiter.CreateChained(limiter1, limiter2, limiter3);

            var stats = chainedLimiter.GetStatistics();
            var stats2 = chainedLimiter.GetStatistics();
            Assert.NotSame(stats, stats2);
        }

        [Fact]
        public async Task GetStatisticsHasCorrectValues()
        {
            using var limiter1 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 34,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 4
            });
            using var limiter2 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 22,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 2
            });
            using var limiter3 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 13,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 10
            });

            using var chainedLimiter = RateLimiter.CreateChained(limiter1, limiter2, limiter3);

            var lease = chainedLimiter.AttemptAcquire(10);
            var stats = chainedLimiter.GetStatistics();

            Assert.Equal(3, stats.CurrentAvailablePermits);
            Assert.Equal(0, stats.CurrentQueuedCount);
            Assert.Equal(1, stats.TotalSuccessfulLeases);
            Assert.Equal(0, stats.TotalFailedLeases);

            var lease2 = chainedLimiter.AttemptAcquire(10);
            Assert.False(lease2.IsAcquired);
            stats = chainedLimiter.GetStatistics();

            Assert.Equal(3, stats.CurrentAvailablePermits);
            Assert.Equal(0, stats.CurrentQueuedCount);
            Assert.Equal(1, stats.TotalSuccessfulLeases);
            Assert.Equal(1, stats.TotalFailedLeases);

            var task = chainedLimiter.AcquireAsync(10);
            Assert.False(task.IsCompleted);
            stats = chainedLimiter.GetStatistics();

            Assert.Equal(2, stats.CurrentAvailablePermits);
            Assert.Equal(10, stats.CurrentQueuedCount);
            Assert.Equal(1, stats.TotalSuccessfulLeases);
            Assert.Equal(1, stats.TotalFailedLeases);

            lease.Dispose();

            lease = await task;
            Assert.True(lease.IsAcquired);
            stats = chainedLimiter.GetStatistics();

            Assert.Equal(3, stats.CurrentAvailablePermits);
            Assert.Equal(0, stats.CurrentQueuedCount);
            Assert.Equal(2, stats.TotalSuccessfulLeases);
            Assert.Equal(1, stats.TotalFailedLeases);
        }

        [Fact]
        public void IdleDurationReturnsLowestValue()
        {
            using var limiter1 = new CustomizableLimiter();
            using var limiter2 = new CustomizableLimiter { IdleDurationImpl = () => TimeSpan.FromMilliseconds(2) };
            using var limiter3 = new CustomizableLimiter { IdleDurationImpl = () => TimeSpan.FromMilliseconds(3) };

            using var chainedLimiter = RateLimiter.CreateChained(limiter1, limiter2, limiter3);

            var idleDuration = chainedLimiter.IdleDuration;
            Assert.Equal(2, idleDuration.Value.TotalMilliseconds);
        }

        [Fact]
        public void AcquireWorksWithSingleLimiter()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var limiter = limiterFactory.GetLimiter(1);

            using var chainedLimiter = RateLimiter.CreateChained(limiter);
            using var lease = chainedLimiter.AttemptAcquire();

            Assert.True(lease.IsAcquired);
            Assert.Single(limiterFactory.Limiters);
            Assert.Equal(1, limiterFactory.Limiters[0].Key);
            Assert.Equal(1, limiterFactory.Limiters[0].Limiter.AcquireCallCount);
        }

        [Fact]
        public async Task AcquireAsyncWorksWithSingleLimiter()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var limiter = limiterFactory.GetLimiter(1);

            using var chainedLimiter = RateLimiter.CreateChained(limiter);
            using var lease = await chainedLimiter.AcquireAsync();

            Assert.True(lease.IsAcquired);
            Assert.Single(limiterFactory.Limiters);
            Assert.Equal(1, limiterFactory.Limiters[0].Key);
            Assert.Equal(1, limiterFactory.Limiters[0].Limiter.AcquireAsyncCallCount);
        }

        [Fact]
        public void AcquireWorksWithMultipleLimiters()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var limiter1 = limiterFactory.GetLimiter(1);
            using var limiter2 = limiterFactory.GetLimiter(2);

            using var chainedLimiter = RateLimiter.CreateChained(limiter1, limiter2);
            using var lease = chainedLimiter.AttemptAcquire();

            Assert.True(lease.IsAcquired);
            Assert.Equal(2, limiterFactory.Limiters.Count);
            Assert.Equal(1, limiterFactory.Limiters[0].Key);
            Assert.Equal(1, limiterFactory.Limiters[0].Limiter.AcquireCallCount);
            Assert.Equal(2, limiterFactory.Limiters[1].Key);
            Assert.Equal(1, limiterFactory.Limiters[1].Limiter.AcquireCallCount);
        }

        [Fact]
        public async Task AcquireAsyncWorksWithMultipleLimiters()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var limiter1 = limiterFactory.GetLimiter(1);
            using var limiter2 = limiterFactory.GetLimiter(2);

            using var chainedLimiter = RateLimiter.CreateChained(limiter1, limiter2);
            using var lease = await chainedLimiter.AcquireAsync();

            Assert.True(lease.IsAcquired);
            Assert.Equal(2, limiterFactory.Limiters.Count);
            Assert.Equal(1, limiterFactory.Limiters[0].Key);
            Assert.Equal(1, limiterFactory.Limiters[0].Limiter.AcquireAsyncCallCount);
            Assert.Equal(2, limiterFactory.Limiters[1].Key);
            Assert.Equal(1, limiterFactory.Limiters[1].Limiter.AcquireAsyncCallCount);
        }

        [Fact]
        public void AcquireLeaseCorrectlyDisposesWithMultipleLimiters()
        {
            var limiter1 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
            var limiter2 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });

            using var chainedLimiter = RateLimiter.CreateChained(limiter1, limiter2);
            var lease = chainedLimiter.AttemptAcquire();

            Assert.True(lease.IsAcquired);
            Assert.Equal(0, limiter1.GetStatistics().CurrentAvailablePermits);
            Assert.Equal(0, limiter2.GetStatistics().CurrentAvailablePermits);

            lease.Dispose();
            Assert.Equal(1, limiter1.GetStatistics().CurrentAvailablePermits);
            Assert.Equal(1, limiter2.GetStatistics().CurrentAvailablePermits);
        }

        [Fact]
        public async Task AcquireAsyncLeaseCorrectlyDisposesWithMultipleLimiters()
        {
            var limiter1 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
            var limiter2 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });

            using var chainedLimiter = RateLimiter.CreateChained(limiter1, limiter2);
            var lease = await chainedLimiter.AcquireAsync();

            Assert.True(lease.IsAcquired);
            Assert.Equal(0, limiter1.GetStatistics().CurrentAvailablePermits);
            Assert.Equal(0, limiter2.GetStatistics().CurrentAvailablePermits);

            lease.Dispose();
            Assert.Equal(1, limiter1.GetStatistics().CurrentAvailablePermits);
            Assert.Equal(1, limiter2.GetStatistics().CurrentAvailablePermits);
        }

        [Fact]
        public void AcquireLeaseCorrectlyDisposesWithSingleLimiter()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });

            using var chainedLimiter = RateLimiter.CreateChained(limiter);
            var lease = chainedLimiter.AttemptAcquire();

            Assert.True(lease.IsAcquired);
            Assert.Equal(0, limiter.GetStatistics().CurrentAvailablePermits);

            lease.Dispose();
            Assert.Equal(1, limiter.GetStatistics().CurrentAvailablePermits);
        }

        [Fact]
        public async Task AcquireAsyncLeaseCorrectlyDisposesWithSingleLimiter()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });

            using var chainedLimiter = RateLimiter.CreateChained(limiter);
            var lease = await chainedLimiter.AcquireAsync();

            Assert.True(lease.IsAcquired);
            Assert.Equal(0, limiter.GetStatistics().CurrentAvailablePermits);

            lease.Dispose();
            Assert.Equal(1, limiter.GetStatistics().CurrentAvailablePermits);
        }

        [Fact]
        public void AcquireFailsWhenOneLimiterDoesNotHaveEnoughResources()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var limiter1 = limiterFactory.GetLimiter(1);
            using var limiter2 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });

            // Acquire the only permit on the ConcurrencyLimiter so the chained limiter fails when calling acquire
            var concurrencyLease = limiter2.AttemptAcquire();

            using var chainedLimiter = RateLimiter.CreateChained(limiter1, limiter2);
            using var lease = chainedLimiter.AttemptAcquire();

            Assert.False(lease.IsAcquired);
            Assert.Single(limiterFactory.Limiters);
            Assert.Equal(1, limiterFactory.Limiters[0].Key);
            Assert.Equal(1, limiterFactory.Limiters[0].Limiter.AcquireCallCount);
        }

        [Fact]
        public async Task AcquireAsyncFailsWhenOneLimiterDoesNotHaveEnoughResources()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var limiter1 = limiterFactory.GetLimiter(1);
            using var limiter2 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });

            // Acquire the only permit on the ConcurrencyLimiter so the chained limiter fails when calling acquire
            var concurrencyLease = await limiter2.AcquireAsync();

            using var chainedLimiter = RateLimiter.CreateChained(limiter1, limiter2);
            using var lease = chainedLimiter.AttemptAcquire();

            Assert.False(lease.IsAcquired);
            Assert.Single(limiterFactory.Limiters);
            Assert.Equal(1, limiterFactory.Limiters[0].Key);
            Assert.Equal(1, limiterFactory.Limiters[0].Limiter.AcquireCallCount);
        }

        [Fact]
        public void AcquireFailsAndReleasesAcquiredResources()
        {
            using var limiter1 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
            using var limiter2 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });

            // Acquire the only permit on the ConcurrencyLimiter so the chained limiter fails when calling acquire
            var concurrencyLease = limiter2.AttemptAcquire();

            using var chainedLimiter = RateLimiter.CreateChained(limiter1, limiter2);
            using var lease = chainedLimiter.AttemptAcquire();

            Assert.False(lease.IsAcquired);
            Assert.Equal(1, limiter1.GetStatistics().CurrentAvailablePermits);
        }

        [Fact]
        public async Task AcquireAsyncFailsAndReleasesAcquiredResources()
        {
            using var limiter1 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
            using var limiter2 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });

            // Acquire the only permit on the ConcurrencyLimiter so the chained limiter fails when calling acquire
            var concurrencyLease = await limiter2.AcquireAsync();

            using var chainedLimiter = RateLimiter.CreateChained(limiter1, limiter2);
            using var lease = chainedLimiter.AttemptAcquire();

            Assert.False(lease.IsAcquired);
            Assert.Equal(1, limiter1.GetStatistics().CurrentAvailablePermits);
        }

        [Fact]
        public void AcquireThrowsAndReleasesAcquiredResources()
        {
            using var limiter1 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
            using var limiter2 = new NotImplementedLimiter();

            using var chainedLimiter = RateLimiter.CreateChained(limiter1, limiter2);
            Assert.Throws<NotImplementedException>(() => chainedLimiter.AttemptAcquire());
            Assert.Equal(1, limiter1.GetStatistics().CurrentAvailablePermits);
        }

        [Fact]
        public async Task AcquireAsyncThrowsAndReleasesAcquiredResources()
        {
            using var limiter1 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
            using var limiter2 = new NotImplementedLimiter();

            using var chainedLimiter = RateLimiter.CreateChained(limiter1, limiter2);
            await Assert.ThrowsAsync<NotImplementedException>(async () => await chainedLimiter.AcquireAsync());
            Assert.Equal(1, limiter1.GetStatistics().CurrentAvailablePermits);
        }

        [Fact]
        public void AcquireThrows_SingleLimiter()
        {
            using var limiter1 = new NotImplementedLimiter();

            using var chainedLimiter = RateLimiter.CreateChained(limiter1);
            Assert.Throws<NotImplementedException>(() => chainedLimiter.AttemptAcquire());
        }

        [Fact]
        public async Task AcquireAsyncThrows_SingleLimiter()
        {
            using var limiter1 = new NotImplementedLimiter();

            using var chainedLimiter = RateLimiter.CreateChained(limiter1);
            await Assert.ThrowsAsync<NotImplementedException>(async () => await chainedLimiter.AcquireAsync());
        }

        [Fact]
        public void AcquireFailsDisposeThrows()
        {
            using var limiter1 = new CustomizableLimiter() { AttemptAcquireCoreImpl = _ => new ThrowDisposeLease() };
            using var limiter2 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });

            var lease = limiter2.AttemptAcquire();

            using var chainedLimiter = RateLimiter.CreateChained(limiter1, limiter2);
            var ex = Assert.Throws<AggregateException>(() => chainedLimiter.AttemptAcquire());
            Assert.Single(ex.InnerExceptions);
            Assert.IsType<NotImplementedException>(ex.InnerException);
        }

        [Fact]
        public async Task AcquireAsyncFailsDisposeThrows()
        {
            using var limiter1 = new CustomizableLimiter() { AttemptAcquireCoreImpl = _ => new ThrowDisposeLease() };
            using var limiter2 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });

            var lease = await limiter2.AcquireAsync();

            using var chainedLimiter = RateLimiter.CreateChained(limiter1, limiter2);
            var ex = Assert.Throws<AggregateException>(() => chainedLimiter.AttemptAcquire());
            Assert.Single(ex.InnerExceptions);
            Assert.IsType<NotImplementedException>(ex.InnerException);
        }

        [Fact]
        public void AcquireFailsDisposeThrowsMultipleLimitersThrow()
        {
            using var limiter1 = new CustomizableLimiter() { AttemptAcquireCoreImpl = _ => new ThrowDisposeLease() };
            using var limiter2 = new CustomizableLimiter() { AttemptAcquireCoreImpl = _ => new ThrowDisposeLease() };
            using var limiter3 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });

            var lease = limiter3.AttemptAcquire();

            using var chainedLimiter = RateLimiter.CreateChained(limiter1, limiter2, limiter3);
            var ex = Assert.Throws<AggregateException>(() => chainedLimiter.AttemptAcquire());
            Assert.Equal(2, ex.InnerExceptions.Count);
            Assert.IsType<NotImplementedException>(ex.InnerExceptions[0]);
            Assert.IsType<NotImplementedException>(ex.InnerExceptions[1]);
        }

        [Fact]
        public async Task AcquireAsyncFailsDisposeThrowsMultipleLimitersThrow()
        {
            using var limiter1 = new CustomizableLimiter() { AcquireAsyncCoreImpl = (_, _) => new ValueTask<RateLimitLease>(new ThrowDisposeLease()) };
            using var limiter2 = new CustomizableLimiter() { AcquireAsyncCoreImpl = (_, _) => new ValueTask<RateLimitLease>(new ThrowDisposeLease()) };
            using var limiter3 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });

            var lease = limiter3.AttemptAcquire();

            using var chainedLimiter = RateLimiter.CreateChained(limiter1, limiter2, limiter3);
            var ex = await Assert.ThrowsAsync<AggregateException>(async () => await chainedLimiter.AcquireAsync());
            Assert.Equal(2, ex.InnerExceptions.Count);
            Assert.IsType<NotImplementedException>(ex.InnerExceptions[0]);
            Assert.IsType<NotImplementedException>(ex.InnerExceptions[1]);
        }

        [Fact]
        public void AcquireThrowsDisposeThrowsMultipleLimitersThrow()
        {
            using var limiter1 = new CustomizableLimiter() { AttemptAcquireCoreImpl = _ => new ThrowDisposeLease() };
            using var limiter2 = new CustomizableLimiter() { AttemptAcquireCoreImpl = _ => new ThrowDisposeLease() };
            using var limiter3 = new NotImplementedLimiter();

            using var chainedLimiter = RateLimiter.CreateChained(limiter1, limiter2, limiter3);
            var ex = Assert.Throws<AggregateException>(() => chainedLimiter.AttemptAcquire());
            Assert.Equal(3, ex.InnerExceptions.Count);
            Assert.IsType<NotImplementedException>(ex.InnerExceptions[0]);
            Assert.IsType<NotImplementedException>(ex.InnerExceptions[1]);
            Assert.IsType<NotImplementedException>(ex.InnerExceptions[2]);
        }

        [Fact]
        public async Task AcquireAsyncThrowsDisposeThrowsMultipleLimitersThrow()
        {
            using var limiter1 = new CustomizableLimiter() { AcquireAsyncCoreImpl = (_, _) => new ValueTask<RateLimitLease>(new ThrowDisposeLease()) };
            using var limiter2 = new CustomizableLimiter() { AcquireAsyncCoreImpl = (_, _) => new ValueTask<RateLimitLease>(new ThrowDisposeLease()) };
            using var limiter3 = new NotImplementedLimiter();

            using var chainedLimiter = RateLimiter.CreateChained(limiter1, limiter2, limiter3);
            var ex = await Assert.ThrowsAsync<AggregateException>(async () => await chainedLimiter.AcquireAsync());
            Assert.Equal(3, ex.InnerExceptions.Count);
            Assert.IsType<NotImplementedException>(ex.InnerExceptions[0]);
            Assert.IsType<NotImplementedException>(ex.InnerExceptions[1]);
            Assert.IsType<NotImplementedException>(ex.InnerExceptions[2]);
        }

        [Fact]
        public void AcquireSucceedsDisposeThrowsAndReleasesResources()
        {
            using var limiter1 = new CustomizableLimiter() { AttemptAcquireCoreImpl = _ => new ThrowDisposeLease() };
            using var limiter2 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });

            using var chainedLimiter = RateLimiter.CreateChained(limiter1, limiter2);
            var lease = chainedLimiter.AttemptAcquire();
            Assert.True(lease.IsAcquired);
            Assert.Equal(0, limiter2.GetStatistics().CurrentAvailablePermits);
            var ex = Assert.Throws<AggregateException>(() => lease.Dispose());
            Assert.Single(ex.InnerExceptions);
            Assert.IsType<NotImplementedException>(ex.InnerException);

            Assert.Equal(1, limiter2.GetStatistics().CurrentAvailablePermits);
        }

        [Fact]
        public async Task AcquireAsyncSucceedsDisposeThrowsAndReleasesResources()
        {
            using var limiter1 = new CustomizableLimiter() { AcquireAsyncCoreImpl = (_, _) => new ValueTask<RateLimitLease>(new ThrowDisposeLease()) };
            using var limiter2 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });

            using var chainedLimiter = RateLimiter.CreateChained(limiter1, limiter2);
            var lease = await chainedLimiter.AcquireAsync();
            Assert.True(lease.IsAcquired);
            Assert.Equal(0, limiter2.GetStatistics().CurrentAvailablePermits);
            var ex = Assert.Throws<AggregateException>(() => lease.Dispose());
            Assert.Single(ex.InnerExceptions);
            Assert.IsType<NotImplementedException>(ex.InnerException);

            Assert.Equal(1, limiter2.GetStatistics().CurrentAvailablePermits);
        }

        [Fact]
        public void AcquireForwardsCorrectPermitCount()
        {
            using var limiter1 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 5,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
            using var limiter2 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 3,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
            using var chainedLimiter = RateLimiter.CreateChained(limiter1, limiter2);

            var lease = chainedLimiter.AttemptAcquire(3);
            Assert.True(lease.IsAcquired);
            Assert.Equal(2, limiter1.GetStatistics().CurrentAvailablePermits);
            Assert.Equal(0, limiter2.GetStatistics().CurrentAvailablePermits);

            lease.Dispose();
            Assert.Equal(5, limiter1.GetStatistics().CurrentAvailablePermits);
            Assert.Equal(3, limiter2.GetStatistics().CurrentAvailablePermits);
        }

        [Fact]
        public async Task AcquireAsyncForwardsCorrectPermitCount()
        {
            using var limiter1 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 5,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
            using var limiter2 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 3,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
            using var chainedLimiter = RateLimiter.CreateChained(limiter1, limiter2);

            var lease = await chainedLimiter.AcquireAsync(3);
            Assert.True(lease.IsAcquired);
            Assert.Equal(2, limiter1.GetStatistics().CurrentAvailablePermits);
            Assert.Equal(0, limiter2.GetStatistics().CurrentAvailablePermits);

            lease.Dispose();
            Assert.Equal(5, limiter1.GetStatistics().CurrentAvailablePermits);
            Assert.Equal(3, limiter2.GetStatistics().CurrentAvailablePermits);
        }

        [Fact]
        public async Task AcquireAsyncCanBeCanceled()
        {
            using var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1
            });
            using var chainedLimiter = RateLimiter.CreateChained(limiter);

            var lease = chainedLimiter.AttemptAcquire();
            Assert.True(lease.IsAcquired);

            var cts = new CancellationTokenSource();
            var task = chainedLimiter.AcquireAsync(1, cts.Token);

            cts.Cancel();
             await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
        }

        [Fact]
        public async Task AcquireAsyncCanceledReleasesAcquiredResources()
        {
            var limiter1 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
            var limiter2 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1
            });
            using var chainedLimiter = RateLimiter.CreateChained(limiter1, limiter2);

            var lease = chainedLimiter.AttemptAcquire();
            Assert.True(lease.IsAcquired);
            Assert.Equal(1, limiter1.GetStatistics().CurrentAvailablePermits);

            var cts = new CancellationTokenSource();
            var task = chainedLimiter.AcquireAsync(1, cts.Token);

            Assert.Equal(0, limiter1.GetStatistics().CurrentAvailablePermits);
            cts.Cancel();
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
            Assert.Equal(1, limiter1.GetStatistics().CurrentAvailablePermits);
        }

        [Fact]
        public async Task AcquireAsyncWaitsForResourcesBeforeCallingNextLimiter()
        {
            var limiter1 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1
            });
            // 0 queue limit to verify this isn't called while the previous limiter is waiting for resource(s)
            // as it would return a failed lease when no queue is available
            var limiter2 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
            
            using var chainedLimiter = RateLimiter.CreateChained(limiter1, limiter2);

            var lease = chainedLimiter.AttemptAcquire();
            Assert.True(lease.IsAcquired);

            var task = chainedLimiter.AcquireAsync();
            Assert.False(task.IsCompleted);

            lease.Dispose();
            lease = await task;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public void LeasesAreDisposedInReverseOrder()
        {
            var limiter1 = new CustomizableLimiter();
            var limiter2 = new CustomizableLimiter();
            using var chainedLimiter = RateLimiter.CreateChained(limiter1, limiter2);

            var customizableLease1 = new CustomizableLease();
            var disposeCalled = false;
            customizableLease1.DisposeImpl = _ =>
            {
                Assert.True(disposeCalled);
            };
            limiter1.AttemptAcquireCoreImpl = _ => customizableLease1;

            var customizableLease2 = new CustomizableLease();
            customizableLease2.DisposeImpl = _ =>
            {
                disposeCalled = true;
            };
            limiter2.AttemptAcquireCoreImpl = _ => customizableLease2;

            var lease = chainedLimiter.AttemptAcquire();
            Assert.True(lease.IsAcquired);

            lease.Dispose();
        }

        [Fact]
        public void LeasesAreDisposedInReverseOrderWhenAcquireThrows()
        {
            var limiter1 = new CustomizableLimiter();
            var limiter2 = new CustomizableLimiter();
            var limiter3 = new NotImplementedLimiter();
            using var chainedLimiter = RateLimiter.CreateChained(limiter1, limiter2, limiter3);

            var customizableLease1 = new CustomizableLease();
            var disposeCalled = false;
            customizableLease1.DisposeImpl = _ =>
            {
                Assert.True(disposeCalled);
            };
            limiter1.AttemptAcquireCoreImpl = _ => customizableLease1;

            var customizableLease2 = new CustomizableLease();
            customizableLease2.DisposeImpl = _ =>
            {
                disposeCalled = true;
            };
            limiter2.AttemptAcquireCoreImpl = _ => customizableLease2;

            Assert.Throws<NotImplementedException>(() => chainedLimiter.AttemptAcquire());
        }

        [Fact]
        public async Task LeasesAreDisposedInReverseOrderWhenAcquireAsyncThrows()
        {
            var limiter1 = new CustomizableLimiter();
            var limiter2 = new CustomizableLimiter();
            var limiter3 = new NotImplementedLimiter();
            using var chainedLimiter = RateLimiter.CreateChained(limiter1, limiter2, limiter3);

            var customizableLease1 = new CustomizableLease();
            var disposeCalled = false;
            customizableLease1.DisposeImpl = _ =>
            {
                Assert.True(disposeCalled);
            };
            limiter1.AcquireAsyncCoreImpl = (_, _) => new ValueTask<RateLimitLease>(customizableLease1);

            var customizableLease2 = new CustomizableLease();
            customizableLease2.DisposeImpl = _ =>
            {
                disposeCalled = true;
            };
            limiter2.AcquireAsyncCoreImpl = (_, _) => new ValueTask<RateLimitLease>(customizableLease2);

            await Assert.ThrowsAsync<NotImplementedException>(async () => await chainedLimiter.AcquireAsync());
        }

        [Fact]
        public void MetadataIsCombined()
        {
            var limiter1 = new CustomizableLimiter();
            limiter1.AttemptAcquireCoreImpl = _ => new CustomizableLease()
            {
                MetadataNamesImpl = () =>
                {
                    return new[] { "1", "2" };
                },
                TryGetMetadataImpl = (string name, out object? metadata) =>
                {
                    if (name == "1")
                    {
                        metadata = new DateTime();
                        return true;
                    }
                    if (name == "2")
                    {
                        metadata = new TimeSpan();
                        return true;
                    }
                    metadata = null;
                    return false;
                }
            };
            var limiter2 = new CustomizableLimiter();
            limiter2.AttemptAcquireCoreImpl = _ => new CustomizableLease()
            {
                MetadataNamesImpl = () =>
                {
                    return new[] { "3", "4" };
                },
                TryGetMetadataImpl = (string name, out object? metadata) =>
                {
                    if (name == "3")
                    {
                        metadata = new Exception();
                        return true;
                    }
                    if (name == "4")
                    {
                        metadata = new List<int>();
                        return true;
                    }
                    metadata = null;
                    return false;
                }
            };
            using var chainedLimiter = RateLimiter.CreateChained(limiter1, limiter2);

            var lease = chainedLimiter.AttemptAcquire();

            var metaDataNames = lease.MetadataNames.ToArray();
            Assert.Equal(4, metaDataNames.Length);
            Assert.Contains("1", metaDataNames);
            Assert.Contains("2", metaDataNames);
            Assert.Contains("3", metaDataNames);
            Assert.Contains("4", metaDataNames);

            Assert.True(lease.TryGetMetadata("1", out var obj));
            Assert.IsType<DateTime>(obj);
            Assert.True(lease.TryGetMetadata("2", out obj));
            Assert.IsType<TimeSpan>(obj);
            Assert.True(lease.TryGetMetadata("3", out obj));
            Assert.IsType<Exception>(obj);
            Assert.True(lease.TryGetMetadata("4", out obj));
            Assert.IsType<List<int>>(obj);
        }

        [Fact]
        public void DuplicateMetadataUsesFirstOne()
        {
            var limiter1 = new CustomizableLimiter();
            limiter1.AttemptAcquireCoreImpl = _ => new CustomizableLease()
            {
                MetadataNamesImpl = () =>
                {
                    return new[] { "1", "2" };
                },
                TryGetMetadataImpl = (string name, out object? metadata) =>
                {
                    if (name == "1")
                    {
                        metadata = new DateTime();
                        return true;
                    }
                    if (name == "2")
                    {
                        metadata = new TimeSpan();
                        return true;
                    }
                    metadata = null;
                    return false;
                }
            };
            var limiter2 = new CustomizableLimiter();
            limiter2.AttemptAcquireCoreImpl = _ => new CustomizableLease()
            {
                MetadataNamesImpl = () =>
                {
                    return new[] { "1", "3" };
                },
                TryGetMetadataImpl = (string name, out object? metadata) =>
                {
                    // duplicate metadata name, previous one will win
                    if (name == "1")
                    {
                        metadata = new Exception();
                        return true;
                    }
                    if (name == "3")
                    {
                        metadata = new List<int>();
                        return true;
                    }
                    metadata = null;
                    return false;
                }
            };
            using var chainedLimiter = RateLimiter.CreateChained(limiter1, limiter2);

            var lease = chainedLimiter.AttemptAcquire();

            var metadataNames = lease.MetadataNames.ToArray();
            Assert.Equal(3, metadataNames.Length);
            Assert.Contains("1", metadataNames);
            Assert.Contains("2", metadataNames);
            Assert.Contains("3", metadataNames);

            Assert.True(lease.TryGetMetadata("1", out var obj));
            Assert.IsType<DateTime>(obj);
            Assert.True(lease.TryGetMetadata("2", out obj));
            Assert.IsType<TimeSpan>(obj);
            Assert.True(lease.TryGetMetadata("3", out obj));
            Assert.IsType<List<int>>(obj);
        }
    }
}
