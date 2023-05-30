// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    public interface IMeterFactory : System.IDisposable
    {
        System.Diagnostics.Metrics.Meter Create(System.Diagnostics.Metrics.MeterOptions options);
    }
    public static class MeterFactoryExtensions
    {
        public static System.Diagnostics.Metrics.Meter Create(this Microsoft.Extensions.Diagnostics.Metrics.IMeterFactory meterFactory, string name, string? version = null, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>>? tags = null) { return null!; }
    }
}