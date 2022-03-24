// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// A program abstraction.
    /// </summary>
    public interface IHost : IDisposable
    {
        /// <summary>
        /// Gets the services configured for the program (for example, using <see cref="M:HostBuilder.ConfigureServices(Action&lt;HostBuilderContext,IServiceCollection&gt;)" />).
        /// </summary>
        IServiceProvider Services { get; }

        /// <summary>
        /// Starts the <see cref="IHostedService" /> objects configured for the program.
        /// The application will run until interrupted or until <see cref="M:IHostApplicationLifetime.StopApplication()" /> is called.
        /// </summary>
        /// <param name="cancellationToken">Used to abort program start.</param>
        /// <returns>A <see cref="Task"/> that will be completed when the <see cref="IHost"/> starts.</returns>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Attempts to gracefully stop the program.
        /// </summary>
        /// <param name="cancellationToken">Used to indicate when stop should no longer be graceful.</param>
        /// <returns>A <see cref="Task"/> that will be completed when the <see cref="IHost"/> stops.</returns>
        Task StopAsync(CancellationToken cancellationToken = default);
    }
}
