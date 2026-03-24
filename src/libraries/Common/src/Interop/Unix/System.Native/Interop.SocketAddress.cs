// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [RequiresUnsafe]
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetSocketAddressSizes")]
        [SuppressGCTransition]
        internal static unsafe partial Error GetSocketAddressSizes(int* ipv4SocketAddressSize, int* ipv6SocketAddressSize, int* udsSocketAddressSize, int* maxSocketAddressSize);

        [RequiresUnsafe]
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetAddressFamily")]
        [SuppressGCTransition]
        internal static unsafe partial Error GetAddressFamily(byte* socketAddress, int socketAddressLen, int* addressFamily);

        [RequiresUnsafe]
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetAddressFamily")]
        [SuppressGCTransition]
        internal static unsafe partial Error SetAddressFamily(byte* socketAddress, int socketAddressLen, int addressFamily);

        [RequiresUnsafe]
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetPort")]
        [SuppressGCTransition]
        internal static unsafe partial Error GetPort(byte* socketAddress, int socketAddressLen, ushort* port);

        [RequiresUnsafe]
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetPort")]
        [SuppressGCTransition]
        internal static unsafe partial Error SetPort(byte* socketAddress, int socketAddressLen, ushort port);

        [RequiresUnsafe]
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetIPv4Address")]
        [SuppressGCTransition]
        internal static unsafe partial Error GetIPv4Address(byte* socketAddress, int socketAddressLen, uint* address);

        [RequiresUnsafe]
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetIPv4Address")]
        [SuppressGCTransition]
        internal static unsafe partial Error SetIPv4Address(byte* socketAddress, int socketAddressLen, uint address);

        [RequiresUnsafe]
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetIPv6Address")]
        internal static unsafe partial Error GetIPv6Address(byte* socketAddress, int socketAddressLen, byte* address, int addressLen, uint* scopeId);

        [RequiresUnsafe]
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetIPv6Address")]
        internal static unsafe partial Error SetIPv6Address(byte* socketAddress, int socketAddressLen, byte* address, int addressLen, uint scopeId);
    }
}
