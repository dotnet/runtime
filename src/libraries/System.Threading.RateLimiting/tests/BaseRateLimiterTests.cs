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
        public abstract Task FailsWhenQueuingMoreThanLimit_OldestFirst();

        [Fact]
        public abstract Task DropsOldestWhenQueuingMoreThanLimit_NewestFirst();

        [Fact]
        public abstract Task DropsMultipleOldestWhenQueuingMoreThanLimit_NewestFirst();

        [Fact]
        public abstract Task DropsRequestedLeaseIfPermitCountGreaterThanQueueLimitAndNoAvailability_NewestFirst();

        [Fact]
        public abstract Task QueueAvailableAfterQueueLimitHitAndResources_BecomeAvailable();

        [Fact]
        public abstract Task LargeAcquiresAndQueuesDoNotIntegerOverflow();

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
        public abstract Task AcquireAsyncZero_WithAvailability();

        [Fact]
        public abstract Task AcquireAsyncZero_WithoutAvailabilityWaitsForAvailability();

        [Fact]
        public abstract Task CanDequeueMultipleResourcesAtOnce();

        [Fact]
        public abstract Task CanAcquireResourcesWithAcquireAsyncWithQueuedItemsIfNewestFirst();

        [Fact]
        public abstract Task CannotAcquireResourcesWithAcquireAsyncWithQueuedItemsIfOldestFirst();

        [Fact]
        public abstract Task CanCancelAcquireAsyncAfterQueuing();

        [Fact]
        public abstract Task CanFillQueueWithOldestFirstAfterCancelingFirstQueuedRequestManually();

        [Fact]
        public abstract Task CanCancelAcquireAsyncBeforeQueuing();

        [Fact]
        public abstract Task CanFillQueueWithNewestFirstAfterCancelingQueuedRequestWithAnotherQueuedRequest();

        [Fact]
        public abstract Task CanDisposeAfterCancelingQueuedRequest();

        [Fact]
        public abstract Task CancelUpdatesQueueLimit();

        [Fact]
        public abstract Task CanAcquireResourcesWithAcquireWithQueuedItemsIfNewestFirst();

        [Fact]
        public abstract Task CannotAcquireResourcesWithAcquireWithQueuedItemsIfOldestFirst();

        [Fact]
        public abstract void NoMetadataOnAcquiredLease();

        [Fact]
        public abstract void MetadataNamesContainsAllMetadata();

        [Fact]
        public abstract Task DisposeReleasesQueuedAcquires();

        [Fact]
        public abstract Task DisposeAsyncReleasesQueuedAcquires();

        [Fact]
        public abstract void NullIdleDurationWhenActive();

        [Fact]
        public abstract Task IdleDurationUpdatesWhenIdle();

        [Fact]
        public abstract void IdleDurationUpdatesWhenChangingFromActive();

        [Fact]
        public abstract void GetStatisticsReturnsNewInstances();

        [Fact]
        public abstract Task GetStatisticsHasCorrectValues();

        [Fact]
        public abstract Task GetStatisticsWithZeroPermitCount();

        [Fact]
        public abstract void GetStatisticsThrowsAfterDispose();
    }
}
