// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Metrics;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    public static class MetricsBuilderEnableExtensions
    {
        // common overloads
        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder, string? meterName) => throw null!;
        public static IMetricsBuilder EnableMetrics<T>(this IMetricsBuilder builder, string? meterName) where T : IMetricsListener => throw null!;

        // less common overloads
        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder, string? meterName, string? instrumentName) => throw null!;
        public static IMetricsBuilder EnableMetrics<T>(this IMetricsBuilder builder, string? meterName, string? instrumentName) where T : IMetricsListener => throw null!;
        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder, Func<Instrument, bool> filter) => throw null!;
        public static IMetricsBuilder EnableMetrics<T>(this IMetricsBuilder builder, Func<Instrument, bool> filter) where T : IMetricsListener => throw null!;
        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder, string? meterName, MeterScope scopes) => throw null!;
        public static IMetricsBuilder EnableMetrics<T>(this IMetricsBuilder builder, string? meterName, MeterScope scopes) where T : IMetricsListener => throw null!;
        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder, string? meterName, Action<Instrument, bool> filter) => throw null!;
        public static IMetricsBuilder EnableMetrics<T>(this IMetricsBuilder builder, string? meterName, Action<Instrument, bool> filter) where T : IMetricsListener => throw null!;
        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder, string? meterName, MeterScope scopes, Action<Instrument, bool> filter) => throw null!;
        public static IMetricsBuilder EnableMetrics<T>(this IMetricsBuilder builder, string? meterName, MeterScope scopes, Action<Instrument, bool> filter) where T : IMetricsListener => throw null!;

        // all the same extension methods on MetricsEnableOptions
        public static MetricsEnableOptions EnableMetrics(this MetricsEnableOptions options, string? meterName) => throw null!;
        public static MetricsEnableOptions EnableMetrics<T>(this MetricsEnableOptions options, string? meterName) where T : IMetricsListener => throw null!;
        public static MetricsEnableOptions EnableMetrics(this MetricsEnableOptions options, string? meterName, string? instrumentName) => throw null!;
        public static MetricsEnableOptions EnableMetrics<T>(this MetricsEnableOptions options, string? meterName, string? instrumentName) where T : IMetricsListener => throw null!;
        public static MetricsEnableOptions EnableMetrics(this MetricsEnableOptions options, Func<Instrument, bool> filter) => throw null!;
        public static MetricsEnableOptions EnableMetrics<T>(this MetricsEnableOptions options, Func<Instrument, bool> filter) where T : IMetricsListener => throw null!;
        public static MetricsEnableOptions EnableMetrics(this MetricsEnableOptions options, string? meterName, MeterScope scopes) => throw null!;
        public static MetricsEnableOptions EnableMetrics<T>(this MetricsEnableOptions options, string? meterName, MeterScope scopes) where T : IMetricsListener => throw null!;
        public static MetricsEnableOptions EnableMetrics(this MetricsEnableOptions options, string? meterName, Action<Instrument, bool> filter) => throw null!;
        public static MetricsEnableOptions EnableMetrics<T>(this MetricsEnableOptions options, string? meterName, Action<Instrument, bool> filter) where T : IMetricsListener => throw null!;
        public static MetricsEnableOptions EnableMetrics(this MetricsEnableOptions options, string? meterName, MeterScope scopes, Action<Instrument, bool> filter) => throw null!;
        public static MetricsEnableOptions EnableMetrics<T>(this MetricsEnableOptions options, string? meterName, MeterScope scopes, Action<Instrument, bool> filter) where T : IMetricsListener => throw null!;
    }
}
