// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    public static class MetricsBuilderEnableExtensions
    {
        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder)
            => builder.ConfigureRule(options => options.EnableMetrics());

        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder, string? meterName)
            => builder.ConfigureRule(options => options.EnableMetrics(meterName));

        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder, string? meterName, string? instrumentName)
            => builder.ConfigureRule(options => options.EnableMetrics(meterName, instrumentName));

        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder, string? meterName, string? instrumentName, string? listenerName)
            => builder.ConfigureRule(options => options.EnableMetrics(meterName, instrumentName, listenerName));

        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder, string? meterName, string? instrumentName, string? listenerName, MeterScope scopes)
            => builder.ConfigureRule(options => options.EnableMetrics(meterName, instrumentName, listenerName, scopes));

        public static MetricsEnableOptions EnableMetrics(this MetricsEnableOptions options)
            => options.AddRule();

        public static MetricsEnableOptions EnableMetrics(this MetricsEnableOptions options, string? meterName)
            => options.AddRule(meterName: meterName);

        public static MetricsEnableOptions EnableMetrics(this MetricsEnableOptions options, string? meterName, string? instrumentName)
            => options.AddRule(meterName: meterName, instrumentName: instrumentName);

        public static MetricsEnableOptions EnableMetrics(this MetricsEnableOptions options, string? meterName, string? instrumentName, string? listenerName)
            => options.AddRule(meterName, instrumentName, listenerName);

        public static MetricsEnableOptions EnableMetrics(this MetricsEnableOptions options, string? meterName, string? instrumentName, string? listenerName, MeterScope scopes)
            => options.AddRule(meterName, instrumentName, listenerName, scopes);

        // TODO: How many overloads of this do we want?
        public static MetricsEnableOptions DisableMetrics(this MetricsEnableOptions options, string? meterName, string? instrumentName, string? listenerName, MeterScope scopes)
            => options.AddRule(meterName, instrumentName, listenerName, scopes, enable: false);

        private static IMetricsBuilder ConfigureRule(this IMetricsBuilder builder, Action<MetricsEnableOptions> configureOptions)
        {
            builder.Services.Configure(configureOptions);
            return builder;
        }

        private static MetricsEnableOptions AddRule(this MetricsEnableOptions options, string? meterName = null, string? instrumentName = null, string? listenerName = null,
            MeterScope scopes = MeterScope.Local | MeterScope.Global, bool? enable = true)
        {
            ThrowHelper.ThrowIfNull(options);
            options.Rules.Add(new InstrumentEnableRule(meterName, instrumentName, listenerName, scopes, enable ?? true));
            return options;
        }
    }
}
