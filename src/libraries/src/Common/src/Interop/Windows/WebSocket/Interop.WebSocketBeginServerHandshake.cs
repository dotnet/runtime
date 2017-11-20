// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security;

internal static partial class Interop
{
    internal static partial class WebSocket
    {
        [DllImport(Libraries.WebSocket)]
        internal static extern int WebSocketBeginServerHandshake(
            [In] SafeHandle webSocketHandle,
            [In] IntPtr subProtocol,
            [In] IntPtr extensions,
            [In] uint extensionCount,
            [In] HttpHeader[] requestHeaders,
            [In] uint requestHeaderCount,
            [Out] out IntPtr responseHeadersPtr,
            [Out] out uint responseHeaderCount);
    }
}
