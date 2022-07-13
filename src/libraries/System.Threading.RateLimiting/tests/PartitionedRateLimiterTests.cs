﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.RateLimiting.Tests
{
    public class PartitionedRateLimiterTests
    {
        [Fact]
        public void ThrowsWhenAcquiringLessThanZero()
        {
            using var limiter = new NotImplementedPartitionedRateLimiter<string>();
            Assert.Throws<ArgumentOutOfRangeException>(() => limiter.Acquire(string.Empty, -1));
        }

        [Fact]
        public async Task ThrowsWhenWaitingForLessThanZero()
        {
            using var limiter = new NotImplementedPartitionedRateLimiter<string>();
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await limiter.WaitAndAcquireAsync(string.Empty, -1));
        }

        [Fact]
        public async Task WaitAsyncThrowsWhenPassedACanceledToken()
        {
            using var limiter = new NotImplementedPartitionedRateLimiter<string>();
            await Assert.ThrowsAsync<TaskCanceledException>(
                async () => await limiter.WaitAndAcquireAsync(string.Empty, 1, new CancellationToken(true)));
        }

        [Fact]
        public void Create_AcquireCallsUnderlyingPartitionsLimiter()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => limiterFactory.GetLimiter(key));
            });

            limiter.Acquire("");
            Assert.Equal(1, limiterFactory.Limiters.Count);
            Assert.Equal(1, limiterFactory.Limiters[0].Limiter.AcquireCallCount);
        }

        [Fact]
        public async Task Create_WaitAsyncCallsUnderlyingPartitionsLimiter()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => limiterFactory.GetLimiter(key));
            });

            await limiter.WaitAndAcquireAsync("");
            Assert.Equal(1, limiterFactory.Limiters.Count);
            Assert.Equal(1, limiterFactory.Limiters[0].Limiter.WaitAndAcquireAsyncCallCount);
        }

        [Fact]
        public void Create_GetAvailablePermitsCallsUnderlyingPartitionsLimiter()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => limiterFactory.GetLimiter(key));
            });

            limiter.GetAvailablePermits("");
            Assert.Equal(1, limiterFactory.Limiters.Count);
            Assert.Equal(1, limiterFactory.Limiters[0].Limiter.GetAvailablePermitsCallCount);
        }

        [Fact]
        public async Task Create_PartitionIsCached()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => limiterFactory.GetLimiter(key));
            });

            limiter.Acquire("");
            await limiter.WaitAndAcquireAsync("");
            limiter.Acquire("");
            await limiter.WaitAndAcquireAsync("");
            Assert.Equal(1, limiterFactory.Limiters.Count);
            Assert.Equal(2, limiterFactory.Limiters[0].Limiter.AcquireCallCount);
            Assert.Equal(2, limiterFactory.Limiters[0].Limiter.WaitAndAcquireAsyncCallCount);
        }

        [Fact]
        public void Create_MultiplePartitionsWork()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                if (resource == "1")
                {
                    return RateLimitPartition.Get(1, key => limiterFactory.GetLimiter(key));
                }
                else
                {
                    return RateLimitPartition.Get(2, key => limiterFactory.GetLimiter(key));
                }
            });

            limiter.Acquire("1");
            limiter.Acquire("2");
            limiter.Acquire("1");
            limiter.Acquire("2");

            Assert.Equal(2, limiterFactory.Limiters.Count);

            Assert.Equal(2, limiterFactory.Limiters[0].Limiter.AcquireCallCount);
            Assert.Equal(1, limiterFactory.Limiters[0].Key);

            Assert.Equal(2, limiterFactory.Limiters[1].Limiter.AcquireCallCount);
            Assert.Equal(2, limiterFactory.Limiters[1].Key);
        }

        [Fact]
        public async Task Create_BlockingWaitDoesNotBlockOtherPartitions()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                if (resource == "1")
                {
                    return RateLimitPartition.Get(1, key => limiterFactory.GetLimiter(key));
                }
                return RateLimitPartition.GetConcurrencyLimiter(2,
                    _ => new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 2));
            });

            var lease = await limiter.WaitAndAcquireAsync("2");
            var wait = limiter.WaitAndAcquireAsync("2");
            Assert.False(wait.IsCompleted);

            // Different partition, should not be blocked by the wait in the other partition
            await limiter.WaitAndAcquireAsync("1");

            lease.Dispose();
            await wait;

            Assert.Equal(1, limiterFactory.Limiters.Count);
            Assert.Equal(0, limiterFactory.Limiters[0].Limiter.AcquireCallCount);
            Assert.Equal(1, limiterFactory.Limiters[0].Limiter.WaitAndAcquireAsyncCallCount);
        }

        // Uses Task.Wait in a Task.Run to purposefully test a blocking scenario, this doesn't work on WASM currently
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task Create_BlockingFactoryDoesNotBlockOtherPartitions()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var startedTcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                if (resource == "1")
                {
                    return RateLimitPartition.Get(1, key =>
                    {
                        startedTcs.SetResult(null);
                        // block the factory method
                        Assert.True(tcs.Task.Wait(TimeSpan.FromSeconds(10)));
                        return limiterFactory.GetLimiter(key);
                    });
                }
                return RateLimitPartition.Get(2,
                    key => limiterFactory.GetLimiter(key));
            });

            var lease = await limiter.WaitAndAcquireAsync("2");

            var blockedTask = Task.Run(async () =>
            {
                await limiter.WaitAndAcquireAsync("1");
            });
            await startedTcs.Task;

            // Other partitions aren't blocked
            await limiter.WaitAndAcquireAsync("2");

            // Try to acquire from the blocking limiter, this should wait until the blocking limiter has been resolved and not create a new one
            var blockedTask2 = Task.Run(async () =>
            {
                await limiter.WaitAndAcquireAsync("1");
            });

            // unblock limiter factory
            tcs.SetResult(null);
            await blockedTask;
            await blockedTask2;

            // Only 2 limiters should have been created
            Assert.Equal(2, limiterFactory.Limiters.Count);
            Assert.Equal(2, limiterFactory.Limiters[0].Limiter.WaitAndAcquireAsyncCallCount);
            Assert.Equal(2, limiterFactory.Limiters[1].Limiter.WaitAndAcquireAsyncCallCount);
        }

        [Fact]
        public void Create_PassedInEqualityComparerIsUsed()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            var equality = new TestEquality();
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                if (resource == "1")
                {
                    return RateLimitPartition.Get(1, key => limiterFactory.GetLimiter(key));
                }
                return RateLimitPartition.Get(2, key => limiterFactory.GetLimiter(key));
            }, equality);

            limiter.Acquire("1");
            // GetHashCode to add item to dictionary (skips TryGet for empty dictionary)
            Assert.Equal(0, equality.EqualsCallCount);
            Assert.Equal(1, equality.GetHashCodeCallCount);
            limiter.Acquire("1");
            // GetHashCode and Equal from TryGet to see if item is in dictionary
            Assert.Equal(1, equality.EqualsCallCount);
            Assert.Equal(2, equality.GetHashCodeCallCount);
            limiter.Acquire("2");
            // GetHashCode from TryGet (fails check) and second GetHashCode to add item to dictionary
            Assert.Equal(1, equality.EqualsCallCount);
            Assert.Equal(4, equality.GetHashCodeCallCount);

            Assert.Equal(2, limiterFactory.Limiters.Count);
            Assert.Equal(2, limiterFactory.Limiters[0].Limiter.AcquireCallCount);
            Assert.Equal(1, limiterFactory.Limiters[1].Limiter.AcquireCallCount);
        }

        [Fact]
        public void Create_DisposeWithoutLimitersNoops()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => limiterFactory.GetLimiter(key));
            });

            limiter.Dispose();

            Assert.Equal(0, limiterFactory.Limiters.Count);
        }

        [Fact]
        public void Create_DisposeDisposesAllLimiters()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                if (resource == "1")
                {
                    return RateLimitPartition.Get(1, key => limiterFactory.GetLimiter(key));
                }
                return RateLimitPartition.Get(2, key => limiterFactory.GetLimiter(key));
            });

            limiter.Acquire("1");
            limiter.Acquire("2");

            limiter.Dispose();

            Assert.Equal(2, limiterFactory.Limiters.Count);
            Assert.Equal(1, limiterFactory.Limiters[0].Limiter.AcquireCallCount);
            Assert.Equal(1, limiterFactory.Limiters[0].Limiter.DisposeCallCount);

            Assert.Equal(1, limiterFactory.Limiters[1].Limiter.AcquireCallCount);
            Assert.Equal(1, limiterFactory.Limiters[1].Limiter.DisposeCallCount);
        }

        [Fact]
        public void Create_DisposeWithThrowingDisposes_DisposesAllLimiters()
        {
            var limiter1 = new CustomizableLimiter();
            limiter1.DisposeImpl = _ => throw new Exception();
            var limiter2 = new CustomizableLimiter();
            limiter2.DisposeImpl = _ => throw new Exception();
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                if (resource == "1")
                {
                    return RateLimitPartition.Get(1, _ => limiter1);
                }
                return RateLimitPartition.Get(2, _ => limiter2);
            });

            limiter.Acquire("1");
            limiter.Acquire("2");

            var ex = Assert.Throws<AggregateException>(() => limiter.Dispose());
            Assert.Equal(2, ex.InnerExceptions.Count);
        }

        [Fact]
        public void Create_DisposeThrowsForFutureMethodCalls()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, key => limiterFactory.GetLimiter(key));
            });

            limiter.Dispose();

            Assert.Throws<ObjectDisposedException>(() => limiter.Acquire("1"));

            Assert.Equal(0, limiterFactory.Limiters.Count);
        }

        [Fact]
        public async Task Create_DisposeAsyncDisposesAllLimiters()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                if (resource == "1")
                {
                    return RateLimitPartition.Get(1, key => limiterFactory.GetLimiter(key));
                }
                return RateLimitPartition.Get(2, key => limiterFactory.GetLimiter(key));
            });

            limiter.Acquire("1");
            limiter.Acquire("2");

            await limiter.DisposeAsync();

            Assert.Equal(2, limiterFactory.Limiters.Count);
            Assert.Equal(1, limiterFactory.Limiters[0].Limiter.AcquireCallCount);
            Assert.Equal(1, limiterFactory.Limiters[0].Limiter.DisposeCallCount);
            Assert.Equal(1, limiterFactory.Limiters[0].Limiter.DisposeAsyncCallCount);

            Assert.Equal(1, limiterFactory.Limiters[1].Limiter.AcquireCallCount);
            Assert.Equal(1, limiterFactory.Limiters[1].Limiter.DisposeCallCount);
            Assert.Equal(1, limiterFactory.Limiters[1].Limiter.DisposeAsyncCallCount);
        }

        [Fact]
        public async Task Create_DisposeAsyncWithThrowingDisposes_DisposesAllLimiters()
        {
            var limiter1 = new CustomizableLimiter();
            limiter1.DisposeAsyncCoreImpl = () => throw new Exception();
            var limiter2 = new CustomizableLimiter();
            limiter2.DisposeAsyncCoreImpl = () => throw new Exception();
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                if (resource == "1")
                {
                    return RateLimitPartition.Get(1, _ => limiter1);
                }
                return RateLimitPartition.Get(2, _ => limiter2);
            });

            limiter.Acquire("1");
            limiter.Acquire("2");

            var ex = await Assert.ThrowsAsync<AggregateException>(() => limiter.DisposeAsync().AsTask());
            Assert.Equal(2, ex.InnerExceptions.Count);
        }

        [Fact]
        public async Task Create_WithTokenBucketReplenishesAutomatically()
        {
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.GetTokenBucketLimiter(1,
                    _ => new TokenBucketRateLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1, TimeSpan.FromMilliseconds(100), 1, false));
            });

            var lease = limiter.Acquire("");
            Assert.True(lease.IsAcquired);

            lease = await limiter.WaitAndAcquireAsync("");
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public async Task Create_WithReplenishingLimiterReplenishesAutomatically()
        {
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                // Use the non-specific Create method to make sure ReplenishingRateLimiters are still handled properly
                return RateLimitPartition.Get(1,
                    _ => new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1, TimeSpan.FromMilliseconds(100), 1, false)));
            });

            var lease = limiter.Acquire("");
            Assert.True(lease.IsAcquired);

            lease = await limiter.WaitAndAcquireAsync("");
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public async Task Create_MultipleReplenishingLimitersReplenishAutomatically()
        {
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                if (resource == "1")
                {
                    return RateLimitPartition.GetTokenBucketLimiter(1,
                        _ => new TokenBucketRateLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1, TimeSpan.FromMilliseconds(100), 1, false));
                }
                return RateLimitPartition.GetTokenBucketLimiter(2,
                    _ => new TokenBucketRateLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1, TimeSpan.FromMilliseconds(100), 1, false));
            });

            var lease = limiter.Acquire("1");
            Assert.True(lease.IsAcquired);

            lease = await limiter.WaitAndAcquireAsync("1");
            Assert.True(lease.IsAcquired);

            // Creates the second Replenishing limiter
            // Indirectly tests that the cached list of limiters used by the timer is probably updated by making sure a limiter already made use of it before we create a second replenishing one
            lease = limiter.Acquire("2");
            Assert.True(lease.IsAcquired);

            lease = await limiter.WaitAndAcquireAsync("1");
            Assert.True(lease.IsAcquired);
            lease = await limiter.WaitAndAcquireAsync("2");
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public async Task Create_CancellationTokenPassedToUnderlyingLimiter()
        {
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.GetConcurrencyLimiter(1,
                    _ => new ConcurrencyLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1));
            });

            var lease = limiter.Acquire("");
            Assert.True(lease.IsAcquired);

            var cts = new CancellationTokenSource();
            var waitTask = limiter.WaitAndAcquireAsync("", 1, cts.Token);
            Assert.False(waitTask.IsCompleted);
            cts.Cancel();
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await waitTask);
        }

        [Fact]
        public async Task IdleLimiterIsCleanedUp()
        {
            CustomizableLimiter innerLimiter = null;
            var factoryCallCount = 0;
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Get(1, _ =>
                {
                    factoryCallCount++;
                    innerLimiter = new CustomizableLimiter();
                    return innerLimiter;
                });
            });

            var timerLoopMethod = Utils.StopTimerAndGetTimerFunc(limiter);

            var lease = limiter.Acquire("");
            Assert.True(lease.IsAcquired);

            Assert.Equal(1, factoryCallCount);

            var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            innerLimiter.DisposeAsyncCoreImpl = () =>
            {
                tcs.SetResult(null);
                return default;
            };
            innerLimiter.IdleDurationImpl = () => TimeSpan.FromMinutes(1);

            await timerLoopMethod();

            // Limiter is disposed when timer runs and sees that IdleDuration is greater than idle limit
            await tcs.Task;
            innerLimiter.DisposeAsyncCoreImpl = () => default;

            // Acquire will call limiter factory again as the limiter was disposed and removed
            lease = limiter.Acquire("");
            Assert.True(lease.IsAcquired);
            Assert.Equal(2, factoryCallCount);
        }

        [Fact]
        public async Task AllIdleLimitersCleanedUp_DisposeThrows()
        {
            CustomizableLimiter innerLimiter1 = null;
            CustomizableLimiter innerLimiter2 = null;
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                if (resource == "1")
                {
                    return RateLimitPartition.Get(1, _ =>
                    {
                        innerLimiter1 = new CustomizableLimiter();
                        return innerLimiter1;
                    });
                }
                else
                {
                    return RateLimitPartition.Get(2, _ =>
                    {
                        innerLimiter2 = new CustomizableLimiter();
                        return innerLimiter2;
                    });
                }
            });

            var timerLoopMethod = Utils.StopTimerAndGetTimerFunc(limiter);

            var lease = limiter.Acquire("1");
            Assert.True(lease.IsAcquired);
            Assert.NotNull(innerLimiter1);
            limiter.Acquire("2");
            Assert.NotNull(innerLimiter2);

            var dispose1Called = false;
            var dispose2Called = false;
            innerLimiter1.DisposeAsyncCoreImpl = () =>
            {
                dispose1Called = true;
                throw new Exception();
            };
            innerLimiter1.IdleDurationImpl = () => TimeSpan.FromMinutes(1);
            innerLimiter2.DisposeAsyncCoreImpl = () =>
            {
                dispose2Called = true;
                throw new Exception();
            };
            innerLimiter2.IdleDurationImpl = () => TimeSpan.FromMinutes(1);

            // Run Timer
            var ex = await Assert.ThrowsAsync<AggregateException>(() => timerLoopMethod());

            Assert.True(dispose1Called);
            Assert.True(dispose2Called);

            Assert.Equal(2, ex.InnerExceptions.Count);
        }

        [Fact]
        public async Task ThrowingTryReplenishDoesNotPreventIdleLimiterBeingCleanedUp()
        {
            CustomizableReplenishingLimiter replenishLimiter = new CustomizableReplenishingLimiter();
            CustomizableLimiter idleLimiter = null;
            var factoryCallCount = 0;
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                if (resource == "1")
                {
                    return RateLimitPartition.Get(1, _ =>
                    {
                        factoryCallCount++;
                        idleLimiter = new CustomizableLimiter();
                        return idleLimiter;
                    });
                }
                return RateLimitPartition.Get(2, _ =>
                {
                    return replenishLimiter;
                });
            });

            var timerLoopMethod = Utils.StopTimerAndGetTimerFunc(limiter);

            // Add the replenishing limiter to the internal storage
            limiter.Acquire("2");
            var lease = limiter.Acquire("1");
            Assert.True(lease.IsAcquired);
            Assert.Equal(1, factoryCallCount);

            // Start throwing from TryReplenish, this will happen the next time the Timer runs
            replenishLimiter.TryReplenishImpl = () => throw new Exception();

            var disposeTcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            // This DisposeAsync will be called in the same Timer iteration as the throwing TryReplenish, so we block below on the disposeTcs to make sure DisposeAsync is called even with a throwing TryReplenish
            idleLimiter.DisposeAsyncCoreImpl = () =>
            {
                disposeTcs.SetResult(null);
                return default;
            };
            idleLimiter.IdleDurationImpl = () => TimeSpan.FromMinutes(1);

            var ex = await Assert.ThrowsAsync<AggregateException>(() => timerLoopMethod());
            Assert.Single(ex.InnerExceptions);

            // Wait for Timer to run again which will see the throwing TryReplenish and an idle limiter it needs to clean-up
            await disposeTcs.Task;
        }
    }
}
