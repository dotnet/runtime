// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    public static class MetricsBuilderEnableExtensions
    {
        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder, string? meterName)
            => builder.ConfigureRule(options => options.EnableMetrics(meterName));

        public static IMetricsBuilder EnableMetrics<T>(this IMetricsBuilder builder, string? meterName) where T : IMetricsListener
            => builder.ConfigureRule(options => options.EnableMetrics<T>(meterName));

        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder, string? meterName, string? instrumentName)
            => builder.ConfigureRule(options => options.EnableMetrics(meterName, instrumentName));

        public static IMetricsBuilder EnableMetrics<T>(this IMetricsBuilder builder, string? meterName, string? instrumentName) where T : IMetricsListener
            => builder.ConfigureRule(options => options.EnableMetrics<T>(meterName, instrumentName));

        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder, Func<Instrument, bool> filter)
            => builder.ConfigureRule(options => options.EnableMetrics(filter));

        public static IMetricsBuilder EnableMetrics<T>(this IMetricsBuilder builder, Func<Instrument, bool> filter) where T : IMetricsListener
            => builder.ConfigureRule(options => options.EnableMetrics<T>(filter));

        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder, string? meterName, MeterScope scopes)
            => builder.ConfigureRule(options => options.EnableMetrics(meterName, scopes));

        public static IMetricsBuilder EnableMetrics<T>(this IMetricsBuilder builder, string? meterName, MeterScope scopes) where T : IMetricsListener
            => builder.ConfigureRule(options => options.EnableMetrics<T>(meterName, scopes));

        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder, string? meterName, Func<Instrument, bool> filter)
            => builder.ConfigureRule(options => options.EnableMetrics(meterName, filter));

        public static IMetricsBuilder EnableMetrics<T>(this IMetricsBuilder builder, string? meterName, Func<Instrument, bool> filter) where T : IMetricsListener
            => builder.ConfigureRule(options => options.EnableMetrics<T>(meterName, filter));

        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder, string? meterName, MeterScope scopes, Func<Instrument, bool> filter)
            => builder.ConfigureRule(options => options.EnableMetrics(meterName, scopes, filter));

        public static IMetricsBuilder EnableMetrics<T>(this IMetricsBuilder builder, string? meterName, MeterScope scopes, Func<Instrument, bool> filter) where T : IMetricsListener
            => builder.ConfigureRule(options => options.EnableMetrics<T>(meterName, scopes, filter));

        public static MetricsEnableOptions EnableMetrics(this MetricsEnableOptions options, string? meterName) => options.AddRule(meterName: meterName);

        public static MetricsEnableOptions EnableMetrics<T>(this MetricsEnableOptions options, string? meterName) where T : IMetricsListener
            => options.AddRule(meterName: meterName, listenerName: typeof(T).FullName);

        public static MetricsEnableOptions EnableMetrics(this MetricsEnableOptions options, string? meterName, string? instrumentName)
            => options.AddRule(meterName: meterName, instrumentName: instrumentName);

        public static MetricsEnableOptions EnableMetrics<T>(this MetricsEnableOptions options, string? meterName, string? instrumentName) where T : IMetricsListener
            => options.AddRule(meterName: meterName, instrumentName: instrumentName, listenerName: typeof(T).FullName);

        public static MetricsEnableOptions EnableMetrics(this MetricsEnableOptions options, Func<Instrument, bool> filter)
            => options.AddRule(filter: (_, instrument) => filter(instrument));

        public static MetricsEnableOptions EnableMetrics<T>(this MetricsEnableOptions options, Func<Instrument, bool> filter) where T : IMetricsListener
            => options.AddRule(filter: (_, instrument) => filter(instrument), listenerName: typeof(T).FullName);

        public static MetricsEnableOptions EnableMetrics(this MetricsEnableOptions options, string? meterName, MeterScope scopes)
            => options.AddRule(meterName: meterName, scopes: scopes);

        public static MetricsEnableOptions EnableMetrics<T>(this MetricsEnableOptions options, string? meterName, MeterScope scopes) where T : IMetricsListener
            => options.AddRule(meterName: meterName, scopes: scopes, listenerName: typeof(T).FullName);

        public static MetricsEnableOptions EnableMetrics(this MetricsEnableOptions options, string? meterName, Func<Instrument, bool> filter)
            => options.AddRule(meterName: meterName, filter: (_, instrument) => filter(instrument));

        public static MetricsEnableOptions EnableMetrics<T>(this MetricsEnableOptions options, string? meterName, Func<Instrument, bool> filter) where T : IMetricsListener
            => options.AddRule(meterName: meterName, filter: (_, instrument) => filter(instrument), listenerName: typeof(T).FullName);

        public static MetricsEnableOptions EnableMetrics(this MetricsEnableOptions options, string? meterName, MeterScope scopes, Func<Instrument, bool> filter)
            => options.AddRule(meterName: meterName, scopes: scopes, filter: (_, instrument) => filter(instrument));

        public static MetricsEnableOptions EnableMetrics<T>(this MetricsEnableOptions options, string? meterName, MeterScope scopes, Func<Instrument, bool> filter)
            where T : IMetricsListener => options.AddRule(meterName: meterName, scopes: scopes, filter: (_, instrument) => filter(instrument), listenerName: typeof(T).FullName);

        private static IMetricsBuilder ConfigureRule(this IMetricsBuilder builder, Action<MetricsEnableOptions> configureOptions)
        {
            builder.Services.Configure(configureOptions);
            return builder;
        }

        private static MetricsEnableOptions AddRule(this MetricsEnableOptions options, string? meterName = null, MeterScope scopes = MeterScope.Local | MeterScope.Global,
            string? instrumentName = null, Func<string?, Instrument, bool>? filter = null, string? listenerName = null)
        {
            ThrowHelper.ThrowIfNull(options);
            options.Rules.Add(new InstrumentEnableRule(listenerName, meterName, scopes, instrumentName, filter));
            return options;
        }
    }
}
