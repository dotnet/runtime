// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;

namespace System.Net.NetworkInformation
{
    // TCP specific statistics.
    internal class SystemTcpStatistics : TcpStatistics
    {
        private readonly Interop.IpHlpApi.MibTcpStats _stats;

        private SystemTcpStatistics() { }

        internal SystemTcpStatistics(AddressFamily family)
        {
            uint result = Interop.IpHlpApi.GetTcpStatisticsEx(out _stats, family);

            if (result != Interop.IpHlpApi.ERROR_SUCCESS)
            {
                throw new NetworkInformationException((int)result);
            }
        }

        public override long MinimumTransmissionTimeout { get { return _stats.minimumRetransmissionTimeOut; } }

        public override long MaximumTransmissionTimeout { get { return _stats.maximumRetransmissionTimeOut; } }

        public override long MaximumConnections { get { return _stats.maximumConnections; } }

        public override long ConnectionsInitiated { get { return _stats.activeOpens; } }

        public override long ConnectionsAccepted { get { return _stats.passiveOpens; } }//  is this true?  We should check

        public override long FailedConnectionAttempts { get { return _stats.failedConnectionAttempts; } }

        public override long ResetConnections { get { return _stats.resetConnections; } }

        public override long CurrentConnections { get { return _stats.currentConnections; } }

        public override long SegmentsReceived { get { return _stats.segmentsReceived; } }

        public override long SegmentsSent { get { return _stats.segmentsSent; } }

        public override long SegmentsResent { get { return _stats.segmentsResent; } }

        public override long ErrorsReceived { get { return _stats.errorsReceived; } }

        public override long ResetsSent { get { return _stats.segmentsSentWithReset; } }

        public override long CumulativeConnections { get { return _stats.cumulativeConnections; } }
    }
}
