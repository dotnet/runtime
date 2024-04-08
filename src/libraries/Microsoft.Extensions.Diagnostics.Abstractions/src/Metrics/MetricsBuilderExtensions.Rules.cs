// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    /// <summary>
    /// Extension methods for <see cref="IMetricsBuilder"/> to add or clear <see cref="IMetricsListener"/> registrations, and to enable or disable metrics.
    /// </summary>
    public static partial class MetricsBuilderExtensions
    {
        /// <summary>
        /// Enables all <see cref="Instrument"/>'s for the given meter, for all registered <see cref="IMetricsListener"/>'s.
        /// </summary>
        /// <param name="builder">The <see cref="IMetricsBuilder"/>.</param>
        /// <param name="meterName">The <see cref="Meter.Name"/> or prefix. A null value matches all meters.</param>
        /// <returns>The original <see cref="IMetricsBuilder"/> for chaining.</returns>
        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder, string? meterName)
            => builder.ConfigureRule(options => options.EnableMetrics(meterName));

        /// <summary>
        /// Enables a specified <see cref="Instrument"/> for the given <see cref="Meter"/> and <see cref="IMetricsListener"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IMetricsBuilder"/>.</param>
        /// <param name="meterName">The <see cref="Meter.Name"/> or prefix. A null value matches all meters.</param>
        /// <param name="instrumentName">The <see cref="Instrument.Name"/>. A null value matches all instruments.</param>
        /// <param name="listenerName">The <see cref="IMetricsListener"/>.Name. A null value matches all listeners.</param>
        /// <param name="scopes">Indicates which <see cref="MeterScope"/>'s to consider. Default to all scopes.</param>
        /// <returns>The original <see cref="IMetricsBuilder"/> for chaining.</returns>
        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder, string? meterName, string? instrumentName = null, string? listenerName = null,
            MeterScope scopes = MeterScope.Global | MeterScope.Local)
            => builder.ConfigureRule(options => options.EnableMetrics(meterName, instrumentName, listenerName, scopes));

        /// <summary>
        /// Enables all <see cref="Instrument"/>'s for the given meter, for all registered <see cref="IMetricsListener"/>'s.
        /// </summary>
        /// <param name="options">The <see cref="MetricsOptions"/>.</param>
        /// <param name="meterName">The <see cref="Meter.Name"/> or prefix. A null value matches all meters.</param>
        /// <returns>The original <see cref="MetricsOptions"/> for chaining.</returns>
        public static MetricsOptions EnableMetrics(this MetricsOptions options, string? meterName)
            => options.EnableMetrics(meterName: meterName, instrumentName: null);

        /// <summary>
        /// Enables a specified <see cref="Instrument"/> for the given <see cref="Meter"/> and <see cref="IMetricsListener"/>.
        /// </summary>
        /// <param name="options">The <see cref="MetricsOptions"/>.</param>
        /// <param name="meterName">The <see cref="Meter.Name"/> or prefix. A null value matches all meters.</param>
        /// <param name="instrumentName">The <see cref="Instrument.Name"/>. A null value matches all instruments.</param>
        /// <param name="listenerName">The <see cref="IMetricsListener"/>.Name. A null value matches all listeners.</param>
        /// <param name="scopes">Indicates which <see cref="MeterScope"/>'s to consider. Default to all scopes.</param>
        /// <returns>The original <see cref="MetricsOptions"/> for chaining.</returns>
        public static MetricsOptions EnableMetrics(this MetricsOptions options, string? meterName, string? instrumentName = null, string? listenerName = null,
            MeterScope scopes = MeterScope.Global | MeterScope.Local)
            => options.AddRule(meterName, instrumentName, listenerName, scopes, enable: true);

        /// <summary>
        /// Disables all <see cref="Instrument"/>'s for the given meter, for all registered <see cref="IMetricsListener"/>'s.
        /// </summary>
        /// <param name="builder">The <see cref="IMetricsBuilder"/>.</param>
        /// <param name="meterName">The <see cref="Meter.Name"/> or prefix. A null value matches all meters.</param>
        /// <returns>The original <see cref="IMetricsBuilder"/> for chaining.</returns>
        public static IMetricsBuilder DisableMetrics(this IMetricsBuilder builder, string? meterName)
            => builder.ConfigureRule(options => options.DisableMetrics(meterName));

        /// <summary>
        /// Disables a specified <see cref="Instrument"/> for the given <see cref="Meter"/> and <see cref="IMetricsListener"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IMetricsBuilder"/>.</param>
        /// <param name="meterName">The <see cref="Meter.Name"/> or prefix. A null value matches all meters.</param>
        /// <param name="instrumentName">The <see cref="Instrument.Name"/>. A null value matches all instruments.</param>
        /// <param name="listenerName">The <see cref="IMetricsListener"/>.Name. A null value matches all listeners.</param>
        /// <param name="scopes">Indicates which <see cref="MeterScope"/>'s to consider. Default to all scopes.</param>
        /// <returns>The original <see cref="IMetricsBuilder"/> for chaining.</returns>
        public static IMetricsBuilder DisableMetrics(this IMetricsBuilder builder, string? meterName, string? instrumentName = null, string? listenerName = null,
            MeterScope scopes = MeterScope.Global | MeterScope.Local)
            => builder.ConfigureRule(options => options.DisableMetrics(meterName, instrumentName, listenerName, scopes));

        /// <summary>
        /// Disables all <see cref="Instrument"/>'s for the given meter, for all registered <see cref="IMetricsListener"/>'s.
        /// </summary>
        /// <param name="options">The <see cref="MetricsOptions"/>.</param>
        /// <param name="meterName">The <see cref="Meter.Name"/> or prefix. A null value matches all meters.</param>
        /// <returns>The original <see cref="MetricsOptions"/> for chaining.</returns>
        public static MetricsOptions DisableMetrics(this MetricsOptions options, string? meterName)
            => options.DisableMetrics(meterName: meterName, instrumentName: null);

        /// <summary>
        /// Disables a specified <see cref="Instrument"/> for the given <see cref="Meter"/> and <see cref="IMetricsListener"/>.
        /// </summary>
        /// <param name="options">The <see cref="MetricsOptions"/>.</param>
        /// <param name="meterName">The <see cref="Meter.Name"/> or prefix. A null value matches all meters.</param>
        /// <param name="instrumentName">The <see cref="Instrument.Name"/>. A null value matches all instruments.</param>
        /// <param name="listenerName">The <see cref="IMetricsListener"/>.Name. A null value matches all listeners.</param>
        /// <param name="scopes">Indicates which <see cref="MeterScope"/>'s to consider. Default to all scopes.</param>
        /// <returns>The original <see cref="MetricsOptions"/> for chaining.</returns>
        public static MetricsOptions DisableMetrics(this MetricsOptions options, string? meterName, string? instrumentName = null, string? listenerName = null,
            MeterScope scopes = MeterScope.Global | MeterScope.Local)
            => options.AddRule(meterName, instrumentName, listenerName, scopes, enable: false);

        private static IMetricsBuilder ConfigureRule(this IMetricsBuilder builder, Action<MetricsOptions> configureOptions)
        {
            ThrowHelper.ThrowIfNull(builder);
            builder.Services.Configure(configureOptions);
            return builder;
        }

        private static MetricsOptions AddRule(this MetricsOptions options, string? meterName, string? instrumentName, string? listenerName,
            MeterScope scopes, bool enable)
        {
            ThrowHelper.ThrowIfNull(options);
            options.Rules.Add(new InstrumentRule(meterName, instrumentName, listenerName, scopes, enable));
            return options;
        }
    }
}
