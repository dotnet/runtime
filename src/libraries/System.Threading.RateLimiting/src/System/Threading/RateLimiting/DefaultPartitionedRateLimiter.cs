// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Threading.RateLimiting
{
    internal sealed class DefaultPartitionedRateLimiter<TResource, TKey> : PartitionedRateLimiter<TResource> where TKey : notnull
    {
        private readonly Func<TResource, RateLimitPartition<TKey>> _partitioner;
        private static readonly TimeSpan s_idleTimeLimit = TimeSpan.FromSeconds(10);

        // TODO: Look at ConcurrentDictionary to try and avoid a global lock
        private readonly Dictionary<TKey, Lazy<LimiterEntry>> _limiters;
        private bool _disposed;
        private readonly TaskCompletionSource<object?> _disposeComplete = new(TaskCreationOptions.RunContinuationsAsynchronously);

        // Used by the Timer to call TryRelenish on ReplenishingRateLimiters
        // We use a separate list to avoid running TryReplenish (which might be user code) inside our lock
        // And we cache the list to amortize the allocation cost to as close to 0 as we can get
        private readonly List<KeyValuePair<TKey, Lazy<LimiterEntry>>> _cachedLimiters = new();
        private bool _cacheInvalid;
        private readonly List<RateLimiter> _limitersToDispose = new();
        private readonly TimerAwaitable _timer;
        private readonly Task _timerTask;

        // Use the Dictionary as the lock field so we don't need to allocate another object for a lock and have another field in the object
        private object Lock => _limiters;

        public DefaultPartitionedRateLimiter(Func<TResource, RateLimitPartition<TKey>> partitioner,
            IEqualityComparer<TKey>? equalityComparer = null)
            : this(partitioner, equalityComparer, TimeSpan.FromMilliseconds(100))
        {
        }

        // Extra ctor for testing purposes, primarily used when wanting to test the timer manually
        private DefaultPartitionedRateLimiter(Func<TResource, RateLimitPartition<TKey>> partitioner,
            IEqualityComparer<TKey>? equalityComparer, TimeSpan timerInterval)
        {
            _limiters = new Dictionary<TKey, Lazy<LimiterEntry>>(equalityComparer);
            _partitioner = partitioner;

            _timer = new TimerAwaitable(timerInterval, timerInterval);
            _timerTask = RunTimer();
        }

        private async Task RunTimer()
        {
            _timer.Start();
            while (await _timer)
            {
                try
                {
                    await Heartbeat().ConfigureAwait(
#if NET
                        ConfigureAwaitOptions.SuppressThrowing
#else
                        false
#endif
                        );
                }
                catch { }
            }
            _timer.Dispose();
        }

        public override RateLimiterStatistics? GetStatistics(TResource resource)
        {
            return GetRateLimiter(resource).GetStatistics();
        }

        protected override RateLimitLease AttemptAcquireCore(TResource resource, int permitCount)
        {
            return GetRateLimiter(resource).AttemptAcquire(permitCount);
        }

        protected override ValueTask<RateLimitLease> AcquireAsyncCore(TResource resource, int permitCount, CancellationToken cancellationToken)
        {
            return GetRateLimiter(resource).AcquireAsync(permitCount, cancellationToken);
        }

        private RateLimiter GetRateLimiter(TResource resource)
        {
            RateLimitPartition<TKey> partition = _partitioner(resource);
            Lazy<LimiterEntry>? entry;
            lock (Lock)
            {
                ThrowIfDisposed();
                if (!_limiters.TryGetValue(partition.PartitionKey, out entry))
                {
                    // Using Lazy avoids calling user code (partition.Factory) inside the lock
                    entry = new Lazy<LimiterEntry>(() => new LimiterEntry(partition.Factory(partition.PartitionKey)));
                    _limiters.Add(partition.PartitionKey, entry);
                    // Cache is invalid now
                    _cacheInvalid = true;
                }
                else if (entry.IsValueCreated)
                {
                    // Refresh the last-access timestamp under the lock so the Heartbeat won't observe
                    // a stale value and concurrently evict a limiter that's actively being used.
                    entry.Value.LastAccessTimestamp = Stopwatch.GetTimestamp();
                }
            }

            LimiterEntry limiterEntry = entry.Value;
            // Refresh the last-access timestamp so that the Heartbeat can evict limiters whose
            // IdleDuration is always null (e.g. NoopLimiter) once they stop being accessed.
            limiterEntry.LastAccessTimestamp = Stopwatch.GetTimestamp();
            return limiterEntry.Limiter;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            bool alreadyDisposed = CommonDispose();

            _timerTask.GetAwaiter().GetResult();
            _cachedLimiters.Clear();

            if (alreadyDisposed)
            {
                _disposeComplete.Task.GetAwaiter().GetResult();
                return;
            }

            List<Exception>? exceptions = null;

            // Safe to access _limiters outside the lock
            // The timer is no longer running and _disposed is set so anyone trying to access fields will be checking that first
            foreach (KeyValuePair<TKey, Lazy<LimiterEntry>> limiter in _limiters)
            {
                try
                {
                    limiter.Value.Value.Limiter.Dispose();
                }
                catch (Exception ex)
                {
                    exceptions ??= new List<Exception>();
                    exceptions.Add(ex);
                }
            }
            _limiters.Clear();
            _disposeComplete.TrySetResult(null);

            if (exceptions is not null)
            {
                throw new AggregateException(exceptions);
            }
        }

        protected override async ValueTask DisposeAsyncCore()
        {
            bool alreadyDisposed = CommonDispose();

            await _timerTask.ConfigureAwait(false);
            _cachedLimiters.Clear();

            if (alreadyDisposed)
            {
                await _disposeComplete.Task.ConfigureAwait(false);
                return;
            }

            List<Exception>? exceptions = null;
            foreach (KeyValuePair<TKey, Lazy<LimiterEntry>> limiter in _limiters)
            {
                try
                {
                    await limiter.Value.Value.Limiter.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions ??= new List<Exception>();
                    exceptions.Add(ex);
                }
            }
            _limiters.Clear();
            _disposeComplete.TrySetResult(null);

            if (exceptions is not null)
            {
                throw new AggregateException(exceptions);
            }
        }

        // This handles the common state changes that Dispose and DisposeAsync need to do, the individual limiters still need to be Disposed after this call
        private bool CommonDispose()
        {
            lock (Lock)
            {
                if (_disposed)
                {
                    return true;
                }
                _disposed = true;
                _timer.Stop();
            }
            return false;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(PartitionedRateLimiter));
            }
        }

        private async Task Heartbeat()
        {
            lock (Lock)
            {
                if (_disposed)
                {
                    return;
                }

                // If the cache has been invalidated we need to recreate it
                if (_cacheInvalid)
                {
                    _cachedLimiters.Clear();
                    _cachedLimiters.AddRange(_limiters);
                    _cacheInvalid = false;
                }
            }

            List<Exception>? aggregateExceptions = null;

            // cachedLimiters is safe to use outside the lock because it is only updated by the Timer
            foreach (KeyValuePair<TKey, Lazy<LimiterEntry>> rateLimiter in _cachedLimiters)
            {
                if (!rateLimiter.Value.IsValueCreated)
                {
                    continue;
                }
                LimiterEntry limiterEntry = rateLimiter.Value.Value;
                // Fall back to our internally tracked last-access timestamp when the limiter
                // does not report an idle duration (e.g. NoopLimiter always returns null).
                // This ensures idle partitions are eventually evicted regardless of the limiter implementation.
                TimeSpan idleDuration = limiterEntry.Limiter.IdleDuration
                    ?? RateLimiterHelper.GetElapsedTime(limiterEntry.LastAccessTimestamp).GetValueOrDefault();
                if (idleDuration > s_idleTimeLimit)
                {
                    lock (Lock)
                    {
                        // Check time again under lock to make sure no one calls Acquire or WaitAsync after checking the time and removing the limiter
                        idleDuration = limiterEntry.Limiter.IdleDuration
                            ?? RateLimiterHelper.GetElapsedTime(limiterEntry.LastAccessTimestamp).GetValueOrDefault();
                        if (idleDuration > s_idleTimeLimit)
                        {
                            // Remove limiter from the lookup table and mark cache as invalid
                            // If a request for this partition comes in it will have to create a new limiter now
                            // And the next time the timer runs the cache needs to be updated to no longer have a reference to this limiter
                            _cacheInvalid = true;
                            _limiters.Remove(rateLimiter.Key);

                            // We don't want to dispose inside the lock so we need to defer it
                            _limitersToDispose.Add(limiterEntry.Limiter);
                        }
                    }
                }
                // We know the limiter can be replenished so let's attempt to replenish tokens
                else if (limiterEntry.Limiter is ReplenishingRateLimiter replenishingRateLimiter)
                {
                    try
                    {
                        replenishingRateLimiter.TryReplenish();
                    }
                    catch (Exception ex)
                    {
                        aggregateExceptions ??= [];
                        aggregateExceptions.Add(ex);
                    }
                }
            }

            foreach (RateLimiter limiter in _limitersToDispose)
            {
                try
                {
                    await limiter.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    aggregateExceptions ??= new List<Exception>();
                    aggregateExceptions.Add(ex);
                }
            }
            _limitersToDispose.Clear();

            if (aggregateExceptions is not null)
            {
                throw new AggregateException(aggregateExceptions);
            }
        }

        // Wraps a RateLimiter with a timestamp of when it was last accessed by the partitioned limiter.
        // The timestamp is used by the Heartbeat to evict limiters whose IdleDuration is always null
        // (e.g. NoopLimiter), which would otherwise never be cleaned up.
        private sealed class LimiterEntry
        {
            public LimiterEntry(RateLimiter limiter)
            {
                Limiter = limiter;
                LastAccessTimestamp = Stopwatch.GetTimestamp();
            }

            public RateLimiter Limiter { get; }
            public long LastAccessTimestamp;
        }
    }
}
