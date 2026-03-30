// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace System.Threading.RateLimiting
{
    /// <summary>
    /// Acquires leases from rate limiters in the order given. If a lease fails to be acquired (throwing or IsAcquired == false)
    /// then the already acquired leases are disposed in reverse order and the failing lease is returned or the exception is thrown to user code.
    /// </summary>
    internal sealed partial class ChainedRateLimiter : RateLimiter
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
            return GetStatisticsCore(_limiters);
        }

        public override TimeSpan? IdleDuration
        {
            get
            {
                ThrowIfDisposed();
                return GetIdleDurationCore(_limiters);
            }
        }

        protected override RateLimitLease AttemptAcquireCore(int permitCount)
        {
            ThrowIfDisposed();
            return AttemptAcquireChained(_limiters, permitCount);
        }

        protected override async ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return await AcquireAsyncChained(_limiters, permitCount, cancellationToken).ConfigureAwait(false);
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

        internal static RateLimiterStatistics GetStatisticsCore(RateLimiter[] limiters)
        {
            long lowestAvailablePermits = long.MaxValue;
            long currentQueuedCount = 0;
            long totalFailedLeases = 0;
            long innerMostSuccessfulLeases = 0;

            foreach (RateLimiter limiter in limiters)
            {
                if (limiter.GetStatistics() is { } statistics)
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

        internal static TimeSpan? GetIdleDurationCore(RateLimiter[] limiters)
        {
            TimeSpan? lowestIdleDuration = null;

            foreach (RateLimiter limiter in limiters)
            {
                TimeSpan? idleDuration = limiter.IdleDuration;
                if (idleDuration is null)
                {
                    // The chain should not be considered idle if any of its children is not idle.
                    return null;
                }

                if (lowestIdleDuration is null || idleDuration < lowestIdleDuration)
                {
                    lowestIdleDuration = idleDuration;
                }
            }

            return lowestIdleDuration;
        }

        internal static RateLimitLease AttemptAcquireChained(RateLimiter[] limiters, int permitCount)
        {
            RateLimitLease[]? leases = null;

            for (int i = 0; i < limiters.Length; i++)
            {
                RateLimitLease? lease = null;
                Exception? exception = null;

                try
                {
                    lease = limiters[i].AttemptAcquire(permitCount);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                RateLimitLease? notAcquiredLease = CommonAcquireLogic(exception, lease, ref leases, i, limiters.Length);

                if (notAcquiredLease is not null)
                {
                    return notAcquiredLease;
                }
            }

            return new CombinedRateLimitLease(leases!);
        }

        internal static async ValueTask<RateLimitLease> AcquireAsyncChained(RateLimiter[] limiters, int permitCount, CancellationToken cancellationToken)
        {
            RateLimitLease[]? leases = null;

            for (int i = 0; i < limiters.Length; i++)
            {
                RateLimitLease? lease = null;
                Exception? exception = null;

                try
                {
                    lease = await limiters[i].AcquireAsync(permitCount, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                RateLimitLease? notAcquiredLease = CommonAcquireLogic(exception, lease, ref leases, i, limiters.Length);

                if (notAcquiredLease is not null)
                {
                    return notAcquiredLease;
                }
            }

            return new CombinedRateLimitLease(leases!);
        }

        internal static RateLimitLease? CommonAcquireLogic(Exception? ex, RateLimitLease? lease, ref RateLimitLease[]? leases, int index, int length)
        {
            if (ex is not null)
            {
                AggregateException? innerEx = CommonDispose(leases, index);

                if (innerEx is not null)
                {
                    Exception[] exceptions = new Exception[innerEx.InnerExceptions.Count + 1];
                    innerEx.InnerExceptions.CopyTo(exceptions, 0);
                    exceptions[exceptions.Length - 1] = ex;
                    throw new AggregateException(exceptions);
                }

                ExceptionDispatchInfo.Capture(ex).Throw();
            }

            if (!lease!.IsAcquired)
            {
                AggregateException? innerEx = CommonDispose(leases, index);
                return innerEx is not null ? throw innerEx : lease;
            }

            leases ??= new RateLimitLease[length];
            leases[index] = lease;
            return null;
        }

        private static AggregateException? CommonDispose(RateLimitLease[]? leases, int i)
        {
            List<Exception>? exceptions = null;

            while (i > 0)
            {
                i--;

                try
                {
                    leases![i].Dispose();
                }
                catch (Exception ex)
                {
                    exceptions ??= [];
                    exceptions.Add(ex);
                }
            }

            if (exceptions is not null)
            {
                return new AggregateException(exceptions);
            }

            return null;
        }
    }
}
