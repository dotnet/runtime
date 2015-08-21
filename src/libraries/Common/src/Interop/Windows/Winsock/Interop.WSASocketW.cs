// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Net.Sockets;
#if !SYSTEM_NET_SOCKETS_DLL
using SocketType = System.Net.Internals.SocketType;
#endif

internal static partial class Interop
{
    internal static partial class Winsock
    {
        [DllImport(Interop.Libraries.Ws2_32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr WSASocketW(
                                        [In] AddressFamily addressFamily,
                                        [In] SocketType socketType,
                                        [In] int protocolType,
                                        [In] IntPtr protocolInfo,
                                        [In] int group,
                                        [In] int flags);
    }
}
