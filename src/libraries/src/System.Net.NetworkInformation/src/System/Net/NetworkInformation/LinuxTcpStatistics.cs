// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Net.NetworkInformation
{
    internal class LinuxTcpStatistics : TcpStatistics
    {
        private readonly TcpGlobalStatisticsTable _table;
        private readonly int _currentConnections;

        public LinuxTcpStatistics(bool ipv4)
        {
            _table = StringParsingHelpers.ParseTcpGlobalStatisticsFromSnmpFile(NetworkFiles.SnmpV4StatsFile);

            string sockstatFile = ipv4 ? NetworkFiles.SockstatFile : NetworkFiles.Sockstat6File;
            string protoName = ipv4 ? "TCP" : "TCP6";
            _currentConnections = StringParsingHelpers.ParseNumSocketConnections(sockstatFile, protoName);
        }

        public override long ConnectionsAccepted { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override long ConnectionsInitiated { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override long CumulativeConnections { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override long CurrentConnections { get { return _currentConnections; } }

        public override long ErrorsReceived { get { return _table.InErrs; } }

        public override long FailedConnectionAttempts { get { return _table.AttemptFails; } }

        public override long MaximumConnections { get { return _table.MaxConn; } }

        public override long MaximumTransmissionTimeout { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override long MinimumTransmissionTimeout { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override long ResetConnections { get { return _table.EstabResets; } }

        public override long ResetsSent { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override long SegmentsReceived { get { return _table.InSegs; } }

        public override long SegmentsResent { get { return _table.RetransSegs; } }

        public override long SegmentsSent { get { return _table.OutSegs; } }
    }
}
