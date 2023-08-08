// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    /// <summary>
    /// Extensions for <see cref="IMetricsBuilder"/> for enabling metrics based on <see cref="IConfiguration"/>.
    /// </summary>
    public static class MetricsBuilderConfigurationExtensions
    {
        /// <summary>
        /// Reads metrics configuration from the provided <see cref="IConfiguration"/> section and configures
        /// which <see cref="Meter"/>'s, <see cref="Instrument"/>'s, and <see cref="IMetricsListener"/>'s are enabled.
        /// </summary>
        /// <param name="builder">The <see cref="IMetricsBuilder"/>.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> section to load.</param>
        /// <returns>The original <see cref="IMetricsBuilder"/> for chaining.</returns>
        public static IMetricsBuilder AddConfiguration(this IMetricsBuilder builder, IConfiguration configuration)
        {
            // TODO:
            return builder;
        }
    }
}
