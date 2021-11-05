// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        [GeneratedDllImport(Interop.Libraries.Ws2_32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static partial IntPtr WSASocketW(
            AddressFamily addressFamily,
            SocketType socketType,
            ProtocolType protocolType,
            IntPtr protocolInfo,
            uint group,
            SocketConstructorFlags flags);
    }
}
