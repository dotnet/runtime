// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Allows consumers to be notified of application lifetime events. This interface is not intended to be user-replaceable.
    /// </summary>
    public interface IHostApplicationLifetime
    {
        /// <summary>
        /// Triggered when the application host has fully started.
        /// </summary>
        CancellationToken ApplicationStarted { get; }

        /// <summary>
        /// Triggered when the application host is starting a graceful shutdown.
        /// Shutdown will block until all callbacks registered on this token have completed.
        /// </summary>
        CancellationToken ApplicationStopping { get; }

        /// <summary>
        /// Triggered when the application host has completed a graceful shutdown.
        /// The application will not exit until all callbacks registered on this token have completed.
        /// </summary>
        CancellationToken ApplicationStopped { get; }

        /// <summary>
        /// Requests termination of the current application.
        /// </summary>
        void StopApplication();
    }
}
