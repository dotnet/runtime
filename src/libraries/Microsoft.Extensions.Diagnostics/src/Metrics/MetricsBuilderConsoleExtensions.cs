// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    /// <summary>
    /// IMetricsBuilder extension methods for console output.
    /// </summary>
    public static class MetricsBuilderConsoleExtensions
    {
        /// <summary>
        /// Enables console output for metrics for debugging purposes. This is not recommended for production use.
        /// </summary>
        /// <param name="builder">The metrics builder.</param>
        /// <returns>The original metrics builder for chaining.</returns>
        public static IMetricsBuilder AddDebugConsole(this IMetricsBuilder builder)
        {
            ThrowHelper.ThrowIfNull(builder);
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IMetricsListener, ConsoleMetricListener>());
            return builder;
        }
    }
}
