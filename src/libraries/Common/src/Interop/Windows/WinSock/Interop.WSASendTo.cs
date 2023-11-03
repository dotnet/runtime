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
        [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        private static unsafe partial SocketError WSASendTo(
            SafeHandle socketHandle,
            WSABuffer* buffers,
            int bufferCount,
            out int bytesTransferred,
            SocketFlags socketFlags,
            ReadOnlySpan<byte> socketAddress,
            int socketAddressSize,
            NativeOverlapped* overlapped,
            IntPtr completionRoutine);

        internal static unsafe SocketError WSASendTo(
            SafeHandle socketHandle,
            ref WSABuffer buffer,
            int bufferCount,
            out int bytesTransferred,
            SocketFlags socketFlags,
            ReadOnlySpan<byte> socketAddress,
            NativeOverlapped* overlapped,
            IntPtr completionRoutine)
        {
            // We intentionally do NOT copy this back after the function completes:
            // We don't want to cause a race in async scenarios.
            // The WSABuffer struct should be unchanged anyway.
            WSABuffer localBuffer = buffer;
            return WSASendTo(socketHandle, &localBuffer, bufferCount, out bytesTransferred, socketFlags, socketAddress, socketAddress.Length, overlapped, completionRoutine);
        }

        internal static unsafe SocketError WSASendTo(
            SafeHandle socketHandle,
            WSABuffer[] buffers,
            int bufferCount,
            [Out] out int bytesTransferred,
            SocketFlags socketFlags,
            ReadOnlySpan<byte> socketAddress,
            NativeOverlapped* overlapped,
            IntPtr completionRoutine)
        {
            Debug.Assert(buffers != null && buffers.Length > 0);
            fixed (WSABuffer* buffersPtr = &buffers[0])
            {
                return WSASendTo(socketHandle, buffersPtr, bufferCount, out bytesTransferred, socketFlags, socketAddress, socketAddress.Length, overlapped, completionRoutine);
            }
        }
    }
}
