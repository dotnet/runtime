// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Net.Sockets;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static extern SocketError WSARecv(
            [In] SafeCloseSocket socketHandle,
            [In] ref WSABuffer buffer,
            [In] int bufferCount,
            [Out] out int bytesTransferred,
            [In, Out] ref SocketFlags socketFlags,
            [In] SafeHandle overlapped,
            [In] IntPtr completionRoutine);

        [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static extern SocketError WSARecv(
            [In] SafeCloseSocket socketHandle,
            [In, Out] WSABuffer[] buffers,
            [In] int bufferCount,
            [Out] out int bytesTransferred,
            [In, Out] ref SocketFlags socketFlags,
            [In] SafeHandle overlapped,
            [In] IntPtr completionRoutine);

        [DllImport(Interop.Libraries.Ws2_32, SetLastError = true, EntryPoint = "WSARecv")]
        internal static extern SocketError WSARecv_Blocking(
            [In] IntPtr socketHandle,
            [In, Out] WSABuffer[] buffers,
            [In] int bufferCount,
            [Out] out int bytesTransferred,
            [In, Out] ref SocketFlags socketFlags,
            [In] SafeHandle overlapped,
            [In] IntPtr completionRoutine);
    }
}
