// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal partial class HttpConnection : IDisposable
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
        /// Delay after which we'll send the request payload for ExpectContinue if
        /// the server hasn't yet responded.
        /// </summary>
        private const int Expect100TimeoutMilliseconds = 1000;
        /// <summary>
        /// Size after which we'll close the connection rather than send the payload in response
        /// to final error status code sent by the server when using Expect: 100-continue.
        /// </summary>
        private const int Expect100ErrorSendThreshold = 1024;

        private static readonly byte[] s_contentLength0NewlineAsciiBytes = Encoding.ASCII.GetBytes("Content-Length: 0\r\n");
        private static readonly byte[] s_spaceHttp10NewlineAsciiBytes = Encoding.ASCII.GetBytes(" HTTP/1.0\r\n");
        private static readonly byte[] s_spaceHttp11NewlineAsciiBytes = Encoding.ASCII.GetBytes(" HTTP/1.1\r\n");
        private static readonly byte[] s_hostKeyAndSeparator = Encoding.ASCII.GetBytes(HttpKnownHeaderNames.Host + ": ");
        private static readonly byte[] s_httpSchemeAndDelimiter = Encoding.ASCII.GetBytes(Uri.UriSchemeHttp + Uri.SchemeDelimiter);
        private static readonly byte[] s_http1DotBytes = Encoding.ASCII.GetBytes("HTTP/1.");
        private static readonly ulong s_http10Bytes = BitConverter.ToUInt64(Encoding.ASCII.GetBytes("HTTP/1.0"));
        private static readonly ulong s_http11Bytes = BitConverter.ToUInt64(Encoding.ASCII.GetBytes("HTTP/1.1"));
        private static readonly string s_cancellationMessage = new OperationCanceledException().Message; // use same message as the default ctor

        private readonly HttpConnectionPool _pool;
        private readonly Stream _stream;
        private readonly TransportContext _transportContext;
        private readonly bool _usingProxy;
        private readonly byte[] _idnHostAsciiBytes;
        private readonly WeakReference<HttpConnection> _weakThisRef;

        private HttpRequestMessage _currentRequest;
        private Task _sendRequestContentTask;
        private readonly byte[] _writeBuffer;
        private int _writeOffset;
        private Exception _pendingException;
        private int _allowedReadLineBytes;

        private Task<int> _readAheadTask;
        private byte[] _readBuffer;
        private int _readOffset;
        private int _readLength;

        private bool _canRetry;
        private bool _connectionClose; // Connection: close was seen on last response
        private int _disposed; // 1 yes, 0 no

        public HttpConnection(
            HttpConnectionPool pool,
            Stream stream, 
            TransportContext transportContext)
        {
            Debug.Assert(pool != null);
            Debug.Assert(stream != null);

            _pool = pool;
            _stream = stream;
            _transportContext = transportContext;
            _usingProxy = pool.UsingProxy;
            _idnHostAsciiBytes = pool.IdnHostAsciiBytes;

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
                }
            }
        }

        public bool ReadAheadCompleted
        {
            get
            {
                Debug.Assert(_readAheadTask != null, $"{nameof(_readAheadTask)} should have been initialized");
                return _readAheadTask.IsCompleted;
            }
        }

        public bool IsNewConnection
        {
            get
            {
                // This is only valid when we are not actually processing a request.
                Debug.Assert(_currentRequest == null);
                return (_readAheadTask == null);
            }
        }

        public bool CanRetry
        {
            get
            {
                // Should only be called when we have been disposed.
                Debug.Assert(_disposed != 0);
                return _canRetry;
            }
        }

        public DateTimeOffset CreationTime { get; } = DateTimeOffset.UtcNow;

        private async Task WriteHeadersAsync(HttpHeaders headers, string cookiesFromContainer)
        {
            foreach (KeyValuePair<string, IEnumerable<string>> header in headers)
            {
                await WriteAsciiStringAsync(header.Key).ConfigureAwait(false);
                await WriteTwoBytesAsync((byte)':', (byte)' ').ConfigureAwait(false);

                var values = (string[])header.Value; // typed as IEnumerable<string>, but always a string[]
                Debug.Assert(values.Length > 0, "No values for header??");
                if (values.Length > 0)
                {
                    await WriteStringAsync(values[0]).ConfigureAwait(false);

                    if (cookiesFromContainer != null && header.Key == HttpKnownHeaderNames.Cookie)
                    {
                        await WriteTwoBytesAsync((byte)';', (byte)' ').ConfigureAwait(false);
                        await WriteStringAsync(cookiesFromContainer).ConfigureAwait(false);

                        cookiesFromContainer = null;
                    }

                    for (int i = 1; i < values.Length; i++)
                    {
                        await WriteTwoBytesAsync((byte)',', (byte)' ').ConfigureAwait(false);
                        await WriteStringAsync(values[i]).ConfigureAwait(false);
                    }
                }

                await WriteTwoBytesAsync((byte)'\r', (byte)'\n').ConfigureAwait(false);
            }

            if (cookiesFromContainer != null)
            {
                await WriteAsciiStringAsync(HttpKnownHeaderNames.Cookie).ConfigureAwait(false);
                await WriteTwoBytesAsync((byte)':', (byte)' ').ConfigureAwait(false);
                await WriteAsciiStringAsync(cookiesFromContainer).ConfigureAwait(false);
                await WriteTwoBytesAsync((byte)'\r', (byte)'\n').ConfigureAwait(false);
            }
        }

        private async Task WriteHostHeaderAsync(Uri uri)
        {
            await WriteBytesAsync(s_hostKeyAndSeparator).ConfigureAwait(false);

            await (_idnHostAsciiBytes != null ?
                WriteBytesAsync(_idnHostAsciiBytes) :
                WriteAsciiStringAsync(uri.IdnHost)).ConfigureAwait(false);

            if (!uri.IsDefaultPort)
            {
                await WriteByteAsync((byte)':').ConfigureAwait(false);
                await WriteFormattedInt32Async(uri.Port).ConfigureAwait(false);
            }

            await WriteTwoBytesAsync((byte)'\r', (byte)'\n').ConfigureAwait(false);
        }

        private Task WriteFormattedInt32Async(int value)
        {
            // Try to format into our output buffer directly.
            if (Utf8Formatter.TryFormat(value, new Span<byte>(_writeBuffer, _writeOffset, _writeBuffer.Length - _writeOffset), out int bytesWritten))
            {
                _writeOffset += bytesWritten;
                return Task.CompletedTask;
            }

            // If we don't have enough room, do it the slow way.
            return WriteAsciiStringAsync(value.ToString(CultureInfo.InvariantCulture));
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            TaskCompletionSource<bool> allowExpect100ToContinue = null;
            Debug.Assert(_currentRequest == null, $"Expected null {nameof(_currentRequest)}.");
            _currentRequest = request;

            Debug.Assert(!_canRetry);
            _canRetry = true;

            // Send the request.
            if (NetEventSource.IsEnabled) Trace($"Sending request: {request}");
            CancellationTokenRegistration cancellationRegistration = RegisterCancellation(cancellationToken);
            try
            {
                // Write request line
                await WriteStringAsync(request.Method.Method).ConfigureAwait(false);
                await WriteByteAsync((byte)' ').ConfigureAwait(false);

                if (_usingProxy)
                {
                    // Proxied requests contain full URL
                    Debug.Assert(request.RequestUri.Scheme == Uri.UriSchemeHttp);
                    await WriteBytesAsync(s_httpSchemeAndDelimiter).ConfigureAwait(false);
                    await WriteAsciiStringAsync(request.RequestUri.IdnHost).ConfigureAwait(false);
                }

                await WriteStringAsync(request.RequestUri.PathAndQuery).ConfigureAwait(false);

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

                // Write request headers
                if (request.HasHeaders || cookiesFromContainer != null)
                {
                    await WriteHeadersAsync(request.Headers, cookiesFromContainer).ConfigureAwait(false);
                }

                if (request.Content == null)
                {
                    // Write out Content-Length: 0 header to indicate no body,
                    // unless this is a method that never has a body.
                    if (request.Method != HttpMethod.Get && request.Method != HttpMethod.Head)
                    {
                        await WriteBytesAsync(s_contentLength0NewlineAsciiBytes).ConfigureAwait(false);
                    }
                }
                else
                {
                    // Write content headers
                    await WriteHeadersAsync(request.Content.Headers, cookiesFromContainer: null).ConfigureAwait(false);
                }

                // Write special additional headers.  If a host isn't in the headers list, then a Host header
                // wasn't sent, so as it's required by HTTP 1.1 spec, send one based on the Request Uri.
                if (!request.HasHeaders || request.Headers.Host == null)
                {
                    await WriteHostHeaderAsync(request.RequestUri).ConfigureAwait(false);
                }

                // CRLF for end of headers.
                await WriteTwoBytesAsync((byte)'\r', (byte)'\n').ConfigureAwait(false);

                Debug.Assert(_sendRequestContentTask == null);
                if (request.Content == null)
                {
                    // We have nothing more to send, so flush out any headers we haven't yet sent.
                    await FlushAsync().ConfigureAwait(false);
                }
                else
                {
                    // Asynchronously send the body if there is one.  This can run concurrently with receiving
                    // the response. The write content streams will handle ensuring appropriate flushes are done
                    // to ensure the headers and content are sent.
                    bool transferEncodingChunked = request.HasHeaders && request.Headers.TransferEncodingChunked == true;
                    HttpContentWriteStream stream = transferEncodingChunked ? (HttpContentWriteStream)
                        new ChunkedEncodingWriteStream(this) :
                        new ContentLengthWriteStream(this);

                    if (!request.HasHeaders || request.Headers.ExpectContinue != true)
                    {
                        // Send the request content asynchronously.  Note that elsewhere in SendAsync we don't pass
                        // the cancellation token around, as we simply register with it for the duration of the
                        // method in order to dispose of this connection and wake up any operations.  But SendRequestContentAsync
                        // is special in that it ends up dealing with an external entity, the request HttpContent provided
                        // by the caller to this handler, and we could end up blocking as part of getting that content,
                        // which won't be affected by disposing this connection. Thus, we do pass the token in here.
                        Task sendTask = _sendRequestContentTask = SendRequestContentAsync(request, stream, cancellationToken);
                        if (sendTask.IsFaulted)
                        {
                            // Technically this isn't necessary: if the task failed, it will have stored the exception
                            // and disposed of the stream, which will cause subsequent reads to fail.  This is also
                            // only special-casing the case where the operation fails synchronously or at least very
                            // quickly.  But it results in slightly nicer flow, and since we can handle this case,
                            // we may as well do so.
                            _sendRequestContentTask = null;
                            sendTask.GetAwaiter().GetResult();
                        }
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
                            allowExpect100ToContinue, TimeSpan.FromMilliseconds(Expect100TimeoutMilliseconds), Timeout.InfiniteTimeSpan);
                        _sendRequestContentTask = SendRequestContentWithExpect100ContinueAsync(
                            request, allowExpect100ToContinue.Task, stream, expect100Timer, cancellationToken);
                    }
                }

                // Start to read response.
                _allowedReadLineBytes = _pool.Settings._maxResponseHeadersLength * 1024;

                // We should not have any buffered data here; if there was, it should have been treated as an error
                // by the previous request handling.  (Note we do not support HTTP pipelining.)
                Debug.Assert(_readOffset == _readLength);

                // When the connection was put back into the pool, a pre-emptive read was performed
                // into the read buffer.  That read should not complete prior to us using the
                // connection again, as that would mean the connection was either closed or had
                // erroneous data sent on it by the server in response to no request from us.
                // We need to consume that read prior to issuing another read request.
                Task<int> t = _readAheadTask;
                if (t != null)
                {
                    _readAheadTask = null;

                    int bytesRead = await t.ConfigureAwait(false);
                    if (NetEventSource.IsEnabled) Trace($"Received {bytesRead} bytes.");

                    if (bytesRead == 0)
                    {
                        throw new IOException(SR.net_http_invalid_response);
                    }

                    _readOffset = 0;
                    _readLength = bytesRead;
                }

                // The request is no longer retryable; either we received data from the _readAheadTask, 
                // or there was no _readAheadTask because this is the first request on the connection.
                _canRetry = false;

                // Parse the response status line.
                var response = new HttpResponseMessage() { RequestMessage = request, Content = new HttpConnectionResponseContent() };
                ParseStatusLine(await ReadNextLineAsync().ConfigureAwait(false), response);

                // If we sent an Expect: 100-continue header, handle the response accordingly.
                if (allowExpect100ToContinue != null)
                {
                    if ((int)response.StatusCode >= 300 &&
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
                        // For any success or informational status codes (including 100 continue), send the payload.
                        allowExpect100ToContinue.TrySetResult(true);

                        // And if this was 100 continue, deal with the extra headers.
                        if (response.StatusCode == HttpStatusCode.Continue)
                        {
                            // We got our continue header.  Read the subsequent empty line and parse the additional status line.
                            if (!LineIsEmpty(await ReadNextLineAsync().ConfigureAwait(false)))
                            {
                                ThrowInvalidHttpResponse();
                            }

                            ParseStatusLine(await ReadNextLineAsync().ConfigureAwait(false), response);
                        }
                    }
                }

                // Parse the response headers.
                while (true)
                {
                    ArraySegment<byte> line = await ReadNextLineAsync().ConfigureAwait(false);
                    if (LineIsEmpty(line))
                    {
                        break;
                    }
                    ParseHeaderNameValue(line, response);
                }

                // Determine whether we need to force close the connection when the request/response has completed.
                if (response.Headers.ConnectionClose.GetValueOrDefault())
                {
                    _connectionClose = true;
                }

                // Before creating the response stream, check to see if we're done sending any content,
                // and propagate any exceptions that may have occurred.  The most common case is that
                // the server won't send back response content until it's received the whole request,
                // so the majority of the time this task will be complete.
                Task sendRequestContentTask = _sendRequestContentTask;
                if (sendRequestContentTask != null && sendRequestContentTask.IsCompleted)
                {
                    sendRequestContentTask.GetAwaiter().GetResult();
                    _sendRequestContentTask = null;
                }

                // We're about to create the response stream, at which point responsibility for canceling
                // the remainder of the response lies with the stream.  Thus we dispose of our registration
                // here (if an exception has occurred or does occur while creating/returning the stream,
                // we'll still dispose of it in the catch below as part of Dispose'ing the connection).
                cancellationRegistration.Dispose();
                cancellationToken.ThrowIfCancellationRequested(); // in case cancellation may have disposed of the stream

                // Create the response stream.
                HttpContentStream responseStream;
                if (request.Method == HttpMethod.Head || (int)response.StatusCode == 204 || (int)response.StatusCode == 304)
                {
                    responseStream = EmptyReadStream.Instance;
                    ReturnConnectionToPool();
                }
                else if (response.Content.Headers.ContentLength != null)
                {
                    long contentLength = response.Content.Headers.ContentLength.GetValueOrDefault();
                    if (contentLength <= 0)
                    {
                        responseStream = EmptyReadStream.Instance;
                        ReturnConnectionToPool();
                    }
                    else
                    {
                        responseStream = new ContentLengthReadStream(this, (ulong)contentLength);
                    }
                }
                else if (response.Headers.TransferEncodingChunked == true)
                {
                    responseStream = new ChunkedEncodingReadStream(this);
                }
                else if (response.StatusCode == HttpStatusCode.SwitchingProtocols)
                {
                    responseStream = new RawConnectionStream(this);
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

                if (ShouldWrapInOperationCanceledException(error, cancellationToken))
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
                    throw CreateOperationCanceledException(_pendingException ?? error, cancellationToken);
                }
                else if (_pendingException != null)
                {
                    // If we incurred an exception in non-linear control flow such that
                    // the exception didn't bubble up here (e.g. concurrent sending of
                    // the request content), use that error instead.
                    throw new HttpRequestException(SR.net_http_client_execution_error, _pendingException);
                }
                else if (error is InvalidOperationException || error is IOException)
                {
                    // If it's an InvalidOperationException or an IOException, for consistency
                    // with other handlers we wrap the exception in an HttpRequestException.
                    throw new HttpRequestException(SR.net_http_client_execution_error, error);
                }
                else
                {
                    // Otherwise, just allow the original exception to propagate.
                    throw;
                }
            }
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

        private static bool ShouldWrapInOperationCanceledException(Exception error, CancellationToken cancellationToken) =>
            !(error is OperationCanceledException) && cancellationToken.IsCancellationRequested;

        private static Exception CreateOperationCanceledException(Exception error, CancellationToken cancellationToken) =>
            new OperationCanceledException(s_cancellationMessage, error, cancellationToken);

        private static bool LineIsEmpty(ArraySegment<byte> line) => line.Count == 0;

        private async Task SendRequestContentAsync(HttpRequestMessage request, HttpContentWriteStream stream, CancellationToken cancellationToken)
        {
            // Now that we're sending content, prohibit retries on this connection.
            _canRetry = false;

            try
            {
                // Copy all of the data to the server.
                await request.Content.CopyToAsync(stream, _transportContext, cancellationToken).ConfigureAwait(false);

                // Finish the content; with a chunked upload, this includes writing the terminating chunk.
                await stream.FinishAsync().ConfigureAwait(false);

                // Flush any content that might still be buffered.
                await FlushAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _pendingException = e;
                if (NetEventSource.IsEnabled) Trace($"Error while sending request content: {e}");
                Dispose();
                throw;
            }
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

        // TODO: Remove this overload once https://github.com/dotnet/roslyn/issues/17287 is addressed
        // and the compiler doesn't lift the span temporary from the call site into the async state
        // machine in debug builds.
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
                ThrowInvalidHttpResponse();
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
                    ThrowInvalidHttpResponse();
                }
            }

            // Set the status code
            byte status1 = line[9], status2 = line[10], status3 = line[11];
            if (!IsDigit(status1) || !IsDigit(status2) || !IsDigit(status3))
            {
                ThrowInvalidHttpResponse();
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
                        ThrowInvalidHttpResponse(error);
                    }
                }
            }
            else
            {
                ThrowInvalidHttpResponse();
            }
        }

        // TODO: Remove this overload once https://github.com/dotnet/roslyn/issues/17287 is addressed
        // and the compiler doesn't lift the span temporary from the call site into the async state
        // machine in debug builds.
        private static void ParseHeaderNameValue(ArraySegment<byte> line, HttpResponseMessage response) =>
            ParseHeaderNameValue((Span<byte>)line, response);

        private static void ParseHeaderNameValue(Span<byte> line, HttpResponseMessage response)
        {
            Debug.Assert(line.Length > 0);

            int pos = 0;
            while (line[pos] != (byte)':' && line[pos] != (byte)' ')
            {
                pos++;
                if (pos == line.Length)
                {
                    // Invalid header line that doesn't contain ':'.
                    ThrowInvalidHttpResponse();
                }
            }

            if (pos == 0)
            {
                // Invalid empty header name.
                ThrowInvalidHttpResponse();
            }

            if (!HeaderDescriptor.TryGet(line.Slice(0, pos), out HeaderDescriptor descriptor))
            {
                // Invalid header name
                ThrowInvalidHttpResponse();
            }

            // Eat any trailing whitespace
            while (line[pos] == (byte)' ')
            {
                pos++;
                if (pos == line.Length)
                {
                    // Invalid header line that doesn't contain ':'.
                    ThrowInvalidHttpResponse();
                }
            }

            if (line[pos++] != ':')
            {
                // Invalid header line that doesn't contain ':'.
                ThrowInvalidHttpResponse();
            }

            // Skip whitespace after colon
            while (pos < line.Length && (line[pos] == (byte)' ' || line[pos] == (byte)'\t'))
            {
                pos++;
            }

            string headerValue = descriptor.GetHeaderValue(line.Slice(pos));

            // Note we ignore the return value from TryAddWithoutValidation; 
            // if the header can't be added, we silently drop it.
            if (descriptor.HeaderType == HttpHeaderType.Content)
            {
                response.Content.Headers.TryAddWithoutValidation(descriptor, headerValue);
            }
            else
            {
                response.Headers.TryAddWithoutValidation(descriptor, headerValue);
            }
        }

        private static bool IsDigit(byte c) => (uint)(c - '0') <= '9' - '0';

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
                // CONSIDER: May want to be a bit smarter here?  Think about how large writes should work...
                await WriteToStreamAsync(source).ConfigureAwait(false);
            }
            else
            {
                // Copy remainder into buffer
                WriteToBuffer(source);
            }
        }

        private Task WriteWithoutBufferingAsync(ReadOnlyMemory<byte> source)
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
            return FlushThenWriteWithoutBufferingAsync(source);
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

        private Task FlushAsync()
        {
            if (_writeOffset > 0)
            {
                Task t = WriteToStreamAsync(new ReadOnlyMemory<byte>(_writeBuffer, 0, _writeOffset));
                _writeOffset = 0;
                return t;
            }
            return Task.CompletedTask;
        }

        private Task WriteToStreamAsync(ReadOnlyMemory<byte> source)
        {
            if (NetEventSource.IsEnabled) Trace($"Writing {source.Length} bytes.");
            return _stream.WriteAsync(source);
        }

        private async ValueTask<ArraySegment<byte>> ReadNextLineAsync()
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
                    if (length > 0 && _readBuffer[startIndex + length - 1] == '\r')
                    {
                        length--;
                    }

                    // Advance read position past the LF
                    _allowedReadLineBytes -= lfIndex + 1 - scanOffset;
                    if (_allowedReadLineBytes < 0)
                    {
                        ThrowInvalidHttpResponse();
                    }
                    _readOffset = lfIndex + 1;

                    return new ArraySegment<byte>(_readBuffer, startIndex, length);
                }

                // Couldn't find LF.  Read more.
                // Note this may cause _readOffset to change.
                previouslyScannedBytes = _readLength - _readOffset;
                _allowedReadLineBytes -= _readLength - scanOffset;
                if (_allowedReadLineBytes < 0)
                {
                    ThrowInvalidHttpResponse();
                }
                await FillAsync().ConfigureAwait(false);
            }
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

                byte[] newReadBuffer = new byte[_readBuffer.Length * 2];
                Buffer.BlockCopy(_readBuffer, 0, newReadBuffer, 0, remaining);
                _readBuffer = newReadBuffer;
                _readOffset = 0;
                _readLength = remaining;
            }

            int bytesRead = await _stream.ReadAsync(new Memory<byte>(_readBuffer, _readLength, _readBuffer.Length - _readLength)).ConfigureAwait(false);

            if (NetEventSource.IsEnabled) Trace($"Received {bytesRead} bytes.");
            if (bytesRead == 0)
            {
                throw new IOException(SR.net_http_invalid_response);
            }

            _readLength += bytesRead;
        }

        private void ReadFromBuffer(Span<byte> buffer)
        {
            Debug.Assert(buffer.Length <= _readLength - _readOffset);

            new Span<byte>(_readBuffer, _readOffset, buffer.Length).CopyTo(buffer);
            _readOffset += buffer.Length;
        }

        private async ValueTask<int> ReadAsync(Memory<byte> destination)
        {
            // This is called when reading the response body

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

        private async Task CopyFromBufferAsync(Stream destination, int count)
        {
            Debug.Assert(count <= _readLength - _readOffset);

            if (NetEventSource.IsEnabled) Trace($"Copying {count} bytes to stream.");
            await destination.WriteAsync(_readBuffer, _readOffset, count).ConfigureAwait(false);
            _readOffset += count;
        }

        private async Task CopyToAsync(Stream destination)
        {
            Debug.Assert(destination != null);

            int remaining = _readLength - _readOffset;
            if (remaining > 0)
            {
                await CopyFromBufferAsync(destination, remaining).ConfigureAwait(false);
            }

            while (true)
            {
                _readOffset = 0;

                // Don't use FillAsync here as it will throw on EOF.
                Debug.Assert(_readAheadTask == null);
                _readLength = await _stream.ReadAsync(_readBuffer).ConfigureAwait(false);
                if (_readLength == 0)
                {
                    // End of stream
                    break;
                }

                await CopyFromBufferAsync(destination, _readLength).ConfigureAwait(false);
            }
        }

        // Copy *exactly* [length] bytes into destination; throws on end of stream.
        private async Task CopyToAsync(Stream destination, ulong length)
        {
            Debug.Assert(destination != null);
            Debug.Assert(length > 0);

            int remaining = _readLength - _readOffset;
            if (remaining > 0)
            {
                if ((ulong)remaining > length)
                {
                    remaining = (int)length;
                }
                await CopyFromBufferAsync(destination, remaining).ConfigureAwait(false);

                length -= (ulong)remaining;
                if (length == 0)
                {
                    return;
                }
            }

            while (true)
            {
                await FillAsync().ConfigureAwait(false);

                remaining = (ulong)_readLength < length ? _readLength : (int)length;
                await CopyFromBufferAsync(destination, remaining).ConfigureAwait(false);

                length -= (ulong)remaining;
                if (length == 0)
                {
                    return;
                }
            }
        }

        private void ReturnConnectionToPool()
        {
            Debug.Assert(_readAheadTask == null, "Expected a previous initial read to already be consumed.");
            Debug.Assert(_currentRequest != null, "Expected the connection to be associated with a request.");

            // Disassociate the connection from a request.  If there's an in-flight request content still
            // being sent, it'll see this nulled out and stop sending.  Also clear out other request-specific content.
            _currentRequest = null;
            _pendingException = null;

            // Check to see if we're still sending request content.
            Task sendRequestContentTask = _sendRequestContentTask;
            if (sendRequestContentTask != null)
            {
                if (!sendRequestContentTask.IsCompleted)
                {
                    // We're still transferring request content.  Only put the connection back into the
                    // pool when we're done transferring.
                    if (NetEventSource.IsEnabled) Trace("Still transferring request content. Delaying returning connection to pool.");
                    sendRequestContentTask.ContinueWith((_, state) =>
                    {
                        var innerConnection = (HttpConnection)state;
                        if (NetEventSource.IsEnabled) innerConnection.Trace("Request content send completed.");
                        innerConnection.ReturnConnectionToPoolCore();
                    }, this, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
                    return;
                }

                // We're done transferring request content.  Check whether we incurred an exception,
                // and if we did, propagate it to our caller.
                if (!sendRequestContentTask.IsCompletedSuccessfully)
                {
                    sendRequestContentTask.GetAwaiter().GetResult();
                }
            }

            ReturnConnectionToPoolCore();
        }

        private void ReturnConnectionToPoolCore()
        {
            Debug.Assert(_sendRequestContentTask == null || _sendRequestContentTask.IsCompleted);
            Debug.Assert(_writeOffset == 0, "Everything in write buffer should have been flushed.");

            if (NetEventSource.IsEnabled)
            {
                if (_connectionClose)
                {
                    Trace("Server requested connection be closed.");
                }
                if (_sendRequestContentTask != null && _sendRequestContentTask.IsFaulted)
                {
                    Trace($"Sending request content incurred an exception: {_sendRequestContentTask.Exception.InnerException}");
                }
            }

            // If server told us it's closing the connection, don't put this back in the pool.
            // And if we incurred an error while transferring request content, also skip the pool.
            if (!_connectionClose &&
                (_sendRequestContentTask == null || _sendRequestContentTask.IsCompletedSuccessfully))
            {
                try
                {
                    // Any remaining request content has completed successfully.  Drop it.
                    _sendRequestContentTask = null;

                    // When putting a connection back into the pool, we initiate a pre-emptive
                    // read on the stream.  When the connection is subsequently taken out of the
                    // pool, this can be used in place of the first read on the stream that would
                    // otherwise be done.  But by doing it now, we can check the status of the read
                    // at any point to understand if the connection has been closed or if errant data
                    // has been sent on the connection by the server, either of which would mean we
                    // should close the connection and not use it for subsequent requests.
                    Debug.Assert(_readLength == _readOffset, $"{_readLength} != {_readOffset}");
                    _readAheadTask = _stream.ReadAsync(_readBuffer, 0, _readBuffer.Length);

                    // Put connection back in the pool.
                    _pool.ReturnConnection(this);
                    return;
                }
                catch
                {
                    // If reading throws, eat the error and don't pool the connection.
                }
            }

            // We're not putting the connection back in the pool. Dispose it.
            Dispose();
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

        private static void ThrowInvalidHttpResponse() => throw new HttpRequestException(SR.net_http_invalid_response);

        private static void ThrowInvalidHttpResponse(Exception innerException) => throw new HttpRequestException(SR.net_http_invalid_response, innerException);

        internal void Trace(string message, [CallerMemberName] string memberName = null) =>
            NetEventSource.Log.HandlerMessage(
                _pool?.GetHashCode() ?? 0,    // pool ID
                GetHashCode(),                // connection ID
                _currentRequest?.GetHashCode() ?? 0,  // request ID
                memberName,                   // method name
                ToString() + ": " + message); // message
    }

    internal sealed class HttpConnectionWithFinalizer : HttpConnection
    {
        public HttpConnectionWithFinalizer(HttpConnectionPool pool, Stream stream, TransportContext transportContext) : base(pool, stream, transportContext) { }

        // This class is separated from HttpConnection so we only pay the price of having a finalizer
        // when it's actually needed, e.g. when MaxConnectionsPerServer is enabled.
        ~HttpConnectionWithFinalizer() => Dispose(disposing: false);
    }
}
