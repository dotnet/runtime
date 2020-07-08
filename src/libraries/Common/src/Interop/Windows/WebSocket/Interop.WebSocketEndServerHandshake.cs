// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class WebSocket
    {
        [DllImport(Libraries.WebSocket)]
        internal static extern int WebSocketEndServerHandshake([In] SafeHandle webSocketHandle);
    }
}
