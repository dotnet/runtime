// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.NetworkInformation
{
    internal class OsxTcpStatistics : TcpStatistics
    {
        private readonly long _connectionsAccepted;
        private readonly long _connectionsInitiated;
        private readonly long _cumulativeConnections;
        private readonly long _errorsReceived;
        private readonly long _failedConnectionAttempts;
        private readonly long _segmentsSent;
        private readonly long _segmentsResent;
        private readonly long _segmentsReceived;
        private readonly int _currentConnections;

        public OsxTcpStatistics()
        {
            Interop.Sys.TcpGlobalStatistics statistics;
            if (Interop.Sys.GetTcpGlobalStatistics(out statistics) != 0)
            {
                throw new NetworkInformationException(SR.net_PInvokeError);
            }

            _connectionsAccepted = (long)statistics.ConnectionsAccepted;
            _connectionsInitiated = (long)statistics.ConnectionsInitiated;
            _cumulativeConnections = (long)statistics.CumulativeConnections;
            _errorsReceived = (long)statistics.ErrorsReceived;
            _failedConnectionAttempts = (long)statistics.FailedConnectionAttempts;
            _segmentsSent = (long)statistics.SegmentsSent;
            _segmentsResent = (long)statistics.SegmentsResent;
            _segmentsReceived = (long)statistics.SegmentsReceived;
            _currentConnections = statistics.CurrentConnections;
        }

        public override long ConnectionsAccepted { get { return _connectionsAccepted; } }

        public override long ConnectionsInitiated { get { return _connectionsInitiated; } }

        public override long CumulativeConnections { get { return _cumulativeConnections; } }

        public override long CurrentConnections { get { return _currentConnections; } }

        public override long ErrorsReceived { get { return _errorsReceived; } }

        public override long FailedConnectionAttempts { get { return _failedConnectionAttempts; } }

        public override long MaximumConnections { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override long MaximumTransmissionTimeout { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override long MinimumTransmissionTimeout { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override long ResetConnections { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override long ResetsSent { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override long SegmentsReceived { get { return _segmentsReceived; } }

        public override long SegmentsResent { get { return _segmentsResent; } }

        public override long SegmentsSent { get { return _segmentsSent; } }
    }
}
