// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Diagnostics.Tracing
{
    /// <summary>
    /// Resolves an <see cref="IConfiguration"/> view for a named <see cref="ActivityListener"/>.
    /// </summary>
    /// <remarks>
    /// Implementations merge every <see cref="IConfiguration"/> section registered through
    /// <see cref="TracingBuilderConfigurationExtensions.AddConfiguration"/> that targets the supplied listener name,
    /// returning a single merged <see cref="IConfiguration"/> instance per call.
    /// </remarks>
    public abstract class ActivityListenerConfigurationFactory
    {
        /// <summary>
        /// Gets the merged <see cref="IConfiguration"/> for the listener identified by <paramref name="listenerName"/>.
        /// </summary>
        /// <param name="listenerName">The name of the listener whose configuration is requested.</param>
        /// <returns>An <see cref="IConfiguration"/> that aggregates every section registered for <paramref name="listenerName"/>.</returns>
        public abstract IConfiguration GetConfiguration(string listenerName);
    }
}
