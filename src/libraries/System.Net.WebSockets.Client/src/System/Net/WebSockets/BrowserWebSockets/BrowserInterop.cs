// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.InteropServices;

namespace System.Net.WebSockets
{
    internal static partial class BrowserInterop
    {
        public static string? GetProtocol(JSObject? webSocket)
        {
            if (webSocket == null || webSocket.IsDisposed) return null;
            string? protocol = webSocket.GetPropertyAsString("protocol");
            return protocol;
        }

        public static int GetReadyState(JSObject? webSocket)
        {
            if (webSocket == null || webSocket.IsDisposed) return -1;
            int? readyState = webSocket.GetPropertyAsInt32("readyState");
            if (!readyState.HasValue) return -1;
            return readyState.Value;
        }

        [JSImport("INTERNAL.ws_wasm_create")]
        public static partial JSObject WebSocketCreate(
            string uri,
            string?[]? subProtocols,
            [JSMarshalAs<JSType.Function<JSType.Number, JSType.String>>] Action<int, string> onClosed);

        [JSImport("INTERNAL.ws_wasm_open")]
        public static partial Task WebSocketOpen(
            JSObject webSocket);

        [JSImport("INTERNAL.ws_wasm_send")]
        public static partial Task? WebSocketSend(
            JSObject webSocket,
            [JSMarshalAs<JSType.MemoryView>] ArraySegment<byte> buffer,
            int messageType,
            bool endOfMessage);

        [JSImport("INTERNAL.ws_wasm_receive")]
        public static partial Task? WebSocketReceive(
            JSObject webSocket,
            [JSMarshalAs<JSType.MemoryView>] ArraySegment<byte> buffer,
            [JSMarshalAs<JSType.MemoryView>] ArraySegment<int> response);

        [JSImport("INTERNAL.ws_wasm_close")]
        public static partial Task? WebSocketClose(
            JSObject webSocket,
            int code,
            string? reason,
            bool waitForCloseReceived);

        [JSImport("INTERNAL.ws_wasm_abort")]
        public static partial void WebSocketAbort(
            JSObject webSocket);

    }
}
