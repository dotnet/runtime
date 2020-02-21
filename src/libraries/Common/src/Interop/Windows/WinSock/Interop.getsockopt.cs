// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Net.Sockets;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static extern SocketError getsockopt(
            [In] SafeSocketHandle socketHandle,
            [In] SocketOptionLevel optionLevel,
            [In] SocketOptionName optionName,
            [Out] out int optionValue,
            [In, Out] ref int optionLength);

        [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static extern SocketError getsockopt(
            [In] SafeSocketHandle socketHandle,
            [In] SocketOptionLevel optionLevel,
            [In] SocketOptionName optionName,
            [Out] byte[] optionValue,
            [In, Out] ref int optionLength);

        [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static extern SocketError getsockopt(
            [In] SafeSocketHandle socketHandle,
            [In] SocketOptionLevel optionLevel,
            [In] SocketOptionName optionName,
            [Out] out Linger optionValue,
            [In, Out] ref int optionLength);

        [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static extern SocketError getsockopt(
            [In] SafeSocketHandle socketHandle,
            [In] SocketOptionLevel optionLevel,
            [In] SocketOptionName optionName,
            [Out] out IPMulticastRequest optionValue,
            [In, Out] ref int optionLength);

        [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static extern SocketError getsockopt(
            [In] SafeSocketHandle socketHandle,
            [In] SocketOptionLevel optionLevel,
            [In] SocketOptionName optionName,
            [Out] out IPv6MulticastRequest optionValue,
            [In, Out] ref int optionLength);
    }
}
