// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace System.Threading.RateLimiting
{
    /// <summary>
    /// <see cref="RateLimiter"/> implementation that refreshes allowed permits in a window periodically.
    /// </summary>
    public sealed class FixedWindowRateLimiter : ReplenishingRateLimiter
    {
        private int _requestCount;
        private int _queueCount;
        private long _lastReplenishmentTick;
        private long? _idleSince;
        private bool _disposed;

        private readonly Timer? _renewTimer;
        private readonly FixedWindowRateLimiterOptions _options;
        private readonly Deque<RequestRegistration> _queue = new Deque<RequestRegistration>();

        private object Lock => _queue;

        private static readonly RateLimitLease SuccessfulLease = new FixedWindowLease(true, null);
        private static readonly RateLimitLease FailedLease = new FixedWindowLease(false, null);
        private static readonly double TickFrequency = (double)TimeSpan.TicksPerSecond / Stopwatch.Frequency;

        /// <inheritdoc />
        public override TimeSpan? IdleDuration => _idleSince is null ? null : new TimeSpan((long)((Stopwatch.GetTimestamp() - _idleSince) * TickFrequency));

        /// <inheritdoc />
        public override bool IsAutoReplenishing => _options.AutoReplenishment;

        /// <inheritdoc />
        public override TimeSpan ReplenishmentPeriod => _options.Window;

        /// <summary>
        /// Initializes the <see cref="FixedWindowRateLimiter"/>.
        /// </summary>
        /// <param name="options">Options to specify the behavior of the <see cref="FixedWindowRateLimiter"/>.</param>
        public FixedWindowRateLimiter(FixedWindowRateLimiterOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _requestCount = options.PermitLimit;

            _idleSince = _lastReplenishmentTick = Stopwatch.GetTimestamp();

            if (_options.AutoReplenishment)
            {
                _renewTimer = new Timer(Replenish, this, _options.Window, _options.Window);
            }
        }

        /// <inheritdoc/>
        public override int GetAvailablePermits() => _requestCount;

        /// <inheritdoc/>
        protected override RateLimitLease AcquireCore(int requestCount)
        {
            // These amounts of resources can never be acquired
            // Raises a PermitLimitExceeded ArgumentOutOFRangeException
            if (requestCount > _options.PermitLimit)
            {
                throw new ArgumentOutOfRangeException(nameof(requestCount), requestCount, SR.Format(SR.PermitLimitExceeded, requestCount, _options.PermitLimit));
            }

            // Return SuccessfulLease or FailedLease depending to indicate limiter state
            if (requestCount == 0 && !_disposed)
            {
                // Check if the requests are permitted in a window
                // Requests will be allowed if the total served request is less than the max allowed requests (permit limit).
                if (_requestCount > 0)
                {
                    return SuccessfulLease;
                }

                return CreateFailedWindowLease(requestCount);
            }

            lock (Lock)
            {
                if (TryLeaseUnsynchronized(requestCount, out RateLimitLease? lease))
                {
                    return lease;
                }

                return CreateFailedWindowLease(requestCount);
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

            // Return SuccessfulAcquisition if requestCount is 0 and resources are available
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
                        return new ValueTask<RateLimitLease>(CreateFailedWindowLease(requestCount));
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

        private RateLimitLease CreateFailedWindowLease(int requestCount)
        {
            int replenishAmount = requestCount - _requestCount + _queueCount;
            // can't have 0 replenish window, that would mean it should be a successful lease
            int replenishWindow = Math.Max(replenishAmount / _options.PermitLimit, 1);

            return new FixedWindowLease(false, TimeSpan.FromTicks(_options.Window.Ticks * replenishWindow));
        }

        private bool TryLeaseUnsynchronized(int requestCount, [NotNullWhen(true)] out RateLimitLease? lease)
        {
            ThrowIfDisposed();

            // if permitCount is 0 we want to queue it if there are no available permits
            if (_requestCount >= requestCount && _requestCount != 0)
            {
                if (requestCount == 0)
                {
                    // Edge case where the check before the lock showed 0 available permit counters but when we got the lock, some permits were now available
                    lease = SuccessfulLease;
                    return true;
                }

                // a. If there are no items queued we can lease
                // b. If there are items queued but the processing order is newest first, then we can lease the incoming request since it is the newest
                if (_queueCount == 0 || (_queueCount > 0 && _options.QueueProcessingOrder == QueueProcessingOrder.NewestFirst))
                {
                    _idleSince = null;
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
        /// Attempts to replenish request counters in the window.
        /// </summary>
        /// <returns>
        /// False if <see cref="FixedWindowRateLimiterOptions.AutoReplenishment"/> is enabled, otherwise true.
        /// Does not reflect if counters were replenished.
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
            FixedWindowRateLimiter limiter = (state as FixedWindowRateLimiter)!;
            Debug.Assert(limiter is not null);

            // Use Stopwatch instead of DateTime.UtcNow to avoid issues on systems where the clock can change
            long nowTicks = Stopwatch.GetTimestamp();
            limiter!.ReplenishInternal(nowTicks);
        }

        // Used in tests that test behavior with specific time intervals
        private void ReplenishInternal(long nowTicks)
        {
            // Method is re-entrant (from Timer), lock to avoid multiple simultaneous replenishes
            lock (Lock)
            {
                if (_disposed)
                {
                    return;
                }

                if ((long)((nowTicks - _lastReplenishmentTick) * TickFrequency) < _options.Window.Ticks)
                {
                    return;
                }

                _lastReplenishmentTick = nowTicks;

                int availableRequestCounters = _requestCount;
                int maxPermits = _options.PermitLimit;
                int resourcesToAdd;

                if (availableRequestCounters < maxPermits)
                {
                    resourcesToAdd = maxPermits - availableRequestCounters;
                }
                else
                {
                    // All counters available, nothing to do
                    return;
                }

                _requestCount += resourcesToAdd;
                Debug.Assert(_requestCount == _options.PermitLimit);

                // Process queued requests
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
                        Debug.Assert(_requestCount >= 0);

                        if (!nextPendingRequest.Tcs.TrySetResult(SuccessfulLease))
                        {
                            // Queued item was canceled so add count back
                            _requestCount += nextPendingRequest.Count;
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
                throw new ObjectDisposedException(nameof(FixedWindowRateLimiter));
            }
        }

        private sealed class FixedWindowLease : RateLimitLease
        {
            private static readonly string[] s_allMetadataNames = new[] { MetadataName.RetryAfter.Name };

            private readonly TimeSpan? _retryAfter;

            public FixedWindowLease(bool isAcquired, TimeSpan? retryAfter)
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
            private readonly FixedWindowRateLimiter _limiter;
            private readonly CancellationToken _cancellationToken;

            public CancelQueueState(int requestCount, FixedWindowRateLimiter limiter, CancellationToken cancellationToken)
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
