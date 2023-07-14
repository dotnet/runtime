// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;

namespace System.Net.Http.Metrics
{
    internal sealed class SocketsHttpHandlerMetrics(Meter meter)
    {
        public readonly UpDownCounter<long> CurrentConnections = meter.CreateUpDownCounter<long>(
            name: "http-client-current-connections",
            description: "Number of outbound HTTP connections that are currently active on the client.");

        public readonly UpDownCounter<long> IdleConnections = meter.CreateUpDownCounter<long>(
            name: "http-client-current-idle-connections",
            description: "Number of outbound HTTP connections that are currently idle on the client.");

        public readonly Histogram<double> ConnectionDuration = meter.CreateHistogram<double>(
            name: "http-client-connection-duration",
            unit: "s",
            description: "The duration of outbound HTTP connections.");
    }
}
