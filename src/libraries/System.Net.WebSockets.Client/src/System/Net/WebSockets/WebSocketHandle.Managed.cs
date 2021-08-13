// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    internal sealed class WebSocketHandle
    {
        /// <summary>Shared, lazily-initialized handler for when using default options.</summary>
        private static SocketsHttpHandler? s_defaultHandler;

        private readonly CancellationTokenSource _abortSource = new CancellationTokenSource();
        private WebSocketState _state = WebSocketState.Connecting;
        private WebSocketDeflateOptions? _negotiatedDeflateOptions;

        public WebSocket? WebSocket { get; private set; }
        public WebSocketState State => WebSocket?.State ?? _state;

        public static ClientWebSocketOptions CreateDefaultOptions() => new ClientWebSocketOptions() { Proxy = DefaultWebProxy.Instance };

        public void Dispose()
        {
            _state = WebSocketState.Closed;
            WebSocket?.Dispose();
        }

        public void Abort()
        {
            _abortSource.Cancel();
            WebSocket?.Abort();
        }

        public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken, ClientWebSocketOptions options)
        {
            HttpResponseMessage? response = null;
            SocketsHttpHandler? handler = null;
            bool disposeHandler = true;
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, uri);
                if (options._requestHeaders?.Count > 0) // use field to avoid lazily initializing the collection
                {
                    foreach (string key in options.RequestHeaders)
                    {
                        request.Headers.TryAddWithoutValidation(key, options.RequestHeaders[key]);
                    }
                }

                // Create the security key and expected response, then build all of the request headers
                KeyValuePair<string, string> secKeyAndSecWebSocketAccept = CreateSecKeyAndSecWebSocketAccept();
                AddWebSocketHeaders(request, secKeyAndSecWebSocketAccept.Key, options);

                // Create the handler for this request and populate it with all of the options.
                // Try to use a shared handler rather than creating a new one just for this request, if
                // the options are compatible.
                if (options.Credentials == null &&
                    !options.UseDefaultCredentials &&
                    options.Proxy == null &&
                    options.Cookies == null &&
                    options.RemoteCertificateValidationCallback == null &&
                    options._clientCertificates?.Count == 0)
                {
                    disposeHandler = false;
                    handler = s_defaultHandler;
                    if (handler == null)
                    {
                        handler = new SocketsHttpHandler()
                        {
                            PooledConnectionLifetime = TimeSpan.Zero,
                            UseProxy = false,
                            UseCookies = false,
                        };
                        if (Interlocked.CompareExchange(ref s_defaultHandler, handler, null) != null)
                        {
                            handler.Dispose();
                            handler = s_defaultHandler;
                        }
                    }
                }
                else
                {
                    handler = new SocketsHttpHandler();
                    handler.PooledConnectionLifetime = TimeSpan.Zero;
                    handler.CookieContainer = options.Cookies;
                    handler.UseCookies = options.Cookies != null;
                    handler.SslOptions.RemoteCertificateValidationCallback = options.RemoteCertificateValidationCallback;

                    if (options.UseDefaultCredentials)
                    {
                        handler.Credentials = CredentialCache.DefaultCredentials;
                    }
                    else
                    {
                        handler.Credentials = options.Credentials;
                    }

                    if (options.Proxy == null)
                    {
                        handler.UseProxy = false;
                    }
                    else if (options.Proxy != DefaultWebProxy.Instance)
                    {
                        handler.Proxy = options.Proxy;
                    }

                    if (options._clientCertificates?.Count > 0) // use field to avoid lazily initializing the collection
                    {
                        Debug.Assert(handler.SslOptions.ClientCertificates == null);
                        handler.SslOptions.ClientCertificates = new X509Certificate2Collection();
                        handler.SslOptions.ClientCertificates.AddRange(options.ClientCertificates);
                    }
                }

                // Issue the request.  The response must be status code 101.
                CancellationTokenSource? linkedCancellation;
                CancellationTokenSource externalAndAbortCancellation;
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
                    response = await new HttpMessageInvoker(handler).SendAsync(request, externalAndAbortCancellation.Token).ConfigureAwait(false);
                    externalAndAbortCancellation.Token.ThrowIfCancellationRequested(); // poll in case sends/receives in request/response didn't observe cancellation
                }

                if (response.StatusCode != HttpStatusCode.SwitchingProtocols)
                {
                    throw new WebSocketException(WebSocketError.NotAWebSocket, SR.Format(SR.net_WebSockets_Connect101Expected, (int)response.StatusCode));
                }

                // The Connection, Upgrade, and SecWebSocketAccept headers are required and with specific values.
                ValidateHeader(response.Headers, HttpKnownHeaderNames.Connection, "Upgrade");
                ValidateHeader(response.Headers, HttpKnownHeaderNames.Upgrade, "websocket");
                ValidateHeader(response.Headers, HttpKnownHeaderNames.SecWebSocketAccept, secKeyAndSecWebSocketAccept.Value);

                // The SecWebSocketProtocol header is optional.  We should only get it with a non-empty value if we requested subprotocols,
                // and then it must only be one of the ones we requested.  If we got a subprotocol other than one we requested (or if we
                // already got one in a previous header), fail. Otherwise, track which one we got.
                string? subprotocol = null;
                if (response.Headers.TryGetValues(HttpKnownHeaderNames.SecWebSocketProtocol, out IEnumerable<string>? subprotocolEnumerableValues))
                {
                    Debug.Assert(subprotocolEnumerableValues is string[]);
                    string[] subprotocolArray = (string[])subprotocolEnumerableValues;
                    if (subprotocolArray.Length > 0 && !string.IsNullOrEmpty(subprotocolArray[0]))
                    {
                        if (options._requestedSubProtocols is not null)
                        {
                            foreach (string requestedProtocol in options._requestedSubProtocols)
                            {
                                if (requestedProtocol.Equals(subprotocolArray[0], StringComparison.OrdinalIgnoreCase))
                                {
                                    subprotocol = requestedProtocol;
                                    break;
                                }
                            }
                        }

                        if (subprotocol == null)
                        {
                            throw new WebSocketException(
                                WebSocketError.UnsupportedProtocol,
                                SR.Format(SR.net_WebSockets_AcceptUnsupportedProtocol, string.Join(", ", options.RequestedSubProtocols), string.Join(", ", subprotocolArray)));
                        }
                    }
                }

                // Because deflate options are negotiated we need a new object
                WebSocketDeflateOptions? negotiatedDeflateOptions = null;

                if (options.DangerousDeflateOptions is not null && response.Headers.TryGetValues(HttpKnownHeaderNames.SecWebSocketExtensions, out IEnumerable<string>? extensions))
                {
                    foreach (ReadOnlySpan<char> extension in extensions)
                    {
                        if (extension.TrimStart().StartsWith(ClientWebSocketDeflateConstants.Extension))
                        {
                            negotiatedDeflateOptions = ParseDeflateOptions(extension, options.DangerousDeflateOptions);
                            break;
                        }
                    }
                }

                if (response.Content is null)
                {
                    throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely);
                }

                // Get the response stream and wrap it in a web socket.
                Stream connectedStream = response.Content.ReadAsStream();
                Debug.Assert(connectedStream.CanWrite);
                Debug.Assert(connectedStream.CanRead);
                WebSocket = WebSocket.CreateFromStream(connectedStream, new WebSocketCreationOptions
                {
                    IsServer = false,
                    SubProtocol = subprotocol,
                    KeepAliveInterval = options.KeepAliveInterval,
                    DangerousDeflateOptions = negotiatedDeflateOptions
                });
                _negotiatedDeflateOptions = negotiatedDeflateOptions;
            }
            catch (Exception exc)
            {
                if (_state < WebSocketState.Closed)
                {
                    _state = WebSocketState.Closed;
                }

                Abort();
                response?.Dispose();

                if (exc is WebSocketException ||
                    (exc is OperationCanceledException && cancellationToken.IsCancellationRequested))
                {
                    throw;
                }

                throw new WebSocketException(WebSocketError.Faulted, SR.net_webstatus_ConnectFailure, exc);
            }
            finally
            {
                // Disposing the handler will not affect any active stream wrapped in the WebSocket.
                if (disposeHandler)
                {
                    handler?.Dispose();
                }
            }
        }

        private static WebSocketDeflateOptions ParseDeflateOptions(ReadOnlySpan<char> extension, WebSocketDeflateOptions original)
        {
            var options = new WebSocketDeflateOptions();

            while (true)
            {
                int end = extension.IndexOf(';');
                ReadOnlySpan<char> value = (end >= 0 ? extension[..end] : extension).Trim();

                if (value.Length > 0)
                {
                    if (value.SequenceEqual(ClientWebSocketDeflateConstants.ClientNoContextTakeover))
                    {
                        options.ClientContextTakeover = false;
                    }
                    else if (value.SequenceEqual(ClientWebSocketDeflateConstants.ServerNoContextTakeover))
                    {
                        options.ServerContextTakeover = false;
                    }
                    else if (value.StartsWith(ClientWebSocketDeflateConstants.ClientMaxWindowBits))
                    {
                        options.ClientMaxWindowBits = ParseWindowBits(value);
                    }
                    else if (value.StartsWith(ClientWebSocketDeflateConstants.ServerMaxWindowBits))
                    {
                        options.ServerMaxWindowBits = ParseWindowBits(value);
                    }

                    static int ParseWindowBits(ReadOnlySpan<char> value)
                    {
                        var startIndex = value.IndexOf('=');

                        if (startIndex < 0 ||
                            !int.TryParse(value.Slice(startIndex + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int windowBits) ||
                            windowBits < WebSocketValidate.MinDeflateWindowBits ||
                            windowBits > WebSocketValidate.MaxDeflateWindowBits)
                        {
                            throw new WebSocketException(WebSocketError.HeaderError,
                                SR.Format(SR.net_WebSockets_InvalidResponseHeader, ClientWebSocketDeflateConstants.Extension, value.ToString()));
                        }

                        return windowBits;
                    }
                }

                if (end < 0)
                {
                    break;
                }
                extension = extension[(end + 1)..];
            }

            if (options.ClientMaxWindowBits > original.ClientMaxWindowBits)
            {
                throw new WebSocketException(string.Format(SR.net_WebSockets_ClientWindowBitsNegotiationFailure,
                    original.ClientMaxWindowBits, options.ClientMaxWindowBits));
            }

            if (options.ServerMaxWindowBits > original.ServerMaxWindowBits)
            {
                throw new WebSocketException(string.Format(SR.net_WebSockets_ServerWindowBitsNegotiationFailure,
                    original.ServerMaxWindowBits, options.ServerMaxWindowBits));
            }

            return options;
        }

        /// <summary>Adds the necessary headers for the web socket request.</summary>
        /// <param name="request">The request to which the headers should be added.</param>
        /// <param name="secKey">The generated security key to send in the Sec-WebSocket-Key header.</param>
        /// <param name="options">The options controlling the request.</param>
        private static void AddWebSocketHeaders(HttpRequestMessage request, string secKey, ClientWebSocketOptions options)
        {
            request.Headers.TryAddWithoutValidation(HttpKnownHeaderNames.Connection, HttpKnownHeaderNames.Upgrade);
            request.Headers.TryAddWithoutValidation(HttpKnownHeaderNames.Upgrade, "websocket");
            request.Headers.TryAddWithoutValidation(HttpKnownHeaderNames.SecWebSocketVersion, "13");
            request.Headers.TryAddWithoutValidation(HttpKnownHeaderNames.SecWebSocketKey, secKey);
            if (options._requestedSubProtocols?.Count > 0)
            {
                request.Headers.TryAddWithoutValidation(HttpKnownHeaderNames.SecWebSocketProtocol, string.Join(", ", options.RequestedSubProtocols));
            }
            if (options.DangerousDeflateOptions is not null)
            {
                request.Headers.TryAddWithoutValidation(HttpKnownHeaderNames.SecWebSocketExtensions, GetDeflateOptions(options.DangerousDeflateOptions));

                static string GetDeflateOptions(WebSocketDeflateOptions options)
                {
                    var builder = new StringBuilder(ClientWebSocketDeflateConstants.MaxExtensionLength);
                    builder.Append(ClientWebSocketDeflateConstants.Extension).Append("; ");

                    if (options.ClientMaxWindowBits != WebSocketValidate.MaxDeflateWindowBits)
                    {
                        builder.Append(CultureInfo.InvariantCulture, $"{ClientWebSocketDeflateConstants.ClientMaxWindowBits}={options.ClientMaxWindowBits}");
                    }
                    else
                    {
                        // Advertise that we support this option
                        builder.Append(ClientWebSocketDeflateConstants.ClientMaxWindowBits);
                    }

                    if (!options.ClientContextTakeover)
                    {
                        builder.Append("; ").Append(ClientWebSocketDeflateConstants.ClientNoContextTakeover);
                    }

                    if (options.ServerMaxWindowBits != WebSocketValidate.MaxDeflateWindowBits)
                    {
                        builder.Append(CultureInfo.InvariantCulture, $"; {ClientWebSocketDeflateConstants.ServerMaxWindowBits}={options.ServerMaxWindowBits}");
                    }

                    if (!options.ServerContextTakeover)
                    {
                        builder.Append("; ").Append(ClientWebSocketDeflateConstants.ServerNoContextTakeover);
                    }

                    Debug.Assert(builder.Length <= ClientWebSocketDeflateConstants.MaxExtensionLength);
                    return builder.ToString();
                }
            }
        }

        /// <summary>
        /// Creates a pair of a security key for sending in the Sec-WebSocket-Key header and
        /// the associated response we expect to receive as the Sec-WebSocket-Accept header value.
        /// </summary>
        /// <returns>A key-value pair of the request header security key and expected response header value.</returns>
        [SuppressMessage("Microsoft.Security", "CA5350", Justification = "Required by RFC6455")]
        private static KeyValuePair<string, string> CreateSecKeyAndSecWebSocketAccept()
        {
            // GUID appended by the server as part of the security key response.  Defined in the RFC.
            ReadOnlySpan<byte> wsServerGuidBytes = new byte[]
            {
                (byte)'2', (byte)'5', (byte)'8', (byte)'E', (byte)'A', (byte)'F', (byte)'A', (byte)'5', (byte)'-',
                (byte)'E', (byte)'9', (byte)'1', (byte)'4', (byte)'-',
                (byte)'4', (byte)'7', (byte)'D', (byte)'A', (byte)'-',
                (byte)'9', (byte)'5', (byte)'C', (byte)'A', (byte)'-',
                (byte)'C', (byte)'5', (byte)'A', (byte)'B', (byte)'0', (byte)'D', (byte)'C', (byte)'8', (byte)'5', (byte)'B', (byte)'1', (byte)'1'
            };

            Span<byte> bytes = stackalloc byte[24 /* Base64 guid length */ + wsServerGuidBytes.Length];

            // Base64-encode a new Guid's bytes to get the security key
            bool success = Guid.NewGuid().TryWriteBytes(bytes);
            Debug.Assert(success);
            string secKey = Convert.ToBase64String(bytes.Slice(0, 16 /*sizeof(Guid)*/));

            // Get the corresponding ASCII bytes for seckey+wsServerGuidBytes
            for (int i = 0; i < secKey.Length; i++) bytes[i] = (byte)secKey[i];
            wsServerGuidBytes.CopyTo(bytes.Slice(secKey.Length));

            // Hash the seckey+wsServerGuidBytes bytes
            SHA1.TryHashData(bytes, bytes, out int bytesWritten);
            Debug.Assert(bytesWritten == 20 /* SHA1 hash length */);

            // Return the security key + the base64 encoded hashed bytes
            return new KeyValuePair<string, string>(
                secKey,
                Convert.ToBase64String(bytes.Slice(0, bytesWritten)));
        }

        private static void ValidateHeader(HttpHeaders headers, string name, string expectedValue)
        {
            if (headers.NonValidated.TryGetValues(name, out HeaderStringValues hsv))
            {
                if (hsv.Count == 1)
                {
                    foreach (string value in hsv)
                    {
                        if (string.Equals(value, expectedValue, StringComparison.OrdinalIgnoreCase))
                        {
                            return;
                        }
                        break;
                    }
                }

                throw new WebSocketException(WebSocketError.HeaderError, SR.Format(SR.net_WebSockets_InvalidResponseHeader, name, hsv));
            }

            throw new WebSocketException(WebSocketError.Faulted, SR.Format(SR.net_WebSockets_MissingResponseHeader, name));
        }

        /// <summary>Used as a sentinel to indicate that ClientWebSocket should use the system's default proxy.</summary>
        private sealed class DefaultWebProxy : IWebProxy
        {
            public static DefaultWebProxy Instance { get; } = new DefaultWebProxy();
            public ICredentials? Credentials { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public Uri? GetProxy(Uri destination) => throw new NotSupportedException();
            public bool IsBypassed(Uri host) => throw new NotSupportedException();
        }
    }
}
