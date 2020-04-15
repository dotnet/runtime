// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetSocketType")]
        internal static extern Error GetSocketType(SafeSocketHandle socket, out AddressFamily addressFamily, out SocketType socketType, out ProtocolType protocolType);
    }
}
