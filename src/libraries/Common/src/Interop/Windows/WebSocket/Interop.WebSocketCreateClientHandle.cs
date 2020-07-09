// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Security;

internal static partial class Interop
{
    internal static partial class WebSocket
    {
        [DllImport(Libraries.WebSocket)]
        internal static extern int WebSocketCreateClientHandle(
           [In] Property[] properties,
           [In] uint propertyCount,
           [Out] out SafeWebSocketHandle webSocketHandle);
    }
}
