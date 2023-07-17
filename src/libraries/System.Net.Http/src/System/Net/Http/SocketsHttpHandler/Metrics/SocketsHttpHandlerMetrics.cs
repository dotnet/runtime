// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
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

        public readonly Histogram<double> RequestsQueueDuration = meter.CreateHistogram<double>(
            name: "http-client-requests-queue-duration",
            unit: "s",
            description: "The amount of time requests spent on a queue waiting for an available connection.");

        public void RequestLeftQueue(HttpConnectionPool pool, TimeSpan duration, int versionMajor)
        {
            Debug.Assert(versionMajor is 1 or 2 or 3);

            if (RequestsQueueDuration.Enabled)
            {
                TagList tags = default;

                // While requests may report HTTP/1.0 as the protocol, we treat all HTTP/1.X connections as HTTP/1.1.
                tags.Add("protocol", versionMajor switch
                {
                    1 => "HTTP/1.1",
                    2 => "HTTP/2",
                    _ => "HTTP/3"
                });

                tags.Add("scheme", pool.IsSecure ? "https" : "http");
                tags.Add("host", pool.OriginAuthority.HostValue);

                if (!pool.IsDefaultPort)
                {
                    tags.Add("port", pool.OriginAuthority.Port);
                }

                RequestsQueueDuration.Record(duration.TotalSeconds, tags);
            }
        }
    }
}
