// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static partial SocketError setsockopt(
            IntPtr handle,
            SocketOptionLevel optionLevel,
            SocketOptionName optionName,
            ref Linger linger,
            int optionLength);

        [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static partial SocketError setsockopt(
            SafeSocketHandle socketHandle,
            SocketOptionLevel optionLevel,
            SocketOptionName optionName,
            ref int optionValue,
            int optionLength);

        [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static unsafe partial SocketError setsockopt(
            SafeSocketHandle socketHandle,
            SocketOptionLevel optionLevel,
            SocketOptionName optionName,
            byte* optionValue,
            int optionLength);

        [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static partial SocketError setsockopt(
            SafeSocketHandle socketHandle,
            SocketOptionLevel optionLevel,
            SocketOptionName optionName,
            ref IntPtr pointer,
            int optionLength);

        [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static partial SocketError setsockopt(
            SafeSocketHandle socketHandle,
            SocketOptionLevel optionLevel,
            SocketOptionName optionName,
            ref Linger linger,
            int optionLength);

        [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static partial SocketError setsockopt(
            SafeSocketHandle socketHandle,
            SocketOptionLevel optionLevel,
            SocketOptionName optionName,
            ref IPMulticastRequest mreq,
            int optionLength);

        [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static partial SocketError setsockopt(
            SafeSocketHandle socketHandle,
            SocketOptionLevel optionLevel,
            SocketOptionName optionName,
            in IPv6MulticastRequest mreq,
            int optionLength);
    }
}
