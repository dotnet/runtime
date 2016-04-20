// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance.Sdk;
using System;

namespace GCPerfTestFramework.Metrics
{
    /// <summary>
    /// This attribute marks a xunit-performance test artifact as requiring GC metrics. When this attribute adorns
    /// a test artifact, xunit-performance creates an instance of <see cref="GCMetricDiscoverer"/> and uses
    /// it to populate the list of metrics provided for that artifact.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
#if WINDOWS
    [PerformanceMetricDiscoverer("GCPerfTestFramework.Metrics.GCMetricDiscoverer", "GCPerfTestFramework")]
#endif
    public class CollectGCMetricsAttribute : 
        Attribute
#if WINDOWS
        , IPerformanceMetricAttribute
#endif
    {
    }
}
