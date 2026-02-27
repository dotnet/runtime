// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// An <see cref="IServiceScope" /> implementation that implements <see cref="IAsyncDisposable" />.
    /// </summary>
    [DebuggerDisplay("{ServiceProvider,nq}")]
    public readonly struct AsyncServiceScope : IServiceScope, IAsyncDisposable
    {
        private readonly IServiceScope _serviceScope;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncServiceScope"/> struct.
        /// Wraps an instance of <see cref="IServiceScope" />.
        /// </summary>
        /// <param name="serviceScope">The <see cref="IServiceScope"/> instance to wrap.</param>
        public AsyncServiceScope(IServiceScope serviceScope)
        {
            ArgumentNullException.ThrowIfNull(serviceScope);

            _serviceScope = serviceScope;
        }

        /// <inheritdoc />
        public IServiceProvider ServiceProvider => _serviceScope.ServiceProvider;

        /// <summary>
        /// Ends the scope lifetime and disposes all resolved services.
        /// </summary>
        /// <remarks>
        /// Prefer calling <see cref="DisposeAsync"/> over this method. If any resolved service implements
        /// <see cref="IAsyncDisposable"/> but not <see cref="IDisposable"/>, this method throws an
        /// <see cref="InvalidOperationException"/> (or an <see cref="AggregateException"/> if multiple such
        /// services are resolved). Use <see cref="DisposeAsync"/> to properly handle all disposable services,
        /// or explicitly perform sync-over-async on the caller side if synchronous disposal is required.
        /// </remarks>
        /// <exception cref="InvalidOperationException">A resolved service implements <see cref="IAsyncDisposable"/> but not <see cref="IDisposable"/>.</exception>
        /// <exception cref="AggregateException">Multiple resolved services implement <see cref="IAsyncDisposable"/> but not <see cref="IDisposable"/>.</exception>
        public void Dispose()
        {
            _serviceScope.Dispose();
        }

        /// <summary>
        /// Asynchronously ends the scope lifetime and disposes all resolved services that implement <see cref="IDisposable"/> or <see cref="IAsyncDisposable"/>.
        /// </summary>
        /// <remarks>
        /// This is the preferred disposal method. When the underlying scope implements <see cref="IAsyncDisposable"/>,
        /// this method handles services that implement only <see cref="IAsyncDisposable"/> without throwing.
        /// When it does not, this method falls back to calling <see cref="Dispose"/>.
        /// </remarks>
        /// <returns>A value task that represents the asynchronous operation.</returns>
        public ValueTask DisposeAsync()
        {
            if (_serviceScope is IAsyncDisposable ad)
            {
                return ad.DisposeAsync();
            }
            _serviceScope.Dispose();

            // ValueTask.CompletedTask is only available in net5.0 and later.
            return default;
        }
    }
}
