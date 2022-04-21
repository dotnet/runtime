// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    internal static partial class HttpWebSocket
    {
        internal static Task<HttpListenerWebSocketContext> AcceptWebSocketAsync(HttpListenerContext context,
            string? subProtocol,
            int receiveBufferSize,
            TimeSpan keepAliveInterval,
            ArraySegment<byte> internalBuffer)
        {
            ValidateOptions(subProtocol, receiveBufferSize, MinSendBufferSize, keepAliveInterval);
            WebSocketValidate.ValidateArraySegment(internalBuffer, nameof(internalBuffer));
            WebSocketBuffer.Validate(internalBuffer.Count, receiveBufferSize, MinSendBufferSize, true);

            return AcceptWebSocketAsyncCore(context, subProtocol, receiveBufferSize, keepAliveInterval, internalBuffer);
        }

        private static async Task<HttpListenerWebSocketContext> AcceptWebSocketAsyncCore(HttpListenerContext context,
            string? subProtocol,
            int receiveBufferSize,
            TimeSpan keepAliveInterval,
            ArraySegment<byte> internalBuffer)
        {
            HttpListenerWebSocketContext? webSocketContext = null;
            try
            {
                // get property will create a new response if one doesn't exist.
                HttpListenerResponse response = context.Response;
                HttpListenerRequest request = context.Request;
                ValidateWebSocketHeaders(context);

                string? secWebSocketVersion = request.Headers[HttpKnownHeaderNames.SecWebSocketVersion];

                // Optional for non-browser client
                string? origin = request.Headers[HttpKnownHeaderNames.Origin];

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
                string? secWebSocketKey = request.Headers[HttpKnownHeaderNames.SecWebSocketKey];
                string secWebSocketAccept = HttpWebSocket.GetSecWebSocketAcceptString(secWebSocketKey);

                response.Headers.Add(HttpKnownHeaderNames.Connection, HttpKnownHeaderNames.Upgrade);
                response.Headers.Add(HttpKnownHeaderNames.Upgrade, WebSocketUpgradeToken);
                response.Headers.Add(HttpKnownHeaderNames.SecWebSocketAccept, secWebSocketAccept);

                response.StatusCode = (int)HttpStatusCode.SwitchingProtocols; // HTTP 101
                response.ComputeCoreHeaders();
                ulong hresult = SendWebSocketHeaders(response);
                if (hresult != 0)
                {
                    throw new WebSocketException((int)hresult,
                        SR.Format(SR.net_WebSockets_NativeSendResponseHeaders,
                        nameof(AcceptWebSocketAsync),
                        hresult));
                }

                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Info(null, $"{HttpKnownHeaderNames.Origin} = {origin}");
                    NetEventSource.Info(null, $"{HttpKnownHeaderNames.SecWebSocketVersion} = {secWebSocketVersion}");
                    NetEventSource.Info(null, $"{HttpKnownHeaderNames.SecWebSocketKey} = {secWebSocketKey}");
                    NetEventSource.Info(null, $"{HttpKnownHeaderNames.SecWebSocketAccept} = {secWebSocketAccept}");
                    NetEventSource.Info(null, $"{HttpKnownHeaderNames.SecWebSocketProtocol} = {request.Headers[HttpKnownHeaderNames.SecWebSocketProtocol]}");
                    NetEventSource.Info(null, $"{HttpKnownHeaderNames.SecWebSocketProtocol} = {outgoingSecWebSocketProtocolString}");
                }

                await response.OutputStream.FlushAsync().SuppressContextFlow();

                HttpResponseStream responseStream = (response.OutputStream as HttpResponseStream)!;
                Debug.Assert(responseStream != null, "'responseStream' MUST be castable to System.Net.HttpResponseStream.");
                ((HttpResponseStream)response.OutputStream).SwitchToOpaqueMode();
                HttpRequestStream requestStream = new HttpRequestStream(context);
                requestStream.SwitchToOpaqueMode();
                WebSocketHttpListenerDuplexStream webSocketStream =
                    new WebSocketHttpListenerDuplexStream(requestStream, responseStream, context);
                WebSocket webSocket = ServerWebSocket.Create(webSocketStream,
                    subProtocol,
                    receiveBufferSize,
                    keepAliveInterval,
                    internalBuffer);

                webSocketContext = new HttpListenerWebSocketContext(
                                                                    request.Url!,
                                                                    request.Headers,
                                                                    request.Cookies,
                                                                    context.User!,
                                                                    request.IsAuthenticated,
                                                                    request.IsLocal,
                                                                    request.IsSecureConnection,
                                                                    origin!,
                                                                    secWebSocketProtocols.AsReadOnly(),
                                                                    secWebSocketVersion!,
                                                                    secWebSocketKey!,
                                                                    webSocket);

                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Associate(context, webSocketContext);
                    NetEventSource.Associate(webSocketContext, webSocket);
                }
            }
            catch (Exception ex)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Error(context, ex);
                }
                throw;
            }

            return webSocketContext;
        }

        internal static ConfiguredTaskAwaitable SuppressContextFlow(this Task task)
        {
            // We don't flow the synchronization context within WebSocket.xxxAsync - but the calling application
            // can decide whether the completion callback for the task returned from WebSocket.xxxAsync runs
            // under the caller's synchronization context.
            return task.ConfigureAwait(false);
        }

        internal static ConfiguredTaskAwaitable<T> SuppressContextFlow<T>(this Task<T> task)
        {
            // We don't flow the synchronization context within WebSocket.xxxAsync - but the calling application
            // can decide whether the completion callback for the task returned from WebSocket.xxxAsync runs
            // under the caller's synchronization context.
            return task.ConfigureAwait(false);
        }

        private static unsafe ulong SendWebSocketHeaders(HttpListenerResponse response)
        {
            return response.SendHeaders(null, null,
                Interop.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_OPAQUE |
                Interop.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_MORE_DATA |
                Interop.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_BUFFER_DATA,
                true);
        }

        internal static void ValidateInnerStream(Stream innerStream)
        {
            ArgumentNullException.ThrowIfNull(innerStream);

            if (!innerStream.CanRead)
            {
                throw new ArgumentException(SR.net_writeonlystream, nameof(innerStream));
            }

            if (!innerStream.CanWrite)
            {
                throw new ArgumentException(SR.net_readonlystream, nameof(innerStream));
            }
        }

        internal static void ThrowIfConnectionAborted(Stream connection, bool read)
        {
            if ((!read && !connection.CanWrite) ||
                (read && !connection.CanRead))
            {
                throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely);
            }
        }

        [DoesNotReturn]
        internal static void ThrowPlatformNotSupportedException_WSPC()
        {
            throw new PlatformNotSupportedException(SR.net_WebSockets_UnsupportedPlatform);
        }

        private static string SupportedVersion => WebSocketProtocolComponent.SupportedVersion;

        private static bool WebSocketsSupported { get; } = Environment.OSVersion.Version >= new Version(6, 2);
    }
}
