// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Internals;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace System.Net
{
    internal static partial class SocketProtocolSupportPal
    {
        private static unsafe bool IsSupported(AddressFamily af)
        {
            IntPtr invalid = (IntPtr)(-1);
            IntPtr socket = invalid;
            try
            {
                Interop.Error result = Interop.Sys.Socket(af, SocketType.Dgram, 0, &socket);
                // we get EAFNOSUPPORT when family is not supported by Kernel, EPROTONOSUPPORT may come from policy enforcement like FreeBSD jail()
                return result != Interop.Error.EAFNOSUPPORT && result != Interop.Error.EPROTONOSUPPORT;
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
