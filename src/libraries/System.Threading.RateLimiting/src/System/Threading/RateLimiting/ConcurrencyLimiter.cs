// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace System.Threading.RateLimiting
{
    /// <summary>
    /// <see cref="RateLimiter"/> implementation that helps manage concurrent access to a resource.
    /// </summary>
    public sealed class ConcurrencyLimiter : RateLimiter
    {
        private int _permitCount;
        private int _queueCount;
        private long? _idleSince = Stopwatch.GetTimestamp();
        private bool _disposed;

        private readonly ConcurrencyLimiterOptions _options;
        private readonly Deque<RequestRegistration> _queue = new Deque<RequestRegistration>();

        private static readonly ConcurrencyLease SuccessfulLease = new ConcurrencyLease(true, null, 0);
        private static readonly ConcurrencyLease FailedLease = new ConcurrencyLease(false, null, 0);
        private static readonly ConcurrencyLease QueueLimitLease = new ConcurrencyLease(false, null, 0, "Queue limit reached");
        private static readonly double TickFrequency = (double)TimeSpan.TicksPerSecond / Stopwatch.Frequency;

        // Use the queue as the lock field so we don't need to allocate another object for a lock and have another field in the object
        private object Lock => _queue;

        /// <inheritdoc />
        public override TimeSpan? IdleDuration => _idleSince is null ? null : new TimeSpan((long)((Stopwatch.GetTimestamp() - _idleSince) * TickFrequency));

        /// <summary>
        /// Initializes the <see cref="ConcurrencyLimiter"/>.
        /// </summary>
        /// <param name="options">Options to specify the behavior of the <see cref="ConcurrencyLimiter"/>.</param>
        public ConcurrencyLimiter(ConcurrencyLimiterOptions options!!)
        {
            _options = options;
            _permitCount = _options.PermitLimit;
        }

        /// <inheritdoc/>
        public override int GetAvailablePermits() => _permitCount;

        /// <inheritdoc/>
        protected override RateLimitLease AcquireCore(int permitCount)
        {
            // These amounts of resources can never be acquired
            if (permitCount > _options.PermitLimit)
            {
                throw new ArgumentOutOfRangeException(nameof(permitCount), permitCount, SR.Format(SR.PermitLimitExceeded, permitCount, _options.PermitLimit));
            }

            ThrowIfDisposed();

            // Return SuccessfulLease or FailedLease to indicate limiter state
            if (permitCount == 0)
            {
                return _permitCount > 0 ? SuccessfulLease : FailedLease;
            }

            // Perf: Check SemaphoreSlim implementation instead of locking
            if (_permitCount >= permitCount)
            {
                lock (Lock)
                {
                    if (TryLeaseUnsynchronized(permitCount, out RateLimitLease? lease))
                    {
                        return lease;
                    }
                }
            }

            return FailedLease;
        }

        /// <inheritdoc/>
        protected override ValueTask<RateLimitLease> WaitAsyncCore(int permitCount, CancellationToken cancellationToken = default)
        {
            // These amounts of resources can never be acquired
            if (permitCount > _options.PermitLimit)
            {
                throw new ArgumentOutOfRangeException(nameof(permitCount), permitCount, SR.Format(SR.PermitLimitExceeded, permitCount, _options.PermitLimit));
            }

            // Return SuccessfulLease if requestedCount is 0 and resources are available
            if (permitCount == 0 && _permitCount > 0 && !_disposed)
            {
                return new ValueTask<RateLimitLease>(SuccessfulLease);
            }

            // Perf: Check SemaphoreSlim implementation instead of locking
            lock (Lock)
            {
                if (TryLeaseUnsynchronized(permitCount, out RateLimitLease? lease))
                {
                    return new ValueTask<RateLimitLease>(lease);
                }

                // Avoid integer overflow by using subtraction instead of addition
                Debug.Assert(_options.QueueLimit >= _queueCount);
                if (_options.QueueLimit - _queueCount < permitCount)
                {
                    if (_options.QueueProcessingOrder == QueueProcessingOrder.NewestFirst && permitCount <= _options.QueueLimit)
                    {
                        // remove oldest items from queue until there is space for the newest request
                        do
                        {
                            RequestRegistration oldestRequest = _queue.DequeueHead();
                            _queueCount -= oldestRequest.Count;
                            Debug.Assert(_queueCount >= 0);
                            oldestRequest.Tcs.TrySetResult(FailedLease);
                        }
                        while (_options.QueueLimit - _queueCount < permitCount);
                    }
                    else
                    {
                        // Don't queue if queue limit reached and QueueProcessingOrder is OldestFirst
                        return new ValueTask<RateLimitLease>(QueueLimitLease);
                    }
                }

                CancelQueueState tcs = new CancelQueueState(permitCount, this, cancellationToken);
                CancellationTokenRegistration ctr = default;
                if (cancellationToken.CanBeCanceled)
                {
                    ctr = cancellationToken.Register(static obj =>
                    {
                        ((CancelQueueState)obj!).TrySetCanceled();
                    }, tcs);
                }

                RequestRegistration request = new RequestRegistration(permitCount, tcs, ctr);
                _queue.EnqueueTail(request);
                _queueCount += permitCount;
                Debug.Assert(_queueCount <= _options.QueueLimit);

                return new ValueTask<RateLimitLease>(request.Tcs.Task);
            }
        }

        private bool TryLeaseUnsynchronized(int permitCount, [NotNullWhen(true)] out RateLimitLease? lease)
        {
            ThrowIfDisposed();

            // if permitCount is 0 we want to queue it if there are no available permits
            if (_permitCount >= permitCount && _permitCount != 0)
            {
                if (permitCount == 0)
                {
                    // Edge case where the check before the lock showed 0 available permits but when we got the lock some permits were now available
                    lease = SuccessfulLease;
                    return true;
                }

                // a. if there are no items queued we can lease
                // b. if there are items queued but the processing order is newest first, then we can lease the incoming request since it is the newest
                if (_queueCount == 0 || (_queueCount > 0 && _options.QueueProcessingOrder == QueueProcessingOrder.NewestFirst))
                {
                    _idleSince = null;
                    _permitCount -= permitCount;
                    Debug.Assert(_permitCount >= 0);
                    lease = new ConcurrencyLease(true, this, permitCount);
                    return true;
                }
            }

            lease = null;
            return false;
        }

        private void Release(int releaseCount)
        {
            lock (Lock)
            {
                if (_disposed)
                {
                    return;
                }

                _permitCount += releaseCount;
                Debug.Assert(_permitCount <= _options.PermitLimit);

                while (_queue.Count > 0)
                {
                    RequestRegistration nextPendingRequest =
                        _options.QueueProcessingOrder == QueueProcessingOrder.OldestFirst
                        ? _queue.PeekHead()
                        : _queue.PeekTail();

                    if (_permitCount >= nextPendingRequest.Count)
                    {
                        nextPendingRequest =
                            _options.QueueProcessingOrder == QueueProcessingOrder.OldestFirst
                            ? _queue.DequeueHead()
                            : _queue.DequeueTail();

                        _permitCount -= nextPendingRequest.Count;
                        _queueCount -= nextPendingRequest.Count;
                        Debug.Assert(_permitCount >= 0);

                        ConcurrencyLease lease = nextPendingRequest.Count == 0 ? SuccessfulLease : new ConcurrencyLease(true, this, nextPendingRequest.Count);
                        // Check if request was canceled
                        if (!nextPendingRequest.Tcs.TrySetResult(lease))
                        {
                            // Queued item was canceled so add count back
                            _permitCount += nextPendingRequest.Count;
                            // Updating queue count is handled by the cancellation code
                            _queueCount += nextPendingRequest.Count;
                        }
                        nextPendingRequest.CancellationTokenRegistration.Dispose();
                        Debug.Assert(_queueCount >= 0);
                    }
                    else
                    {
                        break;
                    }
                }

                if (_permitCount == _options.PermitLimit)
                {
                    Debug.Assert(_idleSince is null);
                    Debug.Assert(_queueCount == 0);
                    _idleSince = Stopwatch.GetTimestamp();
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            lock (Lock)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;
                while (_queue.Count > 0)
                {
                    RequestRegistration next = _options.QueueProcessingOrder == QueueProcessingOrder.OldestFirst
                        ? _queue.DequeueHead()
                        : _queue.DequeueTail();
                    next.CancellationTokenRegistration.Dispose();
                    next.Tcs.SetResult(FailedLease);
                }
            }
        }

        protected override ValueTask DisposeAsyncCore()
        {
            Dispose(true);

            return default;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ConcurrencyLimiter));
            }
        }

        private sealed class ConcurrencyLease : RateLimitLease
        {
            private static readonly string[] s_allMetadataNames = new[] { MetadataName.ReasonPhrase.Name };

            private bool _disposed;
            private readonly ConcurrencyLimiter? _limiter;
            private readonly int _count;
            private readonly string? _reason;

            public ConcurrencyLease(bool isAcquired, ConcurrencyLimiter? limiter, int count, string? reason = null)
            {
                IsAcquired = isAcquired;
                _limiter = limiter;
                _count = count;
                _reason = reason;

                // No need to set the limiter if count is 0, Dispose will noop
                Debug.Assert(count == 0 ? limiter is null : true);
            }

            public override bool IsAcquired { get; }

            public override IEnumerable<string> MetadataNames => s_allMetadataNames;

            public override bool TryGetMetadata(string metadataName, out object? metadata)
            {
                if (_reason is not null && metadataName == MetadataName.ReasonPhrase.Name)
                {
                    metadata = _reason;
                    return true;
                }
                metadata = default;
                return false;
            }

            protected override void Dispose(bool disposing)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;

                _limiter?.Release(_count);
            }
        }

        private readonly struct RequestRegistration
        {
            public RequestRegistration(int requestedCount, TaskCompletionSource<RateLimitLease> tcs,
                CancellationTokenRegistration cancellationTokenRegistration)
            {
                Count = requestedCount;
                // Perf: Use AsyncOperation<TResult> instead
                Tcs = tcs;
                CancellationTokenRegistration = cancellationTokenRegistration;
            }

            public int Count { get; }

            public TaskCompletionSource<RateLimitLease> Tcs { get; }

            public CancellationTokenRegistration CancellationTokenRegistration { get; }
        }

        private sealed class CancelQueueState : TaskCompletionSource<RateLimitLease>
        {
            private readonly int _permitCount;
            private readonly ConcurrencyLimiter _limiter;
            private readonly CancellationToken _cancellationToken;

            public CancelQueueState(int permitCount, ConcurrencyLimiter limiter, CancellationToken cancellationToken)
                : base(TaskCreationOptions.RunContinuationsAsynchronously)
            {
                _permitCount = permitCount;
                _limiter = limiter;
                _cancellationToken = cancellationToken;
            }

            public new bool TrySetCanceled()
            {
                if (TrySetCanceled(_cancellationToken))
                {
                    lock (_limiter.Lock)
                    {
                        _limiter._queueCount -= _permitCount;
                    }
                    return true;
                }
                return false;
            }
        }
    }
}
