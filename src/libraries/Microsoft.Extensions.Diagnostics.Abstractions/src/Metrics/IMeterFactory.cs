// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Metrics;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    /// <summary>
    /// A factory for creating <see cref="Meter"/> instances.
    /// </summary>
    /// <remarks>
    /// Meter factories will be accountable for the following responsibilities:
    /// - Creating a new meter.
    /// - Attaching the factory instance as a scope to the Meter constructor for all created Meter objects.
    /// - Storing created meters in a cache and returning a cached instance if a meter with the same parameters (name, version, and tags) is requested.
    /// - Disposing of all cached Meter objects upon factory disposal.
    /// </remarks>
    public interface IMeterFactory : IDisposable
    {
        /// <summary>
        /// Creates a new <see cref="Meter"/> instance.
        /// </summary>
        /// <param name="options">The <see cref="MeterOptions"/> to use when creating the meter.</param>
        /// <returns>A new <see cref="Meter"/> instance.</returns>
        /// <remarks>
        /// The <see cref="Meter"/> instance returned by this method should be cached by the factory and returned for subsequent requests for a meter with the same parameters (name, version, and tags).
        /// </remarks>
        Meter Create(MeterOptions options);
    }
}
