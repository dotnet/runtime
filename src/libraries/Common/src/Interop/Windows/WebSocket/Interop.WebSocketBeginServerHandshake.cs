// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class WebSocket
    {
#pragma warning disable DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        [DllImport(Libraries.WebSocket)]
        // TODO: [DllImportGenerator] Switch to use GeneratedDllImport once we support non-blittable structs.
        internal static extern int WebSocketBeginServerHandshake(
            [In] SafeHandle webSocketHandle,
            [In] IntPtr subProtocol,
            [In] IntPtr extensions,
            [In] uint extensionCount,
            [In] HttpHeader[] requestHeaders,
            [In] uint requestHeaderCount,
            [Out] out IntPtr responseHeadersPtr,
            [Out] out uint responseHeaderCount);
#pragma warning restore DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
    }
}
