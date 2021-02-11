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
        internal static extern unsafe SocketError WSASend(
            SafeHandle socketHandle,
            WSABuffer* buffers,
            int bufferCount,
            out int bytesTransferred,
            SocketFlags socketFlags,
            NativeOverlapped* overlapped,
            IntPtr completionRoutine);

        internal static unsafe SocketError WSASend(
            SafeHandle socketHandle,
            Span<WSABuffer> buffers,
            int bufferCount,
            out int bytesTransferred,
            SocketFlags socketFlags,
            NativeOverlapped* overlapped,
            IntPtr completionRoutine)
        {
            Debug.Assert(!buffers.IsEmpty);
            fixed (WSABuffer* buffersPtr = &MemoryMarshal.GetReference(buffers))
            {
                return WSASend(socketHandle, buffersPtr, bufferCount, out bytesTransferred, socketFlags, overlapped, completionRoutine);
            }
        }
    }
}
