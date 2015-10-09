// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Libraries.SystemNative)]
        internal static extern unsafe Error GetIPSocketAddressSizes(int* ipv4SocketAddressSize, int* ipv6SocketAddressSize);

        [DllImport(Libraries.SystemNative)]
        internal static extern unsafe Error GetAddressFamily(byte* socketAddress, int socketAddressLen, int* addressFamily);

        [DllImport(Libraries.SystemNative)]
        internal static extern unsafe Error SetAddressFamily(byte* socketAddress, int socketAddressLen, int addressFamily);

        [DllImport(Libraries.SystemNative)]
        internal static extern unsafe Error GetPort(byte* socketAddress, int socketAddressLen, ushort* port);

        [DllImport(Libraries.SystemNative)]
        internal static extern unsafe Error SetPort(byte* socketAddress, int socketAddressLen, ushort port);

        [DllImport(Libraries.SystemNative)]
        internal static extern unsafe Error GetIPv4Address(byte* socketAddress, int socketAddressLen, uint* address);

        [DllImport(Libraries.SystemNative)]
        internal static extern unsafe Error SetIPv4Address(byte* socketAddress, int socketAddressLen, uint address);

        [DllImport(Libraries.SystemNative)]
        internal static extern unsafe Error GetIPv6Address(byte* socketAddress, int socketAddressLen, byte* address, int addressLen, uint* scopeId);

        [DllImport(Libraries.SystemNative)]
        internal static extern unsafe Error SetIPv6Address(byte* socketAddress, int socketAddressLen, byte* address, int addressLen, uint scopeId);
    }
}
