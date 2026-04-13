// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace System.Threading.RateLimiting
{
    /// <summary>
    /// Acquires leases from rate limiters in the order given. If a lease fails to be acquired (throwing or IsAcquired == false)
    /// then the already acquired leases are disposed in reverse order and the failing lease is returned or the exception is thrown to user code.
    /// </summary>
    internal sealed class ChainedRateLimiter : RateLimiter
    {
        private readonly RateLimiter[] _limiters;
        private bool _disposed;

        public ChainedRateLimiter(RateLimiter[] limiters)
        {
            _limiters = (RateLimiter[])limiters.Clone();
        }

        public override RateLimiterStatistics? GetStatistics()
        {
            ThrowIfDisposed();
            return ChainedRateLimiterShared.GetStatisticsCore(_limiters);
        }

        public override TimeSpan? IdleDuration
        {
            get
            {
                ThrowIfDisposed();
                return ChainedRateLimiterShared.GetIdleDurationCore(_limiters);
            }
        }

        protected override RateLimitLease AttemptAcquireCore(int permitCount)
        {
            ThrowIfDisposed();
            return ChainedRateLimiterShared.AttemptAcquireChained(_limiters, permitCount);
        }

        protected override async ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return await ChainedRateLimiterShared.AcquireAsyncChained(_limiters, permitCount, cancellationToken).ConfigureAwait(false);
        }

        protected override void Dispose(bool disposing)
        {
            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ChainedRateLimiter));
            }
        }
    }
}
