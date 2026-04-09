// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    internal static partial class HttpWebSocket
    {
        private const string SupportedVersion = "13";

        internal static async Task<HttpListenerWebSocketContext> AcceptWebSocketAsyncCore(HttpListenerContext context,
            string? subProtocol,
            int receiveBufferSize,
            TimeSpan keepAliveInterval)
        {
            ValidateOptions(subProtocol, receiveBufferSize, MinSendBufferSize, keepAliveInterval);

            // get property will create a new response if one doesn't exist.
            HttpListenerResponse response = context.Response;
            HttpListenerRequest request = context.Request;
            ValidateWebSocketHeaders(context);

            string? secWebSocketVersion = request.Headers[HttpKnownHeaderNames.SecWebSocketVersion];

            // Optional for non-browser client
            string? origin = request.Headers[HttpKnownHeaderNames.Origin];

            string[]? secWebSocketProtocols = null;
            string outgoingSecWebSocketProtocolString;
            bool shouldSendSecWebSocketProtocolHeader =
                ProcessWebSocketProtocolHeader(
                    request.Headers[HttpKnownHeaderNames.SecWebSocketProtocol],
                    subProtocol,
                    out outgoingSecWebSocketProtocolString);

            if (shouldSendSecWebSocketProtocolHeader)
            {
                secWebSocketProtocols = new string[] { outgoingSecWebSocketProtocolString };
                response.Headers.Add(HttpKnownHeaderNames.SecWebSocketProtocol, outgoingSecWebSocketProtocolString);
            }

            // negotiate the websocket key return value
            string? secWebSocketKey = request.Headers[HttpKnownHeaderNames.SecWebSocketKey];
            string secWebSocketAccept = HttpWebSocket.GetSecWebSocketAcceptString(secWebSocketKey);

            response.Headers.Add(HttpKnownHeaderNames.Connection, HttpKnownHeaderNames.Upgrade);
            response.Headers.Add(HttpKnownHeaderNames.Upgrade, WebSocketUpgradeToken);
            response.Headers.Add(HttpKnownHeaderNames.SecWebSocketAccept, secWebSocketAccept);

            response.StatusCode = (int)HttpStatusCode.SwitchingProtocols; // HTTP 101
            response.StatusDescription = HttpStatusDescription.Get(HttpStatusCode.SwitchingProtocols)!;

            HttpResponseStream responseStream = (response.OutputStream as HttpResponseStream)!;

            // Send websocket handshake headers
            await responseStream.WriteWebSocketHandshakeHeadersAsync().ConfigureAwait(false);

            WebSocket rawWebSocket = WebSocket.CreateFromStream(context.Connection.ConnectedStream, isServer: true, subProtocol, keepAliveInterval);

            // Wrap the raw websocket so its Dispose() automatically closes the connection.
            // This is important as common and recommended usage is to just Dispose the HttpListenerWebSocketContext.WebSocket
            // and without it connection would not be cleaned up causing memory leaks and other issues.
            HttpListenerWrappedWebSocket webSocket = new(rawWebSocket, context);

            HttpListenerWebSocketContext webSocketContext = new HttpListenerWebSocketContext(
                                                                request.Url!,
                                                                request.Headers,
                                                                request.Cookies,
                                                                context.User!,
                                                                request.IsAuthenticated,
                                                                request.IsLocal,
                                                                request.IsSecureConnection,
                                                                origin!,
                                                                secWebSocketProtocols ?? Array.Empty<string>(),
                                                                secWebSocketVersion!,
                                                                secWebSocketKey!,
                                                                webSocket);

            return webSocketContext;
        }

        private const bool WebSocketsSupported = true;

        /// <summary>
        /// Wraps the real WebSocket so that when Dispose() is called,
        /// we also call context.Response.Close(), triggering UnregisterContext.
        /// </summary>
        private sealed class HttpListenerWrappedWebSocket : WebSocket
        {
            private readonly WebSocket _inner;
            private readonly HttpListenerContext _context;
            private bool _disposed;

            internal HttpListenerWrappedWebSocket(WebSocket inner, HttpListenerContext context)
            {
                _inner = inner;
                _context = context;
            }

            public override WebSocketCloseStatus? CloseStatus => _inner.CloseStatus;
            public override string? CloseStatusDescription => _inner.CloseStatusDescription;
            public override WebSocketState State => _inner.State;
            public override string? SubProtocol => _inner.SubProtocol;

            public override void Abort() => _inner.Abort();

            public override Task CloseAsync(
                WebSocketCloseStatus closeStatus,
                string? statusDescription,
                CancellationToken cancellationToken) =>
                _inner.CloseAsync(closeStatus, statusDescription, cancellationToken);

            public override Task CloseOutputAsync(
                WebSocketCloseStatus closeStatus,
                string? statusDescription,
                CancellationToken cancellationToken) =>
                _inner.CloseOutputAsync(closeStatus, statusDescription, cancellationToken);

            public override void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                if (_disposed)
                    return;
                _disposed = true;

                if (disposing)
                {
                    // Dispose the underlying raw WebSocket
                    _inner.Dispose();
                    // Ensure we remove the context from the HttpListener's tracking dictionary
                    // by forcibly calling Response.Close(). This closes the underlying connection
                    // and also calls UnregisterContext(context), preventing stale HttpListenerContext objects causing memory leak.
                    _context.Response.Close();
                }
                else
                {
                    // Technically we shouldn't be doing the following work when disposing == false,
                    // as the following work relies on other finalizable objects.
                    // But given we have little choice: if someone drops the websocket without
                    // disposing of it we need to close the context to prevent memory leaks.
                    try
                    {
                        _context.Response.Close();
                    }
                    catch
                    {
                        // We are doing best effort here to handle the case where the user does not properly Dispose the WebSocket.
                        // If we fail to close the reponse, we are not going to throw an exception and possibly crash.
                        // We are just going to ignore it and let the GC do its best.
                    }
                }
            }

            ~HttpListenerWrappedWebSocket()
            {
                Dispose(false);
            }

            public override Task<WebSocketReceiveResult> ReceiveAsync(
                ArraySegment<byte> buffer,
                CancellationToken cancellationToken) =>
                _inner.ReceiveAsync(buffer, cancellationToken);

            public override Task SendAsync(
                ArraySegment<byte> buffer,
                WebSocketMessageType messageType,
                bool endOfMessage,
                CancellationToken cancellationToken) =>
                _inner.SendAsync(buffer, messageType, endOfMessage, cancellationToken);
        }
    }
}
