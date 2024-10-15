// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
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

            services.AddOptions();

            services.TryAddSingleton<IMeterFactory, DefaultMeterFactory>();
            services.TryAddSingleton<MetricsSubscriptionManager>();
            // Make sure the subscription manager is started when the host starts.
            // The host will trigger options validation.
            services.AddOptions<NoOpOptions>().ValidateOnStart();
            // Make sure this is only registered/run once.
            services.TryAddSingleton<IConfigureOptions<NoOpOptions>, SubscriptionActivator>();

            services.TryAddSingleton<IMetricListenerConfigurationFactory, MetricListenerConfigurationFactory>();

            return services;
        }

        /// <summary>
        /// Adds metrics services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="configure">A callback to configure the <see cref="IMetricsBuilder"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddMetrics(this IServiceCollection services, Action<IMetricsBuilder> configure)
        {
            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            services.AddMetrics();

            var builder = new MetricsBuilder(services);
            configure(builder);

            return services;
        }

        private sealed class MetricsBuilder(IServiceCollection services) : IMetricsBuilder
        {
            public IServiceCollection Services { get; } = services;
        }

        private sealed class NoOpOptions { }

        private sealed class SubscriptionActivator(MetricsSubscriptionManager manager) : IConfigureOptions<NoOpOptions>
        {
            public void Configure(NoOpOptions options) => manager.Initialize();
        }
    }
}
