// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace System.Threading.RateLimiting
{
    /// <summary>
    /// <see cref="RateLimiter"/> implementation that replenishes tokens periodically instead of via a release mechanism.
    /// </summary>
    public sealed class TokenBucketRateLimiter : ReplenishingRateLimiter
    {
        private double _tokenCount;
        private int _queueCount;
        private long _lastReplenishmentTick;
        private long? _idleSince;
        private bool _disposed;

        private long _failedLeasesCount;
        private long _successfulLeasesCount;

        private readonly double _fillRate;
        private readonly Timer? _renewTimer;
        private readonly TokenBucketRateLimiterOptions _options;
        private readonly Deque<RequestRegistration> _queue = new Deque<RequestRegistration>();

        // Use the queue as the lock field so we don't need to allocate another object for a lock and have another field in the object
        private object Lock => _queue;

        private static readonly RateLimitLease SuccessfulLease = new TokenBucketLease(true, null);
        private static readonly RateLimitLease FailedLease = new TokenBucketLease(false, null);
        private static readonly double TickFrequency = (double)TimeSpan.TicksPerSecond / Stopwatch.Frequency;

        /// <inheritdoc />
        public override TimeSpan? IdleDuration => _idleSince is null ? null : new TimeSpan((long)((Stopwatch.GetTimestamp() - _idleSince) * TickFrequency));

        /// <inheritdoc />
        public override bool IsAutoReplenishing => _options.AutoReplenishment;

        /// <inheritdoc />
        public override TimeSpan ReplenishmentPeriod => _options.ReplenishmentPeriod;

        /// <summary>
        /// Initializes the <see cref="TokenBucketRateLimiter"/>.
        /// </summary>
        /// <param name="options">Options to specify the behavior of the <see cref="TokenBucketRateLimiter"/>.</param>
        public TokenBucketRateLimiter(TokenBucketRateLimiterOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (options.TokenLimit <= 0)
            {
                throw new ArgumentException(SR.Format(SR.ShouldBeGreaterThan0, nameof(options.TokenLimit)), nameof(options));
            }
            if (options.TokensPerPeriod <= 0)
            {
                throw new ArgumentException(SR.Format(SR.ShouldBeGreaterThan0, nameof(options.TokensPerPeriod)), nameof(options));
            }
            if (options.QueueLimit < 0)
            {
                throw new ArgumentException(SR.Format(SR.ShouldBeGreaterThanOrEqual0, nameof(options.QueueLimit)), nameof(options));
            }
            if (options.ReplenishmentPeriod <= TimeSpan.Zero)
            {
                throw new ArgumentException(SR.Format(SR.ShouldBeGreaterThanTimeSpan0, nameof(options.ReplenishmentPeriod)), nameof(options));
            }

            _options = new TokenBucketRateLimiterOptions
            {
                TokenLimit = options.TokenLimit,
                QueueProcessingOrder = options.QueueProcessingOrder,
                QueueLimit = options.QueueLimit,
                ReplenishmentPeriod = options.ReplenishmentPeriod,
                TokensPerPeriod = options.TokensPerPeriod,
                AutoReplenishment = options.AutoReplenishment
            };

            _tokenCount = options.TokenLimit;
            _fillRate = (double)options.TokensPerPeriod / options.ReplenishmentPeriod.Ticks;

            _idleSince = _lastReplenishmentTick = Stopwatch.GetTimestamp();

            if (_options.AutoReplenishment)
            {
                _renewTimer = new Timer(Replenish, this, _options.ReplenishmentPeriod, _options.ReplenishmentPeriod);
            }
        }

        /// <inheritdoc/>
        public override RateLimiterStatistics? GetStatistics()
        {
            ThrowIfDisposed();
            return new RateLimiterStatistics()
            {
                CurrentAvailablePermits = (long)_tokenCount,
                CurrentQueuedCount = _queueCount,
                TotalFailedLeases = Interlocked.Read(ref _failedLeasesCount),
                TotalSuccessfulLeases = Interlocked.Read(ref _successfulLeasesCount),
            };
        }

        /// <inheritdoc/>
        protected override RateLimitLease AttemptAcquireCore(int tokenCount)
        {
            // These amounts of resources can never be acquired
            if (tokenCount > _options.TokenLimit)
            {
                throw new ArgumentOutOfRangeException(nameof(tokenCount), tokenCount, SR.Format(SR.TokenLimitExceeded, tokenCount, _options.TokenLimit));
            }

            // Return SuccessfulLease or FailedLease depending to indicate limiter state
            if (tokenCount == 0 && !_disposed)
            {
                if (_tokenCount > 0)
                {
                    Interlocked.Increment(ref _successfulLeasesCount);
                    return SuccessfulLease;
                }

                Interlocked.Increment(ref _failedLeasesCount);
                return CreateFailedTokenLease(tokenCount);
            }

            lock (Lock)
            {
                if (TryLeaseUnsynchronized(tokenCount, out RateLimitLease? lease))
                {
                    return lease;
                }

                Interlocked.Increment(ref _failedLeasesCount);
                return CreateFailedTokenLease(tokenCount);
            }
        }

        /// <inheritdoc/>
        protected override ValueTask<RateLimitLease> AcquireAsyncCore(int tokenCount, CancellationToken cancellationToken = default)
        {
            // These amounts of resources can never be acquired
            if (tokenCount > _options.TokenLimit)
            {
                throw new ArgumentOutOfRangeException(nameof(tokenCount), tokenCount, SR.Format(SR.TokenLimitExceeded, tokenCount, _options.TokenLimit));
            }

            ThrowIfDisposed();

            // Return SuccessfulAcquisition if requestedCount is 0 and resources are available
            if (tokenCount == 0 && _tokenCount > 0)
            {
                Interlocked.Increment(ref _successfulLeasesCount);
                return new ValueTask<RateLimitLease>(SuccessfulLease);
            }

            using var disposer = default(RequestRegistration.Disposer);
            lock (Lock)
            {
                if (TryLeaseUnsynchronized(tokenCount, out RateLimitLease? lease))
                {
                    return new ValueTask<RateLimitLease>(lease);
                }

                // Avoid integer overflow by using subtraction instead of addition
                Debug.Assert(_options.QueueLimit >= _queueCount);
                if (_options.QueueLimit - _queueCount < tokenCount)
                {
                    if (_options.QueueProcessingOrder == QueueProcessingOrder.NewestFirst && tokenCount <= _options.QueueLimit)
                    {
                        // remove oldest items from queue until there is space for the newest acquisition request
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
                        while (_options.QueueLimit - _queueCount < tokenCount);
                    }
                    else
                    {
                        Interlocked.Increment(ref _failedLeasesCount);
                        // Don't queue if queue limit reached and QueueProcessingOrder is OldestFirst
                        return new ValueTask<RateLimitLease>(CreateFailedTokenLease(tokenCount));
                    }
                }

                var registration = new RequestRegistration(tokenCount, this, cancellationToken);
                _queue.EnqueueTail(registration);
                _queueCount += tokenCount;
                Debug.Assert(_queueCount <= _options.QueueLimit);

                return new ValueTask<RateLimitLease>(registration.Task);
            }
        }

        private TokenBucketLease CreateFailedTokenLease(int tokenCount)
        {
            int replenishAmount = tokenCount - (int)_tokenCount + _queueCount;
            // can't have 0 replenish periods, that would mean it should be a successful lease
            // if TokensPerPeriod is larger than the replenishAmount needed then it would be 0
            Debug.Assert(_options.TokensPerPeriod > 0);
            int replenishPeriods = Math.Max(replenishAmount / _options.TokensPerPeriod, 1);

            return new TokenBucketLease(false, TimeSpan.FromTicks(_options.ReplenishmentPeriod.Ticks * replenishPeriods));
        }

        private bool TryLeaseUnsynchronized(int tokenCount, [NotNullWhen(true)] out RateLimitLease? lease)
        {
            ThrowIfDisposed();

            // if permitCount is 0 we want to queue it if there are no available permits
            if (_tokenCount >= tokenCount && _tokenCount != 0)
            {
                if (tokenCount == 0)
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
                    _tokenCount -= tokenCount;
                    Debug.Assert(_tokenCount >= 0);
                    Interlocked.Increment(ref _successfulLeasesCount);
                    lease = SuccessfulLease;
                    return true;
                }
            }

            lease = null;
            return false;
        }

        /// <summary>
        /// Attempts to replenish the bucket.
        /// </summary>
        /// <returns>
        /// <see langword="false"/> if <see cref="TokenBucketRateLimiterOptions.AutoReplenishment"/> is enabled, otherwise <see langword="true"/>.
        /// Does not reflect if tokens were replenished.
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
            TokenBucketRateLimiter limiter = (state as TokenBucketRateLimiter)!;
            Debug.Assert(limiter is not null);

            // Use Stopwatch instead of DateTime.UtcNow to avoid issues on systems where the clock can change
            long nowTicks = Stopwatch.GetTimestamp();
            limiter!.ReplenishInternal(nowTicks);
        }

        // Used in tests to avoid dealing with real time
        private void ReplenishInternal(long nowTicks)
        {
            using var disposer = default(RequestRegistration.Disposer);

            // method is re-entrant (from Timer), lock to avoid multiple simultaneous replenishes
            lock (Lock)
            {
                if (_disposed)
                {
                    return;
                }

                if (_tokenCount == _options.TokenLimit)
                {
                    return;
                }

                double add;

                // Trust the timer to be close enough to when we want to replenish, this avoids issues with Timer jitter where it might be .99 seconds instead of 1, and 1.1 seconds the next time etc.
                if (_options.AutoReplenishment)
                {
                    add = _options.TokensPerPeriod;
                }
                else
                {
                    add = _fillRate * (nowTicks - _lastReplenishmentTick) * TickFrequency;
                }

                _tokenCount = Math.Min(_options.TokenLimit, _tokenCount + add);

                _lastReplenishmentTick = nowTicks;

                // Process queued requests
                Deque<RequestRegistration> queue = _queue;

                Debug.Assert(_tokenCount <= _options.TokenLimit);
                while (queue.Count > 0)
                {
                    RequestRegistration nextPendingRequest =
                          _options.QueueProcessingOrder == QueueProcessingOrder.OldestFirst
                          ? queue.PeekHead()
                          : queue.PeekTail();

                    // Request was handled already, either via cancellation or being kicked from the queue due to a newer request being queued.
                    // We just need to remove the item and let the next queued item be considered for completion.
                    if (nextPendingRequest.Task.IsCompleted)
                    {
                        nextPendingRequest =
                            _options.QueueProcessingOrder == QueueProcessingOrder.OldestFirst
                            ? queue.DequeueHead()
                            : queue.DequeueTail();
                        disposer.Add(nextPendingRequest);
                    }
                    else if (_tokenCount >= nextPendingRequest.Count)
                    {
                        // Request can be fulfilled
                        nextPendingRequest =
                            _options.QueueProcessingOrder == QueueProcessingOrder.OldestFirst
                            ? queue.DequeueHead()
                            : queue.DequeueTail();

                        _queueCount -= nextPendingRequest.Count;
                        _tokenCount -= nextPendingRequest.Count;
                        Debug.Assert(_tokenCount >= 0);

                        if (!nextPendingRequest.TrySetResult(SuccessfulLease))
                        {
                            // Queued item was canceled so add count back, permits weren't acquired
                            _tokenCount += nextPendingRequest.Count;
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

                if (_tokenCount == _options.TokenLimit)
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
                throw new ObjectDisposedException(nameof(TokenBucketRateLimiter));
            }
        }

        private sealed class TokenBucketLease : RateLimitLease
        {
            private static readonly string[] s_allMetadataNames = new[] { MetadataName.RetryAfter.Name };

            private readonly TimeSpan? _retryAfter;

            public TokenBucketLease(bool isAcquired, TimeSpan? retryAfter)
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

            public RequestRegistration(int permitCount, TokenBucketRateLimiter limiter, CancellationToken cancellationToken)
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
                    var limiter = (TokenBucketRateLimiter)registration.Task.AsyncState!;
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
