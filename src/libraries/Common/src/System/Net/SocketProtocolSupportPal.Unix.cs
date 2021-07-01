// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Internals;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace System.Net
{
    internal static class SocketProtocolSupportPal
    {
        public static bool OSSupportsIPv6 { get; } = IsSupported(AddressFamily.InterNetworkV6);
        public static bool OSSupportsIPv4 { get; } = IsSupported(AddressFamily.InterNetwork);
        public static bool OSSupportsUnixDomainSockets { get; } = IsSupported(AddressFamily.Unix);

        private static unsafe bool IsSupported(AddressFamily af)
        {
            IntPtr invalid = (IntPtr)(-1);
            IntPtr socket = invalid;
            try
            {
                return Interop.Sys.Socket(af, SocketType.Dgram, 0, &socket) != Interop.Error.EAFNOSUPPORT;
            }
            finally
            {
                if (socket != invalid)
                {
                    Interop.Sys.Close(socket);
                }
            }
        }
    }
}
