// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

internal static partial class Interop
{
    internal static partial class Mswsock
    {
        [DllImport(Interop.Libraries.Mswsock, SetLastError = true)]
        internal static extern unsafe bool TransmitFile(
            SafeHandle socket,
            IntPtr fileHandle,
            int numberOfBytesToWrite,
            int numberOfBytesPerSend,
            NativeOverlapped* overlapped,
            TransmitFileBuffers* buffers,
            TransmitFileOptions flags);

        [StructLayout(LayoutKind.Sequential)]
        internal struct TransmitFileBuffers
        {
            internal IntPtr Head;
            internal int HeadLength;
            internal IntPtr Tail;
            internal int TailLength;
        }
    }
}
