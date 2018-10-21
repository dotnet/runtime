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
        [DllImport(Interop.Libraries.Ws2_32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern SafeSocketHandle.InnerSafeCloseSocket WSASocketW(
                                                [In] AddressFamily addressFamily,
                                                [In] SocketType socketType,
                                                [In] ProtocolType protocolType,
                                                [In] IntPtr protocolInfo,
                                                [In] uint group,
                                                [In] SocketConstructorFlags flags);
    }
}
