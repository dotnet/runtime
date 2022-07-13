﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.RateLimiting.Test
{
    public class SlidingWindowRateLimiterTests : BaseRateLimiterTests
    {
        [Fact]
        public override void CanAcquireResource()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1,
                TimeSpan.Zero, 2, autoReplenishment: false));
            var lease = limiter.Acquire();

            Assert.True(lease.IsAcquired);
            Assert.False(limiter.Acquire().IsAcquired);

            lease.Dispose();
            Assert.False(limiter.Acquire().IsAcquired);
            Assert.True(limiter.TryReplenish());
            Assert.True(limiter.TryReplenish());

            Assert.True(limiter.Acquire().IsAcquired);
        }

        [Fact]
        public override void InvalidOptionsThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new SlidingWindowRateLimiterOptions(-1, QueueProcessingOrder.NewestFirst, 1, TimeSpan.FromMinutes(2), 1, autoReplenishment: false));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new SlidingWindowRateLimiterOptions(1, QueueProcessingOrder.NewestFirst, -1, TimeSpan.FromMinutes(2), 1, autoReplenishment: false));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new SlidingWindowRateLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1, TimeSpan.FromMinutes(2), -1, autoReplenishment: false));
        }

        [Fact]
        public override async Task CanAcquireResourceAsync()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(2, QueueProcessingOrder.NewestFirst, 4,
                TimeSpan.Zero, 2, autoReplenishment: false));

            using var lease = await limiter.WaitAndAcquireAsync();

            Assert.True(lease.IsAcquired);
            var wait = limiter.WaitAndAcquireAsync(2);
            Assert.False(wait.IsCompleted);

            Assert.True(limiter.TryReplenish());

            Assert.False(wait.IsCompleted);

            var wait2 = limiter.WaitAndAcquireAsync(2);
            Assert.False(wait2.IsCompleted);

            Assert.True(limiter.TryReplenish());

            Assert.True((await wait2).IsAcquired);
        }

        [Fact]
        public async Task CanAcquireMultipleRequestsAsync()
        {
            // This test verifies the following behavior
            // 1. when we have available permits after replenish to serve the queued requests
            // 2. when the oldest item from queue is remove to accomodate new requests (QueueProcessingOrder: NewestFirst)
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(4, QueueProcessingOrder.NewestFirst, 4,
                TimeSpan.Zero, 3, autoReplenishment: false));

            using var lease = await limiter.WaitAndAcquireAsync(2);

            Assert.True(lease.IsAcquired);
            var wait = limiter.WaitAndAcquireAsync(3);
            Assert.False(wait.IsCompleted);

            Assert.True(limiter.TryReplenish());

            Assert.False(wait.IsCompleted);

            var wait2 = limiter.WaitAndAcquireAsync(2);
            Assert.True(wait2.IsCompleted);

            Assert.True(limiter.TryReplenish());

            var wait3 = limiter.WaitAndAcquireAsync(2);
            Assert.False(wait3.IsCompleted);

            Assert.True(limiter.TryReplenish());
            Assert.True((await wait3).IsAcquired);

            Assert.False((await wait).IsAcquired);
            Assert.Equal(0, limiter.GetAvailablePermits());
        }

        [Fact]
        public override async Task CanAcquireResourceAsync_QueuesAndGrabsOldest()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(2, QueueProcessingOrder.OldestFirst, 3,
                TimeSpan.FromMinutes(0), 2, autoReplenishment: false));
            var lease = await limiter.WaitAndAcquireAsync(2);

            Assert.True(lease.IsAcquired);
            var wait1 = limiter.WaitAndAcquireAsync();
            var wait2 = limiter.WaitAndAcquireAsync(2);
            Assert.False(wait1.IsCompleted);
            Assert.False(wait2.IsCompleted);

            lease.Dispose();
            Assert.True(limiter.TryReplenish());

            Assert.False(wait1.IsCompleted);
            Assert.True(limiter.TryReplenish());

            lease = await wait1;
            Assert.True(lease.IsAcquired);
            Assert.False(wait2.IsCompleted);

            lease.Dispose();
            Assert.Equal(1, limiter.GetAvailablePermits());
            Assert.True(limiter.TryReplenish());
            Assert.True(limiter.TryReplenish());

            lease = await wait2;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task CanAcquireResourceAsync_QueuesAndGrabsNewest()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(2, QueueProcessingOrder.NewestFirst, 3,
                TimeSpan.FromMinutes(0), 2, autoReplenishment: false));

            var lease = await limiter.WaitAndAcquireAsync(2);
            Assert.True(lease.IsAcquired);

            var wait1 = limiter.WaitAndAcquireAsync(2);
            var wait2 = limiter.WaitAndAcquireAsync();
            Assert.False(wait1.IsCompleted);
            Assert.False(wait2.IsCompleted);

            lease.Dispose();
            Assert.True(limiter.TryReplenish());
            Assert.False(wait2.IsCompleted);

            Assert.True(limiter.TryReplenish());
            // second queued item completes first with NewestFirst
            lease = await wait2;
            Assert.True(lease.IsAcquired);
            Assert.False(wait1.IsCompleted);

            lease.Dispose();
            Assert.Equal(1, limiter.GetAvailablePermits());
            Assert.True(limiter.TryReplenish());
            Assert.True(limiter.TryReplenish());

            lease = await wait1;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task FailsWhenQueuingMoreThanLimit_OldestFirst()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(1, QueueProcessingOrder.OldestFirst, 1,
                TimeSpan.Zero, 2, autoReplenishment: false));
            using var lease = limiter.Acquire(1);
            var wait = limiter.WaitAndAcquireAsync(1);

            var failedLease = await limiter.WaitAndAcquireAsync(1);
            Assert.False(failedLease.IsAcquired);
        }

        [Fact]
        public override async Task DropsOldestWhenQueuingMoreThanLimit_NewestFirst()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1,
                   TimeSpan.Zero, 2, autoReplenishment: false));
            var lease = limiter.Acquire(1);
            var wait = limiter.WaitAndAcquireAsync(1);
            Assert.False(wait.IsCompleted);

            var wait2 = limiter.WaitAndAcquireAsync(1);
            var lease1 = await wait;
            Assert.False(lease1.IsAcquired);
            Assert.False(wait2.IsCompleted);

            limiter.TryReplenish();
            limiter.TryReplenish();

            lease = await wait2;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task DropsMultipleOldestWhenQueuingMoreThanLimit_NewestFirst()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(2, QueueProcessingOrder.NewestFirst, 2,
                   TimeSpan.Zero, 2, autoReplenishment: false));
            var lease = limiter.Acquire(2);
            Assert.True(lease.IsAcquired);
            var wait = limiter.WaitAndAcquireAsync(1);
            Assert.False(wait.IsCompleted);

            var wait2 = limiter.WaitAndAcquireAsync(1);
            Assert.False(wait2.IsCompleted);

            var wait3 = limiter.WaitAndAcquireAsync(2);
            var lease1 = await wait;
            var lease2 = await wait2;
            Assert.False(lease1.IsAcquired);
            Assert.False(lease2.IsAcquired);
            Assert.False(wait3.IsCompleted);

            limiter.TryReplenish();
            limiter.TryReplenish();

            lease = await wait3;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task DropsRequestedLeaseIfPermitCountGreaterThanQueueLimitAndNoAvailability_NewestFirst()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(2, QueueProcessingOrder.NewestFirst, 1,
                   TimeSpan.Zero, 2, autoReplenishment: false));
            var lease = limiter.Acquire(2);
            Assert.True(lease.IsAcquired);

            // Fill queue
            var wait = limiter.WaitAndAcquireAsync(1);
            Assert.False(wait.IsCompleted);

            var lease1 = await limiter.WaitAndAcquireAsync(2);
            Assert.False(lease1.IsAcquired);

            limiter.TryReplenish();
            limiter.TryReplenish();

            lease = await wait;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task QueueAvailableAfterQueueLimitHitAndResources_BecomeAvailable()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(3, QueueProcessingOrder.OldestFirst, 2,
                TimeSpan.Zero, 3, autoReplenishment: false));
            var lease = limiter.Acquire(2);
            var wait = limiter.WaitAndAcquireAsync(2);

            var failedLease = await limiter.WaitAndAcquireAsync(2);
            Assert.False(failedLease.IsAcquired);

            limiter.TryReplenish();
            limiter.TryReplenish();
            Assert.False(wait.IsCompleted);

            limiter.TryReplenish();
            lease = await wait;
            Assert.True(lease.IsAcquired);

            wait = limiter.WaitAndAcquireAsync(2);
            Assert.False(wait.IsCompleted);

            limiter.TryReplenish();
            limiter.TryReplenish();
            limiter.TryReplenish();

            lease = await wait;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task LargeAcquiresAndQueuesDoNotIntegerOverflow()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(int.MaxValue, QueueProcessingOrder.NewestFirst, int.MaxValue,
                TimeSpan.Zero, 2, autoReplenishment: false));
            var lease = limiter.Acquire(int.MaxValue);
            Assert.True(lease.IsAcquired);

            // Fill queue
            var wait = limiter.WaitAndAcquireAsync(3);
            Assert.False(wait.IsCompleted);

            var wait2 = limiter.WaitAndAcquireAsync(int.MaxValue);
            Assert.False(wait2.IsCompleted);

            var lease1 = await wait;
            Assert.False(lease1.IsAcquired);

            limiter.TryReplenish();
            limiter.TryReplenish();
            var lease2 = await wait2;
            Assert.True(lease2.IsAcquired);
        }

        [Fact]
        public override void ThrowsWhenAcquiringMoreThanLimit()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1,
                TimeSpan.Zero, 1, autoReplenishment: false));
            Assert.Throws<ArgumentOutOfRangeException>(() => limiter.Acquire(2));
        }

        [Fact]
        public override async Task ThrowsWhenWaitingForMoreThanLimit()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1,
                TimeSpan.Zero, 1, autoReplenishment: false));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await limiter.WaitAndAcquireAsync(2));
        }

        [Fact]
        public override void ThrowsWhenAcquiringLessThanZero()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1,
                TimeSpan.Zero, 1, autoReplenishment: false));
            Assert.Throws<ArgumentOutOfRangeException>(() => limiter.Acquire(-1));
        }

        [Fact]
        public override async Task ThrowsWhenWaitingForLessThanZero()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1,
                TimeSpan.Zero, 1, autoReplenishment: false));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await limiter.WaitAndAcquireAsync(-1));
        }

        [Fact]
        public override void AcquireZero_WithAvailability()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1,
                TimeSpan.Zero, 1, autoReplenishment: false));

            using var lease = limiter.Acquire(0);
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override void AcquireZero_WithoutAvailability()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1,
                TimeSpan.Zero, 1, autoReplenishment: false));
            using var lease = limiter.Acquire(1);
            Assert.True(lease.IsAcquired);

            var lease2 = limiter.Acquire(0);
            Assert.False(lease2.IsAcquired);
            lease2.Dispose();
        }

        [Fact]
        public override async Task WaitAndAcquireAsyncZero_WithAvailability()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1,
                TimeSpan.Zero, 1, autoReplenishment: false));

            using var lease = await limiter.WaitAndAcquireAsync(0);
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task WaitAndAcquireAsyncZero_WithoutAvailabilityWaitsForAvailability()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1,
                TimeSpan.Zero, 2, autoReplenishment: false));
            var lease = await limiter.WaitAndAcquireAsync(1);
            Assert.True(lease.IsAcquired);

            var wait = limiter.WaitAndAcquireAsync(0);
            Assert.False(wait.IsCompleted);

            lease.Dispose();
            Assert.True(limiter.TryReplenish());
            Assert.True(limiter.TryReplenish());
            using var lease2 = await wait;
            Assert.True(lease2.IsAcquired);
        }

        [Fact]
        public override async Task CanDequeueMultipleResourcesAtOnce()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(2, QueueProcessingOrder.OldestFirst, 4,
                TimeSpan.Zero, 2, autoReplenishment: false));
            using var lease = await limiter.WaitAndAcquireAsync(2);
            Assert.True(lease.IsAcquired);

            var wait1 = limiter.WaitAndAcquireAsync(1);
            var wait2 = limiter.WaitAndAcquireAsync(1);
            Assert.False(wait1.IsCompleted);
            Assert.False(wait2.IsCompleted);

            lease.Dispose();
            Assert.True(limiter.TryReplenish());
            Assert.True(limiter.TryReplenish());

            var lease1 = await wait1;
            var lease2 = await wait2;
            Assert.True(lease1.IsAcquired);
            Assert.True(lease2.IsAcquired);
        }

        [Fact]
        public override async Task CanCancelWaitAndAcquireAsyncAfterQueuing()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(2, QueueProcessingOrder.OldestFirst, 1,
                TimeSpan.Zero, 2, autoReplenishment: false));
            var lease = limiter.Acquire(2);
            Assert.True(lease.IsAcquired);

            var cts = new CancellationTokenSource();
            var wait = limiter.WaitAndAcquireAsync(1, cts.Token);

            cts.Cancel();
            var ex = await Assert.ThrowsAsync<TaskCanceledException>(() => wait.AsTask());
            Assert.Equal(cts.Token, ex.CancellationToken);

            lease.Dispose();
            Assert.True(limiter.TryReplenish());

            Assert.Equal(0, limiter.GetAvailablePermits());
        }

        [Fact]
        public override async Task CanCancelWaitAndAcquireAsyncBeforeQueuing()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(2, QueueProcessingOrder.OldestFirst, 1,
                TimeSpan.Zero, 2, autoReplenishment: false));
            var lease = limiter.Acquire(2);
            Assert.True(lease.IsAcquired);

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var ex = await Assert.ThrowsAsync<TaskCanceledException>(() => limiter.WaitAndAcquireAsync(1, cts.Token).AsTask());
            Assert.Equal(cts.Token, ex.CancellationToken);

            lease.Dispose();
            Assert.True(limiter.TryReplenish());

            Assert.Equal(0, limiter.GetAvailablePermits());
        }

        [Fact]
        public override async Task CancelUpdatesQueueLimit()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(2, QueueProcessingOrder.NewestFirst, 1,
                TimeSpan.Zero, 2, autoReplenishment: false));
            var lease = limiter.Acquire(2);
            Assert.True(lease.IsAcquired);

            var cts = new CancellationTokenSource();
            var wait = limiter.WaitAndAcquireAsync(1, cts.Token);

            cts.Cancel();
            var ex = await Assert.ThrowsAsync<TaskCanceledException>(() => wait.AsTask());
            Assert.Equal(cts.Token, ex.CancellationToken);

            wait = limiter.WaitAndAcquireAsync(1);
            Assert.False(wait.IsCompleted);

            limiter.TryReplenish();
            limiter.TryReplenish();

            lease = await wait;
            Assert.True(lease.IsAcquired);
            Assert.Equal(1, limiter.GetAvailablePermits());
        }

        [Fact]
        public override void NoMetadataOnAcquiredLease()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(1, QueueProcessingOrder.OldestFirst, 1,
                TimeSpan.Zero, 2, autoReplenishment: false));
            using var lease = limiter.Acquire(1);
            Assert.False(lease.TryGetMetadata(MetadataName.RetryAfter, out _));
        }

        [Fact]
        public override void MetadataNamesContainsAllMetadata()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(1, QueueProcessingOrder.OldestFirst, 1,
                TimeSpan.Zero, 1, autoReplenishment: false));
            using var lease = limiter.Acquire(1);
            Assert.Collection(lease.MetadataNames, metadataName => Assert.Equal(metadataName, MetadataName.RetryAfter.Name));
        }

        [Fact]
        public override async Task DisposeReleasesQueuedAcquires()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(1, QueueProcessingOrder.OldestFirst, 3,
                TimeSpan.Zero, 1, autoReplenishment: false));
            var lease = limiter.Acquire(1);
            var wait1 = limiter.WaitAndAcquireAsync(1);
            var wait2 = limiter.WaitAndAcquireAsync(1);
            var wait3 = limiter.WaitAndAcquireAsync(1);
            Assert.False(wait1.IsCompleted);
            Assert.False(wait2.IsCompleted);
            Assert.False(wait3.IsCompleted);

            limiter.Dispose();

            lease = await wait1;
            Assert.False(lease.IsAcquired);
            lease = await wait2;
            Assert.False(lease.IsAcquired);
            lease = await wait3;
            Assert.False(lease.IsAcquired);

            // Throws after disposal
            Assert.Throws<ObjectDisposedException>(() => limiter.Acquire(1));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => limiter.WaitAndAcquireAsync(1).AsTask());
        }

        [Fact]
        public override async Task DisposeAsyncReleasesQueuedAcquires()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(1, QueueProcessingOrder.OldestFirst, 3,
                TimeSpan.Zero, 2, autoReplenishment: false));
            var lease = limiter.Acquire(1);
            var wait1 = limiter.WaitAndAcquireAsync(1);
            var wait2 = limiter.WaitAndAcquireAsync(1);
            var wait3 = limiter.WaitAndAcquireAsync(1);
            Assert.False(wait1.IsCompleted);
            Assert.False(wait2.IsCompleted);
            Assert.False(wait3.IsCompleted);

            await limiter.DisposeAsync();

            lease = await wait1;
            Assert.False(lease.IsAcquired);
            lease = await wait2;
            Assert.False(lease.IsAcquired);
            lease = await wait3;
            Assert.False(lease.IsAcquired);

            // Throws after disposal
            Assert.Throws<ObjectDisposedException>(() => limiter.Acquire(1));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => limiter.WaitAndAcquireAsync(1).AsTask());
        }

        [Fact]
        public void TryReplenishWithAutoReplenish_ReturnsFalse()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(2, QueueProcessingOrder.OldestFirst, 1,
                TimeSpan.FromSeconds(1), 1, autoReplenishment: true));
            Assert.Equal(2, limiter.GetAvailablePermits());
            Assert.False(limiter.TryReplenish());
            Assert.Equal(2, limiter.GetAvailablePermits());
        }

        [Fact]
        public async Task AutoReplenish_ReplenishesCounters()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(2, QueueProcessingOrder.OldestFirst, 1,
                TimeSpan.FromMilliseconds(1000), 2, autoReplenishment: true));
            Assert.Equal(2, limiter.GetAvailablePermits());
            limiter.Acquire(2);

            var lease = await limiter.WaitAndAcquireAsync(1);
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task CanAcquireResourcesWithWaitAndAcquireAsyncWithQueuedItemsIfNewestFirst()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(2, QueueProcessingOrder.NewestFirst, 2,
                TimeSpan.Zero, 3, autoReplenishment: false));

            var lease = limiter.Acquire(1);
            Assert.True(lease.IsAcquired);

            var wait = limiter.WaitAndAcquireAsync(2);
            Assert.False(wait.IsCompleted);

            Assert.Equal(1, limiter.GetAvailablePermits());
            lease = await limiter.WaitAndAcquireAsync(1);
            Assert.True(lease.IsAcquired);
            Assert.False(wait.IsCompleted);

            limiter.TryReplenish();
            Assert.True(limiter.TryReplenish());

            Assert.False(wait.IsCompleted);

            Assert.True(limiter.TryReplenish());
            lease = await wait;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task CannotAcquireResourcesWithWaitAndAcquireAsyncWithQueuedItemsIfOldestFirst()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(3, QueueProcessingOrder.OldestFirst, 5,
                TimeSpan.Zero, 2, autoReplenishment: false));

            var lease = limiter.Acquire(3);
            Assert.True(lease.IsAcquired);

            var wait = limiter.WaitAndAcquireAsync(2);
            var wait2 = limiter.WaitAndAcquireAsync(2);
            Assert.False(wait.IsCompleted);
            Assert.False(wait2.IsCompleted);

            limiter.TryReplenish();

            Assert.False(wait.IsCompleted);
            Assert.False(wait2.IsCompleted);

            limiter.TryReplenish();

            lease = await wait;
            Assert.True(lease.IsAcquired);

            limiter.TryReplenish();
            limiter.TryReplenish();

            lease = await wait2;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task CanAcquireResourcesWithAcquireWithQueuedItemsIfNewestFirst()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(2, QueueProcessingOrder.NewestFirst, 3,
                TimeSpan.Zero, 2, autoReplenishment: false));

            var lease = limiter.Acquire(1);
            Assert.True(lease.IsAcquired);

            var wait = limiter.WaitAndAcquireAsync(2);
            Assert.False(wait.IsCompleted);

            lease = limiter.Acquire(1);
            Assert.True(lease.IsAcquired);
            Assert.False(wait.IsCompleted);

            limiter.TryReplenish();
            limiter.TryReplenish();

            lease = await wait;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task CannotAcquireResourcesWithAcquireWithQueuedItemsIfOldestFirst()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(2, QueueProcessingOrder.OldestFirst, 3,
                TimeSpan.Zero, 2, autoReplenishment: false));

            var lease = limiter.Acquire(1);
            Assert.True(lease.IsAcquired);

            var wait = limiter.WaitAndAcquireAsync(2);
            Assert.False(wait.IsCompleted);

            lease = limiter.Acquire(1);
            Assert.False(lease.IsAcquired);

            limiter.TryReplenish();
            Assert.True(limiter.TryReplenish());

            lease = await wait;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override void NullIdleDurationWhenActive()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(1, QueueProcessingOrder.OldestFirst, 2,
                   TimeSpan.FromMilliseconds(2), 1, autoReplenishment: false));
            limiter.Acquire(1);
            Assert.Null(limiter.IdleDuration);
        }

        [Fact]
        public override async Task IdleDurationUpdatesWhenIdle()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(3, QueueProcessingOrder.OldestFirst, 2,
                TimeSpan.FromMilliseconds(2), 2, autoReplenishment: false));
            Assert.NotNull(limiter.IdleDuration);
            var previousDuration = limiter.IdleDuration;
            await Task.Delay(15);
            Assert.True(previousDuration < limiter.IdleDuration);
        }

        [Fact]
        public override void IdleDurationUpdatesWhenChangingFromActive()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(1, QueueProcessingOrder.OldestFirst, 2,
                TimeSpan.Zero, 2, autoReplenishment: false));
            limiter.Acquire(1);
            limiter.TryReplenish();
            limiter.TryReplenish();
            Assert.NotNull(limiter.IdleDuration);
        }

        [Fact]
        public void ReplenishingRateLimiterPropertiesHaveCorrectValues()
        {
            var replenishPeriod = TimeSpan.FromMinutes(1);
            using ReplenishingRateLimiter limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(1, QueueProcessingOrder.OldestFirst, 2,
                replenishPeriod, 1, autoReplenishment: true));
            Assert.True(limiter.IsAutoReplenishing);
            Assert.Equal(replenishPeriod, limiter.ReplenishmentPeriod);

            replenishPeriod = TimeSpan.FromSeconds(2);
            using ReplenishingRateLimiter limiter2 = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(1, QueueProcessingOrder.OldestFirst, 2,
                replenishPeriod, 1, autoReplenishment: false));
            Assert.False(limiter2.IsAutoReplenishing);
            Assert.Equal(replenishPeriod, limiter2.ReplenishmentPeriod);
        }

        [Fact]
        public override async Task CanFillQueueWithNewestFirstAfterCancelingQueuedRequestWithAnotherQueuedRequest()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(2, QueueProcessingOrder.NewestFirst, 2,
                TimeSpan.Zero, 2, autoReplenishment: false));
            var lease = limiter.Acquire(2);
            Assert.True(lease.IsAcquired);

            var cts = new CancellationTokenSource();
            var wait = limiter.WaitAndAcquireAsync(1, cts.Token);

            // Add another item to queue, will be completed as failed later when we queue another item
            var wait2 = limiter.WaitAndAcquireAsync(1);
            Assert.False(wait.IsCompleted);

            cts.Cancel();
            var ex = await Assert.ThrowsAsync<TaskCanceledException>(() => wait.AsTask());
            Assert.Equal(cts.Token, ex.CancellationToken);

            lease.Dispose();
            limiter.TryReplenish();

            var wait3 = limiter.WaitAndAcquireAsync(2);
            Assert.False(wait3.IsCompleted);

            // will be kicked by wait3 because we're using NewestFirst
            lease = await wait2;
            Assert.False(lease.IsAcquired);

            limiter.TryReplenish();
            lease = await wait3;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task CanDisposeAfterCancelingQueuedRequest()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions(1, QueueProcessingOrder.OldestFirst, 1,
                TimeSpan.Zero, 2, autoReplenishment: false));
            var lease = limiter.Acquire(1);
            Assert.True(lease.IsAcquired);

            var cts = new CancellationTokenSource();
            var wait = limiter.WaitAndAcquireAsync(1, cts.Token);

            cts.Cancel();
            var ex = await Assert.ThrowsAsync<TaskCanceledException>(() => wait.AsTask());
            Assert.Equal(cts.Token, ex.CancellationToken);

            // Make sure dispose doesn't have any side-effects when dealing with a canceled queued item
            limiter.Dispose();
        }
    }
}
