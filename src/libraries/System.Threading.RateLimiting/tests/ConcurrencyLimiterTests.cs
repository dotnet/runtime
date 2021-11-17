// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting.Tests.Internal;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.RateLimiting.Test
{
    public class ConcurrencyLimiterTests : BaseRateLimiterTests
    {
        [Fact]
        public override void InvalidOptionsThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ConcurrencyLimiterOptions(-1, QueueProcessingOrder.NewestFirst, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ConcurrencyLimiterOptions(1, QueueProcessingOrder.NewestFirst, -1));
        }

        [Fact]
        public override void CanAcquireResource()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1));
            var lease = limiter.Acquire();

            Assert.True(lease.IsAcquired);
            Assert.False(limiter.Acquire().IsAcquired);

            lease.Dispose();

            Assert.True(limiter.Acquire().IsAcquired);
        }

        [Fact]
        public override async Task CanAcquireResourceAsync()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1));
            var lease = await limiter.WaitAsync().DefaultTimeout();

            Assert.True(lease.IsAcquired);
            var wait = limiter.WaitAsync();
            Assert.False(wait.IsCompleted);

            lease.Dispose();

            Assert.True((await wait.DefaultTimeout()).IsAcquired);
        }

        [Fact]
        public override async Task CanAcquireResourceAsync_QueuesAndGrabsOldest()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 2));
            var lease = await limiter.WaitAsync().DefaultTimeout();

            Assert.True(lease.IsAcquired);
            var wait1 = limiter.WaitAsync();
            var wait2 = limiter.WaitAsync();
            Assert.False(wait1.IsCompleted);
            Assert.False(wait2.IsCompleted);

            lease.Dispose();

            lease = await wait1.DefaultTimeout();
            Assert.True(lease.IsAcquired);
            Assert.False(wait2.IsCompleted);

            lease.Dispose();

            lease = await wait2.DefaultTimeout();
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task CanAcquireResourceAsync_QueuesAndGrabsNewest()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.NewestFirst, 2));
            var lease = await limiter.WaitAsync().DefaultTimeout();

            Assert.True(lease.IsAcquired);
            var wait1 = limiter.WaitAsync();
            var wait2 = limiter.WaitAsync();
            Assert.False(wait1.IsCompleted);
            Assert.False(wait2.IsCompleted);

            lease.Dispose();

            // second queued item completes first with NewestFirst
            lease = await wait2.DefaultTimeout();
            Assert.True(lease.IsAcquired);
            Assert.False(wait1.IsCompleted);

            lease.Dispose();

            lease = await wait1.DefaultTimeout();
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task FailsWhenQueuingMoreThanLimit()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1));
            using var lease = limiter.Acquire(1);
            var wait = limiter.WaitAsync(1);

            var failedLease = await limiter.WaitAsync(1).DefaultTimeout();
            Assert.False(failedLease.IsAcquired);
        }

        [Fact]
        public override async Task QueueAvailableAfterQueueLimitHitAndResources_BecomeAvailable()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1));
            var lease = limiter.Acquire(1);
            var wait = limiter.WaitAsync(1);

            var failedLease = await limiter.WaitAsync(1).DefaultTimeout();
            Assert.False(failedLease.IsAcquired);

            lease.Dispose();
            lease = await wait.DefaultTimeout();
            Assert.True(lease.IsAcquired);

            wait = limiter.WaitAsync(1);
            Assert.False(wait.IsCompleted);

            lease.Dispose();
            lease = await wait.DefaultTimeout();
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override void ThrowsWhenAcquiringMoreThanLimit()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1));
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => limiter.Acquire(2));
            Assert.Equal("permitCount", ex.ParamName);
        }

        [Fact]
        public override async Task ThrowsWhenWaitingForMoreThanLimit()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1));
            var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await limiter.WaitAsync(2).DefaultTimeout());
            Assert.Equal("permitCount", ex.ParamName);
        }

        [Fact]
        public override void ThrowsWhenAcquiringLessThanZero()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => limiter.Acquire(-1));
        }

        [Fact]
        public override async Task ThrowsWhenWaitingForLessThanZero()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await limiter.WaitAsync(-1).DefaultTimeout());
        }

        [Fact]
        public override void AcquireZero_WithAvailability()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1));

            using var lease = limiter.Acquire(0);
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override void AcquireZero_WithoutAvailability()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1));
            using var lease = limiter.Acquire(1);
            Assert.True(lease.IsAcquired);

            var lease2 = limiter.Acquire(0);
            Assert.False(lease2.IsAcquired);
            lease2.Dispose();
        }

        [Fact]
        public override async Task WaitAsyncZero_WithAvailability()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1));

            using var lease = await limiter.WaitAsync(0).DefaultTimeout();
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task WaitAsyncZero_WithoutAvailabilityWaitsForAvailability()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1));
            var lease = await limiter.WaitAsync(1).DefaultTimeout();
            Assert.True(lease.IsAcquired);

            var wait = limiter.WaitAsync(0);
            Assert.False(wait.IsCompleted);

            lease.Dispose();
            using var lease2 = await wait.DefaultTimeout();
            Assert.True(lease2.IsAcquired);
        }

        [Fact]
        public override async Task CanDequeueMultipleResourcesAtOnce()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(2, QueueProcessingOrder.OldestFirst, 2));
            using var lease = await limiter.WaitAsync(2).DefaultTimeout();
            Assert.True(lease.IsAcquired);

            var wait1 = limiter.WaitAsync(1);
            var wait2 = limiter.WaitAsync(1);
            Assert.False(wait1.IsCompleted);
            Assert.False(wait2.IsCompleted);

            lease.Dispose();

            var lease1 = await wait1.DefaultTimeout();
            var lease2 = await wait2.DefaultTimeout();
            Assert.True(lease1.IsAcquired);
            Assert.True(lease2.IsAcquired);
        }

        [Fact]
        public override async Task CanAcquireResourcesWithWaitAsyncWithQueuedItemsIfNewestFirst()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(2, QueueProcessingOrder.NewestFirst, 3));
            using var lease = await limiter.WaitAsync(1).DefaultTimeout();
            Assert.True(lease.IsAcquired);

            var wait1 = limiter.WaitAsync(2);
            Assert.False(wait1.IsCompleted);
            var wait2 = limiter.WaitAsync(1);
            var lease2 = await wait2.DefaultTimeout();
            Assert.True(lease2.IsAcquired);

            lease.Dispose();

            Assert.False(wait1.IsCompleted);
            lease2.Dispose();

            var lease1 = await wait1.DefaultTimeout();
            Assert.True(lease1.IsAcquired);
        }

        [Fact]
        public override async Task CannotAcquireResourcesWithWaitAsyncWithQueuedItemsIfOldestFirst()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(2, QueueProcessingOrder.OldestFirst, 3));
            using var lease = await limiter.WaitAsync(1).DefaultTimeout();
            Assert.True(lease.IsAcquired);

            var wait1 = limiter.WaitAsync(2);
            var wait2 = limiter.WaitAsync(1);
            Assert.False(wait1.IsCompleted);
            Assert.False(wait2.IsCompleted);

            lease.Dispose();

            var lease1 = await wait1.DefaultTimeout();
            Assert.True(lease1.IsAcquired);
            Assert.False(wait2.IsCompleted);

            lease1.Dispose();
            var lease2 = await wait2.DefaultTimeout();
            Assert.True(lease2.IsAcquired);
        }

        [Fact]
        public override async Task CanAcquireResourcesWithAcquireWithQueuedItemsIfNewestFirst()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(2, QueueProcessingOrder.NewestFirst, 3));
            using var lease = await limiter.WaitAsync(1).DefaultTimeout();
            Assert.True(lease.IsAcquired);

            var wait1 = limiter.WaitAsync(2);
            Assert.False(wait1.IsCompleted);
            var lease2 = limiter.Acquire(1);
            Assert.True(lease2.IsAcquired);

            lease.Dispose();

            Assert.False(wait1.IsCompleted);
            lease2.Dispose();

            var lease1 = await wait1.DefaultTimeout();
            Assert.True(lease1.IsAcquired);
        }

        [Fact]
        public override async Task CannotAcquireResourcesWithAcquireWithQueuedItemsIfOldestFirst()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(2, QueueProcessingOrder.OldestFirst, 3));
            using var lease = await limiter.WaitAsync(1).DefaultTimeout();
            Assert.True(lease.IsAcquired);

            var wait1 = limiter.WaitAsync(2);
            Assert.False(wait1.IsCompleted);
            var lease2 = limiter.Acquire(1);
            Assert.False(lease2.IsAcquired);

            lease.Dispose();

            var lease1 = await wait1.DefaultTimeout();
            Assert.True(lease1.IsAcquired);
        }

        [Fact]
        public override async Task CanCancelWaitAsyncAfterQueuing()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 1));
            var lease = limiter.Acquire(1);
            Assert.True(lease.IsAcquired);

            var cts = new CancellationTokenSource();
            var wait = limiter.WaitAsync(1, cts.Token);

            cts.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(() => wait.DefaultTimeout());

            lease.Dispose();

            Assert.Equal(1, limiter.GetAvailablePermits());
        }

        [Fact]
        public override async Task CanCancelWaitAsyncBeforeQueuing()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 1));
            var lease = limiter.Acquire(1);
            Assert.True(lease.IsAcquired);

            var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() => limiter.WaitAsync(1, cts.Token).DefaultTimeout());

            lease.Dispose();

            Assert.Equal(1, limiter.GetAvailablePermits());
        }

        [Fact]
        public override void NoMetadataOnAcquiredLease()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 1));
            using var lease = limiter.Acquire(1);
            Assert.Empty(lease.MetadataNames);
            Assert.False(lease.TryGetMetadata(MetadataName.ReasonPhrase.Name, out _));
        }

        [Fact]
        public async Task ReasonMetadataOnFailedWaitAsync()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions(2, QueueProcessingOrder.OldestFirst, 1));
            using var lease = limiter.Acquire(2);

            var failedLease = await limiter.WaitAsync(2).DefaultTimeout();
            Assert.False(failedLease.IsAcquired);
            Assert.True(failedLease.TryGetMetadata(MetadataName.ReasonPhrase.Name, out var metadata));
            Assert.Equal("Queue limit reached", metadata);

            Assert.True(failedLease.TryGetMetadata(MetadataName.ReasonPhrase, out var typedMetadata));
            Assert.Equal("Queue limit reached", typedMetadata);
            Assert.Collection(failedLease.MetadataNames, item => item.Equals(MetadataName.ReasonPhrase.Name));
        }
    }
}
