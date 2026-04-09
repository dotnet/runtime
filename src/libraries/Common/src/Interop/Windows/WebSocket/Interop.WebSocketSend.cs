// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security;
using static System.Net.WebSockets.WebSocketProtocolComponent;

internal static partial class Interop
{
    internal static partial class WebSocket
    {
        [LibraryImport(Libraries.WebSocket, EntryPoint = "WebSocketSend")]
        internal static partial int WebSocketSend_Raw(
            SafeHandle webSocketHandle,
            BufferType bufferType,
            ref Buffer buffer,
            IntPtr applicationContext);

        [LibraryImport(Libraries.WebSocket, EntryPoint = "WebSocketSend")]
        internal static partial int WebSocketSendWithoutBody_Raw(
            SafeHandle webSocketHandle,
            BufferType bufferType,
            IntPtr buffer,
            IntPtr applicationContext);
    }
}
