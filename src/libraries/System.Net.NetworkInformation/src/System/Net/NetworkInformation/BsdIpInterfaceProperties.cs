// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

        private struct Context
        {
            internal int _interfaceIndex;
            internal HashSet<IPAddress> _addressSet;
        }

        private static unsafe GatewayIPAddressInformationCollection GetGatewayAddresses(int interfaceIndex)
        {
            Context context;
            context._interfaceIndex = interfaceIndex;
            context._addressSet = new HashSet<IPAddress>();
            if (Interop.Sys.EnumerateGatewayAddressesForInterface(Unsafe.AsPointer(ref context), (uint)interfaceIndex, &OnGatewayFound) == -1)
            {
                throw new NetworkInformationException(SR.net_PInvokeError);
            }

            GatewayIPAddressInformationCollection collection = new GatewayIPAddressInformationCollection();
            foreach (IPAddress address in context._addressSet)
            {
                collection.InternalAdd(new SimpleGatewayIPAddressInformation(address));
            }

            return collection;
        }

        [UnmanagedCallersOnly]
        private static unsafe void OnGatewayFound(void* pContext, Interop.Sys.IpAddressInfo* gatewayAddressInfo)
        {
            ref Context context = ref Unsafe.As<byte, Context>(ref *(byte*)pContext);

            IPAddress ipAddress = new IPAddress(new Span<byte>(gatewayAddressInfo->AddressBytes, gatewayAddressInfo->NumAddressBytes).ToArray());
            if (ipAddress.IsIPv6LinkLocal)
            {
                // For Link-Local addresses add ScopeId as that is not part of the route entry.
                ipAddress.ScopeId = context._interfaceIndex;
            }
            context._addressSet.Add(ipAddress);
        }
    }
}
