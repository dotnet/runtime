// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    internal static partial class HttpWebSocket
    {
        private const string SupportedVersion = "13";

        internal static async Task<HttpListenerWebSocketContext> AcceptWebSocketAsyncCore(HttpListenerContext context,
            string subProtocol,
            int receiveBufferSize,
            TimeSpan keepAliveInterval,
            ArraySegment<byte>? internalBuffer = null)
        {
            ValidateOptions(subProtocol, receiveBufferSize, MinSendBufferSize, keepAliveInterval);

            // get property will create a new response if one doesn't exist.
            HttpListenerResponse response = context.Response;
            HttpListenerRequest request = context.Request;
            ValidateWebSocketHeaders(context);

            string secWebSocketVersion = request.Headers[HttpKnownHeaderNames.SecWebSocketVersion];

            // Optional for non-browser client
            string origin = request.Headers[HttpKnownHeaderNames.Origin];

            string[] secWebSocketProtocols = null;
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
            string secWebSocketKey = request.Headers[HttpKnownHeaderNames.SecWebSocketKey];
            string secWebSocketAccept = HttpWebSocket.GetSecWebSocketAcceptString(secWebSocketKey);

            response.Headers.Add(HttpKnownHeaderNames.Connection, HttpKnownHeaderNames.Upgrade);
            response.Headers.Add(HttpKnownHeaderNames.Upgrade, WebSocketUpgradeToken);
            response.Headers.Add(HttpKnownHeaderNames.SecWebSocketAccept, secWebSocketAccept);

            response.StatusCode = (int)HttpStatusCode.SwitchingProtocols; // HTTP 101
            response.StatusDescription = HttpStatusDescription.Get(HttpStatusCode.SwitchingProtocols);

            HttpResponseStream responseStream = response.OutputStream as HttpResponseStream;
            
            // Send websocket handshake headers
            await responseStream.WriteWebSocketHandshakeHeadersAsync().ConfigureAwait(false);

            const int MinBufferSize = 14; // from ManagedWebSocket.MaxMessageHeaderLength
            Memory<byte> buffer =
                internalBuffer.GetValueOrDefault().Count >= MinBufferSize ? internalBuffer.GetValueOrDefault() : // use provided buffer if it's big enough
                receiveBufferSize >= MinBufferSize ? new byte[receiveBufferSize] : // or use provided size if it's big enough
                Memory<byte>.Empty; // or use the default
            WebSocket webSocket = WebSocket.CreateFromStream(context.Connection.ConnectedStream, isServer:true, subProtocol, keepAliveInterval, buffer);

            HttpListenerWebSocketContext webSocketContext = new HttpListenerWebSocketContext(
                                                                request.Url,
                                                                request.Headers,
                                                                request.Cookies,
                                                                context.User,
                                                                request.IsAuthenticated,
                                                                request.IsLocal,
                                                                request.IsSecureConnection,
                                                                origin,
                                                                secWebSocketProtocols != null ? secWebSocketProtocols : Array.Empty<string>(),
                                                                secWebSocketVersion,
                                                                secWebSocketKey,
                                                                webSocket);

            return webSocketContext;
        }

        private const bool WebSocketsSupported = true;
    }
}

