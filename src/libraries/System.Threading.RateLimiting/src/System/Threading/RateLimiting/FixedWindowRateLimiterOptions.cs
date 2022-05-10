// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading.RateLimiting
{
    /// <summary>
    /// Options to specify the behavior of a <see cref="FixedWindowRateLimiter"/>.
    /// </summary>
    public sealed class FixedWindowRateLimiterOptions
    {
        /// <summary>
        /// Initializes the <see cref="FixedWindowRateLimiterOptions"/>.
        /// </summary>
        /// <param name="permitLimit">Maximum number of requests that can be served in the window.</param>
        /// <param name="queueProcessingOrder"></param>
        /// <param name="queueLimit">Maximum number of unprocessed request counters waiting via <see cref="RateLimiter.WaitAsync(int, CancellationToken)"/>.</param>
        /// <param name="window">
        /// Specifies how often request counters can be replenished. Replenishing is triggered either by an internal timer if <paramref name="autoReplenishment"/> is true, or by calling <see cref="FixedWindowRateLimiter.TryReplenish"/>.
        /// </param>
        /// <param name="autoReplenishment">
        /// Specifies whether request replenishment will be handled by the <see cref="FixedWindowRateLimiter"/> or by another party via <see cref="FixedWindowRateLimiter.TryReplenish"/>.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">When <paramref name="permitLimit"/> or <paramref name="queueLimit"/> are less than 0. </exception>
        public FixedWindowRateLimiterOptions(
            int permitLimit,
            QueueProcessingOrder queueProcessingOrder,
            int queueLimit,
            TimeSpan window,
            bool autoReplenishment = true)
        {
            if (permitLimit < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(permitLimit));
            }
            if (queueLimit < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(queueLimit));
            }

            PermitLimit = permitLimit;
            QueueProcessingOrder = queueProcessingOrder;
            QueueLimit = queueLimit;
            Window = window;
            AutoReplenishment = autoReplenishment;
        }

        /// <summary>
        /// Specifies the time window that takes in the requests.
        /// </summary>
        public TimeSpan Window { get; }

        /// <summary>
        /// Specified whether the <see cref="FixedWindowRateLimiter"/> is automatically refresh counters or if someone else
        /// will be calling <see cref="FixedWindowRateLimiter.TryReplenish"/> to refresh counters.
        /// </summary>
        public bool AutoReplenishment { get; }

        /// <summary>
        /// Maximum number of permit counters that can be allowed in a window.
        /// </summary>
        public int PermitLimit { get; }

        /// <summary>
        /// Determines the behaviour of <see cref="RateLimiter.WaitAsync"/> when not enough resources can be leased.
        /// </summary>
        /// <value>
        /// <see cref="QueueProcessingOrder.OldestFirst"/> by default.
        /// </value>
        public QueueProcessingOrder QueueProcessingOrder { get; }

        /// <summary>
        /// Maximum cumulative permit count of queued acquisition requests.
        /// </summary>
        public int QueueLimit { get; }
    }
}
