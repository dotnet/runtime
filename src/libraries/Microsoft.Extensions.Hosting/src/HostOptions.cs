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
        /// The behavior the <see cref="IHost"/> will follow when stopping registered instances of <see cref="IHostedService"/>
        /// </summary>
        /// <remarks>
        /// Defaults to <see cref="BackgroundServiceStopBehavior.Asynchronous"/>
        /// </remarks>
        public BackgroundServiceStopBehavior BackgroundServiceStopBehavior { get; set; } =
            BackgroundServiceStopBehavior.Asynchronous;

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
            if (!string.IsNullOrWhiteSpace(timeoutSeconds)
                && int.TryParse(timeoutSeconds, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds))
            {
                ShutdownTimeout = TimeSpan.FromSeconds(seconds);
            }

            var backgroundServiceStopBehavior = configuration["backgroundServiceStopBehavior"];
            if (!string.IsNullOrWhiteSpace(backgroundServiceStopBehavior)
                && Enum.TryParse<BackgroundServiceStopBehavior>(backgroundServiceStopBehavior, out var stopBehavior))
            {
                BackgroundServiceStopBehavior = stopBehavior;
            }

            var backgroundServiceExceptionBehavior = configuration["backgroundServiceExceptionBehavior"];
            if (!string.IsNullOrWhiteSpace(backgroundServiceExceptionBehavior)
                && Enum.TryParse<BackgroundServiceExceptionBehavior>(backgroundServiceExceptionBehavior, out var exceptionBehavior))
            {
                BackgroundServiceExceptionBehavior = exceptionBehavior;
            }
        }
    }
}
