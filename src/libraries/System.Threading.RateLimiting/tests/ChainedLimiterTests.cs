﻿// Licensed to the .NET Foundation under one or more agreements.
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
            Assert.Throws<ArgumentException>(() => PartitionedRateLimiter.CreateChained<string>());
            Assert.Throws<ArgumentException>(() => PartitionedRateLimiter.CreateChained<string>(new PartitionedRateLimiter<string>[0]));
        }

        [Fact]
        public void ThrowsWhenNullPassedIn()
        {
            Assert.Throws<ArgumentNullException>(() => PartitionedRateLimiter.CreateChained<string>(null));
        }

        [Fact]
        public async Task DisposeMakesMethodsThrow()
        {
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.GetConcurrencyLimiter(1, _ => new ConcurrencyLimiterOptions(1, QueueProcessingOrder.NewestFirst, 0));
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.GetConcurrencyLimiter(1, _ => new ConcurrencyLimiterOptions(1, QueueProcessingOrder.NewestFirst, 0));
            });
            var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);

            chainedLimiter.Dispose();

            Assert.Throws<ObjectDisposedException>(() => chainedLimiter.GetAvailablePermits(""));
            Assert.Throws<ObjectDisposedException>(() => chainedLimiter.Acquire(""));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await chainedLimiter.WaitAndAcquireAsync(""));
        }

        [Fact]
        public async Task DisposeAsyncMakesMethodsThrow()
        {
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.GetConcurrencyLimiter(1, _ => new ConcurrencyLimiterOptions(1, QueueProcessingOrder.NewestFirst, 0));
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.GetConcurrencyLimiter(1, _ => new ConcurrencyLimiterOptions(1, QueueProcessingOrder.NewestFirst, 0));
            });
            var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);

            await chainedLimiter.DisposeAsync();

            Assert.Throws<ObjectDisposedException>(() => chainedLimiter.GetAvailablePermits(""));
            Assert.Throws<ObjectDisposedException>(() => chainedLimiter.Acquire(""));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await chainedLimiter.WaitAndAcquireAsync(""));
        }

        [Fact]
        public void AvailablePermitsReturnsLowestValue()
        {
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.GetConcurrencyLimiter(1, _ => new ConcurrencyLimiterOptions(34, QueueProcessingOrder.NewestFirst, 0));
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.GetConcurrencyLimiter(1, _ => new ConcurrencyLimiterOptions(22, QueueProcessingOrder.NewestFirst, 0));
            });
            using var limiter3 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.GetConcurrencyLimiter(1, _ => new ConcurrencyLimiterOptions(13, QueueProcessingOrder.NewestFirst, 0));
            });

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2, limiter3);
            Assert.Equal(13, chainedLimiter.GetAvailablePermits(""));
        }

        [Fact]
        public void AvailablePermitsWithSingleLimiterWorks()
        {
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.GetConcurrencyLimiter(1, _ => new ConcurrencyLimiterOptions(34, QueueProcessingOrder.NewestFirst, 0));
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
                return RateLimitPartition.Get(1, key => limiterFactory.GetLimiter(key));
            });

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter);
            using var lease = chainedLimiter.Acquire("");

            Assert.True(lease.IsAcquired);
            Assert.Single(limiterFactory.Limiters);
            Assert.Equal(1, limiterFactory.Limiters[0].Key);
            Assert.Equal(1, limiterFactory.Limiters[0].Limiter.AcquireCallCount);
        }

        [Fact]
        public async Task WaitAndAcquireAsyncWorksWithSingleLimiter()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => limiterFactory.GetLimiter(key));
            });

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter);
            using var lease = await chainedLimiter.WaitAndAcquireAsync("");

            Assert.True(lease.IsAcquired);
            Assert.Single(limiterFactory.Limiters);
            Assert.Equal(1, limiterFactory.Limiters[0].Key);
            Assert.Equal(1, limiterFactory.Limiters[0].Limiter.WaitAndAcquireAsyncCallCount);
        }

        [Fact]
        public void AcquireWorksWithMultipleLimiters()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => limiterFactory.GetLimiter(key));
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(2, key => limiterFactory.GetLimiter(key));
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
        public async Task WaitAndAcquireAsyncWorksWithMultipleLimiters()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => limiterFactory.GetLimiter(key));
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(2, key => limiterFactory.GetLimiter(key));
            });

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);
            using var lease = await chainedLimiter.WaitAndAcquireAsync("");

            Assert.True(lease.IsAcquired);
            Assert.Equal(2, limiterFactory.Limiters.Count);
            Assert.Equal(1, limiterFactory.Limiters[0].Key);
            Assert.Equal(1, limiterFactory.Limiters[0].Limiter.WaitAndAcquireAsyncCallCount);
            Assert.Equal(2, limiterFactory.Limiters[1].Key);
            Assert.Equal(1, limiterFactory.Limiters[1].Limiter.WaitAndAcquireAsyncCallCount);
        }

        [Fact]
        public void AcquireLeaseCorrectlyDisposesWithMultipleLimiters()
        {
            var concurrencyLimiter1 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            var concurrencyLimiter2 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => concurrencyLimiter1);
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(2, key => concurrencyLimiter2);
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
        public async Task WaitAndAcquireAsyncLeaseCorrectlyDisposesWithMultipleLimiters()
        {
            var concurrencyLimiter1 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            var concurrencyLimiter2 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => concurrencyLimiter1);
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(2, key => concurrencyLimiter2);
            });

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);
            var lease = await chainedLimiter.WaitAndAcquireAsync("");

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
                return RateLimitPartition.Get(1, key => concurrencyLimiter);
            });

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter);
            var lease = chainedLimiter.Acquire("");

            Assert.True(lease.IsAcquired);
            Assert.Equal(0, concurrencyLimiter.GetAvailablePermits());

            lease.Dispose();
            Assert.Equal(1, concurrencyLimiter.GetAvailablePermits());
        }

        [Fact]
        public async Task WaitAndAcquireAsyncLeaseCorrectlyDisposesWithSingleLimiter()
        {
            var concurrencyLimiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => concurrencyLimiter);
            });

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter);
            var lease = await chainedLimiter.WaitAndAcquireAsync("");

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
                return RateLimitPartition.Get(1, key => limiterFactory.GetLimiter(key));
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(2, key => concurrencyLimiter);
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
        public async Task WaitAndAcquireAsyncFailsWhenOneLimiterDoesNotHaveEnoughResources()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var concurrencyLimiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => limiterFactory.GetLimiter(key));
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(2, key => concurrencyLimiter);
            });

            // Acquire the only permit on the ConcurrencyLimiter so the chained limiter fails when calling acquire
            var concurrencyLease = await concurrencyLimiter.WaitAndAcquireAsync();

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
                return RateLimitPartition.Get(1, key => concurrencyLimiter1);
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(2, key => concurrencyLimiter2);
            });

            // Acquire the only permit on the ConcurrencyLimiter so the chained limiter fails when calling acquire
            var concurrencyLease = concurrencyLimiter2.Acquire();

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);
            using var lease = chainedLimiter.Acquire("");

            Assert.False(lease.IsAcquired);
            Assert.Equal(1, concurrencyLimiter1.GetAvailablePermits());
        }

        [Fact]
        public async Task WaitAndAcquireAsyncFailsAndReleasesAcquiredResources()
        {
            using var concurrencyLimiter1 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            using var concurrencyLimiter2 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => concurrencyLimiter1);
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(2, key => concurrencyLimiter2);
            });

            // Acquire the only permit on the ConcurrencyLimiter so the chained limiter fails when calling acquire
            var concurrencyLease = await concurrencyLimiter2.WaitAndAcquireAsync();

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
                return RateLimitPartition.Get(1, key => concurrencyLimiter);
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(2, key => new NotImplementedLimiter());
            });

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);
            Assert.Throws<NotImplementedException>(() => chainedLimiter.Acquire(""));
            Assert.Equal(1, concurrencyLimiter.GetAvailablePermits());
        }

        [Fact]
        public async Task WaitAndAcquireAsyncThrowsAndReleasesAcquiredResources()
        {
            using var concurrencyLimiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => concurrencyLimiter);
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(2, key => new NotImplementedLimiter());
            });

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);
            await Assert.ThrowsAsync<NotImplementedException>(async () => await chainedLimiter.WaitAndAcquireAsync(""));
            Assert.Equal(1, concurrencyLimiter.GetAvailablePermits());
        }

        [Fact]
        public void AcquireThrows_SingleLimiter()
        {
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => new NotImplementedLimiter());
            });

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1);
            Assert.Throws<NotImplementedException>(() => chainedLimiter.Acquire(""));
        }

        [Fact]
        public async Task WaitAndAcquireAsyncThrows_SingleLimiter()
        {
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => new NotImplementedLimiter());
            });

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1);
            await Assert.ThrowsAsync<NotImplementedException>(async () => await chainedLimiter.WaitAndAcquireAsync(""));
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
                return RateLimitPartition.Get(1, key => new CustomizableLimiter() { AcquireCoreImpl = _ => new ThrowDisposeLease() });
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => concurrencyLimiter);
            });

            var lease = concurrencyLimiter.Acquire();

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);
            var ex = Assert.Throws<AggregateException>(() => chainedLimiter.Acquire(""));
            Assert.Single(ex.InnerExceptions);
            Assert.IsType<NotImplementedException>(ex.InnerException);
        }

        [Fact]
        public async Task WaitAndAcquireAsyncFailsDisposeThrows()
        {
            using var concurrencyLimiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => new CustomizableLimiter() { AcquireCoreImpl = _ => new ThrowDisposeLease() });
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => concurrencyLimiter);
            });

            var lease = await concurrencyLimiter.WaitAndAcquireAsync();

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
                return RateLimitPartition.Get(1, key => new CustomizableLimiter() { AcquireCoreImpl = _ => new ThrowDisposeLease() });
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => new CustomizableLimiter() { AcquireCoreImpl = _ => new ThrowDisposeLease() });
            });
            using var limiter3 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => concurrencyLimiter);
            });

            var lease = concurrencyLimiter.Acquire();

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2, limiter3);
            var ex = Assert.Throws<AggregateException>(() => chainedLimiter.Acquire(""));
            Assert.Equal(2, ex.InnerExceptions.Count);
            Assert.IsType<NotImplementedException>(ex.InnerExceptions[0]);
            Assert.IsType<NotImplementedException>(ex.InnerExceptions[1]);
        }

        [Fact]
        public async Task WaitAndAcquireAsyncFailsDisposeThrowsMultipleLimitersThrow()
        {
            using var concurrencyLimiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => new CustomizableLimiter() { WaitAndAcquireAsyncCoreImpl = (_, _) => new ValueTask<RateLimitLease>(new ThrowDisposeLease()) });
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => new CustomizableLimiter() { WaitAndAcquireAsyncCoreImpl = (_, _) => new ValueTask<RateLimitLease>(new ThrowDisposeLease()) });
            });
            using var limiter3 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => concurrencyLimiter);
            });

            var lease = concurrencyLimiter.Acquire();

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2, limiter3);
            var ex = await Assert.ThrowsAsync<AggregateException>(async () => await chainedLimiter.WaitAndAcquireAsync(""));
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
                return RateLimitPartition.Get(1, key => new CustomizableLimiter() { AcquireCoreImpl = _ => new ThrowDisposeLease() });
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => new CustomizableLimiter() { AcquireCoreImpl = _ => new ThrowDisposeLease() });
            });
            using var limiter3 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => new NotImplementedLimiter());
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
        public async Task WaitAndAcquireAsyncThrowsDisposeThrowsMultipleLimitersThrow()
        {
            using var concurrencyLimiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => new CustomizableLimiter() { WaitAndAcquireAsyncCoreImpl = (_, _) => new ValueTask<RateLimitLease>(new ThrowDisposeLease()) });
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => new CustomizableLimiter() { WaitAndAcquireAsyncCoreImpl = (_, _) => new ValueTask<RateLimitLease>(new ThrowDisposeLease()) });
            });
            using var limiter3 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => new NotImplementedLimiter());
            });

            var lease = concurrencyLimiter.Acquire();

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2, limiter3);
            var ex = await Assert.ThrowsAsync<AggregateException>(async () => await chainedLimiter.WaitAndAcquireAsync(""));
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
                return RateLimitPartition.Get(1, key => new CustomizableLimiter() { AcquireCoreImpl = _ => new ThrowDisposeLease() });
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => concurrencyLimiter);
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
        public async Task WaitAndAcquireAsyncSucceedsDisposeThrowsAndReleasesResources()
        {
            using var concurrencyLimiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => new CustomizableLimiter() { WaitAndAcquireAsyncCoreImpl = (_, _) => new ValueTask<RateLimitLease>(new ThrowDisposeLease()) });
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => concurrencyLimiter);
            });

            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);
            var lease = await chainedLimiter.WaitAndAcquireAsync("");
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
                return RateLimitPartition.Get(1, key => concurrencyLimiter1);
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => concurrencyLimiter2);
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
        public async Task WaitAndAcquireAsyncForwardsCorrectPermitCount()
        {
            using var concurrencyLimiter1 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(5, QueueProcessingOrder.OldestFirst, 0));
            using var concurrencyLimiter2 = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(3, QueueProcessingOrder.OldestFirst, 0));
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => concurrencyLimiter1);
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => concurrencyLimiter2);
            });
            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);

            var lease = await chainedLimiter.WaitAndAcquireAsync("", 3);
            Assert.True(lease.IsAcquired);
            Assert.Equal(2, concurrencyLimiter1.GetAvailablePermits());
            Assert.Equal(0, concurrencyLimiter2.GetAvailablePermits());

            lease.Dispose();
            Assert.Equal(5, concurrencyLimiter1.GetAvailablePermits());
            Assert.Equal(3, concurrencyLimiter2.GetAvailablePermits());
        }

        [Fact]
        public void AcquireForwardsCorrectResource()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                if (resource == "1")
                {
                    return RateLimitPartition.Get(1, key => limiterFactory.GetLimiter(key));
                }
                return RateLimitPartition.Get(2, key => limiterFactory.GetLimiter(key));
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                if (resource == "1")
                {
                    return RateLimitPartition.Get(3, key => limiterFactory.GetLimiter(key));
                }
                return RateLimitPartition.Get(4, key => limiterFactory.GetLimiter(key));
            });
            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);

            var lease = chainedLimiter.Acquire("1");
            Assert.True(lease.IsAcquired);
            Assert.Equal(2, limiterFactory.Limiters.Count);
            Assert.Equal(1, limiterFactory.Limiters[0].Key);
            Assert.Equal(3, limiterFactory.Limiters[1].Key);
        }

        [Fact]
        public async Task WaitAndAcquireAsyncForwardsCorrectResource()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                if (resource == "1")
                {
                    return RateLimitPartition.Get(1, key => limiterFactory.GetLimiter(key));
                }
                return RateLimitPartition.Get(2, key => limiterFactory.GetLimiter(key));
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                if (resource == "1")
                {
                    return RateLimitPartition.Get(3, key => limiterFactory.GetLimiter(key));
                }
                return RateLimitPartition.Get(4, key => limiterFactory.GetLimiter(key));
            });
            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);

            var lease = await chainedLimiter.WaitAndAcquireAsync("1");
            Assert.True(lease.IsAcquired);
            Assert.Equal(2, limiterFactory.Limiters.Count);
            Assert.Equal(1, limiterFactory.Limiters[0].Key);
            Assert.Equal(3, limiterFactory.Limiters[1].Key);
        }

        [Fact]
        public async Task WaitAndAcquireAsyncCanBeCanceled()
        {
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.GetConcurrencyLimiter(1, key => new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 1));
            });
            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter);

            var lease = chainedLimiter.Acquire("");
            Assert.True(lease.IsAcquired);

            var cts = new CancellationTokenSource();
            var task = chainedLimiter.WaitAndAcquireAsync("", 1, cts.Token);

            cts.Cancel();
             await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
        }

        [Fact]
        public async Task WaitAndAcquireAsyncCanceledReleasesAcquiredResources()
        {
            var concurrencyLimiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(2, QueueProcessingOrder.OldestFirst, 0));
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => concurrencyLimiter);
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.GetConcurrencyLimiter(1, key => new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 1));
            });
            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);

            var lease = chainedLimiter.Acquire("");
            Assert.True(lease.IsAcquired);
            Assert.Equal(1, concurrencyLimiter.GetAvailablePermits());

            var cts = new CancellationTokenSource();
            var task = chainedLimiter.WaitAndAcquireAsync("", 1, cts.Token);

            Assert.Equal(0, concurrencyLimiter.GetAvailablePermits());
            cts.Cancel();
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
            Assert.Equal(1, concurrencyLimiter.GetAvailablePermits());
        }

        [Fact]
        public async Task WaitAndAcquireAsyncWaitsForResourcesBeforeCallingNextLimiter()
        {
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.GetConcurrencyLimiter(1, key => new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 1));
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                // 0 queue limit to verify this isn't called while the previous limiter is waiting for resource(s)
                // as it would return a failed lease when no queue is available
                return RateLimitPartition.GetConcurrencyLimiter(1, key => new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 0));
            });
            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);

            var lease = chainedLimiter.Acquire("");
            Assert.True(lease.IsAcquired);

            var task = chainedLimiter.WaitAndAcquireAsync("");
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
                return RateLimitPartition.Get(1, key => customizableLimiter1);
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => customizableLimiter2);
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
                return RateLimitPartition.Get(1, key => customizableLimiter1);
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => customizableLimiter2);
            });
            using var limiter3 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => new NotImplementedLimiter());
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

            Assert.Throws<NotImplementedException>(() => chainedLimiter.Acquire(""));
        }

        [Fact]
        public async Task LeasesAreDisposedInReverseOrderWhenWaitAndAcquireAsyncThrows()
        {
            var customizableLimiter1 = new CustomizableLimiter();
            var customizableLimiter2 = new CustomizableLimiter();
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => customizableLimiter1);
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => customizableLimiter2);
            });
            using var limiter3 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => new NotImplementedLimiter());
            });
            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2, limiter3);

            var customizableLease1 = new CustomizableLease();
            var disposeCalled = false;
            customizableLease1.DisposeImpl = _ =>
            {
                Assert.True(disposeCalled);
            };
            customizableLimiter1.WaitAndAcquireAsyncCoreImpl = (_, _) => new ValueTask<RateLimitLease>(customizableLease1);

            var customizableLease2 = new CustomizableLease();
            customizableLease2.DisposeImpl = _ =>
            {
                disposeCalled = true;
            };
            customizableLimiter2.WaitAndAcquireAsyncCoreImpl = (_, _) => new ValueTask<RateLimitLease>(customizableLease2);

            await Assert.ThrowsAsync<NotImplementedException>(async () => await chainedLimiter.WaitAndAcquireAsync(""));
        }

        [Fact]
        public void MetadataIsCombined()
        {
            var customizableLimiter1 = new CustomizableLimiter();
            customizableLimiter1.AcquireCoreImpl = _ => new CustomizableLease()
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
            var customizableLimiter2 = new CustomizableLimiter();
            customizableLimiter2.AcquireCoreImpl = _ => new CustomizableLease()
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
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => customizableLimiter1);
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => customizableLimiter2);
            });
            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);

            var lease = chainedLimiter.Acquire("");

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
            var customizableLimiter1 = new CustomizableLimiter();
            customizableLimiter1.AcquireCoreImpl = _ => new CustomizableLease()
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
            var customizableLimiter2 = new CustomizableLimiter();
            customizableLimiter2.AcquireCoreImpl = _ => new CustomizableLease()
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
            using var limiter1 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => customizableLimiter1);
            });
            using var limiter2 = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => customizableLimiter2);
            });
            using var chainedLimiter = PartitionedRateLimiter.CreateChained<string>(limiter1, limiter2);

            var lease = chainedLimiter.Acquire("");

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
