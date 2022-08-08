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
            _limiters = limiters;
        }

        public override int GetAvailablePermits(TResource resource)
        {
            ThrowIfDisposed();
            int lowestPermitCount = int.MaxValue;
            foreach (PartitionedRateLimiter<TResource> limiter in _limiters)
            {
                int permitCount = limiter.GetAvailablePermits(resource);

                if (permitCount < lowestPermitCount)
                {
                    lowestPermitCount = permitCount;
                }
            }

            return lowestPermitCount;
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
                RateLimitLease? notAcquiredLease = CommonAcquireLogic(exception, lease, ref leases, i, _limiters.Length);
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
                RateLimitLease? notAcquiredLease = CommonAcquireLogic(exception, lease, ref leases, i, _limiters.Length);
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

        private static RateLimitLease? CommonAcquireLogic(Exception? ex, RateLimitLease? lease, ref RateLimitLease[]? leases, int index, int length)
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
                throw ex;
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
            private HashSet<string>? _metadataNames;

            public CombinedRateLimitLease(RateLimitLease[] leases)
            {
                _leases = leases;
            }

            public override bool IsAcquired => true;

            public override IEnumerable<string> MetadataNames
            {
                get
                {
                    if (_leases is null)
                    {
                        return Enumerable.Empty<string>();
                    }

                    if (_metadataNames is null)
                    {
                        _metadataNames = new HashSet<string>();
                        foreach (RateLimitLease lease in _leases)
                        {
                            foreach (string metadataName in lease.MetadataNames)
                            {
                                _metadataNames.Add(metadataName);
                            }
                        }
                    }
                    return _metadataNames;
                }
            }

            public override bool TryGetMetadata(string metadataName, out object? metadata)
            {
                if (_leases is not null)
                {
                    foreach (RateLimitLease lease in _leases)
                    {
                        // Use the first metadata item of a given name, ignore duplicates, we can't reliably merge arbitrary metadata
                        // Creating an object[] if there are multiple of the same metadataName could work, but makes consumption of metadata messy
                        // and makes MetadataName.Create<T>(...) uses no longer work
                        if (lease.TryGetMetadata(metadataName, out metadata))
                        {
                            return true;
                        }
                    }
                }

                metadata = null;
                return false;
            }

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
