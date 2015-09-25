// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Net.Sockets;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        [DllImport(Interop.Libraries.Ws2_32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern SafeCloseSocket.InnerSafeCloseSocket WSASocketW(
                                                [In] AddressFamily addressFamily,
                                                [In] SocketType socketType,
                                                [In] ProtocolType protocolType,
                                                [In] IntPtr protocolInfo,
                                                [In] uint group,
                                                [In] SocketConstructorFlags flags);

        [DllImport(Interop.Libraries.Ws2_32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal unsafe static extern SafeCloseSocket.InnerSafeCloseSocket WSASocketW(
                                    [In] AddressFamily addressFamily,
                                    [In] SocketType socketType,
                                    [In] ProtocolType protocolType,
                                    [In] byte* pinnedBuffer,
                                    [In] uint group,
                                    [In] SocketConstructorFlags flags);
    }
}
