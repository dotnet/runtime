// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace System.Threading.RateLimiting
{
    /// <summary>
    /// Represents a limiter type that users interact with to determine if an operation can proceed.
    /// </summary>
    public abstract class RateLimiter
    {
        /// <summary>
        /// An estimated count of available permits.
        /// </summary>
        /// <returns></returns>
        public abstract int GetAvailablePermits();

        /// <summary>
        /// Fast synchronous attempt to acquire permits.
        /// </summary>
        /// <remarks>
        /// Set <paramref name="permitCount"/> to 0 to get whether permits are exhausted.
        /// </remarks>
        /// <param name="permitCount">Number of permits to try and acquire.</param>
        /// <returns>A successful or failed lease.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public RateLimitLease Acquire(int permitCount = 1)
        {
            if (permitCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(permitCount));
            }

            return AcquireCore(permitCount);
        }

        /// <summary>
        /// Method that <see cref="RateLimiter"/> implementations implement for <see cref="Acquire"/>.
        /// </summary>
        /// <param name="permitCount">Number of permits to try and acquire.</param>
        /// <returns></returns>
        protected abstract RateLimitLease AcquireCore(int permitCount);

        /// <summary>
        /// Wait until the requested permits are available or permits can no longer be acquired.
        /// </summary>
        /// <remarks>
        /// Set <paramref name="permitCount"/> to 0 to wait until permits are replenished.
        /// </remarks>
        /// <param name="permitCount">Number of permits to try and acquire.</param>
        /// <param name="cancellationToken">Optional token to allow canceling a queued request for permits.</param>
        /// <returns>A task that completes when the requested permits are acquired or when the requested permits are denied.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public ValueTask<RateLimitLease> WaitAsync(int permitCount = 1, CancellationToken cancellationToken = default)
        {
            if (permitCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(permitCount));
            }

            return WaitAsyncCore(permitCount, cancellationToken);
        }

        /// <summary>
        /// Method that <see cref="RateLimiter"/> implementations implement for <see cref="WaitAsync"/>.
        /// </summary>
        /// <param name="permitCount">Number of permits to try and acquire.</param>
        /// <param name="cancellationToken">Optional token to allow canceling a queued request for permits.</param>
        /// <returns>A task that completes when the requested permits are acquired or when the requested permits are denied.</returns>
        protected abstract ValueTask<RateLimitLease> WaitAsyncCore(int permitCount, CancellationToken cancellationToken);
    }
}
