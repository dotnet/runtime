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
        public static RateLimitPartition<TKey> Get<TKey>(
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
        public static RateLimitPartition<TKey> GetConcurrencyLimiter<TKey>(
            TKey partitionKey,
            Func<TKey, ConcurrencyLimiterOptions> factory)
        {
            return Get(partitionKey, key => new ConcurrencyLimiter(factory(key)));
        }

        /// <summary>
        /// Defines a partition that will not have a rate limiter.
        /// This means any calls to <see cref="PartitionedRateLimiter{TResource}.AttemptAcquire(TResource, int)"/> or <see cref="PartitionedRateLimiter{TResource}.AcquireAsync(TResource, int, CancellationToken)"/> will always succeed for the given <paramref name="partitionKey"/>.
        /// </summary>
        /// <typeparam name="TKey">The type to distinguish partitions with.</typeparam>
        /// <param name="partitionKey">The specific key for this partition.</param>
        /// <returns></returns>
        public static RateLimitPartition<TKey> GetNoLimiter<TKey>(TKey partitionKey)
        {
            return Get(partitionKey, _ => NoopLimiter.Instance);
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
        public static RateLimitPartition<TKey> GetTokenBucketLimiter<TKey>(
            TKey partitionKey,
            Func<TKey, TokenBucketRateLimiterOptions> factory)
        {
            return Get(partitionKey, key =>
            {
                TokenBucketRateLimiterOptions options = factory(key);
                // We don't want individual TokenBucketRateLimiters to have timers. We will instead have our own internal Timer handling all of them
                if (options.AutoReplenishment is true)
                {
                    options = new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = options.TokenLimit,
                        QueueProcessingOrder = options.QueueProcessingOrder,
                        QueueLimit = options.QueueLimit,
                        ReplenishmentPeriod = options.ReplenishmentPeriod,
                        TokensPerPeriod = options.TokensPerPeriod,
                        AutoReplenishment = false
                    };
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
        public static RateLimitPartition<TKey> GetSlidingWindowLimiter<TKey>(
            TKey partitionKey,
            Func<TKey, SlidingWindowRateLimiterOptions> factory)
        {
            return Get(partitionKey, key =>
            {
                SlidingWindowRateLimiterOptions options = factory(key);
                // We don't want individual SlidingWindowRateLimiters to have timers. We will instead have our own internal Timer handling all of them
                if (options.AutoReplenishment is true)
                {
                    options = new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = options.PermitLimit,
                        QueueProcessingOrder = options.QueueProcessingOrder,
                        QueueLimit = options.QueueLimit,
                        Window = options.Window,
                        SegmentsPerWindow = options.SegmentsPerWindow,
                        AutoReplenishment = false
                    };
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
        public static RateLimitPartition<TKey> GetFixedWindowLimiter<TKey>(
            TKey partitionKey,
            Func<TKey, FixedWindowRateLimiterOptions> factory)
        {
            return Get(partitionKey, key =>
            {
                FixedWindowRateLimiterOptions options = factory(key);
                // We don't want individual FixedWindowRateLimiters to have timers. We will instead have our own internal Timer handling all of them
                if (options.AutoReplenishment is true)
                {
                    options = new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.PermitLimit,
                        QueueProcessingOrder = options.QueueProcessingOrder,
                        QueueLimit = options.QueueLimit,
                        Window = options.Window,
                        AutoReplenishment = false
                    };
                }
                return new FixedWindowRateLimiter(options);
            });
        }
    }
}
