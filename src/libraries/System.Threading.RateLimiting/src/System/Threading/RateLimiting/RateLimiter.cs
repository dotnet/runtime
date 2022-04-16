// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace System.Threading.RateLimiting
{
    /// <summary>
    /// Represents a limiter type that users interact with to determine if an operation can proceed.
    /// </summary>
    public abstract class RateLimiter : IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// An estimated count of available permits.
        /// </summary>
        /// <returns></returns>
        public abstract int GetAvailablePermits();

        /// <summary>
        /// Specifies how long the <see cref="RateLimiter"/> has had all permits available. Used by RateLimiter managers that may want to
        /// clean up unused RateLimiters.
        /// </summary>
        /// <remarks>
        /// Returns <see langword="null"/> when the <see cref="RateLimiter"/> is in use or is not ready to be idle.
        /// </remarks>
        public abstract TimeSpan? IdleDuration { get; }

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

            if (cancellationToken.IsCancellationRequested)
            {
                return new ValueTask<RateLimitLease>(Task.FromCanceled<RateLimitLease>(cancellationToken));
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

        /// <summary>
        /// Dispose method for implementations to write.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing) { }

        /// <summary>
        /// Disposes the RateLimiter. This completes any queued acquires with a failed lease.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// DisposeAsync method for implementations to write.
        /// </summary>
        protected virtual ValueTask DisposeAsyncCore()
        {
            return default;
        }

        /// <summary>
        /// Disposes the RateLimiter asynchronously.
        /// </summary>
        /// <returns>ValueTask representing the completion of the disposal.</returns>
        public async ValueTask DisposeAsync()
        {
            // Perform async cleanup.
            await DisposeAsyncCore().ConfigureAwait(false);

            // Dispose of unmanaged resources.
            Dispose(false);

            // Suppress finalization.
            GC.SuppressFinalize(this);
        }
    }
}
