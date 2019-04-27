// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal partial class HttpConnection : HttpConnectionBase, IDisposable
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

        private static readonly byte[] s_contentLength0NewlineAsciiBytes = Encoding.ASCII.GetBytes("Content-Length: 0\r\n");
        private static readonly byte[] s_spaceHttp10NewlineAsciiBytes = Encoding.ASCII.GetBytes(" HTTP/1.0\r\n");
        private static readonly byte[] s_spaceHttp11NewlineAsciiBytes = Encoding.ASCII.GetBytes(" HTTP/1.1\r\n");
        private static readonly byte[] s_httpSchemeAndDelimiter = Encoding.ASCII.GetBytes(Uri.UriSchemeHttp + Uri.SchemeDelimiter);
        private static readonly byte[] s_http1DotBytes = Encoding.ASCII.GetBytes("HTTP/1.");
        private static readonly ulong s_http10Bytes = BitConverter.ToUInt64(Encoding.ASCII.GetBytes("HTTP/1.0"));
        private static readonly ulong s_http11Bytes = BitConverter.ToUInt64(Encoding.ASCII.GetBytes("HTTP/1.1"));
        private static readonly HashSet<KnownHeader> s_disallowedTrailers = new HashSet<KnownHeader>    // rfc7230 4.1.2.
        {
            // Message framing headers.
            KnownHeaders.TransferEncoding, KnownHeaders.ContentLength,

            // Routing headers.
            KnownHeaders.Host,

            // Request modifiers: controls and conditionals.
            // rfc7231#section-5.1: Controls.
            KnownHeaders.CacheControl, KnownHeaders.Expect, KnownHeaders.MaxForwards, KnownHeaders.Pragma, KnownHeaders.Range, KnownHeaders.TE,

            // rfc7231#section-5.2: Conditionals.
            KnownHeaders.IfMatch, KnownHeaders.IfNoneMatch, KnownHeaders.IfModifiedSince, KnownHeaders.IfUnmodifiedSince, KnownHeaders.IfRange,

            // Authentication headers.
            KnownHeaders.Authorization, KnownHeaders.SetCookie,

            // Response control data.
            // rfc7231#section-7.1: Control Data.
            KnownHeaders.Age, KnownHeaders.Expires, KnownHeaders.Date, KnownHeaders.Location, KnownHeaders.RetryAfter, KnownHeaders.Vary, KnownHeaders.Warning,

            // Content-Encoding, Content-Type, Content-Range, and Trailer itself.
            KnownHeaders.ContentEncoding, KnownHeaders.ContentType, KnownHeaders.ContentRange, KnownHeaders.Trailer
        };

        private readonly HttpConnectionPool _pool;
        private readonly Socket _socket; // used for polling; _stream should be used for all reading/writing. _stream owns disposal.
        private readonly Stream _stream;
        private readonly TransportContext _transportContext;
        private readonly WeakReference<HttpConnection> _weakThisRef;

        private HttpRequestMessage _currentRequest;
        private readonly byte[] _writeBuffer;
        private int _writeOffset;
        private int _allowedReadLineBytes;

        private ValueTask<int>? _readAheadTask;
        private int _readAheadTaskLock = 0; // 0 == free, 1 == held
        private byte[] _readBuffer;
        private int _readOffset;
        private int _readLength;

        private bool _inUse;
        private bool _canRetry;
        private bool _connectionClose; // Connection: close was seen on last response
        private int _disposed; // 1 yes, 0 no

        public HttpConnection(
            HttpConnectionPool pool,
            Socket socket,
            Stream stream,
            TransportContext transportContext)
        {
            Debug.Assert(pool != null);
            Debug.Assert(stream != null);

            _pool = pool;
            _socket = socket; // may be null in cases where we couldn't easily get the underlying socket
            _stream = stream;
            _transportContext = transportContext;

            _writeBuffer = new byte[InitialWriteBufferSize];
            _readBuffer = new byte[InitialReadBufferSize];

            _weakThisRef = new WeakReference<HttpConnection>(this);

            if (NetEventSource.IsEnabled)
            {
                if (pool.IsSecure)
                {
                    var sslStream = (SslStream)_stream;
                    Trace(
                        $"Secure connection created to {pool}. " +
                        $"SslProtocol:{sslStream.SslProtocol}, " +
                        $"CipherAlgorithm:{sslStream.CipherAlgorithm}, CipherStrength:{sslStream.CipherStrength}, " +
                        $"HashAlgorithm:{sslStream.HashAlgorithm}, HashStrength:{sslStream.HashStrength}, " +
                        $"KeyExchangeAlgorithm:{sslStream.KeyExchangeAlgorithm}, KeyExchangeStrength:{sslStream.KeyExchangeStrength}, " +
                        $"LocalCert:{sslStream.LocalCertificate}, RemoteCert:{sslStream.RemoteCertificate}");
                }
                else
                {
                    Trace($"Connection created to {pool}.");
                }
            }
        }

        public void Dispose() => Dispose(disposing: true);

        protected void Dispose(bool disposing)
        {
            // Ensure we're only disposed once.  Dispose could be called concurrently, for example,
            // if the request and the response were running concurrently and both incurred an exception.
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                if (NetEventSource.IsEnabled) Trace("Connection closing.");
                _pool.DecrementConnectionCount();
                if (disposing)
                {
                    GC.SuppressFinalize(this);
                    _stream.Dispose();

                    // Eat any exceptions from the read-ahead task.  We don't need to log, as we expect
                    // failures from this task due to closing the connection while a read is in progress.
                    ValueTask<int>? readAheadTask = ConsumeReadAheadTask();
                    if (readAheadTask != null)
                    {
                        IgnoreExceptionsAsync(readAheadTask.GetValueOrDefault());
                    }
                }
            }
        }

        /// <summary>Awaits a task, ignoring any resulting exceptions.</summary>
        private static async void IgnoreExceptionsAsync(ValueTask<int> task)
        {
            try { await task.ConfigureAwait(false); } catch { }
        }

        /// <summary>Do a non-blocking poll to see whether the connection has data available or has been closed.</summary>
        /// <remarks>If we don't have direct access to the underlying socket, we instead use a read-ahead task.</remarks>
        public bool PollRead()
        {
            if (_socket != null) // may be null if we don't have direct access to the socket
            {
                try
                {
                    return _socket.Poll(0, SelectMode.SelectRead);
                }
                catch (Exception e) when (e is SocketException || e is ObjectDisposedException)
                {
                    // Poll can throw when used on a closed socket.
                    return true;
                }
            }
            else
            {
                return EnsureReadAheadAndPollRead();
            }
        }

        /// <summary>
        /// Issues a read-ahead on the connection, which will serve both as the first read on the
        /// response as well as a polling indication of whether the connection is usable.
        /// </summary>
        /// <returns>true if there's data available on the connection or it's been closed; otherwise, false.</returns>
        public bool EnsureReadAheadAndPollRead()
        {
            try
            {
                Debug.Assert(_readAheadTask == null || _socket == null, "Should only already have a read-ahead task if we don't have a socket to poll");
                if (_readAheadTask == null)
                {
                    _readAheadTask = _stream.ReadAsync(new Memory<byte>(_readBuffer));
                }
            }
            catch (Exception error)
            {
                // If reading throws, eat the error and don't pool the connection.
                if (NetEventSource.IsEnabled) Trace($"Error performing read ahead: {error}");
                Dispose();
                _readAheadTask = new ValueTask<int>(0);
            }

            return _readAheadTask.Value.IsCompleted; // equivalent to polling
        }

        private ValueTask<int>? ConsumeReadAheadTask()
        {
            if (Interlocked.CompareExchange(ref _readAheadTaskLock, 1, 0) == 0)
            {
                ValueTask<int>? t = _readAheadTask;
                _readAheadTask = null;
                Volatile.Write(ref _readAheadTaskLock, 0);
                return t;
            }

            // We couldn't get the lock, which means it must already be held
            // by someone else who will consume the task.
            return null;
        }

        public TransportContext TransportContext => _transportContext;

        public HttpConnectionKind Kind => _pool.Kind;

        private int ReadBufferSize => _readBuffer.Length;

        private ReadOnlyMemory<byte> RemainingBuffer => new ReadOnlyMemory<byte>(_readBuffer, _readOffset, _readLength - _readOffset);

        private void ConsumeFromRemainingBuffer(int bytesToConsume)
        {
            Debug.Assert(bytesToConsume <= _readLength - _readOffset, $"{bytesToConsume} > {_readLength} - {_readOffset}");
            _readOffset += bytesToConsume;
        }

        private async Task WriteHeadersAsync(HttpHeaders headers, string cookiesFromContainer)
        {
            foreach (KeyValuePair<HeaderDescriptor, string[]> header in headers.GetHeaderDescriptorsAndValues())
            {
                if (header.Key.KnownHeader != null)
                {
                    await WriteBytesAsync(header.Key.KnownHeader.AsciiBytesWithColonSpace).ConfigureAwait(false);
                }
                else
                {
                    await WriteAsciiStringAsync(header.Key.Name).ConfigureAwait(false);
                    await WriteTwoBytesAsync((byte)':', (byte)' ').ConfigureAwait(false);
                }

                Debug.Assert(header.Value.Length > 0, "No values for header??");
                if (header.Value.Length > 0)
                {
                    await WriteStringAsync(header.Value[0]).ConfigureAwait(false);

                    if (cookiesFromContainer != null && header.Key.KnownHeader == KnownHeaders.Cookie)
                    {
                        await WriteTwoBytesAsync((byte)';', (byte)' ').ConfigureAwait(false);
                        await WriteStringAsync(cookiesFromContainer).ConfigureAwait(false);

                        cookiesFromContainer = null;
                    }

                    // Some headers such as User-Agent and Server use space as a separator (see: ProductInfoHeaderParser)
                    if (header.Value.Length > 1)
                    {
                        HttpHeaderParser parser = header.Key.Parser;
                        string separator = HttpHeaderParser.DefaultSeparator;
                        if (parser != null && parser.SupportsMultipleValues)
                        {
                            separator = parser.Separator;
                        }

                        for (int i = 1; i < header.Value.Length; i++)
                        {
                            await WriteAsciiStringAsync(separator).ConfigureAwait(false);
                            await WriteStringAsync(header.Value[i]).ConfigureAwait(false);
                        }
                    }
                }

                await WriteTwoBytesAsync((byte)'\r', (byte)'\n').ConfigureAwait(false);
            }

            if (cookiesFromContainer != null)
            {
                await WriteAsciiStringAsync(HttpKnownHeaderNames.Cookie).ConfigureAwait(false);
                await WriteTwoBytesAsync((byte)':', (byte)' ').ConfigureAwait(false);
                await WriteStringAsync(cookiesFromContainer).ConfigureAwait(false);
                await WriteTwoBytesAsync((byte)'\r', (byte)'\n').ConfigureAwait(false);
            }
        }

        private async Task WriteHostHeaderAsync(Uri uri)
        {
            await WriteBytesAsync(KnownHeaders.Host.AsciiBytesWithColonSpace).ConfigureAwait(false);

            if (_pool.HostHeaderValueBytes != null)
            {
                Debug.Assert(Kind != HttpConnectionKind.Proxy);
                await WriteBytesAsync(_pool.HostHeaderValueBytes).ConfigureAwait(false);
            }
            else
            {
                Debug.Assert(Kind == HttpConnectionKind.Proxy);

                // TODO: #28863 Uri.IdnHost is missing '[', ']' characters around IPv6 address.
                // So, we need to add them manually for now.
                if (uri.HostNameType == UriHostNameType.IPv6)
                {
                    await WriteByteAsync((byte)'[').ConfigureAwait(false);
                    await WriteAsciiStringAsync(uri.IdnHost).ConfigureAwait(false);
                    await WriteByteAsync((byte)']').ConfigureAwait(false);
                }
                else
                {
                    await WriteAsciiStringAsync(uri.IdnHost).ConfigureAwait(false);
                }

                if (!uri.IsDefaultPort)
                {
                    await WriteByteAsync((byte)':').ConfigureAwait(false);
                    await WriteDecimalInt32Async(uri.Port).ConfigureAwait(false);
                }
            }

            await WriteTwoBytesAsync((byte)'\r', (byte)'\n').ConfigureAwait(false);
        }

        private Task WriteDecimalInt32Async(int value)
        {
            // Try to format into our output buffer directly.
            if (Utf8Formatter.TryFormat(value, new Span<byte>(_writeBuffer, _writeOffset, _writeBuffer.Length - _writeOffset), out int bytesWritten))
            {
                _writeOffset += bytesWritten;
                return Task.CompletedTask;
            }

            // If we don't have enough room, do it the slow way.
            return WriteAsciiStringAsync(value.ToString());
        }

        private Task WriteHexInt32Async(int value)
        {
            // Try to format into our output buffer directly.
            if (Utf8Formatter.TryFormat(value, new Span<byte>(_writeBuffer, _writeOffset, _writeBuffer.Length - _writeOffset), out int bytesWritten, 'X'))
            {
                _writeOffset += bytesWritten;
                return Task.CompletedTask;
            }

            // If we don't have enough room, do it the slow way.
            return WriteAsciiStringAsync(value.ToString("X", CultureInfo.InvariantCulture));
        }

        public async Task<HttpResponseMessage> SendAsyncCore(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            TaskCompletionSource<bool> allowExpect100ToContinue = null;
            Debug.Assert(_currentRequest == null, $"Expected null {nameof(_currentRequest)}.");
            Debug.Assert(RemainingBuffer.Length == 0, "Unexpected data in read buffer");

            _currentRequest = request;
            HttpMethod normalizedMethod = HttpMethod.Normalize(request.Method);

            Debug.Assert(!_canRetry);
            _canRetry = true;

            // Send the request.
            if (NetEventSource.IsEnabled) Trace($"Sending request: {request}");
            CancellationTokenRegistration cancellationRegistration = RegisterCancellation(cancellationToken);
            try
            {
                // Write request line
                await WriteStringAsync(normalizedMethod.Method).ConfigureAwait(false);
                await WriteByteAsync((byte)' ').ConfigureAwait(false);

                if (ReferenceEquals(normalizedMethod, HttpMethod.Connect))
                {
                    // RFC 7231 #section-4.3.6.
                    // Write only CONNECT foo.com:345 HTTP/1.1
                    if (!request.HasHeaders || request.Headers.Host == null)
                    {
                        throw new HttpRequestException(SR.net_http_request_no_host);
                    }
                    await WriteAsciiStringAsync(request.Headers.Host).ConfigureAwait(false);
                }
                else
                {
                    if (Kind == HttpConnectionKind.Proxy)
                    {
                        // Proxied requests contain full URL
                        Debug.Assert(request.RequestUri.Scheme == Uri.UriSchemeHttp);
                        await WriteBytesAsync(s_httpSchemeAndDelimiter).ConfigureAwait(false);

                        // TODO: #28863 Uri.IdnHost is missing '[', ']' characters around IPv6 address.
                        // So, we need to add them manually for now.
                        if (request.RequestUri.HostNameType == UriHostNameType.IPv6)
                        {
                            await WriteByteAsync((byte)'[').ConfigureAwait(false);
                            await WriteAsciiStringAsync(request.RequestUri.IdnHost).ConfigureAwait(false);
                            await WriteByteAsync((byte)']').ConfigureAwait(false);
                        }
                        else
                        {
                            await WriteAsciiStringAsync(request.RequestUri.IdnHost).ConfigureAwait(false);
                        }

                        if (!request.RequestUri.IsDefaultPort)
                        {
                            await WriteByteAsync((byte)':').ConfigureAwait(false);
                            await WriteDecimalInt32Async(request.RequestUri.Port).ConfigureAwait(false);
                        }
                    }
                    await WriteStringAsync(request.RequestUri.PathAndQuery).ConfigureAwait(false);
                }

                // Fall back to 1.1 for all versions other than 1.0
                Debug.Assert(request.Version.Major >= 0 && request.Version.Minor >= 0); // guaranteed by Version class
                bool isHttp10 = request.Version.Minor == 0 && request.Version.Major == 1;
                await WriteBytesAsync(isHttp10 ? s_spaceHttp10NewlineAsciiBytes : s_spaceHttp11NewlineAsciiBytes).ConfigureAwait(false);

                // Determine cookies to send
                string cookiesFromContainer = null;
                if (_pool.Settings._useCookies)
                {
                    cookiesFromContainer = _pool.Settings._cookieContainer.GetCookieHeader(request.RequestUri);
                    if (cookiesFromContainer == "")
                    {
                        cookiesFromContainer = null;
                    }
                }

                // Write special additional headers.  If a host isn't in the headers list, then a Host header
                // wasn't sent, so as it's required by HTTP 1.1 spec, send one based on the Request Uri.
                if (!request.HasHeaders || request.Headers.Host == null)
                {
                    await WriteHostHeaderAsync(request.RequestUri).ConfigureAwait(false);
                }

                // Write request headers
                if (request.HasHeaders || cookiesFromContainer != null)
                {
                    await WriteHeadersAsync(request.Headers, cookiesFromContainer).ConfigureAwait(false);
                }

                if (request.Content == null)
                {
                    // Write out Content-Length: 0 header to indicate no body,
                    // unless this is a method that never has a body.
                    if (normalizedMethod.MustHaveRequestBody)
                    {
                        await WriteBytesAsync(s_contentLength0NewlineAsciiBytes).ConfigureAwait(false);
                    }
                }
                else
                {
                    // Write content headers
                    await WriteHeadersAsync(request.Content.Headers, cookiesFromContainer: null).ConfigureAwait(false);
                }

                // CRLF for end of headers.
                await WriteTwoBytesAsync((byte)'\r', (byte)'\n').ConfigureAwait(false);

                Task sendRequestContentTask = null;
                if (request.Content == null)
                {
                    // We have nothing more to send, so flush out any headers we haven't yet sent.
                    await FlushAsync().ConfigureAwait(false);
                }
                else
                {
                    bool hasExpectContinueHeader = request.HasHeaders && request.Headers.ExpectContinue == true;
                    if (NetEventSource.IsEnabled) Trace($"Request content is not null, start processing it. hasExpectContinueHeader = {hasExpectContinueHeader}");

                    // Send the body if there is one.  We prefer to serialize the sending of the content before
                    // we try to receive any response, but if ExpectContinue has been set, we allow the sending
                    // to run concurrently until we receive the final status line, at which point we wait for it.
                    if (!hasExpectContinueHeader)
                    {
                        await SendRequestContentAsync(request, CreateRequestContentStream(request), cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        // We're sending an Expect: 100-continue header. We need to flush headers so that the server receives
                        // all of them, and we need to do so before initiating the send, as once we do that, it effectively
                        // owns the right to write, and we don't want to concurrently be accessing the write buffer.
                        await FlushAsync().ConfigureAwait(false);

                        // Create a TCS we'll use to block the request content from being sent, and create a timer that's used
                        // as a fail-safe to unblock the request content if we don't hear back from the server in a timely manner.
                        // Then kick off the request.  The TCS' result indicates whether content should be sent or not.
                        allowExpect100ToContinue = new TaskCompletionSource<bool>();
                        var expect100Timer = new Timer(
                            s => ((TaskCompletionSource<bool>)s).TrySetResult(true),
                            allowExpect100ToContinue, _pool.Settings._expect100ContinueTimeout, Timeout.InfiniteTimeSpan);
                        sendRequestContentTask = SendRequestContentWithExpect100ContinueAsync(
                            request, allowExpect100ToContinue.Task, CreateRequestContentStream(request), expect100Timer, cancellationToken);
                    }
                }

                // Start to read response.
                _allowedReadLineBytes = (int)Math.Min(int.MaxValue, _pool.Settings._maxResponseHeadersLength * 1024L);

                // We should not have any buffered data here; if there was, it should have been treated as an error
                // by the previous request handling.  (Note we do not support HTTP pipelining.)
                Debug.Assert(_readOffset == _readLength);

                // When the connection was taken out of the pool, a pre-emptive read was performed
                // into the read buffer. We need to consume that read prior to issuing another read.
                ValueTask<int>? t = ConsumeReadAheadTask();
                if (t != null)
                {
                    int bytesRead = await t.GetValueOrDefault().ConfigureAwait(false);
                    if (NetEventSource.IsEnabled) Trace($"Received {bytesRead} bytes.");

                    if (bytesRead == 0)
                    {
                        throw new IOException(SR.net_http_invalid_response_premature_eof);
                    }

                    _readOffset = 0;
                    _readLength = bytesRead;
                }

                // The request is no longer retryable; either we received data from the _readAheadTask,
                // or there was no _readAheadTask because this is the first request on the connection.
                // (We may have already set this as well if we sent request content.)
                _canRetry = false;

                // Parse the response status line.
                var response = new HttpResponseMessage() { RequestMessage = request, Content = new HttpConnectionResponseContent() };
                ParseStatusLine(await ReadNextResponseHeaderLineAsync().ConfigureAwait(false), response);

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
                    if (NetEventSource.IsEnabled) Trace($"Current {response.StatusCode} response is an interim response or not expected, need to read for a final response.");

                    // Discard headers that come with the interim 1xx responses.
                    // RFC7231: 1xx responses are terminated by the first empty line after the status-line.
                    while (!IsLineEmpty(await ReadNextResponseHeaderLineAsync().ConfigureAwait(false)));

                    // Parse the status line for next response.
                    ParseStatusLine(await ReadNextResponseHeaderLineAsync().ConfigureAwait(false), response);
                }

                // If we sent an Expect: 100-continue header, and didn't receive a 100-continue. Handle the final response accordingly.
                // Note that the developer may have added an Expect: 100-continue header even if there is no Content.
                if (allowExpect100ToContinue != null)
                {
                    if ((int)response.StatusCode >= 300 &&
                        request.Content != null &&
                        (request.Content.Headers.ContentLength == null || request.Content.Headers.ContentLength.GetValueOrDefault() > Expect100ErrorSendThreshold))
                    {
                        // For error final status codes, try to avoid sending the payload if its size is unknown or if it's known to be "big".
                        // If we already sent a header detailing the size of the payload, if we then don't send that payload, the server may wait
                        // for it and assume that the next request on the connection is actually this request's payload.  Thus we mark the connection
                        // to be closed.  However, we may have also lost a race condition with the Expect: 100-continue timeout, so if it turns out
                        // we've already started sending the payload (we weren't able to cancel it), then we don't need to force close the connection.
                        allowExpect100ToContinue.TrySetResult(false);
                        if (!allowExpect100ToContinue.Task.Result) // if Result is true, the timeout already expired and we started sending content
                        {
                            _connectionClose = true;
                        }
                    }
                    else
                    {
                        // For any success status codes or for errors when the request content length is known to be small, send the payload
                        // (if there is one... if there isn't, Content is null and thus allowExpect100ToContinue is also null, we won't get here).
                        allowExpect100ToContinue.TrySetResult(true);
                    }
                }

                // Now that we've received our final status line, wait for the request content to fully send.
                // In most common scenarios, the server won't send back a response until all of the request
                // content has been received, so this task should generally already be complete.
                if (sendRequestContentTask != null)
                {
                    await sendRequestContentTask.ConfigureAwait(false);
                    sendRequestContentTask = null;
                }

                // Now we are sure that the request was fully sent.
                if (NetEventSource.IsEnabled) Trace("Request is fully sent.");

                // Parse the response headers.
                while (true)
                {
                    ArraySegment<byte> line = await ReadNextResponseHeaderLineAsync(foldedHeadersAllowed: true).ConfigureAwait(false);
                    if (IsLineEmpty(line))
                    {
                        break;
                    }
                    ParseHeaderNameValue(this, line, response);
                }

                // Determine whether we need to force close the connection when the request/response has completed.
                if (response.Headers.ConnectionClose.GetValueOrDefault())
                {
                    _connectionClose = true;
                }

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

                }
                else if (response.StatusCode == HttpStatusCode.SwitchingProtocols)
                {
                    responseStream = new RawConnectionStream(this);
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
                else if (response.Headers.TransferEncodingChunked == true)
                {
                    responseStream = new ChunkedEncodingReadStream(this, response);
                }
                else
                {
                    responseStream = new ConnectionCloseReadStream(this);
                }
                ((HttpConnectionResponseContent)response.Content).SetStream(responseStream);

                if (NetEventSource.IsEnabled) Trace($"Received response: {response}");

                // Process Set-Cookie headers.
                if (_pool.Settings._useCookies)
                {
                    CookieHelper.ProcessReceivedCookies(response, _pool.Settings._cookieContainer);
                }

                return response;
            }
            catch (Exception error)
            {
                // Clean up the cancellation registration in case we're still registered.
                cancellationRegistration.Dispose();

                // Make sure to complete the allowExpect100ToContinue task if it exists.
                allowExpect100ToContinue?.TrySetResult(false);

                if (NetEventSource.IsEnabled) Trace($"Error sending request: {error}");
                Dispose();

                // At this point, we're going to throw an exception; we just need to
                // determine which exception to throw.

                if (CancellationHelper.ShouldWrapInOperationCanceledException(error, cancellationToken))
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
                    throw CancellationHelper.CreateOperationCanceledException(error, cancellationToken);
                }
                else if (error is InvalidOperationException)
                {
                    // For consistency with other handlers we wrap the exception in an HttpRequestException.
                    throw new HttpRequestException(SR.net_http_client_execution_error, error);
                }
                else if (error is IOException ioe)
                {
                    // For consistency with other handlers we wrap the exception in an HttpRequestException.
                    // If the request is retryable, indicate that on the exception.
                    throw new HttpRequestException(SR.net_http_client_execution_error, ioe, _canRetry);
                }
                else
                {
                    // Otherwise, just allow the original exception to propagate.
                    throw;
                }
            }
        }

        public sealed override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return SendAsyncCore(request, cancellationToken);
        }

        private HttpContentWriteStream CreateRequestContentStream(HttpRequestMessage request)
        {
            bool requestTransferEncodingChunked = request.HasHeaders && request.Headers.TransferEncodingChunked == true;
            HttpContentWriteStream requestContentStream = requestTransferEncodingChunked ? (HttpContentWriteStream)
                new ChunkedEncodingWriteStream(this) :
                new ContentLengthWriteStream(this);
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
            // - A weak reference to this HttpConnection is stored in the cancellation token, to prevent the token from
            //   artificially keeping this connection alive.
            return cancellationToken.Register(s =>
            {
                var weakThisRef = (WeakReference<HttpConnection>)s;
                if (weakThisRef.TryGetTarget(out HttpConnection strongThisRef))
                {
                    if (NetEventSource.IsEnabled) strongThisRef.Trace("Cancellation requested. Disposing of the connection.");
                    strongThisRef.Dispose();
                }
            }, _weakThisRef);
        }

        private static bool IsLineEmpty(ArraySegment<byte> line) => line.Count == 0;

        private async Task SendRequestContentAsync(HttpRequestMessage request, HttpContentWriteStream stream, CancellationToken cancellationToken)
        {
            // Now that we're sending content, prohibit retries on this connection.
            _canRetry = false;

            // Copy all of the data to the server.
            await request.Content.CopyToAsync(stream, _transportContext, cancellationToken).ConfigureAwait(false);

            // Finish the content; with a chunked upload, this includes writing the terminating chunk.
            await stream.FinishAsync().ConfigureAwait(false);

            // Flush any content that might still be buffered.
            await FlushAsync().ConfigureAwait(false);

            if (NetEventSource.IsEnabled) Trace("Finished sending request content.");
        }

        private async Task SendRequestContentWithExpect100ContinueAsync(
            HttpRequestMessage request, Task<bool> allowExpect100ToContinueTask, HttpContentWriteStream stream, Timer expect100Timer, CancellationToken cancellationToken)
        {
            // Wait until we receive a trigger notification that it's ok to continue sending content.
            // This will come either when the timer fires or when we receive a response status line from the server.
            bool sendRequestContent = await allowExpect100ToContinueTask.ConfigureAwait(false);

            // Clean up the timer; it's no longer needed.
            expect100Timer.Dispose();

            // Send the content if we're supposed to.  Otherwise, we're done.
            if (sendRequestContent)
            {
                if (NetEventSource.IsEnabled) Trace($"Sending request content for Expect: 100-continue.");
                await SendRequestContentAsync(request, stream, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                if (NetEventSource.IsEnabled) Trace($"Canceling request content for Expect: 100-continue.");
            }
        }

        // TODO: Remove this overload once https://github.com/dotnet/csharplang/issues/1331 is addressed
        // and the compiler doesn't prevent using spans in async methods.
        private static void ParseStatusLine(ArraySegment<byte> line, HttpResponseMessage response) =>
            ParseStatusLine((Span<byte>)line, response);

        private static void ParseStatusLine(Span<byte> line, HttpResponseMessage response)
        {
            // We sent the request version as either 1.0 or 1.1.
            // We expect a response version of the form 1.X, where X is a single digit as per RFC.

            // Validate the beginning of the status line and set the response version.
            const int MinStatusLineLength = 12; // "HTTP/1.x 123" 
            if (line.Length < MinStatusLineLength || line[8] != ' ')
            {
                throw new HttpRequestException(SR.Format(SR.net_http_invalid_response_status_line, Encoding.ASCII.GetString(line)));
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
                if (IsDigit(minorVersion) &&
                    line.Slice(0, 7).SequenceEqual(s_http1DotBytes))
                {
                    response.SetVersionWithoutValidation(new Version(1, minorVersion - '0'));
                }
                else
                {
                    throw new HttpRequestException(SR.Format(SR.net_http_invalid_response_status_line, Encoding.ASCII.GetString(line)));
                }
            }

            // Set the status code
            byte status1 = line[9], status2 = line[10], status3 = line[11];
            if (!IsDigit(status1) || !IsDigit(status2) || !IsDigit(status3))
            {
                throw new HttpRequestException(SR.Format(SR.net_http_invalid_response_status_code, Encoding.ASCII.GetString(line.Slice(9, 3))));
            }
            response.SetStatusCodeWithoutValidation((HttpStatusCode)(100 * (status1 - '0') + 10 * (status2 - '0') + (status3 - '0')));

            // Parse (optional) reason phrase
            if (line.Length == MinStatusLineLength)
            {
                response.SetReasonPhraseWithoutValidation(string.Empty);
            }
            else if (line[MinStatusLineLength] == ' ')
            {
                Span<byte> reasonBytes = line.Slice(MinStatusLineLength + 1);
                string knownReasonPhrase = HttpStatusDescription.Get(response.StatusCode);
                if (knownReasonPhrase != null && EqualsOrdinal(knownReasonPhrase, reasonBytes))
                {
                    response.SetReasonPhraseWithoutValidation(knownReasonPhrase);
                }
                else
                {
                    try
                    {
                        response.ReasonPhrase = HttpRuleParser.DefaultHttpEncoding.GetString(reasonBytes);
                    }
                    catch (FormatException error)
                    {
                        throw new HttpRequestException(SR.Format(SR.net_http_invalid_response_status_reason, Encoding.ASCII.GetString(reasonBytes.ToArray())), error);
                    }
                }
            }
            else
            {
                throw new HttpRequestException(SR.Format(SR.net_http_invalid_response_status_line, Encoding.ASCII.GetString(line)));
            }
        }

        // TODO: Remove this overload once https://github.com/dotnet/csharplang/issues/1331 is addressed
        // and the compiler doesn't prevent using spans in async methods.
        private static void ParseHeaderNameValue(HttpConnection connection, ArraySegment<byte> line, HttpResponseMessage response) =>
            ParseHeaderNameValue(connection, (Span<byte>)line, response, isFromTrailer:false);

        private static void ParseHeaderNameValue(HttpConnection connection, ReadOnlySpan<byte> line, HttpResponseMessage response, bool isFromTrailer)
        {
            Debug.Assert(line.Length > 0);

            int pos = 0;
            while (line[pos] != (byte)':' && line[pos] != (byte)' ')
            {
                pos++;
                if (pos == line.Length)
                {
                    // Invalid header line that doesn't contain ':'.
                    throw new HttpRequestException(SR.Format(SR.net_http_invalid_response_header_line, Encoding.ASCII.GetString(line)));
                }
            }

            if (pos == 0)
            {
                // Invalid empty header name.
                throw new HttpRequestException(SR.Format(SR.net_http_invalid_response_header_name, ""));
            }

            if (!HeaderDescriptor.TryGet(line.Slice(0, pos), out HeaderDescriptor descriptor))
            {
                // Invalid header name.
                throw new HttpRequestException(SR.Format(SR.net_http_invalid_response_header_name, Encoding.ASCII.GetString(line.Slice(0, pos))));
            }

            if (isFromTrailer && descriptor.KnownHeader != null && s_disallowedTrailers.Contains(descriptor.KnownHeader))
            {
                // Disallowed trailer fields.
                // A recipient MUST ignore fields that are forbidden to be sent in a trailer.
                if (NetEventSource.IsEnabled) connection.Trace($"Stripping forbidden {descriptor.Name} from trailer headers.");
                return;
            }

            // Eat any trailing whitespace
            while (line[pos] == (byte)' ')
            {
                pos++;
                if (pos == line.Length)
                {
                    // Invalid header line that doesn't contain ':'.
                    throw new HttpRequestException(SR.Format(SR.net_http_invalid_response_header_line, Encoding.ASCII.GetString(line)));
                }
            }

            if (line[pos++] != ':')
            {
                // Invalid header line that doesn't contain ':'.
                throw new HttpRequestException(SR.Format(SR.net_http_invalid_response_header_line, Encoding.ASCII.GetString(line)));
            }

            // Skip whitespace after colon
            while (pos < line.Length && (line[pos] == (byte)' ' || line[pos] == (byte)'\t'))
            {
                pos++;
            }

            string headerValue = descriptor.GetHeaderValue(line.Slice(pos));

            // Note we ignore the return value from TryAddWithoutValidation. If the header can't be added, we silently drop it.
            // Request headers returned on the response must be treated as custom headers.
            if (isFromTrailer)
            {
                response.TrailingHeaders.TryAddWithoutValidation(descriptor.HeaderType == HttpHeaderType.Request ? descriptor.AsCustomHeader() : descriptor, headerValue);
            }
            else if (descriptor.HeaderType == HttpHeaderType.Content)
            {
                response.Content.Headers.TryAddWithoutValidation(descriptor, headerValue);
            }
            else
            {
                response.Headers.TryAddWithoutValidation(descriptor.HeaderType == HttpHeaderType.Request ? descriptor.AsCustomHeader() : descriptor, headerValue);
            }
        }

        private static bool IsDigit(byte c) => (uint)(c - '0') <= '9' - '0';

        private void WriteToBuffer(ReadOnlySpan<byte> source)
        {
            Debug.Assert(source.Length <= _writeBuffer.Length - _writeOffset);
            source.CopyTo(new Span<byte>(_writeBuffer, _writeOffset, source.Length));
            _writeOffset += source.Length;
        }

        private void WriteToBuffer(ReadOnlyMemory<byte> source)
        {
            Debug.Assert(source.Length <= _writeBuffer.Length - _writeOffset);
            source.Span.CopyTo(new Span<byte>(_writeBuffer, _writeOffset, source.Length));
            _writeOffset += source.Length;
        }

        private async Task WriteAsync(ReadOnlyMemory<byte> source)
        {
            int remaining = _writeBuffer.Length - _writeOffset;

            if (source.Length <= remaining)
            {
                // Fits in current write buffer.  Just copy and return.
                WriteToBuffer(source);
                return;
            }

            if (_writeOffset != 0)
            {
                // Fit what we can in the current write buffer and flush it.
                WriteToBuffer(source.Slice(0, remaining));
                source = source.Slice(remaining);
                await FlushAsync().ConfigureAwait(false);
            }

            if (source.Length >= _writeBuffer.Length)
            {
                // Large write.  No sense buffering this.  Write directly to stream.
                await WriteToStreamAsync(source).ConfigureAwait(false);
            }
            else
            {
                // Copy remainder into buffer
                WriteToBuffer(source);
            }
        }

        private void WriteWithoutBuffering(ReadOnlySpan<byte> source)
        {
            if (_writeOffset != 0)
            {
                int remaining = _writeBuffer.Length - _writeOffset;
                if (source.Length <= remaining)
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

        private ValueTask WriteWithoutBufferingAsync(ReadOnlyMemory<byte> source)
        {
            if (_writeOffset == 0)
            {
                // There's nothing in the write buffer we need to flush.
                // Just write the supplied data out to the stream.
                return WriteToStreamAsync(source);
            }

            int remaining = _writeBuffer.Length - _writeOffset;
            if (source.Length <= remaining)
            {
                // There's something already in the write buffer, but the content
                // we're writing can also fit after it in the write buffer.  Copy
                // the content to the write buffer and then flush it, so that we
                // can do a single send rather than two.
                WriteToBuffer(source);
                return FlushAsync();
            }

            // There's data in the write buffer and the data we're writing doesn't fit after it.
            // Do two writes, one to flush the buffer and then another to write the supplied content.
            return new ValueTask(FlushThenWriteWithoutBufferingAsync(source));
        }

        private async Task FlushThenWriteWithoutBufferingAsync(ReadOnlyMemory<byte> source)
        {
            await FlushAsync().ConfigureAwait(false);
            await WriteToStreamAsync(source).ConfigureAwait(false);
        }

        private Task WriteByteAsync(byte b)
        {
            if (_writeOffset < _writeBuffer.Length)
            {
                _writeBuffer[_writeOffset++] = b;
                return Task.CompletedTask;
            }
            return WriteByteSlowAsync(b);
        }

        private async Task WriteByteSlowAsync(byte b)
        {
            Debug.Assert(_writeOffset == _writeBuffer.Length);
            await WriteToStreamAsync(_writeBuffer).ConfigureAwait(false);

            _writeBuffer[0] = b;
            _writeOffset = 1;
        }

        private Task WriteTwoBytesAsync(byte b1, byte b2)
        {
            if (_writeOffset <= _writeBuffer.Length - 2)
            {
                byte[] buffer = _writeBuffer;
                buffer[_writeOffset++] = b1;
                buffer[_writeOffset++] = b2;
                return Task.CompletedTask;
            }
            return WriteTwoBytesSlowAsync(b1, b2);
        }

        private async Task WriteTwoBytesSlowAsync(byte b1, byte b2)
        {
            await WriteByteAsync(b1).ConfigureAwait(false);
            await WriteByteAsync(b2).ConfigureAwait(false);
        }

        private Task WriteBytesAsync(byte[] bytes)
        {
            if (_writeOffset <= _writeBuffer.Length - bytes.Length)
            {
                Buffer.BlockCopy(bytes, 0, _writeBuffer, _writeOffset, bytes.Length);
                _writeOffset += bytes.Length;
                return Task.CompletedTask;
            }
            return WriteBytesSlowAsync(bytes);
        }

        private async Task WriteBytesSlowAsync(byte[] bytes)
        {
            int offset = 0;
            while (true)
            {
                int remaining = bytes.Length - offset;
                int toCopy = Math.Min(remaining, _writeBuffer.Length - _writeOffset);
                Buffer.BlockCopy(bytes, offset, _writeBuffer, _writeOffset, toCopy);
                _writeOffset += toCopy;
                offset += toCopy;

                Debug.Assert(offset <= bytes.Length, $"Expected {nameof(offset)} to be <= {bytes.Length}, got {offset}");
                Debug.Assert(_writeOffset <= _writeBuffer.Length, $"Expected {nameof(_writeOffset)} to be <= {_writeBuffer.Length}, got {_writeOffset}");
                if (offset == bytes.Length)
                {
                    break;
                }
                else if (_writeOffset == _writeBuffer.Length)
                {
                    await WriteToStreamAsync(_writeBuffer).ConfigureAwait(false);
                    _writeOffset = 0;
                }
            }
        }

        private Task WriteStringAsync(string s)
        {
            // If there's enough space in the buffer to just copy all of the string's bytes, do so.
            // Unlike WriteAsciiStringAsync, validate each char along the way.
            int offset = _writeOffset;
            if (s.Length <= _writeBuffer.Length - offset)
            {
                byte[] writeBuffer = _writeBuffer;
                foreach (char c in s)
                {
                    if ((c & 0xFF80) != 0)
                    {
                        throw new HttpRequestException(SR.net_http_request_invalid_char_encoding);
                    }
                    writeBuffer[offset++] = (byte)c;
                }
                _writeOffset = offset;
                return Task.CompletedTask;
            }

            // Otherwise, fall back to doing a normal slow string write; we could optimize away
            // the extra checks later, but the case where we cross a buffer boundary should be rare.
            return WriteStringAsyncSlow(s);
        }

        private Task WriteAsciiStringAsync(string s)
        {
            // If there's enough space in the buffer to just copy all of the string's bytes, do so.
            int offset = _writeOffset;
            if (s.Length <= _writeBuffer.Length - offset)
            {
                byte[] writeBuffer = _writeBuffer;
                foreach (char c in s)
                {
                    writeBuffer[offset++] = (byte)c;
                }
                _writeOffset = offset;
                return Task.CompletedTask;
            }

            // Otherwise, fall back to doing a normal slow string write; we could optimize away
            // the extra checks later, but the case where we cross a buffer boundary should be rare.
            return WriteStringAsyncSlow(s);
        }

        private async Task WriteStringAsyncSlow(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if ((c & 0xFF80) != 0)
                {
                    throw new HttpRequestException(SR.net_http_request_invalid_char_encoding);
                }
                await WriteByteAsync((byte)c).ConfigureAwait(false);
            }
        }

        private void Flush()
        {
            if (_writeOffset > 0)
            {
                WriteToStream(new ReadOnlySpan<byte>(_writeBuffer, 0, _writeOffset));
                _writeOffset = 0;
            }
        }

        private ValueTask FlushAsync()
        {
            if (_writeOffset > 0)
            {
                ValueTask t = WriteToStreamAsync(new ReadOnlyMemory<byte>(_writeBuffer, 0, _writeOffset));
                _writeOffset = 0;
                return t;
            }
            return default;
        }

        private void WriteToStream(ReadOnlySpan<byte> source)
        {
            if (NetEventSource.IsEnabled) Trace($"Writing {source.Length} bytes.");
            _stream.Write(source);
        }

        private ValueTask WriteToStreamAsync(ReadOnlyMemory<byte> source)
        {
            if (NetEventSource.IsEnabled) Trace($"Writing {source.Length} bytes.");
            return _stream.WriteAsync(source);
        }

        private bool TryReadNextLine(out ReadOnlySpan<byte> line)
        {
            var buffer = new ReadOnlySpan<byte>(_readBuffer, _readOffset, _readLength - _readOffset);
            int length = buffer.IndexOf((byte)'\n');
            if (length < 0)
            {
                if (_allowedReadLineBytes < buffer.Length)
                {
                    throw new HttpRequestException(SR.Format(SR.net_http_response_headers_exceeded_length, _pool.Settings._maxResponseHeadersLength));
                }

                line = default;
                return false;
            }

            int bytesConsumed = length + 1;
            _readOffset += bytesConsumed;
            _allowedReadLineBytes -= bytesConsumed;
            ThrowIfExceededAllowedReadLineBytes();

            line = buffer.Slice(0, length > 0 && buffer[length - 1] == '\r' ? length - 1 : length);
            return true;
        }

        private async ValueTask<ArraySegment<byte>> ReadNextResponseHeaderLineAsync(bool foldedHeadersAllowed = false)
        {
            int previouslyScannedBytes = 0;
            while (true)
            {
                int scanOffset = _readOffset + previouslyScannedBytes;
                int lfIndex = Array.IndexOf(_readBuffer, (byte)'\n', scanOffset, _readLength - scanOffset);
                if (lfIndex >= 0)
                {
                    int startIndex = _readOffset;
                    int length = lfIndex - startIndex;
                    if (lfIndex > 0 && _readBuffer[lfIndex - 1] == '\r')
                    {
                        length--;
                    }

                    // If this isn't the ending header, we need to account for the possibility
                    // of folded headers, which per RFC2616 are headers split across multiple
                    // lines, where the continuation line begins with a space or horizontal tab.
                    // The feature was deprecated in RFC 7230 3.2.4, but some servers still use it.
                    if (foldedHeadersAllowed && length > 0)
                    {
                        // If the newline is the last character we've buffered, we need at least
                        // one more character in order to see whether it's space/tab, in which
                        // case it's a folded header.
                        if (lfIndex + 1 == _readLength)
                        {
                            // The LF is at the end of the buffer, so we need to read more
                            // to determine whether there's a continuation.  We'll read
                            // and then loop back around again, but to avoid needing to
                            // rescan the whole header, reposition to one character before
                            // the newline so that we'll find it quickly.
                            int backPos = _readBuffer[lfIndex - 1] == '\r' ? lfIndex - 2 : lfIndex - 1;
                            Debug.Assert(backPos >= 0);
                            previouslyScannedBytes = backPos - _readOffset;
                            _allowedReadLineBytes -= backPos - scanOffset;
                            ThrowIfExceededAllowedReadLineBytes();
                            await FillAsync().ConfigureAwait(false);
                            continue;
                        }

                        // We have at least one more character we can look at.
                        Debug.Assert(lfIndex + 1 < _readLength);
                        char nextChar = (char)_readBuffer[lfIndex + 1];
                        if (nextChar == ' ' || nextChar == '\t')
                        {
                            // The next header is a continuation.

                            // Folded headers are only allowed within header field values, not within header field names,
                            // so if we haven't seen a colon, this is invalid.
                            if (Array.IndexOf(_readBuffer, (byte)':', _readOffset, lfIndex - _readOffset) == -1)
                            {
                                throw new HttpRequestException(SR.net_http_invalid_response_header_folder);
                            }

                            // When we return the line, we need the interim newlines filtered out. According
                            // to RFC 7230 3.2.4, a valid approach to dealing with them is to "replace each
                            // received obs-fold with one or more SP octets prior to interpreting the field
                            // value or forwarding the message downstream", so that's what we do.
                            _readBuffer[lfIndex] = (byte)' ';
                            if (_readBuffer[lfIndex - 1] == '\r')
                            {
                                _readBuffer[lfIndex - 1] = (byte)' ';
                            }

                            // Update how much we've read, and simply go back to search for the next newline.
                            previouslyScannedBytes = (lfIndex + 1 - _readOffset);
                            _allowedReadLineBytes -= (lfIndex + 1 - scanOffset);
                            ThrowIfExceededAllowedReadLineBytes();
                            continue;
                        }

                        // Not at the end of a header with a continuation.
                    }

                    // Advance read position past the LF
                    _allowedReadLineBytes -= lfIndex + 1 - scanOffset;
                    ThrowIfExceededAllowedReadLineBytes();
                    _readOffset = lfIndex + 1;

                    return new ArraySegment<byte>(_readBuffer, startIndex, length);
                }

                // Couldn't find LF.  Read more. Note this may cause _readOffset to change.
                previouslyScannedBytes = _readLength - _readOffset;
                _allowedReadLineBytes -= _readLength - scanOffset;
                ThrowIfExceededAllowedReadLineBytes();
                await FillAsync().ConfigureAwait(false);
            }
        }

        private void ThrowIfExceededAllowedReadLineBytes()
        {
            if (_allowedReadLineBytes < 0)
            {
                throw new HttpRequestException(SR.Format(SR.net_http_response_headers_exceeded_length, _pool.Settings._maxResponseHeadersLength));
            }
        }

        // Throws IOException on EOF.  This is only called when we expect more data.
        private void Fill()
        {
            Debug.Assert(_readAheadTask == null);

            int remaining = _readLength - _readOffset;
            Debug.Assert(remaining >= 0);

            if (remaining == 0)
            {
                // No data in the buffer.  Simply reset the offset and length to 0 to allow
                // the whole buffer to be filled.
                _readOffset = _readLength = 0;
            }
            else if (_readOffset > 0)
            {
                // There's some data in the buffer but it's not at the beginning.  Shift it
                // down to make room for more.
                Buffer.BlockCopy(_readBuffer, _readOffset, _readBuffer, 0, remaining);
                _readOffset = 0;
                _readLength = remaining;
            }
            else if (remaining == _readBuffer.Length)
            {
                // The whole buffer is full, but the caller is still requesting more data,
                // so increase the size of the buffer.
                Debug.Assert(_readOffset == 0);
                Debug.Assert(_readLength == _readBuffer.Length);

                var newReadBuffer = new byte[_readBuffer.Length * 2];
                Buffer.BlockCopy(_readBuffer, 0, newReadBuffer, 0, remaining);
                _readBuffer = newReadBuffer;
                _readOffset = 0;
                _readLength = remaining;
            }

            int bytesRead = _stream.Read(_readBuffer, _readLength, _readBuffer.Length - _readLength);
            if (NetEventSource.IsEnabled) Trace($"Received {bytesRead} bytes.");
            if (bytesRead == 0)
            {
                throw new IOException(SR.net_http_invalid_response_premature_eof);
            }

            _readLength += bytesRead;
        }

        // Throws IOException on EOF.  This is only called when we expect more data.
        private async Task FillAsync()
        {
            Debug.Assert(_readAheadTask == null);

            int remaining = _readLength - _readOffset;
            Debug.Assert(remaining >= 0);

            if (remaining == 0)
            {
                // No data in the buffer.  Simply reset the offset and length to 0 to allow
                // the whole buffer to be filled.
                _readOffset = _readLength = 0;
            }
            else if (_readOffset > 0)
            {
                // There's some data in the buffer but it's not at the beginning.  Shift it
                // down to make room for more.
                Buffer.BlockCopy(_readBuffer, _readOffset, _readBuffer, 0, remaining);
                _readOffset = 0;
                _readLength = remaining;
            }
            else if (remaining == _readBuffer.Length)
            {
                // The whole buffer is full, but the caller is still requesting more data,
                // so increase the size of the buffer.
                Debug.Assert(_readOffset == 0);
                Debug.Assert(_readLength == _readBuffer.Length);

                var newReadBuffer = new byte[_readBuffer.Length * 2];
                Buffer.BlockCopy(_readBuffer, 0, newReadBuffer, 0, remaining);
                _readBuffer = newReadBuffer;
                _readOffset = 0;
                _readLength = remaining;
            }

            int bytesRead = await _stream.ReadAsync(new Memory<byte>(_readBuffer, _readLength, _readBuffer.Length - _readLength)).ConfigureAwait(false);

            if (NetEventSource.IsEnabled) Trace($"Received {bytesRead} bytes.");
            if (bytesRead == 0)
            {
                throw new IOException(SR.net_http_invalid_response_premature_eof);
            }

            _readLength += bytesRead;
        }

        private void ReadFromBuffer(Span<byte> buffer)
        {
            Debug.Assert(buffer.Length <= _readLength - _readOffset);

            new Span<byte>(_readBuffer, _readOffset, buffer.Length).CopyTo(buffer);
            _readOffset += buffer.Length;
        }

        private int Read(Span<byte> destination)
        {
            // This is called when reading the response body.

            int remaining = _readLength - _readOffset;
            if (remaining > 0)
            {
                // We have data in the read buffer.  Return it to the caller.
                if (destination.Length <= remaining)
                {
                    ReadFromBuffer(destination);
                    return destination.Length;
                }
                else
                {
                    ReadFromBuffer(destination.Slice(0, remaining));
                    return remaining;
                }
            }

            // No data in read buffer. 
            // Do an unbuffered read directly against the underlying stream.
            Debug.Assert(_readAheadTask == null, "Read ahead task should have been consumed as part of the headers.");
            int count = _stream.Read(destination);
            if (NetEventSource.IsEnabled) Trace($"Received {count} bytes.");
            return count;
        }

        private async ValueTask<int> ReadAsync(Memory<byte> destination)
        {
            // This is called when reading the response body.

            int remaining = _readLength - _readOffset;
            if (remaining > 0)
            {
                // We have data in the read buffer.  Return it to the caller.
                if (destination.Length <= remaining)
                {
                    ReadFromBuffer(destination.Span);
                    return destination.Length;
                }
                else
                {
                    ReadFromBuffer(destination.Span.Slice(0, remaining));
                    return remaining;
                }
            }

            // No data in read buffer. 
            // Do an unbuffered read directly against the underlying stream.
            Debug.Assert(_readAheadTask == null, "Read ahead task should have been consumed as part of the headers.");
            int count = await _stream.ReadAsync(destination).ConfigureAwait(false);
            if (NetEventSource.IsEnabled) Trace($"Received {count} bytes.");
            return count;
        }

        private int ReadBuffered(Span<byte> destination)
        {
            // This is called when reading the response body.
            Debug.Assert(destination.Length != 0);

            int remaining = _readLength - _readOffset;
            if (remaining > 0)
            {
                // We have data in the read buffer.  Return it to the caller.
                if (destination.Length <= remaining)
                {
                    ReadFromBuffer(destination);
                    return destination.Length;
                }
                else
                {
                    ReadFromBuffer(destination.Slice(0, remaining));
                    return remaining;
                }
            }

            // No data in read buffer. 
            _readOffset = _readLength = 0;

            // Do a buffered read directly against the underlying stream.
            Debug.Assert(_readAheadTask == null, "Read ahead task should have been consumed as part of the headers.");
            int bytesRead = _stream.Read(_readBuffer, 0, _readBuffer.Length);
            if (NetEventSource.IsEnabled) Trace($"Received {bytesRead} bytes.");
            _readLength = bytesRead;

            // Hand back as much data as we can fit.
            int bytesToCopy = Math.Min(bytesRead, destination.Length);
            _readBuffer.AsSpan(0, bytesToCopy).CopyTo(destination);
            _readOffset = bytesToCopy;
            return bytesToCopy;
        }

        private ValueTask<int> ReadBufferedAsync(Memory<byte> destination)
        {
            // If the caller provided buffer, and thus the amount of data desired to be read,
            // is larger than the internal buffer, there's no point going through the internal
            // buffer, so just do an unbuffered read.
            return destination.Length >= _readBuffer.Length ?
                ReadAsync(destination) :
                ReadBufferedAsyncCore(destination);
        }

        private async ValueTask<int> ReadBufferedAsyncCore(Memory<byte> destination)
        {
            // This is called when reading the response body.

            int remaining = _readLength - _readOffset;
            if (remaining > 0)
            {
                // We have data in the read buffer.  Return it to the caller.
                if (destination.Length <= remaining)
                {
                    ReadFromBuffer(destination.Span);
                    return destination.Length;
                }
                else
                {
                    ReadFromBuffer(destination.Span.Slice(0, remaining));
                    return remaining;
                }
            }

            // No data in read buffer. 
            _readOffset = _readLength = 0;

            // Do a buffered read directly against the underlying stream.
            Debug.Assert(_readAheadTask == null, "Read ahead task should have been consumed as part of the headers.");
            int bytesRead = await _stream.ReadAsync(_readBuffer.AsMemory()).ConfigureAwait(false);
            if (NetEventSource.IsEnabled) Trace($"Received {bytesRead} bytes.");
            _readLength = bytesRead;

            // Hand back as much data as we can fit.
            int bytesToCopy = Math.Min(bytesRead, destination.Length);
            _readBuffer.AsSpan(0, bytesToCopy).CopyTo(destination.Span);
            _readOffset = bytesToCopy;
            return bytesToCopy;
        }

        private async Task CopyFromBufferAsync(Stream destination, int count, CancellationToken cancellationToken)
        {
            Debug.Assert(count <= _readLength - _readOffset);

            if (NetEventSource.IsEnabled) Trace($"Copying {count} bytes to stream.");
            await destination.WriteAsync(new ReadOnlyMemory<byte>(_readBuffer, _readOffset, count), cancellationToken).ConfigureAwait(false);
            _readOffset += count;
        }

        private Task CopyToUntilEofAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            Debug.Assert(destination != null);

            int remaining = _readLength - _readOffset;
            return remaining > 0 ?
                CopyToUntilEofWithExistingBufferedDataAsync(destination, bufferSize, cancellationToken) :
                _stream.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        private async Task CopyToUntilEofWithExistingBufferedDataAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            int remaining = _readLength - _readOffset;
            Debug.Assert(remaining > 0);

            await CopyFromBufferAsync(destination, remaining, cancellationToken).ConfigureAwait(false);
            _readLength = _readOffset = 0;

            await _stream.CopyToAsync(destination, bufferSize).ConfigureAwait(false);
        }

        // Copy *exactly* [length] bytes into destination; throws on end of stream.
        private async Task CopyToContentLengthAsync(Stream destination, ulong length, int bufferSize, CancellationToken cancellationToken)
        {
            Debug.Assert(destination != null);
            Debug.Assert(length > 0);

            // Copy any data left in the connection's buffer to the destination.
            int remaining = _readLength - _readOffset;
            if (remaining > 0)
            {
                if ((ulong)remaining > length)
                {
                    remaining = (int)length;
                }
                await CopyFromBufferAsync(destination, remaining, cancellationToken).ConfigureAwait(false);

                length -= (ulong)remaining;
                if (length == 0)
                {
                    return;
                }

                Debug.Assert(_readLength - _readOffset == 0, "HttpConnection's buffer should have been empty.");
            }

            // Repeatedly read into HttpConnection's buffer and write that buffer to the destination
            // stream. If after doing so, we find that we filled the whole connection's buffer (which
            // is sized mainly for HTTP headers rather than large payloads), grow the connection's
            // read buffer to the requested buffer size to use for the remainder of the operation. We
            // use a temporary buffer from the ArrayPool so that the connection doesn't hog large
            // buffers from the pool for extended durations, especially if it's going to sit in the
            // connection pool for a prolonged period.
            byte[] origReadBuffer = null;
            try
            {
                while (true)
                {
                    await FillAsync().ConfigureAwait(false);

                    remaining = (ulong)_readLength < length ? _readLength : (int)length;
                    await CopyFromBufferAsync(destination, remaining, cancellationToken).ConfigureAwait(false);

                    length -= (ulong)remaining;
                    if (length == 0)
                    {
                        return;
                    }

                    // If we haven't yet grown the buffer (if we previously grew it, then it's sufficiently large), and
                    // if we filled the read buffer while doing the last read (which is at least one indication that the
                    // data arrival rate is fast enough to warrant a larger buffer), and if the buffer size we'd want is
                    // larger than the one we already have, then grow the connection's read buffer to that size.
                    if (origReadBuffer == null)
                    {
                        byte[] currentReadBuffer = _readBuffer;
                        if (remaining == currentReadBuffer.Length)
                        {
                            int desiredBufferSize = (int)Math.Min((ulong)bufferSize, length);
                            if (desiredBufferSize > currentReadBuffer.Length)
                            {
                                origReadBuffer = currentReadBuffer;
                                _readBuffer = ArrayPool<byte>.Shared.Rent(desiredBufferSize);
                            }
                        }
                    }
                }
            }
            finally
            {
                if (origReadBuffer != null)
                {
                    byte[] tmp = _readBuffer;
                    _readBuffer = origReadBuffer;
                    ArrayPool<byte>.Shared.Return(tmp);

                    // _readOffset and _readLength may not be within range of the original
                    // buffer, and even if they are, they won't refer to read data at this
                    // point.  But we don't care what remaining data there was, other than
                    // that there may have been some, as subsequent code is going to check
                    // whether these are the same and then force the connection closed if
                    // they're not.
                    _readLength = _readOffset < _readLength ? 1 : 0;
                    _readOffset = 0;
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

        private void CompleteResponse()
        {
            Debug.Assert(_currentRequest != null, "Expected the connection to be associated with a request.");
            Debug.Assert(_writeOffset == 0, "Everything in write buffer should have been flushed.");

            // Disassociate the connection from a request.
            _currentRequest = null;

            // If we have extraneous data in the read buffer, don't reuse the connection;
            // otherwise we'd interpret this as part of the next response. Plus, we may
            // have been using a temporary buffer to read this erroneous data, and thus
            // may not even have it any more.
            if (_readLength != _readOffset)
            {
                if (NetEventSource.IsEnabled)
                {
                    Trace("Unexpected data on connection after response read.");
                }

                _readOffset = _readLength = 0;
                _connectionClose = true;
            }

            // If the connection is no longer in use (i.e. for NT authentication), then we can return it to the pool now.
            // Otherwise, it will be returned when the connection is no longer in use (i.e. Release above is called).
            if (!_inUse)
            {
                ReturnConnectionToPool();
            }
        }

        public async Task DrainResponseAsync(HttpResponseMessage response)
        {
            Debug.Assert(_inUse);

            if (_connectionClose)
            {
                throw new HttpRequestException(SR.net_http_authconnectionfailure);
            }

            Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            HttpContentReadStream responseStream = stream as HttpContentReadStream;

            Debug.Assert(responseStream != null || stream is EmptyReadStream);

            if (responseStream != null && responseStream.NeedsDrain)
            {
                Debug.Assert(response.RequestMessage == _currentRequest);

                if (!await responseStream.DrainAsync(_pool.Settings._maxResponseDrainSize).ConfigureAwait(false) ||
                    _connectionClose)       // Draining may have set this
                {
                    throw new HttpRequestException(SR.net_http_authconnectionfailure);
                }
            }

            Debug.Assert(_currentRequest == null);

            response.Dispose();
        }

        private void ReturnConnectionToPool()
        {
            Debug.Assert(_currentRequest == null, "Connection should no longer be associated with a request.");
            Debug.Assert(_readAheadTask == null, "Expected a previous initial read to already be consumed.");
            Debug.Assert(RemainingBuffer.Length == 0, "Unexpected data in connection read buffer.");

            // If we decided not to reuse the connection (either because the server sent Connection: close,
            // or there was some other problem while processing the request that makes the connection unusable),
            // don't put the connection back in the pool.
            if (_connectionClose)
            {
                if (NetEventSource.IsEnabled)
                {
                    Trace("Connection will not be reused.");
                }

                // We're not putting the connection back in the pool. Dispose it.
                Dispose();
            }
            else
            {
                // Put connection back in the pool.
                _pool.ReturnConnection(this);
            }
        }

        private static bool EqualsOrdinal(string left, Span<byte> right)
        {
            Debug.Assert(left != null, "Expected non-null string");

            if (left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }

        public sealed override string ToString() => $"{nameof(HttpConnection)}({_pool})"; // Description for diagnostic purposes

        internal sealed override void Trace(string message, [CallerMemberName] string memberName = null) =>
            NetEventSource.Log.HandlerMessage(
                _pool?.GetHashCode() ?? 0,    // pool ID
                GetHashCode(),                // connection ID
                _currentRequest?.GetHashCode() ?? 0,  // request ID
                memberName,                   // method name
                ToString() + ": " + message); // message
    }

    internal sealed class HttpConnectionWithFinalizer : HttpConnection
    {
        public HttpConnectionWithFinalizer(HttpConnectionPool pool, Socket socket, Stream stream, TransportContext transportContext) : base(pool, socket, stream, transportContext) { }

        // This class is separated from HttpConnection so we only pay the price of having a finalizer
        // when it's actually needed, e.g. when MaxConnectionsPerServer is enabled.
        ~HttpConnectionWithFinalizer() => Dispose(disposing: false);
    }
}
