// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    internal static partial class WebSocketValidate
    {
        internal const string SecWebSocketKeyGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        internal const string WebSocketUpgradeToken = "websocket";
        internal const int DefaultReceiveBufferSize = 16 * 1024;
        internal const int DefaultClientSendBufferSize = 16 * 1024;

        // RFC 6455 requests WebSocket clients to let the server initiate the TCP close to avoid that client sockets 
        // end up in TIME_WAIT-state
        //
        // After both sending and receiving a Close message, an endpoint considers the WebSocket connection closed and 
        // MUST close the underlying TCP connection.  The server MUST close the underlying TCP connection immediately; 
        // the client SHOULD wait for the server to close the connection but MAY close the connection at any time after
        // sending and receiving a Close message, e.g., if it has not received a TCP Close from the server in a 
        // reasonable time period.
        internal const int ClientTcpCloseTimeout = 1000; // 1s

        private static readonly ArraySegment<byte> s_EmptyPayload = new ArraySegment<byte>(new byte[] { }, 0, 0);
        private static readonly Random s_keyGenerator = new Random();
        private static readonly bool s_httpSysSupportsWebSockets = (Environment.OSVersion.Version >= new Version(6, 2));

        internal static ArraySegment<byte> EmptyPayload
        {
            get { return s_EmptyPayload; }
        }

        internal static Task<HttpListenerWebSocketContext> AcceptWebSocketAsync(HttpListenerContext context,
            string subProtocol,
            int receiveBufferSize,
            TimeSpan keepAliveInterval,
            ArraySegment<byte> internalBuffer)
        {
            ValidateOptions(subProtocol, receiveBufferSize, WebSocketBuffer.MinSendBufferSize, keepAliveInterval);
            ValidateArraySegment<byte>(internalBuffer, nameof(internalBuffer));
            WebSocketBuffer.Validate(internalBuffer.Count, receiveBufferSize, WebSocketBuffer.MinSendBufferSize, true);

            return AcceptWebSocketAsyncCore(context, subProtocol, receiveBufferSize, keepAliveInterval, internalBuffer);
        }

        private static async Task<HttpListenerWebSocketContext> AcceptWebSocketAsyncCore(HttpListenerContext context,
            string subProtocol,
            int receiveBufferSize,
            TimeSpan keepAliveInterval,
            ArraySegment<byte> internalBuffer)
        {
            HttpListenerWebSocketContext webSocketContext = null;
            if (NetEventSource.IsEnabled)
            {
                NetEventSource.Enter(null, context);
            }

            try
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
                response.ComputeCoreHeaders();
                ulong hresult = SendWebSocketHeaders(response);
                if (hresult != 0)
                {
                    throw new WebSocketException((int)hresult,
                        SR.Format(SR.net_WebSockets_NativeSendResponseHeaders,
                        nameof(AcceptWebSocketAsync),
                        hresult));
                }

                if (NetEventSource.IsEnabled)
                {
                    NetEventSource.Info(null, $"{HttpKnownHeaderNames.Origin} = {origin}");
                    NetEventSource.Info(null, $"{HttpKnownHeaderNames.SecWebSocketVersion} = {secWebSocketVersion}");
                    NetEventSource.Info(null, $"{HttpKnownHeaderNames.SecWebSocketKey} = {secWebSocketKey}");
                    NetEventSource.Info(null, $"{HttpKnownHeaderNames.SecWebSocketAccept} = {secWebSocketAccept}");
                    NetEventSource.Info(null, $"{HttpKnownHeaderNames.SecWebSocketProtocol} = {request.Headers[HttpKnownHeaderNames.SecWebSocketProtocol]}");
                    NetEventSource.Info(null, $"{HttpKnownHeaderNames.SecWebSocketProtocol} = {outgoingSecWebSocketProtocolString}");
                }

                await response.OutputStream.FlushAsync().SuppressContextFlow();

                HttpResponseStream responseStream = response.OutputStream as HttpResponseStream;
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

                if (NetEventSource.IsEnabled)
                {
                    NetEventSource.Associate(context, webSocketContext);
                    NetEventSource.Associate(webSocketContext, webSocket);
                }
            }
            catch (Exception ex)
            {
                if (NetEventSource.IsEnabled)
                {
                    NetEventSource.Error(context, ex);
                }
                throw;
            }
            finally
            {
                if (NetEventSource.IsEnabled)
                {
                    NetEventSource.Exit(context);
                }
            }

            return webSocketContext;
        }

        [SuppressMessage("Microsoft.Security", "CA5350", Justification = "SHA1 used only for hashing purposes, not for crypto.")]
        internal static string GetSecWebSocketAcceptString(string secWebSocketKey)
        {
            string retVal;

            // SHA1 used only for hashing purposes, not for crypto. Check here for FIPS compat.
            using (SHA1 sha1 = SHA1.Create())
            {
                string acceptString = string.Concat(secWebSocketKey, WebSocketValidate.SecWebSocketKeyGuid);
                byte[] toHash = Encoding.UTF8.GetBytes(acceptString);
                retVal = Convert.ToBase64String(sha1.ComputeHash(toHash));
            }

            return retVal;
        }

        internal static string GetTraceMsgForParameters(int offset, int count, CancellationToken cancellationToken)
        {
            return string.Format(CultureInfo.InvariantCulture,
                "offset: {0}, count: {1}, cancellationToken.CanBeCanceled: {2}",
                offset,
                count,
                cancellationToken.CanBeCanceled);
        }

        // return value here signifies if a Sec-WebSocket-Protocol header should be returned by the server. 
        internal static bool ProcessWebSocketProtocolHeader(string clientSecWebSocketProtocol,
            string subProtocol,
            out string acceptProtocol)
        {
            acceptProtocol = string.Empty;
            if (string.IsNullOrEmpty(clientSecWebSocketProtocol))
            {
                // client hasn't specified any Sec-WebSocket-Protocol header
                if (subProtocol != null)
                {
                    // If the server specified _anything_ this isn't valid.
                    throw new WebSocketException(WebSocketError.UnsupportedProtocol,
                        SR.Format(SR.net_WebSockets_ClientAcceptingNoProtocols, subProtocol));
                }
                // Treat empty and null from the server as the same thing here, server should not send headers. 
                return false;
            }

            // here, we know the client specified something and it's non-empty.

            if (subProtocol == null)
            {
                // client specified some protocols, server specified 'null'. So server should send headers.                 
                return true;
            }

            // here, we know that the client has specified something, it's not empty
            // and the server has specified exactly one protocol

            string[] requestProtocols = clientSecWebSocketProtocol.Split(new char[] { ',' },
                StringSplitOptions.RemoveEmptyEntries);
            acceptProtocol = subProtocol;

            // client specified protocols, serverOptions has exactly 1 non-empty entry. Check that 
            // this exists in the list the client specified. 
            for (int i = 0; i < requestProtocols.Length; i++)
            {
                string currentRequestProtocol = requestProtocols[i].Trim();
                if (string.Compare(acceptProtocol, currentRequestProtocol, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return true;
                }
            }

            throw new WebSocketException(WebSocketError.UnsupportedProtocol,
                SR.Format(SR.net_WebSockets_AcceptUnsupportedProtocol,
                    clientSecWebSocketProtocol,
                    subProtocol));
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

        internal static void ValidateBuffer(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0 || offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 0 || count > (buffer.Length - offset))
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
        }

        private static unsafe ulong SendWebSocketHeaders(HttpListenerResponse response)
        {
            return response.SendHeaders(null, null,
                Interop.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_OPAQUE |
                Interop.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_MORE_DATA |
                Interop.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_BUFFER_DATA,
                true);
        }

        private static void ValidateWebSocketHeaders(HttpListenerContext context)
        {
            EnsureHttpSysSupportsWebSockets();

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

            if (string.Compare(secWebSocketVersion, WebSocketProtocolComponent.SupportedVersion, StringComparison.OrdinalIgnoreCase) != 0)
            {
                throw new WebSocketException(WebSocketError.UnsupportedVersion,
                    SR.Format(SR.net_WebSockets_AcceptUnsupportedWebSocketVersion,
                    nameof(ValidateWebSocketHeaders),
                    secWebSocketVersion,
                    WebSocketProtocolComponent.SupportedVersion));
            }

            if (string.IsNullOrWhiteSpace(context.Request.Headers[HttpKnownHeaderNames.SecWebSocketKey]))
            {
                throw new WebSocketException(WebSocketError.HeaderError,
                    SR.Format(SR.net_WebSockets_AcceptHeaderNotFound,
                    nameof(ValidateWebSocketHeaders),
                    HttpKnownHeaderNames.SecWebSocketKey));
            }
        }
        
        internal static void ValidateOptions(string subProtocol,
            int receiveBufferSize,
            int sendBufferSize,
            TimeSpan keepAliveInterval)
        {
            // We allow the subProtocol to be null. Validate if it is not null.
            if (subProtocol != null)
            {
                ValidateSubprotocol(subProtocol);
            }

            ValidateBufferSizes(receiveBufferSize, sendBufferSize);

            if (keepAliveInterval < Timeout.InfiniteTimeSpan) // -1
            {
                throw new ArgumentOutOfRangeException(nameof(keepAliveInterval), keepAliveInterval,
                    SR.Format(SR.net_WebSockets_ArgumentOutOfRange_TooSmall, Timeout.InfiniteTimeSpan.ToString()));
            }
        }

        internal static void ValidateBufferSizes(int receiveBufferSize, int sendBufferSize)
        {
            if (receiveBufferSize < WebSocketBuffer.MinReceiveBufferSize)
            {
                throw new ArgumentOutOfRangeException(nameof(receiveBufferSize), receiveBufferSize,
                    SR.Format(SR.net_WebSockets_ArgumentOutOfRange_TooSmall, WebSocketBuffer.MinReceiveBufferSize));
            }

            if (sendBufferSize < WebSocketBuffer.MinSendBufferSize)
            {
                throw new ArgumentOutOfRangeException(nameof(sendBufferSize), sendBufferSize,
                    SR.Format(SR.net_WebSockets_ArgumentOutOfRange_TooSmall, WebSocketBuffer.MinSendBufferSize));
            }

            if (receiveBufferSize > WebSocketBuffer.MaxBufferSize)
            {
                throw new ArgumentOutOfRangeException(nameof(receiveBufferSize), receiveBufferSize,
                    SR.Format(SR.net_WebSockets_ArgumentOutOfRange_TooBig,
                        nameof(receiveBufferSize),
                        receiveBufferSize,
                        WebSocketBuffer.MaxBufferSize));
            }

            if (sendBufferSize > WebSocketBuffer.MaxBufferSize)
            {
                throw new ArgumentOutOfRangeException(nameof(sendBufferSize), sendBufferSize,
                    SR.Format(SR.net_WebSockets_ArgumentOutOfRange_TooBig,
                        nameof(sendBufferSize),
                        sendBufferSize,
                        WebSocketBuffer.MaxBufferSize));
            }
        }

        internal static void ValidateInnerStream(Stream innerStream)
        {
            if (innerStream == null)
            {
                throw new ArgumentNullException(nameof(innerStream));
            }

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

        internal static void ThrowPlatformNotSupportedException_WSPC()
        {
            throw new PlatformNotSupportedException(SR.net_WebSockets_UnsupportedPlatform);
        }

        private static void ThrowPlatformNotSupportedException_HTTPSYS()
        {
            throw new PlatformNotSupportedException(SR.net_WebSockets_UnsupportedPlatform);
        }

        internal static void ValidateArraySegment<T>(ArraySegment<T> arraySegment, string parameterName)
        {
            Debug.Assert(!string.IsNullOrEmpty(parameterName), "'parameterName' MUST NOT be NULL or string.Empty");

            if (arraySegment.Array == null)
            {
                throw new ArgumentNullException(parameterName + "." + nameof(arraySegment.Array));
            }

            if (arraySegment.Offset < 0 || arraySegment.Offset > arraySegment.Array.Length)
            {
                throw new ArgumentOutOfRangeException(parameterName + "." + nameof(arraySegment.Offset));
            }
            if (arraySegment.Count < 0 || arraySegment.Count > (arraySegment.Array.Length - arraySegment.Offset))
            {
                throw new ArgumentOutOfRangeException(parameterName + "." + nameof(arraySegment.Count));
            }
        }

        private static void EnsureHttpSysSupportsWebSockets()
        {
            if (!s_httpSysSupportsWebSockets)
            {
                ThrowPlatformNotSupportedException_HTTPSYS();
            }
        }
    }
}
