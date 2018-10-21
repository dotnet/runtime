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
        // Used with SIOGETEXTENSIONFUNCTIONPOINTER - we're assuming that will never block.
        [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static extern SocketError WSAIoctl(
            [In] SafeSocketHandle socketHandle,
            [In] int ioControlCode,
            [In, Out] ref Guid guid,
            [In] int guidSize,
            [Out] out IntPtr funcPtr,
            [In]  int funcPtrSize,
            [Out] out int bytesTransferred,
            [In] IntPtr shouldBeNull,
            [In] IntPtr shouldBeNull2);

        [DllImport(Interop.Libraries.Ws2_32, SetLastError = true, EntryPoint = "WSAIoctl")]
        internal static extern SocketError WSAIoctl_Blocking(
            [In] IntPtr socketHandle,
            [In] int ioControlCode,
            [In] byte[] inBuffer,
            [In] int inBufferSize,
            [Out] byte[] outBuffer,
            [In] int outBufferSize,
            [Out] out int bytesTransferred,
            [In] IntPtr overlapped,
            [In] IntPtr completionRoutine);
    }
}
