// Licensed to the .NET Foundation under one or more agreements.
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
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await limiter.WaitAsync(string.Empty, -1));
        }

        [Fact]
        public async Task WaitAsyncThrowsWhenPassedACanceledToken()
        {
            using var limiter = new NotImplementedPartitionedRateLimiter<string>();
            await Assert.ThrowsAsync<TaskCanceledException>(
                async () => await limiter.WaitAsync(string.Empty, 1, new CancellationToken(true)));
        }

        // Create

        [Fact]
        public void Create_AcquireCallsUnderlyingPartitionsLimiter()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => limiterFactory.GetLimiter(key));
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
                return RateLimitPartition.Create(1, key => limiterFactory.GetLimiter(key));
            });

            await limiter.WaitAsync("");
            Assert.Equal(1, limiterFactory.Limiters.Count);
            Assert.Equal(1, limiterFactory.Limiters[0].Limiter.WaitAsyncCallCount);
        }

        [Fact]
        public void Create_GetAvailablePermitsCallsUnderlyingPartitionsLimiter()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => limiterFactory.GetLimiter(key));
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
                return RateLimitPartition.Create(1, key => limiterFactory.GetLimiter(key));
            });

            limiter.Acquire("");
            await limiter.WaitAsync("");
            limiter.Acquire("");
            await limiter.WaitAsync("");
            Assert.Equal(1, limiterFactory.Limiters.Count);
            Assert.Equal(2, limiterFactory.Limiters[0].Limiter.AcquireCallCount);
            Assert.Equal(2, limiterFactory.Limiters[0].Limiter.WaitAsyncCallCount);
        }

        [Fact]
        public void Create_MultiplePartitionsWork()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                if (resource == "1")
                {
                    return RateLimitPartition.Create(1, key => limiterFactory.GetLimiter(key));
                }
                else
                {
                    return RateLimitPartition.Create(2, key => limiterFactory.GetLimiter(key));
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
                    return RateLimitPartition.Create(1, key => limiterFactory.GetLimiter(key));
                }
                return RateLimitPartition.CreateConcurrencyLimiter(2,
                    _ => new ConcurrencyLimiterOptions(1, QueueProcessingOrder.OldestFirst, 2));
            });

            var lease = await limiter.WaitAsync("2");
            var wait = limiter.WaitAsync("2");
            Assert.False(wait.IsCompleted);

            // Different partition, should not be blocked by the wait in the other partition
            await limiter.WaitAsync("1");

            lease.Dispose();
            await wait;

            Assert.Equal(1, limiterFactory.Limiters.Count);
            Assert.Equal(0, limiterFactory.Limiters[0].Limiter.AcquireCallCount);
            Assert.Equal(1, limiterFactory.Limiters[0].Limiter.WaitAsyncCallCount);
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
                    return RateLimitPartition.Create(1, key =>
                    {
                        startedTcs.SetResult(null);
                        // block the factory method
                        Assert.True(tcs.Task.Wait(TimeSpan.FromSeconds(10)));
                        return limiterFactory.GetLimiter(key);
                    });
                }
                return RateLimitPartition.Create(2,
                    key => limiterFactory.GetLimiter(key));
            });

            var lease = await limiter.WaitAsync("2");

            var blockedTask = Task.Run(async () =>
            {
                await limiter.WaitAsync("1");
            });
            await startedTcs.Task;

            // Other partitions aren't blocked
            await limiter.WaitAsync("2");

            // Try to acquire from the blocking limiter, this should wait until the blocking limiter has been resolved and not create a new one
            var blockedTask2 = Task.Run(async () =>
            {
                await limiter.WaitAsync("1");
            });

            // unblock limiter factory
            tcs.SetResult(null);
            await blockedTask;
            await blockedTask2;

            // Only 2 limiters should have been created
            Assert.Equal(2, limiterFactory.Limiters.Count);
            Assert.Equal(2, limiterFactory.Limiters[0].Limiter.WaitAsyncCallCount);
            Assert.Equal(2, limiterFactory.Limiters[1].Limiter.WaitAsyncCallCount);
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
                    return RateLimitPartition.Create(1, key => limiterFactory.GetLimiter(key));
                }
                return RateLimitPartition.Create(2, key => limiterFactory.GetLimiter(key));
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
                return RateLimitPartition.Create(1, key => limiterFactory.GetLimiter(key));
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
                    return RateLimitPartition.Create(1, key => limiterFactory.GetLimiter(key));
                }
                return RateLimitPartition.Create(2, key => limiterFactory.GetLimiter(key));
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
        public void Create_DisposeThrowsForFutureMethodCalls()
        {
            var limiterFactory = new TrackingRateLimiterFactory<int>();
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.Create(1, key => limiterFactory.GetLimiter(key));
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
                    return RateLimitPartition.Create(1, key => limiterFactory.GetLimiter(key));
                }
                return RateLimitPartition.Create(2, key => limiterFactory.GetLimiter(key));
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
        public async Task Create_WithTokenBucketReplenishesAutomatically()
        {
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.CreateTokenBucketLimiter(1,
                    _ => new TokenBucketRateLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1, TimeSpan.FromMilliseconds(100), 1, false));
            });

            var lease = limiter.Acquire("");
            Assert.True(lease.IsAcquired);

            lease = await limiter.WaitAsync("");
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public async Task Create_WithReplenishingLimiterReplenishesAutomatically()
        {
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                // Use the non-specific Create method to make sure ReplenishingRateLimiters are still handled properly
                return RateLimitPartition.Create(1,
                    _ => new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1, TimeSpan.FromMilliseconds(100), 1, false)));
            });

            var lease = limiter.Acquire("");
            Assert.True(lease.IsAcquired);

            lease = await limiter.WaitAsync("");
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public async Task Create_MultipleReplenishingLimitersReplenishAutomatically()
        {
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                if (resource == "1")
                {
                    return RateLimitPartition.CreateTokenBucketLimiter(1,
                        _ => new TokenBucketRateLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1, TimeSpan.FromMilliseconds(100), 1, false));
                }
                return RateLimitPartition.CreateTokenBucketLimiter(2,
                    _ => new TokenBucketRateLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1, TimeSpan.FromMilliseconds(100), 1, false));
            });

            var lease = limiter.Acquire("1");
            Assert.True(lease.IsAcquired);

            lease = await limiter.WaitAsync("1");
            Assert.True(lease.IsAcquired);

            // Creates the second Replenishing limiter
            // Indirectly tests that the cached list of limiters used by the timer is probably updated by making sure a limiter already made use of it before we create a second replenishing one
            lease = limiter.Acquire("2");
            Assert.True(lease.IsAcquired);

            lease = await limiter.WaitAsync("1");
            Assert.True(lease.IsAcquired);
            lease = await limiter.WaitAsync("2");
            Assert.True(lease.IsAcquired);
        }

        [Fact]
        public async Task Create_CancellationTokenPassedToUnderlyingLimiter()
        {
            using var limiter = PartitionedRateLimiter.Create<string, int>(resource =>
            {
                return RateLimitPartition.CreateConcurrencyLimiter(1,
                    _ => new ConcurrencyLimiterOptions(1, QueueProcessingOrder.NewestFirst, 1));
            });

            var lease = limiter.Acquire("");
            Assert.True(lease.IsAcquired);

            var cts = new CancellationTokenSource();
            var waitTask = limiter.WaitAsync("", 1, cts.Token);
            Assert.False(waitTask.IsCompleted);
            cts.Cancel();
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await waitTask);
        }

        internal sealed class NotImplementedPartitionedRateLimiter<T> : PartitionedRateLimiter<T>
        {
            public override int GetAvailablePermits(T resourceID) => throw new NotImplementedException();
            protected override RateLimitLease AcquireCore(T resourceID, int permitCount) => throw new NotImplementedException();
            protected override ValueTask<RateLimitLease> WaitAsyncCore(T resourceID, int permitCount, CancellationToken cancellationToken) => throw new NotImplementedException();
        }

        internal sealed class TrackingRateLimiter : RateLimiter
        {
            private int _getAvailablePermitsCallCount;
            private int _acquireCallCount;
            private int _waitAsyncCallCount;
            private int _disposeCallCount;
            private int _disposeAsyncCallCount;

            public int GetAvailablePermitsCallCount => _getAvailablePermitsCallCount;
            public int AcquireCallCount => _acquireCallCount;
            public int WaitAsyncCallCount => _waitAsyncCallCount;
            public int DisposeCallCount => _disposeCallCount;
            public int DisposeAsyncCallCount => _disposeAsyncCallCount;

            public override TimeSpan? IdleDuration => throw new NotImplementedException();

            public override int GetAvailablePermits()
            {
                Interlocked.Increment(ref _getAvailablePermitsCallCount);
                return 1;
            }

            protected override RateLimitLease AcquireCore(int permitCount)
            {
                Interlocked.Increment(ref _acquireCallCount);
                return new Lease();
            }

            protected override ValueTask<RateLimitLease> WaitAsyncCore(int permitCount, CancellationToken cancellationToken)
            {
                Interlocked.Increment(ref _waitAsyncCallCount);
                return new ValueTask<RateLimitLease>(new Lease());
            }

            protected override void Dispose(bool disposing)
            {
                Interlocked.Increment(ref _disposeCallCount);
            }

            protected override ValueTask DisposeAsyncCore()
            {
                Interlocked.Increment(ref _disposeAsyncCallCount);
                return new ValueTask();
            }

            private sealed class Lease : RateLimitLease
            {
                public override bool IsAcquired => throw new NotImplementedException();

                public override IEnumerable<string> MetadataNames => throw new NotImplementedException();

                public override bool TryGetMetadata(string metadataName, out object? metadata) => throw new NotImplementedException();
            }
        }

        internal sealed class TrackingRateLimiterFactory<TKey>
        {
            public List<(TKey Key, TrackingRateLimiter Limiter)> Limiters { get; } = new();

            public RateLimiter GetLimiter(TKey key)
            {
                TrackingRateLimiter limiter;
                lock (Limiters)
                {
                    limiter = new TrackingRateLimiter();
                    Limiters.Add((key, limiter));
                }
                return limiter;
            }
        }

        internal sealed class TestEquality : IEqualityComparer<int>
        {
            private int _equalsCallCount;
            private int _getHashCodeCallCount;

            public int EqualsCallCount => _equalsCallCount;
            public int GetHashCodeCallCount => _getHashCodeCallCount;

            public bool Equals(int x, int y)
            {
                Interlocked.Increment(ref _equalsCallCount);
                return x == y;
            }
            public int GetHashCode([DisallowNull] int obj)
            {
                Interlocked.Increment(ref _getHashCodeCallCount);
                return obj.GetHashCode();
            }
        }
    }
}
