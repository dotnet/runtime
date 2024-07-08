// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;

namespace System.Net.Http
{
    internal static class DiagnosticsHelper
    {
        // OTel bucket boundary recommendation for 'http.request.duration':
        // https://github.com/open-telemetry/semantic-conventions/blob/release/v1.23.x/docs/http/http-metrics.md#metric-httpclientrequestduration
        // We are using these boundaries for all network requests that are expected to be short.
        public static InstrumentAdvice<double> ShortHistogramAdvice { get; } = new()
        {
            HistogramBucketBoundaries = [0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10]
        };
    }
}
