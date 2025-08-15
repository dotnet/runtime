// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Threading;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Options for <see cref="IHost"/>.
    /// </summary>
    public class HostOptions
    {
        /// <summary>
        /// Gets or sets the default timeout for <see cref="IHost.StopAsync(CancellationToken)"/>.
        /// </summary>
        /// <remarks>
        /// This timeout also encompasses all host services implementing
        /// <see cref="IHostedLifecycleService.StoppingAsync(CancellationToken)"/> and
        /// <see cref="IHostedLifecycleService.StoppedAsync(CancellationToken)"/>.
        /// </remarks>
        public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the default timeout for <see cref="IHost.StartAsync(CancellationToken)"/>.
        /// </summary>
        /// <remarks>
        /// This timeout also encompasses all host services implementing
        /// <see cref="IHostedLifecycleService.StartingAsync(CancellationToken)"/> and
        /// <see cref="IHostedLifecycleService.StartedAsync(CancellationToken)"/>.
        /// </remarks>
        public TimeSpan StartupTimeout { get; set; } = Timeout.InfiniteTimeSpan;

        /// <summary>
        /// Gets or sets a value that indicates if the <see cref="IHost"/> will start registered instances of <see cref="IHostedService"/> concurrently or sequentially.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the <see cref="IHost"/> will start registered instances of <see cref="IHostedService"/> concurrently; <see langword="false"/> if the <see cref="IHost"/> will start registered instances sequentially. The default is <see langword="false"/> .
        /// </value>
        public bool ServicesStartConcurrently { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates if the <see cref="IHost"/> will stop registered instances of <see cref="IHostedService"/> concurrently or sequentially.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the <see cref="IHost"/> will stop registered instances of <see cref="IHostedService"/> concurrently; <see langword="false"/> if the <see cref="IHost"/> will stop registered instances sequentially. The default is <see langword="false"/> .
        /// </value>
        public bool ServicesStopConcurrently { get; set; }

        /// <summary>
        /// Gets or sets the behavior the <see cref="IHost"/> will follow when any of
        /// its <see cref="BackgroundService"/> instances throw an unhandled exception.
        /// </summary>
        /// <remarks>
        /// Defaults to <see cref="BackgroundServiceExceptionBehavior.StopHost"/>.
        /// </remarks>
        public BackgroundServiceExceptionBehavior BackgroundServiceExceptionBehavior { get; set; } =
            BackgroundServiceExceptionBehavior.StopHost;

        internal void Initialize(IConfiguration configuration)
        {
            var timeoutSeconds = configuration["shutdownTimeoutSeconds"];
            if (!string.IsNullOrEmpty(timeoutSeconds)
                && int.TryParse(timeoutSeconds, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds))
            {
                ShutdownTimeout = TimeSpan.FromSeconds(seconds);
            }

            timeoutSeconds = configuration["startupTimeoutSeconds"];
            if (!string.IsNullOrEmpty(timeoutSeconds)
                && int.TryParse(timeoutSeconds, NumberStyles.None, CultureInfo.InvariantCulture, out seconds))
            {
                StartupTimeout = TimeSpan.FromSeconds(seconds);
            }

            var servicesStartConcurrently = configuration["servicesStartConcurrently"];
            if (!string.IsNullOrEmpty(servicesStartConcurrently)
                && bool.TryParse(servicesStartConcurrently, out bool startBehavior))
            {
                ServicesStartConcurrently = startBehavior;
            }

            var servicesStopConcurrently = configuration["servicesStopConcurrently"];
            if (!string.IsNullOrEmpty(servicesStopConcurrently)
                && bool.TryParse(servicesStopConcurrently, out bool stopBehavior))
            {
                ServicesStopConcurrently = stopBehavior;
            }
        }
    }
}
