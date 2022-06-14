// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Threading.RateLimiting
{
    internal sealed class ChainedPartitionedRateLimiter<TResource> : PartitionedRateLimiter<TResource>
    {
        private readonly PartitionedRateLimiter<TResource>[] _limiters;

        public ChainedPartitionedRateLimiter(PartitionedRateLimiter<TResource>[] limiters)
        {
            _limiters = limiters;
        }

        public override int GetAvailablePermits(TResource resourceID)
        {
            int lowestPermitCount = int.MaxValue;
            foreach (PartitionedRateLimiter<TResource> limiter in _limiters)
            {
                int permitCount = limiter.GetAvailablePermits(resourceID);

                if (permitCount < lowestPermitCount)
                {
                    lowestPermitCount = permitCount;
                }
            }

            return lowestPermitCount;
        }

        protected override RateLimitLease AcquireCore(TResource resourceID, int permitCount)
        {
            RateLimitLease[]? leases = null;
            for (int i = 0; i < _limiters.Length; i++)
            {
                RateLimitLease lease;
                try
                {
                    lease = _limiters[i].Acquire(resourceID, permitCount);
                }
                catch (Exception ex)
                {
                    Exception? innerEx = CommonDispose(leases, i);
                    if (innerEx is AggregateException aggregateException)
                    {
                        Exception[] exceptions = new Exception[aggregateException.InnerExceptions.Count + 1];
                        aggregateException.InnerExceptions.CopyTo(exceptions, 0);
                        exceptions[exceptions.Length - 1] = ex;
                        throw new AggregateException(exceptions);
                    }
                    // REVIEW: Chose consistent exception type here, but could be convinced to just throw the original exception as is
                    throw new AggregateException(ex);
                }

                if (!lease.IsAcquired)
                {
                    Exception? ex = CommonDispose(leases, i);
                    return ex is not null ? throw ex : lease;
                }

                leases ??= new RateLimitLease[_limiters.Length];
                leases[i] = lease;
            }

            return new CombinedRateLimitLease(leases!);
        }

        protected override async ValueTask<RateLimitLease> WaitAsyncCore(TResource resourceID, int permitCount, CancellationToken cancellationToken)
        {
            RateLimitLease[]? leases = null;
            for (int i = 0; i < _limiters.Length; i++)
            {
                RateLimitLease lease;
                try
                {
                    lease = await _limiters[i].WaitAsync(resourceID, permitCount, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Exception? innerEx = CommonDispose(leases, i);
                    if (innerEx is AggregateException aggregateException)
                    {
                        Exception[] exceptions = new Exception[aggregateException.InnerExceptions.Count + 1];
                        aggregateException.InnerExceptions.CopyTo(exceptions, 0);
                        exceptions[exceptions.Length - 1] = ex;
                        throw new AggregateException(exceptions);
                    }
                    // REVIEW: Chose consistent exception type here, but could be convinced to just throw the original exception as is
                    throw new AggregateException(ex);
                }
                if (!lease.IsAcquired)
                {
                    Exception? ex = CommonDispose(leases, i);
                    return ex is not null ? throw ex : lease;
                }

                leases ??= new RateLimitLease[_limiters.Length];
                leases[i] = lease;
            }

            return new CombinedRateLimitLease(leases!);
        }

        // REVIEW: Do we dispose the inner limiters? Or just mark the object as disposed and throw in all the methods
        protected override void Dispose(bool disposing) => base.Dispose(disposing);
        protected override ValueTask DisposeAsyncCore() => base.DisposeAsyncCore();

        // Common dispose logic for leases when calling Acquire or WaitAsync and one of the limiters throws or can't be acquired at this time
        private static Exception? CommonDispose(RateLimitLease[]? leases, int i)
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
                    exceptions ??= new List<Exception>();
                    exceptions.Add(ex);
                }
            }

            if (exceptions is not null)
            {
                return new AggregateException(exceptions);
            }

            return null;
        }

        private sealed class CombinedRateLimitLease : RateLimitLease
        {
            private RateLimitLease[]? _leases;

            public CombinedRateLimitLease(RateLimitLease[] leases)
            {
                _leases = leases;
            }

            public override bool IsAcquired => true;

            public override IEnumerable<string> MetadataNames => throw new NotImplementedException();

            public override bool TryGetMetadata(string metadataName, out object? metadata) => throw new NotImplementedException();

            protected override void Dispose(bool disposing)
            {
                if (_leases is null)
                {
                    return;
                }

                List<Exception>? exceptions = null;
                // Dispose in reverse order
                // Avoids issues where dispose might unblock a queued acquire and then the acquire fails when acquiring the next limiter.
                // When disposing in reverse order there wont be any issues of unblocking an acquire that affects acquires on limiters in the chain after it
                for (int i = _leases.Length - 1; i >= 0; i--)
                {
                    try
                    {
                        _leases[i].Dispose();
                    }
                    catch (Exception ex)
                    {
                        exceptions ??= new List<Exception>();
                        exceptions.Add(ex);
                    }
                }

                _leases = null;

                if (exceptions is not null)
                {
                    throw new AggregateException(exceptions);
                }
            }
        }
    }
}
