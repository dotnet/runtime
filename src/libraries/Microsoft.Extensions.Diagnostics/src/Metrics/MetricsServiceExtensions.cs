// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    /// <summary>
    /// Extension methods for setting up metrics services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class MetricsServiceExtensions
    {
        /// <summary>
        /// Adds metrics services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddMetrics(this IServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.TryAddSingleton<IMeterFactory, DefaultMeterFactory>();

            return services;
        }
    }
}
