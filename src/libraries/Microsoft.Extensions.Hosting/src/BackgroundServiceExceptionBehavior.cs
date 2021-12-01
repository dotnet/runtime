// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Specifies a behavior that the <see cref="IHost"/> will honor if an
    /// unhandled exception occurs in one of its <see cref="BackgroundService"/> instances.
    /// </summary>
    public enum BackgroundServiceExceptionBehavior
    {
        /// <summary>
        /// Stops the <see cref="IHost"/> instance.
        /// </summary>
        /// <remarks>
        /// If a <see cref="BackgroundService"/> throws an exception, the <see cref="IHost"/> instance stops, and the process continues.
        /// </remarks>
        StopHost = 0,

        /// <summary>
        /// Ignore exceptions thrown in <see cref="BackgroundService"/>.
        /// </summary>
        /// <remarks>
        /// If a <see cref="BackgroundService"/> throws an exception, the <see cref="IHost"/> will log the error, but otherwise ignore it.
        /// The <see cref="BackgroundService"/> is not restarted.
        /// </remarks>
        Ignore = 1
    }
}
