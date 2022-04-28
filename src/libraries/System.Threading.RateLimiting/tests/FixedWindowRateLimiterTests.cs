// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.RateLimiting.Test
{
    public class FixedWindowRateLimiterTests : BaseRateLimiterTests
    {
        [Fact]
        public override void CanAcquireResource()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1,
                TimeSpan.Zero, autoReplenishment: false));
            var lease = limiter.Acquire();

            Assert.True(lease.IsAcquired);
            Assert.False(limiter.Acquire().IsAcquired);

            lease.Dispose();
            Assert.False(limiter.Acquire().IsAcquired);
            Assert.True(limiter.TryReplenish());

            Assert.True(limiter.Acquire().IsAcquired);
        }

        [Fact]
        public override void InvalidOptionsThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new FixedWindowRateLimiterOptions(-1, QueueProcessingOrder.NewestFirst, 1, TimeSpan.FromMinutes(2), autoReplenishment: false));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new FixedWindowRateLimiterOptions(1, QueueProcessingOrder.NewestFirst, -1, TimeSpan.FromMinutes(2), autoReplenishment: false));
        }

        [Fact]
        public override async Task CanAcquireResourceAsync()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1,
                TimeSpan.Zero, autoReplenishment: false));

            using var lease = await limiter.WaitAsync();

            Assert.True(lease.IsAcquired);
            var wait = limiter.WaitAsync();
            Assert.False(wait.IsCompleted);

            Assert.True(limiter.TryReplenish());

            Assert.True((await wait).IsAcquired);
        }

        [Fact]
        public override async Task CanAcquireResourceAsync_QueuesAndGrabsOldest()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(1, QueueProcessingOrder.OldestFirst, 2,
                TimeSpan.Zero, autoReplenishment: false));
            var lease = await limiter.WaitAsync();

            Assert.True(lease.IsAcquired);
            var wait1 = limiter.WaitAsync();
            var wait2 = limiter.WaitAsync();
            Assert.False(wait1.IsCompleted);
            Assert.False(wait2.IsCompleted);

            lease.Dispose();
            Assert.True(limiter.TryReplenish());

            lease = await wait1;
            Assert.True(lease.IsAcquired);
            Assert.False(wait2.IsCompleted);

            lease.Dispose();
            Assert.Equal(0, limiter.GetAvailablePermits());
            Assert.True(limiter.TryReplenish());

            lease = await wait2;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task CanAcquireResourceAsync_QueuesAndGrabsNewest()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(2, QueueProcessingOrder.NewestFirst, 3,
                TimeSpan.FromMinutes(0), autoReplenishment: false));

            var lease = await limiter.WaitAsync(2);
            Assert.True(lease.IsAcquired);

            var wait1 = limiter.WaitAsync(2);
            var wait2 = limiter.WaitAsync();
            Assert.False(wait1.IsCompleted);
            Assert.False(wait2.IsCompleted);

            lease.Dispose();
            Assert.True(limiter.TryReplenish());

            // second queued item completes first with NewestFirst
            lease = await wait2;
            Assert.True(lease.IsAcquired);
            Assert.False(wait1.IsCompleted);

            lease.Dispose();
            Assert.Equal(1, limiter.GetAvailablePermits());
            Assert.True(limiter.TryReplenish());

            lease = await wait1;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task FailsWhenQueuingMoreThanLimit_OldestFirst()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(1, QueueProcessingOrder.OldestFirst, 1,
                TimeSpan.Zero, autoReplenishment: false));
            using var lease = limiter.Acquire(1);
            var wait = limiter.WaitAsync(1);

            var failedLease = await limiter.WaitAsync(1);
            Assert.False(failedLease.IsAcquired);
            Assert.True(failedLease.TryGetMetadata(MetadataName.RetryAfter, out var timeSpan));
            Assert.Equal(TimeSpan.Zero, timeSpan);
        }

        [Fact]
        public override async Task DropsOldestWhenQueuingMoreThanLimit_NewestFirst()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1,
                   TimeSpan.Zero, autoReplenishment: false));
            var lease = limiter.Acquire(1);
            var wait = limiter.WaitAsync(1);
            Assert.False(wait.IsCompleted);

            var wait2 = limiter.WaitAsync(1);
            var lease1 = await wait;
            Assert.False(lease1.IsAcquired);
            Assert.False(wait2.IsCompleted);

            limiter.TryReplenish();

            lease = await wait2;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task DropsMultipleOldestWhenQueuingMoreThanLimit_NewestFirst()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(2, QueueProcessingOrder.NewestFirst, 2,
                   TimeSpan.Zero, autoReplenishment: false));
            var lease = limiter.Acquire(2);
            Assert.True(lease.IsAcquired);
            var wait = limiter.WaitAsync(1);
            Assert.False(wait.IsCompleted);

            var wait2 = limiter.WaitAsync(1);
            Assert.False(wait2.IsCompleted);

            var wait3 = limiter.WaitAsync(2);
            var lease1 = await wait;
            var lease2 = await wait2;
            Assert.False(lease1.IsAcquired);
            Assert.False(lease2.IsAcquired);
            Assert.False(wait3.IsCompleted);

            limiter.TryReplenish();

            lease = await wait3;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task DropsRequestedLeaseIfPermitCountGreaterThanQueueLimitAndNoAvailability_NewestFirst()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(2, QueueProcessingOrder.NewestFirst, 1,
                   TimeSpan.Zero, autoReplenishment: false));
            var lease = limiter.Acquire(2);
            Assert.True(lease.IsAcquired);

            // Fill queue
            var wait = limiter.WaitAsync(1);
            Assert.False(wait.IsCompleted);

            var lease1 = await limiter.WaitAsync(2);
            Assert.False(lease1.IsAcquired);

            limiter.TryReplenish();

            lease = await wait;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task QueueAvailableAfterQueueLimitHitAndResources_BecomeAvailable()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(1, QueueProcessingOrder.OldestFirst, 1,
                TimeSpan.Zero, autoReplenishment: false));
            var lease = limiter.Acquire(1);
            var wait = limiter.WaitAsync(1);

            var failedLease = await limiter.WaitAsync(1);
            Assert.False(failedLease.IsAcquired);

            limiter.TryReplenish();
            lease = await wait;
            Assert.True(lease.IsAcquired);

            wait = limiter.WaitAsync(1);
            Assert.False(wait.IsCompleted);

            limiter.TryReplenish();
            lease = await wait;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task LargeAcquiresAndQueuesDoNotIntegerOverflow()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(int.MaxValue, QueueProcessingOrder.NewestFirst, int.MaxValue,
                TimeSpan.Zero, autoReplenishment: false));
            var lease = limiter.Acquire(int.MaxValue);
            Assert.True(lease.IsAcquired);

            // Fill queue
            var wait = limiter.WaitAsync(3);
            Assert.False(wait.IsCompleted);

            var wait2 = limiter.WaitAsync(int.MaxValue);
            Assert.False(wait2.IsCompleted);

            var lease1 = await wait;
            Assert.False(lease1.IsAcquired);

            limiter.TryReplenish();
            var lease2 = await wait2;
            Assert.True(lease2.IsAcquired);
        }

        [Fact]
        public override void ThrowsWhenAcquiringMoreThanLimit()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1,
                TimeSpan.Zero, autoReplenishment: false));
            Assert.Throws<ArgumentOutOfRangeException>(() => limiter.Acquire(2));
        }

        [Fact]
        public override async Task ThrowsWhenWaitingForMoreThanLimit()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1,
                TimeSpan.Zero, autoReplenishment: false));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await limiter.WaitAsync(2));
        }

        [Fact]
        public override void ThrowsWhenAcquiringLessThanZero()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1,
                TimeSpan.Zero, autoReplenishment: false));
            Assert.Throws<ArgumentOutOfRangeException>(() => limiter.Acquire(-1));
        }

        [Fact]
        public override async Task ThrowsWhenWaitingForLessThanZero()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1,
                TimeSpan.Zero, autoReplenishment: false));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await limiter.WaitAsync(-1));
        }

        [Fact]
        public override void AcquireZero_WithAvailability()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1,
                TimeSpan.Zero, autoReplenishment: false));

            using var lease = limiter.Acquire(0);
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override void AcquireZero_WithoutAvailability()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1,
                TimeSpan.Zero, autoReplenishment: false));
            using var lease = limiter.Acquire(1);
            Assert.True(lease.IsAcquired);

            var lease2 = limiter.Acquire(0);
            Assert.False(lease2.IsAcquired);
            lease2.Dispose();
        }

        [Fact]
        public override async Task WaitAsyncZero_WithAvailability()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1,
                TimeSpan.Zero, autoReplenishment: false));

            using var lease = await limiter.WaitAsync(0);
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task WaitAsyncZero_WithoutAvailabilityWaitsForAvailability()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1,
                TimeSpan.Zero, autoReplenishment: false));
            var lease = await limiter.WaitAsync(1);
            Assert.True(lease.IsAcquired);

            var wait = limiter.WaitAsync(0);
            Assert.False(wait.IsCompleted);

            lease.Dispose();
            Assert.True(limiter.TryReplenish());
            using var lease2 = await wait;
            Assert.True(lease2.IsAcquired);
        }

        [Fact]
        public override async Task CanDequeueMultipleResourcesAtOnce()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(2, QueueProcessingOrder.OldestFirst, 2,
                TimeSpan.Zero, autoReplenishment: false));
            using var lease = await limiter.WaitAsync(2);
            Assert.True(lease.IsAcquired);

            var wait1 = limiter.WaitAsync(1);
            var wait2 = limiter.WaitAsync(1);
            Assert.False(wait1.IsCompleted);
            Assert.False(wait2.IsCompleted);

            lease.Dispose();
            Assert.True(limiter.TryReplenish());

            var lease1 = await wait1;
            var lease2 = await wait2;
            Assert.True(lease1.IsAcquired);
            Assert.True(lease2.IsAcquired);
        }

        [Fact]
        public override async Task CanCancelWaitAsyncAfterQueuing()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(1, QueueProcessingOrder.OldestFirst, 1,
                TimeSpan.Zero, autoReplenishment: false));
            var lease = limiter.Acquire(1);
            Assert.True(lease.IsAcquired);

            var cts = new CancellationTokenSource();
            var wait = limiter.WaitAsync(1, cts.Token);

            cts.Cancel();
            var ex = await Assert.ThrowsAsync<TaskCanceledException>(() => wait.AsTask());
            Assert.Equal(cts.Token, ex.CancellationToken);

            lease.Dispose();
            Assert.True(limiter.TryReplenish());

            Assert.Equal(1, limiter.GetAvailablePermits());
        }

        [Fact]
        public override async Task CanCancelWaitAsyncBeforeQueuing()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(1, QueueProcessingOrder.OldestFirst, 1,
                TimeSpan.Zero, autoReplenishment: false));
            var lease = limiter.Acquire(1);
            Assert.True(lease.IsAcquired);

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var ex = await Assert.ThrowsAsync<TaskCanceledException>(() => limiter.WaitAsync(1, cts.Token).AsTask());
            Assert.Equal(cts.Token, ex.CancellationToken);

            lease.Dispose();
            Assert.True(limiter.TryReplenish());

            Assert.Equal(1, limiter.GetAvailablePermits());
        }

        [Fact]
        public override async Task CancelUpdatesQueueLimit()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(1, QueueProcessingOrder.OldestFirst, 1,
                TimeSpan.Zero, autoReplenishment: false));
            var lease = limiter.Acquire(1);
            Assert.True(lease.IsAcquired);

            var cts = new CancellationTokenSource();
            var wait = limiter.WaitAsync(1, cts.Token);

            cts.Cancel();
            var ex = await Assert.ThrowsAsync<TaskCanceledException>(() => wait.AsTask());
            Assert.Equal(cts.Token, ex.CancellationToken);

            wait = limiter.WaitAsync(1);
            Assert.False(wait.IsCompleted);

            limiter.TryReplenish();
            lease = await wait;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override void NoMetadataOnAcquiredLease()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(1, QueueProcessingOrder.OldestFirst, 1,
                TimeSpan.Zero, autoReplenishment: false));
            using var lease = limiter.Acquire(1);
            Assert.False(lease.TryGetMetadata(MetadataName.RetryAfter, out _));
        }

        [Fact]
        public override void MetadataNamesContainsAllMetadata()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(1, QueueProcessingOrder.OldestFirst, 1,
                TimeSpan.Zero, autoReplenishment: false));
            using var lease = limiter.Acquire(1);
            Assert.Collection(lease.MetadataNames, metadataName => Assert.Equal(metadataName, MetadataName.RetryAfter.Name));
        }

        [Fact]
        public override async Task DisposeReleasesQueuedAcquires()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(1, QueueProcessingOrder.OldestFirst, 3,
                TimeSpan.Zero, autoReplenishment: false));
            var lease = limiter.Acquire(1);
            var wait1 = limiter.WaitAsync(1);
            var wait2 = limiter.WaitAsync(1);
            var wait3 = limiter.WaitAsync(1);
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
            await Assert.ThrowsAsync<ObjectDisposedException>(() => limiter.WaitAsync(1).AsTask());
        }

        [Fact]
        public override async Task DisposeAsyncReleasesQueuedAcquires()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(1, QueueProcessingOrder.OldestFirst, 3,
                TimeSpan.Zero, autoReplenishment: false));
            var lease = limiter.Acquire(1);
            var wait1 = limiter.WaitAsync(1);
            var wait2 = limiter.WaitAsync(1);
            var wait3 = limiter.WaitAsync(1);
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
            await Assert.ThrowsAsync<ObjectDisposedException>(() => limiter.WaitAsync(1).AsTask());
        }

        [Fact]
        public async Task RetryMetadataOnFailedWaitAsync()
        {
            var options = new FixedWindowRateLimiterOptions(2, QueueProcessingOrder.OldestFirst, 1,
                TimeSpan.FromSeconds(20), autoReplenishment: false);
            var limiter = new FixedWindowRateLimiter(options);

            using var lease = limiter.Acquire(2);

            var failedLease = await limiter.WaitAsync(2);
            Assert.False(failedLease.IsAcquired);
            Assert.True(failedLease.TryGetMetadata(MetadataName.RetryAfter.Name, out var metadata));
            var metaDataTime = Assert.IsType<TimeSpan>(metadata);
            Assert.Equal(options.Window.Ticks, metaDataTime.Ticks);

            Assert.True(failedLease.TryGetMetadata(MetadataName.RetryAfter, out var typedMetadata));
            Assert.Equal(options.Window.Ticks, typedMetadata.Ticks);
            Assert.Collection(failedLease.MetadataNames, item => item.Equals(MetadataName.RetryAfter.Name));
        }

        [Fact]
        public async Task CorrectRetryMetadataWithQueuedItem()
        {
            var options = new FixedWindowRateLimiterOptions(2, QueueProcessingOrder.OldestFirst, 1,
                TimeSpan.FromSeconds(20), autoReplenishment: false);
            var limiter = new FixedWindowRateLimiter(options);

            using var lease = limiter.Acquire(2);
            // Queue item which changes the retry after time for failed items
            var wait = limiter.WaitAsync(1);
            Assert.False(wait.IsCompleted);

            var failedLease = await limiter.WaitAsync(2);
            Assert.False(failedLease.IsAcquired);
            Assert.True(failedLease.TryGetMetadata(MetadataName.RetryAfter, out var typedMetadata));
            Assert.Equal(options.Window.Ticks, typedMetadata.Ticks);
        }


        [Fact]
        public async Task CorrectRetryMetadataWithNonZeroAvailableItems()
        {
            var options = new FixedWindowRateLimiterOptions(3, QueueProcessingOrder.OldestFirst, 1,
                TimeSpan.FromSeconds(20), autoReplenishment: false);
            var limiter = new FixedWindowRateLimiter(options);

            using var lease = limiter.Acquire(2);

            var failedLease = await limiter.WaitAsync(3);
            Assert.False(failedLease.IsAcquired);
            Assert.True(failedLease.TryGetMetadata(MetadataName.RetryAfter, out var typedMetadata));
            Assert.Equal(options.Window.Ticks, typedMetadata.Ticks);
        }

        [Fact]
        public void TryReplenishWithAutoReplenish_ReturnsFalse()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(2, QueueProcessingOrder.OldestFirst, 1,
                TimeSpan.FromSeconds(1), autoReplenishment: true));
            Assert.Equal(2, limiter.GetAvailablePermits());
            Assert.False(limiter.TryReplenish());
            Assert.Equal(2, limiter.GetAvailablePermits());
        }

        [Fact]
        public async Task AutoReplenish_ReplenishesCounters()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(2, QueueProcessingOrder.OldestFirst, 1,
                TimeSpan.FromMilliseconds(1000), autoReplenishment: true));
            Assert.Equal(2, limiter.GetAvailablePermits());
            limiter.Acquire(2);

            var lease = await limiter.WaitAsync(1);
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task CanAcquireResourcesWithWaitAsyncWithQueuedItemsIfNewestFirst()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(2, QueueProcessingOrder.NewestFirst, 2,
                TimeSpan.Zero, autoReplenishment: false));

            var lease = limiter.Acquire(1);
            Assert.True(lease.IsAcquired);

            var wait = limiter.WaitAsync(2);
            Assert.False(wait.IsCompleted);

            Assert.Equal(1, limiter.GetAvailablePermits());
            lease = await limiter.WaitAsync(1);
            Assert.True(lease.IsAcquired);
            Assert.False(wait.IsCompleted);

            limiter.TryReplenish();

            lease = await wait;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task CannotAcquireResourcesWithWaitAsyncWithQueuedItemsIfOldestFirst()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(2, QueueProcessingOrder.OldestFirst, 3,
                TimeSpan.Zero, autoReplenishment: false));

            var lease = limiter.Acquire(1);
            Assert.True(lease.IsAcquired);

            var wait = limiter.WaitAsync(2);
            var wait2 = limiter.WaitAsync(1);
            Assert.False(wait.IsCompleted);
            Assert.False(wait2.IsCompleted);

            limiter.TryReplenish();

            lease = await wait;
            Assert.True(lease.IsAcquired);
            Assert.False(wait2.IsCompleted);

            limiter.TryReplenish();

            lease = await wait2;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task CanAcquireResourcesWithAcquireWithQueuedItemsIfNewestFirst()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(2, QueueProcessingOrder.NewestFirst, 3,
                TimeSpan.Zero, autoReplenishment: false));

            var lease = limiter.Acquire(1);
            Assert.True(lease.IsAcquired);

            var wait = limiter.WaitAsync(2);
            Assert.False(wait.IsCompleted);

            lease = limiter.Acquire(1);
            Assert.True(lease.IsAcquired);
            Assert.False(wait.IsCompleted);

            limiter.TryReplenish();

            lease = await wait;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task CannotAcquireResourcesWithAcquireWithQueuedItemsIfOldestFirst()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(2, QueueProcessingOrder.OldestFirst, 3,
                TimeSpan.Zero, autoReplenishment: false));

            var lease = limiter.Acquire(1);
            Assert.True(lease.IsAcquired);

            var wait = limiter.WaitAsync(2);
            Assert.False(wait.IsCompleted);

            lease = limiter.Acquire(1);
            Assert.False(lease.IsAcquired);

            limiter.TryReplenish();

            lease = await wait;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override void NullIdleDurationWhenActive()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(1, QueueProcessingOrder.OldestFirst, 2,
                   TimeSpan.FromMilliseconds(2), autoReplenishment: false));
            limiter.Acquire(1);
            Assert.Null(limiter.IdleDuration);
        }

        [Fact]
        public override async Task IdleDurationUpdatesWhenIdle()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(1, QueueProcessingOrder.OldestFirst, 2,
                TimeSpan.FromMilliseconds(2), autoReplenishment: false));
            Assert.NotNull(limiter.IdleDuration);
            var previousDuration = limiter.IdleDuration;
            await Task.Delay(15);
            Assert.True(previousDuration < limiter.IdleDuration);
        }

        [Fact]
        public override void IdleDurationUpdatesWhenChangingFromActive()
        {
            var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(1, QueueProcessingOrder.OldestFirst, 2,
                TimeSpan.Zero, autoReplenishment: false));
            limiter.Acquire(1);
            limiter.TryReplenish();
            Assert.NotNull(limiter.IdleDuration);
        }

        [Fact]
        public void ReplenishingRateLimiterPropertiesHaveCorrectValues()
        {
            var replenishPeriod = TimeSpan.FromMinutes(1);
            using ReplenishingRateLimiter limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(1, QueueProcessingOrder.OldestFirst, 2,
                replenishPeriod, autoReplenishment: true));
            Assert.True(limiter.IsAutoReplenishing);
            Assert.Equal(replenishPeriod, limiter.ReplenishmentPeriod);

            replenishPeriod = TimeSpan.FromSeconds(2);
            using ReplenishingRateLimiter limiter2 = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(1, QueueProcessingOrder.OldestFirst, 2,
                replenishPeriod, autoReplenishment: false));
            Assert.False(limiter2.IsAutoReplenishing);
            Assert.Equal(replenishPeriod, limiter2.ReplenishmentPeriod);
        }
    }
}
