// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
    public static class MetricsBuilderEnableExtensions
    {
        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder) => throw null!;
        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder, string? meterName) => throw null!;
        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder, string? meterName, string? instrumentName) => throw null!;
        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder, string? meterName, string? instrumentName, string? listenerName) => throw null!;
        public static IMetricsBuilder EnableMetrics(this IMetricsBuilder builder, string? meterName, string? instrumentName, string? listenerName, MeterScope scopes) => throw null!;
        public static MetricsEnableOptions EnableMetrics(this MetricsEnableOptions options) => throw null!;
        public static MetricsEnableOptions EnableMetrics(this MetricsEnableOptions options, string? meterName) => throw null!;
        public static MetricsEnableOptions EnableMetrics(this MetricsEnableOptions options, string? meterName, string? instrumentName) => throw null!;
        public static MetricsEnableOptions EnableMetrics(this MetricsEnableOptions options, string? meterName, string? instrumentName, string? listenerName) => throw null!;
        public static MetricsEnableOptions EnableMetrics(this MetricsEnableOptions options, string? meterName, string? instrumentName, string? listenerName, MeterScope scopes) => throw null!;
        public static MetricsEnableOptions DisableMetrics(this MetricsEnableOptions options, string? meterName, string? instrumentName, string? listenerName, MeterScope scopes) => throw null!;
    }
    internal sealed class ConsoleMetricListener : IMetricsListener, IDisposable
    {
        internal TextWriter _textWriter;
        public string Name { get; }
        public System.Diagnostics.Metrics.MeasurementCallback<T> GetMeasurementHandler<T>() where T : struct => throw new NotImplementedException();
        public object? InstrumentPublished(System.Diagnostics.Metrics.Instrument instrument) => throw new NotImplementedException();
        public void MeasurementsCompleted(System.Diagnostics.Metrics.Instrument instrument, object? userState) => throw new NotImplementedException();
        public void SetSource(IMetricsSource source) => throw new NotImplementedException();
        public void Dispose() => throw new NotImplementedException();
    }
    internal sealed class ListenerSubscription
    {
        internal static bool RuleMatches(InstrumentEnableRule rule, System.Diagnostics.Metrics.Instrument instrument, string listenerName) => throw new NotImplementedException();
        internal static bool IsMoreSpecific(InstrumentEnableRule rule, InstrumentEnableRule? best) => throw new NotImplementedException();
    }
}
