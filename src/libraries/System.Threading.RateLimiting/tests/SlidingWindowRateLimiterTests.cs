// Licensed to the .NET Foundation under one or more agreements.
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
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1,
                Window = TimeSpan.Zero,
                SegmentsPerWindow = 2,
                AutoReplenishment = false
            });
            var lease = limiter.AttemptAcquire();

            Assert.True(lease.IsAcquired);
            Assert.False(limiter.AttemptAcquire().IsAcquired);

            lease.Dispose();
            Assert.False(limiter.AttemptAcquire().IsAcquired);
            Assert.True(limiter.TryReplenish());
            Assert.True(limiter.TryReplenish());

            Assert.True(limiter.AttemptAcquire().IsAcquired);
        }

        [Fact]
        public override void InvalidOptionsThrows()
        {
            Assert.Throws<ArgumentException>(
                () => new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = -1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1,
                Window = TimeSpan.FromMinutes(2),
                SegmentsPerWindow = 1,
                AutoReplenishment = false
            }));
            Assert.Throws<ArgumentException>(
                () => new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = -1,
                Window = TimeSpan.FromMinutes(2),
                SegmentsPerWindow = 1,
                AutoReplenishment = false
            }));
            Assert.Throws<ArgumentException>(
                () => new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1,
                Window = TimeSpan.FromMinutes(2),
                SegmentsPerWindow = -1,
                AutoReplenishment = false
            }));
            Assert.Throws<ArgumentException>(
                () => new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1,
                Window = TimeSpan.MinValue,
                SegmentsPerWindow = 1,
                AutoReplenishment = false
            }));
        }

        [Fact]
        public override async Task CanAcquireResourceAsync()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 4,
                Window = TimeSpan.Zero,
                SegmentsPerWindow = 2,
                AutoReplenishment = false
            });

            using var lease = await limiter.AcquireAsync();

            Assert.True(lease.IsAcquired);
            var wait = limiter.AcquireAsync(2);
            Assert.False(wait.IsCompleted);

            Assert.True(limiter.TryReplenish());

            Assert.False(wait.IsCompleted);

            var wait2 = limiter.AcquireAsync(2);
            Assert.False(wait2.IsCompleted);

            Assert.True(limiter.TryReplenish());

            Assert.True((await wait2).IsAcquired);
        }

        [Fact]
        public async Task CanAcquireMultipleRequestsAsync()
        {
            // This test verifies the following behavior
            // 1. when we have available permits after replenish to serve the queued requests
            // 2. when the oldest item from queue is remove to accommodate new requests (QueueProcessingOrder: NewestFirst)
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 4,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 4,
                Window = TimeSpan.Zero,
                SegmentsPerWindow = 3,
                AutoReplenishment = false
            });

            using var lease = await limiter.AcquireAsync(2);

            Assert.True(lease.IsAcquired);
            var wait = limiter.AcquireAsync(3);
            Assert.False(wait.IsCompleted);

            Assert.True(limiter.TryReplenish());

            Assert.False(wait.IsCompleted);

            var wait2 = limiter.AcquireAsync(2);
            Assert.True(wait2.IsCompleted);

            Assert.True(limiter.TryReplenish());

            var wait3 = limiter.AcquireAsync(2);
            Assert.False(wait3.IsCompleted);

            Assert.True(limiter.TryReplenish());
            Assert.True((await wait3).IsAcquired);

            Assert.False((await wait).IsAcquired);
            Assert.Equal(0, limiter.GetAvailablePermits());
        }

        [Fact]
        public override async Task CanAcquireResourceAsync_QueuesAndGrabsOldest()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 3,
                Window = TimeSpan.FromMinutes(0),
                SegmentsPerWindow = 2,
                AutoReplenishment = false
            });
            var lease = await limiter.AcquireAsync(2);

            Assert.True(lease.IsAcquired);
            var wait1 = limiter.AcquireAsync();
            var wait2 = limiter.AcquireAsync(2);
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
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 3,
                Window = TimeSpan.FromMinutes(0),
                SegmentsPerWindow = 2,
                AutoReplenishment = false
            });

            var lease = await limiter.AcquireAsync(2);
            Assert.True(lease.IsAcquired);

            var wait1 = limiter.AcquireAsync(2);
            var wait2 = limiter.AcquireAsync();
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
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1,
                Window = TimeSpan.Zero,
                SegmentsPerWindow = 2,
                AutoReplenishment = false
            });
            using var lease = limiter.AttemptAcquire(1);
            var wait = limiter.AcquireAsync(1);

            var failedLease = await limiter.AcquireAsync(1);
            Assert.False(failedLease.IsAcquired);
        }

        [Fact]
        public override async Task DropsOldestWhenQueuingMoreThanLimit_NewestFirst()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1,
                Window = TimeSpan.Zero,
                SegmentsPerWindow = 2,
                AutoReplenishment = false
            });
            var lease = limiter.AttemptAcquire(1);
            var wait = limiter.AcquireAsync(1);
            Assert.False(wait.IsCompleted);

            var wait2 = limiter.AcquireAsync(1);
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
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 2,
                Window = TimeSpan.Zero,
                SegmentsPerWindow = 2,
                AutoReplenishment = false
            });
            var lease = limiter.AttemptAcquire(2);
            Assert.True(lease.IsAcquired);
            var wait = limiter.AcquireAsync(1);
            Assert.False(wait.IsCompleted);

            var wait2 = limiter.AcquireAsync(1);
            Assert.False(wait2.IsCompleted);

            var wait3 = limiter.AcquireAsync(2);
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
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1,
                Window = TimeSpan.Zero,
                SegmentsPerWindow = 2,
                AutoReplenishment = false
            });
            var lease = limiter.AttemptAcquire(2);
            Assert.True(lease.IsAcquired);

            // Fill queue
            var wait = limiter.AcquireAsync(1);
            Assert.False(wait.IsCompleted);

            var lease1 = await limiter.AcquireAsync(2);
            Assert.False(lease1.IsAcquired);

            limiter.TryReplenish();
            limiter.TryReplenish();

            lease = await wait;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task QueueAvailableAfterQueueLimitHitAndResources_BecomeAvailable()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 3,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2,
                Window = TimeSpan.Zero,
                SegmentsPerWindow = 3,
                AutoReplenishment = false
            });
            var lease = limiter.AttemptAcquire(2);
            var wait = limiter.AcquireAsync(2);

            var failedLease = await limiter.AcquireAsync(2);
            Assert.False(failedLease.IsAcquired);

            limiter.TryReplenish();
            limiter.TryReplenish();
            Assert.False(wait.IsCompleted);

            limiter.TryReplenish();
            lease = await wait;
            Assert.True(lease.IsAcquired);

            wait = limiter.AcquireAsync(2);
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
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = int.MaxValue,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = int.MaxValue,
                Window = TimeSpan.Zero,
                SegmentsPerWindow = 2,
                AutoReplenishment = false
            });
            var lease = limiter.AttemptAcquire(int.MaxValue);
            Assert.True(lease.IsAcquired);

            // Fill queue
            var wait = limiter.AcquireAsync(3);
            Assert.False(wait.IsCompleted);

            var wait2 = limiter.AcquireAsync(int.MaxValue);
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
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1,
                Window = TimeSpan.Zero,
                SegmentsPerWindow = 1,
                AutoReplenishment = false
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => limiter.AttemptAcquire(2));
        }

        [Fact]
        public override async Task ThrowsWhenWaitingForMoreThanLimit()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1,
                Window = TimeSpan.Zero,
                SegmentsPerWindow = 1,
                AutoReplenishment = false
            });
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await limiter.AcquireAsync(2));
        }

        [Fact]
        public override void ThrowsWhenAcquiringLessThanZero()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1,
                Window = TimeSpan.Zero,
                SegmentsPerWindow = 1,
                AutoReplenishment = false
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => limiter.AttemptAcquire(-1));
        }

        [Fact]
        public override async Task ThrowsWhenWaitingForLessThanZero()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1,
                Window = TimeSpan.Zero,
                SegmentsPerWindow = 1,
                AutoReplenishment = false
            });
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await limiter.AcquireAsync(-1));
        }

        [Fact]
        public override void AcquireZero_WithAvailability()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1,
                Window = TimeSpan.Zero,
                SegmentsPerWindow = 1,
                AutoReplenishment = false
            });

            using var lease = limiter.AttemptAcquire(0);
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override void AcquireZero_WithoutAvailability()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1,
                Window = TimeSpan.Zero,
                SegmentsPerWindow = 1,
                AutoReplenishment = false
            });
            using var lease = limiter.AttemptAcquire(1);
            Assert.True(lease.IsAcquired);

            var lease2 = limiter.AttemptAcquire(0);
            Assert.False(lease2.IsAcquired);
            lease2.Dispose();
        }

        [Fact]
        public override async Task AcquireAsyncZero_WithAvailability()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1,
                Window = TimeSpan.Zero,
                SegmentsPerWindow = 1,
                AutoReplenishment = false
            });

            using var lease = await limiter.AcquireAsync(0);
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task AcquireAsyncZero_WithoutAvailabilityWaitsForAvailability()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1,
                Window = TimeSpan.Zero,
                SegmentsPerWindow = 2,
                AutoReplenishment = false
            });
            var lease = await limiter.AcquireAsync(1);
            Assert.True(lease.IsAcquired);

            var wait = limiter.AcquireAsync(0);
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
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 4,
                Window = TimeSpan.Zero,
                SegmentsPerWindow = 2,
                AutoReplenishment = false
            });
            using var lease = await limiter.AcquireAsync(2);
            Assert.True(lease.IsAcquired);

            var wait1 = limiter.AcquireAsync(1);
            var wait2 = limiter.AcquireAsync(1);
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
        public override async Task CanCancelAcquireAsyncAfterQueuing()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1,
                Window = TimeSpan.Zero,
                SegmentsPerWindow = 2,
                AutoReplenishment = false
            });
            var lease = limiter.AttemptAcquire(2);
            Assert.True(lease.IsAcquired);

            var cts = new CancellationTokenSource();
            var wait = limiter.AcquireAsync(1, cts.Token);

            cts.Cancel();
            var ex = await Assert.ThrowsAsync<TaskCanceledException>(() => wait.AsTask());
            Assert.Equal(cts.Token, ex.CancellationToken);

            lease.Dispose();
            Assert.True(limiter.TryReplenish());

            Assert.Equal(0, limiter.GetAvailablePermits());
        }

        [Fact]
        public override async Task CanCancelAcquireAsyncBeforeQueuing()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1,
                Window = TimeSpan.Zero,
                SegmentsPerWindow = 2,
                AutoReplenishment = false
            });
            var lease = limiter.AttemptAcquire(2);
            Assert.True(lease.IsAcquired);

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var ex = await Assert.ThrowsAsync<TaskCanceledException>(() => limiter.AcquireAsync(1, cts.Token).AsTask());
            Assert.Equal(cts.Token, ex.CancellationToken);

            lease.Dispose();
            Assert.True(limiter.TryReplenish());

            Assert.Equal(0, limiter.GetAvailablePermits());
        }

        [Fact]
        public override async Task CancelUpdatesQueueLimit()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1,
                Window = TimeSpan.Zero,
                SegmentsPerWindow = 2,
                AutoReplenishment = false
            });
            var lease = limiter.AttemptAcquire(2);
            Assert.True(lease.IsAcquired);

            var cts = new CancellationTokenSource();
            var wait = limiter.AcquireAsync(1, cts.Token);

            cts.Cancel();
            var ex = await Assert.ThrowsAsync<TaskCanceledException>(() => wait.AsTask());
            Assert.Equal(cts.Token, ex.CancellationToken);

            wait = limiter.AcquireAsync(1);
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
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1,
                Window = TimeSpan.Zero,
                SegmentsPerWindow = 2,
                AutoReplenishment = false
            });
            using var lease = limiter.AttemptAcquire(1);
            Assert.False(lease.TryGetMetadata(MetadataName.RetryAfter, out _));
        }

        [Fact]
        public override void MetadataNamesContainsAllMetadata()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1,
                Window = TimeSpan.Zero,
                SegmentsPerWindow = 1,
                AutoReplenishment = false
            });
            using var lease = limiter.AttemptAcquire(1);
            Assert.Collection(lease.MetadataNames, metadataName => Assert.Equal(metadataName, MetadataName.RetryAfter.Name));
        }

        [Fact]
        public override async Task DisposeReleasesQueuedAcquires()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 3,
                Window = TimeSpan.Zero,
                SegmentsPerWindow = 1,
                AutoReplenishment = false
            });
            var lease = limiter.AttemptAcquire(1);
            var wait1 = limiter.AcquireAsync(1);
            var wait2 = limiter.AcquireAsync(1);
            var wait3 = limiter.AcquireAsync(1);
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
            Assert.Throws<ObjectDisposedException>(() => limiter.AttemptAcquire(1));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => limiter.AcquireAsync(1).AsTask());
        }

        [Fact]
        public override async Task DisposeAsyncReleasesQueuedAcquires()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 3,
                Window = TimeSpan.Zero,
                SegmentsPerWindow = 2,
                AutoReplenishment = false
            });
            var lease = limiter.AttemptAcquire(1);
            var wait1 = limiter.AcquireAsync(1);
            var wait2 = limiter.AcquireAsync(1);
            var wait3 = limiter.AcquireAsync(1);
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
            Assert.Throws<ObjectDisposedException>(() => limiter.AttemptAcquire(1));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => limiter.AcquireAsync(1).AsTask());
        }

        [Fact]
        public void TryReplenishWithAutoReplenish_ReturnsFalse()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1,
                Window = TimeSpan.FromSeconds(1),
                SegmentsPerWindow = 1,
                AutoReplenishment = true
            });
            Assert.Equal(2, limiter.GetAvailablePermits());
            Assert.False(limiter.TryReplenish());
            Assert.Equal(2, limiter.GetAvailablePermits());
        }

        [Fact]
        public async Task AutoReplenish_ReplenishesCounters()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1,
                Window = TimeSpan.FromMilliseconds(1000),
                SegmentsPerWindow = 2,
                AutoReplenishment = true
            });
            Assert.Equal(2, limiter.GetAvailablePermits());
            limiter.AttemptAcquire(2);

            var lease = await limiter.AcquireAsync(1);
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task CanAcquireResourcesWithAcquireAsyncWithQueuedItemsIfNewestFirst()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 2,
                Window = TimeSpan.Zero,
                SegmentsPerWindow = 3,
                AutoReplenishment = false
            });

            var lease = limiter.AttemptAcquire(1);
            Assert.True(lease.IsAcquired);

            var wait = limiter.AcquireAsync(2);
            Assert.False(wait.IsCompleted);

            Assert.Equal(1, limiter.GetAvailablePermits());
            lease = await limiter.AcquireAsync(1);
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
        public override async Task CannotAcquireResourcesWithAcquireAsyncWithQueuedItemsIfOldestFirst()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 3,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5,
                Window = TimeSpan.Zero,
                SegmentsPerWindow = 2,
                AutoReplenishment = false
            });

            var lease = limiter.AttemptAcquire(3);
            Assert.True(lease.IsAcquired);

            var wait = limiter.AcquireAsync(2);
            var wait2 = limiter.AcquireAsync(2);
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
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 3,
                Window = TimeSpan.Zero,
                SegmentsPerWindow = 2,
                AutoReplenishment = false
            });

            var lease = limiter.AttemptAcquire(1);
            Assert.True(lease.IsAcquired);

            var wait = limiter.AcquireAsync(2);
            Assert.False(wait.IsCompleted);

            lease = limiter.AttemptAcquire(1);
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
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 3,
                Window = TimeSpan.Zero,
                SegmentsPerWindow = 2,
                AutoReplenishment = false
            });

            var lease = limiter.AttemptAcquire(1);
            Assert.True(lease.IsAcquired);

            var wait = limiter.AcquireAsync(2);
            Assert.False(wait.IsCompleted);

            lease = limiter.AttemptAcquire(1);
            Assert.False(lease.IsAcquired);

            limiter.TryReplenish();
            Assert.True(limiter.TryReplenish());

            lease = await wait;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override void NullIdleDurationWhenActive()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2,
                Window = TimeSpan.FromMilliseconds(2),
                SegmentsPerWindow = 1,
                AutoReplenishment = false
            });
            limiter.AttemptAcquire(1);
            Assert.Null(limiter.IdleDuration);
        }

        [Fact]
        public override async Task IdleDurationUpdatesWhenIdle()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 3,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2,
                Window = TimeSpan.FromMilliseconds(2),
                SegmentsPerWindow = 2,
                AutoReplenishment = false
            });
            Assert.NotNull(limiter.IdleDuration);
            var previousDuration = limiter.IdleDuration;
            await Task.Delay(15);
            Assert.True(previousDuration < limiter.IdleDuration);
        }

        [Fact]
        public override void IdleDurationUpdatesWhenChangingFromActive()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2,
                Window = TimeSpan.Zero,
                SegmentsPerWindow = 2,
                AutoReplenishment = false
            });
            limiter.AttemptAcquire(1);
            limiter.TryReplenish();
            limiter.TryReplenish();
            Assert.NotNull(limiter.IdleDuration);
        }

        [Fact]
        public void ReplenishingRateLimiterPropertiesHaveCorrectValues()
        {
            var replenishPeriod = TimeSpan.FromMinutes(1);
            using ReplenishingRateLimiter limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2,
                Window = replenishPeriod,
                SegmentsPerWindow = 1,
                AutoReplenishment = true
            });
            Assert.True(limiter.IsAutoReplenishing);
            Assert.Equal(replenishPeriod, limiter.ReplenishmentPeriod);

            replenishPeriod = TimeSpan.FromSeconds(2);
            using ReplenishingRateLimiter limiter2 = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2,
                Window = replenishPeriod,
                SegmentsPerWindow = 1,
                AutoReplenishment = false
            });
            Assert.False(limiter2.IsAutoReplenishing);
            Assert.Equal(replenishPeriod, limiter2.ReplenishmentPeriod);
        }

        [Fact]
        public override async Task CanFillQueueWithNewestFirstAfterCancelingQueuedRequestWithAnotherQueuedRequest()
        {
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 2,
                Window = TimeSpan.Zero,
                SegmentsPerWindow = 2,
                AutoReplenishment = false
            });
            var lease = limiter.AttemptAcquire(2);
            Assert.True(lease.IsAcquired);

            var cts = new CancellationTokenSource();
            var wait = limiter.AcquireAsync(1, cts.Token);

            // Add another item to queue, will be completed as failed later when we queue another item
            var wait2 = limiter.AcquireAsync(1);
            Assert.False(wait.IsCompleted);

            cts.Cancel();
            var ex = await Assert.ThrowsAsync<TaskCanceledException>(() => wait.AsTask());
            Assert.Equal(cts.Token, ex.CancellationToken);

            lease.Dispose();
            limiter.TryReplenish();

            var wait3 = limiter.AcquireAsync(2);
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
            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1,
                Window = TimeSpan.Zero,
                SegmentsPerWindow = 2,
                AutoReplenishment = false
            });
            var lease = limiter.AttemptAcquire(1);
            Assert.True(lease.IsAcquired);

            var cts = new CancellationTokenSource();
            var wait = limiter.AcquireAsync(1, cts.Token);

            cts.Cancel();
            var ex = await Assert.ThrowsAsync<TaskCanceledException>(() => wait.AsTask());
            Assert.Equal(cts.Token, ex.CancellationToken);

            // Make sure dispose doesn't have any side-effects when dealing with a canceled queued item
            limiter.Dispose();
        }
    }
}
