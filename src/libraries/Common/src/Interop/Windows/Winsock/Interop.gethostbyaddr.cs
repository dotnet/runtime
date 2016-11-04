// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Net.Sockets;
#if !SYSTEM_NET_SOCKETS_DLL
using ProtocolFamily = System.Net.Internals.ProtocolFamily;
#endif

internal static partial class Interop
{
    internal static partial class Winsock
    {
        [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static extern IntPtr gethostbyaddr(
                                              [In] ref int addr,
                                              [In] int len,
                                              [In] ProtocolFamily type
                                              );
    }
}
