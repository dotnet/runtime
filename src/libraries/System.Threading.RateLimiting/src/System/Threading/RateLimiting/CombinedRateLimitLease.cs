// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace System.Threading.RateLimiting
{
    internal sealed class CombinedRateLimitLease : RateLimitLease
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
                    _metadataNames = [];
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
