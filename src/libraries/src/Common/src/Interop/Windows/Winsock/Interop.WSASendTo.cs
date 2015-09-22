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
        internal static extern SocketError WSASendTo(
            [In] SafeCloseSocket socketHandle,
            [In] ref WSABuffer buffer,
            [In] int bufferCount,
            [Out] out int bytesTransferred,
            [In] SocketFlags socketFlags,
            [In] IntPtr socketAddress,
            [In] int socketAddressSize,
            [In] SafeHandle overlapped,
            [In] IntPtr completionRoutine);

        [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static extern SocketError WSASendTo(
            [In] SafeCloseSocket socketHandle,
            [In] WSABuffer[] buffersArray,
            [In] int bufferCount,
            [Out] out int bytesTransferred,
            [In] SocketFlags socketFlags,
            [In] IntPtr socketAddress,
            [In] int socketAddressSize,
            [In] SafeNativeOverlapped overlapped,
            [In] IntPtr completionRoutine);
    }
}
