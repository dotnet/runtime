// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security;
using static System.Net.WebSockets.WebSocketProtocolComponent;

internal static partial class Interop
{
    internal static partial class WebSocket
    {
        [DllImport(Libraries.WebSocket, EntryPoint = "WebSocketSend", ExactSpelling = true)]
        internal static extern int WebSocketSend_Raw(
            [In] SafeHandle webSocketHandle,
            [In] BufferType bufferType,
            [In] ref Buffer buffer,
            [In] IntPtr applicationContext);

        [DllImport(Libraries.WebSocket, EntryPoint = "WebSocketSend", ExactSpelling = true)]
        internal static extern int WebSocketSendWithoutBody_Raw(
            [In] SafeHandle webSocketHandle,
            [In] BufferType bufferType,
            [In] IntPtr buffer,
            [In] IntPtr applicationContext);
    }
}
