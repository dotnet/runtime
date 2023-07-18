// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    public static class MetricsBuilderConfigurationExtensions
    {
        public static IMetricsBuilder AddConfiguration(this IMetricsBuilder builder, Microsoft.Extensions.Configuration.IConfiguration configuration) => throw null!;
    }
}
namespace Microsoft.Extensions.Diagnostics.Metrics.Configuration
{
    public interface IMetricListenerConfiguration<T>
    {
        Microsoft.Extensions.Configuration.IConfiguration Configuration { get; }
    }
    public interface IMetricListenerConfigurationFactory
    {
        Microsoft.Extensions.Configuration.IConfiguration GetConfiguration(System.Type listenerType);
    }
}
