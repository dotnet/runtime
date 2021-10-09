// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.NetworkInformation
{
    internal sealed class SystemIPv4InterfaceStatistics : IPv4InterfaceStatistics
    {
        private readonly Interop.IpHlpApi.MibIfRow2 _ifRow;

        internal SystemIPv4InterfaceStatistics(long index)
        {
            _ifRow = SystemIPInterfaceStatistics.GetIfEntry2(index);
        }

        public override long OutputQueueLength { get { return (long)_ifRow.outQLen; } }

        public override long BytesSent { get { return (long)_ifRow.outOctets; } }

        public override long BytesReceived { get { return (long)_ifRow.inOctets; } }

        public override long UnicastPacketsSent { get { return (long)_ifRow.outUcastPkts; } }

        public override long UnicastPacketsReceived { get { return (long)_ifRow.inUcastPkts; } }

        public override long NonUnicastPacketsSent { get { return (long)_ifRow.outNUcastPkts; } }

        public override long NonUnicastPacketsReceived { get { return (long)_ifRow.inNUcastPkts; } }

        public override long IncomingPacketsDiscarded { get { return (long)_ifRow.inDiscards; } }

        public override long OutgoingPacketsDiscarded { get { return (long)_ifRow.outDiscards; } }

        public override long IncomingPacketsWithErrors { get { return (long)_ifRow.inErrors; } }

        public override long OutgoingPacketsWithErrors { get { return (long)_ifRow.outErrors; } }

        public override long IncomingUnknownProtocolPackets { get { return (long)_ifRow.inUnknownProtos; } }
    }
}
