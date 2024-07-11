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

        private long _failedLeasesCount;
        private long _successfulLeasesCount;

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
        public ConcurrencyLimiter(ConcurrencyLimiterOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (options.PermitLimit <= 0)
            {
                throw new ArgumentException(SR.Format(SR.ShouldBeGreaterThan0, nameof(options.PermitLimit)), nameof(options));
            }
            if (options.QueueLimit < 0)
            {
                throw new ArgumentException(SR.Format(SR.ShouldBeGreaterThanOrEqual0, nameof(options.QueueLimit)), nameof(options));
            }

            _options = new ConcurrencyLimiterOptions
            {
                PermitLimit = options.PermitLimit,
                QueueProcessingOrder = options.QueueProcessingOrder,
                QueueLimit = options.QueueLimit
            };

            _permitCount = _options.PermitLimit;
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

            ThrowIfDisposed();

            // Return SuccessfulLease or FailedLease to indicate limiter state
            if (permitCount == 0)
            {
                if (_permitCount > 0)
                {
                    Interlocked.Increment(ref _successfulLeasesCount);
                    return SuccessfulLease;
                }
                Interlocked.Increment(ref _failedLeasesCount);
                return FailedLease;
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

            Interlocked.Increment(ref _failedLeasesCount);
            return FailedLease;
        }

        /// <inheritdoc/>
        protected override ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken = default)
        {
            // These amounts of resources can never be acquired
            if (permitCount > _options.PermitLimit)
            {
                throw new ArgumentOutOfRangeException(nameof(permitCount), permitCount, SR.Format(SR.PermitLimitExceeded, permitCount, _options.PermitLimit));
            }

            // Return SuccessfulLease if requestedCount is 0 and resources are available
            if (permitCount == 0 && _permitCount > 0 && !_disposed)
            {
                Interlocked.Increment(ref _successfulLeasesCount);
                return new ValueTask<RateLimitLease>(SuccessfulLease);
            }

            using var disposer = default(RequestRegistration.Disposer);

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
                        return new ValueTask<RateLimitLease>(QueueLimitLease);
                    }
                }

                var request = new RequestRegistration(permitCount, this, cancellationToken);
                _queue.EnqueueTail(request);
                _queueCount += permitCount;
                Debug.Assert(_queueCount <= _options.QueueLimit);

                return new ValueTask<RateLimitLease>(request.Task);
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

                // a. if there are no items queued we can lease
                // b. if there are items queued but the processing order is newest first, then we can lease the incoming request since it is the newest
                if (_queueCount == 0 || (_queueCount > 0 && _options.QueueProcessingOrder == QueueProcessingOrder.NewestFirst))
                {
                    _idleSince = null;
                    _permitCount -= permitCount;
                    Debug.Assert(_permitCount >= 0);
                    Interlocked.Increment(ref _successfulLeasesCount);
                    lease = new ConcurrencyLease(true, this, permitCount);
                    return true;
                }
            }

            lease = null;
            return false;
        }

#if DEBUG
        // for unit testing
        internal event Action? ReleasePreHook;
        internal event Action? ReleasePostHook;
#endif

        private void Release(int releaseCount)
        {
            using var disposer = default(RequestRegistration.Disposer);
            lock (Lock)
            {
                if (_disposed)
                {
                    return;
                }

                _permitCount += releaseCount;
                Debug.Assert(_permitCount <= _options.PermitLimit);

#if DEBUG
                ReleasePreHook?.Invoke();
#endif

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
                        continue;
                    }

#if DEBUG
                    ReleasePostHook?.Invoke();
#endif

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
                        if (!nextPendingRequest.TrySetResult(lease))
                        {
                            // Queued item was canceled so add count back, permits weren't acquired
                            _permitCount += nextPendingRequest.Count;
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        private sealed class RequestRegistration : TaskCompletionSource<RateLimitLease>
        {
            private readonly CancellationToken _cancellationToken;
            private CancellationTokenRegistration _cancellationTokenRegistration;

            // Update under the limiter lock and only if the queue count was updated by the calling code
            public bool QueueCountModified { get; set; }

            // this field is used only by the disposal mechanics and never shared between threads
            private RequestRegistration? _next;

            public RequestRegistration(int permitCount, ConcurrencyLimiter limiter, CancellationToken cancellationToken)
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
                    var limiter = (ConcurrencyLimiter)registration.Task.AsyncState!;
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
