// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.NetworkInformation
{
    internal sealed class BsdIPv4InterfaceStatistics : IPv4InterfaceStatistics
    {
        private readonly BsdIpInterfaceStatistics _statistics;

        public BsdIPv4InterfaceStatistics(string name)
        {
            _statistics = new BsdIpInterfaceStatistics(name);
        }

        public override long BytesReceived => _statistics.BytesReceived;

        public override long BytesSent => _statistics.BytesSent;

        public override long IncomingPacketsDiscarded => _statistics.IncomingPacketsDiscarded;

        public override long IncomingPacketsWithErrors => _statistics.IncomingPacketsWithErrors;

        public override long IncomingUnknownProtocolPackets => _statistics.IncomingUnknownProtocolPackets;

        public override long NonUnicastPacketsReceived => _statistics.NonUnicastPacketsReceived;

        public override long NonUnicastPacketsSent => _statistics.NonUnicastPacketsSent;

        public override long OutgoingPacketsDiscarded => _statistics.OutgoingPacketsDiscarded;

        public override long OutgoingPacketsWithErrors => _statistics.OutgoingPacketsWithErrors;

        public override long OutputQueueLength => _statistics.OutputQueueLength;

        public override long UnicastPacketsReceived => _statistics.UnicastPacketsReceived;

        public override long UnicastPacketsSent => _statistics.UnicastPacketsSent;
    }
}
