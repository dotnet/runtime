// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Threading.RateLimiting
{
    /// <summary>
    /// Acquires leases from rate limiters in the order given. If a lease fails to be acquired (throwing or IsAcquired == false)
    /// then the already acquired leases are disposed in reverse order and the failing lease is returned or the exception is thrown to user code.
    /// </summary>
    internal sealed class ChainedPartitionedRateLimiter<TResource> : PartitionedRateLimiter<TResource>
    {
        private readonly PartitionedRateLimiter<TResource>[] _limiters;
        private bool _disposed;

        public ChainedPartitionedRateLimiter(PartitionedRateLimiter<TResource>[] limiters)
        {
            _limiters = (PartitionedRateLimiter<TResource>[])limiters.Clone();
        }

        public override RateLimiterStatistics? GetStatistics(TResource resource)
        {
            ThrowIfDisposed();

            long lowestAvailablePermits = long.MaxValue;
            long currentQueuedCount = 0;
            long totalFailedLeases = 0;
            long innerMostSuccessfulLeases = 0;

            foreach (PartitionedRateLimiter<TResource> limiter in _limiters)
            {
                if (limiter.GetStatistics(resource) is { } statistics)
                {
                    if (statistics.CurrentAvailablePermits < lowestAvailablePermits)
                    {
                        lowestAvailablePermits = statistics.CurrentAvailablePermits;
                    }

                    currentQueuedCount += statistics.CurrentQueuedCount;
                    totalFailedLeases += statistics.TotalFailedLeases;
                    innerMostSuccessfulLeases = statistics.TotalSuccessfulLeases;
                }
            }

            return new RateLimiterStatistics()
            {
                CurrentAvailablePermits = lowestAvailablePermits,
                CurrentQueuedCount = currentQueuedCount,
                TotalFailedLeases = totalFailedLeases,
                TotalSuccessfulLeases = innerMostSuccessfulLeases,
            };
        }

        protected override RateLimitLease AttemptAcquireCore(TResource resource, int permitCount)
        {
            ThrowIfDisposed();

            RateLimitLease[]? leases = null;

            for (int i = 0; i < _limiters.Length; i++)
            {
                RateLimitLease? lease = null;
                Exception? exception = null;

                try
                {
                    lease = _limiters[i].AttemptAcquire(resource, permitCount);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                RateLimitLease? notAcquiredLease = ChainedRateLimiter.CommonAcquireLogic(exception, lease, ref leases, i, _limiters.Length);

                if (notAcquiredLease is not null)
                {
                    return notAcquiredLease;
                }
            }

            return new CombinedRateLimitLease(leases!);
        }

        protected override async ValueTask<RateLimitLease> AcquireAsyncCore(TResource resource, int permitCount, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            RateLimitLease[]? leases = null;

            for (int i = 0; i < _limiters.Length; i++)
            {
                RateLimitLease? lease = null;
                Exception? exception = null;

                try
                {
                    lease = await _limiters[i].AcquireAsync(resource, permitCount, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                RateLimitLease? notAcquiredLease = ChainedRateLimiter.CommonAcquireLogic(exception, lease, ref leases, i, _limiters.Length);

                if (notAcquiredLease is not null)
                {
                    return notAcquiredLease;
                }
            }

            return new CombinedRateLimitLease(leases!);
        }

        protected override void Dispose(bool disposing)
        {
            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ChainedPartitionedRateLimiter<TResource>));
            }
        }
    }
}
