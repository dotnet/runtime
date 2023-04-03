// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading.RateLimiting
{
    /// <summary>
    /// Snapshot of statistics for a <see cref="RateLimiter"/>.
    /// </summary>
    public class RateLimiterStatistics
    {
        /// <summary>
        /// Initializes an instance of <see cref="RateLimiterStatistics"/>.
        /// </summary>
        public RateLimiterStatistics() { }

        /// <summary>
        /// Gets the number of permits currently available for the <see cref="RateLimiter"/>.
        /// </summary>
        public long CurrentAvailablePermits { get; init; }

        /// <summary>
        /// Gets the number of queued permits for the <see cref="RateLimiter"/>.
        /// </summary>
        public long CurrentQueuedCount { get; init; }

        /// <summary>
        /// Gets the total number of failed <see cref="RateLimitLease"/>s returned.
        /// </summary>
        public long TotalFailedLeases { get; init; }

        /// <summary>
        /// Gets the total number of successful <see cref="RateLimitLease"/>s returned.
        /// </summary>
        public long TotalSuccessfulLeases { get; init; }
    }
}
