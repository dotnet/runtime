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
        internal static extern int WebSocketBeginClientHandshake(
            [In] SafeHandle webSocketHandle,
            [In] IntPtr subProtocols,
            [In] uint subProtocolCount,
            [In] IntPtr extensions,
            [In] uint extensionCount,
            [In] HttpHeader[] initialHeaders,
            [In] uint initialHeaderCount,
            [Out] out IntPtr additionalHeadersPtr,
            [Out] out uint additionalHeaderCount);
#pragma warning restore DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
    }
}
