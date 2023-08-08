// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MetricsServiceExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddMetrics(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddMetrics(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.Extensions.Diagnostics.Metrics.IMetricsBuilder> configure) { throw null; }
    }
}
namespace Microsoft.Extensions.Diagnostics.Metrics
{
    public static class ConsoleMetrics
    {
        public static string ListenerName => throw null!;
    }
    public static class MetricsBuilderConsoleExtensions
    {
        public static IMetricsBuilder AddDebugConsole(this IMetricsBuilder builder) => throw null!;
    }
    internal sealed class ConsoleMetricListener : IMetricsListener, IDisposable
    {
        internal TextWriter _textWriter;
        public string Name { get; }
        public MeasurementHandlers GetMeasurementHandlers() => throw null!;
        public bool InstrumentPublished(System.Diagnostics.Metrics.Instrument instrument, out object? userState) => throw null!;
        public void MeasurementsCompleted(System.Diagnostics.Metrics.Instrument instrument, object? userState) => throw null!;
        public void Initialize(IObservableInstrumentsSource source) => throw null!;
        public void Dispose() => throw null!;
    }
    internal sealed class ListenerSubscription
    {
        internal ListenerSubscription(Microsoft.Extensions.Diagnostics.Metrics.IMetricsListener listener) { }
        public void Initialize() { }
        internal void UpdateRules(IList<InstrumentRule> rules) { }
        internal static bool RuleMatches(InstrumentRule rule, System.Diagnostics.Metrics.Instrument instrument, string listenerName) => throw null!;
        internal static bool IsMoreSpecific(InstrumentRule rule, InstrumentRule? best, bool isLocalScope) => throw null!;
    }
    internal sealed class DefaultMeterFactory : System.Diagnostics.Metrics.IMeterFactory
    {
        public DefaultMeterFactory() { }
        public System.Diagnostics.Metrics.Meter Create(System.Diagnostics.Metrics.MeterOptions options) => throw null!;
        public void Dispose() { }
    }
}
