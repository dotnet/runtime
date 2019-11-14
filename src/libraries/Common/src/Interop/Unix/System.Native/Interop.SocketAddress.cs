// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetIPSocketAddressSizes")]
        internal static extern unsafe Error GetIPSocketAddressSizes(int* ipv4SocketAddressSize, int* ipv6SocketAddressSize);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetAddressFamily")]
        internal static extern unsafe Error GetAddressFamily(byte* socketAddress, int socketAddressLen, int* addressFamily);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetAddressFamily")]
        internal static extern unsafe Error SetAddressFamily(byte* socketAddress, int socketAddressLen, int addressFamily);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetPort")]
        internal static extern unsafe Error GetPort(byte* socketAddress, int socketAddressLen, ushort* port);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetPort")]
        internal static extern unsafe Error SetPort(byte* socketAddress, int socketAddressLen, ushort port);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetIPv4Address")]
        internal static extern unsafe Error GetIPv4Address(byte* socketAddress, int socketAddressLen, uint* address);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetIPv4Address")]
        internal static extern unsafe Error SetIPv4Address(byte* socketAddress, int socketAddressLen, uint address);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetIPv6Address")]
        internal static extern unsafe Error GetIPv6Address(byte* socketAddress, int socketAddressLen, byte* address, int addressLen, uint* scopeId);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetIPv6Address")]
        internal static extern unsafe Error SetIPv6Address(byte* socketAddress, int socketAddressLen, byte* address, int addressLen, uint scopeId);
    }
}
