// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace System.Net
{
    internal static partial class SocketProtocolSupportPal
    {
        private const int DgramSocketType = 2;

        private static unsafe bool IsSupported(AddressFamily af)
        {
            // Check for AF_UNIX on iOS/tvOS. The OS claims to support this, but returns EPERM on bind.
            // We should explicitly set the return here to false, to avoid giving a false impression.
            if (af == AddressFamily.Unix && (OperatingSystem.IsTvOS() || (OperatingSystem.IsIOS() && !OperatingSystem.IsMacCatalyst())))
            {
                return false;
            }
            IntPtr invalid = (IntPtr)(-1);
            IntPtr socket = invalid;
            try
            {
                Interop.Error result = Interop.Sys.Socket((int)af, DgramSocketType, 0, &socket);
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
