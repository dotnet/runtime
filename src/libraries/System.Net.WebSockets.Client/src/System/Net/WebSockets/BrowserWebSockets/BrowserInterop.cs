// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    internal static partial class BrowserInterop
    {
        public static string? GetProtocol(JSObject? webSocket)
        {
            if (webSocket == null || webSocket.IsDisposed)
            {
                return null;
            }

            string? protocol = webSocket.GetPropertyAsString("protocol");
            return protocol;
        }

        public static WebSocketCloseStatus? GetCloseStatus(JSObject? webSocket)
        {
            if (webSocket == null || webSocket.IsDisposed)
            {
                return null;
            }
            if (!webSocket.HasProperty("close_status"))
            {
                return null;
            }

            int status = webSocket.GetPropertyAsInt32("close_status");
            return (WebSocketCloseStatus)status;
        }

        public static string? GetCloseStatusDescription(JSObject? webSocket)
        {
            if (webSocket == null || webSocket.IsDisposed)
            {
                return null;
            }

            string? description = webSocket.GetPropertyAsString("close_status_description");
            return description;
        }

        public static int GetReadyState(JSObject? webSocket)
        {
            if (webSocket == null || webSocket.IsDisposed)
            {
                return -1;
            }

            return BrowserInterop.WebSocketGetState(webSocket);
        }

        [JSImport("INTERNAL.wsGetState")]
        public static partial int WebSocketGetState(
            JSObject webSocket);

        [JSImport("INTERNAL.wsCreate")]
        public static partial JSObject WebSocketCreate(
            string uri,
            string?[]? subProtocols,
            IntPtr responseStatusPtr);

        public static unsafe JSObject UnsafeCreate(
            string uri,
            string?[]? subProtocols,
            MemoryHandle responseHandle)
        {
            return WebSocketCreate(uri, subProtocols, (IntPtr)responseHandle.Pointer);
        }

        [JSImport("INTERNAL.wsOpen")]
        public static partial Task WebSocketOpen(
            JSObject webSocket);

        [JSImport("INTERNAL.wsSend")]
        public static partial Task? WebSocketSend(
            JSObject webSocket,
            IntPtr bufferPtr,
            int bufferLength,
            int messageType,
            bool endOfMessage);

        public static unsafe Task? UnsafeSend(JSObject jsWs, MemoryHandle pinBuffer, int length, WebSocketMessageType messageType, bool endOfMessage)
        {
            return WebSocketSend(jsWs, (IntPtr)pinBuffer.Pointer, length, (int)messageType, endOfMessage);
        }

        [JSImport("INTERNAL.wsReceive")]
        public static partial Task? WebSocketReceive(
            JSObject webSocket,
            IntPtr bufferPtr,
            int bufferLength);

        public static unsafe Task? ReceiveUnsafe(JSObject jsWs, MemoryHandle pinBuffer, int length)
        {
            return WebSocketReceive(jsWs, (IntPtr)pinBuffer.Pointer, length);
        }

        [JSImport("INTERNAL.wsClose")]
        public static partial Task? WebSocketClose(
            JSObject webSocket,
            int code,
            string? reason,
            bool waitForCloseReceived);

        [JSImport("INTERNAL.wsAbort")]
        public static partial void WebSocketAbort(
            JSObject webSocket);

    }
}
