// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace System.Threading.RateLimiting
{
    internal sealed class NoopLimiter : RateLimiter
    {
        private static readonly RateLimitLease _lease = new NoopLease();

        private long _totalSuccessfulLeases;

        public NoopLimiter() { }

        public override TimeSpan? IdleDuration => null;

        public override RateLimiterStatistics? GetStatistics()
        {
            return new RateLimiterStatistics()
            {
                CurrentAvailablePermits = long.MaxValue,
                CurrentQueuedCount = 0,
                TotalFailedLeases = 0,
                TotalSuccessfulLeases = Interlocked.Read(ref _totalSuccessfulLeases)
            };
        }

        protected override RateLimitLease AttemptAcquireCore(int permitCount)
        {
            Interlocked.Increment(ref _totalSuccessfulLeases);
            return _lease;
        }

        protected override ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _totalSuccessfulLeases);
            return new ValueTask<RateLimitLease>(_lease);
        }

        private sealed class NoopLease : RateLimitLease
        {
            public override bool IsAcquired => true;

            public override IEnumerable<string> MetadataNames => Array.Empty<string>();

            public override bool TryGetMetadata(string metadataName, out object? metadata)
            {
                metadata = null;
                return false;
            }
        }
    }
}
