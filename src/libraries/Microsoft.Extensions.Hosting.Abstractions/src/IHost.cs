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
        /// The programs configured services.
        /// </summary>
        IServiceProvider Services { get; }

        /// <summary>
        /// Start the program.
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
