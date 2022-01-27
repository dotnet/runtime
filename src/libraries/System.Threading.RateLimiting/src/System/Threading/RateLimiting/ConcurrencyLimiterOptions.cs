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
        /// Initializes the <see cref="ConcurrencyLimiterOptions"/>.
        /// </summary>
        /// <param name="permitLimit">Maximum number of permits that can be leased concurrently.</param>
        /// <param name="queueProcessingOrder">Determines the behaviour of <see cref="RateLimiter.WaitAsync"/> when not enough resources can be leased.</param>
        /// <param name="queueLimit">Maximum number of permits that can be queued concurrently.</param>
        /// <exception cref="ArgumentOutOfRangeException">When <paramref name="permitLimit"/> or <paramref name="queueLimit"/> are less than 0.</exception>
        public ConcurrencyLimiterOptions(int permitLimit, QueueProcessingOrder queueProcessingOrder, int queueLimit)
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
        }

        /// <summary>
        /// Maximum number of permits that can be leased concurrently.
        /// </summary>
        public int PermitLimit { get; }

        /// <summary>
        /// Determines the behaviour of <see cref="RateLimiter.WaitAsync"/> when not enough resources can be leased.
        /// </summary>
        /// <value>
        /// <see cref="QueueProcessingOrder.OldestFirst"/> by default.
        /// </value>
        public QueueProcessingOrder QueueProcessingOrder { get; } = QueueProcessingOrder.OldestFirst;

        /// <summary>
        /// Maximum number of permits that can be queued concurrently.
        /// </summary>
        public int QueueLimit { get; }
    }
}
