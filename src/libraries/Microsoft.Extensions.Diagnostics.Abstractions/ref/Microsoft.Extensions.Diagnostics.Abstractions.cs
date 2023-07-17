// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    public interface IMetricsBuilder
    {
        Microsoft.Extensions.DependencyInjection.IServiceCollection Services { get; }
    }
    public interface IMetricsListener
    {
        public void SetSource(IMetricsSource source);
        public object? InstrumentPublished(System.Diagnostics.Metrics.Instrument instrument);
        public void MeasurementsCompleted(System.Diagnostics.Metrics.Instrument instrument, object? userState);
        public System.Diagnostics.Metrics.MeasurementCallback<T> GetMeasurementHandler<T>() where T : struct;
    }
    public interface IMetricsSource
    {
        public void RecordObservableInstruments();
    }
    public class InstrumentEnableRule
    {
        public InstrumentEnableRule(string? listenerName, string? meterName, MeterScope scopes, string? instrumentName, Action<string?, System.Diagnostics.Metrics.Instrument, bool> filter) { }
        public string? ListenerName { get; }
        public string? MeterName { get; }
        public MeterScope Scopes { get; }
        public string? InstrumentName { get; }
        public Func<string?, System.Diagnostics.Metrics.Instrument, bool>? Filter { get; }
    }
    [Flags]
    public enum MeterScope
    {
        Global,
        Local
    }
    public static class MetricsBuilderEnableExtensions
    {
        // common overloads
        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder, string? meterName) => throw null!;
        public static IMetricsBuilder EnableMetrics<T>(this IMetricsBuilder builder, string? meterName) where T : IMetricsListener => throw null!;

        // less common overloads
        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder, string? meterName, string? instrumentName) => throw null!;
        public static IMetricsBuilder EnableMetrics<T>(this IMetricsBuilder builder, string? meterName, string? instrumentName) where T : IMetricsListener => throw null!;
        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder, Func<System.Diagnostics.Metrics.Instrument, bool> filter) => throw null!;
        public static IMetricsBuilder EnableMetrics<T>(this IMetricsBuilder builder, Func<System.Diagnostics.Metrics.Instrument, bool> filter) where T : IMetricsListener => throw null!;
        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder, string? meterName, MeterScope scopes) => throw null!;
        public static IMetricsBuilder EnableMetrics<T>(this IMetricsBuilder builder, string? meterName, MeterScope scopes) where T : IMetricsListener => throw null!;
        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder, string? meterName, Action<System.Diagnostics.Metrics.Instrument, bool> filter) => throw null!;
        public static IMetricsBuilder EnableMetrics<T>(this IMetricsBuilder builder, string? meterName, Action<System.Diagnostics.Metrics.Instrument, bool> filter) where T : IMetricsListener => throw null!;
        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder, string? meterName, MeterScope scopes, Action<System.Diagnostics.Metrics.Instrument, bool> filter) => throw null!;
        public static IMetricsBuilder EnableMetrics<T>(this IMetricsBuilder builder, string? meterName, MeterScope scopes, Action<System.Diagnostics.Metrics.Instrument, bool> filter) where T : IMetricsListener => throw null!;

        // all the same extension methods on MetricsEnableOptions
        public static MetricsEnableOptions EnableMetrics(this MetricsEnableOptions options, string? meterName) => throw null!;
        public static MetricsEnableOptions EnableMetrics<T>(this MetricsEnableOptions options, string? meterName) where T : IMetricsListener => throw null!;
        public static MetricsEnableOptions EnableMetrics(this MetricsEnableOptions options, string? meterName, string? instrumentName) => throw null!;
        public static MetricsEnableOptions EnableMetrics<T>(this MetricsEnableOptions options, string? meterName, string? instrumentName) where T : IMetricsListener => throw null!;
        public static MetricsEnableOptions EnableMetrics(this MetricsEnableOptions options, Func<System.Diagnostics.Metrics.Instrument, bool> filter) => throw null!;
        public static MetricsEnableOptions EnableMetrics<T>(this MetricsEnableOptions options, Func<System.Diagnostics.Metrics.Instrument, bool> filter) where T : IMetricsListener => throw null!;
        public static MetricsEnableOptions EnableMetrics(this MetricsEnableOptions options, string? meterName, MeterScope scopes) => throw null!;
        public static MetricsEnableOptions EnableMetrics<T>(this MetricsEnableOptions options, string? meterName, MeterScope scopes) where T : IMetricsListener => throw null!;
        public static MetricsEnableOptions EnableMetrics(this MetricsEnableOptions options, string? meterName, Action<System.Diagnostics.Metrics.Instrument, bool> filter) => throw null!;
        public static MetricsEnableOptions EnableMetrics<T>(this MetricsEnableOptions options, string? meterName, Action<System.Diagnostics.Metrics.Instrument, bool> filter) where T : IMetricsListener => throw null!;
        public static MetricsEnableOptions EnableMetrics(this MetricsEnableOptions options, string? meterName, MeterScope scopes, Action<System.Diagnostics.Metrics.Instrument, bool> filter) => throw null!;
        public static MetricsEnableOptions EnableMetrics<T>(this MetricsEnableOptions options, string? meterName, MeterScope scopes, Action<System.Diagnostics.Metrics.Instrument, bool> filter) where T : IMetricsListener => throw null!;
    }
    public static class MetricsBuilderExtensions
    {
        public static IMetricsBuilder AddListener(this IMetricsBuilder builder, IMetricsListener listener) { throw null!; }
        public static IMetricsBuilder ClearListeners(this IMetricsBuilder builder) { throw null!; }
    }
    public class MetricsEnableOptions
    {
        public IList<InstrumentEnableRule> Rules { get; } = null!;
    }
}
