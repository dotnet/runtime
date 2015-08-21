// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net
{
    internal class SocketProtocolSupportPal
    {
        private static bool s_ipV4 = true;
        private static bool s_ipV6 = true;

        private static bool s_initialized;
        private static readonly object s_initializedLock = new object();

        public static bool OSSupportsIPv6
        {
            get
            {
                EnsureInitialized();
                return s_ipV6;
            }
        }

        public static bool OSSupportsIPv4
        {
            get
            {
                EnsureInitialized();
                return s_ipV4;
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
                        s_ipV4 = IsProtocolSupported(AddressFamily.InterNetwork);
                        s_ipV6 = IsProtocolSupported(AddressFamily.InterNetworkV6);

                        Volatile.Write(ref s_initialized, true);
                    }
                }
            }
        }

        private static bool IsProtocolSupported(AddressFamily af)
        {
            int family;
            switch (af)
            {
                case AddressFamily.InterNetwork:
                    family = Interop.libc.AF_INET;
                    break;
                case AddressFamily.InterNetworkV6:
                    family = Interop.libc.AF_INET6;
                    break;
                default:
                    Debug.Fail("Invalid address family: " + af.ToString());
                    throw new ArgumentException("af");
            }

            int socket = -1;
            try
            {
                socket = Interop.libc.socket(family, Interop.libc.SOCK_DGRAM, 0);
                if (socket == -1)
                {
                    return Interop.Sys.GetLastError() != Interop.Error.EAFNOSUPPORT;
                }
                return true;
            }
            finally
            {
                if (socket != -1)
                {
                    Interop.Sys.Close(socket);
                }
            }
        }
    }
}
