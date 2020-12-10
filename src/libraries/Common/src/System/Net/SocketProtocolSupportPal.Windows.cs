// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Runtime.InteropServices;
#if !SYSTEM_NET_SOCKETS_DLL
using SocketType = System.Net.Internals.SocketType;
#endif

namespace System.Net
{
    internal static partial class SocketProtocolSupportPal
    {
        private static bool IsSupported(AddressFamily af)
        {
            Interop.Winsock.EnsureInitialized();

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
