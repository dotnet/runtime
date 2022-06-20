// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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

        /// <summary>
        /// Creates a single <see cref="PartitionedRateLimiter{TResource}"/> that wraps the passed in <see cref="PartitionedRateLimiter{TResource}"/>s.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Methods on the returned <see cref="PartitionedRateLimiter{TResource}"/> will iterate over the passed in <paramref name="limiters"/> in the order given.
        /// </para>
        /// <para>
        /// <see cref="PartitionedRateLimiter{TResource}.GetAvailablePermits(TResource)"/> will return the lowest value of all the <paramref name="limiters"/>.
        /// </para>
        /// <para>
        /// <see cref="RateLimitLease"/>s returned will aggregate metadata and for duplicates use the value of the first lease with the same metadata name.
        /// </para>
        /// </remarks>
        /// <typeparam name="TResource">The resource type that is being rate limited.</typeparam>
        /// <param name="limiters">The <see cref="PartitionedRateLimiter{TResource}"/>s that will be called in order when acquiring resources.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="limiters"/> is a null parameter.</exception>
        /// <exception cref="ArgumentException"><paramref name="limiters"/> is an empty array.</exception>
        public static PartitionedRateLimiter<TResource> CreateChained<TResource>(
            params PartitionedRateLimiter<TResource>[] limiters)
        {
            if (limiters is null)
            {
                throw new ArgumentNullException(nameof(limiters));
            }
            if (limiters.Length == 0)
            {
                throw new ArgumentException("Must pass in at least 1 limiter.", nameof(limiters));
            }
            return new ChainedPartitionedRateLimiter<TResource>(limiters);
        }
    }
}
