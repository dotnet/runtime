// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    public static partial class MetricsBuilderExtensions
    {
        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder, string? meterName)
            => builder.ConfigureRule(options => options.EnableMetrics(meterName));

        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder, string? meterName = null, string? instrumentName = null, string? listenerName = null,
            MeterScope scopes = MeterScope.Global | MeterScope.Local)
            => builder.ConfigureRule(options => options.EnableMetrics(meterName, instrumentName, listenerName, scopes));

        public static MetricsOptions EnableMetrics(this MetricsOptions options, string? meterName)
            => options.EnableMetrics(meterName: meterName, instrumentName: null);

        public static MetricsOptions EnableMetrics(this MetricsOptions options, string? meterName = null, string? instrumentName = null, string? listenerName = null,
            MeterScope scopes = MeterScope.Global | MeterScope.Local)
            => options.AddRule(meterName, instrumentName, listenerName, scopes, enable: true);

        public static IMetricsBuilder DisableMetrics(this IMetricsBuilder builder, string? meterName)
            => builder.ConfigureRule(options => options.DisableMetrics(meterName));

        public static IMetricsBuilder DisableMetrics(this IMetricsBuilder builder, string? meterName = null, string? instrumentName = null, string? listenerName = null,
            MeterScope scopes = MeterScope.Global | MeterScope.Local)
            => builder.ConfigureRule(options => options.DisableMetrics(meterName, instrumentName, listenerName, scopes));

        public static MetricsOptions DisableMetrics(this MetricsOptions options, string? meterName)
            => options.DisableMetrics(meterName: meterName, instrumentName: null);

        public static MetricsOptions DisableMetrics(this MetricsOptions options, string? meterName = null, string? instrumentName = null, string? listenerName = null,
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
            if (scopes == MeterScope.None)
            {
                throw new ArgumentOutOfRangeException(nameof(scopes), "The MeterScope must be Global, Local, or both.");
            }
            options.Rules.Add(new InstrumentRule(meterName, instrumentName, listenerName, scopes, enable));
            return options;
        }
    }
}
