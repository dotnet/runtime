// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            [In] SafeCloseSocket socketHandle,
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
            [In] SafeHandle overlapped,
            [In] IntPtr completionRoutine);

        [DllImport(Interop.Libraries.Ws2_32, SetLastError = true, EntryPoint = "WSAIoctl")]
        internal static extern SocketError WSAIoctl_Blocking_Internal(
            [In]  IntPtr socketHandle,
            [In]  uint ioControlCode,
            [In]  IntPtr inBuffer,
            [In]  int inBufferSize,
            [Out] IntPtr outBuffer,
            [In]  int outBufferSize,
            [Out] out int bytesTransferred,
            [In]  SafeHandle overlapped,
            [In]  IntPtr completionRoutine);
    }
}
