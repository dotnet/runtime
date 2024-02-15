// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Diagnostics.Metrics;
using System.Net.Quic;

using Microsoft.Quic;
using static Microsoft.Quic.MsQuic;

namespace System.Net
{
    internal sealed partial class NetEventSource
    {
        private static Meter s_meter = new Meter("Private.InternalDiagnostics.System.Net.Quic");
        private static readonly long[] s_counters = new long[(int)QUIC_PERFORMANCE_COUNTERS.MAX];

        public static readonly ObservableGauge<long> MsQuicCountersGauge = s_meter.CreateObservableGauge<long>(
            name: "MsQuic",
            observeValues: GetGauges,
            unit: null,
            description: "MsQuic performance counters");

        public static readonly ObservableCounter<long> MsQuicCountersCounter = s_meter.CreateObservableCounter<long>(
            name: "MsQuic",
            observeValues: GetCounters,
            unit: null,
            description: "MsQuic performance counters");

        [NonEvent]
        private static void UpdateCounters()
        {
            unsafe
            {
                fixed (long* pCounters = s_counters)
                {
                    uint size = (uint)s_counters.Length * sizeof(long);
                    MsQuicApi.Api.ApiTable->GetParam(null, QUIC_PARAM_GLOBAL_PERF_COUNTERS, &size, (byte*)pCounters);
                }
            }
        }

        [NonEvent]
        private static IEnumerable<Measurement<long>> GetGauges()
        {
            if (!MsQuicApi.IsQuicSupported)
            {
                // Avoid calling into MsQuic if not supported (or not initialized yet)
                return Array.Empty<Measurement<long>>();
            }

            UpdateCounters();

            var measurements = new List<Measurement<long>>();

            static void AddMeasurement(List<Measurement<long>> measurements, QUIC_PERFORMANCE_COUNTERS counter)
            {
                measurements.Add(new Measurement<long>(s_counters[(int)counter], new KeyValuePair<string, object?>("Name", counter)));
            }

            AddMeasurement(measurements, QUIC_PERFORMANCE_COUNTERS.CONN_ACTIVE);
            AddMeasurement(measurements, QUIC_PERFORMANCE_COUNTERS.CONN_CONNECTED);
            AddMeasurement(measurements, QUIC_PERFORMANCE_COUNTERS.STRM_ACTIVE);
            AddMeasurement(measurements, QUIC_PERFORMANCE_COUNTERS.CONN_QUEUE_DEPTH);
            AddMeasurement(measurements, QUIC_PERFORMANCE_COUNTERS.CONN_OPER_QUEUE_DEPTH);
            AddMeasurement(measurements, QUIC_PERFORMANCE_COUNTERS.WORK_OPER_QUEUE_DEPTH);

            return measurements;
        }


        [NonEvent]
        private static IEnumerable<Measurement<long>> GetCounters()
        {
            if (!MsQuicApi.IsQuicSupported)
            {
                // Avoid calling into MsQuic if not supported (or not initialized yet)
                return Array.Empty<Measurement<long>>();
            }

            UpdateCounters();

            var measurements = new List<Measurement<long>>();

            static void AddMeasurement(List<Measurement<long>> measurements, QUIC_PERFORMANCE_COUNTERS counter)
            {
                measurements.Add(new Measurement<long>(s_counters[(int)counter], new KeyValuePair<string, object?>("Name", counter)));
            }

            AddMeasurement(measurements, QUIC_PERFORMANCE_COUNTERS.CONN_CREATED);
            AddMeasurement(measurements, QUIC_PERFORMANCE_COUNTERS.CONN_HANDSHAKE_FAIL);
            AddMeasurement(measurements, QUIC_PERFORMANCE_COUNTERS.CONN_APP_REJECT);
            AddMeasurement(measurements, QUIC_PERFORMANCE_COUNTERS.CONN_RESUMED);
            AddMeasurement(measurements, QUIC_PERFORMANCE_COUNTERS.CONN_PROTOCOL_ERRORS);
            AddMeasurement(measurements, QUIC_PERFORMANCE_COUNTERS.CONN_NO_ALPN);
            AddMeasurement(measurements, QUIC_PERFORMANCE_COUNTERS.PKTS_SUSPECTED_LOST);
            AddMeasurement(measurements, QUIC_PERFORMANCE_COUNTERS.PKTS_DROPPED);
            AddMeasurement(measurements, QUIC_PERFORMANCE_COUNTERS.PKTS_DECRYPTION_FAIL);
            AddMeasurement(measurements, QUIC_PERFORMANCE_COUNTERS.UDP_RECV);
            AddMeasurement(measurements, QUIC_PERFORMANCE_COUNTERS.UDP_SEND);
            AddMeasurement(measurements, QUIC_PERFORMANCE_COUNTERS.UDP_RECV_BYTES);
            AddMeasurement(measurements, QUIC_PERFORMANCE_COUNTERS.UDP_SEND_BYTES);
            AddMeasurement(measurements, QUIC_PERFORMANCE_COUNTERS.UDP_RECV_EVENTS);
            AddMeasurement(measurements, QUIC_PERFORMANCE_COUNTERS.UDP_SEND_CALLS);
            AddMeasurement(measurements, QUIC_PERFORMANCE_COUNTERS.APP_SEND_BYTES);
            AddMeasurement(measurements, QUIC_PERFORMANCE_COUNTERS.APP_RECV_BYTES);
            AddMeasurement(measurements, QUIC_PERFORMANCE_COUNTERS.CONN_OPER_QUEUED);
            AddMeasurement(measurements, QUIC_PERFORMANCE_COUNTERS.CONN_OPER_COMPLETED);
            AddMeasurement(measurements, QUIC_PERFORMANCE_COUNTERS.WORK_OPER_QUEUED);
            AddMeasurement(measurements, QUIC_PERFORMANCE_COUNTERS.WORK_OPER_COMPLETED);
            AddMeasurement(measurements, QUIC_PERFORMANCE_COUNTERS.PATH_VALIDATED);
            AddMeasurement(measurements, QUIC_PERFORMANCE_COUNTERS.PATH_FAILURE);
            AddMeasurement(measurements, QUIC_PERFORMANCE_COUNTERS.SEND_STATELESS_RESET);
            AddMeasurement(measurements, QUIC_PERFORMANCE_COUNTERS.SEND_STATELESS_RETRY);
            AddMeasurement(measurements, QUIC_PERFORMANCE_COUNTERS.CONN_LOAD_REJECT);

            return measurements;
        }
    }
}
