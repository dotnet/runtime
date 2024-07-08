// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace System.Net.Http.Metrics
{
    internal sealed class SocketsHttpHandlerMetrics(Meter meter)
    {
        public readonly UpDownCounter<long> OpenConnections = meter.CreateUpDownCounter<long>(
            name: "http.client.open_connections",
            unit: "{connection}",
            description: "Number of outbound HTTP connections that are currently active or idle on the client.");

        public readonly Histogram<double> ConnectionDuration = meter.CreateHistogram<double>(
            name: "http.client.connection.duration",
            unit: "s",
            description: "The duration of successfully established outbound HTTP connections.",
            advice: new InstrumentAdvice<double>()
            {
                // These values are not based on a standard and may change in the future.
                HistogramBucketBoundaries = [0.01, 0.02, 0.05, 0.1, 0.2, 0.5, 1, 2, 5, 10, 30, 60, 120, 300]
            });

        public readonly Histogram<double> RequestsQueueDuration = meter.CreateHistogram<double>(
            name: "http.client.request.time_in_queue",
            unit: "s",
            description: "The amount of time requests spent on a queue waiting for an available connection.",
            advice: DiagnosticsHelper.ShortHistogramAdvice);

        public void RequestLeftQueue(HttpRequestMessage request, HttpConnectionPool pool, TimeSpan duration, int versionMajor)
        {
            Debug.Assert(versionMajor is 1 or 2 or 3);

            if (RequestsQueueDuration.Enabled)
            {
                TagList tags = default;

                // While requests may report HTTP/1.0 as the protocol, we treat all HTTP/1.X connections as HTTP/1.1.
                tags.Add("network.protocol.version", versionMajor switch
                {
                    1 => "1.1",
                    2 => "2",
                    _ => "3"
                });

                tags.Add("url.scheme", pool.IsSecure ? "https" : "http");
                tags.Add("server.address", pool.OriginAuthority.HostValue);

                if (!pool.IsDefaultPort)
                {
                    tags.Add("server.port", pool.OriginAuthority.Port);
                }

                tags.Add(MetricsHandler.GetMethodTag(request.Method));

                RequestsQueueDuration.Record(duration.TotalSeconds, tags);
            }
        }
    }
}
