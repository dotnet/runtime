// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Net.Internals;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

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

        private static unsafe bool IsProtocolSupported(AddressFamily af)
        {
            int socket = -1;
            try
            {
                Interop.Error err = Interop.Sys.Socket(af, SocketType.Dgram, (ProtocolType)0, &socket);
                return err != Interop.Error.EAFNOSUPPORT;
            }
            finally
            {
                if (socket != -1)
                {
                    Interop.Sys.Close((IntPtr)socket);
                }
            }
        }
    }
}
