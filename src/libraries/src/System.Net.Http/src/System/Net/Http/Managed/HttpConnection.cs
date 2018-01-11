// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed partial class HttpConnection : IDisposable
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

        private readonly HttpConnectionPool _pool;
        private readonly HttpConnectionKey _key;
        private readonly Stream _stream;
        private readonly TransportContext _transportContext;
        private readonly bool _usingProxy;
        private readonly byte[] _idnHostAsciiBytes;

        private HttpRequestMessage _currentRequest;
        private Task _sendRequestContentTask;
        private readonly byte[] _writeBuffer;
        private int _writeOffset;
        private Exception _pendingException;

        private Task<int> _readAheadTask;
        private byte[] _readBuffer;
        private int _readOffset;
        private int _readLength;

        private bool _connectionClose; // Connection: close was seen on last response
        private int _disposed; // 1 yes, 0 no

        public HttpConnection(
            HttpConnectionPool pool,
            HttpConnectionKey key,
            string requestIdnHost,
            Stream stream, 
            TransportContext transportContext, 
            bool usingProxy)
        {
            Debug.Assert(pool != null);
            Debug.Assert(stream != null);

            _pool = pool;
            _key = key;
            _stream = stream;
            _transportContext = transportContext;
            _usingProxy = usingProxy;
            if (requestIdnHost != null)
            {
                _idnHostAsciiBytes = Encoding.ASCII.GetBytes(requestIdnHost);
            }

            _writeBuffer = new byte[InitialWriteBufferSize];
            _readBuffer = new byte[InitialReadBufferSize];

            if (NetEventSource.IsEnabled)
            {
                if (_stream is SslStream sslStream)
                {
                    Trace(
                        $"Secure connection created to {key.Host}:{key.Port}. " +
                        $"SslProtocol:{sslStream.SslProtocol}, " +
                        $"CipherAlgorithm:{sslStream.CipherAlgorithm}, CipherStrength:{sslStream.CipherStrength}, " +
                        $"HashAlgorithm:{sslStream.HashAlgorithm}, HashStrength:{sslStream.HashStrength}, " +
                        $"KeyExchangeAlgorithm:{sslStream.KeyExchangeAlgorithm}, KeyExchangeStrength:{sslStream.KeyExchangeStrength}, " +
                        $"LocalCert:{sslStream.LocalCertificate}, RemoteCert:{sslStream.RemoteCertificate}");
                }
                else
                {
                    Trace($"Connection created to {key.Host}:{key.Port}.");
                }
            }
        }

        public void Dispose()
        {
            // Ensure we're only disposed once.  Dispose could be called concurrently, for example,
            // if the request and the response were running concurrently and both incurred an exception.
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                if (NetEventSource.IsEnabled) Trace("Connection closing.");
                _pool.DecrementConnectionCount();
                _stream.Dispose();
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

        private async Task WriteHeadersAsync(HttpHeaders headers, CancellationToken cancellationToken)
        {
            foreach (KeyValuePair<string, IEnumerable<string>> header in headers)
            {
                await WriteAsciiStringAsync(header.Key, cancellationToken).ConfigureAwait(false);
                await WriteTwoBytesAsync((byte)':', (byte)' ', cancellationToken).ConfigureAwait(false);

                var values = (string[])header.Value; // typed as IEnumerable<string>, but always a string[]
                Debug.Assert(values.Length > 0, "No values for header??");
                if (values.Length > 0)
                {
                    await WriteStringAsync(values[0], cancellationToken).ConfigureAwait(false);
                    for (int i = 1; i < values.Length; i++)
                    {
                        await WriteTwoBytesAsync((byte)',', (byte)' ', cancellationToken).ConfigureAwait(false);
                        await WriteStringAsync(values[i], cancellationToken).ConfigureAwait(false);
                    }
                }

                await WriteTwoBytesAsync((byte)'\r', (byte)'\n', cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task WriteHostHeaderAsync(Uri uri, CancellationToken cancellationToken)
        {
            await WriteBytesAsync(s_hostKeyAndSeparator, cancellationToken).ConfigureAwait(false);

            await (_idnHostAsciiBytes != null ?
                WriteBytesAsync(_idnHostAsciiBytes, cancellationToken) :
                WriteAsciiStringAsync(uri.IdnHost, cancellationToken)).ConfigureAwait(false);

            if (!uri.IsDefaultPort)
            {
                await WriteByteAsync((byte)':', cancellationToken).ConfigureAwait(false);
                await WriteFormattedInt32Async(uri.Port, cancellationToken).ConfigureAwait(false);
            }

            await WriteTwoBytesAsync((byte)'\r', (byte)'\n', cancellationToken).ConfigureAwait(false);
        }

        private Task WriteFormattedInt32Async(int value, CancellationToken cancellationToken)
        {
            const int MaxFormattedInt32Length = 10; // number of digits in int.MaxValue.ToString()

            // If the maximum possible number of digits fits in our buffer, we can format synchronously
            if (_writeOffset <= _writeBuffer.Length - MaxFormattedInt32Length)
            {
                if (value == 0)
                {
                    _writeBuffer[_writeOffset++] = (byte)'0';
                }
                else
                {
                    int initialOffset = _writeOffset;
                    while (value > 0)
                    {
                        value = Math.DivRem(value, 10, out int digit);
                        _writeBuffer[_writeOffset++] = (byte)('0' + digit);
                    }
                    Array.Reverse(_writeBuffer, initialOffset, _writeOffset - initialOffset);
                }
                return Task.CompletedTask;
            }

            // Otherwise, do it the slower way.
            return WriteAsciiStringAsync(value.ToString(CultureInfo.InvariantCulture), cancellationToken);
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            TaskCompletionSource<bool> allowExpect100ToContinue = null;
            Debug.Assert(_currentRequest == null, $"Expected null {nameof(_currentRequest)}.");
            _currentRequest = request;
            try
            {
                bool isHttp10 = request.Version.Major == 1 && request.Version.Minor == 0;

                // Send the request.
                if (NetEventSource.IsEnabled) Trace($"Sending request: {request}");

                // Add headers to define content transfer, if not present
                if (request.Content != null &&
                    (!request.HasHeaders || request.Headers.TransferEncodingChunked != true) &&
                    request.Content.Headers.ContentLength == null)
                {
                    // We have content, but neither Transfer-Encoding or Content-Length is set.
                    request.Headers.TransferEncodingChunked = true;
                }

                if (isHttp10 && request.HasHeaders && request.Headers.TransferEncodingChunked == true)
                {
                    // HTTP 1.0 does not support chunking
                    throw new NotSupportedException(SR.net_http_unsupported_chunking);
                }

                // Write request line
                await WriteStringAsync(request.Method.Method, cancellationToken).ConfigureAwait(false);
                await WriteByteAsync((byte)' ', cancellationToken).ConfigureAwait(false);
                await WriteStringAsync(
                    _usingProxy ? request.RequestUri.AbsoluteUri : request.RequestUri.PathAndQuery,
                    cancellationToken).ConfigureAwait(false);

                // fall-back to 1.1 for all versions other than 1.0
                await WriteBytesAsync(isHttp10 ? s_spaceHttp10NewlineAsciiBytes : s_spaceHttp11NewlineAsciiBytes,
                                      cancellationToken).ConfigureAwait(false);

                // Write request headers
                if (request.HasHeaders)
                {
                    await WriteHeadersAsync(request.Headers, cancellationToken).ConfigureAwait(false);
                }

                if (request.Content == null)
                {
                    // Write out Content-Length: 0 header to indicate no body,
                    // unless this is a method that never has a body.
                    if (request.Method != HttpMethod.Get && request.Method != HttpMethod.Head)
                    {
                        await WriteBytesAsync(s_contentLength0NewlineAsciiBytes, cancellationToken).ConfigureAwait(false);
                    }
                }
                else
                {
                    // Write content headers
                    await WriteHeadersAsync(request.Content.Headers, cancellationToken).ConfigureAwait(false);
                }

                // Write special additional headers.  If a host isn't in the headers list, then a Host header
                // wasn't sent, so as it's required by HTTP 1.1 spec, send one based on the Request Uri.
                if (!request.HasHeaders || request.Headers.Host == null)
                {
                    await WriteHostHeaderAsync(request.RequestUri, cancellationToken).ConfigureAwait(false);
                }

                // CRLF for end of headers.
                await WriteTwoBytesAsync((byte)'\r', (byte)'\n', cancellationToken).ConfigureAwait(false);

                Debug.Assert(_sendRequestContentTask == null);
                if (request.Content == null)
                {
                    // We have nothing more to send, so flush out any headers we haven't yet sent.
                    await FlushAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // Asynchronously send the body if there is one.  This can run concurrently with receiving
                    // the response. The write content streams will handle ensuring appropriate flushes are done
                    // to ensure the headers and content are sent.
                    bool transferEncodingChunked = request.HasHeaders && request.Headers.TransferEncodingChunked == true;
                    HttpContentWriteStream stream = transferEncodingChunked ? (HttpContentWriteStream)
                        new ChunkedEncodingWriteStream(this, cancellationToken) :
                        new ContentLengthWriteStream(this, cancellationToken);

                    if (!request.HasHeaders || request.Headers.ExpectContinue != true)
                    {
                        // Start the copy from the request.  We do this here in case it synchronously throws
                        // an exception, e.g. StreamContent throwing for non-rewindable content, and because if
                        // we did it in SendRequestContentAsync, that exception would get trapped in the returned
                        // task... at that point, we might get stuck waiting to receive a response from the server
                        // that'll never come, as the server is still expecting us to send data.
                        _sendRequestContentTask = SendRequestContentAsync(request.Content.CopyToAsync(stream, _transportContext), stream);
                    }
                    else
                    {
                        // We're sending an Expect: 100-continue header. We need to flush headers so that the server receives
                        // all of them, and we need to do so before initiating the send, as once we do that, it effectively
                        // owns the right to write, and we don't want to concurrently be accessing the write buffer.
                        await FlushAsync(cancellationToken).ConfigureAwait(false);

                        // Create a TCS we'll use to block the request content from being sent, and create a timer that's used
                        // as a fail-safe to unblock the request content if we don't hear back from the server in a timely manner.
                        // Then kick off the request.  The TCS' result indicates whether content should be sent or not.
                        allowExpect100ToContinue = new TaskCompletionSource<bool>();
                        var expect100Timer = new Timer(
                            s => ((TaskCompletionSource<bool>)s).TrySetResult(true),
                            allowExpect100ToContinue, TimeSpan.FromMilliseconds(Expect100TimeoutMilliseconds), Timeout.InfiniteTimeSpan);
                        _sendRequestContentTask = SendRequestContentWithExpect100ContinueAsync(request, allowExpect100ToContinue.Task, stream, expect100Timer);
                    }
                }

                // Parse the response status line.
                var response = new HttpResponseMessage() { RequestMessage = request, Content = new HttpConnectionContent(CancellationToken.None) };
                ParseStatusLine(await ReadNextLineAsync(cancellationToken).ConfigureAwait(false), response);
                
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
                            // We got our continue header.  Read the subsequent \r\n and parse the additional status line.
                            if (!LineIsEmpty(await ReadNextLineAsync(cancellationToken).ConfigureAwait(false)))
                            {
                                ThrowInvalidHttpResponse();
                            }

                            ParseStatusLine(await ReadNextLineAsync(cancellationToken).ConfigureAwait(false), response);
                        }
                    }
                }

                // Parse the response headers.
                while (true)
                {
                    ArraySegment<byte> line = await ReadNextLineAsync(cancellationToken).ConfigureAwait(false);
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
                ((HttpConnectionContent)response.Content).SetStream(responseStream);

                if (NetEventSource.IsEnabled) Trace($"Received response: {response}");
                return response;
            }
            catch (Exception error)
            {
                // Make sure to complete the allowExpect100ToContinue task if it exists.
                allowExpect100ToContinue?.TrySetResult(false);

                if (NetEventSource.IsEnabled) Trace($"Error sending request: {error}");
                Dispose();

                if (_pendingException != null)
                {
                    // If we incurred an exception in non-linear control flow such that
                    // the exception didn't bubble up here (e.g. concurrent sending of
                    // the request content), use that error instead.
                    throw new HttpRequestException(SR.net_http_client_execution_error, _pendingException);
                }

                // Otherwise, propagate this exception, wrapping it if necessary to
                // match exception type expectations.
                if (error is InvalidOperationException || error is IOException)
                {
                    throw new HttpRequestException(SR.net_http_client_execution_error, error);
                }
                throw;
            }
        }

        private static bool LineIsEmpty(ArraySegment<byte> line)
        {
            Debug.Assert(line.Count >= 2, "Lines should always be \r\n terminated.");
            return line.Count == 2;
        }

        private async Task SendRequestContentAsync(Task copyTask, HttpContentWriteStream stream)
        {
            try
            {
                // Wait for all of the data to be copied to the server.
                await copyTask.ConfigureAwait(false);

                // Finish the content; with a chunked upload, this includes writing the terminating chunk.
                await stream.FinishAsync().ConfigureAwait(false);

                // Flush any content that might still be buffered.
                await FlushAsync(stream.RequestCancellationToken).ConfigureAwait(false);
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
            HttpRequestMessage request, Task<bool> allowExpect100ToContinueTask, HttpContentWriteStream stream, Timer expect100Timer)
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
                await SendRequestContentAsync(request.Content.CopyToAsync(stream, _transportContext), stream).ConfigureAwait(false);
            }
            else
            {
                if (NetEventSource.IsEnabled) Trace($"Canceling request content for Expect: 100-continue.");
            }
        }

        // TODO: Remove this overload once https://github.com/dotnet/roslyn/issues/17287 is addressed
        // and the compiler doesn't lift the span temporary from the call site into the async state
        // machine in debug builds.
        private void ParseStatusLine(ArraySegment<byte> line, HttpResponseMessage response) =>
            ParseStatusLine((Span<byte>)line, response);

        private void ParseStatusLine(Span<byte> line, HttpResponseMessage response)
        {
            if (line.Length < 14 || // "HTTP/1.1 123\r\n" with optional phrase before the crlf
                line[0] != 'H' ||
                line[1] != 'T' ||
                line[2] != 'T' ||
                line[3] != 'P' ||
                line[4] != '/' ||
                line[8] != ' ')
            {
                ThrowInvalidHttpResponse();
            }

            // Set the response HttpVersion and status code
            byte majorVersion = line[5], minorVersion = line[7];
            byte status1 = line[9], status2 = line[10], status3 = line[11];
            if (!IsDigit(majorVersion) || line[6] != (byte)'.' || !IsDigit(minorVersion) ||
                !IsDigit(status1) || !IsDigit(status2) || !IsDigit(status3))
            {
                ThrowInvalidHttpResponse();
            }
            response.Version =
                (majorVersion == '1' && minorVersion == '1') ? HttpVersionInternal.Version11 :
                (majorVersion == '1' && minorVersion == '0') ? HttpVersionInternal.Version10 :
                (majorVersion == '2' && minorVersion == '0') ? HttpVersionInternal.Version20 :
                HttpVersionInternal.Unknown;
            response.StatusCode =
                (HttpStatusCode)(100 * (status1 - '0') + 10 * (status2 - '0') + (status3 - '0'));

            // Parse (optional) reason phrase
            byte c = line[12];
            if (c == '\r')
            {
                response.ReasonPhrase = string.Empty;
            }
            else if (c != ' ')
            {
                ThrowInvalidHttpResponse();
            }
            else
            {
                Span<byte> reasonBytes = line.Slice(13, line.Length - 13 - 2); // 2 == \r\n ending trimmed off
                string knownReasonPhrase = HttpStatusDescription.Get(response.StatusCode);
                if (knownReasonPhrase != null && EqualsOrdinal(knownReasonPhrase, reasonBytes))
                {
                    response.ReasonPhrase = knownReasonPhrase;
                }
                else
                {
                    unsafe
                    {
                        try
                        {
                            fixed (byte* reasonPtr = &MemoryMarshal.GetReference(reasonBytes))
                            {
                                response.ReasonPhrase = Encoding.ASCII.GetString(reasonPtr, reasonBytes.Length);
                            }
                        }
                        catch (FormatException e)
                        {
                            ThrowInvalidHttpResponse(e);
                        }
                    }
                }
            }
        }

        // TODO: Remove this overload once https://github.com/dotnet/roslyn/issues/17287 is addressed
        // and the compiler doesn't lift the span temporary from the call site into the async state
        // machine in debug builds.
        private void ParseHeaderNameValue(ArraySegment<byte> line, HttpResponseMessage response) =>
            ParseHeaderNameValue((Span<byte>)line, response);

        private void ParseHeaderNameValue(Span<byte> line, HttpResponseMessage response)
        {
            int pos = 0;
            while (line[pos] != (byte)':' && line[pos] != (byte)' ')
            {
                pos++;
                if (pos == line.Length)
                {
                    // Ignore invalid header line that doesn't contain ':'.
                    return;
                }
            }

            if (pos == 0)
            {
                // Ignore invalid empty header name.
                return;
            }

            // CONSIDER: trailing whitespace?

            if (!HeaderDescriptor.TryGet(line.Slice(0, pos), out HeaderDescriptor descriptor))
            {
                // Ignore invalid header name
                return;
            }

            // Eat any trailing whitespace
            while (line[pos] == (byte)' ')
            {
                pos++;
                if (pos == line.Length)
                {
                    // Ignore invalid header line that doesn't contain ':'.
                    return;
                }
            }

            if (line[pos++] != ':')
            {
                // Ignore invalid header line that doesn't contain ':'.
                return;
            }

            // Skip whitespace after colon
            while (pos < line.Length && (line[pos] == (byte)' ' || line[pos] == '\t'))
            {
                pos++;
            }

            string headerValue = descriptor.GetHeaderValue(line.Slice(pos, line.Length - pos - 2));     // trim trailing \r\n

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

        private async Task WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
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
                await FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            if (source.Length >= _writeBuffer.Length)
            {
                // Large write.  No sense buffering this.  Write directly to stream.
                // CONSIDER: May want to be a bit smarter here?  Think about how large writes should work...
                await WriteToStreamAsync(source, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Copy remainder into buffer
                WriteToBuffer(source);
            }
        }

        private Task WriteWithoutBufferingAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
        {
            if (_writeOffset == 0)
            {
                // There's nothing in the write buffer we need to flush.
                // Just write the supplied data out to the stream.
                return WriteToStreamAsync(source, cancellationToken);
            }

            int remaining = _writeBuffer.Length - _writeOffset;
            if (source.Length <= remaining)
            {
                // There's something already in the write buffer, but the content
                // we're writing can also fit after it in the write buffer.  Copy
                // the content to the write buffer and then flush it, so that we
                // can do a single send rather than two.
                WriteToBuffer(source);
                return FlushAsync(cancellationToken);
            }

            // There's data in the write buffer and the data we're writing doesn't fit after it.
            // Do two writes, one to flush the buffer and then another to write the supplied content.
            return FlushThenWriteWithoutBufferingAsync(source, cancellationToken);
        }

        private async Task FlushThenWriteWithoutBufferingAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
        {
            await FlushAsync(cancellationToken).ConfigureAwait(false);
            await WriteToStreamAsync(source, cancellationToken).ConfigureAwait(false);
        }

        private Task WriteByteAsync(byte b, CancellationToken cancellationToken)
        {
            if (_writeOffset < _writeBuffer.Length)
            {
                _writeBuffer[_writeOffset++] = b;
                return Task.CompletedTask;
            }
            return WriteByteSlowAsync(b, cancellationToken);
        }

        private async Task WriteByteSlowAsync(byte b, CancellationToken cancellationToken)
        {
            Debug.Assert(_writeOffset == _writeBuffer.Length);
            await WriteToStreamAsync(_writeBuffer, cancellationToken).ConfigureAwait(false);

            _writeBuffer[0] = b;
            _writeOffset = 1;
        }

        private Task WriteTwoBytesAsync(byte b1, byte b2, CancellationToken cancellationToken)
        {
            if (_writeOffset <= _writeBuffer.Length - 2)
            {
                byte[] buffer = _writeBuffer;
                buffer[_writeOffset++] = b1;
                buffer[_writeOffset++] = b2;
                return Task.CompletedTask;
            }
            return WriteTwoBytesSlowAsync(b1, b2, cancellationToken);
        }

        private async Task WriteTwoBytesSlowAsync(byte b1, byte b2, CancellationToken cancellationToken)
        {
            await WriteByteAsync(b1, cancellationToken).ConfigureAwait(false);
            await WriteByteAsync(b2, cancellationToken).ConfigureAwait(false);
        }

        private Task WriteBytesAsync(byte[] bytes, CancellationToken cancellationToken)
        {
            if (_writeOffset <= _writeBuffer.Length - bytes.Length)
            {
                Buffer.BlockCopy(bytes, 0, _writeBuffer, _writeOffset, bytes.Length);
                _writeOffset += bytes.Length;
                return Task.CompletedTask;
            }
            return WriteBytesSlowAsync(bytes, cancellationToken);
        }

        private async Task WriteBytesSlowAsync(byte[] bytes, CancellationToken cancellationToken)
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
                    await WriteToStreamAsync(_writeBuffer, cancellationToken).ConfigureAwait(false);
                    _writeOffset = 0;
                }
            }
        }

        private Task WriteStringAsync(string s, CancellationToken cancellationToken)
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
            return WriteStringAsyncSlow(s, cancellationToken);
        }

        private Task WriteAsciiStringAsync(string s, CancellationToken cancellationToken)
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
            return WriteStringAsyncSlow(s, cancellationToken);
        }

        private async Task WriteStringAsyncSlow(string s, CancellationToken cancellationToken)
        {
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if ((c & 0xFF80) != 0)
                {
                    throw new HttpRequestException(SR.net_http_request_invalid_char_encoding);
                }
                await WriteByteAsync((byte)c, cancellationToken).ConfigureAwait(false);
            }
        }

        private Task FlushAsync(CancellationToken cancellationToken)
        {
            if (_writeOffset > 0)
            {
                Task t = WriteToStreamAsync(new ReadOnlyMemory<byte>(_writeBuffer, 0, _writeOffset), cancellationToken);
                _writeOffset = 0;
                return t;
            }
            return Task.CompletedTask;
        }

        private Task WriteToStreamAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
        {
            if (NetEventSource.IsEnabled) Trace($"Writing {source.Length} bytes.");
            return _stream.WriteAsync(source, cancellationToken);
        }

        private async ValueTask<ArraySegment<byte>> ReadNextLineAsync(CancellationToken cancellationToken)
        {
            int searchOffset = 0;
            while (true)
            {
                int startIndex = _readOffset + searchOffset;
                int length = _readLength - startIndex;
                int crPos = Array.IndexOf(_readBuffer, (byte)'\r', startIndex, length);
                if (crPos < 0)
                {
                    // Couldn't find a \r.  Read more.
                    searchOffset = length;
                    await FillAsync(cancellationToken).ConfigureAwait(false);
                }
                else if (crPos + 1 >= _readLength)
                {
                    // We found a \r, but we don't have enough data buffered to read the \n.
                    searchOffset = length - 1;
                    await FillAsync(cancellationToken).ConfigureAwait(false);
                }
                else if (_readBuffer[crPos + 1] == '\n')
                {
                    // We found a \r\n.  Return the data up to and including it.
                    int lineLength = crPos - _readOffset + 2;
                    var result = new ArraySegment<byte>(_readBuffer, _readOffset, lineLength);
                    _readOffset += lineLength;
                    return result;
                }
                else
                {
                    ThrowInvalidHttpResponse();
                }
            }
        }

        private async Task ReadCrLfAsync(CancellationToken cancellationToken)
        {
            while (_readLength - _readOffset < 2)
            {
                // We have fewer than 2 chars buffered.  Get more.
                await FillAsync(cancellationToken).ConfigureAwait(false);
            }

            // We expect \r\n.  If so, consume them.  If not, it's an error.
            if (_readBuffer[_readOffset] != '\r' || _readBuffer[_readOffset + 1] != '\n')
            {
                ThrowInvalidHttpResponse();
            }

            _readOffset += 2;
        }

        // Throws IOException on EOF.  This is only called when we expect more data.
        private async Task FillAsync(CancellationToken cancellationToken)
        {
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

            // When the connection was put back into the pool, a pre-emptive read was performed
            // into the read buffer.  That read should not complete prior to us using the
            // connection again, as that would mean the connection was either closed or had
            // erroneous data sent on it by the server in response to no request from us.
            // We need to consume that read prior to issuing another read request.
            Task<int> t = _readAheadTask;
            int bytesRead;
            if (t != null)
            {
                Debug.Assert(_readOffset == 0);
                Debug.Assert(_readLength == 0);
                _readAheadTask = null;
                bytesRead = await t.ConfigureAwait(false);
            }
            else
            {
                ValueTask<int> vt = _stream.ReadAsync(new Memory<byte>(_readBuffer, _readLength, _readBuffer.Length - _readLength), cancellationToken);
                bytesRead = vt.IsCompletedSuccessfully ? vt.Result : await vt.AsTask().ConfigureAwait(false);
            }

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

        private async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken)
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
            int count = await _stream.ReadAsync(destination, cancellationToken).ConfigureAwait(false);
            if (NetEventSource.IsEnabled) Trace($"Received {count} bytes.");
            return count;
        }

        private async Task CopyFromBufferAsync(Stream destination, int count, CancellationToken cancellationToken)
        {
            Debug.Assert(count <= _readLength - _readOffset);

            if (NetEventSource.IsEnabled) Trace($"Copying {count} bytes to stream.");
            await destination.WriteAsync(_readBuffer, _readOffset, count, cancellationToken).ConfigureAwait(false);
            _readOffset += count;
        }

        private async Task CopyToAsync(Stream destination, CancellationToken cancellationToken)
        {
            Debug.Assert(destination != null);

            int remaining = _readLength - _readOffset;
            if (remaining > 0)
            {
                await CopyFromBufferAsync(destination, remaining, cancellationToken).ConfigureAwait(false);
            }

            while (true)
            {
                _readOffset = 0;

                // Don't use FillAsync here as it will throw on EOF.
                Debug.Assert(_readAheadTask == null);
                _readLength = await _stream.ReadAsync(_readBuffer, cancellationToken).ConfigureAwait(false);
                if (_readLength == 0)
                {
                    // End of stream
                    break;
                }

                await CopyFromBufferAsync(destination, _readLength, cancellationToken).ConfigureAwait(false);
            }
        }

        // Copy *exactly* [length] bytes into destination; throws on end of stream.
        private async Task CopyToAsync(Stream destination, ulong length, CancellationToken cancellationToken)
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
                await CopyFromBufferAsync(destination, remaining, cancellationToken).ConfigureAwait(false);

                length -= (ulong)remaining;
                if (length == 0)
                {
                    return;
                }
            }

            while (true)
            {
                await FillAsync(cancellationToken).ConfigureAwait(false);

                remaining = (ulong)_readLength < length ? _readLength : (int)length;
                await CopyFromBufferAsync(destination, remaining, cancellationToken).ConfigureAwait(false);

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
                    // Null out the associated request before the connection is potentially reused by another.
                    _currentRequest = null;
                    _sendRequestContentTask = null;

                    // When putting a connection back into the pool, we initiate a pre-emptive
                    // read on the stream.  When the connection is subsequently taken out of the
                    // pool, this can be used in place of the first read on the stream that would
                    // otherwise be done.  But by doing it now, we can check the status of the read
                    // at any point to understand if the connection has been closed or if errant data
                    // has been sent on the connection by the server, either of which would mean we
                    // should close the connection and not use it for subsequent requests.
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

        public override string ToString() => $"{nameof(HttpConnection)}(Host:{_key.Host})"; // Description for diagnostic purposes

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
}
