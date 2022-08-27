// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading.RateLimiting
{
    /// <summary>
    /// Options to control the behavior of a <see cref="TokenBucketRateLimiter"/>.
    /// </summary>
    public sealed class TokenBucketRateLimiterOptions
    {
        /// <summary>
        /// Specifies the minimum period between replenishments.
        /// Must be set to a value >= <see cref="TimeSpan.Zero" /> by the time these options are passed to the constructor of <see cref="TokenBucketRateLimiter"/>.
        /// </summary>
        public TimeSpan ReplenishmentPeriod { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Specifies the maximum number of tokens to restore each replenishment.
        /// Must be set to a value > 0 by the time these options are passed to the constructor of <see cref="TokenBucketRateLimiter"/>.
        /// </summary>
        public int TokensPerPeriod { get; set; }

        /// <summary>
        /// Specified whether the <see cref="TokenBucketRateLimiter"/> is automatically replenishing tokens or if someone else
        /// will be calling <see cref="TokenBucketRateLimiter.TryReplenish"/> to replenish tokens.
        /// </summary>
        /// <value>
        /// <see langword="true" /> by default.
        /// </value>
        public bool AutoReplenishment { get; set; } = true;

        /// <summary>
        /// Maximum number of tokens that can be in the bucket at any time.
        /// Must be set to a value > 0 by the time these options are passed to the constructor of <see cref="TokenBucketRateLimiter"/>.
        /// </summary>
        public int TokenLimit { get; set; }

        /// <summary>
        /// Determines the behaviour of <see cref="RateLimiter.AcquireAsync"/> when not enough resources can be leased.
        /// </summary>
        /// <value>
        /// <see cref="QueueProcessingOrder.OldestFirst"/> by default.
        /// </value>
        public QueueProcessingOrder QueueProcessingOrder { get; set; } = QueueProcessingOrder.OldestFirst;

        /// <summary>
        /// Maximum cumulative token count of queued acquisition requests.
        /// Must be set to a value >= 0 by the time these options are passed to the constructor of <see cref="TokenBucketRateLimiter"/>.
        /// </summary>
        public int QueueLimit { get; set; }
    }
}
