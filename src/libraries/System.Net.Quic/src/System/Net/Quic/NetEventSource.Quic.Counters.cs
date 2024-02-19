// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Diagnostics.Metrics;
using System.Net.Quic;

using Microsoft.Quic;
using static Microsoft.Quic.MsQuic;

namespace System.Net
{
    internal sealed partial class NetEventSource
    {
        private static Meter s_meter = new Meter("Private.InternalDiagnostics.System.Net.Quic.MsQuic");
        private static long s_countersLastFetched;
        private static readonly long[] s_counters = new long[(int)QUIC_PERFORMANCE_COUNTERS.MAX];
        public static readonly ObservableCounter<long> s_CONN_CREATED = s_meter.CreateObservableCounter<long>(
            name: "msquic.connection.created",
            observeValue: () => GetCounterValue(QUIC_PERFORMANCE_COUNTERS.CONN_CREATED),
            unit: "{connection}",
            description: "New connections allocated");

        public static readonly ObservableCounter<long> s_CONN_HANDSHAKE_FAIL = s_meter.CreateObservableCounter<long>(
            name: "msquic.connection.handshake_failures",
            observeValue: () => GetCounterValue(QUIC_PERFORMANCE_COUNTERS.CONN_HANDSHAKE_FAIL),
            unit: "{connection}",
            description: "Connections that failed during handshake");

        public static readonly ObservableCounter<long> s_CONN_APP_REJECT = s_meter.CreateObservableCounter<long>(
            name: "msquic.connection.app_rejected",
            observeValue: () => GetCounterValue(QUIC_PERFORMANCE_COUNTERS.CONN_APP_REJECT),
            unit: "{connection}",
            description: "Connections rejected by the application");

        public static readonly ObservableCounter<long> s_CONN_LOAD_REJECT = s_meter.CreateObservableCounter<long>(
            name: "msquic.connection.load_rejected",
            observeValue: () => GetCounterValue(QUIC_PERFORMANCE_COUNTERS.CONN_LOAD_REJECT),
            unit: "{connection}",
            description: "Connections rejected due to worker load.");

        public static readonly ObservableCounter<long> s_CONN_RESUMED = s_meter.CreateObservableCounter<long>(
            name: "msquic.connection.resumed",
            observeValue: () => GetCounterValue(QUIC_PERFORMANCE_COUNTERS.CONN_RESUMED),
            unit: "{connection}",
            description: "Connections resumed");

        public static readonly ObservableGauge<long> s_CONN_ACTIVE = s_meter.CreateObservableGauge<long>(
            name: "msquic.connection.allocated",
            observeValue: () => GetCounterValue(QUIC_PERFORMANCE_COUNTERS.CONN_ACTIVE),
            unit: "{connection}",
            description: "Connections currently allocated");

        public static readonly ObservableGauge<long> s_CONN_CONNECTED = s_meter.CreateObservableGauge<long>(
            name: "msquic.connection.connected",
            observeValue: () => GetCounterValue(QUIC_PERFORMANCE_COUNTERS.CONN_CONNECTED),
            unit: "{connection}",
            description: "Connections currently in the connected state");

        public static readonly ObservableCounter<long> s_CONN_PROTOCOL_ERRORS = s_meter.CreateObservableCounter<long>(
            name: "msquic.connection.protocol_errors",
            observeValue: () => GetCounterValue(QUIC_PERFORMANCE_COUNTERS.CONN_PROTOCOL_ERRORS),
            unit: "{connection}",
            description: "Connections shutdown with a protocol error");

        public static readonly ObservableCounter<long> s_CONN_NO_ALPN = s_meter.CreateObservableCounter<long>(
            name: "msquic.connection.no_alpn",
            observeValue: () => GetCounterValue(QUIC_PERFORMANCE_COUNTERS.CONN_NO_ALPN),
            unit: "{connection}",
            description: "Connection attempts with no matching ALPN");

        public static readonly ObservableGauge<long> s_STRM_ACTIVE = s_meter.CreateObservableGauge<long>(
            name: "msquic.stream.allocated",
            observeValue: () => GetCounterValue(QUIC_PERFORMANCE_COUNTERS.STRM_ACTIVE),
            unit: "{stream}",
            description: "Current streams allocated");

        public static readonly ObservableCounter<long> s_PKTS_SUSPECTED_LOST = s_meter.CreateObservableCounter<long>(
            name: "msquic.packet.suspected_lost",
            observeValue: () => GetCounterValue(QUIC_PERFORMANCE_COUNTERS.PKTS_SUSPECTED_LOST),
            unit: "{packet}",
            description: "Packets suspected lost");

        public static readonly ObservableCounter<long> s_PKTS_DROPPED = s_meter.CreateObservableCounter<long>(
            name: "msquic.packet.dropped",
            observeValue: () => GetCounterValue(QUIC_PERFORMANCE_COUNTERS.PKTS_DROPPED),
            unit: "{packet}",
            description: "Packets dropped for any reason");

        public static readonly ObservableCounter<long> s_PKTS_DECRYPTION_FAIL = s_meter.CreateObservableCounter<long>(
            name: "msquic.packet.decryption_failures",
            observeValue: () => GetCounterValue(QUIC_PERFORMANCE_COUNTERS.PKTS_DECRYPTION_FAIL),
            unit: "{packet}",
            description: "Packets with decryption failures");

        public static readonly ObservableCounter<long> s_UDP_RECV = s_meter.CreateObservableCounter<long>(
            name: "msquic.udp.recv_datagrams",
            observeValue: () => GetCounterValue(QUIC_PERFORMANCE_COUNTERS.UDP_RECV),
            unit: "{datagram}",
            description: "UDP datagrams received");

        public static readonly ObservableCounter<long> s_UDP_SEND = s_meter.CreateObservableCounter<long>(
            name: "msquic.udp.send_datagrams",
            observeValue: () => GetCounterValue(QUIC_PERFORMANCE_COUNTERS.UDP_SEND),
            unit: "{datagram}",
            description: "UDP datagrams sent");

        public static readonly ObservableCounter<long> s_UDP_RECV_BYTES = s_meter.CreateObservableCounter<long>(
            name: "msquic.udp.recv_bytes",
            observeValue: () => GetCounterValue(QUIC_PERFORMANCE_COUNTERS.UDP_RECV_BYTES),
            unit: "By",
            description: "UDP payload bytes received");

        public static readonly ObservableCounter<long> s_UDP_SEND_BYTES = s_meter.CreateObservableCounter<long>(
            name: "msquic.udp.send_bytes",
            observeValue: () => GetCounterValue(QUIC_PERFORMANCE_COUNTERS.UDP_SEND_BYTES),
            unit: "By",
            description: "UDP payload bytes sent");

        public static readonly ObservableCounter<long> s_UDP_RECV_EVENTS = s_meter.CreateObservableCounter<long>(
            name: "msquic.udp.recv_events",
            observeValue: () => GetCounterValue(QUIC_PERFORMANCE_COUNTERS.UDP_RECV_EVENTS),
            unit: "{event}",
            description: "UDP receive events");

        public static readonly ObservableCounter<long> s_UDP_SEND_CALLS = s_meter.CreateObservableCounter<long>(
            name: "msquic.udp.send_calls",
            observeValue: () => GetCounterValue(QUIC_PERFORMANCE_COUNTERS.UDP_SEND_CALLS),
            unit: "{call}",
            description: "UDP send API calls");

        public static readonly ObservableCounter<long> s_APP_SEND_BYTES = s_meter.CreateObservableCounter<long>(
            name: "msquic.app.send_bytes",
            observeValue: () => GetCounterValue(QUIC_PERFORMANCE_COUNTERS.APP_SEND_BYTES),
            unit: "By",
            description: "Bytes sent by applications");

        public static readonly ObservableCounter<long> s_APP_RECV_BYTES = s_meter.CreateObservableCounter<long>(
            name: "msquic.app.recv_bytes",
            observeValue: () => GetCounterValue(QUIC_PERFORMANCE_COUNTERS.APP_RECV_BYTES),
            unit: "By",
            description: "Bytes received by applications");

        public static readonly ObservableGauge<long> s_CONN_QUEUE_DEPTH = s_meter.CreateObservableGauge<long>(
            name: "msquic.threadpool.conn_queue_depth",
            observeValue: () => GetCounterValue(QUIC_PERFORMANCE_COUNTERS.CONN_QUEUE_DEPTH),
            unit: "{connection}",
            description: "Current connections queued for processing");

        public static readonly ObservableGauge<long> s_CONN_OPER_QUEUE_DEPTH = s_meter.CreateObservableGauge<long>(
            name: "msquic.threadpool.conn_oper_queue_depth",
            observeValue: () => GetCounterValue(QUIC_PERFORMANCE_COUNTERS.CONN_OPER_QUEUE_DEPTH),
            unit: "{operation}",
            description: "Current connection operations queued");

        public static readonly ObservableCounter<long> s_CONN_OPER_QUEUED = s_meter.CreateObservableCounter<long>(
            name: "msquic.threadpool.conn_oper_queued",
            observeValue: () => GetCounterValue(QUIC_PERFORMANCE_COUNTERS.CONN_OPER_QUEUED),
            unit: "{operation}",
            description: "New connection operations queued");

        public static readonly ObservableCounter<long> s_CONN_OPER_COMPLETED = s_meter.CreateObservableCounter<long>(
            name: "msquic.threadpool.conn_oper_completed",
            observeValue: () => GetCounterValue(QUIC_PERFORMANCE_COUNTERS.CONN_OPER_COMPLETED),
            unit: "{operation}",
            description: "Connection operations processed");

        public static readonly ObservableGauge<long> s_WORK_OPER_QUEUE_DEPTH = s_meter.CreateObservableGauge<long>(
            name: "msquic.threadpool.work_oper_queue_depth",
            observeValue: () => GetCounterValue(QUIC_PERFORMANCE_COUNTERS.WORK_OPER_QUEUE_DEPTH),
            unit: "{operation}",
            description: "Current worker operations queued");

        public static readonly ObservableCounter<long> s_WORK_OPER_QUEUED = s_meter.CreateObservableCounter<long>(
            name: "msquic.threadpool.work_oper_queued",
            observeValue: () => GetCounterValue(QUIC_PERFORMANCE_COUNTERS.WORK_OPER_QUEUED),
            unit: "{operation}",
            description: "New worker operations queued");

        public static readonly ObservableCounter<long> s_WORK_OPER_COMPLETED = s_meter.CreateObservableCounter<long>(
            name: "msquic.threadpool.work_oper_completed",
            observeValue: () => GetCounterValue(QUIC_PERFORMANCE_COUNTERS.WORK_OPER_COMPLETED),
            unit: "{operation}",
            description: "Worker operations processed");

        public static readonly ObservableCounter<long> s_PATH_VALIDATED = s_meter.CreateObservableCounter<long>(
            name: "msquic.datapath.path_validated",
            observeValue: () => GetCounterValue(QUIC_PERFORMANCE_COUNTERS.PATH_VALIDATED),
            unit: "{challenge}",
            description: "Successful path challenges");

        public static readonly ObservableCounter<long> s_PATH_FAILURE = s_meter.CreateObservableCounter<long>(
            name: "msquic.datapath.path_failure",
            observeValue: () => GetCounterValue(QUIC_PERFORMANCE_COUNTERS.PATH_FAILURE),
            unit: "{challenge}",
            description: "Unsuccessful path challenges");

        public static readonly ObservableCounter<long> s_SEND_STATELESS_RESET = s_meter.CreateObservableCounter<long>(
            name: "msquic.datapath.send_stateless_reset",
            observeValue: () => GetCounterValue(QUIC_PERFORMANCE_COUNTERS.SEND_STATELESS_RESET),
            unit: "{packet}",
            description: "Stateless reset packets sent ever");

        public static readonly ObservableCounter<long> s_SEND_STATELESS_RETRY = s_meter.CreateObservableCounter<long>(
            name: "msquic.datapath.send_stateless_retry",
            observeValue: () => GetCounterValue(QUIC_PERFORMANCE_COUNTERS.SEND_STATELESS_RETRY),
            unit: "{packet}",
            description: "Stateless retry packets sent");

        [NonEvent]
        private static void UpdateCounters()
        {
            if (!MsQuicApi.IsQuicSupported)
            {
                // Avoid calling into MsQuic if not supported (or not initialized yet)
                return;
            }

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
        private static long GetCounterValue(QUIC_PERFORMANCE_COUNTERS counter)
        {
            //
            // We wan't to avoid refreshing the counter values array for each counter callback,
            // so we refresh the counters array only once every 50ms. This should be enough time
            // for all the counters to be queried and at the same time but still low enough to not
            // confuse any monitoring tool as their polling rate is usually in seconds.
            //
            if (s_countersLastFetched == 0 || Stopwatch.GetElapsedTime(s_countersLastFetched).TotalMilliseconds > 50)
            {
                UpdateCounters();
                s_countersLastFetched = Stopwatch.GetTimestamp();
            }

            return s_counters[(int)counter];
        }
    }
}
