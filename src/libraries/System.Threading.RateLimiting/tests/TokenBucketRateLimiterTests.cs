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
                ReplenishmentPeriod = TimeSpan.Zero,
                TokensPerPeriod = 1,
                AutoReplenishment = false
            });
            var lease = limiter.AttemptAcquire();

            Assert.True(lease.IsAcquired);
            Assert.False(limiter.AttemptAcquire().IsAcquired);

            lease.Dispose();
            Assert.False(limiter.AttemptAcquire().IsAcquired);
            Assert.True(limiter.TryReplenish());

            Assert.True(limiter.AttemptAcquire().IsAcquired);
        }

        [Fact]
        public override void InvalidOptionsThrows()
        {
            Assert.Throws<ArgumentException>(
                () => new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = -1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.FromMinutes(2),
                TokensPerPeriod = 1,
                AutoReplenishment = false
            }));
            Assert.Throws<ArgumentException>(
                () => new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = -1,
                ReplenishmentPeriod = TimeSpan.FromMinutes(2),
                TokensPerPeriod = 1,
                AutoReplenishment = false
            }));
            Assert.Throws<ArgumentException>(
                () => new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.FromMinutes(2),
                TokensPerPeriod = -1,
                AutoReplenishment = false
            }));
            Assert.Throws<ArgumentException>(
                () => new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.MinValue,
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
                ReplenishmentPeriod = TimeSpan.Zero,
                TokensPerPeriod = 1,
                AutoReplenishment = false
            });

            using var lease = await limiter.AcquireAsync();

            Assert.True(lease.IsAcquired);
            var wait = limiter.AcquireAsync();
            Assert.False(wait.IsCompleted);

            Assert.True(limiter.TryReplenish());

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
                ReplenishmentPeriod = TimeSpan.Zero,
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
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 3,
                ReplenishmentPeriod = TimeSpan.FromMinutes(0),
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
            Assert.True(limiter.TryReplenish());

            // second queued item completes first with NewestFirst
            lease = await wait2;
            Assert.True(lease.IsAcquired);
            Assert.False(wait1.IsCompleted);

            lease.Dispose();
            Assert.Equal(0, limiter.GetAvailablePermits());
            Assert.True(limiter.TryReplenish());
            Assert.True(limiter.TryReplenish());

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
                ReplenishmentPeriod = TimeSpan.Zero,
                TokensPerPeriod = 1,
                AutoReplenishment = false
            });
            using var lease = limiter.AttemptAcquire(1);
            var wait = limiter.AcquireAsync(1);

            var failedLease = await limiter.AcquireAsync(1);
            Assert.False(failedLease.IsAcquired);
            Assert.True(failedLease.TryGetMetadata(MetadataName.RetryAfter, out var timeSpan));
            Assert.Equal(TimeSpan.Zero, timeSpan);
        }

        [Fact]
        public override async Task DropsOldestWhenQueuingMoreThanLimit_NewestFirst()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.Zero,
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

            limiter.TryReplenish();

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
                ReplenishmentPeriod = TimeSpan.Zero,
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

            limiter.TryReplenish();
            limiter.TryReplenish();

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
                ReplenishmentPeriod = TimeSpan.Zero,
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

            limiter.TryReplenish();

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
                ReplenishmentPeriod = TimeSpan.Zero,
                TokensPerPeriod = 1,
                AutoReplenishment = false
            });
            var lease = limiter.AttemptAcquire(1);
            var wait = limiter.AcquireAsync(1);

            var failedLease = await limiter.AcquireAsync(1);
            Assert.False(failedLease.IsAcquired);

            limiter.TryReplenish();
            lease = await wait;
            Assert.True(lease.IsAcquired);

            wait = limiter.AcquireAsync(1);
            Assert.False(wait.IsCompleted);

            limiter.TryReplenish();
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
                ReplenishmentPeriod = TimeSpan.Zero,
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

            limiter.TryReplenish();
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
                ReplenishmentPeriod = TimeSpan.Zero,
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
                ReplenishmentPeriod = TimeSpan.Zero,
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
                ReplenishmentPeriod = TimeSpan.Zero,
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
                ReplenishmentPeriod = TimeSpan.Zero,
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
                ReplenishmentPeriod = TimeSpan.Zero,
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
                ReplenishmentPeriod = TimeSpan.Zero,
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
                ReplenishmentPeriod = TimeSpan.Zero,
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
                ReplenishmentPeriod = TimeSpan.Zero,
                TokensPerPeriod = 1,
                AutoReplenishment = false
            });
            var lease = await limiter.AcquireAsync(1);
            Assert.True(lease.IsAcquired);

            var wait = limiter.AcquireAsync(0);
            Assert.False(wait.IsCompleted);

            lease.Dispose();
            Assert.True(limiter.TryReplenish());
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
                ReplenishmentPeriod = TimeSpan.Zero,
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
            Assert.True(limiter.TryReplenish());

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
                ReplenishmentPeriod = TimeSpan.Zero,
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
            Assert.True(limiter.TryReplenish());

            Assert.Equal(1, limiter.GetAvailablePermits());
        }

        [Fact]
        public override async Task CanFillQueueWithNewestFirstAfterCancelingQueuedRequestWithAnotherQueuedRequest()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 2,
                ReplenishmentPeriod = TimeSpan.Zero,
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

            limiter.TryReplenish();
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
                ReplenishmentPeriod = TimeSpan.Zero,
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
                ReplenishmentPeriod = TimeSpan.Zero,
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
            Assert.True(limiter.TryReplenish());

            Assert.Equal(1, limiter.GetAvailablePermits());
        }

        [Fact]
        public override async Task CancelUpdatesQueueLimit()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.Zero,
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

            limiter.TryReplenish();
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
                ReplenishmentPeriod = TimeSpan.Zero,
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
                ReplenishmentPeriod = TimeSpan.Zero,
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
                ReplenishmentPeriod = TimeSpan.Zero,
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
                ReplenishmentPeriod = TimeSpan.Zero,
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
        public void TryReplenishHonorsTokensPerPeriod()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 7,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.Zero,
                TokensPerPeriod = 3,
                AutoReplenishment = false
            });
            Assert.True(limiter.AttemptAcquire(5).IsAcquired);
            Assert.False(limiter.AttemptAcquire(3).IsAcquired);

            Assert.Equal(2, limiter.GetAvailablePermits());
            Assert.True(limiter.TryReplenish());
            Assert.Equal(5, limiter.GetAvailablePermits());

            Assert.True(limiter.TryReplenish());
            Assert.Equal(7, limiter.GetAvailablePermits());
        }

        [Fact]
        public void TryReplenishWithAllTokensAvailable_Noops()
        {
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1,
                ReplenishmentPeriod = TimeSpan.Zero,
                TokensPerPeriod = 1,
                AutoReplenishment = false
            });
            Assert.Equal(2, limiter.GetAvailablePermits());
            Assert.True(limiter.TryReplenish());
            Assert.Equal(2, limiter.GetAvailablePermits());
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
            Assert.Equal(2, limiter.GetAvailablePermits());
            Assert.False(limiter.TryReplenish());
            Assert.Equal(2, limiter.GetAvailablePermits());
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
            Assert.Equal(2, limiter.GetAvailablePermits());
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
                ReplenishmentPeriod = TimeSpan.Zero,
                TokensPerPeriod = 2,
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
                ReplenishmentPeriod = TimeSpan.Zero,
                TokensPerPeriod = 2,
                AutoReplenishment = false
            });

            var lease = limiter.AttemptAcquire(1);
            Assert.True(lease.IsAcquired);

            var wait = limiter.AcquireAsync(2);
            var wait2 = limiter.AcquireAsync(1);
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
            var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 3,
                ReplenishmentPeriod = TimeSpan.Zero,
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

            limiter.TryReplenish();

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
                ReplenishmentPeriod = TimeSpan.Zero,
                TokensPerPeriod = 2,
                AutoReplenishment = false
            });

            var lease = limiter.AttemptAcquire(1);
            Assert.True(lease.IsAcquired);

            var wait = limiter.AcquireAsync(2);
            Assert.False(wait.IsCompleted);

            lease = limiter.AttemptAcquire(1);
            Assert.False(lease.IsAcquired);

            limiter.TryReplenish();

            lease = await wait;
            Assert.True(lease.IsAcquired);
        }

        private static readonly double TickFrequency = (double)TimeSpan.TicksPerSecond / Stopwatch.Frequency;

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

            var lease = limiter.AttemptAcquire(10);
            Assert.True(lease.IsAcquired);

            var wait = limiter.AcquireAsync(1);
            Assert.False(wait.IsCompleted);

            var replenishInternalMethod = typeof(TokenBucketRateLimiter).GetMethod("ReplenishInternal", Reflection.BindingFlags.NonPublic | Reflection.BindingFlags.Instance)!;
            // Ensure next tick is over uint.MaxValue
            var tick = Stopwatch.GetTimestamp() + uint.MaxValue;
            replenishInternalMethod.Invoke(limiter, new object[] { tick });

            lease = await wait;
            Assert.True(lease.IsAcquired);

            wait = limiter.AcquireAsync(1);
            Assert.False(wait.IsCompleted);

            // Tick 1 millisecond too soon and verify that the queued item wasn't completed
            replenishInternalMethod.Invoke(limiter, new object[] { tick + 1L * (long)(TimeSpan.TicksPerMillisecond / TickFrequency) });
            Assert.False(wait.IsCompleted);

            // ticks would wrap if using uint
            replenishInternalMethod.Invoke(limiter, new object[] { tick + 2L * (long)(TimeSpan.TicksPerMillisecond / TickFrequency) });
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
                ReplenishmentPeriod = TimeSpan.Zero,
                TokensPerPeriod = 1,
                AutoReplenishment = false
            });
            limiter.AttemptAcquire(1);
            limiter.TryReplenish();
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
    }
}
