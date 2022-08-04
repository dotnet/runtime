// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.RateLimiting.Tests
{
    internal static class Utils
    {
        // Creates a DefaultPartitionedRateLimiter with the timer effectively disabled
        internal static PartitionedRateLimiter<TResource> CreatePartitionedLimiterWithoutTimer<TResource, TKey>(Func<TResource, RateLimitPartition<TKey>> partitioner)
        {
            var limiterType = Type.GetType("System.Threading.RateLimiting.DefaultPartitionedRateLimiter`2, System.Threading.RateLimiting");
            Assert.NotNull(limiterType);

            var genericLimiterType = limiterType.MakeGenericType(typeof(TResource), typeof(TKey));
            Assert.NotNull(genericLimiterType);

            var limiterCtor = genericLimiterType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)[0];
            Assert.NotNull(limiterCtor);

            return (PartitionedRateLimiter<TResource>)limiterCtor.Invoke(new object[] { partitioner, null, TimeSpan.FromMinutes(10) });
        }

        // Gets and runs the Heartbeat function on the DefaultPartitionedRateLimiter
        internal static Task RunTimerFunc<T>(PartitionedRateLimiter<T> limiter)
        {
            // Use Type.GetType so that trimming can see what type we're reflecting on, but assert it's the one we got
            var limiterTypeDef = Type.GetType("System.Threading.RateLimiting.DefaultPartitionedRateLimiter`2, System.Threading.RateLimiting");
            var limiterType = limiter.GetType();
            Assert.Equal(limiterTypeDef, limiterType.GetGenericTypeDefinition());
            if (string.Empty.Length > 0)
                limiterType = limiterTypeDef;

            var innerTimer = limiterType.GetField("_timer", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(innerTimer);

            var timerLoopMethod = limiterType.GetMethod("Heartbeat", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(timerLoopMethod);

            return (Task)timerLoopMethod.Invoke(limiter, Array.Empty<object>());
        }
    }

    internal sealed class NotImplementedPartitionedRateLimiter<T> : PartitionedRateLimiter<T>
    {
        public override int GetAvailablePermits(T resource) => throw new NotImplementedException();
        protected override RateLimitLease AttemptAcquireCore(T resource, int permitCount) => throw new NotImplementedException();
        protected override ValueTask<RateLimitLease> AcquireAsyncCore(T resource, int permitCount, CancellationToken cancellationToken) => throw new NotImplementedException();
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
        public int AcquireAsyncCallCount => _waitAsyncCallCount;
        public int DisposeCallCount => _disposeCallCount;
        public int DisposeAsyncCallCount => _disposeAsyncCallCount;

        public override TimeSpan? IdleDuration => null;

        public override int GetAvailablePermits()
        {
            Interlocked.Increment(ref _getAvailablePermitsCallCount);
            return 1;
        }

        protected override RateLimitLease AttemptAcquireCore(int permitCount)
        {
            Interlocked.Increment(ref _acquireCallCount);
            return new Lease();
        }

        protected override ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken)
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
            public override bool IsAcquired => true;

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

    internal sealed class NotImplementedLimiter : RateLimiter
    {
        public override TimeSpan? IdleDuration => throw new NotImplementedException();

        public override int GetAvailablePermits() => throw new NotImplementedException();
        protected override RateLimitLease AttemptAcquireCore(int permitCount) => throw new NotImplementedException();
        protected override ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    internal sealed class CustomizableLimiter : RateLimiter
    {
        public Func<TimeSpan?> IdleDurationImpl { get; set; } = () => null;
        public override TimeSpan? IdleDuration => IdleDurationImpl();

        public Func<int> GetAvailablePermitsImpl { get; set; } = () => throw new NotImplementedException();
        public override int GetAvailablePermits() => GetAvailablePermitsImpl();

        public Func<int, RateLimitLease> AttemptAcquireCoreImpl { get; set; } = _ => new Lease();
        protected override RateLimitLease AttemptAcquireCore(int permitCount) => AttemptAcquireCoreImpl(permitCount);

        public Func<int, CancellationToken, ValueTask<RateLimitLease>> AcquireAsyncCoreImpl { get; set; } = (_, _) => new ValueTask<RateLimitLease>(new Lease());
        protected override ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken) => AcquireAsyncCoreImpl(permitCount, cancellationToken);

        public Action<bool> DisposeImpl { get; set; } = _ => { };
        protected override void Dispose(bool disposing) => DisposeImpl(disposing);

        public Func<ValueTask> DisposeAsyncCoreImpl { get; set; } = () => default;
        protected override ValueTask DisposeAsyncCore() => DisposeAsyncCoreImpl();

        private sealed class Lease : RateLimitLease
        {
            public override bool IsAcquired => true;

            public override IEnumerable<string> MetadataNames => throw new NotImplementedException();

            public override bool TryGetMetadata(string metadataName, out object? metadata) => throw new NotImplementedException();
        }
    }

    internal sealed class CustomizableReplenishingLimiter : ReplenishingRateLimiter
    {
        public Func<TimeSpan?> IdleDurationImpl { get; set; } = () => null;
        public override TimeSpan? IdleDuration => IdleDurationImpl();

        public Func<int> GetAvailablePermitsImpl { get; set; } = () => throw new NotImplementedException();
        public override int GetAvailablePermits() => GetAvailablePermitsImpl();

        public Func<int, RateLimitLease> AttemptAcquireCoreImpl { get; set; } = _ => new Lease();
        protected override RateLimitLease AttemptAcquireCore(int permitCount) => AttemptAcquireCoreImpl(permitCount);

        public Func<int, CancellationToken, ValueTask<RateLimitLease>> AcquireAsyncCoreImpl { get; set; } = (_, _) => new ValueTask<RateLimitLease>(new Lease());
        protected override ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken) => AcquireAsyncCoreImpl(permitCount, cancellationToken);

        public Func<ValueTask> DisposeAsyncCoreImpl { get; set; } = () => default;
        protected override ValueTask DisposeAsyncCore() => DisposeAsyncCoreImpl();

        public override bool IsAutoReplenishing => false;

        public override TimeSpan ReplenishmentPeriod => throw new NotImplementedException();

        public Func<bool> TryReplenishImpl { get; set; } = () => true;
        public override bool TryReplenish() => TryReplenishImpl();

        private sealed class Lease : RateLimitLease
        {
            public override bool IsAcquired => true;

            public override IEnumerable<string> MetadataNames => throw new NotImplementedException();

            public override bool TryGetMetadata(string metadataName, out object? metadata) => throw new NotImplementedException();
        }
    }

    internal sealed class CustomizableLease : RateLimitLease
    {
        public Func<bool> IsAcquiredImpl = () => true;
        public override bool IsAcquired => IsAcquiredImpl();

        public Func<IEnumerable<string>> MetadataNamesImpl = () => Enumerable.Empty<string>();
        public override IEnumerable<string> MetadataNames => MetadataNamesImpl();

        public delegate bool TryGetMetadataDelegate(string metadataName, out object? metadata);
        public TryGetMetadataDelegate TryGetMetadataImpl = (string name, out object? metadata) =>
            {
                metadata = null;
                return false;
            };
        public override bool TryGetMetadata(string metadataName, out object? metadata) => TryGetMetadataImpl(metadataName, out metadata);

        public Action<bool> DisposeImpl = _ => { };
        protected override void Dispose(bool disposing) => DisposeImpl(disposing);
    }
}
