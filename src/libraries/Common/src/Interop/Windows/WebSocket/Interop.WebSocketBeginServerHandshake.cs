// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class WebSocket
    {
        [LibraryImport(Libraries.WebSocket)]
        internal static partial int WebSocketBeginServerHandshake(
            SafeHandle webSocketHandle,
            IntPtr subProtocol,
            IntPtr extensions,
            uint extensionCount,
            HttpHeader[] requestHeaders,
            uint requestHeaderCount,
            out IntPtr responseHeadersPtr,
            out uint responseHeaderCount);
    }
}
