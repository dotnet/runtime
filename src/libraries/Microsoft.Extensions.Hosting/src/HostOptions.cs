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
        public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(5);

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
        }
    }
}
