// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct LinkLayerAddressInfo
        {
            public int InterfaceIndex;
            public InlineArray8<byte> AddressBytes;
            public byte NumAddressBytes;
            private byte __padding; // For native struct-size padding. Does not contain useful data.
            public ushort HardwareType;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IpAddressInfo
        {
            public int InterfaceIndex;
            public InlineArray16<byte> AddressBytes;
            public byte NumAddressBytes;
            public byte PrefixLength;
            private InlineArray2<byte> __padding;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NetworkInterfaceInfo
        {
            public InlineArray16<byte> Name;
            public long Speed;
            public int InterfaceIndex;
            public int Mtu;
            public ushort HardwareType;
            public byte OperationalState;
            public byte NumAddressBytes;
            public InlineArray8<byte> AddressBytes;
            public byte SupportsMulticast;
            private InlineArray3<byte> __padding;
        }

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_EnumerateInterfaceAddresses")]
        public static unsafe partial int EnumerateInterfaceAddresses(
            void* context,
            delegate* unmanaged<void*, byte*, IpAddressInfo*, void> ipv4Found,
            delegate* unmanaged<void*, byte*, IpAddressInfo*, uint*, void> ipv6Found,
            delegate* unmanaged<void*, byte*, LinkLayerAddressInfo*, void> linkLayerFound);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_EnumerateGatewayAddressesForInterface")]
        public static unsafe partial int EnumerateGatewayAddressesForInterface(void* context, uint interfaceIndex, delegate* unmanaged<void*, IpAddressInfo*, void> onGatewayFound);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetNetworkInterfaces", SetLastError = true)]
        public static unsafe partial int GetNetworkInterfaces(int* count, NetworkInterfaceInfo** addrs, int* addressCount, IpAddressInfo** aa);

    }
}
