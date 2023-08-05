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
            name: "dns.lookups.duration",
            unit: "s",
            description: "Measures the time taken to perform a DNS lookup.");

        public static bool IsEnabled() => s_lookupDuration.Enabled;

        public static void AfterResolution(TimeSpan duration, string hostName)
        {
            s_lookupDuration.Record(duration.TotalSeconds, KeyValuePair.Create("dns.question.name", (object?)hostName));
        }
    }
}
