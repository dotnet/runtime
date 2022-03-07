// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security;

internal static partial class Interop
{
    internal static partial class WebSocket
    {
        [GeneratedDllImport(Libraries.WebSocket)]
        internal static partial void WebSocketCompleteAction(
            SafeHandle webSocketHandle,
            IntPtr actionContext,
            uint bytesTransferred);
    }
}
