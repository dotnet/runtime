// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace System.Net
{
    internal static partial class SocketProtocolSupportPal
    {
        private static bool IsSupported(AddressFamily af)
        {
            const int StreamSocketType = 1;
            Interop.Winsock.EnsureInitialized();

            IntPtr INVALID_SOCKET = (IntPtr)(-1);
            IntPtr socket = INVALID_SOCKET;
            try
            {
                socket = Interop.Winsock.WSASocketW(af, StreamSocketType, 0, IntPtr.Zero, 0, (int)Interop.Winsock.SocketConstructorFlags.WSA_FLAG_NO_HANDLE_INHERIT);
                return
                    socket != INVALID_SOCKET ||
                    (SocketError)Marshal.GetLastPInvokeError() != SocketError.AddressFamilyNotSupported;
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
