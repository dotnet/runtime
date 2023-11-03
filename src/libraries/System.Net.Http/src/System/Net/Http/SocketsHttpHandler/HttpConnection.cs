// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed partial class HttpConnection : HttpConnectionBase
    {
        /// <summary>Default size of the read buffer used for the connection.</summary>
        private const int InitialReadBufferSize =
#if DEBUG
            10;
#else
            4096;
#endif
        /// <summary>Default size of the write buffer used for the connection.</summary>
        private const int InitialWriteBufferSize = InitialReadBufferSize;
        /// <summary>
        /// Size after which we'll close the connection rather than send the payload in response
        /// to final error status code sent by the server when using Expect: 100-continue.
        /// </summary>
        private const int Expect100ErrorSendThreshold = 1024;
        /// <summary>How long a chunk indicator is allowed to be.</summary>
        /// <remarks>
        /// While most chunks indicators will contain no more than ulong.MaxValue.ToString("X").Length characters,
        /// "chunk extensions" are allowed. We place a limit on how long a line can be to avoid OOM issues if an
        /// infinite chunk length is sent.  This value is arbitrary and can be changed as needed.
        /// </remarks>
        private const int MaxChunkBytesAllowed = 16 * 1024;

        private static readonly ulong s_http10Bytes = BitConverter.ToUInt64("HTTP/1.0"u8);
        private static readonly ulong s_http11Bytes = BitConverter.ToUInt64("HTTP/1.1"u8);

        private readonly HttpConnectionPool _pool;
        internal readonly Stream _stream;
        private readonly TransportContext? _transportContext;

        private HttpRequestMessage? _currentRequest;
        private ArrayBuffer _writeBuffer;
        private int _allowedReadLineBytes;

        /// <summary>Reusable array used to get the values for each header being written to the wire.</summary>
        [ThreadStatic]
        private static string[]? t_headerValues;

        private const int ReadAheadTask_NotStarted = 0;
        private const int ReadAheadTask_Started = 1;
        private const int ReadAheadTask_CompletionReserved = 2;
        private int _readAheadTaskStatus;
        private ValueTask<int> _readAheadTask;
        private ArrayBuffer _readBuffer;

        private int _keepAliveTimeoutSeconds; // 0 == no timeout
        private bool _inUse;
        private bool _detachedFromPool;
        private bool _canRetry;
        private bool _connectionClose; // Connection: close was seen on last response

        private const int Status_Disposed = 1;
        private int _disposed;

        public HttpConnection(
            HttpConnectionPool pool,
            Stream stream,
            TransportContext? transportContext,
            IPEndPoint? remoteEndPoint)
            : base(pool, remoteEndPoint)
        {
            Debug.Assert(pool != null);
            Debug.Assert(stream != null);

            _pool = pool;
            _stream = stream;

            _transportContext = transportContext;

            _writeBuffer = new ArrayBuffer(InitialWriteBufferSize, usePool: false);
            _readBuffer = new ArrayBuffer(InitialReadBufferSize, usePool: false);

            if (NetEventSource.Log.IsEnabled()) TraceConnection(_stream);
        }

        ~HttpConnection() => Dispose(disposing: false);

        public override void Dispose() => Dispose(disposing: true);

        private void Dispose(bool disposing)
        {
            // Ensure we're only disposed once.  Dispose could be called concurrently, for example,
            // if the request and the response were running concurrently and both incurred an exception.
            if (Interlocked.Exchange(ref _disposed, Status_Disposed) != Status_Disposed)
            {
                if (NetEventSource.Log.IsEnabled()) Trace("Connection closing.");

                MarkConnectionAsClosed();

                if (!_detachedFromPool)
                {
                    _pool.InvalidateHttp11Connection(this, disposing);
                }

                if (disposing)
                {
                    GC.SuppressFinalize(this);
                    _stream.Dispose();
                }
            }
        }

        /// <summary>Prepare an idle connection to be used for a new request.
        /// The caller MUST call SendAsync afterwards if this method returns true.</summary>
        /// <param name="async">Indicates whether the coming request will be sync or async.</param>
        /// <returns>True if connection can be used, false if it is invalid due to a timeout or receiving EOF or unexpected data.</returns>
        public bool PrepareForReuse(bool async)
        {
            if (CheckKeepAliveTimeoutExceeded())
            {
                return false;
            }

            // We may already have a read-ahead task if we did a previous scavenge and haven't used the connection since.
            // If the read-ahead task is completed, then we've received either EOF or erroneous data the connection, so it's not usable.
            if (ReadAheadTaskHasStarted)
            {
                return TryOwnReadAheadTaskCompletion();
            }

            // Check to see if we've received anything on the connection; if we have, that's
            // either erroneous data (we shouldn't have received anything yet) or the connection
            // has been closed; either way, we can't use it.
            if (!async && _stream is NetworkStream networkStream)
            {
                // Directly poll the socket rather than doing an async read, so that we can
                // issue an appropriate sync read when we actually need it.
                try
                {
                    return !networkStream.Socket.Poll(0, SelectMode.SelectRead);
                }
                catch (Exception e) when (e is SocketException || e is ObjectDisposedException)
                {
                    // Poll can throw when used on a closed socket.
                    return false;
                }
            }
            else
            {
                Debug.Assert(_readAheadTaskStatus == ReadAheadTask_NotStarted);
                _readAheadTaskStatus = ReadAheadTask_CompletionReserved;

                // Perform an async read on the stream, since we're going to need to read from it
                // anyway, and in doing so we can avoid the extra syscall.
                try
                {
#pragma warning disable CA2012 // we're very careful to ensure the ValueTask is only consumed once, even though it's stored into a field
                    _readAheadTask = _stream.ReadAsync(_readBuffer.AvailableMemory);
#pragma warning restore CA2012

                    return !_readAheadTask.IsCompleted;
                }
                catch (Exception error)
                {
                    // If reading throws, eat the error and don't reuse the connection.
                    if (NetEventSource.Log.IsEnabled()) Trace($"Error performing read ahead: {error}");
                    return false;
                }
            }
        }

        /// <summary>Check whether a currently idle connection is still usable, or should be scavenged.</summary>
        /// <returns>True if connection can be used, false if it is invalid due to a timeout or receiving EOF or unexpected data.</returns>
        public override bool CheckUsabilityOnScavenge()
        {
            if (CheckKeepAliveTimeoutExceeded())
            {
                return false;
            }

            // We may already have a read-ahead task if we did a previous scavenge and haven't used the connection since.
            EnsureReadAheadTaskHasStarted();

            // If the read-ahead task is completed, then we've received either EOF or erroneous data the connection, so it's not usable.
            return !_readAheadTask.IsCompleted;
        }

        private bool ReadAheadTaskHasStarted =>
            _readAheadTaskStatus != ReadAheadTask_NotStarted;

        private bool TryOwnReadAheadTaskCompletion() =>
            Interlocked.CompareExchange(ref _readAheadTaskStatus, ReadAheadTask_CompletionReserved, ReadAheadTask_Started) == ReadAheadTask_Started;

        private void EnsureReadAheadTaskHasStarted()
        {
            if (_readAheadTaskStatus == ReadAheadTask_NotStarted)
            {
                Debug.Assert(_readAheadTask == default);

                _readAheadTaskStatus = ReadAheadTask_Started;

#pragma warning disable CA2012 // we're very careful to ensure the ValueTask is only consumed once, even though it's stored into a field
                _readAheadTask = ReadAheadWithZeroByteReadAsync();
#pragma warning restore CA2012
            }

            async ValueTask<int> ReadAheadWithZeroByteReadAsync()
            {
                Debug.Assert(_readAheadTask == default);
                Debug.Assert(_readBuffer.ActiveLength == 0);

                try
                {
                    // Issue a zero-byte read.
                    // If the underlying stream supports it, this will not complete until the stream has data available,
                    // which will avoid pinning the connection's read buffer (and possibly allow us to release it to the buffer pool in the future, if desired).
                    // If not, it will complete immediately.
                    await _stream.ReadAsync(Memory<byte>.Empty).ConfigureAwait(false);

                    // We don't know for sure that the stream actually has data available, so we need to issue a real read now.
                    int read = await _stream.ReadAsync(_readBuffer.AvailableMemory).ConfigureAwait(false);

                    // PrepareForReuse will check TryOwnReadAheadTaskCompletion before calling into SendAsync.
                    // If we can own the completion from within the read-ahead task, it means that PrepareForReuse hasn't been called yet.
                    // In that case we've received EOF/erroneous data before we sent the request headers, and the connection can't be reused.
                    if (TryOwnReadAheadTaskCompletion())
                    {
                        if (NetEventSource.Log.IsEnabled()) Trace("Read-ahead task observed data before the request was sent.");
                    }

                    return read;
                }
                catch (Exception error) when (TryOwnReadAheadTaskCompletion())
                {
                    if (NetEventSource.Log.IsEnabled()) Trace($"Error performing read ahead: {error}");

                    return 0;
                }
            }
        }

        private bool CheckKeepAliveTimeoutExceeded()
        {
            // We intentionally honor the Keep-Alive timeout on all HTTP/1.X versions, not just 1.0. This is to maximize compat with
            // servers that use a lower idle timeout than the client, but give us a hint in the form of a Keep-Alive timeout parameter.
            // If _keepAliveTimeoutSeconds is 0, no timeout has been set.
            return _keepAliveTimeoutSeconds != 0 &&
                GetIdleTicks(Environment.TickCount64) >= _keepAliveTimeoutSeconds * 1000;
        }

        public TransportContext? TransportContext => _transportContext;

        public HttpConnectionKind Kind => _pool.Kind;

        private int ReadBufferSize => _readBuffer.Capacity;

        private ReadOnlyMemory<byte> RemainingBuffer => _readBuffer.ActiveMemory;

        private void ConsumeFromRemainingBuffer(int bytesToConsume)
        {
            Debug.Assert(bytesToConsume <= _readBuffer.ActiveLength);
            _readBuffer.Discard(bytesToConsume);
        }

        private void WriteHeaders(HttpRequestMessage request, HttpMethod normalizedMethod)
        {
            Debug.Assert(request.RequestUri is not null);

            // Write the request line
            WriteAsciiString(normalizedMethod.Method);
            _writeBuffer.EnsureAvailableSpace(1);
            _writeBuffer.AvailableSpan[0] = (byte)' ';
            _writeBuffer.Commit(1);

            if (ReferenceEquals(normalizedMethod, HttpMethod.Connect))
            {
                // RFC 7231 #section-4.3.6.
                // Write only CONNECT foo.com:345 HTTP/1.1
                if (!request.HasHeaders || request.Headers.Host is not string host)
                {
                    throw new HttpRequestException(SR.net_http_request_no_host);
                }

                WriteAsciiString(host);
            }
            else
            {
                if (Kind == HttpConnectionKind.Proxy)
                {
                    // Proxied requests contain full URL
                    Debug.Assert(request.RequestUri.Scheme == Uri.UriSchemeHttp);
                    WriteBytes("http://"u8);
                    WriteHost(request.RequestUri);
                }

                WriteAsciiString(request.RequestUri.PathAndQuery);
            }

            // Fall back to 1.1 for all versions other than 1.0
            Debug.Assert(request.Version.Major >= 0 && request.Version.Minor >= 0); // guaranteed by Version class
            bool isHttp10 = request.Version.Minor == 0 && request.Version.Major == 1;
            WriteBytes(isHttp10 ? " HTTP/1.0\r\n"u8 : " HTTP/1.1\r\n"u8);

            // Write special additional headers.  If a host isn't in the headers list, then a Host header
            // wasn't set, so as it's required by HTTP 1.1 spec, send one based on the Request Uri.
            if (!request.HasHeaders || request.Headers.Host is null)
            {
                if (_pool.HostHeaderLineBytes is byte[] hostHeaderLineBytes)
                {
                    Debug.Assert(Kind != HttpConnectionKind.Proxy);
                    WriteBytes(hostHeaderLineBytes);
                }
                else
                {
                    Debug.Assert(Kind == HttpConnectionKind.Proxy);
                    WriteBytes(KnownHeaders.Host.AsciiBytesWithColonSpace);
                    WriteHost(request.RequestUri);
                    WriteCRLF();
                }
            }

            // Determine cookies to send
            string? cookiesFromContainer = null;
            if (_pool.Settings._useCookies)
            {
                cookiesFromContainer = _pool.Settings._cookieContainer!.GetCookieHeader(request.RequestUri);
                if (cookiesFromContainer == "")
                {
                    cookiesFromContainer = null;
                }
            }

            // Write request headers
            if (request.HasHeaders || cookiesFromContainer is not null)
            {
                WriteHeaderCollection(request.Headers, cookiesFromContainer);
            }

            // Write content headers
            if (request.Content is HttpContent content)
            {
                WriteHeaderCollection(content.Headers);
            }
            else
            {
                // Write out Content-Length: 0 header to indicate no body,
                // unless this is a method that never has a body.
                if (normalizedMethod.MustHaveRequestBody)
                {
                    WriteBytes("Content-Length: 0\r\n"u8);
                }
            }

            // CRLF for end of headers.
            WriteCRLF();

            void WriteHost(Uri requestUri)
            {
                // Uri.IdnHost is missing '[', ']' characters around IPv6 address
                // and it also contains ScopeID for Link-Local addresses
                string host = requestUri.HostNameType == UriHostNameType.IPv6 ? requestUri.Host : requestUri.IdnHost;
                WriteAsciiString(host);

                if (!requestUri.IsDefaultPort)
                {
                    _writeBuffer.EnsureAvailableSpace(6);
                    Span<byte> buffer = _writeBuffer.AvailableSpan;
                    buffer[0] = (byte)':';
                    bool success = ((uint)requestUri.Port).TryFormat(buffer.Slice(1), out int bytesWritten);
                    Debug.Assert(success);
                    _writeBuffer.Commit(bytesWritten + 1);
                }
            }
        }

        private void WriteHeaderCollection(HttpHeaders headers, string? cookiesFromContainer = null)
        {
            Debug.Assert(_currentRequest is not null);

            HeaderEncodingSelector<HttpRequestMessage>? encodingSelector = _pool.Settings._requestHeaderEncodingSelector;
            ref string[]? headerValues = ref t_headerValues;

            foreach (HeaderEntry header in headers.GetEntries())
            {
                if (header.Key.KnownHeader is KnownHeader knownHeader)
                {
                    WriteBytes(knownHeader.AsciiBytesWithColonSpace);
                }
                else
                {
                    WriteAsciiString(header.Key.Name);
                    WriteBytes(": "u8);
                }

                int headerValuesCount = HttpHeaders.GetStoreValuesIntoStringArray(header.Key, header.Value, ref headerValues);
                Debug.Assert(headerValuesCount > 0, "No values for header??");

                Encoding? valueEncoding = encodingSelector?.Invoke(header.Key.Name, _currentRequest);

                WriteString(headerValues[0], valueEncoding);

                if (cookiesFromContainer is not null && header.Key.Equals(KnownHeaders.Cookie))
                {
                    WriteBytes("; "u8); // Cookies use "; " as the separator
                    WriteString(cookiesFromContainer, valueEncoding);
                    cookiesFromContainer = null;
                }

                // Some headers such as User-Agent and Server use space as a separator (see: ProductInfoHeaderParser)
                if (headerValuesCount > 1)
                {
                    HttpHeaderParser? parser = header.Key.Parser;
                    string separator = HttpHeaderParser.DefaultSeparator;
                    if (parser != null && parser.SupportsMultipleValues)
                    {
                        separator = parser.Separator!;
                    }

                    for (int i = 1; i < headerValuesCount; i++)
                    {
                        WriteAsciiString(separator);
                        WriteString(headerValues[i], valueEncoding);
                    }
                }

                WriteCRLF();
            }

            if (cookiesFromContainer is not null)
            {
                WriteBytes(KnownHeaders.Cookie.AsciiBytesWithColonSpace);
                WriteString(cookiesFromContainer, encodingSelector?.Invoke(HttpKnownHeaderNames.Cookie, _currentRequest));
                WriteCRLF();
            }
        }

        private void WriteCRLF()
        {
            _writeBuffer.EnsureAvailableSpace(2);
            Span<byte> buffer = _writeBuffer.AvailableSpan;
            buffer[1] = (byte)'\n';
            buffer[0] = (byte)'\r';
            _writeBuffer.Commit(2);
        }

        private void WriteBytes(ReadOnlySpan<byte> bytes)
        {
            _writeBuffer.EnsureAvailableSpace(bytes.Length);
            bytes.CopyTo(_writeBuffer.AvailableSpan);
            _writeBuffer.Commit(bytes.Length);
        }

        private void WriteAsciiString(string s)
        {
            _writeBuffer.EnsureAvailableSpace(s.Length);
            int length = Encoding.ASCII.GetBytes(s, _writeBuffer.AvailableSpan);
            Debug.Assert(length == s.Length);
            Debug.Assert(Encoding.ASCII.GetString(_writeBuffer.AvailableSpan.Slice(0, length)) == s);
            _writeBuffer.Commit(length);
        }

        private void WriteString(string s, Encoding? encoding)
        {
            if (encoding is null)
            {
                _writeBuffer.EnsureAvailableSpace(s.Length);
                Span<byte> buffer = _writeBuffer.AvailableSpan;

                OperationStatus status = Ascii.FromUtf16(s, buffer, out int bytesWritten);

                if (status == OperationStatus.InvalidData)
                {
                    ThrowForInvalidCharEncoding();
                }

                Debug.Assert(status == OperationStatus.Done);
                Debug.Assert(bytesWritten == s.Length);

                _writeBuffer.Commit(s.Length);
            }
            else
            {
                _writeBuffer.EnsureAvailableSpace(encoding.GetMaxByteCount(s.Length));
                int length = encoding.GetBytes(s, _writeBuffer.AvailableSpan);
                _writeBuffer.Commit(length);
            }

            static void ThrowForInvalidCharEncoding() =>
                throw new HttpRequestException(SR.net_http_request_invalid_char_encoding);
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
        {
            Debug.Assert(_currentRequest == null, $"Expected null {nameof(_currentRequest)}.");
            Debug.Assert(_readBuffer.ActiveLength == 0, "Unexpected data in read buffer");
            Debug.Assert(_readAheadTaskStatus != ReadAheadTask_Started);

            MarkConnectionAsNotIdle();

            TaskCompletionSource<bool>? allowExpect100ToContinue = null;
            Task? sendRequestContentTask = null;

            _currentRequest = request;
            HttpMethod normalizedMethod = HttpMethod.Normalize(request.Method);

            _canRetry = false;

            // Send the request.
            if (NetEventSource.Log.IsEnabled()) Trace($"Sending request: {request}");
            CancellationTokenRegistration cancellationRegistration = RegisterCancellation(cancellationToken);
            try
            {
                if (HttpTelemetry.Log.IsEnabled()) HttpTelemetry.Log.RequestHeadersStart(Id);

                WriteHeaders(request, normalizedMethod);

                if (HttpTelemetry.Log.IsEnabled()) HttpTelemetry.Log.RequestHeadersStop();

                if (request.Content == null)
                {
                    // We have nothing more to send, so flush out any headers we haven't yet sent.
                    await FlushAsync(async).ConfigureAwait(false);
                }
                else
                {
                    bool hasExpectContinueHeader = request.HasHeaders && request.Headers.ExpectContinue == true;
                    if (NetEventSource.Log.IsEnabled()) Trace($"Request content is not null, start processing it. hasExpectContinueHeader = {hasExpectContinueHeader}");

                    // Send the body if there is one.  We prefer to serialize the sending of the content before
                    // we try to receive any response, but if ExpectContinue has been set, we allow the sending
                    // to run concurrently until we receive the final status line, at which point we wait for it.
                    if (!hasExpectContinueHeader)
                    {
                        await SendRequestContentAsync(request, CreateRequestContentStream(request), async, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        // We're sending an Expect: 100-continue header. We need to flush headers so that the server receives
                        // all of them, and we need to do so before initiating the send, as once we do that, it effectively
                        // owns the right to write, and we don't want to concurrently be accessing the write buffer.
                        await FlushAsync(async).ConfigureAwait(false);

                        // Create a TCS we'll use to block the request content from being sent, and create a timer that's used
                        // as a fail-safe to unblock the request content if we don't hear back from the server in a timely manner.
                        // Then kick off the request.  The TCS' result indicates whether content should be sent or not.
                        allowExpect100ToContinue = new TaskCompletionSource<bool>();
                        var expect100Timer = new Timer(
                            static s => ((TaskCompletionSource<bool>)s!).TrySetResult(true),
                            allowExpect100ToContinue, _pool.Settings._expect100ContinueTimeout, Timeout.InfiniteTimeSpan);
                        sendRequestContentTask = SendRequestContentWithExpect100ContinueAsync(
                            request, allowExpect100ToContinue.Task, CreateRequestContentStream(request), expect100Timer, async, cancellationToken);
                    }
                }

                // Start to read response.
                _allowedReadLineBytes = _pool.Settings.MaxResponseHeadersByteLength;

                // We should not have any buffered data here; if there was, it should have been treated as an error
                // by the previous request handling.  (Note we do not support HTTP pipelining.)
                Debug.Assert(_readBuffer.ActiveLength == 0);

                // When the connection was taken out of the pool, a pre-emptive read was performed
                // into the read buffer. We need to consume that read prior to issuing another read.
                if (ReadAheadTaskHasStarted)
                {
                    // If the read-ahead task completed synchronously, it would have claimed ownership of its completion,
                    // meaning that PrepareForReuse would have failed, and we wouldn't have called SendAsync.
                    // The task therefore shouldn't be 'default', as it's representing an async operation that had to yield at some point.
                    Debug.Assert(_readAheadTask != default);
                    Debug.Assert(_readAheadTaskStatus == ReadAheadTask_CompletionReserved);

                    // Handle the pre-emptive read.  For the async==false case, hopefully the read has
                    // already completed and this will be a nop, but if it hasn't, the caller will be forced to block
                    // waiting for the async operation to complete.  We will only hit this case for proxied HTTPS
                    // requests that use a pooled connection, as in that case we don't have a Socket we
                    // can poll and are forced to issue an async read.
                    ValueTask<int> vt = _readAheadTask;
                    _readAheadTask = default;

                    int bytesRead;
                    if (vt.IsCompleted)
                    {
                        bytesRead = vt.Result;
                    }
                    else
                    {
                        if (NetEventSource.Log.IsEnabled() && !async)
                        {
                            Trace($"Pre-emptive read completed asynchronously for a synchronous request.");
                        }

                        bytesRead = await vt.ConfigureAwait(false);
                    }

                    _readBuffer.Commit(bytesRead);

                    if (NetEventSource.Log.IsEnabled()) Trace($"Received {bytesRead} bytes.");

                    _readAheadTaskStatus = ReadAheadTask_NotStarted;
                }
                else
                {
                    // No read-ahead, so issue a read ourselves. We will check below for EOF.
                    await InitialFillAsync(async).ConfigureAwait(false);
                }

                if (_readBuffer.ActiveLength == 0)
                {
                    // The server shutdown the connection on their end, likely because of an idle timeout.
                    // If we haven't started sending the request body yet (or there is no request body),
                    // then we allow the request to be retried.
                    if (request.Content is null || allowExpect100ToContinue is not null)
                    {
                        _canRetry = true;
                    }

                    throw new HttpIOException(HttpRequestError.ResponseEnded, SR.net_http_invalid_response_premature_eof);
                }


                // Parse the response status line.
                var response = new HttpResponseMessage() { RequestMessage = request, Content = new HttpConnectionResponseContent() };

                while (!ParseStatusLine(response))
                {
                    await FillForHeadersAsync(async).ConfigureAwait(false);
                }

                if (HttpTelemetry.Log.IsEnabled()) HttpTelemetry.Log.ResponseHeadersStart();

                // Multiple 1xx responses handling.
                // RFC 7231: A client MUST be able to parse one or more 1xx responses received prior to a final response,
                // even if the client does not expect one. A user agent MAY ignore unexpected 1xx responses.
                // In .NET Core, apart from 100 Continue, and 101 Switching Protocols, we will treat all other 1xx responses
                // as unknown, and will discard them.
                while ((uint)(response.StatusCode - 100) <= 199 - 100)
                {
                    // If other 1xx responses come before an expected 100 continue, we will wait for the 100 response before
                    // sending request body (if any).
                    if (allowExpect100ToContinue != null && response.StatusCode == HttpStatusCode.Continue)
                    {
                        allowExpect100ToContinue.TrySetResult(true);
                        allowExpect100ToContinue = null;
                    }
                    else if (response.StatusCode == HttpStatusCode.SwitchingProtocols)
                    {
                        // 101 Upgrade is a final response as it's used to switch protocols with WebSockets handshake.
                        // Will return a response object with status 101 and a raw connection stream later.
                        // RFC 7230: If a server receives both an Upgrade and an Expect header field with the "100-continue" expectation,
                        // the server MUST send a 100 (Continue) response before sending a 101 (Switching Protocols) response.
                        // If server doesn't follow RFC, we treat 101 as a final response and stop waiting for 100 continue - as if server
                        // never sends a 100-continue. The request body will be sent after expect100Timer expires.
                        break;
                    }

                    // In case read hangs which eventually leads to connection timeout.
                    if (NetEventSource.Log.IsEnabled()) Trace($"Current {response.StatusCode} response is an interim response or not expected, need to read for a final response.");

                    // Discard headers that come with the interim 1xx responses.
                    while (!ParseHeaders(response: null, isFromTrailer: false))
                    {
                        await FillForHeadersAsync(async).ConfigureAwait(false);
                    }

                    // Parse the status line for next response.
                    while (!ParseStatusLine(response))
                    {
                        await FillForHeadersAsync(async).ConfigureAwait(false);
                    }
                }

                // Parse the response headers.  Logic after this point depends on being able to examine headers in the response object.
                while (!ParseHeaders(response, isFromTrailer: false))
                {
                    await FillForHeadersAsync(async).ConfigureAwait(false);
                }

                if (HttpTelemetry.Log.IsEnabled()) HttpTelemetry.Log.ResponseHeadersStop((int)response.StatusCode);

                if (allowExpect100ToContinue != null)
                {
                    // If we sent an Expect: 100-continue header, and didn't receive a 100-continue. Handle the final response accordingly.
                    // Note that the developer may have added an Expect: 100-continue header even if there is no Content.
                    if ((int)response.StatusCode >= 300 &&
                        request.Content != null &&
                        (request.Content.Headers.ContentLength == null || request.Content.Headers.ContentLength.GetValueOrDefault() > Expect100ErrorSendThreshold) &&
                        !AuthenticationHelper.IsSessionAuthenticationChallenge(response))
                    {
                        // For error final status codes, try to avoid sending the payload if its size is unknown or if it's known to be "big".
                        // If we already sent a header detailing the size of the payload, if we then don't send that payload, the server may wait
                        // for it and assume that the next request on the connection is actually this request's payload.  Thus we mark the connection
                        // to be closed.  However, we may have also lost a race condition with the Expect: 100-continue timeout, so if it turns out
                        // we've already started sending the payload (we weren't able to cancel it), then we don't need to force close the connection.
                        // We also must not clone connection if we do NTLM or Negotiate authentication.
                        allowExpect100ToContinue.TrySetResult(false);

                        if (!allowExpect100ToContinue.Task.Result) // if Result is true, the timeout already expired and we started sending content
                        {
                            _connectionClose = true;
                        }
                    }
                    else
                    {
                        // For any success status codes, for errors when the request content length is known to be small,
                        // or for session-based authentication challenges, send the payload
                        // (if there is one... if there isn't, Content is null and thus allowExpect100ToContinue is also null, we won't get here).
                        allowExpect100ToContinue.TrySetResult(true);
                    }
                }

                // Determine whether we need to force close the connection when the request/response has completed.
                if (response.Headers.ConnectionClose.GetValueOrDefault())
                {
                    _connectionClose = true;
                }

                // Now that we've received our final status line, wait for the request content to fully send.
                // In most common scenarios, the server won't send back a response until all of the request
                // content has been received, so this task should generally already be complete.
                if (sendRequestContentTask != null)
                {
                    Task sendTask = sendRequestContentTask;
                    sendRequestContentTask = null;
                    await sendTask.ConfigureAwait(false);
                }

                // Now we are sure that the request was fully sent.
                if (NetEventSource.Log.IsEnabled()) Trace("Request is fully sent.");

                // We're about to create the response stream, at which point responsibility for canceling
                // the remainder of the response lies with the stream.  Thus we dispose of our registration
                // here (if an exception has occurred or does occur while creating/returning the stream,
                // we'll still dispose of it in the catch below as part of Dispose'ing the connection).
                cancellationRegistration.Dispose();
                CancellationHelper.ThrowIfCancellationRequested(cancellationToken); // in case cancellation may have disposed of the stream

                // Create the response stream.
                Stream responseStream;
                if (ReferenceEquals(normalizedMethod, HttpMethod.Head) || response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.NotModified)
                {
                    responseStream = EmptyReadStream.Instance;
                    CompleteResponse();
                }
                else if (ReferenceEquals(normalizedMethod, HttpMethod.Connect) && response.StatusCode == HttpStatusCode.OK)
                {
                    // Successful response to CONNECT does not have body.
                    // What ever comes next should be opaque.
                    responseStream = new RawConnectionStream(this);

                    // Don't put connection back to the pool if we upgraded to tunnel.
                    // We cannot use it for normal HTTP requests any more.
                    _connectionClose = true;

                    _pool.InvalidateHttp11Connection(this);
                    _detachedFromPool = true;
                }
                else if (response.StatusCode == HttpStatusCode.SwitchingProtocols)
                {
                    responseStream = new RawConnectionStream(this);

                    // Don't put connection back to the pool if we switched protocols.
                    // We cannot use it for normal HTTP requests any more.
                    _connectionClose = true;

                    _pool.InvalidateHttp11Connection(this);
                    _detachedFromPool = true;
                }
                else if (response.Headers.TransferEncodingChunked == true)
                {
                    responseStream = new ChunkedEncodingReadStream(this, response);
                }
                else if (response.Content.Headers.ContentLength != null)
                {
                    long contentLength = response.Content.Headers.ContentLength.GetValueOrDefault();
                    if (contentLength <= 0)
                    {
                        responseStream = EmptyReadStream.Instance;
                        CompleteResponse();
                    }
                    else
                    {
                        responseStream = new ContentLengthReadStream(this, (ulong)contentLength);
                    }
                }
                else
                {
                    responseStream = new ConnectionCloseReadStream(this);
                }
                ((HttpConnectionResponseContent)response.Content).SetStream(responseStream);

                if (NetEventSource.Log.IsEnabled()) Trace($"Received response: {response}");

                // Process Set-Cookie headers.
                if (_pool.Settings._useCookies)
                {
                    CookieHelper.ProcessReceivedCookies(response, _pool.Settings._cookieContainer!);
                }

                return response;
            }
            catch (Exception error)
            {
                // Clean up the cancellation registration in case we're still registered.
                cancellationRegistration.Dispose();

                // Make sure to complete the allowExpect100ToContinue task if it exists.
                if (allowExpect100ToContinue is not null && !allowExpect100ToContinue.TrySetResult(false))
                {
                    // allowExpect100ToContinue was already signaled and we may have started sending the request body.
                    _canRetry = false;
                }

                if (_readAheadTask != default)
                {
                    Debug.Assert(_readAheadTaskStatus == ReadAheadTask_CompletionReserved);

                    LogExceptions(_readAheadTask.AsTask());
                }

                if (NetEventSource.Log.IsEnabled()) Trace($"Error sending request: {error}");

                // In the rare case where Expect: 100-continue was used and then processing
                // of the response headers encountered an error such that we weren't able to
                // wait for the sending to complete, it's possible the sending also encountered
                // an exception or potentially is still going and will encounter an exception
                // (we're about to Dispose for the connection). In such cases, we don't want any
                // exception in that sending task to become unobserved and raise alarm bells, so we
                // hook up a continuation that will log it.
                if (sendRequestContentTask != null && !sendRequestContentTask.IsCompletedSuccessfully)
                {
                    // In case the connection is disposed, it's most probable that
                    // expect100Continue timer expired and request content sending failed.
                    // We're awaiting the task to propagate the exception in this case.
                    if (Volatile.Read(ref _disposed) == Status_Disposed)
                    {
                        try
                        {
                            await sendRequestContentTask.ConfigureAwait(false);
                        }
                        // Map the exception the same way as we normally do.
                        catch (Exception ex) when (MapSendException(ex, cancellationToken, out Exception mappedEx))
                        {
                            throw mappedEx;
                        }
                    }
                    LogExceptions(sendRequestContentTask);
                }

                // Now clean up the connection.
                Dispose();

                // At this point, we're going to throw an exception; we just need to
                // determine which exception to throw.
                if (MapSendException(error, cancellationToken, out Exception mappedException))
                {
                    throw mappedException;
                }
                // Otherwise, just allow the original exception to propagate.
                throw;
            }
        }

        private bool MapSendException(Exception exception, CancellationToken cancellationToken, out Exception mappedException)
        {
            if (CancellationHelper.ShouldWrapInOperationCanceledException(exception, cancellationToken))
            {
                // Cancellation was requested, so assume that the failure is due to
                // the cancellation request. This is a bit unorthodox, as usually we'd
                // prioritize a non-OperationCanceledException over a cancellation
                // request to avoid losing potentially pertinent information.  But given
                // the cancellation design where we tear down the underlying connection upon
                // a cancellation request, which can then result in a myriad of different
                // exceptions (argument exceptions, object disposed exceptions, socket exceptions,
                // etc.), as a middle ground we treat it as cancellation, but still propagate the
                // original information as the inner exception, for diagnostic purposes.
                mappedException = CancellationHelper.CreateOperationCanceledException(exception, cancellationToken);
                return true;
            }

            if (exception is InvalidOperationException)
            {
                // For consistency with other handlers we wrap the exception in an HttpRequestException.
                mappedException = new HttpRequestException(SR.net_http_client_execution_error, exception);
                return true;
            }

            if (exception is IOException ioe)
            {
                // For consistency with other handlers we wrap the exception in an HttpRequestException.
                // If the request is retryable, indicate that on the exception.
                HttpRequestError error = ioe is HttpIOException httpIoe ? httpIoe.HttpRequestError : HttpRequestError.Unknown;
                mappedException = new HttpRequestException(error, SR.net_http_client_execution_error, ioe, _canRetry ? RequestRetryType.RetryOnConnectionFailure : RequestRetryType.NoRetry);
                return true;
            }

            // Otherwise, just allow the original exception to propagate.
            mappedException = exception;
            return false;
        }

        private HttpContentWriteStream CreateRequestContentStream(HttpRequestMessage request)
        {
            Debug.Assert(request.Content is not null);
            bool requestTransferEncodingChunked = request.HasHeaders && request.Headers.TransferEncodingChunked == true;
            HttpContentWriteStream requestContentStream = requestTransferEncodingChunked ? (HttpContentWriteStream)
                new ChunkedEncodingWriteStream(this) :
                new ContentLengthWriteStream(this, request.Content.Headers.ContentLength.GetValueOrDefault());
            return requestContentStream;
        }

        private CancellationTokenRegistration RegisterCancellation(CancellationToken cancellationToken)
        {
            // Cancellation design:
            // - We register with the SendAsync CancellationToken for the duration of the SendAsync operation.
            // - We register with the Read/Write/CopyToAsync methods on the response stream for each such individual operation.
            // - The registration disposes of the connection, tearing it down and causing any pending operations to wake up.
            // - Because such a tear down can result in a variety of different exception types, we check for a cancellation
            //   request and prioritize that over other exceptions, wrapping the actual exception as an inner of an OCE.
            return cancellationToken.Register(static s =>
            {
                var connection = (HttpConnection)s!;
                if (NetEventSource.Log.IsEnabled()) connection.Trace("Cancellation requested. Disposing of the connection.");
                connection.Dispose();
            }, this);
        }

        private async ValueTask SendRequestContentAsync(HttpRequestMessage request, HttpContentWriteStream stream, bool async, CancellationToken cancellationToken)
        {
            Debug.Assert(stream.BytesWritten == 0);
            if (HttpTelemetry.Log.IsEnabled()) HttpTelemetry.Log.RequestContentStart();

            // Copy all of the data to the server.
            if (async)
            {
                await request.Content!.CopyToAsync(stream, _transportContext, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                request.Content!.CopyTo(stream, _transportContext, cancellationToken);
            }

            // Finish the content; with a chunked upload, this includes writing the terminating chunk.
            await stream.FinishAsync(async).ConfigureAwait(false);

            // Flush any content that might still be buffered.
            await FlushAsync(async).ConfigureAwait(false);

            if (HttpTelemetry.Log.IsEnabled()) HttpTelemetry.Log.RequestContentStop(stream.BytesWritten);

            if (NetEventSource.Log.IsEnabled()) Trace("Finished sending request content.");
        }

        private async Task SendRequestContentWithExpect100ContinueAsync(
            HttpRequestMessage request, Task<bool> allowExpect100ToContinueTask,
            HttpContentWriteStream stream, Timer expect100Timer, bool async, CancellationToken cancellationToken)
        {
            // Wait until we receive a trigger notification that it's ok to continue sending content.
            // This will come either when the timer fires or when we receive a response status line from the server.
            bool sendRequestContent = await allowExpect100ToContinueTask.ConfigureAwait(false);

            // Clean up the timer; it's no longer needed.
            expect100Timer.Dispose();

            // Send the content if we're supposed to.  Otherwise, we're done.
            if (sendRequestContent)
            {
                if (NetEventSource.Log.IsEnabled()) Trace($"Sending request content for Expect: 100-continue.");
                try
                {
                    await SendRequestContentAsync(request, stream, async, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    // Tear down the connection if called from the timer thread because caller's thread will wait for server status line indefinitely
                    // or till HttpClient.Timeout tear the connection itself.
                    Dispose();
                    throw;
                }
            }
            else
            {
                if (NetEventSource.Log.IsEnabled()) Trace($"Canceling request content for Expect: 100-continue.");
            }
        }

        private bool ParseStatusLine(HttpResponseMessage response)
        {
            Span<byte> buffer = _readBuffer.ActiveSpan;

            int lineFeedIndex = buffer.IndexOf((byte)'\n');
            if (lineFeedIndex >= 0)
            {
                int bytesConsumed = lineFeedIndex + 1;
                _readBuffer.Discard(bytesConsumed);
                _allowedReadLineBytes -= bytesConsumed;

                int carriageReturnIndex = lineFeedIndex - 1;
                int length = (uint)carriageReturnIndex < (uint)buffer.Length && buffer[carriageReturnIndex] == '\r'
                    ? carriageReturnIndex
                    : lineFeedIndex;

                ParseStatusLineCore(buffer.Slice(0, length), response);
                return true;
            }
            else
            {
                if (_allowedReadLineBytes <= buffer.Length)
                {
                    ThrowExceededAllowedReadLineBytes();
                }
                return false;
            }
        }

        private static void ParseStatusLineCore(Span<byte> line, HttpResponseMessage response)
        {
            // We sent the request version as either 1.0 or 1.1.
            // We expect a response version of the form 1.X, where X is a single digit as per RFC.

            // Validate the beginning of the status line and set the response version.
            const int MinStatusLineLength = 12; // "HTTP/1.x 123"
            if (line.Length < MinStatusLineLength || line[8] != ' ')
            {
                throw new HttpRequestException(HttpRequestError.InvalidResponse, SR.Format(SR.net_http_invalid_response_status_line, Encoding.ASCII.GetString(line)));
            }

            ulong first8Bytes = BitConverter.ToUInt64(line);
            if (first8Bytes == s_http11Bytes)
            {
                response.SetVersionWithoutValidation(HttpVersion.Version11);
            }
            else if (first8Bytes == s_http10Bytes)
            {
                response.SetVersionWithoutValidation(HttpVersion.Version10);
            }
            else
            {
                byte minorVersion = line[7];
                if (IsDigit(minorVersion) && line.StartsWith("HTTP/1."u8))
                {
                    response.SetVersionWithoutValidation(new Version(1, minorVersion - '0'));
                }
                else
                {
                    throw new HttpRequestException(HttpRequestError.InvalidResponse, SR.Format(SR.net_http_invalid_response_status_line, Encoding.ASCII.GetString(line)));
                }
            }

            // Set the status code
            byte status1 = line[9], status2 = line[10], status3 = line[11];
            if (!IsDigit(status1) || !IsDigit(status2) || !IsDigit(status3))
            {
                throw new HttpRequestException(HttpRequestError.InvalidResponse, SR.Format(SR.net_http_invalid_response_status_code, Encoding.ASCII.GetString(line.Slice(9, 3))));
            }
            response.SetStatusCodeWithoutValidation((HttpStatusCode)(100 * (status1 - '0') + 10 * (status2 - '0') + (status3 - '0')));

            // Parse (optional) reason phrase
            if (line.Length == MinStatusLineLength)
            {
                response.SetReasonPhraseWithoutValidation(string.Empty);
            }
            else if (line[MinStatusLineLength] == ' ')
            {
                ReadOnlySpan<byte> reasonBytes = line.Slice(MinStatusLineLength + 1);
                string? knownReasonPhrase = HttpStatusDescription.Get(response.StatusCode);
                if (knownReasonPhrase != null && Ascii.Equals(reasonBytes, knownReasonPhrase))
                {
                    response.SetReasonPhraseWithoutValidation(knownReasonPhrase);
                }
                else
                {
                    try
                    {
                        response.ReasonPhrase = HttpRuleParser.DefaultHttpEncoding.GetString(reasonBytes);
                    }
                    catch (FormatException formatEx)
                    {
                        throw new HttpRequestException(HttpRequestError.InvalidResponse, SR.Format(SR.net_http_invalid_response_status_reason, Encoding.ASCII.GetString(reasonBytes.ToArray())), formatEx);
                    }
                }
            }
            else
            {
                throw new HttpRequestException(HttpRequestError.InvalidResponse, SR.Format(SR.net_http_invalid_response_status_line, Encoding.ASCII.GetString(line)));
            }
        }

        private bool ParseHeaders(HttpResponseMessage? response, bool isFromTrailer)
        {
            Span<byte> buffer = _readBuffer.ActiveSpan;

            (bool finished, int bytesConsumed) = ParseHeadersCore(buffer, response, isFromTrailer);

            int bytesScanned = finished ? bytesConsumed : buffer.Length;
            if (_allowedReadLineBytes < bytesScanned)
            {
                ThrowExceededAllowedReadLineBytes();
            }

            _readBuffer.Discard(bytesConsumed);
            _allowedReadLineBytes -= bytesConsumed;
            Debug.Assert(_allowedReadLineBytes >= 0);

            return finished;
        }

        private (bool finished, int bytesConsumed) ParseHeadersCore(Span<byte> buffer, HttpResponseMessage? response, bool isFromTrailer)
        {
            int originalBufferLength = buffer.Length;

            while (true)
            {
                int colIdx = buffer.IndexOfAny((byte)':', (byte)'\n');
                if (colIdx < 0)
                {
                    return (finished: false, bytesConsumed: originalBufferLength - buffer.Length);
                }

                if (buffer[colIdx] == '\n')
                {
                    if ((colIdx == 1 && buffer[0] == '\r') || colIdx == 0)
                    {
                        return (finished: true, bytesConsumed: originalBufferLength - buffer.Length + colIdx + 1);
                    }

                    ThrowForInvalidHeaderLine(buffer, colIdx);
                }

                int valueStartIdx = colIdx + 1;
                if ((uint)valueStartIdx >= (uint)buffer.Length)
                {
                    return (finished: false, bytesConsumed: originalBufferLength - buffer.Length);
                }

                // Iterate over the value and handle any line folds (new lines followed by SP/HTAB).
                // valueIterator refers to the remainder of the buffer that we can still scan for new lines.
                Span<byte> valueIterator = buffer.Slice(valueStartIdx);

                while (true)
                {
                    int lfIdx = valueIterator.IndexOf((byte)'\n');
                    if ((uint)lfIdx >= (uint)valueIterator.Length)
                    {
                        return (finished: false, bytesConsumed: originalBufferLength - buffer.Length);
                    }

                    int crIdx = lfIdx - 1;
                    int crOrLfIdx = (uint)crIdx < (uint)valueIterator.Length && valueIterator[crIdx] == '\r'
                        ? crIdx
                        : lfIdx;

                    int spIdx = lfIdx + 1;
                    if ((uint)spIdx >= (uint)valueIterator.Length)
                    {
                        return (finished: false, bytesConsumed: originalBufferLength - buffer.Length);
                    }

                    if (valueIterator[spIdx] is not (byte)'\t' and not (byte)' ')
                    {
                        // Found the end of the header value.

                        if (response is not null)
                        {
                            ReadOnlySpan<byte> headerName = buffer.Slice(0, valueStartIdx - 1);
                            ReadOnlySpan<byte> headerValue = buffer.Slice(valueStartIdx, buffer.Length - valueIterator.Length + crOrLfIdx - valueStartIdx);
                            AddResponseHeader(headerName, headerValue, response, isFromTrailer);
                        }

                        buffer = buffer.Slice(buffer.Length - valueIterator.Length + spIdx);
                        break;
                    }

                    // Found an obs-fold (CRLFHT/CRLFSP).
                    // Replace the CRLF with SPSP and keep looking for the final newline.
                    valueIterator[crOrLfIdx] = (byte)' ';
                    valueIterator[lfIdx] = (byte)' ';

                    valueIterator = valueIterator.Slice(spIdx + 1);
                }
            }

            static void ThrowForInvalidHeaderLine(ReadOnlySpan<byte> buffer, int newLineIndex) =>
                throw new HttpRequestException(HttpRequestError.InvalidResponse, SR.Format(SR.net_http_invalid_response_header_line, Encoding.ASCII.GetString(buffer.Slice(0, newLineIndex))));
        }

        private void AddResponseHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value, HttpResponseMessage response, bool isFromTrailer)
        {
            // Skip trailing whitespace and check for empty length.
            while (true)
            {
                int spIdx = name.Length - 1;

                if ((uint)spIdx < (uint)name.Length)
                {
                    if (name[spIdx] != ' ')
                    {
                        // hot path
                        break;
                    }

                    name = name.Slice(0, spIdx);
                }
                else
                {
                    ThrowForEmptyHeaderName();
                }
            }

            // Skip leading OWS for value.
            // hot path: loop body runs only once.
            while (value.Length != 0 && value[0] is (byte)' ' or (byte)'\t')
            {
                value = value.Slice(1);
            }

            // Skip trailing OWS for value.
            while (true)
            {
                int spIdx = value.Length - 1;

                if ((uint)spIdx >= (uint)value.Length || !(value[spIdx] is (byte)' ' or (byte)'\t'))
                {
                    // hot path
                    break;
                }

                value = value.Slice(0, spIdx);
            }

            if (!HeaderDescriptor.TryGet(name, out HeaderDescriptor descriptor))
            {
                ThrowForInvalidHeaderName(name);
            }

            Encoding? valueEncoding = _pool.Settings._responseHeaderEncodingSelector?.Invoke(descriptor.Name, _currentRequest!);

            HttpHeaderType headerType = descriptor.HeaderType;

            // Request headers returned on the response must be treated as custom headers.
            if ((headerType & HttpHeaderType.Request) != 0)
            {
                descriptor = descriptor.AsCustomHeader();
            }

            string headerValue;
            HttpHeaders headers;

            if (isFromTrailer)
            {
                if ((headerType & HttpHeaderType.NonTrailing) != 0)
                {
                    // Disallowed trailer fields.
                    // A recipient MUST ignore fields that are forbidden to be sent in a trailer.
                    return;
                }

                headerValue = descriptor.GetHeaderValue(value, valueEncoding);
                headers = response.TrailingHeaders;
            }
            else if ((headerType & HttpHeaderType.Content) != 0)
            {
                headerValue = descriptor.GetHeaderValue(value, valueEncoding);
                headers = response.Content!.Headers;
            }
            else
            {
                headerValue = GetResponseHeaderValueWithCaching(descriptor, value, valueEncoding);
                headers = response.Headers;

                if (descriptor.Equals(KnownHeaders.KeepAlive))
                {
                    // We are intentionally going against RFC to honor the Keep-Alive header even if
                    // we haven't received a Keep-Alive connection token to maximize compat with servers.
                    ProcessKeepAliveHeader(headerValue);
                }
            }

            bool added = headers.TryAddWithoutValidation(descriptor, headerValue);
            Debug.Assert(added);

            static void ThrowForEmptyHeaderName() =>
                throw new HttpRequestException(HttpRequestError.InvalidResponse, SR.Format(SR.net_http_invalid_response_header_name, ""));

            static void ThrowForInvalidHeaderName(ReadOnlySpan<byte> name) =>
                throw new HttpRequestException(HttpRequestError.InvalidResponse, SR.Format(SR.net_http_invalid_response_header_name, Encoding.ASCII.GetString(name)));
        }

        private void ThrowExceededAllowedReadLineBytes() =>
            throw new HttpRequestException(HttpRequestError.ConfigurationLimitExceeded, SR.Format(SR.net_http_response_headers_exceeded_length, _pool.Settings.MaxResponseHeadersByteLength));

        private void ProcessKeepAliveHeader(string keepAlive)
        {
            var parsedValues = new UnvalidatedObjectCollection<NameValueHeaderValue>();

            if (NameValueHeaderValue.GetNameValueListLength(keepAlive, 0, ',', parsedValues) == keepAlive.Length)
            {
                foreach (NameValueHeaderValue nameValue in parsedValues)
                {
                    // The HTTP/1.1 spec does not define any parameters for the Keep-Alive header, so we are using the de facto standard ones - timeout and max.
                    if (string.Equals(nameValue.Name, "timeout", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrEmpty(nameValue.Value) &&
                            HeaderUtilities.TryParseInt32(nameValue.Value, out int timeout) &&
                            timeout >= 0)
                        {
                            // Some servers are very strict with closing the connection exactly at the timeout.
                            // Avoid using the connection if it is about to exceed the timeout to avoid resulting request failures.
                            const int OffsetSeconds = 1;

                            if (timeout <= OffsetSeconds)
                            {
                                _connectionClose = true;
                            }
                            else
                            {
                                _keepAliveTimeoutSeconds = timeout - OffsetSeconds;
                            }
                        }
                    }
                    else if (string.Equals(nameValue.Name, "max", StringComparison.OrdinalIgnoreCase))
                    {
                        if (nameValue.Value == "0")
                        {
                            _connectionClose = true;
                        }
                    }
                }
            }
        }

        private void WriteToBuffer(ReadOnlySpan<byte> source)
        {
            Debug.Assert(source.Length <= _writeBuffer.AvailableLength);
            source.CopyTo(_writeBuffer.AvailableSpan);
            _writeBuffer.Commit(source.Length);
        }

        private void Write(ReadOnlySpan<byte> source)
        {
            int remaining = _writeBuffer.AvailableLength;

            if (source.Length <= remaining)
            {
                // Fits in current write buffer.  Just copy and return.
                WriteToBuffer(source);
                return;
            }

            if (_writeBuffer.ActiveLength != 0)
            {
                // Fit what we can in the current write buffer and flush it.
                WriteToBuffer(source.Slice(0, remaining));
                source = source.Slice(remaining);
                Flush();
            }

            if (source.Length >= _writeBuffer.Capacity)
            {
                // Large write.  No sense buffering this.  Write directly to stream.
                WriteToStream(source);
            }
            else
            {
                // Copy remainder into buffer
                WriteToBuffer(source);
            }
        }

        private ValueTask WriteAsync(ReadOnlyMemory<byte> source)
        {
            int remaining = _writeBuffer.AvailableLength;

            if (source.Length <= remaining)
            {
                // Fits in current write buffer.  Just copy and return.
                WriteToBuffer(source.Span);
                return default;
            }

            if (_writeBuffer.ActiveLength != 0)
            {
                // Fit what we can in the current write buffer and flush it.
                WriteToBuffer(source.Span.Slice(0, remaining));
                source = source.Slice(remaining);

                ValueTask flushTask = FlushAsync(async: true);

                if (flushTask.IsCompletedSuccessfully)
                {
                    flushTask.GetAwaiter().GetResult();

                    if (source.Length <= _writeBuffer.Capacity)
                    {
                        WriteToBuffer(source.Span);
                        return default;
                    }

                    // Fall-through to WriteToStreamAsync
                }
                else
                {
                    return AwaitFlushAndWriteAsync(flushTask, source);
                }
            }

            // Large write.  No sense buffering this.  Write directly to stream.
            return WriteToStreamAsync(source, async: true);

            async ValueTask AwaitFlushAndWriteAsync(ValueTask flushTask, ReadOnlyMemory<byte> source)
            {
                await flushTask.ConfigureAwait(false);

                if (source.Length <= _writeBuffer.Capacity)
                {
                    WriteToBuffer(source.Span);
                }
                else
                {
                    await WriteToStreamAsync(source, async: true).ConfigureAwait(false);
                }
            }
        }

        private void WriteWithoutBuffering(ReadOnlySpan<byte> source)
        {
            if (_writeBuffer.ActiveLength != 0)
            {
                if (source.Length <= _writeBuffer.AvailableLength)
                {
                    // There's something already in the write buffer, but the content
                    // we're writing can also fit after it in the write buffer.  Copy
                    // the content to the write buffer and then flush it, so that we
                    // can do a single send rather than two.
                    WriteToBuffer(source);
                    Flush();
                    return;
                }

                // There's data in the write buffer and the data we're writing doesn't fit after it.
                // Do two writes, one to flush the buffer and then another to write the supplied content.
                Flush();
            }

            WriteToStream(source);
        }

        private ValueTask WriteWithoutBufferingAsync(ReadOnlyMemory<byte> source, bool async)
        {
            if (_writeBuffer.ActiveLength == 0)
            {
                // There's nothing in the write buffer we need to flush.
                // Just write the supplied data out to the stream.
                return WriteToStreamAsync(source, async);
            }

            if (source.Length <= _writeBuffer.AvailableLength)
            {
                // There's something already in the write buffer, but the content
                // we're writing can also fit after it in the write buffer.  Copy
                // the content to the write buffer and then flush it, so that we
                // can do a single send rather than two.
                WriteToBuffer(source.Span);
                return FlushAsync(async);
            }

            // There's data in the write buffer and the data we're writing doesn't fit after it.
            // Do two writes, one to flush the buffer and then another to write the supplied content.
            return FlushThenWriteWithoutBufferingAsync(source, async);
        }

        private async ValueTask FlushThenWriteWithoutBufferingAsync(ReadOnlyMemory<byte> source, bool async)
        {
            await FlushAsync(async).ConfigureAwait(false);
            await WriteToStreamAsync(source, async).ConfigureAwait(false);
        }

        private ValueTask WriteHexInt32Async(int value, bool async)
        {
            // Try to format into our output buffer directly.
            if (value.TryFormat(_writeBuffer.AvailableSpan, out int bytesWritten, "X"))
            {
                _writeBuffer.Commit(bytesWritten);
                return default;
            }

            // If we don't have enough room, do it the slow way.
            if (async)
            {
                Span<byte> temp = stackalloc byte[8]; // max length of Int32 as hex
                bool formatted = value.TryFormat(temp, out bytesWritten, "X");
                Debug.Assert(formatted);
                return WriteAsync(temp.Slice(0, bytesWritten).ToArray());
            }
            else
            {
                // We should have enough capacity to write any hex-encoded int after flushing the buffer.
                Debug.Assert(_writeBuffer.Capacity >= 8);

                Flush();
                return WriteHexInt32Async(value, async: false);
            }
        }

        private void Flush()
        {
            ReadOnlySpan<byte> bytes = _writeBuffer.ActiveSpan;
            if (bytes.Length > 0)
            {
                _writeBuffer.Discard(bytes.Length);
                WriteToStream(bytes);
            }
        }

        private ValueTask FlushAsync(bool async)
        {
            ReadOnlyMemory<byte> bytes = _writeBuffer.ActiveMemory;
            if (bytes.Length > 0)
            {
                _writeBuffer.Discard(bytes.Length);
                return WriteToStreamAsync(bytes, async);
            }
            return default;
        }

        private void WriteToStream(ReadOnlySpan<byte> source)
        {
            if (NetEventSource.Log.IsEnabled()) Trace($"Writing {source.Length} bytes.");
            _stream.Write(source);
        }

        private ValueTask WriteToStreamAsync(ReadOnlyMemory<byte> source, bool async)
        {
            if (NetEventSource.Log.IsEnabled()) Trace($"Writing {source.Length} bytes.");

            if (async)
            {
                return _stream.WriteAsync(source);
            }
            else
            {
                _stream.Write(source.Span);
                return default;
            }
        }

        private bool TryReadNextChunkedLine(out ReadOnlySpan<byte> line)
        {
            ReadOnlySpan<byte> buffer = _readBuffer.ActiveReadOnlySpan;

            int lineFeedIndex = buffer.IndexOf((byte)'\n');
            if (lineFeedIndex < 0)
            {
                if (buffer.Length < MaxChunkBytesAllowed)
                {
                    line = default;
                    return false;
                }
            }
            else
            {
                int bytesConsumed = lineFeedIndex + 1;
                if (bytesConsumed <= MaxChunkBytesAllowed)
                {
                    _readBuffer.Discard(bytesConsumed);

                    int carriageReturnIndex = lineFeedIndex - 1;

                    int length = (uint)carriageReturnIndex < (uint)buffer.Length && buffer[carriageReturnIndex] == '\r'
                        ? carriageReturnIndex
                        : lineFeedIndex;

                    line = buffer.Slice(0, length);
                    return true;
                }
            }

            throw new HttpRequestException(SR.net_http_chunk_too_large);
        }

        // Does not throw on EOF. Also assumes there is no buffered data.
        private async ValueTask InitialFillAsync(bool async)
        {
            Debug.Assert(!ReadAheadTaskHasStarted);
            Debug.Assert(_readBuffer.AvailableLength == _readBuffer.Capacity);
            Debug.Assert(_readBuffer.AvailableLength >= InitialReadBufferSize);

            int bytesRead = async ?
                await _stream.ReadAsync(_readBuffer.AvailableMemory).ConfigureAwait(false) :
                _stream.Read(_readBuffer.AvailableSpan);

            _readBuffer.Commit(bytesRead);

            if (NetEventSource.Log.IsEnabled()) Trace($"Received {bytesRead} bytes.");
        }

        // Throws IOException on EOF.  This is only called when we expect more data.
        private async ValueTask FillAsync(bool async)
        {
            Debug.Assert(_readAheadTask == default);

            _readBuffer.EnsureAvailableSpace(1);

            int bytesRead = async ?
                await _stream.ReadAsync(_readBuffer.AvailableMemory).ConfigureAwait(false) :
                _stream.Read(_readBuffer.AvailableSpan);

            _readBuffer.Commit(bytesRead);

            if (NetEventSource.Log.IsEnabled()) Trace($"Received {bytesRead} bytes.");
            if (bytesRead == 0)
            {
                throw new HttpIOException(HttpRequestError.ResponseEnded, SR.net_http_invalid_response_premature_eof);
            }
        }

        private ValueTask FillForHeadersAsync(bool async)
        {
            // If the start offset is 0, it means we haven't consumed any data since the last FillAsync.
            // If so, read until we either find the next new line or we hit the MaxResponseHeadersLength limit.
            return _readBuffer.ActiveStartOffset == 0
                ? ReadUntilEndOfHeaderAsync(async)
                : FillAsync(async);

            // This method guarantees that the next call to ParseHeaders will consume at least one header.
            // This is the slow path, but guarantees O(n) worst-case parsing complexity.
            async ValueTask ReadUntilEndOfHeaderAsync(bool async)
            {
                int searchOffset = _readBuffer.ActiveLength;
                if (searchOffset > 0)
                {
                    // The last character we've buffered could be a new line,
                    // we just haven't checked the byte following it to see if it's a space or tab.
                    searchOffset--;
                }

                while (true)
                {
                    await FillAsync(async).ConfigureAwait(false);
                    Debug.Assert(_readBuffer.ActiveStartOffset == 0);
                    Debug.Assert(_readBuffer.ActiveLength > searchOffset);

                    // There's no need to search the whole buffer, only look through the new bytes we just read.
                    if (TryFindEndOfLine(_readBuffer.ActiveReadOnlySpan.Slice(searchOffset), out int offset))
                    {
                        break;
                    }

                    searchOffset += offset;

                    int readLength = _readBuffer.ActiveLength;
                    if (searchOffset != readLength)
                    {
                        Debug.Assert(searchOffset == readLength - 1 && _readBuffer.ActiveReadOnlySpan[searchOffset] == '\n');
                        if (readLength <= 2)
                        {
                            // There are no headers - we start off with a new line.
                            // This is reachable from ChunkedEncodingReadStream if the buffers allign just right and there are no trailing headers.
                            break;
                        }
                    }

                    if (readLength >= _allowedReadLineBytes)
                    {
                        ThrowExceededAllowedReadLineBytes();
                    }
                }

                static bool TryFindEndOfLine(ReadOnlySpan<byte> buffer, out int searchOffset)
                {
                    Debug.Assert(buffer.Length > 0);

                    int originalBufferLength = buffer.Length;

                    while (true)
                    {
                        int newLineOffset = buffer.IndexOf((byte)'\n');
                        if (newLineOffset < 0)
                        {
                            searchOffset = originalBufferLength;
                            return false;
                        }

                        int tabOrSpaceIndex = newLineOffset + 1;
                        if (tabOrSpaceIndex == buffer.Length)
                        {
                            // The new line is the last character, read again to make sure it doesn't continue with space or tab.
                            searchOffset = originalBufferLength - 1;
                            return false;
                        }

                        if (buffer[tabOrSpaceIndex] is not (byte)'\t' and not (byte)' ')
                        {
                            searchOffset = 0;
                            return true;
                        }

                        buffer = buffer.Slice(tabOrSpaceIndex + 1);
                    }
                }
            }
        }

        private int ReadFromBuffer(Span<byte> buffer)
        {
            ReadOnlySpan<byte> available = _readBuffer.ActiveSpan;
            int toCopy = Math.Min(available.Length, buffer.Length);

            available.Slice(0, toCopy).CopyTo(buffer);
            _readBuffer.Discard(toCopy);

            return toCopy;
        }

        private int Read(Span<byte> destination)
        {
            // This is called when reading the response body.

            if (_readBuffer.ActiveLength > 0)
            {
                // We have data in the read buffer.  Return it to the caller.
                return ReadFromBuffer(destination);
            }

            // No data in read buffer.
            // Do an unbuffered read directly against the underlying stream.
            Debug.Assert(_readAheadTask == default, "Read ahead task should have been consumed as part of the headers.");
            int count = _stream.Read(destination);
            if (NetEventSource.Log.IsEnabled()) Trace($"Received {count} bytes.");
            return count;
        }

        private async ValueTask<int> ReadAsync(Memory<byte> destination)
        {
            // This is called when reading the response body.

            if (_readBuffer.ActiveLength > 0)
            {
                // We have data in the read buffer.  Return it to the caller.
                return ReadFromBuffer(destination.Span);
            }

            // No data in read buffer.
            // Do an unbuffered read directly against the underlying stream.
            Debug.Assert(_readAheadTask == default, "Read ahead task should have been consumed as part of the headers.");
            int count = await _stream.ReadAsync(destination).ConfigureAwait(false);
            if (NetEventSource.Log.IsEnabled()) Trace($"Received {count} bytes.");
            return count;
        }

        private int ReadBuffered(Span<byte> destination)
        {
            // This is called when reading the response body.

            if (_readBuffer.ActiveLength == 0)
            {
                // Do a buffered read directly against the underlying stream.
                Debug.Assert(_readAheadTask == default, "Read ahead task should have been consumed as part of the headers.");

                if (destination.Length == 0)
                {
                    return _stream.Read(Array.Empty<byte>());
                }

                Debug.Assert(_readBuffer.AvailableLength == _readBuffer.Capacity);
                int bytesRead = _stream.Read(_readBuffer.AvailableSpan);
                _readBuffer.Commit(bytesRead);

                if (NetEventSource.Log.IsEnabled()) Trace($"Received {bytesRead} bytes.");
            }

            // Hand back as much data as we can fit.
            return ReadFromBuffer(destination);
        }

        private ValueTask<int> ReadBufferedAsync(Memory<byte> destination)
        {
            // If the caller provided buffer, and thus the amount of data desired to be read,
            // is larger than the internal buffer, there's no point going through the internal
            // buffer, so just do an unbuffered read.
            // Also avoid avoid using the internal buffer if the user requested a zero-byte read to allow
            // underlying streams to efficiently handle such a read (e.g. SslStream defering buffer allocation).
            return destination.Length >= _readBuffer.Capacity || destination.Length == 0 ?
                ReadAsync(destination) :
                ReadBufferedAsyncCore(destination);
        }

        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
        private async ValueTask<int> ReadBufferedAsyncCore(Memory<byte> destination)
        {
            // This is called when reading the response body.

            if (_readBuffer.ActiveLength == 0)
            {
                // Do a buffered read directly against the underlying stream.
                Debug.Assert(_readAheadTask == default, "Read ahead task should have been consumed as part of the headers.");

                Debug.Assert(_readBuffer.AvailableLength == _readBuffer.Capacity);
                int bytesRead = await _stream.ReadAsync(_readBuffer.AvailableMemory).ConfigureAwait(false);
                _readBuffer.Commit(bytesRead);

                if (NetEventSource.Log.IsEnabled()) Trace($"Received {bytesRead} bytes.");
            }

            // Hand back as much data as we can fit.
            return ReadFromBuffer(destination.Span);
        }

        private ValueTask CopyFromBufferAsync(Stream destination, bool async, int count, CancellationToken cancellationToken)
        {
            Debug.Assert(count <= _readBuffer.ActiveLength);

            if (NetEventSource.Log.IsEnabled()) Trace($"Copying {count} bytes to stream.");

            ReadOnlyMemory<byte> source = _readBuffer.ActiveMemory.Slice(0, count);
            _readBuffer.Discard(count);

            if (async)
            {
                return destination.WriteAsync(source, cancellationToken);
            }
            else
            {
                destination.Write(source.Span);
                return default;
            }
        }

        private Task CopyToUntilEofAsync(Stream destination, bool async, int bufferSize, CancellationToken cancellationToken)
        {
            Debug.Assert(destination != null);

            if (_readBuffer.ActiveLength > 0)
            {
                return CopyToUntilEofWithExistingBufferedDataAsync(destination, async, bufferSize, cancellationToken);
            }

            if (async)
            {
                return _stream.CopyToAsync(destination, bufferSize, cancellationToken);
            }

            _stream.CopyTo(destination, bufferSize);
            return Task.CompletedTask;
        }

        private async Task CopyToUntilEofWithExistingBufferedDataAsync(Stream destination, bool async, int bufferSize, CancellationToken cancellationToken)
        {
            int remaining = _readBuffer.ActiveLength;
            Debug.Assert(remaining > 0);

            await CopyFromBufferAsync(destination, async, remaining, cancellationToken).ConfigureAwait(false);

            if (async)
            {
                await _stream.CopyToAsync(destination, bufferSize, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                _stream.CopyTo(destination, bufferSize);
            }
        }

        // Copy *exactly* [length] bytes into destination; throws on end of stream.
        private async Task CopyToContentLengthAsync(Stream destination, bool async, ulong length, int bufferSize, CancellationToken cancellationToken)
        {
            Debug.Assert(destination != null);
            Debug.Assert(length > 0);

            // Copy any data left in the connection's buffer to the destination.
            int remaining = _readBuffer.ActiveLength;
            if (remaining > 0)
            {
                if ((ulong)remaining > length)
                {
                    remaining = (int)length;
                }
                await CopyFromBufferAsync(destination, async, remaining, cancellationToken).ConfigureAwait(false);

                length -= (ulong)remaining;
                if (length == 0)
                {
                    return;
                }

                Debug.Assert(_readBuffer.ActiveLength == 0, "HttpConnection's buffer should have been empty.");
            }

            // Repeatedly read into HttpConnection's buffer and write that buffer to the destination
            // stream. If after doing so, we find that we filled the whole connection's buffer (which
            // is sized mainly for HTTP headers rather than large payloads), grow the connection's
            // read buffer to the requested buffer size to use for the remainder of the operation. We
            // use a temporary buffer from the ArrayPool so that the connection doesn't hog large
            // buffers from the pool for extended durations, especially if it's going to sit in the
            // connection pool for a prolonged period.
            byte[]? origReadBuffer = null;
            try
            {
                while (true)
                {
                    await FillAsync(async).ConfigureAwait(false);

                    remaining = (int)Math.Min((ulong)_readBuffer.ActiveLength, length);
                    await CopyFromBufferAsync(destination, async, remaining, cancellationToken).ConfigureAwait(false);

                    length -= (ulong)remaining;
                    if (length == 0)
                    {
                        return;
                    }

                    // If we haven't yet grown the buffer (if we previously grew it, then it's sufficiently large), and
                    // if we filled the read buffer while doing the last read (which is at least one indication that the
                    // data arrival rate is fast enough to warrant a larger buffer), and if the buffer size we'd want is
                    // larger than the one we already have, then grow the connection's read buffer to that size.
                    if (origReadBuffer is null)
                    {
                        int currentCapacity = _readBuffer.Capacity;
                        if (remaining == currentCapacity)
                        {
                            int desiredBufferSize = (int)Math.Min((ulong)bufferSize, length);
                            if (desiredBufferSize > currentCapacity)
                            {
                                origReadBuffer = _readBuffer.DangerousGetUnderlyingBuffer();
                                byte[] pooledBuffer = ArrayPool<byte>.Shared.Rent(desiredBufferSize);
                                _readBuffer = new ArrayBuffer(pooledBuffer);
                            }
                        }
                    }
                }
            }
            finally
            {
                if (origReadBuffer is not null)
                {
                    Debug.Assert(origReadBuffer.Length > 0);

                    // We don't care how much remaining data there was, just if there was any.
                    // Subsequent code is going to check whether the receive buffer is empty
                    // and then force the connection closed if it's not.
                    bool anyDataAvailable = _readBuffer.ActiveLength > 0;

                    byte[] pooledBuffer = _readBuffer.DangerousGetUnderlyingBuffer();
                    _readBuffer = new ArrayBuffer(origReadBuffer);
                    ArrayPool<byte>.Shared.Return(pooledBuffer);

                    if (anyDataAvailable)
                    {
                        _readBuffer.Commit(1);
                    }
                }
            }
        }

        internal void Acquire()
        {
            Debug.Assert(_currentRequest == null);
            Debug.Assert(!_inUse);

            _inUse = true;
        }

        internal void Release()
        {
            Debug.Assert(_inUse);

            _inUse = false;

            // If the last request already completed (because the response had no content), return the connection to the pool now.
            // Otherwise, it will be returned when the response has been consumed and CompleteResponse below is called.
            if (_currentRequest == null)
            {
                ReturnConnectionToPool();
            }
        }

        /// <summary>
        /// Detach the connection from the pool, so it is no longer counted against the connection limit.
        /// This is used when we are creating a replacement connection for NT auth challenges.
        /// </summary>
        internal void DetachFromPool()
        {
            Debug.Assert(_inUse);

            _detachedFromPool = true;
        }

        private void CompleteResponse()
        {
            Debug.Assert(_currentRequest != null, "Expected the connection to be associated with a request.");
            Debug.Assert(_writeBuffer.ActiveLength == 0, "Everything in write buffer should have been flushed.");

            // Disassociate the connection from a request.
            _currentRequest = null;

            // If we have extraneous data in the read buffer, don't reuse the connection;
            // otherwise we'd interpret this as part of the next response. Plus, we may
            // have been using a temporary buffer to read this erroneous data, and thus
            // may not even have it any more.
            if (_readBuffer.ActiveLength > 0)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    Trace("Unexpected data on connection after response read.");
                }

                _readBuffer.Discard(_readBuffer.ActiveLength);
                _connectionClose = true;
            }

            // If the connection is no longer in use (i.e. for NT authentication), then we can return it to the pool now.
            // Otherwise, it will be returned when the connection is no longer in use (i.e. Release above is called).
            if (!_inUse)
            {
                ReturnConnectionToPool();
            }
        }

        public async ValueTask DrainResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            Debug.Assert(_inUse);

            if (_connectionClose)
            {
                throw new HttpRequestException(HttpRequestError.UserAuthenticationError, SR.net_http_authconnectionfailure);
            }

            Debug.Assert(response.Content != null);
            Stream stream = response.Content.ReadAsStream(cancellationToken);
            HttpContentReadStream? responseStream = stream as HttpContentReadStream;

            Debug.Assert(responseStream != null || stream is EmptyReadStream);

            if (responseStream != null && responseStream.NeedsDrain)
            {
                Debug.Assert(response.RequestMessage == _currentRequest);

                if (!await responseStream.DrainAsync(_pool.Settings._maxResponseDrainSize).ConfigureAwait(false) ||
                    _connectionClose)       // Draining may have set this
                {
                    throw new HttpRequestException(HttpRequestError.UserAuthenticationError, SR.net_http_authconnectionfailure);
                }
            }

            Debug.Assert(_currentRequest == null);

            response.Dispose();
        }

        private void ReturnConnectionToPool()
        {
            Debug.Assert(_currentRequest == null, "Connection should no longer be associated with a request.");
            Debug.Assert(_readAheadTask == default, "Expected a previous initial read to already be consumed.");
            Debug.Assert(_readAheadTaskStatus == ReadAheadTask_NotStarted, "Expected SendAsync to reset the read-ahead task status.");
            Debug.Assert(_readBuffer.ActiveLength == 0, "Unexpected data in connection read buffer.");

            // If we decided not to reuse the connection (either because the server sent Connection: close,
            // or there was some other problem while processing the request that makes the connection unusable),
            // don't put the connection back in the pool.
            if (_connectionClose)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    Trace("Connection will not be reused.");
                }

                // We're not putting the connection back in the pool. Dispose it.
                Dispose();
            }
            else
            {
                Debug.Assert(!_detachedFromPool, "Should not be detached from pool unless _connectionClose is true");

                // Put connection back in the pool.
                _pool.RecycleHttp11Connection(this);
            }
        }

        public sealed override string ToString() => $"{nameof(HttpConnection)}({_pool})"; // Description for diagnostic purposes

        public sealed override void Trace(string message, [CallerMemberName] string? memberName = null) =>
            NetEventSource.Log.HandlerMessage(
                _pool?.GetHashCode() ?? 0,           // pool ID
                GetHashCode(),                       // connection ID
                _currentRequest?.GetHashCode() ?? 0, // request ID
                memberName,                          // method name
                message);                            // message
    }
}
