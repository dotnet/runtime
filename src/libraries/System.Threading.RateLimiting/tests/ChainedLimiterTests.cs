// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.RateLimiting.Tests
{
    public class ChainedLimiterTests
    {
        [Fact]
        public void ThrowsWhenNoLimitersProvided()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PartitionedRateLimiter.CreateChained<string>());
        }

        [Fact]
        public void ThrowsWhenNullPassedIn()
        {
            Assert.Throws<ArgumentNullException>(() => PartitionedRateLimiter.CreateChained<string>(null));
        }

        [Fact]
        public void AvailablePermitsReturnsLowestValue()
        {
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.CreateConcurrencyLimiter(1, _ => new ConcurrencyLimiterOptions(34, QueueProcessingOrder.NewestFirst, 0));
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.CreateConcurrencyLimiter(1, _ => new ConcurrencyLimiterOptions(22, QueueProcessingOrder.NewestFirst, 0));
            });
            using var limiter3 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.CreateConcurrencyLimiter(1, _ => new ConcurrencyLimiterOptions(13, QueueProcessingOrder.NewestFirst, 0));
            });

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2, limiter3);
            Assert.Equal(13, chainedLimiter.GetAvailablePermits(""));
        }

        [Fact]
        public void AvailablePermitsWithSingleLimiterWorks()
        {
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.CreateConcurrencyLimiter(1, _ => new ConcurrencyLimiterOptions(34, QueueProcessingOrder.NewestFirst, 0));
            });

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter);
            Assert.Equal(34, chainedLimiter.GetAvailablePermits(""));
        }

        [Fact]
        public void AcquireWorksWithSingleLimiter()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => limiterFactory.GetLimiter(key));
            });

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter);
            using var lease = chainedLimiter.Acquire("");

            Assert.True(lease.IsAcquired);
            Assert.Single(limiterFactory.Limiters);
            Assert.Equal(1, limiterFactory.Limiters[0].Key);
            Assert.Equal(1, limiterFactory.Limiters[0].Limiter.AcquireCallCount);
        }

        [Fact]
        public async Task WaitAsyncWorksWithSingleLimiter()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => limiterFactory.GetLimiter(key));
            });

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter);
            using var lease = await chainedLimiter.WaitAsync("");

            Assert.True(lease.IsAcquired);
            Assert.Single(limiterFactory.Limiters);
            Assert.Equal(1, limiterFactory.Limiters[0].Key);
            Assert.Equal(1, limiterFactory.Limiters[0].Limiter.WaitAsyncCallCount);
        }

        [Fact]
        public void AcquireWorksWithMultipleLimiters()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => limiterFactory.GetLimiter(key));
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(2, key => limiterFactory.GetLimiter(key));
            });

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);
            using var lease = chainedLimiter.Acquire("");

            Assert.True(lease.IsAcquired);
            Assert.Equal(2, limiterFactory.Limiters.Count);
            Assert.Equal(1, limiterFactory.Limiters[0].Key);
            Assert.Equal(1, limiterFactory.Limiters[0].Limiter.AcquireCallCount);
            Assert.Equal(2, limiterFactory.Limiters[1].Key);
            Assert.Equal(1, limiterFactory.Limiters[1].Limiter.AcquireCallCount);
        }

        [Fact]
        public async Task WaitAsyncWorksWithMultipleLimiters()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => limiterFactory.GetLimiter(key));
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(2, key => limiterFactory.GetLimiter(key));
            });

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);
            using var lease = await chainedLimiter.WaitAsync("");

            Assert.True(lease.IsAcquired);
            Assert.Equal(2, limiterFactory.Limiters.Count);
            Assert.Equal(1, limiterFactory.Limiters[0].Key);
            Assert.Equal(1, limiterFactory.Limiters[0].Limiter.WaitAsyncCallCount);
            Assert.Equal(2, limiterFactory.Limiters[1].Key);
            Assert.Equal(1, limiterFactory.Limiters[1].Limiter.WaitAsyncCallCount);
        }

        [Fact]
        public void AcquireLeaseCorrectlyDisposesWithMultipleLimiters()
        {
            var concurrencyLimiter1 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            var concurrencyLimiter2 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => concurrencyLimiter1);
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(2, key => concurrencyLimiter2);
            });

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);
            var lease = chainedLimiter.Acquire("");

            Assert.True(lease.IsAcquired);
            Assert.Equal(0, concurrencyLimiter1.GetAvailablePermits());
            Assert.Equal(0, concurrencyLimiter2.GetAvailablePermits());

            lease.Dispose();
            Assert.Equal(1, concurrencyLimiter1.GetAvailablePermits());
            Assert.Equal(1, concurrencyLimiter2.GetAvailablePermits());
        }

        [Fact]
        public async Task WaitAsyncLeaseCorrectlyDisposesWithMultipleLimiters()
        {
            var concurrencyLimiter1 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            var concurrencyLimiter2 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => concurrencyLimiter1);
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(2, key => concurrencyLimiter2);
            });

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);
            var lease = await chainedLimiter.WaitAsync("");

            Assert.True(lease.IsAcquired);
            Assert.Equal(0, concurrencyLimiter1.GetAvailablePermits());
            Assert.Equal(0, concurrencyLimiter2.GetAvailablePermits());

            lease.Dispose();
            Assert.Equal(1, concurrencyLimiter1.GetAvailablePermits());
            Assert.Equal(1, concurrencyLimiter2.GetAvailablePermits());
        }

        [Fact]
        public void AcquireLeaseCorrectlyDisposesWithSingleLimiter()
        {
            var concurrencyLimiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => concurrencyLimiter);
            });

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter);
            var lease = chainedLimiter.Acquire("");

            Assert.True(lease.IsAcquired);
            Assert.Equal(0, concurrencyLimiter.GetAvailablePermits());

            lease.Dispose();
            Assert.Equal(1, concurrencyLimiter.GetAvailablePermits());
        }

        [Fact]
        public async Task WaitAsyncLeaseCorrectlyDisposesWithSingleLimiter()
        {
            var concurrencyLimiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => concurrencyLimiter);
            });

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter);
            var lease = await chainedLimiter.WaitAsync("");

            Assert.True(lease.IsAcquired);
            Assert.Equal(0, concurrencyLimiter.GetAvailablePermits());

            lease.Dispose();
            Assert.Equal(1, concurrencyLimiter.GetAvailablePermits());
        }

        [Fact]
        public void AcquireFailsWhenOneLimiterDoesNotHaveEnoughResources()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var concurrencyLimiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => limiterFactory.GetLimiter(key));
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(2, key => concurrencyLimiter);
            });

            // Acquire the only permit on the ConcurrencyLimiter so the chained limiter fails when calling acquire
            var concurrencyLease = concurrencyLimiter.Acquire();

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);
            using var lease = chainedLimiter.Acquire("");

            Assert.False(lease.IsAcquired);
            Assert.Single(limiterFactory.Limiters);
            Assert.Equal(1, limiterFactory.Limiters[0].Key);
            Assert.Equal(1, limiterFactory.Limiters[0].Limiter.AcquireCallCount);
        }

        [Fact]
        public async Task WaitAsyncFailsWhenOneLimiterDoesNotHaveEnoughResources()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var concurrencyLimiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => limiterFactory.GetLimiter(key));
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(2, key => concurrencyLimiter);
            });

            // Acquire the only permit on the ConcurrencyLimiter so the chained limiter fails when calling acquire
            var concurrencyLease = await concurrencyLimiter.WaitAsync();

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);
            using var lease = chainedLimiter.Acquire("");

            Assert.False(lease.IsAcquired);
            Assert.Single(limiterFactory.Limiters);
            Assert.Equal(1, limiterFactory.Limiters[0].Key);
            Assert.Equal(1, limiterFactory.Limiters[0].Limiter.AcquireCallCount);
        }

        [Fact]
        public void AcquireFailsAndReleasesAcquiredResources()
        {
            using var concurrencyLimiter1 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            using var concurrencyLimiter2 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => concurrencyLimiter1);
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(2, key => concurrencyLimiter2);
            });

            // Acquire the only permit on the ConcurrencyLimiter so the chained limiter fails when calling acquire
            var concurrencyLease = concurrencyLimiter2.Acquire();

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);
            using var lease = chainedLimiter.Acquire("");

            Assert.False(lease.IsAcquired);
            Assert.Equal(1, concurrencyLimiter1.GetAvailablePermits());
        }

        [Fact]
        public async Task WaitAsyncFailsAndReleasesAcquiredResources()
        {
            using var concurrencyLimiter1 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            using var concurrencyLimiter2 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => concurrencyLimiter1);
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(2, key => concurrencyLimiter2);
            });

            // Acquire the only permit on the ConcurrencyLimiter so the chained limiter fails when calling acquire
            var concurrencyLease = await concurrencyLimiter2.WaitAsync();

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);
            using var lease = chainedLimiter.Acquire("");

            Assert.False(lease.IsAcquired);
            Assert.Equal(1, concurrencyLimiter1.GetAvailablePermits());
        }

        [Fact]
        public void AcquireThrowsAndReleasesAcquiredResources()
        {
            using var concurrencyLimiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => concurrencyLimiter);
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(2, key => new NotImplementedLimiter());
            });

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);
            var ex = Assert.Throws<AggregateException>(() => chainedLimiter.Acquire(""));
            Assert.Single(ex.InnerExceptions);
            Assert.IsType<NotImplementedException>(ex.InnerException);
            Assert.Equal(1, concurrencyLimiter.GetAvailablePermits());
        }

        [Fact]
        public async Task WaitAsyncThrowsAndReleasesAcquiredResources()
        {
            using var concurrencyLimiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => concurrencyLimiter);
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(2, key => new NotImplementedLimiter());
            });

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);
            var ex = await Assert.ThrowsAsync<AggregateException>(async () => await chainedLimiter.WaitAsync(""));
            Assert.Single(ex.InnerExceptions);
            Assert.IsType<NotImplementedException>(ex.InnerException);
            Assert.Equal(1, concurrencyLimiter.GetAvailablePermits());
        }

        [Fact]
        public void AcquireThrows_SingleLimiter()
        {
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => new NotImplementedLimiter());
            });

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1);
            var ex = Assert.Throws<AggregateException>(() => chainedLimiter.Acquire(""));
            Assert.Single(ex.InnerExceptions);
            Assert.IsType<NotImplementedException>(ex.InnerException);
        }

        [Fact]
        public async Task WaitAsyncThrows_SingleLimiter()
        {
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => new NotImplementedLimiter());
            });

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1);
            var ex = await Assert.ThrowsAsync<AggregateException>(async () => await chainedLimiter.WaitAsync(""));
            Assert.Single(ex.InnerExceptions);
            Assert.IsType<NotImplementedException>(ex.InnerException);
        }

        internal sealed class ThrowDisposeLease : RateLimitLease
        {
            public override bool IsAcquired => true;

            public override IEnumerable<string> MetadataNames => throw new NotImplementedException();

            public override bool TryGetMetadata(string metadataName, out object? metadata) => throw new NotImplementedException();

            protected override void Dispose(bool disposing) => throw new NotImplementedException();
        }

        [Fact]
        public void AcquireFailsDisposeThrows()
        {
            using var concurrencyLimiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => new CustomizableLimiter() { AcquireCoreImpl = _ => new ThrowDisposeLease() });
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => concurrencyLimiter);
            });

            var lease = concurrencyLimiter.Acquire();

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);
            var ex = Assert.Throws<AggregateException>(() => chainedLimiter.Acquire(""));
            Assert.Single(ex.InnerExceptions);
            Assert.IsType<NotImplementedException>(ex.InnerException);
        }

        [Fact]
        public async Task WaitAsyncFailsDisposeThrows()
        {
            using var concurrencyLimiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => new CustomizableLimiter() { AcquireCoreImpl = _ => new ThrowDisposeLease() });
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => concurrencyLimiter);
            });

            var lease = await concurrencyLimiter.WaitAsync();

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);
            var ex = Assert.Throws<AggregateException>(() => chainedLimiter.Acquire(""));
            Assert.Single(ex.InnerExceptions);
            Assert.IsType<NotImplementedException>(ex.InnerException);
        }

        [Fact]
        public void AcquireFailsDisposeThrowsMultipleLimitersThrow()
        {
            using var concurrencyLimiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => new CustomizableLimiter() { AcquireCoreImpl = _ => new ThrowDisposeLease() });
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => new CustomizableLimiter() { AcquireCoreImpl = _ => new ThrowDisposeLease() });
            });
            using var limiter3 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => concurrencyLimiter);
            });

            var lease = concurrencyLimiter.Acquire();

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2, limiter3);
            var ex = Assert.Throws<AggregateException>(() => chainedLimiter.Acquire(""));
            Assert.Equal(2, ex.InnerExceptions.Count);
            Assert.IsType<NotImplementedException>(ex.InnerExceptions[0]);
            Assert.IsType<NotImplementedException>(ex.InnerExceptions[1]);
        }

        [Fact]
        public async Task WaitAsyncFailsDisposeThrowsMultipleLimitersThrow()
        {
            using var concurrencyLimiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => new CustomizableLimiter() { WaitAsyncCoreImpl = (_, _) => new ValueTask<RateLimitLease>(new ThrowDisposeLease()) });
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => new CustomizableLimiter() { WaitAsyncCoreImpl = (_, _) => new ValueTask<RateLimitLease>(new ThrowDisposeLease()) });
            });
            using var limiter3 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => concurrencyLimiter);
            });

            var lease = concurrencyLimiter.Acquire();

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2, limiter3);
            var ex = await Assert.ThrowsAsync<AggregateException>(async () => await chainedLimiter.WaitAsync(""));
            Assert.Equal(2, ex.InnerExceptions.Count);
            Assert.IsType<NotImplementedException>(ex.InnerExceptions[0]);
            Assert.IsType<NotImplementedException>(ex.InnerExceptions[1]);
        }

        [Fact]
        public void AcquireThrowsDisposeThrowsMultipleLimitersThrow()
        {
            using var concurrencyLimiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => new CustomizableLimiter() { AcquireCoreImpl = _ => new ThrowDisposeLease() });
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => new CustomizableLimiter() { AcquireCoreImpl = _ => new ThrowDisposeLease() });
            });
            using var limiter3 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => new NotImplementedLimiter());
            });

            var lease = concurrencyLimiter.Acquire();

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2, limiter3);
            var ex = Assert.Throws<AggregateException>(() => chainedLimiter.Acquire(""));
            Assert.Equal(3, ex.InnerExceptions.Count);
            Assert.IsType<NotImplementedException>(ex.InnerExceptions[0]);
            Assert.IsType<NotImplementedException>(ex.InnerExceptions[1]);
            Assert.IsType<NotImplementedException>(ex.InnerExceptions[2]);
        }

        [Fact]
        public async Task WaitAsyncThrowsDisposeThrowsMultipleLimitersThrow()
        {
            using var concurrencyLimiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => new CustomizableLimiter() { WaitAsyncCoreImpl = (_, _) => new ValueTask<RateLimitLease>(new ThrowDisposeLease()) });
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => new CustomizableLimiter() { WaitAsyncCoreImpl = (_, _) => new ValueTask<RateLimitLease>(new ThrowDisposeLease()) });
            });
            using var limiter3 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => new NotImplementedLimiter());
            });

            var lease = concurrencyLimiter.Acquire();

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2, limiter3);
            var ex = await Assert.ThrowsAsync<AggregateException>(async () => await chainedLimiter.WaitAsync(""));
            Assert.Equal(3, ex.InnerExceptions.Count);
            Assert.IsType<NotImplementedException>(ex.InnerExceptions[0]);
            Assert.IsType<NotImplementedException>(ex.InnerExceptions[1]);
            Assert.IsType<NotImplementedException>(ex.InnerExceptions[2]);
        }

        [Fact]
        public void AcquireSucceedsDisposeThrowsAndReleasesResources()
        {
            using var concurrencyLimiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => new CustomizableLimiter() { AcquireCoreImpl = _ => new ThrowDisposeLease() });
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => concurrencyLimiter);
            });

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);
            var lease = chainedLimiter.Acquire("");
            Assert.True(lease.IsAcquired);
            Assert.Equal(0, concurrencyLimiter.GetAvailablePermits());
            var ex = Assert.Throws<AggregateException>(() => lease.Dispose());
            Assert.Single(ex.InnerExceptions);
            Assert.IsType<NotImplementedException>(ex.InnerException);

            Assert.Equal(1, concurrencyLimiter.GetAvailablePermits());
        }

        [Fact]
        public async Task WaitAsyncSucceedsDisposeThrowsAndReleasesResources()
        {
            using var concurrencyLimiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => new CustomizableLimiter() { WaitAsyncCoreImpl = (_, _) => new ValueTask<RateLimitLease>(new ThrowDisposeLease()) });
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => concurrencyLimiter);
            });

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);
            var lease = await chainedLimiter.WaitAsync("");
            Assert.True(lease.IsAcquired);
            Assert.Equal(0, concurrencyLimiter.GetAvailablePermits());
            var ex = Assert.Throws<AggregateException>(() => lease.Dispose());
            Assert.Single(ex.InnerExceptions);
            Assert.IsType<NotImplementedException>(ex.InnerException);

            Assert.Equal(1, concurrencyLimiter.GetAvailablePermits());
        }

        [Fact]
        public void AcquireForwardsCorrectPermitCount()
        {
            using var concurrencyLimiter1 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(5, QueueProcessingOrder.OldestFirst, 0));
            using var concurrencyLimiter2 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(3, QueueProcessingOrder.OldestFirst, 0));
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => concurrencyLimiter1);
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => concurrencyLimiter2);
            });
            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);

            var lease = chainedLimiter.Acquire("", 3);
            Assert.True(lease.IsAcquired);
            Assert.Equal(2, concurrencyLimiter1.GetAvailablePermits());
            Assert.Equal(0, concurrencyLimiter2.GetAvailablePermits());

            lease.Dispose();
            Assert.Equal(5, concurrencyLimiter1.GetAvailablePermits());
            Assert.Equal(3, concurrencyLimiter2.GetAvailablePermits());
        }

        [Fact]
        public async Task WaitAsyncForwardsCorrectPermitCount()
        {
            using var concurrencyLimiter1 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(5, QueueProcessingOrder.OldestFirst, 0));
            using var concurrencyLimiter2 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(3, QueueProcessingOrder.OldestFirst, 0));
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => concurrencyLimiter1);
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => concurrencyLimiter2);
            });
            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);

            var lease = await chainedLimiter.WaitAsync("", 3);
            Assert.True(lease.IsAcquired);
            Assert.Equal(2, concurrencyLimiter1.GetAvailablePermits());
            Assert.Equal(0, concurrencyLimiter2.GetAvailablePermits());

            lease.Dispose();
            Assert.Equal(5, concurrencyLimiter1.GetAvailablePermits());
            Assert.Equal(3, concurrencyLimiter2.GetAvailablePermits());
        }

        [Fact]
        public void AcquireForwardsCorrectResourceID()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                if (resource == "1")
                {
                    return RateLimitPartition.Create(1, key => limiterFactory.GetLimiter(key));
                }
                return RateLimitPartition.Create(2, key => limiterFactory.GetLimiter(key));
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                if (resource == "1")
                {
                    return RateLimitPartition.Create(3, key => limiterFactory.GetLimiter(key));
                }
                return RateLimitPartition.Create(4, key => limiterFactory.GetLimiter(key));
            });
            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);

            var lease = chainedLimiter.Acquire("1");
            Assert.True(lease.IsAcquired);
            Assert.Equal(2, limiterFactory.Limiters.Count);
            Assert.Equal(1, limiterFactory.Limiters[0].Key);
            Assert.Equal(3, limiterFactory.Limiters[1].Key);
        }

        [Fact]
        public async Task WaitAsyncForwardsCorrectResourceID()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                if (resource == "1")
                {
                    return RateLimitPartition.Create(1, key => limiterFactory.GetLimiter(key));
                }
                return RateLimitPartition.Create(2, key => limiterFactory.GetLimiter(key));
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                if (resource == "1")
                {
                    return RateLimitPartition.Create(3, key => limiterFactory.GetLimiter(key));
                }
                return RateLimitPartition.Create(4, key => limiterFactory.GetLimiter(key));
            });
            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);

            var lease = await chainedLimiter.WaitAsync("1");
            Assert.True(lease.IsAcquired);
            Assert.Equal(2, limiterFactory.Limiters.Count);
            Assert.Equal(1, limiterFactory.Limiters[0].Key);
            Assert.Equal(3, limiterFactory.Limiters[1].Key);
        }

        [Fact]
        public async Task WaitAsyncCanBeCanceled()
        {
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.CreateConcurrencyLimiter(1, key => new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 1));
            });
            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter);

            var lease = chainedLimiter.Acquire("");
            Assert.True(lease.IsAcquired);

            var cts = new CancellationTokenSource();
            var task = chainedLimiter.WaitAsync("", 1, cts.Token);

            cts.Cancel();
            var ex = await Assert.ThrowsAsync<AggregateException>(async () => await task);
            Assert.Equal(1, ex.InnerExceptions.Count);
            Assert.IsType<TaskCanceledException>(ex.InnerException);
        }

        [Fact]
        public async Task WaitAsyncCanceledReleasesAcquiredResources()
        {
            var concurrencyLimiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(2, QueueProcessingOrder.OldestFirst, 0));
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => concurrencyLimiter);
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.CreateConcurrencyLimiter(1, key => new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 1));
            });
            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);

            var lease = chainedLimiter.Acquire("");
            Assert.True(lease.IsAcquired);
            Assert.Equal(1, concurrencyLimiter.GetAvailablePermits());

            var cts = new CancellationTokenSource();
            var task = chainedLimiter.WaitAsync("", 1, cts.Token);

            Assert.Equal(0, concurrencyLimiter.GetAvailablePermits());
            cts.Cancel();
            var ex = await Assert.ThrowsAsync<AggregateException>(async () => await task);
            Assert.Equal(1, ex.InnerExceptions.Count);
            Assert.IsType<TaskCanceledException>(ex.InnerException);
            Assert.Equal(1, concurrencyLimiter.GetAvailablePermits());
        }

        [Fact]
        public async Task WaitAsyncWaitsForResourcesBeforeCallingNextLimiter()
        {
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.CreateConcurrencyLimiter(1, key => new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 1));
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                // 0 queue limit to verify this isn't called while the previous limiter is waiting for resource(s)
                // as it would return a failed lease when no queue is available
                return RateLimitPartition.CreateConcurrencyLimiter(1, key => new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            });
            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);

            var lease = chainedLimiter.Acquire("");
            Assert.True(lease.IsAcquired);

            var task = chainedLimiter.WaitAsync("");
            Assert.False(task.IsCompleted);

            lease.Dispose();
            lease = await task;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public void LeasesAreDisposedInReverseOrder()
        {
            var customizableLimiter1 = new CustomizableLimiter();
            var customizableLimiter2 = new CustomizableLimiter();
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => customizableLimiter1);
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => customizableLimiter2);
            });
            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);

            var customizableLease1 = new CustomizableLease();
            var disposeCalled = false;
            customizableLease1.DisposeImpl = _ =>
            {
                Assert.True(disposeCalled);
            };
            customizableLimiter1.AcquireCoreImpl = _ => customizableLease1;

            var customizableLease2 = new CustomizableLease();
            customizableLease2.DisposeImpl = _ =>
            {
                disposeCalled = true;
            };
            customizableLimiter2.AcquireCoreImpl = _ => customizableLease2;

            var lease = chainedLimiter.Acquire("");
            Assert.True(lease.IsAcquired);

            lease.Dispose();
        }

        [Fact]
        public void LeasesAreDisposedInReverseOrderWhenAcquireThrows()
        {
            var customizableLimiter1 = new CustomizableLimiter();
            var customizableLimiter2 = new CustomizableLimiter();
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => customizableLimiter1);
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => customizableLimiter2);
            });
            using var limiter3 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => new NotImplementedLimiter());
            });
            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2, limiter3);

            var customizableLease1 = new CustomizableLease();
            var disposeCalled = false;
            customizableLease1.DisposeImpl = _ =>
            {
                Assert.True(disposeCalled);
            };
            customizableLimiter1.AcquireCoreImpl = _ => customizableLease1;

            var customizableLease2 = new CustomizableLease();
            customizableLease2.DisposeImpl = _ =>
            {
                disposeCalled = true;
            };
            customizableLimiter2.AcquireCoreImpl = _ => customizableLease2;

            var ex = Assert.Throws<AggregateException>(() => chainedLimiter.Acquire(""));
            Assert.Single(ex.InnerExceptions);
            Assert.IsType<NotImplementedException>(ex.InnerException);
        }

        [Fact]
        public async Task LeasesAreDisposedInReverseOrderWhenWaitAsyncThrows()
        {
            var customizableLimiter1 = new CustomizableLimiter();
            var customizableLimiter2 = new CustomizableLimiter();
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => customizableLimiter1);
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => customizableLimiter2);
            });
            using var limiter3 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => new NotImplementedLimiter());
            });
            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2, limiter3);

            var customizableLease1 = new CustomizableLease();
            var disposeCalled = false;
            customizableLease1.DisposeImpl = _ =>
            {
                Assert.True(disposeCalled);
            };
            customizableLimiter1.WaitAsyncCoreImpl = (_, _) => new ValueTask<RateLimitLease>(customizableLease1);

            var customizableLease2 = new CustomizableLease();
            customizableLease2.DisposeImpl = _ =>
            {
                disposeCalled = true;
            };
            customizableLimiter2.WaitAsyncCoreImpl = (_, _) => new ValueTask<RateLimitLease>(customizableLease2);

            var ex = await Assert.ThrowsAsync<AggregateException>(async () => await chainedLimiter.WaitAsync(""));
            Assert.Single(ex.InnerExceptions);
            Assert.IsType<NotImplementedException>(ex.InnerException);
        }
    }
}
