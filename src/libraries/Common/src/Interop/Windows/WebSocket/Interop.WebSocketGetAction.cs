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
        [LibraryImport(Libraries.WebSocket)]
        internal static partial int WebSocketGetAction(
            SafeHandle webSocketHandle,
            ActionQueue actionQueue,
            Buffer[] dataBuffers,
            ref uint dataBufferCount,
            out System.Net.WebSockets.WebSocketProtocolComponent.Action action,
            out BufferType bufferType,
            out IntPtr applicationContext,
            out IntPtr actionContext);
    }
}
