// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    [PerformanceMetricDiscoverer("GCPerfTestFramework.Metrics.GCMetricDiscoverer", "Framework")]
#endif
    public class CollectGCMetricsAttribute : 
        Attribute
#if WINDOWS
        , IPerformanceMetricAttribute
#endif
    {
    }
}
