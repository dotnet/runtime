// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    internal struct WebSocketHandle
    {
        // WebSocketHandle is the PAL abstraction used by ClientWebSocket.  The implementation
        // is a veneer over the real implementation here in ManagedClientWebSocket.

        private ManagedClientWebSocket _webSocket;

        public bool IsValid => _webSocket != null;

        public WebSocketCloseStatus? CloseStatus => _webSocket.CloseStatus;

        public string CloseStatusDescription => _webSocket.CloseStatusDescription;

        public WebSocketState State => _webSocket.State;

        public string SubProtocol => _webSocket.SubProtocol;

        public static WebSocketHandle Create() => new WebSocketHandle { _webSocket = new ManagedClientWebSocket() };

        public static void CheckPlatformSupport() { /* nop */ }

        public Task ConnectAsyncCore(Uri uri, CancellationToken cancellationToken, ClientWebSocketOptions options) =>
            _webSocket.ConnectAsync(uri, cancellationToken, options);

        public Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken) =>
            _webSocket.SendAsync(buffer, messageType, endOfMessage, cancellationToken);

        public Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken) =>
            _webSocket.ReceiveAsync(buffer, cancellationToken);

        public Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken) =>
            _webSocket.CloseAsync(closeStatus, statusDescription, cancellationToken);

        public Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken) =>
            _webSocket.CloseOutputAsync(closeStatus, statusDescription, cancellationToken);

        public void Dispose() => _webSocket.Dispose();

        public void Abort() => _webSocket.Abort();

        /// <summary>A managed implementation of a client web socket.</summary>
        private sealed class ManagedClientWebSocket
        {
            /// <summary>Per-thread cached StringBuilder for building of strings to send on the connection.</summary>
            [ThreadStatic]
            private static StringBuilder t_cachedStringBuilder;
            /// <summary>Per-thread cached 4-byte mask byte array.</summary>
            [ThreadStatic]
            private static byte[] t_headerMask;

            /// <summary>Thread-safe random number generator used to generate masks for each send.</summary>
            private static readonly RandomNumberGenerator s_random = RandomNumberGenerator.Create();
            /// <summary>Default encoding for HTTP requests. Latin alphabeta no 1, ISO/IEC 8859-1.</summary>
            private static readonly Encoding s_defaultHttpEncoding = Encoding.GetEncoding(28591);
            /// <summary>Encoding for the payload of text messages: UTF8 encoding that throws if invalid bytes are discovered, per the RFC.</summary>
            private static readonly UTF8Encoding s_textEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

            /// <summary>Valid states to be in when calling Connect.</summary>
            private static readonly WebSocketState[] s_validConnectStates = { WebSocketState.None };
            /// <summary>Valid states to be in when calling SendAsync.</summary>
            private static readonly WebSocketState[] s_validSendStates = { WebSocketState.Open, WebSocketState.CloseReceived };
            /// <summary>Valid states to be in when calling ReceiveAsync.</summary>
            private static readonly WebSocketState[] s_validReceiveStates = { WebSocketState.Open, WebSocketState.CloseSent };
            /// <summary>Valid states to be in when calling CloseOutputAsync.</summary>
            private static readonly WebSocketState[] s_validCloseOutputStates = { WebSocketState.Open, WebSocketState.CloseReceived };
            /// <summary>Valid states to be in when calling CloseAsync.</summary>
            private static readonly WebSocketState[] s_validCloseStates = { WebSocketState.Open, WebSocketState.CloseReceived, WebSocketState.CloseSent };

            /// <summary>GUID appended by the server as part of the security key response.  Defined in the RFC.</summary>
            private const string WSServerGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            /// <summary>The maximum size in bytes of a message frame header that includes mask bytes from the client.</summary>
            private const int MaxSendMessageHeaderLength = 14;
            /// <summary>The maximum size in bytes of a message frame header that doesn't include mask bytes, as they're not sent by a server.</summary>
            private const int MaxReceiveMessageHeaderLength = 10;
            /// <summary>The maximum size of a control message payload.</summary>
            private const int MaxControlPayloadLength = 125;
            /// <summary>Length of the mask XOR'd with the payload data.</summary>
            private const int MaskLength = 4;
            /// <summary>Default keep-alive interval to use if one wasn't supplied in the options.</summary>
            private const int DefaultKeepAliveIntervalSeconds = 30;
            /// <summary>Size of the receive buffer to use.</summary>
            private const int ReceiveBufferSize = 0x1000;

            /// <summary>The stream used to communicate with the remote server.</summary>
            private Stream _stream;

            /// <summary>CancellationTokenSource used to abort all current and future operations when anything is canceled or any error occurs.</summary>
            private readonly CancellationTokenSource _abortSource = new CancellationTokenSource();
            /// <summary>Timer used to send periodic pings to the server, at the interval specified</summary>
            private Timer _keepAliveTimer;

            /// <summary>The current state of the web socket in the protocol.</summary>
            private WebSocketState _state;
            /// <summary>Lock used to protect update and check-and-update operations on _state.</summary>
            private object StateUpdateLock => _abortSource;
            /// <summary>The agreed upon subprotocol with the server.</summary>
            private string _subprotocol;
            /// <summary>The reason for the close, as sent by the server, or null if not yet closed.</summary>
            private WebSocketCloseStatus? _closeStatus = null;
            /// <summary>A description of the close reason as sent by the server, or null if not yet closed.</summary>
            private string _closeStatusDescription = null;
            /// <summary>true if Dispose has been called; otherwise, false.</summary>
            private bool _disposed;

            /// <summary>
            /// The last header received in a ReceiveAsync.  If ReceiveAsync got a header but then
            /// returned fewer bytes than was indicated in the header, subsequent ReceiveAsync calls
            /// will use the data from the header to construct the subsequent receive results, and
            /// the payload length in this header will be decremented to indicate the number of bytes
            /// remaining to be received for that header.  As a result, between fragments, the payload
            /// length in this header should be 0.
            /// </summary>
            private MessageHeader _lastReceiveHeader = new MessageHeader { Opcode = MessageOpcode.Text, Fin = true, PayloadLength = 0 };
            /// <summary>Buffer used for reading data from the network.</summary>
            private readonly byte[] _receiveBuffer = new byte[ReceiveBufferSize];
            /// <summary>The offset of the next available byte in the _receiveBuffer.</summary>
            private int _receiveBufferOffset = 0;
            /// <summary>The number of bytes available in the _receiveBuffer.</summary>
            private int _receiveBufferCount = 0;
            /// <summary>
            /// Buffer used to store the complete message to be sent to the stream.  This is needed
            /// rather than just sending a header and then the user's buffer, as we need to mutate the
            /// buffered data with the mask, and we don't want to change the data in the user's buffer.
            /// </summary>
            private byte[] _sendBuffer;
            /// <summary>
            /// Whether the last SendAsync had endOfMessage==false. We need to track this so that we
            /// can send the subsequent message with a continuation opcode if the last message was a fragment.
            /// </summary>
            private bool _lastSendWasFragment;

            // Thread-safety:
            // It's acceptable to call ReceiveAsync and SendAsync in parallel.  One of each may run concurrently.
            // Attemping to invoke any other operations in parallel may corrupt the instance.  Attempting to invoke
            // a send operation while another is in progress or a receive operation while another is in progress will
            // result in an exception.

            /// <summary>
            /// The task returned from the last SendAsync operation to not complete synchronously.
            /// If this is not null and not completed when a subsequent SendAsync is issued, an exception occurs.
            /// </summary>
            private Task _lastSendAsync;
            /// <summary>
            /// The task returned from the last ReceiveAsync operation to not complete synchronously.
            /// If this is not null and not completed when a subsequent ReceiveAsync is issued, an exception occurs.
            /// </summary>
            private Task<WebSocketReceiveResult> _lastReceiveAsync;
            /// <summary>
            /// Tracks the state of the validity of the UTF8 encoding of text payloads.  Text may be split across fragments.
            /// </summary>
            private readonly Utf8MessageState _utf8TextState = new Utf8MessageState();
            /// <summary>
            /// Semaphore used to ensure that calls to SendFrameAsync don't run concurrently.  While <see cref="_lastSendAsync"/>
            /// is used to fail if a caller tries to issue another SendAsync while a previous one is running, internally
            /// we use SendFrameAsync as an implementation detail, and it should not cause user requests to SendAsync to fail,
            /// nor should such internal usage be allowed to run concurrently with other internal usage or with SendAsync.
            /// </summary>
            private readonly SemaphoreSlim _sendFrameAsyncLock = new SemaphoreSlim(1, 1);

            public ManagedClientWebSocket()
            {
                // Set up the abort source so that if it's triggered, we transition the instance appropriately.
                _abortSource.Token.Register(s =>
                {
                    var thisRef = (ManagedClientWebSocket)s;

                    lock (thisRef.StateUpdateLock)
                    {
                        WebSocketState state = thisRef._state;
                        if (state != WebSocketState.Closed && state != WebSocketState.Aborted)
                        {
                            thisRef._state = state != WebSocketState.None && state != WebSocketState.Connecting ?
                                WebSocketState.Aborted :
                                WebSocketState.Closed;
                        }
                    }
                }, this);
            }

            public void Dispose()
            {
                lock (StateUpdateLock)
                {
                    if (!_disposed)
                    {
                        _disposed = true;
                        _keepAliveTimer?.Dispose();
                        _stream?.Dispose();
                    }
                }
            }

            public WebSocketCloseStatus? CloseStatus => _closeStatus;

            public string CloseStatusDescription => _closeStatusDescription;

            public WebSocketState State => _state;

            public string SubProtocol => _subprotocol;

            public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken, ClientWebSocketOptions options)
            {
                // Not currently used:
                // - ClientWebSocketOptions.Credentials
                // - ClientWebSocketOptions.Proxy

                lock (StateUpdateLock)
                {
                    ClientWebSocket.ThrowIfInvalidState(_state, _disposed, s_validConnectStates);
                    _state = WebSocketState.Connecting;
                }

                // Establish connection to the server
                CancellationTokenRegistration registration = cancellationToken.Register(s => ((ManagedClientWebSocket)s).Abort(), this);
                try
                {
                    // Connect to the remote server
                    Socket connectedSocket = await ConnectSocketAsync(uri.Host, uri.Port, cancellationToken).ConfigureAwait(false);
                    SetStream(new AsyncEventArgsNetworkStream(connectedSocket));

                    // Upgrade to SSL if needed
                    if (uri.Scheme == UriScheme.Wss)
                    {
                        var sslStream = new SslStream(_stream);
                        await sslStream.AuthenticateAsClientAsync(
                            uri.Host,
                            options.ClientCertificates,
                            SecurityProtocol.AllowedSecurityProtocols,
                            checkCertificateRevocation: false).ConfigureAwait(false);
                        SetStream(sslStream);
                    }

                    // Create the security key and expected response, then build all of the request headers
                    KeyValuePair<string, string> secKeyAndSecWebSocketAccept = CreateSecKeyAndSecWebSocketAccept();
                    byte[] requestHeader = BuildRequestHeader(uri, options, secKeyAndSecWebSocketAccept.Key);

                    // Write out the header to the connection
                    await _stream.WriteAsync(requestHeader, 0, requestHeader.Length, cancellationToken).ConfigureAwait(false);

                    // Parse the response and store our state for the remainder of the connection
                    _subprotocol = await ParseAndValidateConnectResponseAsync(options, secKeyAndSecWebSocketAccept.Value, cancellationToken).ConfigureAwait(false);

                    lock (StateUpdateLock)
                    {
                        if (_state == WebSocketState.Connecting)
                        {
                            _state = WebSocketState.Open;
                        }
                    }

                    // Now that we're opened, initiate the keep alive timer to send periodic pings
                    if (options.KeepAliveInterval > TimeSpan.Zero)
                    {
                        _keepAliveTimer = new Timer(
                            s => ((ManagedClientWebSocket)s).SendKeepAliveFrameAsync(), this,
                            options.KeepAliveInterval, options.KeepAliveInterval);
                    }
                }
                catch (Exception exc)
                {
                    lock (StateUpdateLock)
                    {
                        if (_state < WebSocketState.Closed)
                        {
                            _state = WebSocketState.Closed;
                        }
                    }

                    Abort();

                    if (exc is WebSocketException)
                    {
                        throw;
                    }
                    throw new WebSocketException(SR.net_webstatus_ConnectFailure, exc);
                }
                finally
                {
                    registration.Dispose();
                }
            }

            public Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
            {
                try
                {
                    ClientWebSocket.ThrowIfInvalidState(_state, _disposed, s_validSendStates);
                    ThrowIfOperationInProgress(_lastSendAsync);
                }
                catch (Exception exc)
                {
                    return Task.FromException(exc);
                }

                Task t = SendFrameAsync(_lastSendWasFragment ? MessageOpcode.Continuation : ToMessageOpcode(messageType), endOfMessage, buffer, cancellationToken);
                _lastSendWasFragment = !endOfMessage;
                _lastSendAsync = t;
                return t;
            }

            public Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
            {
                try
                {
                    ClientWebSocket.ThrowIfInvalidState(_state, _disposed, s_validReceiveStates);
                    ThrowIfOperationInProgress(_lastReceiveAsync);
                }
                catch (Exception exc)
                {
                    return Task.FromException<WebSocketReceiveResult>(exc);
                }

                Task<WebSocketReceiveResult> t = ReceiveAsyncPrivate(buffer, cancellationToken);
                _lastReceiveAsync = t;
                return t;
            }

            public Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
            {
                try
                {
                    ClientWebSocket.ThrowIfInvalidState(_state, _disposed, s_validCloseStates);
                }
                catch (Exception exc)
                {
                    return Task.FromException(exc);
                }

                return CloseAsyncPrivate(closeStatus, statusDescription, cancellationToken);
            }

            public Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
            {
                try
                {
                    ClientWebSocket.ThrowIfInvalidState(_state, _disposed, s_validCloseOutputStates);
                }
                catch (Exception exc)
                {
                    return Task.FromException(exc);
                }

                return SendCloseFrameAsync(closeStatus, statusDescription, cancellationToken);
            }

            public void Abort()
            {
                _abortSource.Cancel();
                Dispose(); // forcibly tear down connection
            }

            /// <summary>Connects a socket to the specified host and port, subject to cancellation and aborting.</summary>
            /// <param name="host">The host to which to connect.</param>
            /// <param name="port">The port to which to connect on the host.</param>
            /// <param name="cancellationToken">The CancellationToken to use to cancel the websocket.</param>
            /// <returns>The connected Socket.</returns>
            private async Task<Socket> ConnectSocketAsync(string host, int port, CancellationToken cancellationToken)
            {
                IPAddress[] addresses = await Dns.GetHostAddressesAsync(host).ConfigureAwait(false);

                ExceptionDispatchInfo lastException = null;
                foreach (IPAddress address in addresses)
                {
                    var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        using (cancellationToken.Register(s => ((Socket)s).Dispose(), socket))
                        using (_abortSource.Token.Register(s => ((Socket)s).Dispose(), socket))
                        {
                            try
                            {
                                await socket.ConnectAsync(address, port).ConfigureAwait(false);
                            }
                            catch (ObjectDisposedException ode)
                            {
                                // If the socket was disposed because cancellation was requested, translate the exception
                                // into a new OperationCanceledException.  Otherwise, let the original ObjectDisposedexception propagate.
                                CancellationToken token = cancellationToken.IsCancellationRequested ? cancellationToken : _abortSource.Token;
                                if (token.IsCancellationRequested)
                                {
                                    throw CreateOperationCanceledException(ode, token);
                                }
                                throw;
                            }
                        }
                        cancellationToken.ThrowIfCancellationRequested(); // in case of a race and socket was disposed after the await
                        _abortSource.Token.ThrowIfCancellationRequested();
                        return socket;
                    }
                    catch (Exception exc)
                    {
                        socket.Dispose();
                        lastException = ExceptionDispatchInfo.Capture(exc);
                    }
                }

                lastException?.Throw();

                Debug.Fail("We should never get here. We should have already returned or an exception should have been thrown.");
                throw new WebSocketException(SR.net_webstatus_ConnectFailure);
            }

            /// <summary>Stores the stream onto the websocket as the stream to use for future operations.</summary>
            /// <param name="stream">The stream to store.</param>
            private void SetStream(Stream stream)
            {
                // Synchronize with Dispose to ensure the Stream is propperly disposed regardless
                // of whether it's stored before or after Dispose is called.
                lock (StateUpdateLock) 
                {
                    if (_disposed)
                    {
                        // Make sure we dispose of the stream if this instance has
                        // already been disposed.
                        stream.Dispose();
                    }

                    // Store the stream.
                    _stream = stream;
                }
            }

            /// <summary>Creates a byte[] containing the headers to send to the server.</summary>
            /// <param name="uri">The Uri of the server.</param>
            /// <param name="options">The options used to configure the websocket.</param>
            /// <param name="secKey">The generated security key to send in the Sec-WebSocket-Key header.</param>
            /// <returns>The byte[] containing the encoded headers ready to send to the network.</returns>
            private static byte[] BuildRequestHeader(Uri uri, ClientWebSocketOptions options, string secKey)
            {
                StringBuilder builder = t_cachedStringBuilder ?? (t_cachedStringBuilder = new StringBuilder());
                Debug.Assert(builder.Length == 0, $"Expected builder to be empty, got one of length {builder.Length}");
                try
                {
                    builder.Append("GET ").Append(uri.PathAndQuery).Append(" HTTP/1.1\r\n");

                    // Add all of the required headers
                    builder.Append("Host: ").Append(uri.IdnHost).Append(":").Append(uri.Port).Append("\r\n");
                    builder.Append("Connection: Upgrade\r\n");
                    builder.Append("Upgrade: websocket\r\n");
                    builder.Append("Sec-WebSocket-Version: 13\r\n");
                    builder.Append("Sec-WebSocket-Key: ").Append(secKey).Append("\r\n");

                    // Add all of the additionally requested headers
                    foreach (string key in options.RequestHeaders.AllKeys)
                    {
                        builder.Append(key).Append(": ").Append(options.RequestHeaders[key]).Append("\r\n");
                    }

                    // Add the optional subprotocols header
                    if (options.RequestedSubProtocols.Count > 0)
                    {
                        builder.Append(HttpKnownHeaderNames.SecWebSocketProtocol).Append(": ");
                        builder.Append(options.RequestedSubProtocols[0]);
                        for (int i = 1; i < options.RequestedSubProtocols.Count; i++)
                        {
                            builder.Append(", ").Append(options.RequestedSubProtocols[i]);
                        }
                        builder.Append("\r\n");
                    }

                    // Add an optional cookies header
                    if (options.Cookies != null)
                    {
                        string header = options.Cookies.GetCookieHeader(uri);
                        if (!string.IsNullOrWhiteSpace(header))
                        {
                            builder.Append(HttpKnownHeaderNames.Cookie).Append(": ").Append(header).Append("\r\n");
                        }
                    }

                    // End the headers
                    builder.Append("\r\n");

                    // Return the bytes for the built up header
                    return s_defaultHttpEncoding.GetBytes(builder.ToString());
                }
                finally
                {
                    // Make sure we clear the builder
                    builder.Clear();
                }
            }

            /// <summary>
            /// Creates a pair of a security key for sending in the Sec-WebSocket-Key header and
            /// the associated response we expect to receive as the Sec-WebSocket-Accept header value.
            /// </summary>
            /// <returns>A key-value pair of the request header security key and expected response header value.</returns>
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

            /// <summary>Read and validate the connect response headers from the server.</summary>
            /// <param name="stream">The stream from which to read the response headers.</param>
            /// <param name="options">The options used to configure the websocket.</param>
            /// <param name="expectedSecWebSocketAccept">The expected value of the Sec-WebSocket-Accept header.</param>
            /// <param name="cancellationToken">The CancellationToken to use to cancel the websocket.</param>
            /// <returns>The agreed upon subprotocol with the server, or null if there was none.</returns>
            private async Task<string> ParseAndValidateConnectResponseAsync(
                ClientWebSocketOptions options, string expectedSecWebSocketAccept, CancellationToken cancellationToken)
            {
                // Read the first line of the response
                string statusLine = await ReadResponseHeaderLineAsync(cancellationToken).ConfigureAwait(false);

                // Depending on the underlying sockets implementation and timing, connecting to a server that then
                // immediately closes the connection may either result in an exception getting thrown from the connect
                // earlier, or it may result in getting to here but reading 0 bytes.  If we read 0 bytes and thus have
                // an empty status line, treat it as a connect failure.
                if (string.IsNullOrEmpty(statusLine))
                {
                    throw new WebSocketException(SR.Format(SR.net_webstatus_ConnectFailure));
                }

                const string ExpectedStatusStart = "HTTP/1.1 ";
                const string ExpectedStatusStatWithCode = "HTTP/1.1 101"; // 101 == SwitchingProtocols

                // If the status line doesn't begin with "HTTP/1.1" or isn't long enough to contain a status code, fail.
                if (!statusLine.StartsWith(ExpectedStatusStart, StringComparison.Ordinal) || statusLine.Length < ExpectedStatusStatWithCode.Length)
                {
                    throw new WebSocketException(WebSocketError.HeaderError);
                }

                // If the status line doesn't contain a status code 101, or if it's long enough to have a status description
                // but doesn't contain whitespace after the 101, fail.
                if (!statusLine.StartsWith(ExpectedStatusStatWithCode, StringComparison.Ordinal) ||
                    (statusLine.Length > ExpectedStatusStatWithCode.Length && !char.IsWhiteSpace(statusLine[ExpectedStatusStatWithCode.Length])))
                {
                    throw new WebSocketException(SR.net_webstatus_ConnectFailure);
                }

                // Read each response header. Be liberal in parsing the response header, treating
                // everything to the left of the colon as the key and everything to the right as the value, trimming both.
                // For each header, validate that we got the expected value.
                bool foundUpgrade = false, foundConnection = false, foundSecWebSocketAccept = false;
                string subprotocol = null;
                string line;
                while (!string.IsNullOrEmpty(line = await ReadResponseHeaderLineAsync(cancellationToken).ConfigureAwait(false)))
                {
                    int colonIndex = line.IndexOf(':');
                    if (colonIndex == -1)
                    {
                        throw new WebSocketException(WebSocketError.HeaderError);
                    }

                    string headerName = line.SubstringTrim(0, colonIndex);
                    string headerValue = line.SubstringTrim(colonIndex + 1);

                    // The Connection, Upgrade, and SecWebSocketAccept headers are required and with specific values.
                    ValidateAndTrackHeader(HttpKnownHeaderNames.Connection, "Upgrade", headerName, headerValue, ref foundConnection);
                    ValidateAndTrackHeader(HttpKnownHeaderNames.Upgrade, "websocket", headerName, headerValue, ref foundUpgrade);
                    ValidateAndTrackHeader(HttpKnownHeaderNames.SecWebSocketAccept, expectedSecWebSocketAccept, headerName, headerValue, ref foundSecWebSocketAccept);

                    // The SecWebSocketProtocol header is optional.  We should only get it with a non-empty value if we requested subprotocols,
                    // and then it must only be one of the ones we requested.  If we got a subprotocol other than one we requested (or if we
                    // already got one in a previous header), fail. Otherwise, track which one we got.
                    if (string.Equals(HttpKnownHeaderNames.SecWebSocketProtocol, headerName, StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrWhiteSpace(headerValue))
                    {
                        string newSubprotocol = options.RequestedSubProtocols.Find(requested => string.Equals(requested, headerValue, StringComparison.OrdinalIgnoreCase));
                        if (newSubprotocol == null || subprotocol != null)
                        {
                            throw new WebSocketException(
                                WebSocketError.UnsupportedProtocol,
                                SR.Format(SR.net_WebSockets_AcceptUnsupportedProtocol, string.Join(", ", options.RequestedSubProtocols), subprotocol));
                        }
                        subprotocol = newSubprotocol;
                    }
                }
                if (!foundUpgrade || !foundConnection || !foundSecWebSocketAccept)
                {
                    throw new WebSocketException(SR.net_webstatus_ConnectFailure);
                }

                return subprotocol;
            }

            /// <summary>Validates a received header against expected values and tracks that we've received it.</summary>
            /// <param name="targetHeaderName">The header name against which we're comparing.</param>
            /// <param name="targetHeaderValue">The header value against which we're comparing.</param>
            /// <param name="foundHeaderName">The actual header name received.</param>
            /// <param name="foundHeaderValue">The actual header value received.</param>
            /// <param name="foundHeader">A bool tracking whether this header has been seen.</param>
            private static void ValidateAndTrackHeader(
                string targetHeaderName, string targetHeaderValue,
                string foundHeaderName, string foundHeaderValue,
                ref bool foundHeader)
            {
                bool isTargetHeader = string.Equals(targetHeaderName, foundHeaderName, StringComparison.OrdinalIgnoreCase);
                if (!foundHeader)
                {
                    if (isTargetHeader)
                    {
                        if (!string.Equals(targetHeaderValue, foundHeaderValue, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new WebSocketException(SR.Format(SR.net_WebSockets_InvalidResponseHeader, targetHeaderName, foundHeaderValue));
                        }
                        foundHeader = true;
                    }
                }
                else
                {
                    if (isTargetHeader)
                    {
                        throw new WebSocketException(SR.Format(SR.net_webstatus_ConnectFailure));
                    }
                }
            }

            /// <summary>Reads a line from the stream.</summary>
            /// <param name="stream">The stream from which to read.</param>
            /// <param name="cancellationToken">The CancellationToken used to cancel the websocket.</param>
            /// <returns>The read line, or null if none could be read.</returns>
            private async Task<string> ReadResponseHeaderLineAsync(CancellationToken cancellationToken)
            {
                StringBuilder sb = t_cachedStringBuilder;
                if (sb != null)
                {
                    t_cachedStringBuilder = null;
                    Debug.Assert(sb.Length == 0, $"Expected empty StringBuilder");
                }
                else
                {
                    sb = new StringBuilder();
                }

                char prevChar = '\0';
                try
                {
                    while (true)
                    {
                        // Ensure we have data to process
                        if (_receiveBufferCount == 0)
                        {
                            await EnsureBufferContainsAsync(1, cancellationToken, throwOnPrematureClosure: false).ConfigureAwait(false);
                            if (_receiveBufferCount == 0)
                            {
                                break;
                            }
                        }

                        // Process the next char
                        char curChar = (char)_receiveBuffer[_receiveBufferOffset];
                        ConsumeFromBuffer(1);

                        if (prevChar == '\r' && curChar == '\n')
                        {
                            break;
                        }
                        sb.Append(curChar);
                        prevChar = curChar;
                    }

                    if (sb.Length > 0 && sb[sb.Length - 1] == '\r')
                    {
                        sb.Length = sb.Length - 1;
                    }

                    return sb.ToString();
                }
                finally
                {
                    sb.Clear();
                    t_cachedStringBuilder = sb;
                }
            }

            /// <summary>Sends a websocket frame to the network.</summary>
            /// <param name="opcode">The opcode for the message.</param>
            /// <param name="endOfMessage">The value of the FIN bit for the message.</param>
            /// <param name="payloadBuffer">The buffer containing the payload data fro the message.</param>
            /// <param name="cancellationToken">The CancellationToken to use to cancel the websocket.</param>
            private Task SendFrameAsync(MessageOpcode opcode, bool endOfMessage, ArraySegment<byte> payloadBuffer, CancellationToken cancellationToken)
            {
                // TODO: #4900 SendFrameAsync should in theory typically complete synchronously, making it fast and allocation free.
                // However, due to #4900, it almost always yields, resulting in all of the allocations involved in an async method
                // yielding, e.g. the boxed state machine, the Action delegate, the MoveNextRunner, and the resulting Task, plus it's
                // common that the awaited operation completes so fast after the await that we may end up allocating an AwaitTaskContinuation
                // inside of the TaskAwaiter.  Since SendFrameAsync is such a core code path, until that can be fixed, we put some
                // optimizations in place to avoid a few of those expenses, at the expense of more complicated code; for the common case,
                // this code has fewer than half the number and size of allocations.  If/when that issue is fixed, this method should be deleted
                // and replaced by SendFrameFallbackAsync, which is the same logic but in a much more easily understand flow.

                // If a cancelable cancellation token was provided, that would require registering with it, which means more state we have to
                // pass around (the CancellationTokenRegistration), so if it is cancelable, just immediately go to the fallback path.
                // Similarly, it should be rare that there are multiple outstanding calls to SendFrameAsync, but if there are, again
                // fall back to the fallback path.
                return cancellationToken.CanBeCanceled || !_sendFrameAsyncLock.Wait(0) ?
                    SendFrameFallbackAsync(opcode, endOfMessage, payloadBuffer, cancellationToken) :
                    SendFrameLockAcquiredNonCancelableAsync(opcode, endOfMessage, payloadBuffer);
            }

            /// <summary>Sends a websocket frame to the network. The caller must hold the sending lock.</summary>
            /// <param name="opcode">The opcode for the message.</param>
            /// <param name="endOfMessage">The value of the FIN bit for the message.</param>
            /// <param name="payloadBuffer">The buffer containing the payload data fro the message.</param>
            private Task SendFrameLockAcquiredNonCancelableAsync(MessageOpcode opcode, bool endOfMessage, ArraySegment<byte> payloadBuffer)
            {
                Debug.Assert(_sendFrameAsyncLock.CurrentCount == 0, "Caller should hold the _sendFrameAsyncLock");

                // If we get here, the cancellation token is not cancelable so we don't have to worry about it,
                // and we own the semaphore, so we don't need to asynchronously wait for it.
                Task writeTask = null;
                bool releaseSemaphore = true;
                try
                {
                    // Write the payload synchronously to the buffer, then write that buffer out to the network.
                    int sendBytes = WriteFrameToSendBuffer(opcode, endOfMessage, payloadBuffer);
                    writeTask = _stream.WriteAsync(_sendBuffer, 0, sendBytes, CancellationToken.None);

                    // If the operation happens to complete synchronously (or, more specifically, by
                    // the time we get from the previous line to here, release the semaphore, propagate
                    // exceptions, and we're done.
                    if (writeTask.IsCompleted)
                    {
                        writeTask.GetAwaiter().GetResult(); // propagate any exceptions
                        return Task.CompletedTask;
                    }

                    // Up until this point, if an exception occurred (such as when accessing _stream or when
                    // calling GetResult), we want to release the semaphore. After this point, the semaphore needs
                    // to remain held until writeTask completes.
                    releaseSemaphore = false;
                }
                catch (Exception exc)
                {
                    return Task.FromException(_state == WebSocketState.Aborted ?
                        CreateOperationCanceledException(exc) :
                        new WebSocketException(WebSocketError.ConnectionClosedPrematurely, exc));
                }
                finally
                {
                    if (releaseSemaphore)
                    {
                        _sendFrameAsyncLock.Release();
                    }
                }

                // The write was not yet completed.  Create and return a continuation that will
                // release the semaphore and translate any exception that occurred.
                return writeTask.ContinueWith((t, s) =>
                {
                    var thisRef = (ManagedClientWebSocket)s;
                    thisRef._sendFrameAsyncLock.Release();

                    try { t.GetAwaiter().GetResult(); }
                    catch (Exception exc)
                    {
                        throw thisRef._state == WebSocketState.Aborted ?
                            CreateOperationCanceledException(exc) :
                            new WebSocketException(WebSocketError.ConnectionClosedPrematurely, exc);
                    }
                }, this, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }

            private async Task SendFrameFallbackAsync(MessageOpcode opcode, bool endOfMessage, ArraySegment<byte> payloadBuffer, CancellationToken cancellationToken)
            {
                await _sendFrameAsyncLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    int sendBytes = WriteFrameToSendBuffer(opcode, endOfMessage, payloadBuffer);
                    using (cancellationToken.Register(s => ((ManagedClientWebSocket)s).Abort(), this))
                    {
                        await _stream.WriteAsync(_sendBuffer, 0, sendBytes, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (Exception exc)
                {
                    throw _state == WebSocketState.Aborted ?
                        CreateOperationCanceledException(exc, cancellationToken) :
                        new WebSocketException(WebSocketError.ConnectionClosedPrematurely, exc);
                }
                finally
                {
                    _sendFrameAsyncLock.Release();
                }
            }

            /// <summary>Writes a frame into the send buffer, which can then be sent over the network.</summary>
            private int WriteFrameToSendBuffer(MessageOpcode opcode, bool endOfMessage, ArraySegment<byte> payloadBuffer)
            {
                // Grow our send buffer as needed.  We reuse the buffer for all messages, with it protected by the send frame lock.
                EnsureBufferLength(ref _sendBuffer, payloadBuffer.Count + MaxSendMessageHeaderLength);

                // Write the message header data to the buffer.  We need to know where the mask starts so that we can use
                // the mask to manipulate the payload data, and we need to know the total length for sending it on the wire.
                int maskOffset = WriteHeader(opcode, _sendBuffer, payloadBuffer, endOfMessage);
                int headerLength = maskOffset + MaskLength;

                // If there is payload data, XOR it with the mask.  We do the manipulation in the send buffer so as to avoid
                // changing the data in the caller-supplied payload buffer.
                if (payloadBuffer.Count > 0)
                {
                    for (int i = 0; i < payloadBuffer.Count; i++)
                    {
                        _sendBuffer[i + headerLength] = (byte)
                            (payloadBuffer.Array[payloadBuffer.Offset + i] ^
                            _sendBuffer[maskOffset + (i & 3)]); // (i % MaskLength)
                    }
                }

                // Return the number of bytes in the send buffer
                return headerLength + payloadBuffer.Count;
            }

            private void SendKeepAliveFrameAsync()
            {
                bool acquiredLock = _sendFrameAsyncLock.Wait(0);
                if (acquiredLock)
                {
                    // This exists purely to keep the connection alive; don't wait for the result, and ignore any failures.
                    // The call will handle releasing the lock.
                    SendFrameLockAcquiredNonCancelableAsync(MessageOpcode.Ping, true, new ArraySegment<byte>(Array.Empty<byte>()));
                }
                else
                {
                    // If the lock is already held, something is already getting sent,
                    // so there's no need to send a keep-alive ping.
                }
            }

            private static int WriteHeader(MessageOpcode opcode, byte[] sendBuffer, ArraySegment<byte> payload, bool endOfMessage)
            {
                // Client header format:
                // 1 bit - FIN - 1 if this is the final fragment in the message (it could be the only fragment), otherwise 0
                // 1 bit - RSV1 - Reserved - 0
                // 1 bit - RSV2 - Reserved - 0
                // 1 bit - RSV3 - Reserved - 0
                // 4 bits - Opcode - How to interpret the payload
                //     - 0x0 - continuation
                //     - 0x1 - text
                //     - 0x2 - binary
                //     - 0x8 - connection close
                //     - 0x9 - ping
                //     - 0xA - pong
                //     - (0x3 to 0x7, 0xB-0xF - reserved)
                // 1 bit - Masked - 1 if the payload is masked, 0 if it's not.  Must be 1 for the client
                // 7 bits, 7+16 bits, or 7+64 bits - Payload length
                //     - For length 0 through 125, 7 bits storing the length
                //     - For lengths 126 through 2^16, 7 bits storing the value 126, followed by 16 bits storing the length
                //     - For lengths 2^16+1 through 2^64, 7 bits storing the value 127, followed by 64 bytes storing the length
                // 4 bytes - Mask - random value XOR'd with each 4 bytes of the payload, round-robin
                // Length bytes - Payload data

                Debug.Assert(sendBuffer.Length >= MaxSendMessageHeaderLength, $"Expected sendBuffer to be at least {MaxSendMessageHeaderLength}, got {sendBuffer.Length}");

                sendBuffer[0] = (byte)opcode; // 4 bits for the opcode
                if (endOfMessage)
                {
                    sendBuffer[0] |= 0x80; // 1 bit for FIN
                }

                // Store the payload length.
                int maskOffset;
                if (payload.Count <= 125)
                {
                    sendBuffer[1] = (byte)payload.Count;
                    maskOffset = 2; // no additional payload length
                }
                else if (payload.Count <= ushort.MaxValue)
                {
                    sendBuffer[1] = 126;
                    sendBuffer[2] = (byte)(payload.Count / 256);
                    sendBuffer[3] = (byte)payload.Count;
                    maskOffset = 2 + sizeof(ushort); // additional 2 bytes for 16-bit length
                }
                else
                {
                    sendBuffer[1] = 127;
                    int length = payload.Count;
                    for (int i = 9; i >= 2; i--)
                    {
                        sendBuffer[i] = (byte)length;
                        length = length / 256;
                    }
                    maskOffset = 2 + sizeof(ulong); // additional 8 bytes for 64-bit length
                }

                // Generate the mask.
                sendBuffer[1] |= 0x80;
                WriteRandomMask(sendBuffer, maskOffset);

                // Return the position of the mask.
                return maskOffset;
            }

            /// <summary>Writes a 4-byte random mask to the specified buffer at the specified offset.</summary>
            /// <param name="buffer">The buffer to which to write the mask.</param>
            /// <param name="offset">The offset into the buffer at which to write the mask.</param>
            private static void WriteRandomMask(byte[] buffer, int offset)
            {
                byte[] mask = t_headerMask ?? (t_headerMask = new byte[MaskLength]);
                Debug.Assert(mask.Length == MaskLength, $"Expected mask of length {MaskLength}, got {mask.Length}");
                s_random.GetBytes(mask);
                Buffer.BlockCopy(mask, 0, buffer, offset, MaskLength);
            }

            /// <summary>
            /// Receive the next text, binary, continuation, or close message, returning information about it and
            /// writing its payload into the supplied buffer.  Other control messages may be consumed and processed
            /// as part of this operation, but data about them will not be returned.
            /// </summary>
            /// <param name="payloadBuffer">The buffer into which payload data should be written.</param>
            /// <param name="cancellationToken">The CancellationToken used to cancel the websocket.</param>
            /// <returns>Information about the received message.</returns>
            private async Task<WebSocketReceiveResult> ReceiveAsyncPrivate(ArraySegment<byte> payloadBuffer, CancellationToken cancellationToken)
            {
                // This is a long method.  While splitting it up into pieces would arguably help with readability, doing so would
                // also result in more allocations, as each async method that yields ends up with multiple allocations.  The impact
                // of those allocations is amortized across all of the awaits in the method, and since we generally expect a receive
                // operation to require at most a single yield (while waiting for data to arrive), it's more efficient to have
                // everything in the one method.  We do separate out pieces for handling close and ping/pong messages, as we expect
                // those to be much less frequent (e.g. we should only get one close per websocket), and thus we can afford to pay
                // a bit more for readability and maintainability.

                CancellationTokenRegistration registration = cancellationToken.Register(s => ((ManagedClientWebSocket)s).Abort(), this);
                try
                {
                    while (true) // in case we get control frames that should be ignored from the user's perspective
                    {
                        // Get the last received header.  If its payload length is non-zero, that means we previously
                        // received the header but were only able to read a part of the fragment, so we should skip
                        // reading another header and just proceed to use that same header and read more data associated
                        // with it.  If instead its payload length is zero, then we've completed the processing of
                        // thta message, and we should read the next header.
                        MessageHeader header = _lastReceiveHeader;
                        if (header.PayloadLength == 0)
                        {
                            if (_receiveBufferCount < MaxReceiveMessageHeaderLength)
                            {
                                // Make sure we have the first two bytes, which includes the start of the payload length.
                                if (_receiveBufferCount < 2)
                                {
                                    await EnsureBufferContainsAsync(2, cancellationToken, throwOnPrematureClosure: false).ConfigureAwait(false);
                                    if (_receiveBufferCount < 2)
                                    {
                                        // The connection closed; nothing more to read.
                                        return new WebSocketReceiveResult(0, WebSocketMessageType.Text, true);
                                    }
                                }

                                // Then make sure we have the full header based on the payload length.
                                long payloadLength = _receiveBuffer[_receiveBufferOffset + 1] & 0x7F;
                                if (payloadLength > 125)
                                {
                                    await EnsureBufferContainsAsync(
                                        2 + (payloadLength == 126 ? sizeof(ushort) : sizeof(ulong)), // additional 2 or 8 bytes for 16-bit or 64-bit length
                                        cancellationToken).ConfigureAwait(false);
                                }
                            }

                            if (!TryParseMessageHeaderFromReceiveBuffer(out header))
                            {
                                await CloseWithReceiveErrorAndThrowAsync(WebSocketCloseStatus.ProtocolError, WebSocketError.Faulted, cancellationToken).ConfigureAwait(false);
                            }
                        }

                        // If the header represents a ping or a pong, it's a control message meant
                        // to be transparent to the user, so handle it and then loop around to read again.
                        // Alternatively, if it's a close message, handle it and exit.
                        if (header.Opcode == MessageOpcode.Ping || header.Opcode == MessageOpcode.Pong)
                        {
                            await HandleReceivedPingPongAsync(header, cancellationToken).ConfigureAwait(false);
                            continue;
                        }
                        else if (header.Opcode == MessageOpcode.Close)
                        {
                            return await HandleReceivedCloseAsync(header, cancellationToken).ConfigureAwait(false);
                        }

                        // If this is a continuation, replace the opcode with the one of the message it's continuing
                        if (header.Opcode == MessageOpcode.Continuation)
                        {
                            header.Opcode = _lastReceiveHeader.Opcode;
                        }

                        // The message should now be a binary or text message.  Handle it by reading the payload and returning the contents.
                        Debug.Assert(header.Opcode == MessageOpcode.Binary || header.Opcode == MessageOpcode.Text, $"Unexpected opcode {header.Opcode}");

                        // If there's no data to read, return an appropriate result.
                        int bytesToRead = (int)Math.Min(payloadBuffer.Count, header.PayloadLength);
                        if (bytesToRead == 0)
                        {
                            _lastReceiveHeader = header;
                            return new WebSocketReceiveResult(
                                0, 
                                header.Opcode == MessageOpcode.Text ? WebSocketMessageType.Text : WebSocketMessageType.Binary, 
                                header.PayloadLength == 0 ? header.Fin : false);
                        }

                        // Otherwise, read as much of the payload as we can efficiently, and upate the header to reflect how much data
                        // remains for future reads.

                        if (_receiveBufferCount == 0)
                        {
                            await EnsureBufferContainsAsync(1, cancellationToken, throwOnPrematureClosure: false).ConfigureAwait(false);
                        }

                        int bytesToCopy = Math.Min(bytesToRead, _receiveBufferCount);
                        Buffer.BlockCopy(_receiveBuffer, _receiveBufferOffset, payloadBuffer.Array, payloadBuffer.Offset, bytesToCopy);
                        ConsumeFromBuffer(bytesToCopy);
                        header.PayloadLength -= bytesToCopy;

                        // If this a text message, validate that it contains valid UTF8.
                        if (header.Opcode == MessageOpcode.Text &&
                            !TryValidateUtf8(new ArraySegment<byte>(payloadBuffer.Array, payloadBuffer.Offset, bytesToCopy), header.Fin, _utf8TextState))
                        {
                            await CloseWithReceiveErrorAndThrowAsync(WebSocketCloseStatus.InvalidPayloadData, WebSocketError.Faulted, cancellationToken).ConfigureAwait(false);
                        }

                        _lastReceiveHeader = header;
                        return new WebSocketReceiveResult(
                            bytesToCopy, 
                            header.Opcode == MessageOpcode.Text ? WebSocketMessageType.Text : WebSocketMessageType.Binary,
                            bytesToCopy == 0 || (header.Fin && header.PayloadLength == 0));
                    }
                }
                catch (Exception exc)
                {
                    throw _state == WebSocketState.Aborted ?
                        new WebSocketException(WebSocketError.InvalidState, SR.Format(SR.net_WebSockets_InvalidState_ClosedOrAborted, "System.Net.WebSockets.InternalClientWebSocket", "Aborted"), exc) :
                        new WebSocketException(WebSocketError.ConnectionClosedPrematurely, exc);
                }
                finally
                {
                    registration.Dispose();
                }
            }

            /// <summary>Processes a received close message.</summary>
            /// <param name="header">The message header.</param>
            /// <param name="cancellationToken">The cancellation token to use to cancel the websocket.</param>
            /// <returns>The received result message.</returns>
            private async Task<WebSocketReceiveResult> HandleReceivedCloseAsync(
                MessageHeader header, CancellationToken cancellationToken)
            {
                lock (StateUpdateLock)
                {
                    if (_state == WebSocketState.CloseSent)
                    {
                        _state = WebSocketState.Closed;
                    }
                    else if (_state < WebSocketState.CloseReceived)
                    {
                        _state = WebSocketState.CloseReceived;
                    }
                }

                WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure;
                string closeStatusDescription = string.Empty;

                // Handle any payload by parsing it into the close status and description.
                if (header.PayloadLength == 1)
                {
                    // The close payload length can be 0 or >= 2, but not 1.
                    await CloseWithReceiveErrorAndThrowAsync(WebSocketCloseStatus.ProtocolError, WebSocketError.Faulted, cancellationToken).ConfigureAwait(false);
                }
                else if (header.PayloadLength >= 2)
                {
                    if (_receiveBufferCount < header.PayloadLength)
                    {
                        await EnsureBufferContainsAsync((int)header.PayloadLength, cancellationToken).ConfigureAwait(false);
                    }

                    closeStatus = (WebSocketCloseStatus)(_receiveBuffer[_receiveBufferOffset] << 8 | _receiveBuffer[_receiveBufferOffset + 1]);
                    if (!IsValidCloseStatus(closeStatus))
                    {
                        await CloseWithReceiveErrorAndThrowAsync(WebSocketCloseStatus.ProtocolError, WebSocketError.Faulted, cancellationToken).ConfigureAwait(false);
                    }

                    if (header.PayloadLength > 2)
                    {
                        try
                        {
                            closeStatusDescription = s_textEncoding.GetString(_receiveBuffer, _receiveBufferOffset + 2, (int)header.PayloadLength - 2);
                        }
                        catch (DecoderFallbackException exc)
                        {
                            await CloseWithReceiveErrorAndThrowAsync(WebSocketCloseStatus.ProtocolError, WebSocketError.Faulted, cancellationToken, exc).ConfigureAwait(false);
                        }
                    }
                    ConsumeFromBuffer((int)header.PayloadLength);
                }

                // Store the close status and description onto the instance.
                _closeStatus = closeStatus;
                _closeStatusDescription = closeStatusDescription;

                // And return them as part of the result message.
                return new WebSocketReceiveResult(0, WebSocketMessageType.Close, true, closeStatus, closeStatusDescription);
            }

            /// <summary>Processes a received ping or pong message.</summary>
            /// <param name="header">The message header.</param>
            /// <param name="cancellationToken">The cancellation token to use to cancel the websocket.</param>
            private async Task HandleReceivedPingPongAsync(MessageHeader header, CancellationToken cancellationToken)
            {
                // Consume any (optional) payload associated with the ping/pong.
                if (header.PayloadLength > 0 && _receiveBufferCount < header.PayloadLength)
                {
                    await EnsureBufferContainsAsync((int)header.PayloadLength, cancellationToken).ConfigureAwait(false);
                }

                // If this was a ping, send back a pong response.
                if (header.Opcode == MessageOpcode.Ping)
                {
                    await SendFrameAsync(
                        MessageOpcode.Pong, true,
                        new ArraySegment<byte>(_receiveBuffer, _receiveBufferOffset, (int)header.PayloadLength), cancellationToken).ConfigureAwait(false);
                }

                // Regardless of whether it was a ping or pong, we no longer need the payload.
                if (header.PayloadLength > 0)
                {
                    ConsumeFromBuffer((int)header.PayloadLength);
                }
            }

            /// <summary>Check whether a close status is valid according to the RFC.</summary>
            /// <param name="closeStatus">The status to validate.</param>
            /// <returns>true if the status if valid; otherwise, false.</returns>
            private static bool IsValidCloseStatus(WebSocketCloseStatus closeStatus)
            {
                // 0-999: "not used"
                // 1000-2999: reserved for the protocol; we need to check individual codes manually
                // 3000-3999: reserved for use by higher-level code
                // 4000-4999: reserved for private use
                // 5000-: not mentioned in RFC

                if (closeStatus < (WebSocketCloseStatus)1000 || closeStatus >= (WebSocketCloseStatus)5000)
                {
                    return false;
                }

                if (closeStatus >= (WebSocketCloseStatus)3000)
                {
                    return true;
                }

                switch (closeStatus) // check for the 1000-2999 range known codes
                {
                    case WebSocketCloseStatus.EndpointUnavailable:
                    case WebSocketCloseStatus.InternalServerError:
                    case WebSocketCloseStatus.InvalidMessageType:
                    case WebSocketCloseStatus.InvalidPayloadData:
                    case WebSocketCloseStatus.MandatoryExtension:
                    case WebSocketCloseStatus.MessageTooBig:
                    case WebSocketCloseStatus.NormalClosure:
                    case WebSocketCloseStatus.PolicyViolation:
                    case WebSocketCloseStatus.ProtocolError:
                        return true;

                    default:
                        return false;
                }
            }

            /// <summary>Send a close message to the server and throw an exception, in response to getting bad data from the server.</summary>
            /// <param name="closeStatus">The close status code to use.</param>
            /// <param name="error">The error reason.</param>
            /// <param name="cancellationToken">The CancellationToken used to cancel the websocket.</param>
            /// <param name="innerException">An optional inner exception to include in the thrown exception.</param>
            private async Task CloseWithReceiveErrorAndThrowAsync(
                WebSocketCloseStatus closeStatus, WebSocketError error, CancellationToken cancellationToken, Exception innerException = null)
            {
                // Close the connection if it hasn't already been closed
                if (State == WebSocketState.Open || State == WebSocketState.CloseReceived)
                {
                    await CloseOutputAsync(closeStatus, string.Empty, cancellationToken).ConfigureAwait(false);
                }

                // Dump our receive buffer; we're in a bad state to do any further processing
                _receiveBufferCount = 0;

                // Let the caller know we've failed
                throw new WebSocketException(error, innerException);
            }

            /// <summary>Parses a message header from the buffer.  This assumes the header is in the buffer.</summary>
            /// <param name="header">The read header.</param>
            /// <returns>true if a header was read; false if the header was invalid.</returns>
            private bool TryParseMessageHeaderFromReceiveBuffer(out MessageHeader resultHeader)
            {
                Debug.Assert(_receiveBufferCount >= 2, $"Expected to at least have the first two bytes of the header.");

                var header = new MessageHeader();

                header.Fin = (_receiveBuffer[_receiveBufferOffset] & 0x80) != 0;
                bool reservedSet = (_receiveBuffer[_receiveBufferOffset] & 0x70) != 0;
                header.Opcode = (MessageOpcode)(_receiveBuffer[_receiveBufferOffset] & 0xF);

                bool masked = (_receiveBuffer[_receiveBufferOffset + 1] & 0x80) != 0;
                header.PayloadLength = _receiveBuffer[_receiveBufferOffset + 1] & 0x7F;

                ConsumeFromBuffer(2);

                // Read the remainder of the payload length, if necessary
                if (header.PayloadLength == 126)
                {
                    Debug.Assert(_receiveBufferCount >= 2, $"Expected to have two bytes for the payload length.");
                    header.PayloadLength = (_receiveBuffer[_receiveBufferOffset] << 8) | _receiveBuffer[_receiveBufferOffset + 1];
                    ConsumeFromBuffer(2);
                }
                else if (header.PayloadLength == 127)
                {
                    Debug.Assert(_receiveBufferCount >= 8, $"Expected to have eight bytes for the payload length.");
                    header.PayloadLength = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        header.PayloadLength = (header.PayloadLength << 8) | _receiveBuffer[_receiveBufferOffset + i];
                    }
                    ConsumeFromBuffer(8);
                }

                // Do basic validation of the header
                bool shouldFail = masked || reservedSet;
                switch (header.Opcode)
                {
                    case MessageOpcode.Continuation:
                        if (_lastReceiveHeader.Fin)
                        {
                            // Can't continue from a final message
                            shouldFail = true;
                        }
                        break;

                    case MessageOpcode.Binary:
                    case MessageOpcode.Text:
                        if (!_lastReceiveHeader.Fin)
                        {
                            // Must continue from a non-final message
                            shouldFail = true;
                        }
                        break;

                    case MessageOpcode.Close:
                    case MessageOpcode.Ping:
                    case MessageOpcode.Pong:
                        if (header.PayloadLength > MaxControlPayloadLength || !header.Fin)
                        {
                            // Invalid control messgae
                            shouldFail = true;
                        }
                        break;

                    default:
                        // Unknown opcode
                        shouldFail = true;
                        break;
                }

                // Return the read header
                resultHeader = header;
                return !shouldFail;
            }

            /// <summary>Send a close message, then receive until we get a close response message.</summary>
            /// <param name="closeStatus">The close status to send.</param>
            /// <param name="statusDescription">The close status description to send.</param>
            /// <param name="cancellationToken">The CancellationToken to use to cancel the websocket.</param>
            private async Task CloseAsyncPrivate(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
            {
                // Send the close message
                await SendCloseFrameAsync(closeStatus, statusDescription, cancellationToken).ConfigureAwait(false);

                // Wait for a close response
                byte[] closeBuffer = new byte[MaxSendMessageHeaderLength + MaxControlPayloadLength];
                while (_state < WebSocketState.CloseReceived)
                {
                    await ReceiveAsyncPrivate(new ArraySegment<byte>(closeBuffer), cancellationToken).ConfigureAwait(false);
                }

                // We're closed
                lock (StateUpdateLock)
                {
                    if (_state < WebSocketState.Closed)
                    {
                        _state = WebSocketState.Closed;
                    }
                }
            }

            /// <summary>Sends a close message to the server.</summary>
            /// <param name="closeStatus">The close status to send.</param>
            /// <param name="statusDescription">The close status description to send.</param>
            /// <param name="cancellationToken">The CancellationToken to use to cancel the websocket.</param>
            private async Task SendCloseFrameAsync(WebSocketCloseStatus closeStatus, string closeStatusDescription, CancellationToken cancellationToken)
            {
                // Close payload is two bytes containing the close status followed by a UTF8-encoding of the status description, if it exists.

                byte[] buffer;
                if (string.IsNullOrEmpty(closeStatusDescription))
                {
                    buffer = new byte[2];
                }
                else
                {
                    buffer = new byte[2 + s_textEncoding.GetByteCount(closeStatusDescription)];
                    int encodedLength = s_textEncoding.GetBytes(closeStatusDescription, 0, closeStatusDescription.Length, buffer, 2);
                    Debug.Assert(buffer.Length - 2 == encodedLength, $"GetByteCount and GetBytes encoded count didn't match");
                }

                ushort closeStatusValue = (ushort)closeStatus;
                buffer[0] = (byte)(closeStatusValue >> 8);
                buffer[1] = (byte)(closeStatusValue & 0xFF);

                await SendFrameAsync(MessageOpcode.Close, true, new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);

                lock (StateUpdateLock)
                {
                    if (_state < WebSocketState.CloseSent)
                    {
                        _state = WebSocketState.CloseSent;
                    }
                    else if (_state == WebSocketState.CloseReceived)
                    {
                        _state = WebSocketState.Closed;
                    }
                }
            }

            private void ConsumeFromBuffer(int count)
            {
                Debug.Assert(count >= 0, $"Expected non-negative count, got {count}");
                Debug.Assert(count <= _receiveBufferCount, $"Trying to consume {count}, which is more than exists {_receiveBufferCount}");
                _receiveBufferCount -= count;
                _receiveBufferOffset += count;
            }

            private async Task EnsureBufferContainsAsync(int minimumRequiredBytes, CancellationToken cancellationToken, bool throwOnPrematureClosure = true)
            {
                Debug.Assert(minimumRequiredBytes <= ReceiveBufferSize, $"Requested number of bytes {minimumRequiredBytes} must not exceed {ReceiveBufferSize}");

                // If we don't have enough data in the buffer to satisfy the minimum required, read some more.
                if (_receiveBufferCount < minimumRequiredBytes)
                {
                    // If there's any data in the buffer, shift it down.  
                    if (_receiveBufferCount > 0)
                    {
                        Buffer.BlockCopy(_receiveBuffer, _receiveBufferOffset, _receiveBuffer, 0, _receiveBufferCount);
                    }
                    _receiveBufferOffset = 0;

                    // While we don't have enough data, read more.
                    while (_receiveBufferCount < minimumRequiredBytes)
                    {
                        int numRead = await _stream.ReadAsync(_receiveBuffer, _receiveBufferCount, ReceiveBufferSize - _receiveBufferCount, cancellationToken).ConfigureAwait(false);
                        Debug.Assert(numRead >= 0, $"Expected non-negative bytes read, got {numRead}");
                        _receiveBufferCount += numRead;
                        if (numRead == 0)
                        {
                            // The connection closed before we were able to read everything we needed.
                            // If it was due to use being disposed, fail.  If it was due to the connection
                            // being closed and it wasn't expected, fail.  If it was due to the connection
                            // being closed and that was expected, exit gracefully.
                            if (_disposed)
                            {
                                throw new ObjectDisposedException(nameof(ClientWebSocket));
                            }
                            else if (throwOnPrematureClosure)
                            {
                                throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely);
                            }
                            break;
                        }
                    }
                }
            }

            /// <summary>Converts the public WebSocketMessageType to the internal MessageOpcode.</summary>
            private static MessageOpcode ToMessageOpcode(WebSocketMessageType type)
            {
                switch (type)
                {
                    case WebSocketMessageType.Text:
                        return MessageOpcode.Text;
                    case WebSocketMessageType.Binary:
                        return MessageOpcode.Binary;
                    default:
                        Debug.Assert(type == WebSocketMessageType.Close, $"Unexpected message type {type}");
                        return MessageOpcode.Close;
                }
            }

            /// <summary>
            /// Grows the specified buffer if it's not at least the specified minimum length.
            /// Data is not copied if the buffer is grown.
            /// </summary>
            private static void EnsureBufferLength(ref byte[] buffer, int minLength)
            {
                if (buffer == null || buffer.Length < minLength)
                {
                    buffer = new byte[minLength];
                }
            }

            /// <summary>Aborts the websocket and throws an exception if an existing operation is in progress.</summary>
            private void ThrowIfOperationInProgress(Task operationTask, [CallerMemberName] string methodName = null)
            {
                if (operationTask != null && !operationTask.IsCompleted)
                {
                    Abort();
                    throw new InvalidOperationException(SR.Format(SR.net_Websockets_AlreadyOneOutstandingOperation, methodName));
                }
            }

            /// <summary>Creates an OperationCanceledException instance, using a default message and the specified inner exception and token.</summary>
            private static Exception CreateOperationCanceledException(Exception innerException, CancellationToken cancellationToken = default(CancellationToken))
            {
                return new OperationCanceledException(
                    new OperationCanceledException().Message,
                    innerException,
                    cancellationToken);
            }

            // From https://raw.githubusercontent.com/aspnet/WebSockets/dev/src/Microsoft.AspNetCore.WebSockets.Protocol/Utilities.cs
            // Performs a stateful validation of UTF-8 bytes.
            // It checks for valid formatting, overlong encodings, surrogates, and value ranges.
            private static bool TryValidateUtf8(ArraySegment<byte> arraySegment, bool endOfMessage, Utf8MessageState state)
            {
                for (int i = arraySegment.Offset; i < arraySegment.Offset + arraySegment.Count;)
                {
                    // Have we started a character sequence yet?
                    if (!state.SequenceInProgress)
                    {
                        // The first byte tells us how many bytes are in the sequence.
                        state.SequenceInProgress = true;
                        byte b = arraySegment.Array[i];
                        i++;
                        if ((b & 0x80) == 0) // 0bbbbbbb, single byte
                        {
                            state.AdditionalBytesExpected = 0;
                            state.CurrentDecodeBits = b & 0x7F;
                            state.ExpectedValueMin = 0;
                        }
                        else if ((b & 0xC0) == 0x80)
                        {
                            // Misplaced 10bbbbbb continuation byte. This cannot be the first byte.
                            return false;
                        }
                        else if ((b & 0xE0) == 0xC0) // 110bbbbb 10bbbbbb
                        {
                            state.AdditionalBytesExpected = 1;
                            state.CurrentDecodeBits = b & 0x1F;
                            state.ExpectedValueMin = 0x80;
                        }
                        else if ((b & 0xF0) == 0xE0) // 1110bbbb 10bbbbbb 10bbbbbb
                        {
                            state.AdditionalBytesExpected = 2;
                            state.CurrentDecodeBits = b & 0xF;
                            state.ExpectedValueMin = 0x800;
                        }
                        else if ((b & 0xF8) == 0xF0) // 11110bbb 10bbbbbb 10bbbbbb 10bbbbbb
                        {
                            state.AdditionalBytesExpected = 3;
                            state.CurrentDecodeBits = b & 0x7;
                            state.ExpectedValueMin = 0x10000;
                        }
                        else // 111110bb & 1111110b & 11111110 && 11111111 are not valid
                        {
                            return false;
                        }
                    }
                    while (state.AdditionalBytesExpected > 0 && i < arraySegment.Offset + arraySegment.Count)
                    {
                        byte b = arraySegment.Array[i];
                        if ((b & 0xC0) != 0x80)
                        {
                            return false;
                        }

                        i++;
                        state.AdditionalBytesExpected--;

                        // Each continuation byte carries 6 bits of data 0x10bbbbbb.
                        state.CurrentDecodeBits = (state.CurrentDecodeBits << 6) | (b & 0x3F);

                        if (state.AdditionalBytesExpected == 1 && state.CurrentDecodeBits >= 0x360 && state.CurrentDecodeBits <= 0x37F)
                        {
                            // This is going to end up in the range of 0xD800-0xDFFF UTF-16 surrogates that are not allowed in UTF-8;
                            return false;
                        }
                        if (state.AdditionalBytesExpected == 2 && state.CurrentDecodeBits >= 0x110)
                        {
                            // This is going to be out of the upper Unicode bound 0x10FFFF.
                            return false;
                        }
                    }
                    if (state.AdditionalBytesExpected == 0)
                    {
                        state.SequenceInProgress = false;
                        if (state.CurrentDecodeBits < state.ExpectedValueMin)
                        {
                            // Overlong encoding (e.g. using 2 bytes to encode something that only needed 1).
                            return false;
                        }
                    }
                }
                if (endOfMessage && state.SequenceInProgress)
                {
                    return false;
                }
                return true;
            }

            private sealed class Utf8MessageState
            {
                public bool SequenceInProgress;
                public int AdditionalBytesExpected;
                public int ExpectedValueMin;
                public int CurrentDecodeBits;
            }

            private enum MessageOpcode : byte
            {
                Continuation = 0x0,
                Text = 0x1,
                Binary = 0x2,
                Close = 0x8,
                Ping = 0x9,
                Pong = 0xA
            }

            [StructLayout(LayoutKind.Auto)]
            private struct MessageHeader
            {
                public MessageOpcode Opcode;
                public bool Fin;
                public long PayloadLength;
            }

            /// <summary>
            /// A custom network stream that stores and reuses a single SocketAsyncEventArgs instance
            /// for reads and a single SocketAsyncEventArgs instance for writes.  This limits it to
            /// supporting a single read and a single write at a time, but with much less per-operation
            /// overhead than with System.Net.Sockets.NetworkStream.
            /// </summary>
            private sealed class AsyncEventArgsNetworkStream : NetworkStream
            {
                private readonly Socket _socket;
                private readonly SocketAsyncEventArgs _readArgs;
                private readonly SocketAsyncEventArgs _writeArgs;

                private AsyncTaskMethodBuilder<int> _readAtmb;
                private AsyncTaskMethodBuilder _writeAtmb;
                private bool _disposed;

                public AsyncEventArgsNetworkStream(Socket socket) : base(socket, ownsSocket: true)
                {
                    _socket = socket;

                    _readArgs = new SocketAsyncEventArgs();
                    _readArgs.Completed += ReadCompleted;

                    _writeArgs = new SocketAsyncEventArgs();
                    _writeArgs.Completed += WriteCompleted;
                }

                protected override void Dispose(bool disposing)
                {
                    base.Dispose(disposing);

                    if (disposing && !_disposed)
                    {
                        _disposed = true;
                        try
                        {
                            _readArgs.Dispose();
                            _writeArgs.Dispose();
                        }
                        catch (ObjectDisposedException) { }
                    }
                }

                public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return Task.FromCanceled<int>(cancellationToken);
                    }

                    _readAtmb = new AsyncTaskMethodBuilder<int>();
                    Task<int> t = _readAtmb.Task;

                    _readArgs.SetBuffer(buffer, offset, count);
                    if (!_socket.ReceiveAsync(_readArgs))
                    {
                        ReadCompleted(null, _readArgs);
                    }

                    return t;
                }

                private void ReadCompleted(object sender, SocketAsyncEventArgs e)
                {
                    if (e.SocketError == SocketError.Success)
                    {
                        _readAtmb.SetResult(e.BytesTransferred);
                    }
                    else
                    {
                        _readAtmb.SetException(CreateException(e.SocketError));
                    }
                }

                public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return Task.FromCanceled(cancellationToken);
                    }

                    _writeAtmb = new AsyncTaskMethodBuilder();
                    Task t = _writeAtmb.Task;

                    _writeArgs.SetBuffer(buffer, offset, count);
                    if (!_socket.SendAsync(_writeArgs))
                    {
                        // TODO: #4900 This path should be hit very frequently (sends should very frequently simply
                        // write into the kernel's send buffer), but it's practically never getting hit due to the current
                        // System.Net.Sockets.dll implementation that always completing asynchronously on success :(
                        // If that doesn't get fixed, we should try to come up with some alternative here.  This is
                        // an important path, in part as it means the caller will complete awaits synchronously rather
                        // than spending the costs associated with yielding in each async method up the call chain.
                        // (This applies to ReadAsync as well, but typically to a much less extent.)
                        WriteCompleted(null, _writeArgs);
                    }

                    return t;
                }

                private void WriteCompleted(object sender, SocketAsyncEventArgs e)
                {
                    if (e.SocketError == SocketError.Success)
                    {
                        _writeAtmb.SetResult();
                    }
                    else
                    {
                        _writeAtmb.SetException(CreateException(e.SocketError));
                    }
                }

                private Exception CreateException(SocketError error)
                {
                    if (_disposed)
                    {
                        return new ObjectDisposedException(nameof(ClientWebSocket));
                    }
                    else if (error == SocketError.OperationAborted)
                    {
                        return new OperationCanceledException();
                    }
                    else
                    {
                        return new IOException(SR.net_WebSockets_Generic, new SocketException((int)error));
                    }
                }
            }
        }
    }
}
