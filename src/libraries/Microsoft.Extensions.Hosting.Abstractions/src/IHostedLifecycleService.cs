// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Defines methods that are run before or after
    /// <see cref="IHostedService.StartAsync(CancellationToken)"/> and
    /// <see cref="IHostedService.StopAsync(CancellationToken)"/>.
    /// </summary>
    public interface IHostedLifecycleService : IHostedService
    {
        /// <summary>
        /// Triggered before <see cref="IHostedService.StartAsync(CancellationToken)"/>.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
        Task StartingAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Triggered after <see cref="IHostedService.StartAsync(CancellationToken)"/>.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
        Task StartedAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Triggered before <see cref="IHostedService.StopAsync(CancellationToken)"/>.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
        Task StoppingAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Triggered after <see cref="IHostedService.StopAsync(CancellationToken)"/>.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the stop process has been aborted.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
        Task StoppedAsync(CancellationToken cancellationToken);
    }
}
