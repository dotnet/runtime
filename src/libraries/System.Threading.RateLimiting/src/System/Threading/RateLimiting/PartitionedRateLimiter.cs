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
        private Timer? _timer;
        private bool _disposed;

        // Use the Dictionary as the lock field so we don't need to allocate another object for a lock and have another field in the object
        private object Lock => _limiters;

        public DefaultPartitionedRateLimiter(Func<TResource, RateLimitPartition<TKey>> partitioner,
            IEqualityComparer<TKey>? equalityComparer = null)
        {
            _limiters = new Dictionary<TKey, Lazy<RateLimiter>>(equalityComparer);
            _partitioner = partitioner;

            // TODO: Only create timer once there is an active replenishing limiter
            // TODO: Figure out what interval we should use
            _timer = new Timer(Replenish, this, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
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

            lock (Lock)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;

                _timer?.Dispose();

                foreach (KeyValuePair<TKey, Lazy<RateLimiter>> limiter in _limiters)
                {
                    limiter.Value.Value.Dispose();
                }
                _limiters.Clear();
            }
        }

        protected override async ValueTask DisposeAsyncCore()
        {
            Dictionary<TKey, Lazy<RateLimiter>> limiters;
            lock (Lock)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;

                _timer?.Dispose();

                limiters = _limiters;
                _limiters = new();
            }

            foreach (KeyValuePair<TKey, Lazy<RateLimiter>> limiter in limiters)
            {
                await limiter.Value.Value.DisposeAsync().ConfigureAwait(false);
            }

            limiters.Clear();

            return;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(PartitionedRateLimiter));
            }
        }

        private static void Replenish(object? state)
        {
            DefaultPartitionedRateLimiter<TResource, TKey> limiter = (state as DefaultPartitionedRateLimiter<TResource, TKey>)!;
            Debug.Assert(limiter is not null);

            lock (limiter.Lock)
            {
                if (limiter._disposed)
                {
                    return;
                }

                foreach (KeyValuePair<TKey, Lazy<RateLimiter>> kvp in limiter._limiters)
                {
                    // Check IsValueCreated to avoid potentially blocking inside the lock if the Lazy hasn't been initialized yet
                    if (kvp.Value.IsValueCreated)
                    {
                        if (kvp.Value.Value is ReplenishingRateLimiter replenishingLimiter)
                        {
                            replenishingLimiter.TryReplenish();
                        }
                    }
                }
            }
        }
    }
}
