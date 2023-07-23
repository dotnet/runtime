// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetSocketAddressSizes")]
        [SuppressGCTransition]
        internal static partial Error GetSocketAddressSizes(ref int ipv4SocketAddressSize, ref int ipv6SocketAddressSize, ref int udsSocketAddressSize, ref int maxSocketAddressSize);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetAddressFamily")]
        [SuppressGCTransition]
        internal static unsafe partial Error GetAddressFamily(byte* socketAddress, int socketAddressLen, int* addressFamily);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetAddressFamily")]
        [SuppressGCTransition]
        internal static unsafe partial Error SetAddressFamily(byte* socketAddress, int socketAddressLen, int addressFamily);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetPort")]
        [SuppressGCTransition]
        internal static unsafe partial Error GetPort(byte* socketAddress, int socketAddressLen, ushort* port);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetPort")]
        [SuppressGCTransition]
        internal static unsafe partial Error SetPort(byte* socketAddress, int socketAddressLen, ushort port);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetIPv4Address")]
        [SuppressGCTransition]
        internal static unsafe partial Error GetIPv4Address(byte* socketAddress, int socketAddressLen, uint* address);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetIPv4Address")]
        [SuppressGCTransition]
        internal static unsafe partial Error SetIPv4Address(byte* socketAddress, int socketAddressLen, uint address);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetIPv6Address")]
        internal static unsafe partial Error GetIPv6Address(byte* socketAddress, int socketAddressLen, byte* address, int addressLen, uint* scopeId);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetIPv6Address")]
        internal static unsafe partial Error SetIPv6Address(byte* socketAddress, int socketAddressLen, byte* address, int addressLen, uint scopeId);
    }
}
