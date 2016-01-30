// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
