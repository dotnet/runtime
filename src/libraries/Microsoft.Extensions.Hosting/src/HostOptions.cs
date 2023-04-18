// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Options for <see cref="IHost"/>
    /// </summary>
    public class HostOptions
    {
        /// <summary>
        /// The default timeout for <see cref="IHost.StopAsync(System.Threading.CancellationToken)"/>.
        /// </summary>
        public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Determines if the <see cref="IHost"/> will start registered instances of <see cref="IHostedService"/> concurrently or sequentially. Defaults to false.
        /// </summary>
        public bool ServicesStartConcurrently { get; set; }

        /// <summary>
        /// Determines if the <see cref="IHost"/> will stop registered instances of <see cref="IHostedService"/> concurrently or sequentially. Defaults to false.
        /// </summary>
        public bool ServicesStopConcurrently { get; set; }

        /// <summary>
        /// The behavior the <see cref="IHost"/> will follow when any of
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
