// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading.RateLimiting
{
    /// <summary>
    /// Options to specify the behavior of a <see cref="ConcurrencyLimiter"/>.
    /// </summary>
    public sealed class ConcurrencyLimiterOptions
    {
        /// <summary>
        /// Maximum number of permits that can be leased concurrently.
        /// Must be set to a value > 0 by the time these options are passed to the constructor of <see cref="ConcurrencyLimiter"/>.
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
        /// Maximum number of permits that can be queued concurrently.
        /// Must be set to a value >= 0 by the time these options are passed to the constructor of <see cref="ConcurrencyLimiter"/>.
        /// </summary>
        public int QueueLimit { get; set; }
    }
}
