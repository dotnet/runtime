// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Net.NetworkInformation
{
    /// <summary>
    /// IPInterfaceStatistics provider for Linux.
    /// Reads information out of /proc/net/dev and other locations.
    /// </summary>
    internal sealed class LinuxIPInterfaceStatistics : IPInterfaceStatistics
    {
        // /proc/net/dev statistics table for network interface
        private readonly IPInterfaceStatisticsTable _table;

        // From /sys/class/net/<interface>/tx_queue_len
        private int _transmitQueueLength;

        public LinuxIPInterfaceStatistics(string name)
        {
            _table = StringParsingHelpers.ParseInterfaceStatisticsTableFromFile(NetworkFiles.InterfaceListingFile, name);

            // sys/class/net/<interfacename>/tx_queue_len
            string transmitQueueLengthFilePath = Path.Combine(NetworkFiles.SysClassNetFolder, name, NetworkFiles.TransmitQueueLengthFileName);
            _transmitQueueLength = StringParsingHelpers.ParseRawIntFile(transmitQueueLengthFilePath);

        }

        public override long BytesReceived { get { return _table.BytesReceived; } }

        public override long BytesSent { get { return _table.BytesTransmitted; } }

        public override long IncomingPacketsDiscarded { get { return _table.IncomingPacketsDropped; } }

        public override long IncomingPacketsWithErrors { get { return _table.ErrorsReceived; } }

        public override long IncomingUnknownProtocolPackets { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override long NonUnicastPacketsReceived { get { return _table.MulticastFramesReceived; } }

        public override long NonUnicastPacketsSent { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override long OutgoingPacketsDiscarded { get { return _table.OutgoingPacketsDropped; } }

        public override long OutgoingPacketsWithErrors { get { return _table.ErrorsTransmitted; } }

        public override long OutputQueueLength { get { return _transmitQueueLength; } }

        public override long UnicastPacketsReceived { get { return _table.PacketsReceived; } }

        public override long UnicastPacketsSent { get { return _table.PacketsTransmitted; } }
    }
}
