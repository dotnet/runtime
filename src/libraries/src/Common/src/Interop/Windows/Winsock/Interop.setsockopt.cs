// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        [DllImport(Interop.Libraries.Ws2_32, ExactSpelling = true, SetLastError = true)]
        internal static extern SocketError setsockopt(
            [In] IntPtr handle,
            [In] SocketOptionLevel optionLevel,
            [In] SocketOptionName optionName,
            [In] ref Linger linger,
            [In] int optionLength);

        [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static extern SocketError setsockopt(
            [In] SafeCloseSocket socketHandle,
            [In] SocketOptionLevel optionLevel,
            [In] SocketOptionName optionName,
            [In] ref int optionValue,
            [In] int optionLength);

        [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static extern SocketError setsockopt(
            [In] SafeCloseSocket socketHandle,
            [In] SocketOptionLevel optionLevel,
            [In] SocketOptionName optionName,
            [In] byte[] optionValue,
            [In] int optionLength);

        [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static extern SocketError setsockopt(
            [In] SafeCloseSocket socketHandle,
            [In] SocketOptionLevel optionLevel,
            [In] SocketOptionName optionName,
            [In] ref IntPtr pointer,
            [In] int optionLength);

        [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static extern SocketError setsockopt(
            [In] SafeCloseSocket socketHandle,
            [In] SocketOptionLevel optionLevel,
            [In] SocketOptionName optionName,
            [In] ref Linger linger,
            [In] int optionLength);

        [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static extern SocketError setsockopt(
            [In] SafeCloseSocket socketHandle,
            [In] SocketOptionLevel optionLevel,
            [In] SocketOptionName optionName,
            [In] ref IPMulticastRequest mreq,
            [In] int optionLength);

        [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static extern SocketError setsockopt(
            [In] SafeCloseSocket socketHandle,
            [In] SocketOptionLevel optionLevel,
            [In] SocketOptionName optionName,
            [In] ref IPv6MulticastRequest mreq,
            [In] int optionLength);
    }
}
