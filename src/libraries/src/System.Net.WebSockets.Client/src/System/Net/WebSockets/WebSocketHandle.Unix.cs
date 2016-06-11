// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
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
        private sealed class ManagedClientWebSocket : WebSocket
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
            /// <summary>The maximum size in bytes of a message frame header.</summary>
            private const int MaxMessageHeaderLength = 14;
            /// <summary>The maximum size of a control message payload.</summary>
            private const int MaxControlPayloadLength = 125;
            /// <summary>Length of the mask XOR'd with the payload data.</summary>
            private const int MaskLength = 4;
            /// <summary>Default keep-alive interval to use if one wasn't supplied in the options.</summary>
            private const int DefaultKeepAliveIntervalSeconds = 30;
            /// <summary>Size of the receive buffer to use.</summary>
            private const int ReceiveBufferSize = 0x1000;

            /// <summary>
            /// The TcpClient managing the underlying socket. We hold on to this, even though we don't 
            /// touch it after getting its stream, to keep it from being GC'd while the web socket is still in use.
            /// </summary>
            private readonly TcpClient _client = new TcpClient();
            /// <summary>The stream used to communicate with the remote server.</summary>
            private Stream _stream;

            /// <summary>CancellationTokenSource used to abort all current and future operations when anything is canceled or any error occurs.</summary>
            private readonly CancellationTokenSource _abortSource = new CancellationTokenSource();
            /// <summary>Timer used to send periodic pings to the server, at the interval specified</summary>
            private readonly Timer _keepAliveTimer;

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
            private Utf8MessageState _utf8TextState = new Utf8MessageState();
            /// <summary>
            /// Semaphore used to ensure that calls to SendFrameAsync don't run concurrently.  While <see cref="_lastSendAsync"/>
            /// is used to fail if a caller tries to issue another SendAsync while a previous one is running, internally
            /// we use SendFrameAsync as an implementation detail, and it should not cause user requests to SendAsync to fail,
            /// nor should such internal usage be allowed to run concurrently with other internal usage or with SendAsync.
            /// </summary>
            private readonly SemaphoreSlim _sendFrameAsyncLock = new SemaphoreSlim(1, 1);

            public ManagedClientWebSocket()
            {
                // Initialize the keep alive timer.  When active, it'll send periodic ping frames to the server.
                _keepAliveTimer = new Timer(
                    s => ((ManagedClientWebSocket)s).SendKeepAliveFrameAsync(), this,
                    Timeout.Infinite, Timeout.Infinite);

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

            public override void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;

                    _keepAliveTimer.Dispose();
                    _stream?.Dispose();
                    _client?.Dispose();
                }
            }

            public override WebSocketCloseStatus? CloseStatus => _closeStatus;

            public override string CloseStatusDescription => _closeStatusDescription;

            public override WebSocketState State => _state;

            public override string SubProtocol => _subprotocol;

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
                try
                {
                    // Connect over TCP to the remote server
                    await _client.ConnectAsync(uri.Host, uri.Port).ConfigureAwait(false);
                    _stream = _client.GetStream();

                    // Upgrade to SSL if needed
                    if (uri.Scheme == UriScheme.Wss)
                    {
                        var sslStream = new SslStream(_stream);
                        await sslStream.AuthenticateAsClientAsync(
                            uri.Host,
                            options.ClientCertificates,
                            SecurityProtocol.AllowedSecurityProtocols,
                            checkCertificateRevocation: false).ConfigureAwait(false);
                        _stream = sslStream;
                    }

                    // Create the security key and expected response, then build all of the request headers
                    KeyValuePair<string, string> secKeyAndSecWebSocketAccept = CreateSecKeyAndSecWebSocketAccept();
                    byte[] requestHeader = BuildRequestHeader(uri, options, secKeyAndSecWebSocketAccept.Key);

                    // Write out the header to the connection
                    await _stream.WriteAsync(requestHeader, 0, requestHeader.Length, cancellationToken).ConfigureAwait(false);

                    // Parse the response and store our state for the remainder of the connection
                    _subprotocol = await ParseAndValidateConnectResponseAsync(options, secKeyAndSecWebSocketAccept.Value, cancellationToken).ConfigureAwait(false);

                    // Initiate the keep alive timer
                    if (options.KeepAliveInterval != default(TimeSpan))
                    {
                        _keepAliveTimer.Change(options.KeepAliveInterval, options.KeepAliveInterval);
                    }

                    lock (StateUpdateLock)
                    {
                        if (_state == WebSocketState.Connecting)
                        {
                            _state = WebSocketState.Open;
                        }
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
            }

            public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
            {
                try
                {
                    ClientWebSocket.ThrowIfInvalidState(_state, _disposed, s_validSendStates);
                    ThrowIfOperationInProgress(_lastSendAsync);
                }
                catch (Exception e)
                {
                    return Task.FromException(e);
                }

                Task t = SendFrameAsync(_lastSendWasFragment ? MessageOpcode.Continuation : ToMessageOpcode(messageType), endOfMessage, buffer, cancellationToken);
                _lastSendWasFragment = !endOfMessage;

                return t.Status == TaskStatus.RanToCompletion ?
                    t :
                    (_lastSendAsync = WithAbortAsync<int>(t, AbortErrorType.OperationCanceledException, cancellationToken));
            }

            public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
            {
                try
                {
                    ClientWebSocket.ThrowIfInvalidState(_state, _disposed, s_validReceiveStates);
                    ThrowIfOperationInProgress(_lastReceiveAsync);
                }
                catch (Exception e)
                {
                    return Task.FromException<WebSocketReceiveResult>(e);
                }

                Task<WebSocketReceiveResult> t = ReceiveAsyncPrivate(buffer, cancellationToken);
                return t.Status == TaskStatus.RanToCompletion ?
                    t :
                    (_lastReceiveAsync = WithAbortAsync<WebSocketReceiveResult>(t, AbortErrorType.ClosedOrAbortedWebSocketException, cancellationToken));
            }

            public override Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
            {
                try
                {
                    ClientWebSocket.ThrowIfInvalidState(_state, _disposed, s_validCloseStates);
                }
                catch (Exception e)
                {
                    return Task.FromException(e);
                }

                Task t = CloseAsyncPrivate(closeStatus, statusDescription, cancellationToken);
                return t.Status == TaskStatus.RanToCompletion ? 
                    t :
                    WithAbortAsync<WebSocketReceiveResult>(t, AbortErrorType.ClosedOrAbortedWebSocketException, cancellationToken);
            }

            public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
            {
                try
                {
                    ClientWebSocket.ThrowIfInvalidState(_state, _disposed, s_validCloseOutputStates);
                }
                catch (Exception e)
                {
                    return Task.FromException(e);
                }

                Task t = SendCloseFrameAsync(closeStatus, statusDescription, cancellationToken);
                return t.Status == TaskStatus.RanToCompletion ? 
                    t :
                    WithAbortAsync<int>(t, AbortErrorType.ClosedOrAbortedWebSocketException, cancellationToken);
            }

            public override void Abort()
            {
                _abortSource.Cancel();
                Dispose(); // forcibly tear down connection
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
                    throw new WebSocketException(WebSocketError.HeaderError, SR.net_WebSockets_HeaderError_Generic);
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
                        throw new WebSocketException(WebSocketError.HeaderError, SR.net_WebSockets_HeaderError_Generic);
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
            private Task<string> ReadResponseHeaderLineAsync(CancellationToken cancellationToken)
            {
                Task<string> line = ReadResponseHeaderLineAsyncPrivate(cancellationToken);
                return line.Status == TaskStatus.RanToCompletion ?
                    line :
                    WithAbortAsync<string>(line, AbortErrorType.ConnectFailureWebSocketException, cancellationToken);
            }

            private async Task<string> ReadResponseHeaderLineAsyncPrivate(CancellationToken cancellationToken)
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
            private async Task SendFrameAsync(MessageOpcode opcode, bool endOfMessage, ArraySegment<byte> payloadBuffer, CancellationToken cancellationToken)
            {
                await _sendFrameAsyncLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    // Grow our send buffer as needed.  We reuse the buffer for all messages, with it protected by the send frame lock.
                    EnsureBufferLength(ref _sendBuffer, payloadBuffer.Count + MaxMessageHeaderLength);

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

                    // Write the header to the network
                    await _stream.WriteAsync(_sendBuffer, 0, headerLength + payloadBuffer.Count, cancellationToken).ConfigureAwait(false);
                }
                catch (ObjectDisposedException ode)
                {
                    throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely, ode);
                }
                finally
                {
                    _sendFrameAsyncLock.Release();
                }
            }

            private void SendKeepAliveFrameAsync()
            {
                Task pingTask = SendFrameAsync(MessageOpcode.Ping, true, new ArraySegment<byte>(Array.Empty<byte>()), CancellationToken.None);
                // This exists purely to keep the connection alive; ignore any failures.
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

                Debug.Assert(sendBuffer.Length >= MaxMessageHeaderLength, $"Expected sendBuffer to be at least {MaxMessageHeaderLength}, got {sendBuffer.Length}");

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
                    maskOffset = 2;
                }
                else if (payload.Count <= ushort.MaxValue)
                {
                    sendBuffer[1] = 126;
                    sendBuffer[2] = (byte)(payload.Count / 256);
                    sendBuffer[3] = (byte)payload.Count;
                    maskOffset = 4;
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
                    maskOffset = 10;
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
                        MessageHeader? headerOpt = await ReadMessageHeaderAsync(cancellationToken).ConfigureAwait(false);
                        if (headerOpt == null)
                        {
                            // The connection closed; nothing more to read.
                            return new WebSocketReceiveResult(0, WebSocketMessageType.Text, true);
                        }
                        header = headerOpt.GetValueOrDefault();
                    }

                    // If the header represents a ping or a pong, handle it.
                    if (header.Opcode == MessageOpcode.Ping || header.Opcode == MessageOpcode.Pong)
                    {
                        // Consume any (optional) payload associated with the ping/pong
                        if (header.PayloadLength > 0 && _receiveBufferCount < header.PayloadLength)
                        {
                            await EnsureBufferContainsAsync((int)header.PayloadLength, cancellationToken).ConfigureAwait(false);
                        }

                        // If this was a ping, send back a pong response
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

                        // Control message that's meant to be transparent to the user.  Loop around to read again.
                        continue;
                    }

                    WebSocketMessageType messageType = ToMessageType(header.Opcode == MessageOpcode.Continuation ? _lastReceiveHeader.Opcode : header.Opcode);
                    bool endOfMessage = header.Fin;

                    // If the message is a close, handle it by reading and doing special processing of the payload.
                    if (header.Opcode == MessageOpcode.Close)
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

                        // Handle any payload by parsing it into the close status and description
                        if (header.PayloadLength > 0)
                        {
                            if (_receiveBufferCount < header.PayloadLength)
                            {
                                await EnsureBufferContainsAsync((int)header.PayloadLength, cancellationToken).ConfigureAwait(false);
                            }

                            closeStatus = (WebSocketCloseStatus)(_receiveBuffer[_receiveBufferOffset] << 8 | _receiveBuffer[_receiveBufferOffset + 1]);
                            if (header.PayloadLength > 2)
                            {
                                closeStatusDescription = s_textEncoding.GetString(_receiveBuffer, _receiveBufferOffset + 2, (int)header.PayloadLength - 2);
                            }
                            ConsumeFromBuffer((int)header.PayloadLength);

                            if (!IsValidCloseStatus(closeStatus))
                            {
                                await CloseWithErrorAndThrowAsync(WebSocketCloseStatus.ProtocolError, WebSocketError.Faulted, cancellationToken).ConfigureAwait(false);
                            }
                        }

                        // Store the close status and description onto the instance
                        _closeStatus = closeStatus;
                        _closeStatusDescription = closeStatusDescription;

                        // And return them as part of the result message
                        return new WebSocketReceiveResult(0, messageType, true, closeStatus, closeStatusDescription);
                    }

                    // The message should now be a binary or text message (or a continuation of one of those).  Handle it by reading
                    // the payload and returning the contents.
                    Debug.Assert(
                        header.Opcode == MessageOpcode.Continuation || header.Opcode == MessageOpcode.Binary || header.Opcode == MessageOpcode.Text,
                        $"Unexpected opcode {header.Opcode}");

                    // If this is a continuation, replace the opcode with the one of the message it's continuing
                    if (header.Opcode == MessageOpcode.Continuation)
                    {
                        header.Opcode = _lastReceiveHeader.Opcode;
                    }

                    // If there's no data to read, return an appropriate result.
                    int bytesToRead = (int)Math.Min(payloadBuffer.Count, header.PayloadLength);
                    if (bytesToRead == 0)
                    {
                        _lastReceiveHeader = header;
                        return new WebSocketReceiveResult(0, messageType, header.PayloadLength == 0 ? endOfMessage : false);
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
                    if (messageType == WebSocketMessageType.Text &&
                        !TryValidateUtf8(new ArraySegment<byte>(payloadBuffer.Array, payloadBuffer.Offset, bytesToCopy), endOfMessage, _utf8TextState))
                    {
                        await CloseWithErrorAndThrowAsync(WebSocketCloseStatus.InvalidPayloadData, WebSocketError.Faulted, cancellationToken).ConfigureAwait(false);
                    }

                    _lastReceiveHeader = header;
                    return new WebSocketReceiveResult(bytesToCopy, messageType, bytesToCopy == 0 || (endOfMessage && header.PayloadLength == 0));
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

            /// <summary>Send a close message to the server and throw an exception.</summary>
            /// <param name="closeStatus">The close status code to use.</param>
            /// <param name="error">The error reason.</param>
            /// <param name="cancellationToken">The CancellationToken used to cancel the websocket.</param>
            /// <param name="innerException">An optional inner exception to include in the thrown exception.</param>
            private async Task CloseWithErrorAndThrowAsync(
                WebSocketCloseStatus closeStatus, WebSocketError error, CancellationToken cancellationToken, Exception innerException = null)
            {
                if (State == WebSocketState.Open || State == WebSocketState.CloseReceived)
                {
                    await CloseOutputAsync(closeStatus, string.Empty, cancellationToken).ConfigureAwait(false);
                }

                throw new WebSocketException(error, innerException);
            }

            /// <summary>Reads a message header from the network.</summary>
            /// <param name="cancellationToken">The CancellationToken used to cancel the websocket.</param>
            /// <returns>The read header, or null if we couldn't read one.</returns>
            private async Task<MessageHeader?> ReadMessageHeaderAsync(CancellationToken cancellationToken)
            {
                // Read the first two bytes of the header, which gives us the opcode, FIN, reserved bits, and mask state
                if (_receiveBufferCount < 2)
                {
                    await EnsureBufferContainsAsync(2, cancellationToken, throwOnPrematureClosure: false).ConfigureAwait(false);
                    if (_receiveBufferCount < 2)
                    {
                       return null;
                    }
                }

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
                    if (_receiveBufferCount < 2)
                    {
                        await EnsureBufferContainsAsync(2, cancellationToken).ConfigureAwait(false);
                    }

                    header.PayloadLength = (_receiveBuffer[_receiveBufferOffset] << 8) | _receiveBuffer[_receiveBufferOffset + 1];
                    ConsumeFromBuffer(2);
                }
                else if (header.PayloadLength == 127)
                {
                    header.PayloadLength = 0;

                    if (_receiveBufferCount < 8)
                    {
                        await EnsureBufferContainsAsync(8, cancellationToken).ConfigureAwait(false);
                    }

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

                if (shouldFail)
                {
                    await CloseWithErrorAndThrowAsync(WebSocketCloseStatus.ProtocolError, WebSocketError.Faulted, cancellationToken).ConfigureAwait(false);
                }

                // Return the read header
                return header;
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
                byte[] closeBuffer = new byte[MaxMessageHeaderLength + MaxControlPayloadLength];
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

            /// <summary>Ensures that if the task fails, the websocket will abort.</summary>
            /// <param name="task">The task representing the asynchronous operation.</param>
            /// <param name="abortError">The error type associated with this operation.</param>
            /// <param name="cancellationToken">The CancellationToken to use to cancel the websocket.</param>
            /// <returns>The task that should be used in place of the original.</returns>
            private Task<T> WithAbortAsync<T>(Task task, AbortErrorType abortError, CancellationToken cancellationToken)
            {
                // When any operation gets canceled or experiences an error, we need to abort the whole instance
                // in addition to failing the task.  Further, to match the Windows behavior, different operations
                // result in different exceptions to be used for failing the task.  Some of this may be simplified
                // if/when NetworkStream and SslStream get better cancellation support; right now, they only do
                // up-front checks in Read/WriteAsync, but during the actual I/O the cancellation token doesn't apply,
                // so to ensure we avoid deadlocks and can return promptly in the case of cancellation.

                // Create the task to act as the proxy for the original
                var tcs = new AbortTaskCompletionSource<T>() { _webSocket = this };

                // Determine which exception-producing function we should use if a failure occurs.
                switch (abortError)
                {
                    case AbortErrorType.ConnectFailureWebSocketException:
                        tcs._createExceptionFunc = () => new WebSocketException(SR.net_webstatus_ConnectFailure);
                        break;

                    case AbortErrorType.ClosedOrAbortedWebSocketException:
                        tcs._createExceptionFunc = () => new WebSocketException(WebSocketError.InvalidState, SR.Format(SR.net_WebSockets_InvalidState_ClosedOrAborted, "System.Net.WebSockets.InternalClientWebSocket", "Aborted"));
                        break;

                    default:
                        Debug.Assert(abortError == AbortErrorType.OperationCanceledException, $"Unexpected abort error type: {abortError}");
                        tcs._createExceptionFunc = () => new OperationCanceledException();
                        break;
                }

                // Register for cancellation both with the supplied cancellation token and with the abort cancellation token.
                // If we only have the abort token, we can register with it directly.  If we have both, we created a linked source
                // over both and register with that.
                CancellationToken token;
                if (cancellationToken.CanBeCanceled)
                {
                    tcs._cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(_abortSource.Token, cancellationToken);
                    token = tcs._cancellationSource.Token;
                }
                else
                {
                    token = _abortSource.Token;
                }
                token.Register(s =>
                {
                    var localTcs = (AbortTaskCompletionSource<T>)s;
                    Exception e = localTcs._webSocket._abortSource.IsCancellationRequested ? localTcs._createExceptionFunc?.Invoke() : null;
                    if (localTcs.OwnCompletion())
                    {
                        localTcs._registration.Dispose();
                        localTcs._cancellationSource?.Dispose();

                        localTcs._webSocket.Abort();

                        if (e != null)
                        {
                            localTcs.TrySetException(e);
                        }
                        else
                        {
                            localTcs.TrySetCanceled();
                        }
                    }
                }, tcs);

                // Register for the task's completion.
                task.ContinueWith((t, s) =>
                {
                    var localTcs = (AbortTaskCompletionSource<T>)s;
                    bool ownCompletion = localTcs.OwnCompletion();

                    if (ownCompletion)
                    {
                        localTcs._registration.Dispose();
                        localTcs._cancellationSource?.Dispose();
                    }

                    if (t.Status == TaskStatus.RanToCompletion)
                    {
                        Task<T> localTaskT = t as Task<T>;
                        localTcs.TrySetResult(localTaskT != null ? localTaskT.Result : default(T));
                    }
                    else
                    {
                        if (ownCompletion)
                        {
                            localTcs._webSocket.Abort();
                        }

                        if (t.IsCanceled)
                        {
                            localTcs.TrySetCanceled();
                        }
                        else
                        {
                            Debug.Assert(t.IsFaulted, $"Expected task to be faulted, got {t.Status}");
                            localTcs.TrySetException(t.Exception.InnerExceptions);
                        }
                    }
                }, tcs, token, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.DenyChildAttach, TaskScheduler.Default);

                return tcs.Task;
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
                            if (throwOnPrematureClosure)
                            {
                                throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely);
                            }
                            break;
                        }
                    }
                }
            }

            /// <summary>Converts the internal MessageOpcode to the public WebSocketMessageType.</summary>
            private static WebSocketMessageType ToMessageType(MessageOpcode opcode)
            {
                switch (opcode)
                {
                    case MessageOpcode.Text:
                        return WebSocketMessageType.Text;
                    case MessageOpcode.Binary:
                        return WebSocketMessageType.Binary;
                    default:
                        Debug.Assert(opcode == MessageOpcode.Close, $"Expected Close, got {opcode}");
                        return WebSocketMessageType.Close;
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

            private enum AbortErrorType
            {
                ConnectFailureWebSocketException,
                ClosedOrAbortedWebSocketException,
                OperationCanceledException
            }

            private sealed class AbortTaskCompletionSource<T> : TaskCompletionSource<T>
            {
                internal ManagedClientWebSocket _webSocket;
                internal CancellationTokenRegistration _registration;
                internal CancellationTokenSource _cancellationSource;
                internal Func<Exception> _createExceptionFunc;

                internal bool OwnCompletion() => Interlocked.Exchange(ref _createExceptionFunc, null) != null;
            }
        }
    }
}

