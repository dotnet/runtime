// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.RateLimiting.Tests
{
    internal static class Utils
    {
        internal static Func<Task> StopTimerAndGetTimerFunc<T>(PartitionedRateLimiter<T> limiter)
        {
            var innerTimer = limiter.GetType().GetField("_timer", Reflection.BindingFlags.NonPublic | Reflection.BindingFlags.Instance);
            Assert.NotNull(innerTimer);
            var timerStopMethod = innerTimer.FieldType.GetMethod("Stop");
            Assert.NotNull(timerStopMethod);
            // Stop the current Timer so it doesn't fire unexpectedly
            timerStopMethod.Invoke(innerTimer.GetValue(limiter), Array.Empty<object>());

            // Create a new Timer object so that disposing the PartitionedRateLimiter doesn't fail with an ODE, but this new Timer wont actually do anything
            var timerCtor = innerTimer.FieldType.GetConstructor(new Type[] { typeof(TimeSpan), typeof(TimeSpan) });
            Assert.NotNull(timerCtor);
            var newTimer = timerCtor.Invoke(new object[] { TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10) });
            Assert.NotNull(newTimer);
            innerTimer.SetValue(limiter, newTimer);

            var timerLoopMethod = limiter.GetType().GetMethod("Heartbeat", Reflection.BindingFlags.NonPublic | Reflection.BindingFlags.Instance);
            Assert.NotNull(timerLoopMethod);
            return () => (Task)timerLoopMethod.Invoke(limiter, Array.Empty<object>());
        }
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

        public override TimeSpan? IdleDuration => null;

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
        protected override RateLimitLease AcquireCore(int permitCount) => throw new NotImplementedException();
        protected override ValueTask<RateLimitLease> WaitAsyncCore(int permitCount, CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    internal sealed class CustomizableLimiter : RateLimiter
    {
        public Func<TimeSpan?> IdleDurationImpl { get; set; } = () => null;
        public override TimeSpan? IdleDuration => IdleDurationImpl();

        public Func<int> GetAvailablePermitsImpl { get; set; } = () => throw new NotImplementedException();
        public override int GetAvailablePermits() => GetAvailablePermitsImpl();

        public Func<int, RateLimitLease> AcquireCoreImpl { get; set; } = _ => new Lease();
        protected override RateLimitLease AcquireCore(int permitCount) => AcquireCoreImpl(permitCount);

        public Func<int, CancellationToken, ValueTask<RateLimitLease>> WaitAsyncCoreImpl { get; set; } = (_, _) => new ValueTask<RateLimitLease>(new Lease());
        protected override ValueTask<RateLimitLease> WaitAsyncCore(int permitCount, CancellationToken cancellationToken) => WaitAsyncCoreImpl(permitCount, cancellationToken);

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

        public Func<int, RateLimitLease> AcquireCoreImpl { get; set; } = _ => new Lease();
        protected override RateLimitLease AcquireCore(int permitCount) => AcquireCoreImpl(permitCount);

        public Func<int, CancellationToken, ValueTask<RateLimitLease>> WaitAsyncCoreImpl { get; set; } = (_, _) => new ValueTask<RateLimitLease>(new Lease());
        protected override ValueTask<RateLimitLease> WaitAsyncCore(int permitCount, CancellationToken cancellationToken) => WaitAsyncCoreImpl(permitCount, cancellationToken);

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
