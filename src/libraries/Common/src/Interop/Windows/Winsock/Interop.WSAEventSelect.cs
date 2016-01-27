// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
