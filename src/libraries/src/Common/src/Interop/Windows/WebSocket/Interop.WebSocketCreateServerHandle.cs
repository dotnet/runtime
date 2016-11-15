// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Security;

internal static partial class Interop
{
    internal static partial class WebSocket
    {
        [DllImport(Libraries.WebSocket)]
        [SuppressUnmanagedCodeSecurity]
        internal static extern int WebSocketCreateServerHandle(
            [In]Property[] properties,
            [In] uint propertyCount,
            [Out] out SafeWebSocketHandle webSocketHandle);
    }
}
