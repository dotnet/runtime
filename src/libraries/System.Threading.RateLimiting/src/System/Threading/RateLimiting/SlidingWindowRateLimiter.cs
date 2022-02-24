// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace System.Threading.RateLimiting
{
    /// <summary>
    /// <see cref="RateLimiter"/> implementation that replenishes permit counters periodically instead of via a release mechanism.
    /// </summary>
    public sealed class SlidingWindowRateLimiter : ReplenishingRateLimiter
    {
        private int _requestCount;
        private int _queueCount;
        private int[] _requestsPerSegment;
        private int _currentSegmentIndex;
        private long _lastReplenishmentTick;
        private long? _idleSince;
        private bool _disposed;

        private readonly Timer? _renewTimer;
        private readonly SlidingWindowRateLimiterOptions _options;
        private readonly Deque<RequestRegistration> _queue = new Deque<RequestRegistration>();

        // Use the queue as the lock field so we don't need to allocate another object for a lock and have another field in the object
        private object Lock => _queue;

        private static readonly RateLimitLease SuccessfulLease = new SlidingWindowLease(true, null);
        private static readonly RateLimitLease FailedLease = new SlidingWindowLease(false, null);
        private static readonly double TickFrequency = (double)TimeSpan.TicksPerSecond / Stopwatch.Frequency;

        /// <inheritdoc />
        public override TimeSpan? IdleDuration => _idleSince is null ? null : new TimeSpan((long)((Stopwatch.GetTimestamp() - _idleSince) * TickFrequency));

        /// <inheritdoc />
        public override bool IsAutoReplenishing => _options.AutoReplenishment;

        /// <inheritdoc />
        public override TimeSpan ReplenishmentPeriod => _options.Window;

        /// <summary>
        /// Initializes the <see cref="SlidingWindowRateLimiter"/>.
        /// </summary>
        /// <param name="options">Options to specify the behavior of the <see cref="SlidingWindowRateLimiter"/>.</param>
        public SlidingWindowRateLimiter(SlidingWindowRateLimiterOptions options!!)
        {
            _options = options;
            _requestCount = options.PermitLimit;
            _requestsPerSegment = new int[options.SegmentsPerWindow];
            TimeSpan _windowSegmentInterval = _options.Window / _options.SegmentsPerWindow;
            _currentSegmentIndex = 0;

            if (_options.AutoReplenishment)
            {
                _renewTimer = new Timer(Replenish, this, _windowSegmentInterval, _windowSegmentInterval);
            }
        }

        /// <inheritdoc/>
        public override int GetAvailablePermits() => _options.PermitLimit - _requestCount;

        /// <inheritdoc/>
        protected override RateLimitLease AcquireCore(int requestCount)
        {
            // These amounts of resources can never be acquired
            if (requestCount > _options.PermitLimit)
            {
                throw new ArgumentOutOfRangeException(nameof(requestCount), requestCount, SR.Format(SR.PermitLimitExceeded, requestCount, _options.PermitLimit));
            }

            // Return SuccessfulLease or FailedLease depending to indicate limiter state
            if (requestCount == 0 && !_disposed)
            {
                if (_requestCount > 0)
                {
                    return SuccessfulLease;
                }

                return FailedLease;
            }

            lock (Lock)
            {
                if (TryLeaseUnsynchronized(requestCount, out RateLimitLease? lease))
                {
                    return lease;
                }

                return FailedLease;
            }
        }

        /// <inheritdoc/>
        protected override ValueTask<RateLimitLease> WaitAsyncCore(int requestCount, CancellationToken cancellationToken = default)
        {
            // These amounts of resources can never be acquired
            if (requestCount > _options.PermitLimit)
            {
                throw new ArgumentOutOfRangeException(nameof(requestCount), requestCount, SR.Format(SR.PermitLimitExceeded, requestCount, _options.PermitLimit));
            }

            ThrowIfDisposed();

            // Return SuccessfulAcquisition if resources are available
            if (requestCount == 0 && _requestCount > 0)
            {
                return new ValueTask<RateLimitLease>(SuccessfulLease);
            }

            lock (Lock)
            {
                if (TryLeaseUnsynchronized(requestCount, out RateLimitLease? lease))
                {
                    return new ValueTask<RateLimitLease>(lease);
                }

                // Avoid integer overflow by using subtraction instead of addition
                Debug.Assert(_options.QueueLimit >= _queueCount);
                if (_options.QueueLimit - _queueCount < requestCount)
                {
                    if (_options.QueueProcessingOrder == QueueProcessingOrder.NewestFirst && requestCount <= _options.QueueLimit)
                    {
                        // remove oldest items from queue until there is space for the newest acquisition request
                        do
                        {
                            RequestRegistration oldestRequest = _queue.DequeueHead();
                            _queueCount -= oldestRequest.Count;
                            Debug.Assert(_queueCount >= 0);
                            oldestRequest.Tcs.TrySetResult(FailedLease);
                        }
                        while (_options.QueueLimit - _queueCount < requestCount);
                    }
                    else
                    {
                        // Don't queue if queue limit reached and QueueProcessingOrder is OldestFirst
                        return new ValueTask<RateLimitLease>(FailedLease);
                    }
                }

                CancelQueueState tcs = new CancelQueueState(requestCount, this, cancellationToken);
                CancellationTokenRegistration ctr = default;
                if (cancellationToken.CanBeCanceled)
                {
                    ctr = cancellationToken.Register(static obj =>
                    {
                        ((CancelQueueState)obj!).TrySetCanceled();
                    }, tcs);
                }

                RequestRegistration registration = new RequestRegistration(requestCount, tcs, ctr);
                _queue.EnqueueTail(registration);
                _queueCount += requestCount;
                Debug.Assert(_queueCount <= _options.QueueLimit);

                return new ValueTask<RateLimitLease>(registration.Tcs.Task);
            }
        }

        private bool TryLeaseUnsynchronized(int requestCount, [NotNullWhen(true)] out RateLimitLease? lease)
        {
            ThrowIfDisposed();

            // if requestCount is 0 we want to queue it if there are no available permits
            if (_requestCount >= requestCount && _requestCount != 0)
            {
                if (requestCount == 0)
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
                    _requestsPerSegment[_currentSegmentIndex] += requestCount;
                    _requestCount -= requestCount;
                    Debug.Assert(_requestCount >= 0);
                    lease = SuccessfulLease;
                    return true;
                }
            }

            lease = null;
            return false;
        }

        /// <summary>
        /// Attempts to replenish request counters in a window.
        /// </summary>
        /// <returns>
        /// False if <see cref="SlidingWindowRateLimiterOptions.AutoReplenishment"/> is enabled, otherwise true.
        /// Does not reflect if permits were replenished.
        /// </returns>
        public override bool TryReplenish()
        {
            if (_options.AutoReplenishment)
            {
                return false;
            }
            Replenish(this);
            return true;
        }

        private static void Replenish(object? state)
        {
            SlidingWindowRateLimiter limiter = (state as SlidingWindowRateLimiter)!;
            Debug.Assert(limiter is not null);

            // Use Environment.TickCount instead of DateTime.UtcNow to avoid issues on systems where the clock can change
            long nowTicks = Stopwatch.GetTimestamp();
            limiter!.ReplenishInternal(nowTicks);
        }

        // Used in tests that test behavior with specific time intervals
        private void ReplenishInternal(long nowTicks)
        {
            TimeSpan _windowSegmentInterval = _options.Window / _options.SegmentsPerWindow;

            // method is re-entrant (from Timer), lock to avoid multiple simultaneous replenishes
            lock (Lock)
            {
                if (_disposed)
                {
                    return;
                }

                if ((long)((nowTicks - _lastReplenishmentTick) * TickFrequency) < _windowSegmentInterval.Ticks)
                {
                    return;
                }

                _lastReplenishmentTick = nowTicks + _windowSegmentInterval.Ticks;

                _currentSegmentIndex = (_currentSegmentIndex + 1) % _options.SegmentsPerWindow;
                int _oldSegmentRequestCount = _requestsPerSegment[_currentSegmentIndex];
                _requestsPerSegment[_currentSegmentIndex] = 0;

                if (_oldSegmentRequestCount == 0)
                {
                    return;
                }

                // Process queued requests
                _requestCount += _oldSegmentRequestCount;
                Debug.Assert(_requestCount <= _options.PermitLimit);
                while (_queue.Count > 0)
                {
                    RequestRegistration nextPendingRequest =
                          _options.QueueProcessingOrder == QueueProcessingOrder.OldestFirst
                          ? _queue.PeekHead()
                          : _queue.PeekTail();

                    if (_requestCount >= nextPendingRequest.Count)
                    {
                        // Request can be fulfilled
                        nextPendingRequest =
                            _options.QueueProcessingOrder == QueueProcessingOrder.OldestFirst
                            ? _queue.DequeueHead()
                            : _queue.DequeueTail();

                        _queueCount -= nextPendingRequest.Count;

                        _requestCount -= nextPendingRequest.Count;
                        _requestsPerSegment[_currentSegmentIndex] += _requestCount;
                        Debug.Assert(_requestCount >= 0);

                        if (!nextPendingRequest.Tcs.TrySetResult(SuccessfulLease))
                        {
                            // Queued item was canceled so add count back
                            _requestCount += nextPendingRequest.Count;
                            _requestsPerSegment[_currentSegmentIndex] += _requestCount;
                            // Updating queue count is handled by the cancellation code
                            _queueCount += nextPendingRequest.Count;
                        }
                        nextPendingRequest.CancellationTokenRegistration.Dispose();
                        Debug.Assert(_queueCount >= 0);
                    }
                    else
                    {
                        // Request cannot be fulfilled
                        break;
                    }
                }

                if (_requestCount == _options.PermitLimit)
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
                _renewTimer?.Dispose();
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
                throw new ObjectDisposedException(nameof(SlidingWindowRateLimiter));
            }
        }

        private sealed class SlidingWindowLease : RateLimitLease
        {
            private static readonly string[] s_allMetadataNames = new[] { MetadataName.RetryAfter.Name };

            private readonly TimeSpan? _retryAfter;

            public SlidingWindowLease(bool isAcquired, TimeSpan? retryAfter)
            {
                IsAcquired = isAcquired;
                _retryAfter = retryAfter;
            }

            public override bool IsAcquired { get; }

            public override IEnumerable<string> MetadataNames => s_allMetadataNames;

            public override bool TryGetMetadata(string metadataName, out object? metadata)
            {
                if (metadataName == MetadataName.RetryAfter.Name && _retryAfter.HasValue)
                {
                    metadata = _retryAfter.Value;
                    return true;
                }

                metadata = default;
                return false;
            }
        }

        private readonly struct RequestRegistration
        {
            public RequestRegistration(int requestCount, TaskCompletionSource<RateLimitLease> tcs, CancellationTokenRegistration cancellationTokenRegistration)
            {
                Count = requestCount;
                // Use VoidAsyncOperationWithData<T> instead
                Tcs = tcs;
                CancellationTokenRegistration = cancellationTokenRegistration;
            }

            public int Count { get; }

            public TaskCompletionSource<RateLimitLease> Tcs { get; }

            public CancellationTokenRegistration CancellationTokenRegistration { get; }
        }

        private sealed class CancelQueueState : TaskCompletionSource<RateLimitLease>
        {
            private readonly int _requestCount;
            private readonly SlidingWindowRateLimiter _limiter;
            private readonly CancellationToken _cancellationToken;

            public CancelQueueState(int requestCount, SlidingWindowRateLimiter limiter, CancellationToken cancellationToken)
                : base(TaskCreationOptions.RunContinuationsAsynchronously)
            {
                _requestCount = requestCount;
                _limiter = limiter;
                _cancellationToken = cancellationToken;
            }

            public new bool TrySetCanceled()
            {
                if (TrySetCanceled(_cancellationToken))
                {
                    lock (_limiter.Lock)
                    {
                        _limiter._queueCount -= _requestCount;
                    }
                    return true;
                }
                return false;
            }
        }
    }
}
