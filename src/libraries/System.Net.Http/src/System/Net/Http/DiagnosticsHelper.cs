// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;

namespace System.Net.Http
{
    internal static class DiagnosticsHelper
    {
        public static InstrumentAdvice<double> ShortHistogramAdvice { get; } = new()
        {
            HistogramBucketBoundaries = [0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10]
        };
    }
}
