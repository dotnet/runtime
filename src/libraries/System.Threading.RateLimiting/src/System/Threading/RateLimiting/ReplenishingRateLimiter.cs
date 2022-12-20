// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading.RateLimiting
{
    public abstract class ReplenishingRateLimiter : RateLimiter
    {
        /// <summary>
        /// Specifies how often the <see cref="ReplenishingRateLimiter"/> will replenish tokens.
        /// If <see cref="IsAutoReplenishing"/> is <see langword="false"/> then this is how often <see cref="TryReplenish"/> should be called.
        /// </summary>
        public abstract TimeSpan ReplenishmentPeriod { get; }

        /// <summary>
        /// Specifies if the <see cref="ReplenishingRateLimiter"/> is automatically replenishing
        /// its tokens or if it expects an external source to regularly call <see cref="TryReplenish"/>.
        /// </summary>
        public abstract bool IsAutoReplenishing { get; }

        /// <summary>
        /// Attempts to replenish tokens.
        /// </summary>
        /// <returns>
        /// Generally returns <see langword="false"/> if <see cref="IsAutoReplenishing"/> is enabled
        /// or if no tokens were replenished. Otherwise <see langword="true"/>.
        /// </returns>
        public abstract bool TryReplenish();
    }
}
