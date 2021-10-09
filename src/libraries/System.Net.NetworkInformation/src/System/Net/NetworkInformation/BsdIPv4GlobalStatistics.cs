// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Net.NetworkInformation
{
    internal sealed class BsdIPv4GlobalStatistics : IPGlobalStatistics
    {
        private readonly long _outboundPackets;
        private readonly long _outputPacketsNoRoute;
        private readonly long _cantFrags;
        private readonly long _datagramsFragmented;
        private readonly long _packetsReassembled;
        private readonly long _totalPacketsReceived;
        private readonly long _packetsDelivered;
        private readonly long _packetsDiscarded;
        private readonly long _packetsForwarded;
        private readonly long _badAddress;
        private readonly long _badHeader;
        private readonly long _unknownProtos;
        private readonly int _defaultTtl;
        private readonly bool _forwarding;
        private readonly int _numInterfaces;
        private readonly int _numIPAddresses;
        private readonly int _numRoutes;

        private struct Context
        {
            internal int _numIPAddresses;
            internal HashSet<string> _interfaceSet;
        }

        [UnmanagedCallersOnly]
        private static unsafe void ProcessIpv4Address(void* pContext, byte* ifaceName, Interop.Sys.IpAddressInfo* ipAddr)
        {
            ref Context context = ref Unsafe.As<byte, Context>(ref *(byte*)pContext);

            context._interfaceSet.Add(new string((sbyte*)ifaceName));
            context._numIPAddresses++;
        }

        public unsafe BsdIPv4GlobalStatistics()
        {
            Interop.Sys.IPv4GlobalStatistics statistics;
            if (Interop.Sys.GetIPv4GlobalStatistics(out statistics) == -1)
            {
                throw new NetworkInformationException(SR.net_PInvokeError);
            }

            _outboundPackets = (long)statistics.OutboundPackets;
            _outputPacketsNoRoute = (long)statistics.OutputPacketsNoRoute;
            _cantFrags = (long)statistics.CantFrags;
            _datagramsFragmented = (long)statistics.DatagramsFragmented;
            _packetsReassembled = (long)statistics.PacketsReassembled;
            _totalPacketsReceived = (long)statistics.TotalPacketsReceived;
            _packetsDelivered = (long)statistics.PacketsDelivered;
            _packetsDiscarded = (long)statistics.PacketsDiscarded;
            _packetsForwarded = (long)statistics.PacketsForwarded;
            _badAddress = (long)statistics.BadAddress;
            _badHeader = (long)statistics.BadHeader;
            _unknownProtos = (long)statistics.UnknownProtos;
            _defaultTtl = statistics.DefaultTtl;
            _forwarding = statistics.Forwarding == 1;

            Context context;
            context._numIPAddresses = 0;
            context._interfaceSet = new HashSet<string>();

            Interop.Sys.EnumerateInterfaceAddresses(
                Unsafe.AsPointer(ref context),
                &ProcessIpv4Address,
                null,
                null);

            _numInterfaces = context._interfaceSet.Count;
            _numIPAddresses = context._numIPAddresses;

            _numRoutes = Interop.Sys.GetNumRoutes();
            if (_numRoutes == -1)
            {
                throw new NetworkInformationException(SR.net_PInvokeError);
            }
        }

        public override int DefaultTtl { get { return _defaultTtl; } }

        public override bool ForwardingEnabled { get { return _forwarding; } }

        public override int NumberOfInterfaces { get { return _numInterfaces; } }

        public override int NumberOfIPAddresses { get { return _numIPAddresses; } }

        public override long OutputPacketRequests { get { return _outboundPackets; } }

        [UnsupportedOSPlatform("osx")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("freebsd")]
        public override long OutputPacketRoutingDiscards { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        [UnsupportedOSPlatform("osx")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("freebsd")]
        public override long OutputPacketsDiscarded { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override long OutputPacketsWithNoRoute { get { return _outputPacketsNoRoute; } }

        [UnsupportedOSPlatform("osx")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("freebsd")]
        public override long PacketFragmentFailures { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        [UnsupportedOSPlatform("osx")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("freebsd")]
        public override long PacketReassembliesRequired { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override long PacketReassemblyFailures { get { return _cantFrags; } }

        [UnsupportedOSPlatform("osx")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("freebsd")]
        public override long PacketReassemblyTimeout { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override long PacketsFragmented { get { return _datagramsFragmented; } }

        public override long PacketsReassembled { get { return _packetsReassembled; } }

        public override long ReceivedPackets { get { return _totalPacketsReceived; } }

        public override long ReceivedPacketsDelivered { get { return _packetsDelivered; } }

        public override long ReceivedPacketsDiscarded { get { return _packetsDiscarded; } }

        public override long ReceivedPacketsForwarded { get { return _packetsForwarded; } }

        public override long ReceivedPacketsWithAddressErrors { get { return _badAddress; } }

        public override long ReceivedPacketsWithHeadersErrors { get { return _badHeader; } }

        public override long ReceivedPacketsWithUnknownProtocol { get { return _unknownProtos; } }

        public override int NumberOfRoutes { get { return _numRoutes; } }
    }
}
