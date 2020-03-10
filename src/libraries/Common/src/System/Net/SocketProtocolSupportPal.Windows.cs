// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;
using System.Runtime.InteropServices;
#if !SYSTEM_NET_SOCKETS_DLL
using SocketType = System.Net.Internals.SocketType;
#endif

namespace System.Net
{
    internal static class SocketProtocolSupportPal
    {
        public static bool OSSupportsIPv6 { get; } = IsSupported(AddressFamily.InterNetworkV6);
        public static bool OSSupportsIPv4 { get; } = IsSupported(AddressFamily.InterNetwork);
        public static bool OSSupportsUnixDomainSockets { get; } = IsSupported(AddressFamily.Unix);

        private static bool IsSupported(AddressFamily af)
        {
            IntPtr INVALID_SOCKET = (IntPtr)(-1);
            IntPtr socket = INVALID_SOCKET;
            try
            {
                socket = Interop.Winsock.WSASocketW(af, SocketType.Stream, 0, IntPtr.Zero, 0, (int)Interop.Winsock.SocketConstructorFlags.WSA_FLAG_NO_HANDLE_INHERIT);
                return
                    socket != INVALID_SOCKET ||
                    (SocketError)Marshal.GetLastWin32Error() != SocketError.AddressFamilyNotSupported;
            }
            finally
            {
                if (socket != INVALID_SOCKET)
                {
                    Interop.Winsock.closesocket(socket);
                }
            }
        }
    }
}
