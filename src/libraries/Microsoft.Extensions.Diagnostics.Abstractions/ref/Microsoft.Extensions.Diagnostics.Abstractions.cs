// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    public interface IMeterFactory : System.IDisposable
    {
        System.Diagnostics.Metrics.Meter Create(System.Diagnostics.Metrics.MeterOptions options);
    }
}