// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.NetworkInformation
{
    internal class BsdIpInterfaceStatistics : IPInterfaceStatistics
    {
        private readonly long _outputQueueLength;
        private readonly long _inPackets;
        private readonly long _outPackets;
        private readonly long _inBytes;
        private readonly long _outBytes;
        private readonly long _inPacketsDiscarded;
        private readonly long _inErrors;
        private readonly long _inUnknownProtocols;
        private readonly long _inNonUnicastPackets;
        private readonly long _outNonUnicastPackets;
        private readonly long _outErrors;

        public BsdIpInterfaceStatistics(string name)
        {
            Interop.Sys.NativeIPInterfaceStatistics nativeStats;
            if (Interop.Sys.GetNativeIPInterfaceStatistics(name, out nativeStats) == -1)
            {
                throw new NetworkInformationException(SR.net_PInvokeError);
            }

            _outputQueueLength = (long)nativeStats.SendQueueLength;
            _inPackets = (long)nativeStats.InPackets;
            _outPackets = (long)nativeStats.OutPackets;
            _inBytes = (long)nativeStats.InBytes;
            _outBytes = (long)nativeStats.OutBytes;
            _inPacketsDiscarded = (long)nativeStats.InDrops;
            _inErrors = (long)nativeStats.InErrors;
            _inUnknownProtocols = (long)nativeStats.InNoProto;
            _inNonUnicastPackets = (long)nativeStats.InMulticastPackets;
            _outNonUnicastPackets = (long)nativeStats.OutMulticastPackets;
            _outErrors = (long)nativeStats.OutErrors;
        }

        public override long BytesReceived { get { return _inBytes; } }

        public override long BytesSent { get { return _outBytes; } }

        public override long IncomingPacketsDiscarded { get { return _inPacketsDiscarded; } }

        public override long IncomingPacketsWithErrors { get { return _inErrors; } }

        public override long IncomingUnknownProtocolPackets { get { return _inUnknownProtocols; } }

        public override long NonUnicastPacketsReceived { get { return _inNonUnicastPackets; } }

        public override long NonUnicastPacketsSent { get { return _outNonUnicastPackets; } }

        public override long OutgoingPacketsDiscarded { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override long OutgoingPacketsWithErrors { get { return _outErrors; } }

        public override long OutputQueueLength { get { return _outputQueueLength; } }

        public override long UnicastPacketsReceived { get { return _inPackets - _inNonUnicastPackets; } }

        public override long UnicastPacketsSent { get { return _outPackets - _outNonUnicastPackets; } }
    }
}
