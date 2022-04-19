// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Specifies a behavior that the <see cref="IHost"/> will honor when stopping registered instances of <see cref="IHostedService"/>
    /// </summary>
    public enum BackgroundServiceStopBehavior
    {
        /// <summary>
        /// Stops each <see cref="IHostedService"/> synchronously sequentially in first in last out order.
        /// </summary>
        /// <remarks>
        /// The <see cref="HostOptions.ShutdownTimeout"/> is based on an accumulation of all stop times for all <see cref="IHostedService"/> instances.
        /// </remarks>
        Sequential,

        /// <summary>
        /// Stops each <see cref="IHostedService"/> asynchronously.
        /// </summary>
        Asynchronous
    }
}
