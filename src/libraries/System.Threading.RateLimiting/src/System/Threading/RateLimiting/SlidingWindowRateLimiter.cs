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
        private int _permitCount;
        private int _queueCount;
        private readonly int[] _requestsPerSegment;
        private int _currentSegmentIndex;
        private long _lastReplenishmentTick;
        private long? _idleSince;
        private bool _disposed;

        private long _failedLeasesCount;
        private long _successfulLeasesCount;

        private readonly Timer? _renewTimer;
        private readonly SlidingWindowRateLimiterOptions _options;
        private readonly TimeSpan _replenishmentPeriod;
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
        public override TimeSpan ReplenishmentPeriod => _replenishmentPeriod;

        /// <summary>
        /// Initializes the <see cref="SlidingWindowRateLimiter"/>.
        /// </summary>
        /// <param name="options">Options to specify the behavior of the <see cref="SlidingWindowRateLimiter"/>.</param>
        public SlidingWindowRateLimiter(SlidingWindowRateLimiterOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (options.PermitLimit <= 0)
            {
                throw new ArgumentException(SR.Format(SR.ShouldBeGreaterThan0, nameof(options.PermitLimit)), nameof(options));
            }
            if (options.SegmentsPerWindow <= 0)
            {
                throw new ArgumentException(SR.Format(SR.ShouldBeGreaterThan0, nameof(options.SegmentsPerWindow)), nameof(options));
            }
            if (options.QueueLimit < 0)
            {
                throw new ArgumentException(SR.Format(SR.ShouldBeGreaterThanOrEqual0, nameof(options.QueueLimit)), nameof(options));
            }
            if (options.Window <= TimeSpan.Zero)
            {
                throw new ArgumentException(SR.Format(SR.ShouldBeGreaterThanTimeSpan0, nameof(options.Window)), nameof(options));
            }

            _options = new SlidingWindowRateLimiterOptions
            {
                PermitLimit = options.PermitLimit,
                QueueProcessingOrder = options.QueueProcessingOrder,
                QueueLimit = options.QueueLimit,
                Window = options.Window,
                SegmentsPerWindow = options.SegmentsPerWindow,
                AutoReplenishment = options.AutoReplenishment
            };

            _permitCount = options.PermitLimit;
            _replenishmentPeriod = new TimeSpan(_options.Window.Ticks / _options.SegmentsPerWindow);

            // _requestsPerSegment holds the no. of acquired requests in each window segment
            _requestsPerSegment = new int[options.SegmentsPerWindow];
            _currentSegmentIndex = 0;

            _idleSince = _lastReplenishmentTick = Stopwatch.GetTimestamp();

            if (_options.AutoReplenishment)
            {
                _renewTimer = new Timer(Replenish, this, ReplenishmentPeriod, ReplenishmentPeriod);
            }
        }

        /// <inheritdoc/>
        public override RateLimiterStatistics? GetStatistics()
        {
            ThrowIfDisposed();
            return new RateLimiterStatistics()
            {
                CurrentAvailablePermits = _permitCount,
                CurrentQueuedCount = _queueCount,
                TotalFailedLeases = Interlocked.Read(ref _failedLeasesCount),
                TotalSuccessfulLeases = Interlocked.Read(ref _successfulLeasesCount),
            };
        }

        /// <inheritdoc/>
        protected override RateLimitLease AttemptAcquireCore(int permitCount)
        {
            // These amounts of resources can never be acquired
            if (permitCount > _options.PermitLimit)
            {
                throw new ArgumentOutOfRangeException(nameof(permitCount), permitCount, SR.Format(SR.PermitLimitExceeded, permitCount, _options.PermitLimit));
            }

            // Return SuccessfulLease or FailedLease depending to indicate limiter state
            if (permitCount == 0 && !_disposed)
            {
                if (_permitCount > 0)
                {
                    Interlocked.Increment(ref _successfulLeasesCount);
                    return SuccessfulLease;
                }

                Interlocked.Increment(ref _failedLeasesCount);
                return FailedLease;
            }

            lock (Lock)
            {
                if (TryLeaseUnsynchronized(permitCount, out RateLimitLease? lease))
                {
                    return lease;
                }

                // TODO: Acquire additional metadata during a failed lease decision
                Interlocked.Increment(ref _failedLeasesCount);
                return FailedLease;
            }
        }

        /// <inheritdoc/>
        protected override ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken = default)
        {
            // These amounts of resources can never be acquired
            if (permitCount > _options.PermitLimit)
            {
                throw new ArgumentOutOfRangeException(nameof(permitCount), permitCount, SR.Format(SR.PermitLimitExceeded, permitCount, _options.PermitLimit));
            }

            ThrowIfDisposed();

            // Return SuccessfulAcquisition if resources are available
            if (permitCount == 0 && _permitCount > 0)
            {
                Interlocked.Increment(ref _successfulLeasesCount);
                return new ValueTask<RateLimitLease>(SuccessfulLease);
            }

            using var disposer = default(RequestRegistration.Disposer);
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
                        // Remove oldest items from queue until there is space for the newest acquisition request
                        do
                        {
                            RequestRegistration oldestRequest = _queue.DequeueHead();
                            _queueCount -= oldestRequest.Count;
                            Debug.Assert(_queueCount >= 0);
                            if (!oldestRequest.TrySetResult(FailedLease))
                            {
                                if (!oldestRequest.QueueCountModified)
                                {
                                    // We already updated the queue count, the Cancel code is about to run or running and waiting on our lock,
                                    // tell Cancel not to do anything
                                    oldestRequest.QueueCountModified = true;
                                }
                                else
                                {
                                    // Updating queue count was handled by the cancellation code, don't double count
                                    _queueCount += oldestRequest.Count;
                                }
                            }
                            else
                            {
                                Interlocked.Increment(ref _failedLeasesCount);
                            }
                            disposer.Add(oldestRequest);
                        }
                        while (_options.QueueLimit - _queueCount < permitCount);
                    }
                    else
                    {
                        Interlocked.Increment(ref _failedLeasesCount);
                        // Don't queue if queue limit reached and QueueProcessingOrder is OldestFirst
                        return new ValueTask<RateLimitLease>(FailedLease);
                    }
                }

                var registration = new RequestRegistration(permitCount, this, cancellationToken);
                _queue.EnqueueTail(registration);
                _queueCount += permitCount;
                Debug.Assert(_queueCount <= _options.QueueLimit);

                return new ValueTask<RateLimitLease>(registration.Task);
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
                    Interlocked.Increment(ref _successfulLeasesCount);
                    // Edge case where the check before the lock showed 0 available permits but when we got the lock some permits were now available
                    lease = SuccessfulLease;
                    return true;
                }

                // a. If there are no items queued we can lease
                // b. If there are items queued but the processing order is NewestFirst, then we can lease the incoming request since it is the newest
                if (_queueCount == 0 || (_queueCount > 0 && _options.QueueProcessingOrder == QueueProcessingOrder.NewestFirst))
                {
                    _idleSince = null;
                    _requestsPerSegment[_currentSegmentIndex] += permitCount;
                    _permitCount -= permitCount;
                    Debug.Assert(_permitCount >= 0);
                    Interlocked.Increment(ref _successfulLeasesCount);
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

            // Replenish call will slide the window one segment at a time
            Replenish(this);
            return true;
        }

        private static void Replenish(object? state)
        {
            SlidingWindowRateLimiter limiter = (state as SlidingWindowRateLimiter)!;
            Debug.Assert(limiter is not null);

            // Use Stopwatch instead of DateTime.UtcNow to avoid issues on systems where the clock can change
            long nowTicks = Stopwatch.GetTimestamp();
            limiter!.ReplenishInternal(nowTicks);
        }

        // Used in tests that test behavior with specific time intervals
        private void ReplenishInternal(long nowTicks)
        {
            using var disposer = default(RequestRegistration.Disposer);

            // Method is re-entrant (from Timer), lock to avoid multiple simultaneous replenishes
            lock (Lock)
            {
                if (_disposed)
                {
                    return;
                }

                if (((nowTicks - _lastReplenishmentTick) * TickFrequency) < ReplenishmentPeriod.Ticks && !_options.AutoReplenishment)
                {
                    return;
                }

                _lastReplenishmentTick = nowTicks;

                // Increment the current segment index while move the window
                // We need to know the no. of requests that were acquired in a segment previously to ensure that we don't acquire more than the permit limit.
                _currentSegmentIndex = (_currentSegmentIndex + 1) % _options.SegmentsPerWindow;
                int oldSegmentPermitCount = _requestsPerSegment[_currentSegmentIndex];
                _requestsPerSegment[_currentSegmentIndex] = 0;

                if (oldSegmentPermitCount == 0)
                {
                    return;
                }

                _permitCount += oldSegmentPermitCount;
                Debug.Assert(_permitCount <= _options.PermitLimit);

                // Process queued requests
                while (_queue.Count > 0)
                {
                    RequestRegistration nextPendingRequest =
                          _options.QueueProcessingOrder == QueueProcessingOrder.OldestFirst
                          ? _queue.PeekHead()
                          : _queue.PeekTail();

                    // Request was handled already, either via cancellation or being kicked from the queue due to a newer request being queued.
                    // We just need to remove the item and let the next queued item be considered for completion.
                    if (nextPendingRequest.Task.IsCompleted)
                    {
                        nextPendingRequest =
                            _options.QueueProcessingOrder == QueueProcessingOrder.OldestFirst
                            ? _queue.DequeueHead()
                            : _queue.DequeueTail();
                        disposer.Add(nextPendingRequest);
                    }
                    // If we have enough permits after replenishing to serve the queued requests
                    else if (_permitCount >= nextPendingRequest.Count)
                    {
                        // Request can be fulfilled
                        nextPendingRequest =
                            _options.QueueProcessingOrder == QueueProcessingOrder.OldestFirst
                            ? _queue.DequeueHead()
                            : _queue.DequeueTail();

                        _queueCount -= nextPendingRequest.Count;
                        _permitCount -= nextPendingRequest.Count;
                        _requestsPerSegment[_currentSegmentIndex] += nextPendingRequest.Count;
                        Debug.Assert(_permitCount >= 0);

                        if (!nextPendingRequest.TrySetResult(SuccessfulLease))
                        {
                            // Queued item was canceled so add count back, permits weren't acquired
                            _permitCount += nextPendingRequest.Count;
                            _requestsPerSegment[_currentSegmentIndex] -= nextPendingRequest.Count;
                            if (!nextPendingRequest.QueueCountModified)
                            {
                                // We already updated the queue count, the Cancel code is about to run or running and waiting on our lock,
                                // tell Cancel not to do anything
                                nextPendingRequest.QueueCountModified = true;
                            }
                            else
                            {
                                // Updating queue count was handled by the cancellation code, don't double count
                                _queueCount += nextPendingRequest.Count;
                            }
                        }
                        else
                        {
                            Interlocked.Increment(ref _successfulLeasesCount);
                        }
                        disposer.Add(nextPendingRequest);
                        Debug.Assert(_queueCount >= 0);
                    }
                    else
                    {
                        // Request cannot be fulfilled
                        break;
                    }
                }

                if (_permitCount == _options.PermitLimit)
                {
                    Debug.Assert(_idleSince is null);
                    _idleSince = Stopwatch.GetTimestamp();
                }
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            using var disposer = default(RequestRegistration.Disposer);
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
                    disposer.Add(next);
                    next.TrySetResult(FailedLease);
                }
            }
        }

        /// <inheritdoc />
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

        private sealed class RequestRegistration : TaskCompletionSource<RateLimitLease>
        {
            private readonly CancellationToken _cancellationToken;
            private CancellationTokenRegistration _cancellationTokenRegistration;

            // Update under the limiter lock and only if the queue count was updated by the calling code
            public bool QueueCountModified { get; set; }

            // this field is used only by the disposal mechanics and never shared between threads
            private RequestRegistration? _next;

            public RequestRegistration(int permitCount, SlidingWindowRateLimiter limiter, CancellationToken cancellationToken)
                : base(limiter, TaskCreationOptions.RunContinuationsAsynchronously)
            {
                Count = permitCount;
                _cancellationToken = cancellationToken;

                // RequestRegistration objects are created while the limiter lock is held
                // if cancellationToken fires before or while the lock is held, UnsafeRegister
                // is going to invoke the callback synchronously, but this does not create
                // a deadlock because lock are reentrant
                if (cancellationToken.CanBeCanceled)
#if NET || NETSTANDARD2_1_OR_GREATER
                    _cancellationTokenRegistration = cancellationToken.UnsafeRegister(Cancel, this);
#else
                    _cancellationTokenRegistration = cancellationToken.Register(Cancel, this);
#endif
            }

            public int Count { get; }

            private static void Cancel(object? state)
            {
                if (state is RequestRegistration registration && registration.TrySetCanceled(registration._cancellationToken))
                {
                    var limiter = (SlidingWindowRateLimiter)registration.Task.AsyncState!;
                    lock (limiter.Lock)
                    {
                        // Queuing and replenishing code might modify the _queueCount, since there is no guarantee of when the cancellation
                        // code runs and we only want to update the _queueCount once, we set a bool (under a lock) so either method
                        // can update the count and not double count.
                        if (!registration.QueueCountModified)
                        {
                            limiter._queueCount -= registration.Count;
                            registration.QueueCountModified = true;
                        }
                    }
                }
            }

            /// <summary>
            /// Collects registrations to dispose outside the limiter lock to avoid deadlock.
            /// </summary>
            public struct Disposer : IDisposable
            {
                private RequestRegistration? _next;

                public void Add(RequestRegistration request)
                {
                    request._next = _next;
                    _next = request;
                }

                public void Dispose()
                {
                    for (var current = _next; current is not null; current = current._next)
                    {
                        current._cancellationTokenRegistration.Dispose();
                    }

                    _next = null;
                }
            }
        }
    }
}
