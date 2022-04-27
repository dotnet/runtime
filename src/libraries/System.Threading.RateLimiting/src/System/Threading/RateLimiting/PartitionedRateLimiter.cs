// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace System.Threading.RateLimiting
{
    /// <summary>
    /// Contains methods to assist with creating a <see cref="PartitionedRateLimiter{TResource}"/>.
    /// </summary>
    public static class PartitionedRateLimiter
    {
        /// <summary>
        /// Method used to create a default implementation of <see cref="PartitionedRateLimiter{TResource}"/>.
        /// </summary>
        /// <typeparam name="TResource">The resource type that is being rate limited.</typeparam>
        /// <typeparam name="TPartitionKey">The type to distinguish partitions with.</typeparam>
        /// <param name="partitioner">Method called every time an Acquire or WaitAsync call is made to figure out what rate limiter to apply to the request.
        /// If the <see cref="RateLimitPartition{TKey}.PartitionKey"/> matches a cached entry then the rate limiter previously used for that key is used. Otherwise, the factory is called to get a new rate limiter.</param>
        /// <param name="equalityComparer">Optional <see cref="IEqualityComparer{T}"/> to customize the comparison logic for <typeparamref name="TPartitionKey"/>.</param>
        /// <returns></returns>
        public static PartitionedRateLimiter<TResource> Create<TResource, TPartitionKey>(
            Func<TResource, RateLimitPartition<TPartitionKey>> partitioner,
            IEqualityComparer<TPartitionKey>? equalityComparer = null) where TPartitionKey : notnull
        {
            return new DefaultPartitionedRateLimiter<TResource, TPartitionKey>(partitioner, equalityComparer);
        }
    }

    internal sealed class DefaultPartitionedRateLimiter<TResource, TKey> : PartitionedRateLimiter<TResource> where TKey : notnull
    {
        private readonly Func<TResource, RateLimitPartition<TKey>> _partitioner;

        // TODO: Look at ConcurrentDictionary to try and avoid a global lock
        private Dictionary<TKey, Lazy<RateLimiter>> _limiters;
        private bool _disposed;

        // Used by the Timer to call TryRelenish on ReplenishingRateLimiters
        // We use a separate list to avoid running TryReplenish (which might be user code) inside our lock
        // And we cache the list to amortize the allocation cost to as close to 0 as we can get
        private List<Lazy<RateLimiter>>? _cachedLimiters = new();
        private bool _cacheInvalid;
        private TimerAwaitable _timer;
        private Task _timerTask;

        // Use the Dictionary as the lock field so we don't need to allocate another object for a lock and have another field in the object
        private object Lock => _limiters;

        public DefaultPartitionedRateLimiter(Func<TResource, RateLimitPartition<TKey>> partitioner,
            IEqualityComparer<TKey>? equalityComparer = null)
        {
            _limiters = new Dictionary<TKey, Lazy<RateLimiter>>(equalityComparer);
            _partitioner = partitioner;

            // TODO: Figure out what interval we should use
            _timer = new TimerAwaitable(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            _timerTask = RunTimer();
        }

        private async Task RunTimer()
        {
            _timer.Start();
            while (!_timer.IsCompleted && !_disposed)
            {
                await _timer;
                Replenish(this);
            }
        }

        public override int GetAvailablePermits(TResource resourceID)
        {
            return GetRateLimiter(resourceID).GetAvailablePermits();
        }

        protected override RateLimitLease AcquireCore(TResource resourceID, int permitCount)
        {
            return GetRateLimiter(resourceID).Acquire(permitCount);
        }

        protected override ValueTask<RateLimitLease> WaitAsyncCore(TResource resourceID, int permitCount, CancellationToken cancellationToken)
        {
            return GetRateLimiter(resourceID).WaitAsync(permitCount, cancellationToken);
        }

        private RateLimiter GetRateLimiter(TResource resourceID)
        {
            RateLimitPartition<TKey> partition = _partitioner(resourceID);
            Lazy<RateLimiter>? limiter;
            lock (Lock)
            {
                ThrowIfDisposed();
                if (!_limiters.TryGetValue(partition.PartitionKey, out limiter))
                {
                    // Using Lazy avoids calling user code (partition.Factory) inside the lock
                    limiter = new Lazy<RateLimiter>(() => partition.Factory(partition.PartitionKey));
                    _limiters.Add(partition.PartitionKey, limiter);
                    // Cache is invalid now
                    _cacheInvalid = true;
                }
            }
            return limiter.Value;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            Dictionary<TKey, Lazy<RateLimiter>> limiters;
            lock (Lock)
            {
                if (CommonDisposeUnsynchronized())
                {
                    return;
                }

                limiters = _limiters;
                // Don't call Clear() as that would clear the local limiters reference as well, which is what we'll be using to call DisposeAsync on all the limiters
                _limiters = new Dictionary<TKey, Lazy<RateLimiter>>();
            }

            _timerTask.GetAwaiter().GetResult();

            foreach (KeyValuePair<TKey, Lazy<RateLimiter>> limiter in limiters)
            {
                limiter.Value.Value.Dispose();
            }
        }

        protected override async ValueTask DisposeAsyncCore()
        {
            Dictionary<TKey, Lazy<RateLimiter>> limiters;
            lock (Lock)
            {
                if (CommonDisposeUnsynchronized())
                {
                    return;
                }

                limiters = _limiters;
                // Don't call Clear() as that would clear the local limiters reference as well, which is what we'll be using to call DisposeAsync on all the limiters
                _limiters = new Dictionary<TKey, Lazy<RateLimiter>>();
            }

            await _timerTask.ConfigureAwait(false);

            foreach (KeyValuePair<TKey, Lazy<RateLimiter>> limiter in limiters)
            {
                await limiter.Value.Value.DisposeAsync().ConfigureAwait(false);
            }

            limiters.Clear();

            return;
        }

        // This handles the common state changes that Dipose and DisposeAsync need to do, the individual limiters still need to be Disposed after this call
        private bool CommonDisposeUnsynchronized()
        {
            if (_disposed)
            {
                return true;
            }
            _disposed = true;
            _timer.Stop();
            _timer.Dispose();

            return false;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(PartitionedRateLimiter));
            }
        }

        private static void Replenish(DefaultPartitionedRateLimiter<TResource, TKey> limiter)
        {
            List<Lazy<RateLimiter>>? cachedLimiters = Interlocked.Exchange(ref limiter._cachedLimiters, null);
            if (cachedLimiters is null)
            {
                // Timer already running, do nothing
                // This might be an indication that there is a slow ReplenishingRateLimiter.TryReplenish implementation
                // Or many limiters that need updating and a short timer period
                // Or threadpool exhaustion unrelated to this type directly
                // We might want to write an event for this in the future so it's visible to diagnostic tools
                return;
            }

            lock (limiter.Lock)
            {
                if (limiter._disposed)
                {
                    cachedLimiters.Clear();
                    return;
                }

                // If the cache has been invalidated we need to recreate it
                if (limiter._cacheInvalid)
                {
                    cachedLimiters.Clear();
                    bool cacheStillInvalid = false;
                    foreach (KeyValuePair<TKey, Lazy<RateLimiter>> kvp in limiter._limiters)
                    {
                        if (kvp.Value.IsValueCreated)
                        {
                            if (kvp.Value.Value is ReplenishingRateLimiter)
                            {
                                cachedLimiters.Add(kvp.Value);
                            }
                        }
                        else
                        {
                            // In rare cases the RateLimiter will be added to the storage but not be initialized yet
                            // keep cache invalid if there was a non-initialized RateLimiter
                            // the next time we run the timer the cache will be updated
                            // with the initialized RateLimiter
                            cacheStillInvalid = true;
                        }
                    }
                    limiter._cacheInvalid = cacheStillInvalid;
                }
            }

            try
            {
                // cachedLimiters is safe to use outside the lock because it is only updated by the Timer
                // and the Timer avoids re-entrancy issues via the _executingTimer field
                foreach (Lazy<RateLimiter> rateLimiter in cachedLimiters)
                {
                    Debug.Assert(rateLimiter.IsValueCreated && rateLimiter.Value is ReplenishingRateLimiter);
                    ((ReplenishingRateLimiter)rateLimiter.Value).TryReplenish();
                }
            }
            finally
            {
                Interlocked.Exchange(ref limiter._cachedLimiters, cachedLimiters);
            }
        }
    }
}
