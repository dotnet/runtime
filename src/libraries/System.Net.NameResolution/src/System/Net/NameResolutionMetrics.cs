// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.Sockets;

namespace System.Net
{
    internal static class NameResolutionMetrics
    {
        private static readonly Meter s_meter = new("System.Net.NameResolution");

        private static readonly Histogram<double> s_lookupDuration = s_meter.CreateHistogram<double>(
            name: "dns.lookup.duration",
            unit: "s",
            description: "Measures the time taken to perform a DNS lookup.",
            advice: new InstrumentAdvice<double>()
            {
                // OTel bucket boundary recommendation for 'http.request.duration':
                // https://github.com/open-telemetry/semantic-conventions/blob/release/v1.23.x/docs/http/http-metrics.md#metric-httpclientrequestduration
                // We are using these boundaries for all network requests that are expected to be short.
                HistogramBucketBoundaries = [0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10]
            });

        public static bool IsEnabled() => s_lookupDuration.Enabled;

        public static void AfterResolution(TimeSpan duration, string hostName, Exception? exception)
        {
            var hostNameTag = KeyValuePair.Create("dns.question.name", (object?)hostName);

            if (exception is null)
            {
                s_lookupDuration.Record(duration.TotalSeconds, hostNameTag);
            }
            else
            {
                string errorType = NameResolutionTelemetry.GetErrorType(exception);
                var errorTypeTag = KeyValuePair.Create("error.type", (object?)errorType);
                s_lookupDuration.Record(duration.TotalSeconds, hostNameTag, errorTypeTag);
            }
        }
    }
}
