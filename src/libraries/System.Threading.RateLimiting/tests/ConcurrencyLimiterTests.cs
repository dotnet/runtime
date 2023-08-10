// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.RateLimiting.Test
{
    public class ConcurrencyLimiterTests : BaseRateLimiterTests
    {
        [Fact]
        public override void InvalidOptionsThrows()
        {
            AssertExtensions.Throws<ArgumentException>("options", () => new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = -1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1
            }));
            AssertExtensions.Throws<ArgumentException>("options", () => new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = -1
            }));
        }

        [Fact]
        public override void CanAcquireResource()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1
            });
            var lease = limiter.AttemptAcquire();

            Assert.True(lease.IsAcquired);
            Assert.False(limiter.AttemptAcquire().IsAcquired);

            lease.Dispose();

            Assert.True(limiter.AttemptAcquire().IsAcquired);
        }

        [Fact]
        public override async Task CanAcquireResourceAsync()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1
            });
            var lease = await limiter.AcquireAsync();

            Assert.True(lease.IsAcquired);
            var wait = limiter.AcquireAsync();
            Assert.False(wait.IsCompleted);

            lease.Dispose();

            Assert.True((await wait).IsAcquired);
        }

        [Fact]
        public override async Task CanAcquireResourceAsync_QueuesAndGrabsOldest()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2
            });
            var lease = await limiter.AcquireAsync();

            Assert.True(lease.IsAcquired);
            var wait1 = limiter.AcquireAsync();
            var wait2 = limiter.AcquireAsync();
            Assert.False(wait1.IsCompleted);
            Assert.False(wait2.IsCompleted);

            lease.Dispose();

            lease = await wait1;
            Assert.True(lease.IsAcquired);
            Assert.False(wait2.IsCompleted);

            lease.Dispose();

            lease = await wait2;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task CanAcquireResourceAsync_QueuesAndGrabsNewest()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 3
            });
            var lease = await limiter.AcquireAsync(2);

            Assert.True(lease.IsAcquired);
            var wait1 = limiter.AcquireAsync(2);
            var wait2 = limiter.AcquireAsync();
            Assert.False(wait1.IsCompleted);
            Assert.False(wait2.IsCompleted);

            lease.Dispose();

            // second queued item completes first with NewestFirst
            lease = await wait2;
            Assert.True(lease.IsAcquired);
            Assert.False(wait1.IsCompleted);

            lease.Dispose();

            lease = await wait1;
            Assert.True(lease.IsAcquired);
        }

#if DEBUG
        [Fact]
        public Task DoesNotDeadlockCleaningUpCanceledRequestedLease_Pre() =>
            DoesNotDeadlockCleaningUpCanceledRequestedLease((limiter, hook) => SetReleasePreHook(limiter, hook));

        [Fact]
        public Task DoesNotDeadlockCleaningUpCanceledRequestedLease_Post() =>
            DoesNotDeadlockCleaningUpCanceledRequestedLease((limiter, hook) => SetReleasePostHook(limiter, hook));

        private void SetReleasePreHook(ConcurrencyLimiter limiter, Action hook)
        {
            typeof(ConcurrencyLimiter).GetEvent("ReleasePreHook", BindingFlags.NonPublic | BindingFlags.Instance).AddMethod.Invoke(limiter, new object[] { hook });
        }

        private void SetReleasePostHook(ConcurrencyLimiter limiter, Action hook)
        {
            typeof(ConcurrencyLimiter).GetEvent("ReleasePostHook", BindingFlags.NonPublic | BindingFlags.Instance).AddMethod.Invoke(limiter, new object[] { hook });
        }

        private async Task DoesNotDeadlockCleaningUpCanceledRequestedLease(Action<ConcurrencyLimiter, Action> attachHook)
        {
            using var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1
            });
            var lease = limiter.AttemptAcquire(1);
            Assert.True(lease.IsAcquired);

            var cts = new CancellationTokenSource();
            _ = limiter.AcquireAsync(1, cts.Token);
            attachHook(limiter, () =>
            {
                Task.Run(cts.Cancel);
                Thread.Sleep(1);
            });

            var task1 = Task.Delay(1000);
            var task2 = Task.Run(lease.Dispose);
            Assert.Same(task2, await Task.WhenAny(task1, task2));
            await task2;
        }
#endif

        [Fact]
        public override async Task FailsWhenQueuingMoreThanLimit_OldestFirst()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1
            });
            using var lease = limiter.AttemptAcquire(1);
            var wait = limiter.AcquireAsync(1);

            var failedLease = await limiter.AcquireAsync(1);
            Assert.False(failedLease.IsAcquired);
        }

        [Fact]
        public override async Task DropsOldestWhenQueuingMoreThanLimit_NewestFirst()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1
            });
            var lease = limiter.AttemptAcquire(1);
            var wait = limiter.AcquireAsync(1);
            Assert.False(wait.IsCompleted);

            var wait2 = limiter.AcquireAsync(1);
            var lease1 = await wait;
            Assert.False(lease1.IsAcquired);
            Assert.False(wait2.IsCompleted);

            lease.Dispose();

            lease = await wait2;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task DropsMultipleOldestWhenQueuingMoreThanLimit_NewestFirst()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 2
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

            lease.Dispose();

            lease = await wait3;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task DropsRequestedLeaseIfPermitCountGreaterThanQueueLimitAndNoAvailability_NewestFirst()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1
            });
            var lease = limiter.AttemptAcquire(2);
            Assert.True(lease.IsAcquired);

            // Fill queue
            var wait = limiter.AcquireAsync(1);
            Assert.False(wait.IsCompleted);

            var lease1 = await limiter.AcquireAsync(2);
            Assert.False(lease1.IsAcquired);

            lease.Dispose();
            var lease2 = await wait;
            Assert.True(lease2.IsAcquired);
        }

        [Fact]
        public override async Task LargeAcquiresAndQueuesDoNotIntegerOverflow()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = int.MaxValue,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = int.MaxValue
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

            lease.Dispose();
            var lease2 = await wait2;
            Assert.True(lease2.IsAcquired);
        }

        [Fact]
        public override async Task QueueAvailableAfterQueueLimitHitAndResources_BecomeAvailable()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1
            });
            var lease = limiter.AttemptAcquire(1);
            var wait = limiter.AcquireAsync(1);

            var failedLease = await limiter.AcquireAsync(1);
            Assert.False(failedLease.IsAcquired);

            lease.Dispose();
            lease = await wait;
            Assert.True(lease.IsAcquired);

            wait = limiter.AcquireAsync(1);
            Assert.False(wait.IsCompleted);

            lease.Dispose();
            lease = await wait;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override void ThrowsWhenAcquiringMoreThanLimit()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1
            });
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => limiter.AttemptAcquire(2));
            Assert.Equal("permitCount", ex.ParamName);
        }

        [Fact]
        public override async Task ThrowsWhenWaitingForMoreThanLimit()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1
            });
            var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await limiter.AcquireAsync(2));
            Assert.Equal("permitCount", ex.ParamName);
        }

        [Fact]
        public override void ThrowsWhenAcquiringLessThanZero()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => limiter.AttemptAcquire(-1));
        }

        [Fact]
        public override async Task ThrowsWhenWaitingForLessThanZero()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1
            });
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await limiter.AcquireAsync(-1));
        }

        [Fact]
        public override void AcquireZero_WithAvailability()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1
            });

            using var lease = limiter.AttemptAcquire(0);
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override void AcquireZero_WithoutAvailability()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1
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
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1
            });

            using var lease = await limiter.AcquireAsync(0);
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task AcquireAsyncZero_WithoutAvailabilityWaitsForAvailability()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 1
            });
            var lease = await limiter.AcquireAsync(1);
            Assert.True(lease.IsAcquired);

            var wait = limiter.AcquireAsync(0);
            Assert.False(wait.IsCompleted);

            lease.Dispose();
            using var lease2 = await wait;
            Assert.True(lease2.IsAcquired);
        }

        [Fact]
        public override async Task CanDequeueMultipleResourcesAtOnce()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2
            });
            using var lease = await limiter.AcquireAsync(2);
            Assert.True(lease.IsAcquired);

            var wait1 = limiter.AcquireAsync(1);
            var wait2 = limiter.AcquireAsync(1);
            Assert.False(wait1.IsCompleted);
            Assert.False(wait2.IsCompleted);

            lease.Dispose();

            var lease1 = await wait1;
            var lease2 = await wait2;
            Assert.True(lease1.IsAcquired);
            Assert.True(lease2.IsAcquired);
        }

        [Fact]
        public override async Task CanAcquireResourcesWithAcquireAsyncWithQueuedItemsIfNewestFirst()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 3
            });
            using var lease = await limiter.AcquireAsync(1);
            Assert.True(lease.IsAcquired);

            var wait1 = limiter.AcquireAsync(2);
            Assert.False(wait1.IsCompleted);
            var wait2 = limiter.AcquireAsync(1);
            var lease2 = await wait2;
            Assert.True(lease2.IsAcquired);

            lease.Dispose();

            Assert.False(wait1.IsCompleted);
            lease2.Dispose();

            var lease1 = await wait1;
            Assert.True(lease1.IsAcquired);
        }

        [Fact]
        public override async Task CannotAcquireResourcesWithAcquireAsyncWithQueuedItemsIfOldestFirst()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 3
            });
            using var lease = await limiter.AcquireAsync(1);
            Assert.True(lease.IsAcquired);

            var wait1 = limiter.AcquireAsync(2);
            var wait2 = limiter.AcquireAsync(1);
            Assert.False(wait1.IsCompleted);
            Assert.False(wait2.IsCompleted);

            lease.Dispose();

            var lease1 = await wait1;
            Assert.True(lease1.IsAcquired);
            Assert.False(wait2.IsCompleted);

            lease1.Dispose();
            var lease2 = await wait2;
            Assert.True(lease2.IsAcquired);
        }

        [Fact]
        public override async Task CanAcquireResourcesWithAcquireWithQueuedItemsIfNewestFirst()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 3
            });
            using var lease = await limiter.AcquireAsync(1);
            Assert.True(lease.IsAcquired);

            var wait1 = limiter.AcquireAsync(2);
            Assert.False(wait1.IsCompleted);
            var lease2 = limiter.AttemptAcquire(1);
            Assert.True(lease2.IsAcquired);

            lease.Dispose();

            Assert.False(wait1.IsCompleted);
            lease2.Dispose();

            var lease1 = await wait1;
            Assert.True(lease1.IsAcquired);
        }

        [Fact]
        public override async Task CannotAcquireResourcesWithAcquireWithQueuedItemsIfOldestFirst()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 3
            });
            using var lease = await limiter.AcquireAsync(1);
            Assert.True(lease.IsAcquired);

            var wait1 = limiter.AcquireAsync(2);
            Assert.False(wait1.IsCompleted);
            var lease2 = limiter.AttemptAcquire(1);
            Assert.False(lease2.IsAcquired);

            lease.Dispose();

            var lease1 = await wait1;
            Assert.True(lease1.IsAcquired);
        }

        [Fact]
        public override async Task CanCancelAcquireAsyncAfterQueuing()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1
            });
            var lease = limiter.AttemptAcquire(1);
            Assert.True(lease.IsAcquired);

            var cts = new CancellationTokenSource();
            var wait = limiter.AcquireAsync(1, cts.Token);

            cts.Cancel();
            var ex = await Assert.ThrowsAsync<TaskCanceledException>(() => wait.AsTask());
            Assert.Equal(cts.Token, ex.CancellationToken);

            lease.Dispose();

            Assert.Equal(1, limiter.GetStatistics()?.CurrentAvailablePermits);
        }

        [Fact]
        public override async Task CanFillQueueWithOldestFirstAfterCancelingFirstQueuedRequestManually()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 3,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 3,
            });

            var lease = limiter.AttemptAcquire(2);
            Assert.True(lease.IsAcquired);

            var lease1 = limiter.AttemptAcquire(1);
            Assert.True(lease1.IsAcquired);

            var cts = new CancellationTokenSource();
            var wait = limiter.AcquireAsync(3, cts.Token);
            cts.Cancel();

            var ex = await Assert.ThrowsAsync<TaskCanceledException>(() => wait.AsTask());
            Assert.Equal(cts.Token, ex.CancellationToken);

            var wait2 = limiter.AcquireAsync(2);

            lease.Dispose();
            lease = await wait2;
            Assert.True(lease.IsAcquired);

            Assert.Equal(0, limiter.GetStatistics().CurrentAvailablePermits);
            Assert.Equal(0, limiter.GetStatistics().CurrentQueuedCount);
        }

        [Fact]
        public override async Task CanCancelAcquireAsyncBeforeQueuing()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1
            });
            var lease = limiter.AttemptAcquire(1);
            Assert.True(lease.IsAcquired);

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var ex = await Assert.ThrowsAsync<TaskCanceledException>(() => limiter.AcquireAsync(1, cts.Token).AsTask());
            Assert.Equal(cts.Token, ex.CancellationToken);

            lease.Dispose();

            Assert.Equal(1, limiter.GetStatistics()?.CurrentAvailablePermits);
        }

        [Fact]
        public override async Task CancelUpdatesQueueLimit()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1
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

            lease.Dispose();
            lease = await wait;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task CanFillQueueWithNewestFirstAfterCancelingQueuedRequestWithAnotherQueuedRequest()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 2
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

            var wait3 = limiter.AcquireAsync(2);
            Assert.False(wait3.IsCompleted);

            // will be kicked by wait3 because we're using NewestFirst
            var lease2 = await wait2;
            Assert.False(lease2.IsAcquired);

            lease.Dispose();

            lease = await wait3;
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public override async Task CanDisposeAfterCancelingQueuedRequest()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1
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
        public override void NoMetadataOnAcquiredLease()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1
            });
            using var lease = limiter.AttemptAcquire(1);
            Assert.False(lease.TryGetMetadata(MetadataName.ReasonPhrase.Name, out _));
        }

        [Fact]
        public override void MetadataNamesContainsAllMetadata()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1
            });
            using var lease = limiter.AttemptAcquire(1);
            Assert.Collection(lease.MetadataNames, metadataName => Assert.Equal(metadataName, MetadataName.ReasonPhrase.Name));
        }

        [Fact]
        public override async Task DisposeReleasesQueuedAcquires()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 3
            });
            using var lease = limiter.AttemptAcquire(1);

            var wait1 = limiter.AcquireAsync(1);
            var wait2 = limiter.AcquireAsync(1);
            var wait3 = limiter.AcquireAsync(1);
            Assert.False(wait1.IsCompleted);
            Assert.False(wait2.IsCompleted);
            Assert.False(wait3.IsCompleted);

            limiter.Dispose();

            var failedLease = await wait1;
            Assert.False(failedLease.IsAcquired);
            failedLease = await wait2;
            Assert.False(failedLease.IsAcquired);
            failedLease = await wait3;
            Assert.False(failedLease.IsAcquired);

            lease.Dispose();

            // Throws after disposal
            Assert.Throws<ObjectDisposedException>(() => limiter.AttemptAcquire(1));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => limiter.AcquireAsync(1).AsTask());
        }

        [Fact]
        public override async Task DisposeAsyncReleasesQueuedAcquires()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 3
            });
            using var lease = limiter.AttemptAcquire(1);

            var wait1 = limiter.AcquireAsync(1);
            var wait2 = limiter.AcquireAsync(1);
            var wait3 = limiter.AcquireAsync(1);
            Assert.False(wait1.IsCompleted);
            Assert.False(wait2.IsCompleted);
            Assert.False(wait3.IsCompleted);

            await limiter.DisposeAsync();

            var failedLease = await wait1;
            Assert.False(failedLease.IsAcquired);
            failedLease = await wait2;
            Assert.False(failedLease.IsAcquired);
            failedLease = await wait3;
            Assert.False(failedLease.IsAcquired);

            lease.Dispose();

            // Throws after disposal
            Assert.Throws<ObjectDisposedException>(() => limiter.AttemptAcquire(1));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => limiter.AcquireAsync(1).AsTask());
        }

        [Fact]
        public async Task ReasonMetadataOnFailedWaitAsync()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1
            });
            using var lease = limiter.AttemptAcquire(2);

            var failedLease = await limiter.AcquireAsync(2);
            Assert.False(failedLease.IsAcquired);
            Assert.True(failedLease.TryGetMetadata(MetadataName.ReasonPhrase.Name, out var metadata));
            Assert.Equal("Queue limit reached", metadata);

            Assert.True(failedLease.TryGetMetadata(MetadataName.ReasonPhrase, out var typedMetadata));
            Assert.Equal("Queue limit reached", typedMetadata);
            Assert.Collection(failedLease.MetadataNames, item => item.Equals(MetadataName.ReasonPhrase.Name));
        }

        [Fact]
        public override void NullIdleDurationWhenActive()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1
            });
            using var lease = limiter.AttemptAcquire(1);
            Assert.Null(limiter.IdleDuration);
        }

        [Fact]
        public override async Task IdleDurationUpdatesWhenIdle()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1
            });
            Assert.NotNull(limiter.IdleDuration);
            var previousDuration = limiter.IdleDuration;
            await Task.Delay(15);
            Assert.True(previousDuration < limiter.IdleDuration);
        }

        [Fact]
        public override void IdleDurationUpdatesWhenChangingFromActive()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1
            });
            var lease = limiter.AttemptAcquire(1);
            lease.Dispose();
            Assert.NotNull(limiter.IdleDuration);
        }

        [Fact]
        public override void GetStatisticsReturnsNewInstances()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1
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
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 100,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 50
            });

            var stats = limiter.GetStatistics();
            Assert.Equal(100, stats.CurrentAvailablePermits);
            Assert.Equal(0, stats.CurrentQueuedCount);
            Assert.Equal(0, stats.TotalFailedLeases);
            Assert.Equal(0, stats.TotalSuccessfulLeases);

            // success from acquire + available
            var lease1 = limiter.AttemptAcquire(60);
            stats = limiter.GetStatistics();
            Assert.Equal(40, stats.CurrentAvailablePermits);
            Assert.Equal(0, stats.CurrentQueuedCount);
            Assert.Equal(0, stats.TotalFailedLeases);
            Assert.Equal(1, stats.TotalSuccessfulLeases);

            // queue
            var lease2Task = limiter.AcquireAsync(50);
            stats = limiter.GetStatistics();
            Assert.Equal(40, stats.CurrentAvailablePermits);
            Assert.Equal(50, stats.CurrentQueuedCount);
            Assert.Equal(0, stats.TotalFailedLeases);
            Assert.Equal(1, stats.TotalSuccessfulLeases);

            // failure from wait
            var lease3 = await limiter.AcquireAsync(1);
            Assert.False(lease3.IsAcquired);
            stats = limiter.GetStatistics();
            Assert.Equal(40, stats.CurrentAvailablePermits);
            Assert.Equal(50, stats.CurrentQueuedCount);
            Assert.Equal(1, stats.TotalFailedLeases);
            Assert.Equal(1, stats.TotalSuccessfulLeases);

            // failure from acquire
            var lease4 = limiter.AttemptAcquire(100);
            Assert.False(lease4.IsAcquired);
            stats = limiter.GetStatistics();
            Assert.Equal(40, stats.CurrentAvailablePermits);
            Assert.Equal(50, stats.CurrentQueuedCount);
            Assert.Equal(2, stats.TotalFailedLeases);
            Assert.Equal(1, stats.TotalSuccessfulLeases);

            lease1.Dispose();
            await lease2Task;

            // success from wait + available + queue
            stats = limiter.GetStatistics();
            Assert.Equal(50, stats.CurrentAvailablePermits);
            Assert.Equal(0, stats.CurrentQueuedCount);
            Assert.Equal(2, stats.TotalFailedLeases);
            Assert.Equal(2, stats.TotalSuccessfulLeases);
        }

        [Fact]
        public override async Task GetStatisticsWithZeroPermitCount()
        {
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 100,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 50
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
            var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = 100,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 50
            });
            limiter.Dispose();
            Assert.Throws<ObjectDisposedException>(limiter.GetStatistics);
        }
    }
}
