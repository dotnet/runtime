// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;
using System.Net.Quic;

using Microsoft.Quic;
using static Microsoft.Quic.MsQuic;

namespace System.Net
{
    internal sealed partial class NetEventSource
    {
        private static readonly long[] s_counters = new long[(int)QUIC_PERFORMANCE_COUNTERS.MAX];

        private IncrementingPollingCounter? _connCreated;
        private IncrementingPollingCounter? _connHandshakeFail;
        private IncrementingPollingCounter? _connAppReject;
        private IncrementingPollingCounter? _connLoadReject;

        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            if (command.Command == EventCommand.Enable && MsQuicApi.IsQuicSupported)
            {
                _connCreated ??= new IncrementingPollingCounter("CONN_CREATED", this, () => GetCounter(QUIC_PERFORMANCE_COUNTERS.CONN_CREATED))
                {
                    DisplayName = "Connections Created",
                    DisplayRateTimeScale = TimeSpan.FromSeconds(1)
                };

                _connHandshakeFail ??= new IncrementingPollingCounter("CONN_HANDSHAKE_FAIL", this, () => GetCounter(QUIC_PERFORMANCE_COUNTERS.CONN_HANDSHAKE_FAIL))
                {
                    DisplayName = "Connection Handshake Failures",
                    DisplayRateTimeScale = TimeSpan.FromSeconds(1)
                };

                _connAppReject ??= new IncrementingPollingCounter("CONN_APP_REJECT", this, () => GetCounter(QUIC_PERFORMANCE_COUNTERS.CONN_APP_REJECT))
                {
                    DisplayName = "Connections Rejected on Application Layer",
                    DisplayRateTimeScale = TimeSpan.FromSeconds(1)
                };

                _connLoadReject ??= new IncrementingPollingCounter("CONN_LOAD_REJECT", this, () => GetCounter(QUIC_PERFORMANCE_COUNTERS.CONN_LOAD_REJECT))
                {
                    DisplayName = "Connections Rejected due to worker load",
                    DisplayRateTimeScale = TimeSpan.FromSeconds(1)
                };
            }
        }

        [NonEvent]
        private void UpdateCounters()
        {
            // if (!MsQuicApi.IsQuicSupported)
            // {
            //     // MsQuicApi static ctor also uses this event source for logging, so if this event source is enabled, logging can
            //     // actually transitively call this method. Since IsQuicSupported is set at the very end of that method, we just
            //     return;
            // }

            unsafe
            {
                fixed (long* pCounters = s_counters)
                {
                    MsQuicHelpers.GetMsQuicParameter(null, QUIC_PARAM_GLOBAL_PERF_COUNTERS, (uint)s_counters.Length * sizeof(long), (byte*)pCounters);
                }
            }
        }

        [NonEvent]
        private long GetCounter(QUIC_PERFORMANCE_COUNTERS counter)
        {
            UpdateCounters();
            return s_counters[(int)counter];
        }
    }
}
