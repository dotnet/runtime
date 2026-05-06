// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Net.NetworkInformation
{
    internal sealed class AndroidIPGlobalStatistics : IPGlobalStatistics
    {
        public AndroidIPGlobalStatistics(bool ipv4)
        {
            AndroidNetworkInterface[] networkInterfaces = NetworkInterfacePal.GetAndroidNetworkInterfaces();

            foreach (var networkInterface in networkInterfaces)
            {
                var component = ipv4 ? NetworkInterfaceComponent.IPv4 : NetworkInterfaceComponent.IPv6;
                if (networkInterface.Supports(component))
                {
                    NumberOfInterfaces++;
                }

                foreach (UnixUnicastIPAddressInformation addressInformation in networkInterface.UnicastAddress)
                {
                    bool isIPv4 = addressInformation.Address.AddressFamily == AddressFamily.InterNetwork;
                    if (isIPv4 == ipv4)
                    {
                        NumberOfIPAddresses++;
                    }
                }

                if (networkInterface.MulticastAddresess != null)
                {
                    foreach (IPAddress address in networkInterface.MulticastAddresess)
                    {
                        bool isIPv4 = address.AddressFamily == AddressFamily.InterNetwork;
                        if (isIPv4 == ipv4)
                        {
                            NumberOfIPAddresses++;
                        }
                    }
                }
            }
        }

        public override int NumberOfInterfaces { get; }
        public override int NumberOfIPAddresses { get; }

        public override int DefaultTtl => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        public override bool ForwardingEnabled => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        public override int NumberOfRoutes => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        public override long OutputPacketRequests => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        public override long OutputPacketRoutingDiscards => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        public override long OutputPacketsDiscarded => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        public override long OutputPacketsWithNoRoute => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        public override long PacketFragmentFailures => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        public override long PacketReassembliesRequired => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        public override long PacketReassemblyFailures => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        public override long PacketReassemblyTimeout => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        public override long PacketsFragmented => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        public override long PacketsReassembled => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        public override long ReceivedPackets => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        public override long ReceivedPacketsDelivered => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        public override long ReceivedPacketsDiscarded => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        public override long ReceivedPacketsForwarded => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        public override long ReceivedPacketsWithAddressErrors => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        public override long ReceivedPacketsWithHeadersErrors => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        public override long ReceivedPacketsWithUnknownProtocol => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
    }
}
