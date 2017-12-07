// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    internal sealed class WebSocketHandle
    {
        /// <summary>GUID appended by the server as part of the security key response.  Defined in the RFC.</summary>
        private const string WSServerGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        private readonly CancellationTokenSource _abortSource = new CancellationTokenSource();
        private WebSocketState _state = WebSocketState.Connecting;
        private WebSocket _webSocket;

        public static WebSocketHandle Create() => new WebSocketHandle();

        public static bool IsValid(WebSocketHandle handle) => handle != null;

        public WebSocketCloseStatus? CloseStatus => _webSocket?.CloseStatus;

        public string CloseStatusDescription => _webSocket?.CloseStatusDescription;

        public WebSocketState State => _webSocket?.State ?? _state;

        public string SubProtocol => _webSocket?.SubProtocol;

        public static void CheckPlatformSupport() { /* nop */ }

        public void Dispose()
        {
            _state = WebSocketState.Closed;
            _webSocket?.Dispose();
        }

        public void Abort()
        {
            _abortSource.Cancel();
            _webSocket?.Abort();
        }

        public Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken) =>
            _webSocket.SendAsync(buffer, messageType, endOfMessage, cancellationToken);

        public Task SendAsync(ReadOnlyMemory<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken) =>
            _webSocket.SendAsync(buffer, messageType, endOfMessage, cancellationToken);

        public Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken) =>
            _webSocket.ReceiveAsync(buffer, cancellationToken);

        public ValueTask<ValueWebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken) =>
            _webSocket.ReceiveAsync(buffer, cancellationToken);

        public Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken) =>
            _webSocket.CloseAsync(closeStatus, statusDescription, cancellationToken);

        public Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken) =>
            _webSocket.CloseOutputAsync(closeStatus, statusDescription, cancellationToken);

        private sealed class DirectManagedHttpClientHandler : HttpClientHandler
        {
            private const string ManagedHandlerEnvVar = "COMPlus_UseManagedHttpClientHandler";
            private static readonly LocalDataStoreSlot s_managedHandlerSlot = GetSlot();
            private static readonly object s_true = true;

            private static LocalDataStoreSlot GetSlot()
            {
                LocalDataStoreSlot slot = Thread.GetNamedDataSlot(ManagedHandlerEnvVar);
                if (slot != null)
                {
                    return slot;
                }

                try
                {
                    return Thread.AllocateNamedDataSlot(ManagedHandlerEnvVar);
                }
                catch (ArgumentException) // in case of a race condition where multiple threads all try to allocate the slot concurrently
                {
                    return Thread.GetNamedDataSlot(ManagedHandlerEnvVar);
                }
            }

            public static DirectManagedHttpClientHandler CreateHandler()
            {
                Thread.SetData(s_managedHandlerSlot, s_true);
                try
                {
                    return new DirectManagedHttpClientHandler();
                }
                finally { Thread.SetData(s_managedHandlerSlot, null); }
            }

            public new Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken) =>
                base.SendAsync(request, cancellationToken);
        }

        public async Task ConnectAsyncCore(Uri uri, CancellationToken cancellationToken, ClientWebSocketOptions options)
        {
            HttpResponseMessage response = null;
            try
            {
                // Create the request message, including a uri with ws{s} switched to http{s}.
                uri = new UriBuilder(uri) { Scheme = (uri.Scheme == UriScheme.Ws) ? UriScheme.Http : UriScheme.Https }.Uri;
                var request = new HttpRequestMessage(HttpMethod.Get, uri);
                if (options._requestHeaders?.Count > 0) // use field to avoid lazily initializing the collection
                {
                    foreach (string key in options.RequestHeaders)
                    {
                        request.Headers.Add(key, options.RequestHeaders[key]);
                    }
                }

                // Create the security key and expected response, then build all of the request headers
                KeyValuePair<string, string> secKeyAndSecWebSocketAccept = CreateSecKeyAndSecWebSocketAccept();
                AddWebSocketHeaders(request, secKeyAndSecWebSocketAccept.Key, options);

                // Create the handler for this request and populate it with all of the options.
                DirectManagedHttpClientHandler handler = DirectManagedHttpClientHandler.CreateHandler();
                handler.UseDefaultCredentials = options.UseDefaultCredentials;
                handler.Credentials = options.Credentials;
                handler.Proxy = options.Proxy;
                handler.CookieContainer = options.Cookies;
                if (options._clientCertificates?.Count > 0) // use field to avoid lazily initializing the collection
                {
                    handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                    handler.ClientCertificates.AddRange(options.ClientCertificates);
                }

                // Issue the request.  The response must be status code 101.
                CancellationTokenSource linkedCancellation, externalAndAbortCancellation;
                if (cancellationToken.CanBeCanceled) // avoid allocating linked source if external token is not cancelable
                {
                    linkedCancellation =
                        externalAndAbortCancellation = 
                        CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _abortSource.Token);
                }
                else
                {
                    linkedCancellation = null;
                    externalAndAbortCancellation = _abortSource;
                }

                using (linkedCancellation)
                {
                    response = await handler.SendAsync(request, externalAndAbortCancellation.Token).ConfigureAwait(false);
                    externalAndAbortCancellation.Token.ThrowIfCancellationRequested(); // poll in case sends/receives in request/response didn't observe cancellation
                }

                if (response.StatusCode != HttpStatusCode.SwitchingProtocols)
                {
                    throw new WebSocketException(SR.net_webstatus_ConnectFailure);
                }

                // The Connection, Upgrade, and SecWebSocketAccept headers are required and with specific values.
                ValidateHeader(response.Headers, HttpKnownHeaderNames.Connection, "Upgrade");
                ValidateHeader(response.Headers, HttpKnownHeaderNames.Upgrade, "websocket");
                ValidateHeader(response.Headers, HttpKnownHeaderNames.SecWebSocketAccept, secKeyAndSecWebSocketAccept.Value);

                // The SecWebSocketProtocol header is optional.  We should only get it with a non-empty value if we requested subprotocols,
                // and then it must only be one of the ones we requested.  If we got a subprotocol other than one we requested (or if we
                // already got one in a previous header), fail. Otherwise, track which one we got.
                string subprotocol = null;
                IEnumerable<string> subprotocolEnumerableValues;
                if (response.Headers.TryGetValues(HttpKnownHeaderNames.SecWebSocketProtocol, out subprotocolEnumerableValues))
                {
                    Debug.Assert(subprotocolEnumerableValues is string[]);
                    string[] subprotocolArray = (string[])subprotocolEnumerableValues;
                    if (subprotocolArray.Length > 0 && !string.IsNullOrEmpty(subprotocolArray[0]))
                    {
                        subprotocol = options.RequestedSubProtocols.Find(requested => string.Equals(requested, subprotocolArray[0], StringComparison.OrdinalIgnoreCase));
                        if (subprotocol == null)
                        {
                            throw new WebSocketException(
                                WebSocketError.UnsupportedProtocol,
                                SR.Format(SR.net_WebSockets_AcceptUnsupportedProtocol, string.Join(", ", options.RequestedSubProtocols), string.Join(", ", subprotocolArray)));
                        }
                    }
                }

                // Get or create the buffer to use
                const int MinBufferSize = 14; // from ManagedWebSocket.MaxMessageHeaderLength
                ArraySegment<byte> optionsBuffer = options.Buffer.GetValueOrDefault();
                Memory<byte> buffer =
                    optionsBuffer.Count >= MinBufferSize ? optionsBuffer : // use the provided buffer if it's big enough
                    options.ReceiveBufferSize >= MinBufferSize ? new byte[options.ReceiveBufferSize] : // or use the requested size if it's big enough
                    Memory<byte>.Empty; // or let WebSocket.CreateFromStream use its default

                // Get the response stream and wrap it in a web socket.
                Stream connectedStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                Debug.Assert(connectedStream.CanWrite);
                Debug.Assert(connectedStream.CanRead);
                _webSocket = WebSocket.CreateFromStream(
                    connectedStream,
                    isServer: false,
                    subprotocol,
                    options.KeepAliveInterval,
                    buffer);
            }
            catch (Exception exc)
            {
                if (_state < WebSocketState.Closed)
                {
                    _state = WebSocketState.Closed;
                }

                Abort();
                response?.Dispose();

                if (exc is WebSocketException)
                {
                    throw;
                }
                throw new WebSocketException(SR.net_webstatus_ConnectFailure, exc);
            }
        }

        /// <param name="secKey">The generated security key to send in the Sec-WebSocket-Key header.</param>
        private static void AddWebSocketHeaders(HttpRequestMessage request, string secKey, ClientWebSocketOptions options)
        {
            request.Headers.TryAddWithoutValidation(HttpKnownHeaderNames.Connection, HttpKnownHeaderNames.Upgrade);
            request.Headers.TryAddWithoutValidation(HttpKnownHeaderNames.Upgrade, "websocket");
            request.Headers.TryAddWithoutValidation(HttpKnownHeaderNames.SecWebSocketVersion, "13");
            request.Headers.TryAddWithoutValidation(HttpKnownHeaderNames.SecWebSocketKey, secKey);
            if (options._requestedSubProtocols?.Count > 0)
            {
                request.Headers.Add(HttpKnownHeaderNames.SecWebSocketProtocol, string.Join(", ", options.RequestedSubProtocols));
            }
        }

        /// <summary>
        /// Creates a pair of a security key for sending in the Sec-WebSocket-Key header and
        /// the associated response we expect to receive as the Sec-WebSocket-Accept header value.
        /// </summary>
        /// <returns>A key-value pair of the request header security key and expected response header value.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "Required by RFC6455")]
        private static KeyValuePair<string, string> CreateSecKeyAndSecWebSocketAccept()
        {
            string secKey = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            using (SHA1 sha = SHA1.Create())
            {
                return new KeyValuePair<string, string>(
                    secKey,
                    Convert.ToBase64String(sha.ComputeHash(Encoding.ASCII.GetBytes(secKey + WSServerGuid))));
            }
        }

        private static void ValidateHeader(HttpHeaders headers, string name, string expectedValue)
        {
            if (!headers.TryGetValues(name, out IEnumerable<string> values))
            {
                ThrowConnectFailure();
            }

            Debug.Assert(values is string[]);
            string[] array = (string[])values;
            if (array.Length != 1 || !string.Equals(array[0], expectedValue, StringComparison.OrdinalIgnoreCase))
            {
                throw new WebSocketException(SR.Format(SR.net_WebSockets_InvalidResponseHeader, name, string.Join(", ", array)));
            }
        }

        private static void ThrowConnectFailure() => throw new WebSocketException(SR.net_webstatus_ConnectFailure);
    }
}
