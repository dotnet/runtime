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
        [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static extern SocketError WSAIoctl(
            SafeSocketHandle socketHandle,
            [In] int ioControlCode,
            [In, Out] ref Guid guid,
            [In] int guidSize,
            [Out] out IntPtr funcPtr,
            [In]  int funcPtrSize,
            [Out] out int bytesTransferred,
            [In] IntPtr shouldBeNull,
            [In] IntPtr shouldBeNull2);

        [GeneratedDllImport(Interop.Libraries.Ws2_32, EntryPoint = "WSAIoctl", SetLastError = true)]
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
