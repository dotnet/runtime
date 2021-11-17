// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.Threading.RateLimiting.Test
{
    public abstract class BaseRateLimiterTests
    {
        [Fact]
        public abstract void CanAcquireResource();

        [Fact]
        public abstract void InvalidOptionsThrows();

        [Fact]
        public abstract Task CanAcquireResourceAsync();

        [Fact]
        public abstract Task CanAcquireResourceAsync_QueuesAndGrabsOldest();

        [Fact]
        public abstract Task CanAcquireResourceAsync_QueuesAndGrabsNewest();

        [Fact]
        public abstract Task FailsWhenQueuingMoreThanLimit();

        [Fact]
        public abstract Task QueueAvailableAfterQueueLimitHitAndResources_BecomeAvailable();

        [Fact]
        public abstract void ThrowsWhenAcquiringMoreThanLimit();

        [Fact]
        public abstract Task ThrowsWhenWaitingForMoreThanLimit();

        [Fact]
        public abstract void ThrowsWhenAcquiringLessThanZero();

        [Fact]
        public abstract Task ThrowsWhenWaitingForLessThanZero();

        [Fact]
        public abstract void AcquireZero_WithAvailability();

        [Fact]
        public abstract void AcquireZero_WithoutAvailability();

        [Fact]
        public abstract Task WaitAsyncZero_WithAvailability();

        [Fact]
        public abstract Task WaitAsyncZero_WithoutAvailabilityWaitsForAvailability();

        [Fact]
        public abstract Task CanDequeueMultipleResourcesAtOnce();

        [Fact]
        public abstract Task CanAcquireResourcesWithWaitAsyncWithQueuedItemsIfNewestFirst();

        [Fact]
        public abstract Task CannotAcquireResourcesWithWaitAsyncWithQueuedItemsIfOldestFirst();

        [Fact]
        public abstract Task CanCancelWaitAsyncAfterQueuing();

        [Fact]
        public abstract Task CanCancelWaitAsyncBeforeQueuing();

        [Fact]
        public abstract Task CanAcquireResourcesWithAcquireWithQueuedItemsIfNewestFirst();

        [Fact]
        public abstract Task CannotAcquireResourcesWithAcquireWithQueuedItemsIfOldestFirst();

        [Fact]
        public abstract void NoMetadataOnAcquiredLease();
    }
}
