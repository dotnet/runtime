// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace System.Threading.RateLimiting
{
    /// <summary>
    /// Represents a limiter type that users interact with to determine if an operation can proceed given a specific <typeparamref name="TResource"/>.
    /// </summary>
    /// <typeparam name="TResource">The resource type that is being limited.</typeparam>
    public abstract class PartitionedRateLimiter<TResource> : IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// An estimated count of available permits.
        /// </summary>
        /// <returns></returns>
        public abstract int GetAvailablePermits(TResource resource);

        /// <summary>
        /// Fast synchronous attempt to acquire permits.
        /// </summary>
        /// <remarks>
        /// Set <paramref name="permitCount"/> to 0 to get whether permits are exhausted.
        /// </remarks>
        /// <param name="resource">The resource to limit.</param>
        /// <param name="permitCount">Number of permits to try and acquire.</param>
        /// <returns>A successful or failed lease.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public RateLimitLease AttemptAcquire(TResource resource, int permitCount = 1)
        {
            if (permitCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(permitCount));
            }

            return AttemptAcquireCore(resource, permitCount);
        }

        /// <summary>
        /// Method that <see cref="PartitionedRateLimiter{TResource}"/> implementations implement for <see cref="AttemptAcquire"/>.
        /// </summary>
        /// <param name="resource">The resource to limit.</param>
        /// <param name="permitCount">Number of permits to try and acquire.</param>
        /// <returns></returns>
        protected abstract RateLimitLease AttemptAcquireCore(TResource resource, int permitCount);

        /// <summary>
        /// Wait until the requested permits are available or permits can no longer be acquired.
        /// </summary>
        /// <remarks>
        /// Set <paramref name="permitCount"/> to 0 to wait until permits are replenished.
        /// </remarks>
        /// <param name="resource">The resource to limit.</param>
        /// <param name="permitCount">Number of permits to try and acquire.</param>
        /// <param name="cancellationToken">Optional token to allow canceling a queued request for permits.</param>
        /// <returns>A task that completes when the requested permits are acquired or when the requested permits are denied.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public ValueTask<RateLimitLease> AcquireAsync(TResource resource, int permitCount = 1, CancellationToken cancellationToken = default)
        {
            if (permitCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(permitCount));
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return new ValueTask<RateLimitLease>(Task.FromCanceled<RateLimitLease>(cancellationToken));
            }

            return AcquireAsyncCore(resource, permitCount, cancellationToken);
        }

        /// <summary>
        /// Method that <see cref="PartitionedRateLimiter{TResource}"/> implementations implement for <see cref="AcquireAsync"/>.
        /// </summary>
        /// <param name="resource">The resource to limit.</param>
        /// <param name="permitCount">Number of permits to try and acquire.</param>
        /// <param name="cancellationToken">Optional token to allow canceling a queued request for permits.</param>
        /// <returns>A task that completes when the requested permits are acquired or when the requested permits are denied.</returns>
        protected abstract ValueTask<RateLimitLease> AcquireAsyncCore(TResource resource, int permitCount, CancellationToken cancellationToken);

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

        /// <summary>
        /// Translates PartitionedRateLimiter&lt;TOuter&gt; into the current <see cref="PartitionedRateLimiter{TResource}"/>
        /// using the <paramref name="keyAdapter"/> to translate <typeparamref name="TOuter"/> to <typeparamref name="TResource"/>.
        /// </summary>
        /// <typeparam name="TOuter">The type to translate into <typeparamref name="TResource"/>.</typeparam>
        /// <param name="keyAdapter">The function to be called every time a <typeparamref name="TOuter"/> is passed to
        /// PartitionedRateLimiter&lt;TOuter&gt;.Acquire(TOuter, int) or PartitionedRateLimiter&lt;TOuter&gt;.WaitAsync(TOuter, int, CancellationToken).</param>
        /// <remarks><see cref="PartitionedRateLimiter{TResource}.Dispose()"/> or <see cref="PartitionedRateLimiter{TResource}.DisposeAsync()"/> does not dispose the wrapped <see cref="PartitionedRateLimiter{TResource}"/>.</remarks>
        /// <returns>A new PartitionedRateLimiter&lt;TOuter&gt; that translates <typeparamref name="TOuter"/>
        /// to <typeparamref name="TResource"/> and calls the inner <see cref="PartitionedRateLimiter{TResource}"/>.</returns>
        public PartitionedRateLimiter<TOuter> TranslateKey<TOuter>(Func<TOuter, TResource> keyAdapter)
        {
            // REVIEW: Do we want to have an option to dispose the inner limiter?
            // Should the default be to dispose the inner limiter and have an option to not dispose it?
            // See Stream wrappers like SslStream for prior-art
            return new TranslatingLimiter<TResource, TOuter>(this, keyAdapter);
        }
    }
}
