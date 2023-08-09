// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        public static string DebugListenerName => throw null!;
    }
    public static class MetricsBuilderConsoleExtensions
    {
        public static IMetricsBuilder AddDebugConsole(this IMetricsBuilder builder) => throw null!;
    }
    public static class MetricsBuilderConfigurationExtensions
    {
        public static IMetricsBuilder AddConfiguration(this IMetricsBuilder builder, Microsoft.Extensions.Configuration.IConfiguration configuration) => throw null!;
    }
    internal sealed class DebugConsoleMetricListener : IMetricsListener, System.IDisposable
    {
        internal System.IO.TextWriter _textWriter;
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
        internal void UpdateRules(System.Collections.Generic.IList<InstrumentRule> rules) { }
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
namespace Microsoft.Extensions.Diagnostics.Metrics.Configuration
{
    public interface IMetricListenerConfigurationFactory
    {
        Microsoft.Extensions.Configuration.IConfiguration GetConfiguration(string listenerName);
    }
    internal class MetricsConfigureOptions : Microsoft.Extensions.Options.IConfigureOptions<MetricsOptions>
    {
        public MetricsConfigureOptions(Microsoft.Extensions.Configuration.IConfiguration configuration) { }
        public void Configure(MetricsOptions options) => throw null!;
        internal static void LoadMeterRules(MetricsOptions options, Microsoft.Extensions.Configuration.IConfigurationSection configurationSection, MeterScope scopes, string? listenerName) => throw null!;
        internal static void LoadInstrumentRules(MetricsOptions options, Microsoft.Extensions.Configuration.IConfigurationSection meterSection, MeterScope scopes, string? listenerName) => throw null!;
    }
}
