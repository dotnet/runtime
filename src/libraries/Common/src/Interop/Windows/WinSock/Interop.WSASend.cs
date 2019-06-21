// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static extern unsafe SocketError WSASend(
            IntPtr socketHandle,
            WSABuffer* buffers,
            int bufferCount,
            out int bytesTransferred,
            SocketFlags socketFlags,
            NativeOverlapped* overlapped,
            IntPtr completionRoutine);

        [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
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
            ref WSABuffer buffer,
            int bufferCount,
            out int bytesTransferred,
            SocketFlags socketFlags,
            NativeOverlapped* overlapped,
            IntPtr completionRoutine)
        {
            // We intentionally do NOT copy this back after the function completes:
            // We don't want to cause a race in async scenarios.
            // The WSABuffer struct should be unchanged anyway.
            WSABuffer localBuffer = buffer;
            return WSASend(socketHandle, &localBuffer, bufferCount, out bytesTransferred, socketFlags, overlapped, completionRoutine);
        }

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

        internal static unsafe SocketError WSASend(
            IntPtr socketHandle,
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
