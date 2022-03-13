// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        // Used with SIOGETEXTENSIONFUNCTIONPOINTER - we're assuming that will never block.
        [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static partial SocketError WSAIoctl(
            SafeSocketHandle socketHandle,
            int ioControlCode,
            ref Guid guid,
            int guidSize,
            out IntPtr funcPtr,
            int funcPtrSize,
            out int bytesTransferred,
            IntPtr shouldBeNull,
            IntPtr shouldBeNull2);

        [LibraryImport(Interop.Libraries.Ws2_32, EntryPoint = "WSAIoctl", SetLastError = true)]
        internal static partial SocketError WSAIoctl_Blocking(
            SafeSocketHandle socketHandle,
            int ioControlCode,
            byte[]? inBuffer,
            int inBufferSize,
            byte[]? outBuffer,
            int outBufferSize,
            out int bytesTransferred,
            IntPtr overlapped,
            IntPtr completionRoutine);
    }
}
