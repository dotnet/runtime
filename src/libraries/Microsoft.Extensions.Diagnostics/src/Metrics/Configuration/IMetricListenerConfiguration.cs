// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Diagnostics.Metrics.Configuration
{
    /// <summary>
    /// Used to retrieve the metrics configuration for the given T type of listener.
    /// </summary>
    /// <typeparam name="T">The type of metric listener.</typeparam>
    public interface IMetricListenerConfiguration<T>
    {
        /// <summary>
        /// The configuration for the given T type of listener.
        /// </summary>
        IConfiguration Configuration { get; }
    }
}
