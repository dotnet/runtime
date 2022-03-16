// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class WebSocket
    {
        [LibraryImport(Libraries.WebSocket)]
        internal static partial int WebSocketBeginClientHandshake(
            SafeHandle webSocketHandle,
            IntPtr subProtocols,
            uint subProtocolCount,
            IntPtr extensions,
            uint extensionCount,
            HttpHeader[] initialHeaders,
            uint initialHeaderCount,
            out IntPtr additionalHeadersPtr,
            out uint additionalHeaderCount);
    }
}
