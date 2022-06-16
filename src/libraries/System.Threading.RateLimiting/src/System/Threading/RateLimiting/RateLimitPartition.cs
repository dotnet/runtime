// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading.RateLimiting
{
    /// <summary>
    /// Contains methods used in <see cref="PartitionedRateLimiter.Create"/> to assist in the creation of partitions for your rate limiter.
    /// </summary>
    public static class RateLimitPartition
    {
        /// <summary>
        /// Defines a partition with the given rate limiter factory.
        /// </summary>
        /// <typeparam name="TKey">The type to distinguish partitions with.</typeparam>
        /// <remarks>
        /// The <paramref name="factory"/> should return a new instance of a rate limiter every time it is called.
        /// </remarks>
        /// <param name="partitionKey">The specific key for this partition. This will be used to check for an existing cached limiter before calling the <paramref name="factory"/>.</param>
        /// <param name="factory">The function called when a rate limiter for the given <paramref name="partitionKey"/> is needed. This should be a new instance of a rate limiter every time it is called.</param>
        /// <returns></returns>
        public static RateLimitPartition<TKey> Create<TKey>(
            TKey partitionKey,
            Func<TKey, RateLimiter> factory)
        {
            return new RateLimitPartition<TKey>(partitionKey, factory);
        }

        /// <summary>
        /// Defines a partition with a <see cref="ConcurrencyLimiter"/> with the given <see cref="ConcurrencyLimiterOptions"/>.
        /// </summary>
        /// <typeparam name="TKey">The type to distinguish partitions with.</typeparam>
        /// <param name="partitionKey">The specific key for this partition. This will be used to check for an existing cached limiter before calling the <paramref name="factory"/>.</param>
        /// <param name="factory">The function called when a rate limiter for the given <paramref name="partitionKey"/> is needed. This can return the same instance of <see cref="ConcurrencyLimiterOptions"/> across different calls.</param>
        /// <returns></returns>
        public static RateLimitPartition<TKey> CreateConcurrencyLimiter<TKey>(
            TKey partitionKey,
            Func<TKey, ConcurrencyLimiterOptions> factory)
        {
            return Create(partitionKey, key => new ConcurrencyLimiter(factory(key)));
        }

        /// <summary>
        /// Defines a partition that will not have a rate limiter.
        /// This means any calls to <see cref="PartitionedRateLimiter{TResource}.Acquire(TResource, int)"/> or <see cref="PartitionedRateLimiter{TResource}.WaitAsync(TResource, int, CancellationToken)"/> will always succeed for the given <paramref name="partitionKey"/>.
        /// </summary>
        /// <typeparam name="TKey">The type to distinguish partitions with.</typeparam>
        /// <param name="partitionKey">The specific key for this partition.</param>
        /// <returns></returns>
        public static RateLimitPartition<TKey> CreateNoLimiter<TKey>(TKey partitionKey)
        {
            return Create(partitionKey, _ => NoopLimiter.Instance);
        }

        /// <summary>
        /// Defines a partition with a <see cref="TokenBucketRateLimiter"/> with the given <see cref="TokenBucketRateLimiterOptions"/>.
        /// </summary>
        /// <remarks>
        /// Set <see cref="TokenBucketRateLimiterOptions.AutoReplenishment"/> to <see langword="false"/> to save an allocation. This method will create a new options type and set <see cref="TokenBucketRateLimiterOptions.AutoReplenishment"/> to <see langword="false"/> otherwise.
        /// </remarks>
        /// <typeparam name="TKey">The type to distinguish partitions with.</typeparam>
        /// <param name="partitionKey">The specific key for this partition.</param>
        /// <param name="factory">The function called when a rate limiter for the given <paramref name="partitionKey"/> is needed. This can return the same instance of <see cref="TokenBucketRateLimiterOptions"/> across different calls.</param>
        /// <returns></returns>
        public static RateLimitPartition<TKey> CreateTokenBucketLimiter<TKey>(
            TKey partitionKey,
            Func<TKey, TokenBucketRateLimiterOptions> factory)
        {
            return Create(partitionKey, key =>
            {
                TokenBucketRateLimiterOptions options = factory(key);
                // We don't want individual TokenBucketRateLimiters to have timers. We will instead have our own internal Timer handling all of them
                if (options.AutoReplenishment is true)
                {
                    options = new TokenBucketRateLimiterOptions(options.TokenLimit, options.QueueProcessingOrder, options.QueueLimit,
                        options.ReplenishmentPeriod, options.TokensPerPeriod, autoReplenishment: false);
                }
                return new TokenBucketRateLimiter(options);
            });
        }

        /// <summary>
        /// Defines a partition with a <see cref="SlidingWindowRateLimiter"/> with the given <see cref="SlidingWindowRateLimiterOptions"/>.
        /// </summary>
        /// <remarks>
        /// Set <see cref="SlidingWindowRateLimiterOptions.AutoReplenishment"/> to <see langword="false"/> to save an allocation. This method will create a new options type and set <see cref="SlidingWindowRateLimiterOptions.AutoReplenishment"/> to <see langword="false"/> otherwise.
        /// </remarks>
        /// <typeparam name="TKey">The type to distinguish partitions with.</typeparam>
        /// <param name="partitionKey">The specific key for this partition.</param>
        /// <param name="factory">The function called when a rate limiter for the given <paramref name="partitionKey"/> is needed. This can return the same instance of <see cref="SlidingWindowRateLimiterOptions"/> across different calls.</param>
        /// <returns></returns>
        public static RateLimitPartition<TKey> CreateSlidingWindowLimiter<TKey>(
            TKey partitionKey,
            Func<TKey, SlidingWindowRateLimiterOptions> factory)
        {
            return Create(partitionKey, key =>
            {
                SlidingWindowRateLimiterOptions options = factory(key);
                // We don't want individual SlidingWindowRateLimiters to have timers. We will instead have our own internal Timer handling all of them
                if (options.AutoReplenishment is true)
                {
                    options = new SlidingWindowRateLimiterOptions(options.PermitLimit, options.QueueProcessingOrder, options.QueueLimit,
                        options.Window, options.SegmentsPerWindow, autoReplenishment: false);
                }
                return new SlidingWindowRateLimiter(options);
            });
        }

        /// <summary>
        /// Defines a partition with a <see cref="FixedWindowRateLimiter"/> with the given <see cref="FixedWindowRateLimiterOptions"/>.
        /// </summary>
        /// <remarks>
        /// Set <see cref="FixedWindowRateLimiterOptions.AutoReplenishment"/> to <see langword="false"/> to save an allocation. This method will create a new options type and set <see cref="FixedWindowRateLimiterOptions.AutoReplenishment"/> to <see langword="false"/> otherwise.
        /// </remarks>
        /// <typeparam name="TKey">The type to distinguish partitions with.</typeparam>
        /// <param name="partitionKey">The specific key for this partition.</param>
        /// <param name="factory">The function called when a rate limiter for the given <paramref name="partitionKey"/> is needed. This can return the same instance of <see cref="FixedWindowRateLimiterOptions"/> across different calls.</param>
        /// <returns></returns>
        public static RateLimitPartition<TKey> CreateFixedWindowLimiter<TKey>(
            TKey partitionKey,
            Func<TKey, FixedWindowRateLimiterOptions> factory)
        {
            return Create(partitionKey, key =>
            {
                FixedWindowRateLimiterOptions options = factory(key);
                // We don't want individual FixedWindowRateLimiters to have timers. We will instead have our own internal Timer handling all of them
                if (options.AutoReplenishment is true)
                {
                    options = new FixedWindowRateLimiterOptions(options.PermitLimit, options.QueueProcessingOrder, options.QueueLimit,
                        options.Window, autoReplenishment: false);
                }
                return new FixedWindowRateLimiter(options);
            });
        }
    }
}
