// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MetricsServiceExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddMetrics(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddMetrics(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.Extensions.Diagnostics.Metrics.IMetricsBuilder> configure) { throw null; }
    }
    public static class TracingServiceExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddTracing(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddTracing(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.Extensions.Diagnostics.Tracing.ITracingBuilder> configure) { throw null; }
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
}
namespace Microsoft.Extensions.Diagnostics.Tracing
{
    public abstract class ActivityListenerConfigurationFactory
    {
        protected ActivityListenerConfigurationFactory() { }
        public abstract Microsoft.Extensions.Configuration.IConfiguration GetConfiguration(string listenerName);
    }
    public static class TracingBuilderConfigurationExtensions
    {
        public static ITracingBuilder AddConfiguration(this ITracingBuilder builder, Microsoft.Extensions.Configuration.IConfiguration configuration) => throw null!;
    }
}
namespace Microsoft.Extensions.Diagnostics.Metrics.Configuration
{
    public interface IMetricListenerConfigurationFactory
    {
        Microsoft.Extensions.Configuration.IConfiguration GetConfiguration(string listenerName);
    }
}
