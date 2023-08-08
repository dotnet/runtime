// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Diagnostics.Metrics.Configuration
{
    /// <summary>
    /// Used to retrieve the metrics configuration for any type of listener.
    /// </summary>
    public interface IMetricListenerConfigurationFactory
    {
        /// <summary>
        /// Gets the configuration for the given type of listener.
        /// </summary>
        /// <param name="listenerType">The type of listener.</param>
        /// <returns>The configuration for this listener type.</returns>
        IConfiguration GetConfiguration(Type listenerType);
    }
}
