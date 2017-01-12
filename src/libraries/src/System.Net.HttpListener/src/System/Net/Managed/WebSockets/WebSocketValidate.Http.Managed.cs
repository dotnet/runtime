// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    internal static partial class WebSocketValidate
    {
        internal static async Task<HttpListenerWebSocketContext> AcceptWebSocketAsyncCore(HttpListenerContext context,
            string subProtocol,
            int receiveBufferSize,
            TimeSpan keepAliveInterval,
            ArraySegment<byte>? internalBuffer = null)
        {
            // get property will create a new response if one doesn't exist.
            HttpListenerResponse response = context.Response;
            HttpListenerRequest request = context.Request;
            ValidateWebSocketHeaders(context);

            string secWebSocketVersion = request.Headers[HttpKnownHeaderNames.SecWebSocketVersion];

            // Optional for non-browser client
            string origin = request.Headers[HttpKnownHeaderNames.Origin];

            List<string> secWebSocketProtocols = new List<string>();
            string outgoingSecWebSocketProtocolString;
            bool shouldSendSecWebSocketProtocolHeader =
                ProcessWebSocketProtocolHeader(
                    request.Headers[HttpKnownHeaderNames.SecWebSocketProtocol],
                    subProtocol,
                    out outgoingSecWebSocketProtocolString);

            if (shouldSendSecWebSocketProtocolHeader)
            {
                secWebSocketProtocols.Add(outgoingSecWebSocketProtocolString);
                response.Headers.Add(HttpKnownHeaderNames.SecWebSocketProtocol,
                    outgoingSecWebSocketProtocolString);
            }

            // negotiate the websocket key return value
            string secWebSocketKey = request.Headers[HttpKnownHeaderNames.SecWebSocketKey];
            string secWebSocketAccept = WebSocketValidate.GetSecWebSocketAcceptString(secWebSocketKey);

            response.Headers.Add(HttpKnownHeaderNames.Connection, HttpKnownHeaderNames.Upgrade);
            response.Headers.Add(HttpKnownHeaderNames.Upgrade, WebSocketUpgradeToken);
            response.Headers.Add(HttpKnownHeaderNames.SecWebSocketAccept, secWebSocketAccept);

            response.StatusCode = (int)HttpStatusCode.SwitchingProtocols; // HTTP 101
            response.StatusDescription = HttpStatusDescription.Get(HttpStatusCode.SwitchingProtocols);

            HttpResponseStream responseStream = response.OutputStream as HttpResponseStream;
            
            // Send websocket handshake headers
            await responseStream.WriteWebSocketHandshakeHeadersAsync();

            WebSocket webSocket = ManagedWebSocket.CreateFromConnectedStream(context.Connection.ConnectedStream, true, subProtocol, keepAliveInterval, receiveBufferSize, internalBuffer);

            HttpListenerWebSocketContext webSocketContext = new HttpListenerWebSocketContext(
                                                                request.Url,
                                                                request.Headers,
                                                                request.Cookies,
                                                                context.User,
                                                                request.IsAuthenticated,
                                                                request.IsLocal,
                                                                request.IsSecureConnection,
                                                                origin,
                                                                secWebSocketProtocols.AsReadOnly(),
                                                                secWebSocketVersion,
                                                                secWebSocketKey,
                                                                webSocket);

            return webSocketContext;
        }

        private static void ValidateWebSocketHeaders(HttpListenerContext context)
        {
            if (!context.Request.IsWebSocketRequest)
            {
                throw new WebSocketException(WebSocketError.NotAWebSocket,
                    SR.Format(SR.net_WebSockets_AcceptNotAWebSocket,
                    nameof(ValidateWebSocketHeaders),
                    HttpKnownHeaderNames.Connection,
                    HttpKnownHeaderNames.Upgrade,
                    WebSocketValidate.WebSocketUpgradeToken,
                    context.Request.Headers[HttpKnownHeaderNames.Upgrade]));
            }

            string secWebSocketVersion = context.Request.Headers[HttpKnownHeaderNames.SecWebSocketVersion];
            if (string.IsNullOrEmpty(secWebSocketVersion))
            {
                throw new WebSocketException(WebSocketError.HeaderError,
                    SR.Format(SR.net_WebSockets_AcceptHeaderNotFound,
                    nameof(ValidateWebSocketHeaders),
                    HttpKnownHeaderNames.SecWebSocketVersion));
            }

            // TODO: Figure out how to validate version.

            if (string.IsNullOrWhiteSpace(context.Request.Headers[HttpKnownHeaderNames.SecWebSocketKey]))
            {
                throw new WebSocketException(WebSocketError.HeaderError,
                    SR.Format(SR.net_WebSockets_AcceptHeaderNotFound,
                    nameof(ValidateWebSocketHeaders),
                    HttpKnownHeaderNames.SecWebSocketKey));
            }
        }
    }
}
