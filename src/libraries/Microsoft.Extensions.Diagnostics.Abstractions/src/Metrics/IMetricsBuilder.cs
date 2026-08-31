// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    /// <summary>
    /// Configures the metrics system by registering IMetricsListeners and using rules
    /// to determine which metrics are enabled.
    /// </summary>
    public interface IMetricsBuilder
    {
        /// <summary>
        /// Gets the application service collection that's used by extension methods to register services.
        /// </summary>
        IServiceCollection Services { get; }
    }
}
