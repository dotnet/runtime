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
        /// Specifies the minimum period between replenishments.
        /// Must be set to a value >= <see cref="TimeSpan.Zero" /> by the time these options are passed to the constructor of <see cref="SlidingWindowRateLimiter"/>.
        /// </summary>
        public TimeSpan Window { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Specifies the maximum number of segments a window is divided into.
        /// Must be set to a value > 0 by the time these options are passed to the constructor of <see cref="SlidingWindowRateLimiter"/>.
        /// </summary>
        public int SegmentsPerWindow { get; set; }

        /// <summary>
        /// Specified whether the <see cref="SlidingWindowRateLimiter"/> is automatically replenishing request counters or if someone else
        /// will be calling <see cref="SlidingWindowRateLimiter.TryReplenish"/> to replenish tokens.
        /// </summary>
        /// <value>
        /// <see langword="true" /> by default.
        /// </value>
        public bool AutoReplenishment { get; set; } = true;

        /// <summary>
        /// Maximum number of requests that can be served in a window.
        /// Must be set to a value > 0 by the time these options are passed to the constructor of <see cref="SlidingWindowRateLimiter"/>.
        /// </summary>
        public int PermitLimit { get; set; }

        /// <summary>
        /// Determines the behaviour of <see cref="RateLimiter.AcquireAsync"/> when not enough resources can be leased.
        /// </summary>
        /// <value>
        /// <see cref="QueueProcessingOrder.OldestFirst"/> by default.
        /// </value>
        public QueueProcessingOrder QueueProcessingOrder { get; set; } = QueueProcessingOrder.OldestFirst;

        /// <summary>
        /// Maximum cumulative permit count of queued acquisition requests.
        /// Must be set to a value >= 0 by the time these options are passed to the constructor of <see cref="SlidingWindowRateLimiter"/>.
        /// </summary>
        public int QueueLimit { get; set; }
    }
}
