// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

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
    }
}
