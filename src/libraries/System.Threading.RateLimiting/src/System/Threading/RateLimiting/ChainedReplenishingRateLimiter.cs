// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace System.Threading.RateLimiting
{
    /// <summary>
    /// A chained rate limiter that extends <see cref="ReplenishingRateLimiter"/> when at least one of the
    /// chained limiters is a <see cref="ReplenishingRateLimiter"/>.
    /// </summary>
    internal sealed class ChainedReplenishingRateLimiter : ReplenishingRateLimiter
    {
        private readonly RateLimiter[] _limiters;
        private readonly ReplenishingRateLimiter[] _replenishingLimiters;
        private readonly bool _isAutoReplenishing;
        private readonly TimeSpan _replenishmentPeriod;
        private bool _disposed;

        public ChainedReplenishingRateLimiter(RateLimiter[] limiters)
        {
            _limiters = (RateLimiter[])limiters.Clone();

            var replenishingLimiters = new List<ReplenishingRateLimiter>();
            bool isAutoReplenishing = true;
            TimeSpan lowestPeriod = TimeSpan.MaxValue;

            foreach (RateLimiter limiter in _limiters)
            {
                if (limiter is ReplenishingRateLimiter replenishing)
                {
                    replenishingLimiters.Add(replenishing);

                    if (!replenishing.IsAutoReplenishing)
                    {
                        isAutoReplenishing = false;
                    }

                    TimeSpan period = replenishing.ReplenishmentPeriod;
                    if (period > TimeSpan.Zero && period < lowestPeriod)
                    {
                        lowestPeriod = period;
                    }
                }
            }

            _replenishingLimiters = replenishingLimiters.ToArray();
            _isAutoReplenishing = isAutoReplenishing;
            _replenishmentPeriod = lowestPeriod == TimeSpan.MaxValue ? TimeSpan.Zero : lowestPeriod;
        }

        public override bool IsAutoReplenishing => _isAutoReplenishing;

        public override TimeSpan ReplenishmentPeriod => _replenishmentPeriod;

        public override bool TryReplenish()
        {
            ThrowIfDisposed();

            bool replenished = false;
            List<Exception>? exceptions = null;
            foreach (ReplenishingRateLimiter limiter in _replenishingLimiters)
            {
                try
                {
                    if (limiter.TryReplenish())
                    {
                        replenished = true;
                    }
                }
                catch (Exception ex)
                {
                    exceptions ??= new List<Exception>();
                    exceptions.Add(ex);
                }
            }

            if (exceptions is not null)
            {
                throw new AggregateException(exceptions);
            }

            return replenished;
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
                throw new ObjectDisposedException(nameof(ChainedReplenishingRateLimiter));
            }
        }
    }
}
