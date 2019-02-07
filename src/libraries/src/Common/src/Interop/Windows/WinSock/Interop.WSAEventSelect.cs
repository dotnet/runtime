// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static extern SocketError WSAEventSelect(
            [In] SafeSocketHandle socketHandle,
            [In] SafeHandle Event,
            [In] AsyncEventBits NetworkEvents);

        [Flags]
        internal enum AsyncEventBits
        {
            FdNone = 0,
            FdRead = 1 << 0,
            FdWrite = 1 << 1,
            FdOob = 1 << 2,
            FdAccept = 1 << 3,
            FdConnect = 1 << 4,
            FdClose = 1 << 5,
            FdQos = 1 << 6,
            FdGroupQos = 1 << 7,
            FdRoutingInterfaceChange = 1 << 8,
            FdAddressListChange = 1 << 9,
            FdAllEvents = (1 << 10) - 1,
        }
    }
}
