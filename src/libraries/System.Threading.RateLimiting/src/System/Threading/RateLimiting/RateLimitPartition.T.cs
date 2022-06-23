// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading.RateLimiting
{
    /// <summary>
    /// Type returned by <see cref="RateLimitPartition.Create"/> methods to be used by <see cref="PartitionedRateLimiter.Create"/> to know what partitions are configured.
    /// </summary>
    /// <typeparam name="TKey">The type to distinguish partitions with.</typeparam>
    public struct RateLimitPartition<TKey>
    {
        /// <summary>
        /// Constructs the <see cref="RateLimitPartition{TKey}"/> for use in <see cref="PartitionedRateLimiter.Create"/>.
        /// </summary>
        /// <param name="partitionKey">The specific key for this partition.</param>
        /// <param name="factory">The function called when a rate limiter for the given <paramref name="partitionKey"/> is needed.</param>
        public RateLimitPartition(TKey partitionKey, Func<TKey, RateLimiter> factory)
        {
            PartitionKey = partitionKey;
            Factory = factory;
        }

        /// <summary>
        /// The specific key for this partition.
        /// </summary>
        public TKey PartitionKey { get; }

        internal readonly Func<TKey, RateLimiter> Factory;
    }
}
