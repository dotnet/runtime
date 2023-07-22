// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
#if SYSTEM_NET_SOCKETS_DLL
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_Socket")]
        internal static unsafe partial Error Socket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType, IntPtr* socket);
#endif
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_Socket")]
        internal static unsafe partial Error Socket(int addressFamily, int socketType, int protocolType, IntPtr* socket);
    }
}
