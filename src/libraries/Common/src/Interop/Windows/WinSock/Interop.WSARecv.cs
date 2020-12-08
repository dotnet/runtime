// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        [DllImport(Libraries.Ws2_32, SetLastError = true)]
        internal static extern unsafe SocketError WSARecv(
            SafeHandle socketHandle,
            WSABuffer* buffer,
            int bufferCount,
            out int bytesTransferred,
            ref SocketFlags socketFlags,
            NativeOverlapped* overlapped,
            IntPtr completionRoutine);

        internal static unsafe SocketError WSARecv(
            SafeHandle socketHandle,
            Span<WSABuffer> buffers,
            int bufferCount,
            out int bytesTransferred,
            ref SocketFlags socketFlags,
            NativeOverlapped* overlapped,
            IntPtr completionRoutine)
        {
            Debug.Assert(!buffers.IsEmpty);
            fixed (WSABuffer* buffersPtr = &MemoryMarshal.GetReference(buffers))
            {
                return WSARecv(socketHandle, buffersPtr, bufferCount, out bytesTransferred, ref socketFlags, overlapped, completionRoutine);
            }
        }
    }
}
