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
        public InstrumentEnableRule(string? listenerName, string? meterName, MeterScope scopes, string? instrumentName, bool enable) { }
        public string? ListenerName { get; }
        public string? MeterName { get; }
        public MeterScope Scopes { get; }
        public string? InstrumentName { get; }
        public bool Enable { get; }
    }
    [Flags]
    public enum MeterScope
    {
        Global,
        Local
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

    public interface IMetricsSubscriptionManager
    {
        public void Start();
    }
}
