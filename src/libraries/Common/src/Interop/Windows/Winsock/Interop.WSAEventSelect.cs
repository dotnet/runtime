// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static extern SocketError WSAEventSelect(
            [In] SafeCloseSocket socketHandle,
            [In] SafeHandle Event,
            [In] AsyncEventBits NetworkEvents);

        [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static extern SocketError WSAEventSelect(
            [In] SafeCloseSocket socketHandle,
            [In] IntPtr Event,
            [In] AsyncEventBits NetworkEvents);

        [DllImport(Interop.Libraries.Ws2_32, ExactSpelling = true, SetLastError = true)]
        internal static extern SocketError WSAEventSelect(
            [In] IntPtr handle,
            [In] IntPtr Event,
            [In] AsyncEventBits NetworkEvents);
    }
}
