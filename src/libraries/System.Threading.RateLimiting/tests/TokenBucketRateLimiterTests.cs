// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.RateLimiting.Test
{
    public class TokenBucketRateLimiterTests : BaseRateLimiterTests
    {
        [Fact]
        public override void CanAcquireResource()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 1,
                AutoReplenishment = false
            });
            var lease = limiter.AttemptAcquire();

            Assert.True(lease.IsAcquired);
            Assert.False(limiter.AttemptAcquire().IsAcquired);

            // Dispose doesn't change token count
            lease.Dispose();
            Assert.False(limiter.AttemptAcquire().IsAcquired);
            Replenish(limiter, 1L);

            Assert.True(limiter.AttemptAcquire().IsAcquired);
        }

        [Fact]
        public override void InvalidOptionsThrows()
        {
            AssertExtensions.Throws<ArgumentException>("options",
                () => new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
                {
                    TokenLimit = -1,
                    QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                    QueueLimit = 1,
                    ReplenishmentPeriod = TimeSpan.FromMinutes(2),
                    TokensPerPeriod = 1,
                    AutoReplenishment = false
                }));
            AssertExtensions.Throws<ArgumentException>("options",
                () => new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 1,
                    QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                    QueueLimit = -1,
                    ReplenishmentPeriod = TimeSpan.FromMinutes(2),
                    TokensPerPeriod = 1,
                    AutoReplenishment = false
                }));
            AssertExtensions.Throws<ArgumentException>("options",
                () => new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 1,
                    QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                    QueueLimit = 1,
                    ReplenishmentPeriod = TimeSpan.FromMinutes(2),
                    TokensPerPeriod = -1,
                    AutoReplenishment = false
                }));
            AssertExtensions.Throws<ArgumentException>("options",
                () => new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 1,
                    QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                    QueueLimit = 1,
                    ReplenishmentPeriod = TimeSpan.MinValue,
                    TokensPerPeriod = 1,
                    AutoReplenishment = false
                }));
            AssertExtensions.Throws<ArgumentException>("options",
                () => new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 1,
                    QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                    QueueLimit = 1,
                    ReplenishmentPeriod = TimeSpan.FromMilliseconds(-1),
                    TokensPerPeriod = 1,
                    AutoReplenishment = false
                }));
            AssertExtensions.Throws<ArgumentException>("options",
                () => new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 1,
                    QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                    QueueLimit = 1,
                    ReplenishmentPeriod = TimeSpan.Zero,
                    TokensPerPeriod = 1,
                    AutoReplenishment = false
                }));
        }

        [Fact]
        public override async Task CanAcquireResourceAsync()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 1,
                AutoReplenishment = false
            });

            using var lease = await limiter.AcquireAsync();

            Assert.True(lease.IsAcquired);
            var wait = limiter.AcquireAsync();
            Assert.False(wait.IsCompleted);

            Replenish(limiter, 1L);

            Assert.True((await wait).IsAcquired);
        }

        [Fact]
        public override async Task CanAcquireResourceAsync_QueuesAndGrabsOldest()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 1,
                AutoReplenishment = false
            });
            var lease = await limiter.AcquireAsync();

            Assert.True(lease.IsAcquired);
            var wait1 = limiter.AcquireAsync();
            var wait2 = limiter.AcquireAsync();
            Assert.False(wait1.IsCompleted);
            Assert.False(wait2.IsCompleted);

            lease.Dispose();
            Replenish(limiter, 1L);

            lease = await wait1;
            Assert.True(lease.IsAcquired);
            Assert.False(wait2.IsCompleted);

            lease.Dispose();
            Assert.Equal(0, limiter.GetStatistics().CurrentAvailablePermits);
            Replenish(limiter, 1L);

            lease = await wait2;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task CanAcquireResourceAsync_QueuesAndGrabsNewest()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 3,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 1,
                AutoReplenishment = false
            });

            var lease = await limiter.AcquireAsync(2);
            Assert.True(lease.IsAcquired);

            var wait1 = limiter.AcquireAsync(2);
            var wait2 = limiter.AcquireAsync();
            Assert.False(wait1.IsCompleted);
            Assert.False(wait2.IsCompleted);

            lease.Dispose();
            Replenish(limiter, 1L);

            // second queued item completes first with NewestFirst
            lease = await wait2;
            Assert.True(lease.IsAcquired);
            Assert.False(wait1.IsCompleted);

            lease.Dispose();
            Assert.Equal(0, limiter.GetStatistics().CurrentAvailablePermits);
            Replenish(limiter, 1L);
            Assert.Equal(1, limiter.GetStatistics().CurrentAvailablePermits);
            Replenish(limiter, 1L);

            lease = await wait1;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task FailsWhenQueuingMoreThanLimit_OldestFirst()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 1,
                AutoReplenishment = false
            });
            using var lease = limiter.AttemptAcquire(1);
            var wait = limiter.AcquireAsync(1);

            var failedLease = await limiter.AcquireAsync(1);
            Assert.False(failedLease.IsAcquired);
            Assert.True(failedLease.TryGetMetadata(MetadataName.RetryAfter, out var timeSpan));
            Assert.Equal(TimeSpan.FromMilliseconds(2), timeSpan);
        }

        [Fact]
        public override async Task DropsOldestWhenQueuingMoreThanLimit_NewestFirst()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 1,
                AutoReplenishment = false
            });
            var lease = limiter.AttemptAcquire(1);
            var wait = limiter.AcquireAsync(1);
            Assert.False(wait.IsCompleted);

            var wait2 = limiter.AcquireAsync(1);
            var lease1 = await wait;
            Assert.False(lease1.IsAcquired);
            Assert.False(wait2.IsCompleted);

            Replenish(limiter, 1L);

            lease = await wait2;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task DropsMultipleOldestWhenQueuingMoreThanLimit_NewestFirst()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 2,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 1,
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

            Replenish(limiter, 1L);
            Replenish(limiter, 1L);

            lease = await wait3;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task DropsRequestedLeaseIfPermitCountGreaterThanQueueLimitAndNoAvailability_NewestFirst()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 1,
                AutoReplenishment = false
            });
            var lease = limiter.AttemptAcquire(2);
            Assert.True(lease.IsAcquired);

            // Fill queue
            var wait = limiter.AcquireAsync(1);
            Assert.False(wait.IsCompleted);

            var lease1 = await limiter.AcquireAsync(2);
            Assert.False(lease1.IsAcquired);

            Replenish(limiter, 1L);

            lease = await wait;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task QueueAvailableAfterQueueLimitHitAndResources_BecomeAvailable()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 1,
                AutoReplenishment = false
            });
            var lease = limiter.AttemptAcquire(1);
            var wait = limiter.AcquireAsync(1);

            var failedLease = await limiter.AcquireAsync(1);
            Assert.False(failedLease.IsAcquired);

            Replenish(limiter, 1L);
            lease = await wait;
            Assert.True(lease.IsAcquired);

            wait = limiter.AcquireAsync(1);
            Assert.False(wait.IsCompleted);

            Replenish(limiter, 1L);
            lease = await wait;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task LargeAcquiresAndQueuesDoNotIntegerOverflow()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = int.MaxValue,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = int.MaxValue,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = int.MaxValue,
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

            Replenish(limiter, 1L);
            var lease2 = await wait2;
            Assert.True(lease2.IsAcquired);
        }

        [Fact]
        public override void ThrowsWhenAcquiringMoreThanLimit()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 1,
                AutoReplenishment = false
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => limiter.AttemptAcquire(2));
        }

        [Fact]
        public override async Task ThrowsWhenWaitingForMoreThanLimit()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 1,
                AutoReplenishment = false
            });
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await limiter.AcquireAsync(2));
        }

        [Fact]
        public override void ThrowsWhenAcquiringLessThanZero()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 1,
                AutoReplenishment = false
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => limiter.AttemptAcquire(-1));
        }

        [Fact]
        public override async Task ThrowsWhenWaitingForLessThanZero()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 1,
                AutoReplenishment = false
            });
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await limiter.AcquireAsync(-1));
        }

        [Fact]
        public override void AcquireZero_WithAvailability()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 1,
                AutoReplenishment = false
            });

            using var lease = limiter.AttemptAcquire(0);
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override void AcquireZero_WithoutAvailability()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 1,
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
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 1,
                AutoReplenishment = false
            });

            using var lease = await limiter.AcquireAsync(0);
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task AcquireAsyncZero_WithoutAvailabilityWaitsForAvailability()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 1,
                AutoReplenishment = false
            });
            var lease = await limiter.AcquireAsync(1);
            Assert.True(lease.IsAcquired);

            var wait = limiter.AcquireAsync(0);
            Assert.False(wait.IsCompleted);

            lease.Dispose();
            Replenish(limiter, 1L);
            using var lease2 = await wait;
            Assert.True(lease2.IsAcquired);
        }

        [Fact]
        public override async Task CanDequeueMultipleResourcesAtOnce()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 2,
                AutoReplenishment = false
            });
            using var lease = await limiter.AcquireAsync(2);
            Assert.True(lease.IsAcquired);

            var wait1 = limiter.AcquireAsync(1);
            var wait2 = limiter.AcquireAsync(1);
            Assert.False(wait1.IsCompleted);
            Assert.False(wait2.IsCompleted);

            lease.Dispose();
            Replenish(limiter, 1L);

            var lease1 = await wait1;
            var lease2 = await wait2;
            Assert.True(lease1.IsAcquired);
            Assert.True(lease2.IsAcquired);
        }

        [Fact]
        public override async Task CanCancelAcquireAsyncAfterQueuing()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 1,
                AutoReplenishment = false
            });
            var lease = limiter.AttemptAcquire(1);
            Assert.True(lease.IsAcquired);

            var cts = new CancellationTokenSource();
            var wait = limiter.AcquireAsync(1, cts.Token);

            cts.Cancel();
            var ex = await Assert.ThrowsAsync<TaskCanceledException>(() => wait.AsTask());
            Assert.Equal(cts.Token, ex.CancellationToken);

            lease.Dispose();
            Replenish(limiter, 1L);

            Assert.Equal(1, limiter.GetStatistics().CurrentAvailablePermits);
        }

        [Fact]
        public override async Task CanFillQueueWithOldestFirstAfterCancelingFirstQueuedRequestManually()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 3,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 3,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(3),
                TokensPerPeriod = 3,
                AutoReplenishment = false
            });

            var lease = limiter.AttemptAcquire(3);
            Assert.True(lease.IsAcquired);

            var cts = new CancellationTokenSource();
            var wait = limiter.AcquireAsync(2, cts.Token);
            cts.Cancel();

            var ex = await Assert.ThrowsAsync<TaskCanceledException>(() => wait.AsTask());
            Assert.Equal(cts.Token, ex.CancellationToken);

            var wait2 = limiter.AcquireAsync(1);
            Replenish(limiter, 1L);

            lease = await wait2;
            Assert.True(lease.IsAcquired);

            Assert.Equal(0, limiter.GetStatistics().CurrentAvailablePermits);
            Assert.Equal(0, limiter.GetStatistics().CurrentQueuedCount);
        }

        [Fact]
        public override async Task CanFillQueueWithNewestFirstAfterCancelingQueuedRequestWithAnotherQueuedRequest()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 2,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 2,
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

            var wait3 = limiter.AcquireAsync(2);
            Assert.False(wait3.IsCompleted);

            // will be kicked by wait3 because we're using NewestFirst
            lease = await wait2;
            Assert.False(lease.IsAcquired);

            Replenish(limiter, 1L);
            lease = await wait3;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task CanDisposeAfterCancelingQueuedRequest()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 1,
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

        [Fact]
        public override async Task CanCancelAcquireAsyncBeforeQueuing()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 1,
                AutoReplenishment = false
            });
            var lease = limiter.AttemptAcquire(1);
            Assert.True(lease.IsAcquired);

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var ex = await Assert.ThrowsAsync<TaskCanceledException>(() => limiter.AcquireAsync(1, cts.Token).AsTask());
            Assert.Equal(cts.Token, ex.CancellationToken);

            lease.Dispose();
            Replenish(limiter, 1L);

            Assert.Equal(1, limiter.GetStatistics().CurrentAvailablePermits);
        }

        [Fact]
        public override async Task CancelUpdatesQueueLimit()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 1,
                AutoReplenishment = false
            });
            var lease = limiter.AttemptAcquire(1);
            Assert.True(lease.IsAcquired);

            var cts = new CancellationTokenSource();
            var wait = limiter.AcquireAsync(1, cts.Token);

            cts.Cancel();
            var ex = await Assert.ThrowsAsync<TaskCanceledException>(() => wait.AsTask());
            Assert.Equal(cts.Token, ex.CancellationToken);

            wait = limiter.AcquireAsync(1);
            Assert.False(wait.IsCompleted);

            Replenish(limiter, 1L);
            lease = await wait;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override void NoMetadataOnAcquiredLease()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 1,
                AutoReplenishment = false
            });
            using var lease = limiter.AttemptAcquire(1);
            Assert.False(lease.TryGetMetadata(MetadataName.RetryAfter, out _));
        }

        [Fact]
        public override void MetadataNamesContainsAllMetadata()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 1,
                AutoReplenishment = false
            });
            using var lease = limiter.AttemptAcquire(1);
            Assert.Collection(lease.MetadataNames, metadataName => Assert.Equal(metadataName, MetadataName.RetryAfter.Name));
        }

        [Fact]
        public override async Task DisposeReleasesQueuedAcquires()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 3,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 1,
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
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 3,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 1,
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
        public async Task RetryMetadataOnFailedWaitAsync()
        {
            var options = new TokenBucketRateLimiterOptions
            {
                TokenLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.FromSeconds(20),
                TokensPerPeriod = 1,
                AutoReplenishment = false
            };
            var limiter = new TokenBucketRateLimiter(options);

            using var lease = limiter.AttemptAcquire(2);

            var failedLease = await limiter.AcquireAsync(2);
            Assert.False(failedLease.IsAcquired);
            Assert.True(failedLease.TryGetMetadata(MetadataName.RetryAfter.Name, out var metadata));
            var metaDataTime = Assert.IsType<TimeSpan>(metadata);
            Assert.Equal(options.ReplenishmentPeriod.Ticks * 2, metaDataTime.Ticks);

            Assert.True(failedLease.TryGetMetadata(MetadataName.RetryAfter, out var typedMetadata));
            Assert.Equal(options.ReplenishmentPeriod.Ticks * 2, typedMetadata.Ticks);
            Assert.Collection(failedLease.MetadataNames, item => item.Equals(MetadataName.RetryAfter.Name));
        }

        [Fact]
        public async Task CorrectRetryMetadataWithQueuedItem()
        {
            var options = new TokenBucketRateLimiterOptions
            {
                TokenLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.FromSeconds(20),
                TokensPerPeriod = 1,
                AutoReplenishment = false
            };
            var limiter = new TokenBucketRateLimiter(options);

            using var lease = limiter.AttemptAcquire(2);
            // Queue item which changes the retry after time for failed items
            var wait = limiter.AcquireAsync(1);
            Assert.False(wait.IsCompleted);

            var failedLease = await limiter.AcquireAsync(2);
            Assert.False(failedLease.IsAcquired);
            Assert.True(failedLease.TryGetMetadata(MetadataName.RetryAfter, out var typedMetadata));
            Assert.Equal(options.ReplenishmentPeriod.Ticks * 3, typedMetadata.Ticks);
        }

        [Fact]
        public async Task CorrectRetryMetadataWithMultipleTokensPerPeriod()
        {
            var options = new TokenBucketRateLimiterOptions
            {
                TokenLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.FromSeconds(20),
                TokensPerPeriod = 2,
                AutoReplenishment = false
            };
            var limiter = new TokenBucketRateLimiter(options);

            using var lease = limiter.AttemptAcquire(2);
            // Queue item which changes the retry after time for failed waits
            var wait = limiter.AcquireAsync(1);
            Assert.False(wait.IsCompleted);

            var failedLease = await limiter.AcquireAsync(2);
            Assert.False(failedLease.IsAcquired);
            Assert.True(failedLease.TryGetMetadata(MetadataName.RetryAfter, out var typedMetadata));
            Assert.Equal(options.ReplenishmentPeriod, typedMetadata);
        }

        [Fact]
        public async Task CorrectRetryMetadataWithLargeTokensPerPeriod()
        {
            var options = new TokenBucketRateLimiterOptions
            {
                TokenLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.FromSeconds(20),
                TokensPerPeriod = 100,
                AutoReplenishment = false
            };
            var limiter = new TokenBucketRateLimiter(options);

            using var lease = limiter.AttemptAcquire(2);
            // Queue item which changes the retry after time for failed items
            var wait = limiter.AcquireAsync(1);
            Assert.False(wait.IsCompleted);

            var failedLease = await limiter.AcquireAsync(2);
            Assert.False(failedLease.IsAcquired);
            Assert.True(failedLease.TryGetMetadata(MetadataName.RetryAfter, out var typedMetadata));
            Assert.Equal(options.ReplenishmentPeriod, typedMetadata);
        }

        [Fact]
        public async Task CorrectRetryMetadataWithNonZeroAvailableItems()
        {
            var options = new TokenBucketRateLimiterOptions
            {
                TokenLimit = 3,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.FromSeconds(20),
                TokensPerPeriod = 1,
                AutoReplenishment = false
            };
            var limiter = new TokenBucketRateLimiter(options);

            using var lease = limiter.AttemptAcquire(2);

            var failedLease = await limiter.AcquireAsync(3);
            Assert.False(failedLease.IsAcquired);
            Assert.True(failedLease.TryGetMetadata(MetadataName.RetryAfter, out var typedMetadata));
            Assert.Equal(options.ReplenishmentPeriod.Ticks * 2, typedMetadata.Ticks);
        }

        [Fact]
        public void ReplenishHonorsTokensPerPeriod()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 7,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 3,
                AutoReplenishment = false
            });
            Assert.True(limiter.AttemptAcquire(5).IsAcquired);
            Assert.False(limiter.AttemptAcquire(3).IsAcquired);

            Assert.Equal(2, limiter.GetStatistics().CurrentAvailablePermits);
            Replenish(limiter, 1L);
            Assert.Equal(5, limiter.GetStatistics().CurrentAvailablePermits);

            Replenish(limiter, 1L);
            Assert.Equal(7, limiter.GetStatistics().CurrentAvailablePermits);
        }

        [Fact]
        public async void TryReplenishWithAllTokensAvailable_Noops()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(30),
                TokensPerPeriod = 1,
                AutoReplenishment = false
            });
            Assert.Equal(2, limiter.GetStatistics().CurrentAvailablePermits);
            await Task.Delay(100);
            limiter.TryReplenish();
            Assert.Equal(2, limiter.GetStatistics().CurrentAvailablePermits);
        }

        [Fact]
        public void TryReplenishWithAutoReplenish_ReturnsFalse()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                TokensPerPeriod = 1,
                AutoReplenishment = true
            });
            Assert.Equal(2, limiter.GetStatistics().CurrentAvailablePermits);
            Assert.False(limiter.TryReplenish());
            Assert.Equal(2, limiter.GetStatistics().CurrentAvailablePermits);
        }

        [Fact]
        public async Task AutoReplenish_ReplenishesTokens()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1000),
                TokensPerPeriod = 1,
                AutoReplenishment = true
            });
            Assert.Equal(2, limiter.GetStatistics().CurrentAvailablePermits);
            limiter.AttemptAcquire(2);

            var lease = await limiter.AcquireAsync(1);
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task CanAcquireResourcesWithAcquireAsyncWithQueuedItemsIfNewestFirst()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 2,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 2,
                AutoReplenishment = false
            });

            var lease = limiter.AttemptAcquire(1);
            Assert.True(lease.IsAcquired);

            var wait = limiter.AcquireAsync(2);
            Assert.False(wait.IsCompleted);

            Assert.Equal(1, limiter.GetStatistics().CurrentAvailablePermits);
            lease = await limiter.AcquireAsync(1);
            Assert.True(lease.IsAcquired);
            Assert.False(wait.IsCompleted);

            Replenish(limiter, 1L);

            lease = await wait;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task CannotAcquireResourcesWithAcquireAsyncWithQueuedItemsIfOldestFirst()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 3,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 2,
                AutoReplenishment = false
            });

            var lease = limiter.AttemptAcquire(1);
            Assert.True(lease.IsAcquired);

            var wait = limiter.AcquireAsync(2);
            var wait2 = limiter.AcquireAsync(1);
            Assert.False(wait.IsCompleted);
            Assert.False(wait2.IsCompleted);

            Replenish(limiter, 1L);

            lease = await wait;
            Assert.True(lease.IsAcquired);
            Assert.False(wait2.IsCompleted);

            Replenish(limiter, 1L);

            lease = await wait2;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task CanAcquireResourcesWithAcquireWithQueuedItemsIfNewestFirst()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 3,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 2,
                AutoReplenishment = false
            });

            var lease = limiter.AttemptAcquire(1);
            Assert.True(lease.IsAcquired);

            var wait = limiter.AcquireAsync(2);
            Assert.False(wait.IsCompleted);

            lease = limiter.AttemptAcquire(1);
            Assert.True(lease.IsAcquired);
            Assert.False(wait.IsCompleted);

            Replenish(limiter, 1L);

            lease = await wait;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task CannotAcquireResourcesWithAcquireWithQueuedItemsIfOldestFirst()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 3,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 2,
                AutoReplenishment = false
            });

            var lease = limiter.AttemptAcquire(1);
            Assert.True(lease.IsAcquired);

            var wait = limiter.AcquireAsync(2);
            Assert.False(wait.IsCompleted);

            lease = limiter.AttemptAcquire(1);
            Assert.False(lease.IsAcquired);

            Replenish(limiter, 1L);

            lease = await wait;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public async Task ReplenishWorksWithTicksOverInt32Max()
        {
            using var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 10,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(2),
                TokensPerPeriod = 1,
                AutoReplenishment = false
            });

            // Ensure next tick is over uint.MaxValue
            Replenish(limiter, uint.MaxValue);

            var lease = limiter.AttemptAcquire(10);
            Assert.True(lease.IsAcquired);

            var wait = limiter.AcquireAsync(1);
            Assert.False(wait.IsCompleted);

            Replenish(limiter, 2L);

            lease = await wait;
            Assert.True(lease.IsAcquired);

            wait = limiter.AcquireAsync(1);
            Assert.False(wait.IsCompleted);

            // Tick 1 millisecond too soon and verify that the queued item wasn't completed
            Replenish(limiter, 1L);
            Assert.False(wait.IsCompleted);

            // ticks would wrap if using uint
            Replenish(limiter, 2L);
            lease = await wait;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override void NullIdleDurationWhenActive()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(2),
                TokensPerPeriod = 1,
                AutoReplenishment = false
            });
            limiter.AttemptAcquire(1);
            Assert.Null(limiter.IdleDuration);
        }

        [Fact]
        public override async Task IdleDurationUpdatesWhenIdle()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(2),
                TokensPerPeriod = 1,
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
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 1,
                AutoReplenishment = false
            });
            limiter.AttemptAcquire(1);
            Replenish(limiter, 1L);
            Assert.NotNull(limiter.IdleDuration);
        }

        [Fact]
        public void ReplenishingRateLimiterPropertiesHaveCorrectValues()
        {
            var replenishPeriod = TimeSpan.FromMinutes(1);
            using ReplenishingRateLimiter limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2,
                ReplenishmentPeriod = replenishPeriod,
                TokensPerPeriod = 1,
                AutoReplenishment = true
            });
            Assert.True(limiter.IsAutoReplenishing);
            Assert.Equal(replenishPeriod, limiter.ReplenishmentPeriod);

            replenishPeriod = TimeSpan.FromSeconds(2);
            using ReplenishingRateLimiter limiter2 = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2,
                ReplenishmentPeriod = replenishPeriod,
                TokensPerPeriod = 1,
                AutoReplenishment = false
            });
            Assert.False(limiter2.IsAutoReplenishing);
            Assert.Equal(replenishPeriod, limiter2.ReplenishmentPeriod);
        }

        [Fact]
        public override void GetStatisticsReturnsNewInstances()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 2,
                AutoReplenishment = false
            });

            var stats = limiter.GetStatistics();
            Assert.Equal(1, stats.CurrentAvailablePermits);

            var lease = limiter.AttemptAcquire(1);

            var stats2 = limiter.GetStatistics();
            Assert.NotSame(stats, stats2);
            Assert.Equal(1, stats.CurrentAvailablePermits);
            Assert.Equal(0, stats2.CurrentAvailablePermits);
        }

        [Fact]
        public override async Task GetStatisticsHasCorrectValues()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 100,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 50,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 30,
                AutoReplenishment = false
            });

            var stats = limiter.GetStatistics();
            Assert.Equal(100, stats.CurrentAvailablePermits);
            Assert.Equal(0, stats.CurrentQueuedCount);
            Assert.Equal(0, stats.TotalFailedLeases);
            Assert.Equal(0, stats.TotalSuccessfulLeases);

            var lease1 = limiter.AttemptAcquire(60);
            stats = limiter.GetStatistics();
            Assert.Equal(40, stats.CurrentAvailablePermits);
            Assert.Equal(0, stats.CurrentQueuedCount);
            Assert.Equal(0, stats.TotalFailedLeases);
            Assert.Equal(1, stats.TotalSuccessfulLeases);

            var lease2Task = limiter.AcquireAsync(50);
            stats = limiter.GetStatistics();
            Assert.Equal(40, stats.CurrentAvailablePermits);
            Assert.Equal(50, stats.CurrentQueuedCount);
            Assert.Equal(0, stats.TotalFailedLeases);
            Assert.Equal(1, stats.TotalSuccessfulLeases);

            var lease3 = await limiter.AcquireAsync(1);
            Assert.False(lease3.IsAcquired);
            stats = limiter.GetStatistics();
            Assert.Equal(40, stats.CurrentAvailablePermits);
            Assert.Equal(50, stats.CurrentQueuedCount);
            Assert.Equal(1, stats.TotalFailedLeases);
            Assert.Equal(1, stats.TotalSuccessfulLeases);

            var lease4 = limiter.AttemptAcquire(100);
            Assert.False(lease4.IsAcquired);
            stats = limiter.GetStatistics();
            Assert.Equal(40, stats.CurrentAvailablePermits);
            Assert.Equal(50, stats.CurrentQueuedCount);
            Assert.Equal(2, stats.TotalFailedLeases);
            Assert.Equal(1, stats.TotalSuccessfulLeases);

            Replenish(limiter, 1);
            await lease2Task;

            stats = limiter.GetStatistics();
            Assert.Equal(20, stats.CurrentAvailablePermits);
            Assert.Equal(0, stats.CurrentQueuedCount);
            Assert.Equal(2, stats.TotalFailedLeases);
            Assert.Equal(2, stats.TotalSuccessfulLeases);
        }

        [Fact]
        public override async Task GetStatisticsWithZeroPermitCount()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 100,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 50,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 30,
                AutoReplenishment = false
            });
            var lease = limiter.AttemptAcquire(0);
            Assert.True(lease.IsAcquired);
            Assert.Equal(1, limiter.GetStatistics().TotalSuccessfulLeases);
            Assert.Equal(100, limiter.GetStatistics().CurrentAvailablePermits);

            lease = await limiter.AcquireAsync(0);
            Assert.True(lease.IsAcquired);
            Assert.Equal(2, limiter.GetStatistics().TotalSuccessfulLeases);
            Assert.Equal(100, limiter.GetStatistics().CurrentAvailablePermits);

            lease = limiter.AttemptAcquire(100);
            Assert.True(lease.IsAcquired);
            Assert.Equal(3, limiter.GetStatistics().TotalSuccessfulLeases);
            Assert.Equal(0, limiter.GetStatistics().CurrentAvailablePermits);

            var lease2 = limiter.AttemptAcquire(0);
            Assert.False(lease2.IsAcquired);
            Assert.Equal(3, limiter.GetStatistics().TotalSuccessfulLeases);
            Assert.Equal(1, limiter.GetStatistics().TotalFailedLeases);
            Assert.Equal(0, limiter.GetStatistics().CurrentAvailablePermits);
        }

        [Fact]
        public override void GetStatisticsThrowsAfterDispose()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 100,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 50,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
                TokensPerPeriod = 30,
                AutoReplenishment = false
            });
            limiter.Dispose();
            Assert.Throws<ObjectDisposedException>(limiter.GetStatistics);
        }

        [Fact]
        public void AutoReplenishIgnoresTimerJitter()
        {
            var replenishmentPeriod = TimeSpan.FromMinutes(10);
            using var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 10,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = replenishmentPeriod,
                AutoReplenishment = true,
                TokensPerPeriod = 1,
            });

            var lease = limiter.AttemptAcquire(permitCount: 3);
            Assert.True(lease.IsAcquired);

            Assert.Equal(7, limiter.GetStatistics().CurrentAvailablePermits);

            // Replenish 1 millisecond less than ReplenishmentPeriod while AutoReplenishment is enabled
            Replenish(limiter, (long)replenishmentPeriod.TotalMilliseconds - 1);

            Assert.Equal(8, limiter.GetStatistics().CurrentAvailablePermits);

            // Replenish 1 millisecond longer than ReplenishmentPeriod while AutoReplenishment is enabled
            Replenish(limiter, (long)replenishmentPeriod.TotalMilliseconds + 1);

            Assert.Equal(9, limiter.GetStatistics().CurrentAvailablePermits);
        }

        [Fact]
        public void ManualReplenishPreservesTimeWithTimerJitter()
        {
            var replenishmentPeriod = TimeSpan.FromMinutes(10);
            using var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 10,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = replenishmentPeriod,
                AutoReplenishment = false,
                TokensPerPeriod = 1,
            });

            var lease = limiter.AttemptAcquire(permitCount: 3);
            Assert.True(lease.IsAcquired);

            Assert.Equal(7, limiter.GetStatistics().CurrentAvailablePermits);

            // Replenish 1 millisecond less than ReplenishmentPeriod while AutoReplenishment is enabled
            Replenish(limiter, (long)replenishmentPeriod.TotalMilliseconds - 1);

            Assert.Equal(7, limiter.GetStatistics().CurrentAvailablePermits);

            // Replenish 1 millisecond longer than ReplenishmentPeriod while AutoReplenishment is enabled
            Replenish(limiter, (long)replenishmentPeriod.TotalMilliseconds + 1);

            Assert.Equal(9, limiter.GetStatistics().CurrentAvailablePermits);
        }

        private static readonly double TickFrequency = (double)TimeSpan.TicksPerSecond / Stopwatch.Frequency;

        static internal void Replenish(TokenBucketRateLimiter limiter, long addMilliseconds)
        {
            var replenishInternalMethod = typeof(TokenBucketRateLimiter).GetMethod("ReplenishInternal", Reflection.BindingFlags.NonPublic | Reflection.BindingFlags.Instance)!;
            var internalTick = typeof(TokenBucketRateLimiter).GetField("_lastReplenishmentTick", Reflection.BindingFlags.NonPublic | Reflection.BindingFlags.Instance)!;
            var currentTick = (long)internalTick.GetValue(limiter);
            replenishInternalMethod.Invoke(limiter, new object[] { currentTick + addMilliseconds * (long)(TimeSpan.TicksPerMillisecond / TickFrequency) });
        }
    }
}
