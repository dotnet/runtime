// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Net.NetworkInformation
{
    internal sealed class BsdIpInterfaceProperties : UnixIPInterfaceProperties
    {
        private readonly BsdIPv4InterfaceProperties _ipv4Properties;
        private readonly BsdIPv6InterfaceProperties _ipv6Properties;
        private readonly GatewayIPAddressInformationCollection _gatewayAddresses;

        public BsdIpInterfaceProperties(BsdNetworkInterface oni, int mtu) : base(oni)
        {
            _ipv4Properties = new BsdIPv4InterfaceProperties(oni, mtu);
            _ipv6Properties = new BsdIPv6InterfaceProperties(oni, mtu);
            _gatewayAddresses = GetGatewayAddresses(oni.Index);
        }

        public override IPAddressInformationCollection AnycastAddresses { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override IPAddressCollection DhcpServerAddresses { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override GatewayIPAddressInformationCollection GatewayAddresses { get { return _gatewayAddresses; } }

        public override bool IsDnsEnabled { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override bool IsDynamicDnsEnabled { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override IPAddressCollection WinsServersAddresses { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override IPv4InterfaceProperties GetIPv4Properties()
        {
            return _ipv4Properties;
        }

        public override IPv6InterfaceProperties GetIPv6Properties()
        {
            return _ipv6Properties;
        }

        private static unsafe GatewayIPAddressInformationCollection GetGatewayAddresses(int interfaceIndex)
        {
            HashSet<IPAddress> addressSet = new HashSet<IPAddress>();
            if (Interop.Sys.EnumerateGatewayAddressesForInterface((uint)interfaceIndex,
                (gatewayAddressInfo) =>
                {
                    byte[] ipBytes = new byte[gatewayAddressInfo->NumAddressBytes];
                    fixed (byte* ipArrayPtr = ipBytes)
                    {
                        Buffer.MemoryCopy(gatewayAddressInfo->AddressBytes, ipArrayPtr, ipBytes.Length, ipBytes.Length);
                    }
                    IPAddress ipAddress = new IPAddress(ipBytes);
                    if (ipAddress.IsIPv6LinkLocal)
                    {
                        // For Link-Local addresses add ScopeId as that is not part of the route entry.
                        ipAddress.ScopeId = interfaceIndex;
                    }
                    addressSet.Add(ipAddress);
                }) == -1)
            {
                throw new NetworkInformationException(SR.net_PInvokeError);
            }

            GatewayIPAddressInformationCollection collection = new GatewayIPAddressInformationCollection();
            foreach (IPAddress address in addressSet)
            {
                collection.InternalAdd(new SimpleGatewayIPAddressInformation(address));
            }

            return collection;
        }
    }
}
