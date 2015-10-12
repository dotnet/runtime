// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        // Used as last parameter to WSASocket call.
        [Flags]
        internal enum SocketConstructorFlags
        {
            WSA_FLAG_OVERLAPPED = 0x01,
            WSA_FLAG_MULTIPOINT_C_ROOT = 0x02,
            WSA_FLAG_MULTIPOINT_C_LEAF = 0x04,
            WSA_FLAG_MULTIPOINT_D_ROOT = 0x08,
            WSA_FLAG_MULTIPOINT_D_LEAF = 0x10,
        }

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
