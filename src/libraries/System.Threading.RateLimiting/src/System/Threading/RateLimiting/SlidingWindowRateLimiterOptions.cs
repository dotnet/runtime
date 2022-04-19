// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading.RateLimiting
{
    /// <summary>
    /// Options to specify the behavior of a <see cref="SlidingWindowRateLimiter"/>.
    /// </summary>
    public sealed class SlidingWindowRateLimiterOptions
    {
        /// <summary>
        /// Initializes the <see cref="SlidingWindowRateLimiterOptions"/>.
        /// </summary>
        /// <param name="permitLimit">Maximum number of request counters that can be served in a window.</param>
        /// <param name="queueProcessingOrder"></param>
        /// <param name="queueLimit">Maximum number of unprocessed request counters waiting via <see cref="RateLimiter.WaitAsync(int, CancellationToken)"/>.</param>
        /// <param name="window">
        /// Specifies how often requests can be replenished. Replenishing is triggered either by an internal timer if <paramref name="autoReplenishment"/> is true, or by calling <see cref="SlidingWindowRateLimiter.TryReplenish"/>.
        /// </param>
        /// <param name="segmentsPerWindow">Specified how many segments a window can be divided into. The total requests a segment can serve cannot exceed the max limit.<paramref name="permitLimit"/>.</param>
        /// <param name="autoReplenishment">
        /// Specifies whether request replenishment will be handled by the <see cref="SlidingWindowRateLimiter"/> or by another party via <see cref="SlidingWindowRateLimiter.TryReplenish"/>.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">When <paramref name="permitLimit"/>, <paramref name="queueLimit"/>, or <paramref name="segmentsPerWindow"/> are less than 0. </exception>
        public SlidingWindowRateLimiterOptions(
            int permitLimit,
            QueueProcessingOrder queueProcessingOrder,
            int queueLimit,
            TimeSpan window,
            int segmentsPerWindow,
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
            if (segmentsPerWindow <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(segmentsPerWindow));
            }

            PermitLimit = permitLimit;
            QueueProcessingOrder = queueProcessingOrder;
            QueueLimit = queueLimit;
            Window = window;
            SegmentsPerWindow = segmentsPerWindow;
            AutoReplenishment = autoReplenishment;
        }

        /// <summary>
        /// Specifies the minimum period between replenishments.
        /// </summary>
        public TimeSpan Window { get; }

        /// <summary>
        /// Specifies the maximum number of segments a window is divided into.
        /// </summary>
        public int SegmentsPerWindow { get; }

        /// <summary>
        /// Specified whether the <see cref="SlidingWindowRateLimiter"/> is automatically replenishing request counters or if someone else
        /// will be calling <see cref="SlidingWindowRateLimiter.TryReplenish"/> to replenish tokens.
        /// </summary>
        public bool AutoReplenishment { get; }

        /// <summary>
        /// Maximum number of requests that can be served in a window.
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
