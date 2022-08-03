// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using System.Runtime.InteropServices.JavaScript;
using System.Buffers;

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
            IntPtr responseStatusPtr,
            [JSMarshalAs<JSType.Function<JSType.Number, JSType.String>>] Action<int, string> onClosed);

        public static unsafe JSObject UnsafeCreate(
            string uri,
            string?[]? subProtocols,
            MemoryHandle responseHandle,
            [JSMarshalAs<JSType.Function<JSType.Number, JSType.String>>] Action<int, string> onClosed)
        {
            return WebSocketCreate(uri, subProtocols, (IntPtr)responseHandle.Pointer, onClosed);
        }

        [JSImport("INTERNAL.ws_wasm_open")]
        public static partial Task WebSocketOpen(
            JSObject webSocket);

        [JSImport("INTERNAL.ws_wasm_send")]
        public static partial Task? WebSocketSend(
            JSObject webSocket,
            IntPtr bufferPtr,
            int bufferLength,
            int messageType,
            bool endOfMessage);

        public static unsafe Task? UnsafeSendSync(JSObject jsWs, ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage)
        {
            if (buffer.Count == 0)
            {
                return WebSocketSend(jsWs, IntPtr.Zero, 0, (int)messageType, endOfMessage);
            }

            var span = buffer.AsSpan();
            // we can do this because the bytes in the buffer are always consumed synchronously (not later with Task resolution)
            fixed (void* spanPtr = span)
            {
                return WebSocketSend(jsWs, (IntPtr)spanPtr, buffer.Count, (int)messageType, endOfMessage);
            }
        }

        [JSImport("INTERNAL.ws_wasm_receive")]
        public static partial Task? WebSocketReceive(
            JSObject webSocket,
            IntPtr bufferPtr,
            int bufferLength);

        public static unsafe Task? ReceiveUnsafeSync(JSObject jsWs, MemoryHandle pinBuffer, int length)
        {
            return WebSocketReceive(jsWs, (IntPtr)pinBuffer.Pointer, length);
        }

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
