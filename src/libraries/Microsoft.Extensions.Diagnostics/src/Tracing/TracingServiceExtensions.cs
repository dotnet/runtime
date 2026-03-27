// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for setting up tracing services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class TracingServiceExtensions
    {
        /// <summary>
        /// Adds tracing services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddTracing(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddOptions();

            services.TryAddSingleton<IActivitySourceFactory, DefaultActivitySourceFactory>();
            services.TryAddSingleton<IActivityListenerConfigurationFactory, ActivityListenerConfigurationFactory>();

            return services;
        }

        /// <summary>
        /// Adds tracing services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="configure">A callback to configure the <see cref="ITracingBuilder"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddTracing(this IServiceCollection services, Action<ITracingBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);

            services.AddTracing();

            var builder = new TracingBuilder(services);
            configure(builder);

            return services;
        }

        private sealed class TracingBuilder(IServiceCollection services) : ITracingBuilder
        {
            public IServiceCollection Services { get; } = services;
        }
    }
}
