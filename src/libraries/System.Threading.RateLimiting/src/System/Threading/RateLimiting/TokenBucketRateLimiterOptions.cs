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
        /// Initializes the <see cref="TokenBucketRateLimiterOptions"/>.
        /// </summary>
        /// <param name="tokenLimit">Maximum number of tokens that can be in the token bucket.</param>
        /// <param name="queueProcessingOrder"></param>
        /// <param name="queueLimit">Maximum number of unprocessed tokens waiting via <see cref="RateLimiter.WaitAsync(int, CancellationToken)"/>.</param>
        /// <param name="replenishmentPeriod">
        /// Specifies how often tokens can be replenished. Replenishing is triggered either by an internal timer if <paramref name="autoReplenishment"/> is true, or by calling <see cref="TokenBucketRateLimiter.TryReplenish"/>.
        /// </param>
        /// <param name="tokensPerPeriod">Specified how many tokens can be added to the token bucket on a successful replenish. Available token count will not exceed <paramref name="tokenLimit"/>.</param>
        /// <param name="autoReplenishment">
        /// Specifies whether token replenishment will be handled by the <see cref="TokenBucketRateLimiter"/> or by another party via <see cref="TokenBucketRateLimiter.TryReplenish"/>.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">When <paramref name="tokenLimit"/>, <paramref name="queueLimit"/>, or <paramref name="tokensPerPeriod"/> are less than 0.</exception>
        public TokenBucketRateLimiterOptions(
            int tokenLimit,
            QueueProcessingOrder queueProcessingOrder,
            int queueLimit,
            TimeSpan replenishmentPeriod,
            int tokensPerPeriod,
            bool autoReplenishment = true)
        {
            if (tokenLimit < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tokenLimit));
            }
            if (queueLimit < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(queueLimit));
            }
            if (tokensPerPeriod <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tokensPerPeriod));
            }

            TokenLimit = tokenLimit;
            QueueProcessingOrder = queueProcessingOrder;
            QueueLimit = queueLimit;
            ReplenishmentPeriod = replenishmentPeriod;
            TokensPerPeriod = tokensPerPeriod;
            AutoReplenishment = autoReplenishment;
        }

        /// <summary>
        /// Specifies the minimum period between replenishments.
        /// </summary>
        public TimeSpan ReplenishmentPeriod { get; }

        /// <summary>
        /// Specifies the maximum number of tokens to restore each replenishment.
        /// </summary>
        public int TokensPerPeriod { get; }

        /// <summary>
        /// Specified whether the <see cref="TokenBucketRateLimiter"/> is automatically replenishing tokens or if someone else
        /// will be calling <see cref="TokenBucketRateLimiter.TryReplenish"/> to replenish tokens.
        /// </summary>
        public bool AutoReplenishment { get; }

        /// <summary>
        /// Maximum number of tokens that can be in the bucket at any time.
        /// </summary>
        public int TokenLimit { get; }

        /// <summary>
        /// Determines the behaviour of <see cref="RateLimiter.WaitAsync"/> when not enough resources can be leased.
        /// </summary>
        /// <value>
        /// <see cref="QueueProcessingOrder.OldestFirst"/> by default.
        /// </value>
        public QueueProcessingOrder QueueProcessingOrder { get; }

        /// <summary>
        /// Maximum cumulative token count of queued acquisition requests.
        /// </summary>
        public int QueueLimit { get; }
    }
}
