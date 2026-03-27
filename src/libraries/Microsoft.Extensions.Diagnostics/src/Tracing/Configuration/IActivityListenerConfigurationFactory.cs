// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Diagnostics.Configuration
{
    /// <summary>
    /// Used to retrieve the tracing configuration for any listener name.
    /// </summary>
    public interface IActivityListenerConfigurationFactory
    {
        /// <summary>
        /// Gets the configuration for the given listener.
        /// </summary>
        /// <param name="listenerName">The name of listener.</param>
        /// <returns>The configuration for this listener type.</returns>
        IConfiguration GetConfiguration(string listenerName);
    }
}
