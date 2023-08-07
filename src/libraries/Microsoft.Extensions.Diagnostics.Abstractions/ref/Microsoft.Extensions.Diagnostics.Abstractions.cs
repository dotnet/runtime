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
        public string Name { get; }
        public void Initialize(IObservableInstrumentsSource source);
        public bool InstrumentPublished(System.Diagnostics.Metrics.Instrument instrument, out object? userState);
        public void MeasurementsCompleted(System.Diagnostics.Metrics.Instrument instrument, object? userState);
        public MeasurementHandlers GetMeasurementHandlers();
    }
    public class MeasurementHandlers
    {
        public System.Diagnostics.Metrics.MeasurementCallback<byte>? ByteHandler { get; set; }
        public System.Diagnostics.Metrics.MeasurementCallback<short>? ShortHandler { get; set; }
        public System.Diagnostics.Metrics.MeasurementCallback<int>? IntHandler { get; set; }
        public System.Diagnostics.Metrics.MeasurementCallback<long>? LongHandler { get; set; }
        public System.Diagnostics.Metrics.MeasurementCallback<float>? FloatHandler { get; set; }
        public System.Diagnostics.Metrics.MeasurementCallback<double>? DoubleHandler { get; set; }
        public System.Diagnostics.Metrics.MeasurementCallback<decimal>? DecimalHandler { get; set; }
    }
    public interface IObservableInstrumentsSource
    {
        public void RecordObservableInstruments();
    }
    public class InstrumentRule
    {
        public InstrumentRule(string? meterName, string? instrumentName, string? listenerName, MeterScope scopes, bool enable) { }
        public string? MeterName { get; }
        public string? InstrumentName { get; }
        public string? ListenerName { get; }
        public MeterScope Scopes { get; }
        public bool Enable { get; }
    }
    [Flags]
    public enum MeterScope
    {
        None = 0,
        Global,
        Local
    }
    public static class MetricsBuilderExtensions
    {
        public static IMetricsBuilder AddListener<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] T>
            (this IMetricsBuilder builder) where T : class, IMetricsListener { throw null!; }
        public static IMetricsBuilder AddListener(this IMetricsBuilder builder, IMetricsListener listener) { throw null!; }
        public static IMetricsBuilder ClearListeners(this IMetricsBuilder builder) { throw null!; }

        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder, string? meterName) => throw null!;
        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder, string? meterName, string? instrumentName = null, string? listenerName = null, MeterScope scopes = MeterScope.Global | MeterScope.Local) => throw null!;
        public static MetricsOptions EnableMetrics(this MetricsOptions options, string? meterName) => throw null!;
        public static MetricsOptions EnableMetrics(this MetricsOptions options, string? meterName, string? instrumentName = null, string? listenerName = null, MeterScope scopes = MeterScope.Global | MeterScope.Local) => throw null!;

        public static IMetricsBuilder DisableMetrics(this IMetricsBuilder builder, string? meterName) => throw null!;
        public static IMetricsBuilder DisableMetrics(this IMetricsBuilder builder, string? meterName, string? instrumentName = null, string? listenerName = null, MeterScope scopes = MeterScope.Global | MeterScope.Local) => throw null!;
        public static MetricsOptions DisableMetrics(this MetricsOptions options, string? meterName) => throw null!;
        public static MetricsOptions DisableMetrics(this MetricsOptions options, string? meterName, string? instrumentName = null, string? listenerName = null, MeterScope scopes = MeterScope.Global | MeterScope.Local) => throw null!;
    }
    public class MetricsOptions
    {
        public IList<InstrumentRule> Rules { get; } = null!;
    }
}
