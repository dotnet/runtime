// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Net.Sockets;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        // This function is always potentially blocking so it uses an IntPtr.
        [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static extern SocketError WSAConnect(
            [In] IntPtr socketHandle,
            [In] byte[] socketAddress,
            [In] int socketAddressSize,
            [In] IntPtr inBuffer,
            [In] IntPtr outBuffer,
            [In] IntPtr sQOS,
            [In] IntPtr gQOS);
    }
}
