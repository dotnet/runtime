// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Threading;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static unsafe extern bool WSAGetOverlappedResult(
            [In] SafeSocketHandle socketHandle,
            [In] NativeOverlapped* overlapped,
            [Out] out uint bytesTransferred,
            [In] bool wait,
            [Out] out SocketFlags socketFlags);
    }
}
