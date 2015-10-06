// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        // Blocking call - requires IntPtr instead of SafeCloseSocket.
        [DllImport(Interop.Libraries.Ws2_32, ExactSpelling = true, SetLastError = true)]
        internal static extern SafeCloseSocket.InnerSafeCloseSocket accept(
            [In] IntPtr socketHandle,
            [Out] byte[] socketAddress,
            [In, Out] ref int socketAddressSize);
    }
}
