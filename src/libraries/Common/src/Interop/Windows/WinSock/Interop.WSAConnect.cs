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
        private static partial SocketError WSAConnect(
            SafeSocketHandle socketHandle,
            ReadOnlySpan<byte> socketAddress,
            int socketAddressSize,
            IntPtr inBuffer,
            IntPtr outBuffer,
            IntPtr sQOS,
            IntPtr gQOS);

        internal static SocketError WSAConnect(
            SafeSocketHandle socketHandle,
            ReadOnlySpan<byte> socketAddress,
            IntPtr inBuffer,
            IntPtr outBuffer,
            IntPtr sQOS,
            IntPtr gQOS) =>
            WSAConnect(socketHandle, socketAddress, socketAddress.Length, inBuffer, outBuffer, sQOS, gQOS);
    }
}
