// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
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
