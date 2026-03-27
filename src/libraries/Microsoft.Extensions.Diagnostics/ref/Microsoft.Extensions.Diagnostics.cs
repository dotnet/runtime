// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MetricsServiceExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddMetrics(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddMetrics(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.Extensions.Diagnostics.Metrics.IMetricsBuilder> configure) { throw null; }
    }
    public static class ActivityServiceExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddTracing(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddTracing(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.Extensions.Diagnostics.Configuration.ITracingBuilder> configure) { throw null; }
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
namespace Microsoft.Extensions.Diagnostics.Configuration
{
    public static class ActivityBuilderExtensions
    {
        public static ITracingBuilder AddConfiguration(this ITracingBuilder builder, Microsoft.Extensions.Configuration.IConfiguration configuration) => throw null!;
    }
}
namespace Microsoft.Extensions.Diagnostics.Configuration
{
    public interface IActivityListenerConfigurationFactory
    {
        Microsoft.Extensions.Configuration.IConfiguration GetConfiguration(string listenerName);
    }
}
namespace Microsoft.Extensions.Diagnostics.Metrics.Configuration
{
    public interface IMetricListenerConfigurationFactory
    {
        Microsoft.Extensions.Configuration.IConfiguration GetConfiguration(string listenerName);
    }
}
