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
        [DllImport(Libraries.WebSocket)]
        [SuppressUnmanagedCodeSecurity]
        internal static extern int WebSocketGetAction(
            [In] SafeHandle webSocketHandle,
            [In] ActionQueue actionQueue,
            [In, Out] Buffer[] dataBuffers,
            [In, Out] ref uint dataBufferCount,
            [Out] out System.Net.WebSockets.WebSocketProtocolComponent.Action action,
            [Out] out BufferType bufferType,
            [Out] out IntPtr applicationContext,
            [Out] out IntPtr actionContext);
    }
}
