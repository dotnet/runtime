// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Net.Sockets;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        // This method is always blocking, so it uses an IntPtr.
        [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static unsafe partial int recvfrom(
            SafeSocketHandle socketHandle,
            byte* pinnedBuffer,
            int len,
            SocketFlags socketFlags,
            byte[] socketAddress,
            ref int socketAddressSize);
    }
}
