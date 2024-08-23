// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial IntPtr WSASocketW(
            AddressFamily addressFamily,
            int socketType,
            int protocolType,
            IntPtr protocolInfo,
            int group,
            int flags);
    }
}
