// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Threading.RateLimiting
{
    internal sealed class TranslatingLimiter<TInner, TResource> : PartitionedRateLimiter<TResource>
    {
        private readonly PartitionedRateLimiter<TInner> _innerRateLimiter;
        private readonly Func<TResource, TInner> _keyAdapter;

        private bool _disposed;

        public TranslatingLimiter(PartitionedRateLimiter<TInner> inner, Func<TResource, TInner> keyAdapter)
        {
            _innerRateLimiter = inner;
            _keyAdapter = keyAdapter;
        }

        public override int GetAvailablePermits(TResource resource)
        {
            ThrowIfDispose();
            TInner key = _keyAdapter(resource);
            return _innerRateLimiter.GetAvailablePermits(key);
        }

        protected override RateLimitLease AttemptAcquireCore(TResource resource, int permitCount)
        {
            ThrowIfDispose();
            TInner key = _keyAdapter(resource);
            return _innerRateLimiter.AttemptAcquire(key, permitCount);
        }

        protected override ValueTask<RateLimitLease> AcquireAsyncCore(TResource resource, int permitCount, CancellationToken cancellationToken)
        {
            ThrowIfDispose();
            TInner key = _keyAdapter(resource);
            return _innerRateLimiter.AcquireAsync(key, permitCount, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            _disposed = true;
            base.Dispose(disposing);
        }

        protected override ValueTask DisposeAsyncCore()
        {
            _disposed = true;
            return base.DisposeAsyncCore();
        }

        private void ThrowIfDispose()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(PartitionedRateLimiter));
            }
        }
    }
}
