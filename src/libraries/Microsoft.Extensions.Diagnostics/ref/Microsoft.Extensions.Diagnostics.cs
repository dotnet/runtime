// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    public static class MetricsServiceExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddMetrics(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { return null!; }
    }
    public static class MeterFactoryExtensions
    {
        public static System.Diagnostics.Metrics.Meter Create(this Microsoft.Extensions.Diagnostics.Metrics.IMeterFactory meterFactory, string name, string? version = null, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>>? tags = null,  object? scope = null) { return null!; }
    }
}