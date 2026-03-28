// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static partial SocketError setsockopt(
            IntPtr handle,
            SocketOptionLevel optionLevel,
            SocketOptionName optionName,
            ref Linger linger,
            int optionLength);

        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static partial SocketError setsockopt(
            SafeSocketHandle socketHandle,
            SocketOptionLevel optionLevel,
            SocketOptionName optionName,
            ref int optionValue,
            int optionLength);

        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static unsafe partial SocketError setsockopt(
            SafeSocketHandle socketHandle,
            SocketOptionLevel optionLevel,
            SocketOptionName optionName,
            byte* optionValue,
            int optionLength);

        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static partial SocketError setsockopt(
            SafeSocketHandle socketHandle,
            SocketOptionLevel optionLevel,
            SocketOptionName optionName,
            ref IntPtr pointer,
            int optionLength);

        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static partial SocketError setsockopt(
            SafeSocketHandle socketHandle,
            SocketOptionLevel optionLevel,
            SocketOptionName optionName,
            ref Linger linger,
            int optionLength);

        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static partial SocketError setsockopt(
            SafeSocketHandle socketHandle,
            SocketOptionLevel optionLevel,
            SocketOptionName optionName,
            ref IPMulticastRequest mreq,
            int optionLength);

        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static partial SocketError setsockopt(
            SafeSocketHandle socketHandle,
            SocketOptionLevel optionLevel,
            SocketOptionName optionName,
            in IPv6MulticastRequest mreq,
            int optionLength);
    }
}
