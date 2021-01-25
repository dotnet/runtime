// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security;

internal static partial class Interop
{
    internal static partial class WebSocket
    {
        [DllImport(Libraries.WebSocket)]
        internal static extern void WebSocketDeleteHandle(
            [In] IntPtr webSocketHandle);
    }
}
