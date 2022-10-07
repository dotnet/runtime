// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace System.Threading.RateLimiting
{
    internal sealed class TranslatingLimiter<TInner, TResource> : PartitionedRateLimiter<TResource>
    {
        private readonly PartitionedRateLimiter<TInner> _innerRateLimiter;
        private readonly Func<TResource, TInner> _keyAdapter;
        private readonly bool _disposeInnerLimiter;

        private int _disposed;

        public TranslatingLimiter(PartitionedRateLimiter<TInner> inner, Func<TResource, TInner> keyAdapter, bool leaveOpen)
        {
            _innerRateLimiter = inner;
            _keyAdapter = keyAdapter;
            _disposeInnerLimiter = !leaveOpen;
        }

        public override RateLimiterStatistics? GetStatistics(TResource resource)
        {
            ThrowIfDispose();
            TInner key = _keyAdapter(resource);
            return _innerRateLimiter.GetStatistics(key);
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
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
            {
                if (_disposeInnerLimiter)
                {
                    _innerRateLimiter.Dispose();
                }
            }
        }

        protected override ValueTask DisposeAsyncCore()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
            {
                if (_disposeInnerLimiter)
                {
                    return _innerRateLimiter.DisposeAsync();
                }
            }
            return default(ValueTask);
        }

        private void ThrowIfDispose()
        {
            if (_disposed == 1)
            {
                throw new ObjectDisposedException(nameof(PartitionedRateLimiter<TResource>));
            }
        }
    }
}
