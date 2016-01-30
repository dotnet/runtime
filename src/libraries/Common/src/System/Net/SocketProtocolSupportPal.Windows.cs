// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
#if !SYSTEM_NET_SOCKETS_DLL
using SocketType = System.Net.Internals.SocketType;
#endif

namespace System.Net
{
    internal class SocketProtocolSupportPal
    {
        private static bool s_ipv4 = true;
        private static bool s_ipv6 = true;

        private static bool s_initialized;
        private static readonly object s_initializedLock = new object();

        public static bool OSSupportsIPv6
        {
            get
            {
                EnsureInitialized();
                return s_ipv6;
            }
        }

        public static bool OSSupportsIPv4
        {
            get
            {
                EnsureInitialized();
                return s_ipv4;
            }
        }

        private static void EnsureInitialized()
        {
            if (!Volatile.Read(ref s_initialized))
            {
                lock (s_initializedLock)
                {
                    if (!s_initialized)
                    {
                        s_ipv4 = IsProtocolSupported(AddressFamily.InterNetwork);
                        s_ipv6 = IsProtocolSupported(AddressFamily.InterNetworkV6);

                        Volatile.Write(ref s_initialized, true);
                    }
                }
            }
        }

        private static bool IsProtocolSupported(AddressFamily af)
        {
            SocketError errorCode;
            IntPtr s = IntPtr.Zero;
            bool ret = true;

            try
            {
                s = Interop.Winsock.WSASocketW(af, SocketType.Dgram, 0, IntPtr.Zero, 0, 0);

                if (s == IntPtr.Zero)
                {
                    errorCode = (SocketError)Marshal.GetLastWin32Error();
                    if (errorCode == SocketError.AddressFamilyNotSupported)
                    {
                        ret = false;
                    }
                }
            }
            finally
            {
                if (s != IntPtr.Zero)
                {
                    SocketError closeResult = Interop.Winsock.closesocket(s);
#if DEBUG
                    if (closeResult != SocketError.Success)
                    {
                        errorCode = (SocketError)Marshal.GetLastWin32Error();
                        Debug.Fail("Failed to detect " + af.ToString() + " protocol: " + errorCode.ToString());
                    }
#endif
                }
            }

            return ret;
        }
    }
}
