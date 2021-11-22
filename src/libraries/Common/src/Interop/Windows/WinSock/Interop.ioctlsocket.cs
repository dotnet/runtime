// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        [GeneratedDllImport(Interop.Libraries.Ws2_32, ExactSpelling = true, SetLastError = true)]
        internal static partial SocketError ioctlsocket(
            IntPtr handle,
            int cmd,
            ref int argp);

        [GeneratedDllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static partial SocketError ioctlsocket(
            SafeSocketHandle socketHandle,
            int cmd,
            ref int argp);
    }
}
