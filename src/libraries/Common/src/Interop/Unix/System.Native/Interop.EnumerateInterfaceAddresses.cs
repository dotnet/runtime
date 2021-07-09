// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            public byte PrefixLength;
            private fixed byte __padding[2];
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct NetworkInterfaceInfo
        {
            public fixed byte Name[16];
            public long Speed;
            public int InterfaceIndex;
            public int Mtu;
            public ushort HardwareType;
            public byte OperationalState;
            public byte NumAddressBytes;
            public fixed byte AddressBytes[8];
            public byte SupportsMulticast;
            private fixed byte __padding[3];
        }

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_EnumerateInterfaceAddresses")]
        public static extern unsafe int EnumerateInterfaceAddresses(
            void* context,
            delegate* unmanaged<void*, byte*, IpAddressInfo*, void> ipv4Found,
            delegate* unmanaged<void*, byte*, IpAddressInfo*, uint*, void> ipv6Found,
            delegate* unmanaged<void*, byte*, LinkLayerAddressInfo*, void> linkLayerFound);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_EnumerateGatewayAddressesForInterface")]
        public static extern unsafe int EnumerateGatewayAddressesForInterface(void* context, uint interfaceIndex, delegate* unmanaged<void*, IpAddressInfo*, void> onGatewayFound);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetNetworkInterfaces")]
        public static unsafe extern int GetNetworkInterfaces(ref int count, ref NetworkInterfaceInfo* addrs, ref int addressCount, ref IpAddressInfo *aa);

    }
}
