// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool WSAGetOverlappedResult(
            SafeSocketHandle socketHandle,
            NativeOverlapped* overlapped,
            out uint bytesTransferred,
            [MarshalAs(UnmanagedType.Bool)] bool wait,
            out SocketFlags socketFlags);
    }
}
