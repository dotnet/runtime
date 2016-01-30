// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct LinkLayerAddressInfo
        {
            public int InterfaceIndex;
            public fixed byte AddressBytes[8];
            public byte NumAddressBytes;
            private byte __padding; // For native struct-size padding. Does not contain useful data.
            public ushort HardwareType;
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct IpAddressInfo
        {
            public int InterfaceIndex;
            public fixed byte AddressBytes[16];
            public byte NumAddressBytes;
            private fixed byte __padding[3];
        }

        public unsafe delegate void IPv4AddressDiscoveredCallback(string ifaceName, IpAddressInfo* ipAddressInfo, IpAddressInfo* netMaskInfo);
        public unsafe delegate void IPv6AddressDiscoveredCallback(string ifaceName, IpAddressInfo* ipAddressInfo, uint* scopeId);
        public unsafe delegate void LinkLayerAddressDiscoveredCallback(string ifaceName, LinkLayerAddressInfo* llAddress);
        public unsafe delegate void DnsAddessDiscoveredCallback(IpAddressInfo* gatewayAddress);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_EnumerateInterfaceAddresses")]
        public static extern int EnumerateInterfaceAddresses(
            IPv4AddressDiscoveredCallback ipv4Found,
            IPv6AddressDiscoveredCallback ipv6Found,
            LinkLayerAddressDiscoveredCallback linkLayerFound);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_EnumerateGatewayAddressesForInterface")]
        public static extern int EnumerateGatewayAddressesForInterface(uint interfaceIndex, DnsAddessDiscoveredCallback onGatewayFound);

    }
}
