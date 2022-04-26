// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Quic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Net.Http.QPack;
using System.Runtime.ExceptionServices;

namespace System.Net.Http
{
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    internal sealed class Http3RequestStream : IHttpStreamHeadersHandler, IAsyncDisposable, IDisposable
    {
        private readonly HttpRequestMessage _request;
        private Http3Connection _connection;
        private long _streamId = -1; // A stream does not have an ID until the first I/O against it. This gets set almost immediately following construction.
        private QuicStream _stream;
        private ArrayBuffer _sendBuffer;
        private readonly ReadOnlyMemory<byte>[] _gatheredSendBuffer = new ReadOnlyMemory<byte>[2];
        private ArrayBuffer _recvBuffer;
        private TaskCompletionSource<bool>? _expect100ContinueCompletionSource; // True indicates we should send content (e.g. received 100 Continue).
        private bool _disposed;

        private CancellationTokenSource? _goawayCancellationSource;
        private CancellationToken _goawayCancellationToken;
        private CancellationTokenSource? _sendContentCts;

        // Allocated when we receive a :status header.
        private HttpResponseMessage? _response;

        // Header decoding.
        private QPackDecoder _headerDecoder;
        private HeaderState _headerState;
        private long _headerBudgetRemaining;

        /// <summary>Reusable array used to get the values for each header being written to the wire.</summary>
        private string[] _headerValues = Array.Empty<string>();

        /// <summary>Any trailing headers.</summary>
        private List<(HeaderDescriptor name, string value)>? _trailingHeaders;

        // When reading response content, keep track of the number of bytes left in the current data frame.
        private long _responseDataPayloadRemaining;

        // When our request content has a precomputed length, it is sent over a single DATA frame.
        // Keep track of how much is remaining in that frame.
        private long _requestContentLengthRemaining;

        // For the precomputed length case, we need to add the DATA framing for the first write only.
        private bool _singleDataFrameWritten;

        public long StreamId
        {
            get => Volatile.Read(ref _streamId);
            set => Volatile.Write(ref _streamId, value);
        }

        public Http3RequestStream(HttpRequestMessage request, Http3Connection connection, QuicStream stream)
        {
            _request = request;
            _connection = connection;
            _stream = stream;
            _sendBuffer = new ArrayBuffer(initialSize: 64, usePool: true);
            _recvBuffer = new ArrayBuffer(initialSize: 64, usePool: true);

            _headerBudgetRemaining = connection.Pool.Settings._maxResponseHeadersLength * 1024L; // _maxResponseHeadersLength is in KiB.
            _headerDecoder = new QPackDecoder(maxHeadersLength: (int)Math.Min(int.MaxValue, _headerBudgetRemaining));

            _goawayCancellationSource = new CancellationTokenSource();
            _goawayCancellationToken = _goawayCancellationSource.Token;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                AbortStream();
                _stream.Dispose();
                DisposeSyncHelper();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;
                AbortStream();
                await _stream.DisposeAsync().ConfigureAwait(false);
                DisposeSyncHelper();
            }
        }

        private void DisposeSyncHelper()
        {
            _connection.RemoveStream(_stream);

            _sendBuffer.Dispose();
            _recvBuffer.Dispose();

            // Dispose() might be called concurrently with GoAway(), we need to make sure to not Dispose/Cancel the CTS concurrently.
            Interlocked.Exchange(ref _goawayCancellationSource, null)?.Dispose();
        }

        public void GoAway()
        {
            // Dispose() might be called concurrently with GoAway(), we need to make sure to not Dispose/Cancel the CTS concurrently.
            using CancellationTokenSource? cts = Interlocked.Exchange(ref _goawayCancellationSource, null);
            cts?.Cancel();
        }

        public async Task<HttpResponseMessage> SendAsync(CancellationToken cancellationToken)
        {
            // If true, dispose the stream upon return. Will be set to false if we're duplex or returning content.
            bool disposeSelf = true;

            // Link the input token with _resetCancellationTokenSource, so cancellation will trigger on GoAway() or Abort().
            using CancellationTokenSource requestCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _goawayCancellationToken);
            CancellationTokenRegistration sendContentCancellationRegistration = default;

            try
            {
                BufferHeaders(_request);

                // If using Expect 100 Continue, setup a TCS to wait to send content until we get a response.
                if (_request.HasHeaders && _request.Headers.ExpectContinue == true)
                {
                    _expect100ContinueCompletionSource = new TaskCompletionSource<bool>();
                }

                if (_expect100ContinueCompletionSource != null || _request.Content == null)
                {
                    // Ideally, headers will be sent out in a gathered write inside of SendContentAsync().
                    // If we don't have content, or we are doing Expect 100 Continue, then we can't rely on
                    // this and must send our headers immediately.

                    // End the stream writing if there's no content to send, do it as part of the write so that the FIN flag isn't send in an empty QUIC frame.
                    // Note that there's no need to call Shutdown separately since the FIN flag in the last write is the same thing.
                    await FlushSendBufferAsync(endStream: _request.Content == null, requestCancellationSource.Token).ConfigureAwait(false);
                }

                Task sendContentTask;
                if (_request.Content != null)
                {
                    // If using duplex content, the content will continue sending after this method completes.
                    // So, only observe the cancellation token during this method.
                    CancellationToken sendContentCancellationToken;
                    if (requestCancellationSource.Token.CanBeCanceled)
                    {
                        _sendContentCts = new CancellationTokenSource();
                        sendContentCancellationToken = _sendContentCts.Token;
                        sendContentCancellationRegistration = requestCancellationSource.Token.UnsafeRegister(
                            static s => ((CancellationTokenSource)s!).Cancel(), _sendContentCts);
                    }
                    else
                    {
                        sendContentCancellationToken = default;
                    }

                    sendContentTask = SendContentAsync(_request.Content!, sendContentCancellationToken);
                }
                else
                {
                    sendContentTask = Task.CompletedTask;
                }

                // In parallel, send content and read response.
                // Depending on Expect 100 Continue usage, one will depend on the other making progress.
                Task readResponseTask = ReadResponseAsync(requestCancellationSource.Token);
                bool sendContentObserved = false;

                // If we're not doing duplex, wait for content to finish sending here.
                // If we are doing duplex and have the unlikely event that it completes here, observe the result.
                // See Http2Connection.SendAsync for a full comment on this logic -- it is identical behavior.
                if (sendContentTask.IsCompleted ||
                    _request.Content?.AllowDuplex != true ||
                    await Task.WhenAny(sendContentTask, readResponseTask).ConfigureAwait(false) == sendContentTask ||
                    sendContentTask.IsCompleted)
                {
                    try
                    {
                        await sendContentTask.ConfigureAwait(false);
                        sendContentObserved = true;
                    }
                    catch
                    {
                        // Exceptions will be bubbled up from sendContentTask here,
                        // which means the result of readResponseTask won't be observed directly:
                        // Do a background await to log any exceptions.
                        _connection.LogExceptions(readResponseTask);
                        throw;
                    }
                }
                else
                {
                    // Duplex is being used, so we can't wait for content to finish sending.
                    // Do a background await to log any exceptions.
                    _connection.LogExceptions(sendContentTask);
                }

                // Wait for the response headers to be read.
                await readResponseTask.ConfigureAwait(false);

                // Now that we've received the response, we no longer need to observe GOAWAY.
                // Use an atomic exchange to avoid a race to Cancel()/Dispose().
                Interlocked.Exchange(ref _goawayCancellationSource, null)?.Dispose();

                Debug.Assert(_response != null && _response.Content != null);
                // Set our content stream.
                var responseContent = (HttpConnectionResponseContent)_response.Content;

                // If we have received Content-Length: 0 and have completed sending content (which may not be the case if duplex),
                // we can close our Http3RequestStream immediately and return a singleton empty content stream. Otherwise, we
                // need to return a Http3ReadStream which will be responsible for disposing the Http3RequestStream.
                bool useEmptyResponseContent = responseContent.Headers.ContentLength == 0 && sendContentObserved;
                if (useEmptyResponseContent)
                {
                    // Drain the response frames to read any trailing headers.
                    await DrainContentLength0Frames(requestCancellationSource.Token).ConfigureAwait(false);
                    responseContent.SetStream(EmptyReadStream.Instance);
                }
                else
                {
                    // A read stream is required to finish up the request.
                    responseContent.SetStream(new Http3ReadStream(this));
                }
                if (NetEventSource.Log.IsEnabled()) Trace($"Received response: {_response}");

                // Process any Set-Cookie headers.
                if (_connection.Pool.Settings._useCookies)
                {
                    CookieHelper.ProcessReceivedCookies(_response, _connection.Pool.Settings._cookieContainer!);
                }

                // To avoid a circular reference (stream->response->content->stream), null out the stream's response.
                HttpResponseMessage response = _response;
                _response = null;

                // If we're 100% done with the stream, dispose.
                disposeSelf = useEmptyResponseContent;

                return response;
            }
            catch (QuicStreamAbortedException ex) when (ex.ErrorCode == (long)Http3ErrorCode.VersionFallback)
            {
                // The server is requesting us fall back to an older HTTP version.
                throw new HttpRequestException(SR.net_http_retry_on_older_version, ex, RequestRetryType.RetryOnLowerHttpVersion);
            }
            catch (QuicStreamAbortedException ex) when (ex.ErrorCode == (long)Http3ErrorCode.RequestRejected)
            {
                // The server is rejecting the request without processing it, retry it on a different connection.
                throw new HttpRequestException(SR.net_http_request_aborted, ex, RequestRetryType.RetryOnConnectionFailure);
            }
            catch (QuicStreamAbortedException ex)
            {
                // Our stream was reset.

                Exception? abortException = _connection.AbortException;
                throw new HttpRequestException(SR.net_http_client_execution_error, abortException ?? ex);
            }
            catch (QuicConnectionAbortedException ex)
            {
                // Our connection was reset. Start shutting down the connection.

                Exception abortException = _connection.Abort(ex);
                throw new HttpRequestException(SR.net_http_client_execution_error, abortException);
            }
            // It is possible for user's Content code to throw an unexpected OperationCanceledException.
            catch (OperationCanceledException ex) when (ex.CancellationToken == requestCancellationSource.Token || ex.CancellationToken == _sendContentCts?.Token)
            {
                // We're either observing GOAWAY, or the cancellationToken parameter has been canceled.
                if (cancellationToken.IsCancellationRequested)
                {
                    _stream.AbortWrite((long)Http3ErrorCode.RequestCancelled);
                    throw new TaskCanceledException(ex.Message, ex, cancellationToken);
                }
                else
                {
                    Debug.Assert(_goawayCancellationToken.IsCancellationRequested == true);
                    throw new HttpRequestException(SR.net_http_request_aborted, ex, RequestRetryType.RetryOnConnectionFailure);
                }
            }
            catch (Http3ConnectionException ex)
            {
                // A connection-level protocol error has occurred on our stream.
                _connection.Abort(ex);
                throw new HttpRequestException(SR.net_http_client_execution_error, ex);
            }
            catch (Exception ex)
            {
                _stream.AbortWrite((long)Http3ErrorCode.InternalError);
                if (ex is HttpRequestException)
                {
                    throw;
                }
                throw new HttpRequestException(SR.net_http_client_execution_error, ex);
            }
            finally
            {
                sendContentCancellationRegistration.Dispose();
                if (disposeSelf)
                {
                    await DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Waits for the response headers to be read, and handles (Expect 100 etc.) informational statuses.
        /// </summary>
        private async Task ReadResponseAsync(CancellationToken cancellationToken)
        {
            if (HttpTelemetry.Log.IsEnabled()) HttpTelemetry.Log.ResponseHeadersStart();

            Debug.Assert(_response == null);
            do
            {
                _headerState = HeaderState.StatusHeader;

                (Http3FrameType? frameType, long payloadLength) = await ReadFrameEnvelopeAsync(cancellationToken).ConfigureAwait(false);

                if (frameType != Http3FrameType.Headers)
                {
                    if (NetEventSource.Log.IsEnabled())
                    {
                        Trace($"Expected HEADERS as first response frame; received {frameType}.");
                    }
                    throw new HttpRequestException(SR.net_http_invalid_response);
                }

                await ReadHeadersAsync(payloadLength, cancellationToken).ConfigureAwait(false);
                Debug.Assert(_response != null);
            }
            while ((int)_response.StatusCode < 200);

            _headerState = HeaderState.TrailingHeaders;

            if (HttpTelemetry.Log.IsEnabled()) HttpTelemetry.Log.ResponseHeadersStop();
        }

        private async Task SendContentAsync(HttpContent content, CancellationToken cancellationToken)
        {
            // If we're using Expect 100 Continue, wait to send content
            // until we get a response back or until our timeout elapses.
            if (_expect100ContinueCompletionSource != null)
            {
                Timer? timer = null;

                try
                {
                    if (_connection.Pool.Settings._expect100ContinueTimeout != Timeout.InfiniteTimeSpan)
                    {
                        timer = new Timer(static o => ((Http3RequestStream)o!)._expect100ContinueCompletionSource!.TrySetResult(true),
                            this, _connection.Pool.Settings._expect100ContinueTimeout, Timeout.InfiniteTimeSpan);
                    }

                    if (!await _expect100ContinueCompletionSource.Task.ConfigureAwait(false))
                    {
                        // We received an error response code, so the body should not be sent.
                        return;
                    }
                }
                finally
                {
                    if (timer != null)
                    {
                        await timer.DisposeAsync().ConfigureAwait(false);
                    }
                }
            }

            if (HttpTelemetry.Log.IsEnabled()) HttpTelemetry.Log.RequestContentStart();

            // If we have a Content-Length, keep track of it so we don't over-send and so we can send in a single DATA frame.
            _requestContentLengthRemaining = content.Headers.ContentLength ?? -1;

            var writeStream = new Http3WriteStream(this);
            try
            {
                await content.CopyToAsync(writeStream, null, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                writeStream.Dispose();
            }

            if (_requestContentLengthRemaining > 0)
            {
                // The number of bytes we actually sent doesn't match the advertised Content-Length
                long contentLength = content.Headers.ContentLength.GetValueOrDefault();
                long sent = contentLength - _requestContentLengthRemaining;
                throw new HttpRequestException(SR.Format(SR.net_http_request_content_length_mismatch, sent, contentLength));
            }

            // Set to 0 to recognize that the whole request body has been sent and therefore there's no need to abort write side in case of a premature disposal.
            _requestContentLengthRemaining = 0;

            if (_sendBuffer.ActiveLength != 0)
            {
                // Our initial send buffer, which has our headers, is normally sent out on the first write to the Http3WriteStream.
                // If we get here, it means the content didn't actually do any writing. Send out the headers now.
                // Also send the FIN flag, since this is the last write. No need to call Shutdown separately.
                await FlushSendBufferAsync(endStream: true, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                _stream.Shutdown();
            }

            if (HttpTelemetry.Log.IsEnabled()) HttpTelemetry.Log.RequestContentStop(writeStream.BytesWritten);
        }

        private async ValueTask WriteRequestContentAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            if (buffer.Length == 0)
            {
                return;
            }

            long remaining = _requestContentLengthRemaining;
            if (remaining != -1)
            {
                // This HttpContent had a precomputed length, and a DATA frame was written as part of the headers. We can write directly without framing.

                if (buffer.Length > _requestContentLengthRemaining)
                {
                    throw new HttpRequestException(SR.net_http_content_write_larger_than_content_length);
                }
                _requestContentLengthRemaining -= buffer.Length;

                if (!_singleDataFrameWritten)
                {
                    // Note we may not have sent headers yet; if so, _sendBuffer.ActiveLength will be > 0, and we will write them in a single write.

                    // Because we have a Content-Length, we can write it in a single DATA frame.
                    BufferFrameEnvelope(Http3FrameType.Data, remaining);

                    _gatheredSendBuffer[0] = _sendBuffer.ActiveMemory;
                    _gatheredSendBuffer[1] = buffer;
                    await _stream.WriteAsync(_gatheredSendBuffer, cancellationToken).ConfigureAwait(false);

                    _sendBuffer.Discard(_sendBuffer.ActiveLength);

                    _singleDataFrameWritten = true;
                }
                else
                {
                    // DATA frame already sent, send just the content buffer directly.
                    await _stream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                // Variable-length content: write both a DATA frame and buffer. (and headers, which will still be in _sendBuffer if this is the first content write)
                // It's up to the HttpContent to give us sufficiently large writes to avoid excessively small DATA frames.
                BufferFrameEnvelope(Http3FrameType.Data, buffer.Length);

                _gatheredSendBuffer[0] = _sendBuffer.ActiveMemory;
                _gatheredSendBuffer[1] = buffer;
                await _stream.WriteAsync(_gatheredSendBuffer, cancellationToken).ConfigureAwait(false);

                _sendBuffer.Discard(_sendBuffer.ActiveLength);
            }
        }

        private async ValueTask FlushSendBufferAsync(bool endStream, CancellationToken cancellationToken)
        {
            await _stream.WriteAsync(_sendBuffer.ActiveMemory, endStream, cancellationToken).ConfigureAwait(false);
            _sendBuffer.Discard(_sendBuffer.ActiveLength);

            await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask DrainContentLength0Frames(CancellationToken cancellationToken)
        {
            Http3FrameType? frameType;
            long payloadLength;

            while (true)
            {
                (frameType, payloadLength) = await ReadFrameEnvelopeAsync(cancellationToken).ConfigureAwait(false);

                switch (frameType)
                {
                    case Http3FrameType.Headers:
                        // Pick up any trailing headers.
                        _trailingHeaders = new List<(HeaderDescriptor name, string value)>();
                        await ReadHeadersAsync(payloadLength, cancellationToken).ConfigureAwait(false);

                        // Stop looping after a trailing header.
                        // There may be extra frames after this one, but they would all be unknown extension
                        // frames that can be safely ignored. Just stop reading here.
                        // Note: this does leave us open to a bad server sending us an out of order DATA frame.
                        goto case null;
                    case null:
                        // Done receiving: copy over trailing headers.
                        CopyTrailersToResponseMessage(_response!);

                        _responseDataPayloadRemaining = -1; // Set to -1 to indicate EOS.
                        return;
                    case Http3FrameType.Data:
                        // The sum of data frames must equal content length. Because this method is only
                        // called for a Content-Length of 0, anything other than 0 here would be an error.
                        // Per spec, 0-length payload is allowed.
                        if (payloadLength != 0)
                        {
                            if (NetEventSource.Log.IsEnabled())
                            {
                                Trace("Response content exceeded Content-Length.");
                            }
                            throw new HttpRequestException(SR.net_http_invalid_response);
                        }
                        break;
                    default:
                        Debug.Fail($"Received unexpected frame type {frameType}.");
                        return;
                }
            }
        }

        private void CopyTrailersToResponseMessage(HttpResponseMessage responseMessage)
        {
            if (_trailingHeaders?.Count > 0)
            {
                foreach ((HeaderDescriptor name, string value) in _trailingHeaders)
                {
                    responseMessage.TrailingHeaders.TryAddWithoutValidation(name, value);
                }
                _trailingHeaders.Clear();
            }
        }

        private void BufferHeaders(HttpRequestMessage request)
        {
            if (HttpTelemetry.Log.IsEnabled()) HttpTelemetry.Log.RequestHeadersStart();

            // Reserve space for the header frame envelope.
            // The envelope needs to be written after headers are serialized, as we need to know the payload length first.
            const int PreHeadersReserveSpace = Http3Frame.MaximumEncodedFrameEnvelopeLength;

            // This should be the first write to our buffer. The trick of reserving space won't otherwise work.
            Debug.Assert(_sendBuffer.ActiveLength == 0);

            // Reserve space for header frame envelope.
            _sendBuffer.Commit(PreHeadersReserveSpace);

            // Add header block prefix. We aren't using dynamic table, so these are simple zeroes.
            // https://tools.ietf.org/html/draft-ietf-quic-qpack-11#section-4.5.1
            _sendBuffer.EnsureAvailableSpace(2);
            _sendBuffer.AvailableSpan[0] = 0x00; // required insert count.
            _sendBuffer.AvailableSpan[1] = 0x00; // s + delta base.
            _sendBuffer.Commit(2);

            HttpMethod normalizedMethod = HttpMethod.Normalize(request.Method);
            BufferBytes(normalizedMethod.Http3EncodedBytes);
            BufferIndexedHeader(H3StaticTable.SchemeHttps);

            if (request.HasHeaders && request.Headers.Host != null)
            {
                BufferLiteralHeaderWithStaticNameReference(H3StaticTable.Authority, request.Headers.Host);
            }
            else
            {
                BufferBytes(_connection.Pool._http3EncodedAuthorityHostHeader);
            }

            Debug.Assert(request.RequestUri != null);
            string pathAndQuery = request.RequestUri.PathAndQuery;
            if (pathAndQuery == "/")
            {
                BufferIndexedHeader(H3StaticTable.PathSlash);
            }
            else
            {
                BufferLiteralHeaderWithStaticNameReference(H3StaticTable.PathSlash, pathAndQuery);
            }

            // The only way to reach H3 is to upgrade via an Alt-Svc header, so we can encode Alt-Used for every connection.
            BufferBytes(_connection.AltUsedEncodedHeaderBytes);

            if (request.HasHeaders)
            {
                // H3 does not support Transfer-Encoding: chunked.
                if (request.HasHeaders && request.Headers.TransferEncodingChunked == true)
                {
                    request.Headers.TransferEncodingChunked = false;
                }

                BufferHeaderCollection(request.Headers);
            }

            if (_connection.Pool.Settings._useCookies)
            {
                string cookiesFromContainer = _connection.Pool.Settings._cookieContainer!.GetCookieHeader(request.RequestUri);
                if (cookiesFromContainer != string.Empty)
                {
                    Encoding? valueEncoding = _connection.Pool.Settings._requestHeaderEncodingSelector?.Invoke(HttpKnownHeaderNames.Cookie, request);
                    BufferLiteralHeaderWithStaticNameReference(H3StaticTable.Cookie, cookiesFromContainer, valueEncoding);
                }
            }

            if (request.Content == null)
            {
                if (normalizedMethod.MustHaveRequestBody)
                {
                    BufferIndexedHeader(H3StaticTable.ContentLength0);
                }
            }
            else
            {
                BufferHeaderCollection(request.Content.Headers);
            }

            // Determine our header envelope size.
            // The reserved space was the maximum required; discard what wasn't used.
            int headersLength = _sendBuffer.ActiveLength - PreHeadersReserveSpace;
            int headersLengthEncodedSize = VariableLengthIntegerHelper.GetByteCount(headersLength);
            _sendBuffer.Discard(PreHeadersReserveSpace - headersLengthEncodedSize - 1);

            // Encode header type in first byte, and payload length in subsequent bytes.
            _sendBuffer.ActiveSpan[0] = (byte)Http3FrameType.Headers;
            int actualHeadersLengthEncodedSize = VariableLengthIntegerHelper.WriteInteger(_sendBuffer.ActiveSpan.Slice(1, headersLengthEncodedSize), headersLength);
            Debug.Assert(actualHeadersLengthEncodedSize == headersLengthEncodedSize);

            if (HttpTelemetry.Log.IsEnabled()) HttpTelemetry.Log.RequestHeadersStop();
        }

        // TODO: special-case Content-Type for static table values values?
        private void BufferHeaderCollection(HttpHeaders headers)
        {
            HeaderEncodingSelector<HttpRequestMessage>? encodingSelector = _connection.Pool.Settings._requestHeaderEncodingSelector;

            foreach (HeaderEntry header in headers.GetEntries())
            {
                int headerValuesCount = HttpHeaders.GetStoreValuesIntoStringArray(header.Key, header.Value, ref _headerValues);
                Debug.Assert(headerValuesCount > 0, "No values for header??");
                ReadOnlySpan<string> headerValues = _headerValues.AsSpan(0, headerValuesCount);

                Encoding? valueEncoding = encodingSelector?.Invoke(header.Key.Name, _request);

                KnownHeader? knownHeader = header.Key.KnownHeader;
                if (knownHeader != null)
                {
                    // The Host header is not sent for HTTP/3 because we send the ":authority" pseudo-header instead
                    // (see pseudo-header handling below in WriteHeaders).
                    // The Connection, Upgrade and ProxyConnection headers are also not supported in HTTP/3.
                    if (knownHeader != KnownHeaders.Host && knownHeader != KnownHeaders.Connection && knownHeader != KnownHeaders.Upgrade && knownHeader != KnownHeaders.ProxyConnection)
                    {
                        if (knownHeader == KnownHeaders.TE)
                        {
                            // HTTP/2 allows only 'trailers' TE header. rfc7540 8.1.2.2
                            // HTTP/3 does not mention this one way or another; assume it has the same rule.
                            foreach (string value in headerValues)
                            {
                                if (string.Equals(value, "trailers", StringComparison.OrdinalIgnoreCase))
                                {
                                    BufferLiteralHeaderWithoutNameReference("TE", value, valueEncoding);
                                    break;
                                }
                            }
                            continue;
                        }

                        // For all other known headers, send them via their pre-encoded name and the associated value.
                        BufferBytes(knownHeader.Http3EncodedName);
                        string? separator = null;
                        if (headerValues.Length > 1)
                        {
                            HttpHeaderParser? parser = header.Key.Parser;
                            if (parser != null && parser.SupportsMultipleValues)
                            {
                                separator = parser.Separator;
                            }
                            else
                            {
                                separator = HttpHeaderParser.DefaultSeparator;
                            }
                        }

                        BufferLiteralHeaderValues(headerValues, separator, valueEncoding);
                    }
                }
                else
                {
                    // The header is not known: fall back to just encoding the header name and value(s).
                    BufferLiteralHeaderWithoutNameReference(header.Key.Name, headerValues, HttpHeaderParser.DefaultSeparator, valueEncoding);
                }
            }
        }

        private void BufferIndexedHeader(int index)
        {
            int bytesWritten;
            while (!QPackEncoder.EncodeStaticIndexedHeaderField(index, _sendBuffer.AvailableSpan, out bytesWritten))
            {
                _sendBuffer.Grow();
            }
            _sendBuffer.Commit(bytesWritten);
        }

        private void BufferLiteralHeaderWithStaticNameReference(int nameIndex, string value, Encoding? valueEncoding = null)
        {
            int bytesWritten;
            while (!QPackEncoder.EncodeLiteralHeaderFieldWithStaticNameReference(nameIndex, value, valueEncoding, _sendBuffer.AvailableSpan, out bytesWritten))
            {
                _sendBuffer.Grow();
            }
            _sendBuffer.Commit(bytesWritten);
        }

        private void BufferLiteralHeaderWithoutNameReference(string name, ReadOnlySpan<string> values, string separator, Encoding? valueEncoding)
        {
            int bytesWritten;
            while (!QPackEncoder.EncodeLiteralHeaderFieldWithoutNameReference(name, values, separator, valueEncoding, _sendBuffer.AvailableSpan, out bytesWritten))
            {
                _sendBuffer.Grow();
            }
            _sendBuffer.Commit(bytesWritten);
        }

        private void BufferLiteralHeaderWithoutNameReference(string name, string value, Encoding? valueEncoding)
        {
            int bytesWritten;
            while (!QPackEncoder.EncodeLiteralHeaderFieldWithoutNameReference(name, value, valueEncoding, _sendBuffer.AvailableSpan, out bytesWritten))
            {
                _sendBuffer.Grow();
            }
            _sendBuffer.Commit(bytesWritten);
        }

        private void BufferLiteralHeaderValues(ReadOnlySpan<string> values, string? separator, Encoding? valueEncoding)
        {
            int bytesWritten;
            while (!QPackEncoder.EncodeValueString(values, separator, valueEncoding, _sendBuffer.AvailableSpan, out bytesWritten))
            {
                _sendBuffer.Grow();
            }
            _sendBuffer.Commit(bytesWritten);
        }

        private void BufferFrameEnvelope(Http3FrameType frameType, long payloadLength)
        {
            int bytesWritten;
            while (!Http3Frame.TryWriteFrameEnvelope(frameType, payloadLength, _sendBuffer.AvailableSpan, out bytesWritten))
            {
                _sendBuffer.Grow();
            }
            _sendBuffer.Commit(bytesWritten);
        }

        private void BufferBytes(ReadOnlySpan<byte> span)
        {
            _sendBuffer.EnsureAvailableSpace(span.Length);
            span.CopyTo(_sendBuffer.AvailableSpan);
            _sendBuffer.Commit(span.Length);
        }

        private async ValueTask<(Http3FrameType? frameType, long payloadLength)> ReadFrameEnvelopeAsync(CancellationToken cancellationToken)
        {
            long frameType, payloadLength;
            int bytesRead;

            while (true)
            {
                while (!Http3Frame.TryReadIntegerPair(_recvBuffer.ActiveSpan, out frameType, out payloadLength, out bytesRead))
                {
                    _recvBuffer.EnsureAvailableSpace(VariableLengthIntegerHelper.MaximumEncodedLength * 2);
                    bytesRead = await _stream.ReadAsync(_recvBuffer.AvailableMemory, cancellationToken).ConfigureAwait(false);

                    if (bytesRead != 0)
                    {
                        _recvBuffer.Commit(bytesRead);
                    }
                    else if (_recvBuffer.ActiveLength == 0)
                    {
                        // End of stream.
                        return (null, 0);
                    }
                    else
                    {
                        // Our buffer has partial frame data in it but not enough to complete the read: bail out.
                        throw new HttpRequestException(SR.net_http_invalid_response_premature_eof);
                    }
                }

                _recvBuffer.Discard(bytesRead);

                if (NetEventSource.Log.IsEnabled())
                {
                    Trace($"Received frame {frameType} of length {payloadLength}.");
                }

                switch ((Http3FrameType)frameType)
                {
                    case Http3FrameType.Headers:
                    case Http3FrameType.Data:
                        return ((Http3FrameType)frameType, payloadLength);
                    case Http3FrameType.Settings: // These frames should only be received on a control stream, not a response stream.
                    case Http3FrameType.GoAway:
                    case Http3FrameType.MaxPushId:
                    case Http3FrameType.ReservedHttp2Priority: // These frames are explicitly reserved and must never be sent.
                    case Http3FrameType.ReservedHttp2Ping:
                    case Http3FrameType.ReservedHttp2WindowUpdate:
                    case Http3FrameType.ReservedHttp2Continuation:
                        throw new Http3ConnectionException(Http3ErrorCode.UnexpectedFrame);
                    case Http3FrameType.PushPromise:
                    case Http3FrameType.CancelPush:
                        // Because we haven't sent any MAX_PUSH_ID frames, any of these push-related
                        // frames that the server sends will have an out-of-range push ID.
                        throw new Http3ConnectionException(Http3ErrorCode.IdError);
                    default:
                        // Unknown frame types should be skipped.
                        await SkipUnknownPayloadAsync(payloadLength, cancellationToken).ConfigureAwait(false);
                        break;
                }
            }
        }

        private async ValueTask ReadHeadersAsync(long headersLength, CancellationToken cancellationToken)
        {
            // TODO: this header budget is sent as SETTINGS_MAX_HEADER_LIST_SIZE, so it should not use frame payload but rather 32 bytes + uncompressed size per entry.
            // https://tools.ietf.org/html/draft-ietf-quic-http-24#section-4.1.1
            if (headersLength > _headerBudgetRemaining)
            {
                _stream.AbortWrite((long)Http3ErrorCode.ExcessiveLoad);
                throw new HttpRequestException(SR.Format(SR.net_http_response_headers_exceeded_length, _connection.Pool.Settings._maxResponseHeadersLength * 1024L));
            }

            _headerBudgetRemaining -= headersLength;

            while (headersLength != 0)
            {
                if (_recvBuffer.ActiveLength == 0)
                {
                    _recvBuffer.EnsureAvailableSpace(1);

                    int bytesRead = await _stream.ReadAsync(_recvBuffer.AvailableMemory, cancellationToken).ConfigureAwait(false);
                    if (bytesRead != 0)
                    {
                        _recvBuffer.Commit(bytesRead);
                    }
                    else
                    {
                        if (NetEventSource.Log.IsEnabled()) Trace($"Server closed response stream before entire header payload could be read. {headersLength:N0} bytes remaining.");
                        throw new HttpRequestException(SR.net_http_invalid_response_premature_eof);
                    }
                }

                int processLength = (int)Math.Min(headersLength, _recvBuffer.ActiveLength);
                bool endHeaders = headersLength == processLength;

                _headerDecoder.Decode(_recvBuffer.ActiveSpan.Slice(0, processLength), endHeaders, this);
                _recvBuffer.Discard(processLength);
                headersLength -= processLength;
            }

            // Reset decoder state. Require because one decoder instance is reused to decode headers and trailers.
            _headerDecoder.Reset();
        }

        void IHttpStreamHeadersHandler.OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
        {
            Debug.Assert(name.Length > 0);
            if (!HeaderDescriptor.TryGet(name, out HeaderDescriptor descriptor))
            {
                // Invalid header name
                throw new HttpRequestException(SR.Format(SR.net_http_invalid_response_header_name, Encoding.ASCII.GetString(name)));
            }
            OnHeader(staticIndex: null, descriptor, staticValue: default, literalValue: value);
        }

        void IHttpStreamHeadersHandler.OnStaticIndexedHeader(int index)
        {
            GetStaticQPackHeader(index, out HeaderDescriptor descriptor, out string? knownValue);
            OnHeader(index, descriptor, knownValue, literalValue: default);
        }

        void IHttpStreamHeadersHandler.OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
        {
            GetStaticQPackHeader(index, out HeaderDescriptor descriptor, knownValue: out _);
            OnHeader(index, descriptor, staticValue: null, literalValue: value);
        }

        void IHttpStreamHeadersHandler.OnDynamicIndexedHeader(int? index, ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
        {
            ((IHttpStreamHeadersHandler)this).OnHeader(name, value);
        }

        private void GetStaticQPackHeader(int index, out HeaderDescriptor descriptor, out string? knownValue)
        {
            if (!HeaderDescriptor.TryGetStaticQPackHeader(index, out descriptor, out knownValue))
            {
                if (NetEventSource.Log.IsEnabled()) Trace($"Response contains invalid static header index '{index}'.");
                throw new Http3ConnectionException(Http3ErrorCode.ProtocolError);
            }
        }

        /// <param name="staticIndex">The static index of the header, if any.</param>
        /// <param name="descriptor">A descriptor for either a known header or unknown header.</param>
        /// <param name="staticValue">The static indexed value, if any.</param>
        /// <param name="literalValue">The literal ASCII value, if any.</param>
        /// <remarks>One of <paramref name="staticValue"/> or <paramref name="literalValue"/> will be set.</remarks>
        private void OnHeader(int? staticIndex, HeaderDescriptor descriptor, string? staticValue, ReadOnlySpan<byte> literalValue)
        {
            if (descriptor.Name[0] == ':')
            {
                if (!descriptor.Equals(KnownHeaders.PseudoStatus))
                {
                    if (NetEventSource.Log.IsEnabled()) Trace($"Received unknown pseudo-header '{descriptor.Name}'.");
                    throw new Http3ConnectionException(Http3ErrorCode.ProtocolError);
                }

                if (_headerState != HeaderState.StatusHeader)
                {
                    if (NetEventSource.Log.IsEnabled()) Trace("Received extra status header.");
                    throw new Http3ConnectionException(Http3ErrorCode.ProtocolError);
                }

                int statusCode;
                if (staticValue != null) // Indexed Header Field -- both name and value are taken from the table
                {
                    statusCode = staticIndex switch
                    {
                        H3StaticTable.Status103 => 103,
                        H3StaticTable.Status200 => 200,
                        H3StaticTable.Status304 => 304,
                        H3StaticTable.Status404 => 404,
                        H3StaticTable.Status503 => 503,
                        H3StaticTable.Status100 => 100,
                        H3StaticTable.Status204 => 204,
                        H3StaticTable.Status206 => 206,
                        H3StaticTable.Status302 => 302,
                        H3StaticTable.Status400 => 400,
                        H3StaticTable.Status403 => 403,
                        H3StaticTable.Status421 => 421,
                        H3StaticTable.Status425 => 425,
                        H3StaticTable.Status500 => 500,
                        // We should never get here, at least while we only use static table. But we can still parse staticValue.
                        _ => ParseStatusCode(staticIndex, staticValue)
                    };

                    int ParseStatusCode(int? index, string value)
                    {
                        string message = $"Unexpected QPACK table reference for Status code: index={index} value=\'{value}\'";
                        Debug.Fail(message);
                        if (NetEventSource.Log.IsEnabled()) Trace(message);

                        // TODO: The parsing is not optimal, but I don't expect this line to be executed at all for now.
                        return HttpConnectionBase.ParseStatusCode(Encoding.ASCII.GetBytes(value));
                    }
                }
                else // Literal Header Field With Name Reference -- only name is taken from the table
                {
                    statusCode = HttpConnectionBase.ParseStatusCode(literalValue);
                }

                _response = new HttpResponseMessage()
                {
                    Version = HttpVersion.Version30,
                    RequestMessage = _request,
                    Content = new HttpConnectionResponseContent(),
                    StatusCode = (HttpStatusCode)statusCode
                };

                if (statusCode < 200)
                {
                    // Informational responses should not contain headers -- skip them.
                    _headerState = HeaderState.SkipExpect100Headers;

                    if (_response.StatusCode == HttpStatusCode.Continue && _expect100ContinueCompletionSource != null)
                    {
                        _expect100ContinueCompletionSource.TrySetResult(true);
                    }
                }
                else
                {
                    _headerState = HeaderState.ResponseHeaders;
                    if (_expect100ContinueCompletionSource != null)
                    {
                        // If the final status code is >= 300, skip sending the body.
                        bool shouldSendBody = (statusCode < 300);

                        if (NetEventSource.Log.IsEnabled()) Trace($"Expecting 100 Continue but received final status {statusCode}.");
                        _expect100ContinueCompletionSource.TrySetResult(shouldSendBody);
                    }
                }
            }
            else if (_headerState == HeaderState.SkipExpect100Headers)
            {
                // Ignore any headers that came as part of an informational (i.e. 100 Continue) response.
                return;
            }
            else
            {
                string? headerValue = staticValue;

                if (headerValue is null)
                {
                    Encoding? encoding = _connection.Pool.Settings._responseHeaderEncodingSelector?.Invoke(descriptor.Name, _request);
                    headerValue = _connection.GetResponseHeaderValueWithCaching(descriptor, literalValue, encoding);
                }

                switch (_headerState)
                {
                    case HeaderState.StatusHeader:
                        if (NetEventSource.Log.IsEnabled()) Trace($"Received headers without :status.");
                        throw new Http3ConnectionException(Http3ErrorCode.ProtocolError);
                    case HeaderState.ResponseHeaders when descriptor.HeaderType.HasFlag(HttpHeaderType.Content):
                        _response!.Content!.Headers.TryAddWithoutValidation(descriptor, headerValue);
                        break;
                    case HeaderState.ResponseHeaders:
                        _response!.Headers.TryAddWithoutValidation(descriptor.HeaderType.HasFlag(HttpHeaderType.Request) ? descriptor.AsCustomHeader() : descriptor, headerValue);
                        break;
                    case HeaderState.TrailingHeaders:
                        _trailingHeaders!.Add((descriptor.HeaderType.HasFlag(HttpHeaderType.Request) ? descriptor.AsCustomHeader() : descriptor, headerValue));
                        break;
                    default:
                        Debug.Fail($"Unexpected {nameof(Http3RequestStream)}.{nameof(_headerState)} '{_headerState}'.");
                        break;
                }
            }
        }

        void IHttpStreamHeadersHandler.OnHeadersComplete(bool endStream)
        {
            Debug.Fail($"This has no use in HTTP/3 and should never be called by {nameof(QPackDecoder)}.");
        }

        private async ValueTask SkipUnknownPayloadAsync(long payloadLength, CancellationToken cancellationToken)
        {
            while (payloadLength != 0)
            {
                if (_recvBuffer.ActiveLength == 0)
                {
                    _recvBuffer.EnsureAvailableSpace(1);
                    int bytesRead = await _stream.ReadAsync(_recvBuffer.AvailableMemory, cancellationToken).ConfigureAwait(false);

                    if (bytesRead != 0)
                    {
                        _recvBuffer.Commit(bytesRead);
                    }
                    else
                    {
                        // Our buffer has partial frame data in it but not enough to complete the read: bail out.
                        throw new Http3ConnectionException(Http3ErrorCode.FrameError);
                    }
                }

                long readLength = Math.Min(payloadLength, _recvBuffer.ActiveLength);
                _recvBuffer.Discard((int)readLength);
                payloadLength -= readLength;
            }
        }

        private int ReadResponseContent(HttpResponseMessage response, Span<byte> buffer)
        {
            // Response headers should be done reading by the time this is called. _response is nulled out as part of this.
            // Verify that this is being called in correct order.
            Debug.Assert(_response == null);

            try
            {
                int totalBytesRead = 0;

                do
                {
                    // Sync over async here -- QUIC implementation does it per-I/O already; this is at least more coarse-grained.
                    if (_responseDataPayloadRemaining <= 0 && !ReadNextDataFrameAsync(response, CancellationToken.None).AsTask().GetAwaiter().GetResult())
                    {
                        // End of stream.
                        break;
                    }

                    if (_recvBuffer.ActiveLength != 0)
                    {
                        // Some of the payload is in our receive buffer, so copy it.

                        int copyLen = (int)Math.Min(buffer.Length, Math.Min(_responseDataPayloadRemaining, _recvBuffer.ActiveLength));
                        _recvBuffer.ActiveSpan.Slice(0, copyLen).CopyTo(buffer);

                        totalBytesRead += copyLen;
                        _responseDataPayloadRemaining -= copyLen;
                        _recvBuffer.Discard(copyLen);
                        buffer = buffer.Slice(copyLen);

                        // Stop, if we've reached the end of a data frame and start of the next data frame is not buffered yet
                        // Waiting for the next data frame may cause a hang, e.g. in echo scenario
                        // TODO: this is inefficient if data is already available in transport
                        if (_responseDataPayloadRemaining == 0 && _recvBuffer.ActiveLength == 0)
                        {
                            break;
                        }
                    }
                    else
                    {
                        // Receive buffer is empty -- bypass it and read directly into user's buffer.

                        int copyLen = (int)Math.Min(buffer.Length, _responseDataPayloadRemaining);
                        int bytesRead = _stream.Read(buffer.Slice(0, copyLen));

                        if (bytesRead == 0 && buffer.Length != 0)
                        {
                            throw new HttpRequestException(SR.Format(SR.net_http_invalid_response_premature_eof_bytecount, _responseDataPayloadRemaining));
                        }

                        totalBytesRead += bytesRead;
                        _responseDataPayloadRemaining -= bytesRead;
                        buffer = buffer.Slice(bytesRead);

                        // Stop, even if we are in the middle of a data frame. Waiting for the next data may cause a hang
                        // TODO: this is inefficient if data is already available in transport
                        break;
                    }
                }
                while (buffer.Length != 0);

                return totalBytesRead;
            }
            catch (Exception ex)
            {
                HandleReadResponseContentException(ex, CancellationToken.None);
                return 0; // never reached.
            }
        }

        private async ValueTask<int> ReadResponseContentAsync(HttpResponseMessage response, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            // Response headers should be done reading by the time this is called. _response is nulled out as part of this.
            // Verify that this is being called in correct order.
            Debug.Assert(_response == null);

            try
            {
                int totalBytesRead = 0;

                do
                {
                    if (_responseDataPayloadRemaining <= 0 && !await ReadNextDataFrameAsync(response, cancellationToken).ConfigureAwait(false))
                    {
                        // End of stream.
                        break;
                    }

                    if (_recvBuffer.ActiveLength != 0)
                    {
                        // Some of the payload is in our receive buffer, so copy it.

                        int copyLen = (int)Math.Min(buffer.Length, Math.Min(_responseDataPayloadRemaining, _recvBuffer.ActiveLength));
                        _recvBuffer.ActiveSpan.Slice(0, copyLen).CopyTo(buffer.Span);

                        totalBytesRead += copyLen;
                        _responseDataPayloadRemaining -= copyLen;
                        _recvBuffer.Discard(copyLen);
                        buffer = buffer.Slice(copyLen);

                        // Stop, if we've reached the end of a data frame and start of the next data frame is not buffered yet
                        // Waiting for the next data frame may cause a hang, e.g. in echo scenario
                        // TODO: this is inefficient if data is already available in transport
                        if (_responseDataPayloadRemaining == 0 && _recvBuffer.ActiveLength == 0)
                        {
                            break;
                        }
                    }
                    else
                    {
                        // Receive buffer is empty -- bypass it and read directly into user's buffer.

                        int copyLen = (int)Math.Min(buffer.Length, _responseDataPayloadRemaining);
                        int bytesRead = await _stream.ReadAsync(buffer.Slice(0, copyLen), cancellationToken).ConfigureAwait(false);

                        if (bytesRead == 0 && buffer.Length != 0)
                        {
                            throw new HttpRequestException(SR.Format(SR.net_http_invalid_response_premature_eof_bytecount, _responseDataPayloadRemaining));
                        }

                        totalBytesRead += bytesRead;
                        _responseDataPayloadRemaining -= bytesRead;
                        buffer = buffer.Slice(bytesRead);

                        // Stop, even if we are in the middle of a data frame. Waiting for the next data may cause a hang
                        // TODO: this is inefficient if data is already available in transport
                        break;
                    }
                }
                while (buffer.Length != 0);

                return totalBytesRead;
            }
            catch (Exception ex)
            {
                HandleReadResponseContentException(ex, cancellationToken);
                return 0; // never reached.
            }
        }

        private void HandleReadResponseContentException(Exception ex, CancellationToken cancellationToken)
        {
            switch (ex)
            {
                // Peer aborted the stream
                case QuicStreamAbortedException _:
                // User aborted the stream
                case QuicOperationAbortedException _:
                    throw new IOException(SR.net_http_client_execution_error, new HttpRequestException(SR.net_http_client_execution_error, ex));
                case QuicConnectionAbortedException _:
                    // Our connection was reset. Start aborting the connection.
                    Exception abortException = _connection.Abort(ex);
                    throw new IOException(SR.net_http_client_execution_error, new HttpRequestException(SR.net_http_client_execution_error, abortException));
                case Http3ConnectionException _:
                    // A connection-level protocol error has occurred on our stream.
                    _connection.Abort(ex);
                    throw new IOException(SR.net_http_client_execution_error, new HttpRequestException(SR.net_http_client_execution_error, ex));
                case OperationCanceledException oce when oce.CancellationToken == cancellationToken:
                    _stream.AbortRead((long)Http3ErrorCode.RequestCancelled);
                    ExceptionDispatchInfo.Throw(ex); // Rethrow.
                    return; // Never reached.
                default:
                    _stream.AbortRead((long)Http3ErrorCode.InternalError);
                    throw new IOException(SR.net_http_client_execution_error, new HttpRequestException(SR.net_http_client_execution_error, ex));
            }
        }

        private async ValueTask<bool> ReadNextDataFrameAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (_responseDataPayloadRemaining == -1)
            {
                // EOS -- this branch will only be taken if user calls Read again after EOS.
                return false;
            }

            Http3FrameType? frameType;
            long payloadLength;

            while (true)
            {
                (frameType, payloadLength) = await ReadFrameEnvelopeAsync(cancellationToken).ConfigureAwait(false);

                switch (frameType)
                {
                    case Http3FrameType.Data:
                        // Ignore DATA frames with 0 length.
                        if (payloadLength == 0)
                        {
                            continue;
                        }
                        _responseDataPayloadRemaining = payloadLength;
                        return true;
                    case Http3FrameType.Headers:
                        // Read any trailing headers.
                        _trailingHeaders = new List<(HeaderDescriptor name, string value)>();
                        await ReadHeadersAsync(payloadLength, cancellationToken).ConfigureAwait(false);

                        // There may be more frames after this one, but they would all be unknown extension
                        // frames that we are allowed to skip. Just close the stream early.

                        // Note: if a server sends additional HEADERS or DATA frames at this point, it
                        // would be a connection error -- not draining the stream means we won't catch this.
                        goto case null;
                    case null:
                        // End of stream.
                        CopyTrailersToResponseMessage(response);

                        _responseDataPayloadRemaining = -1; // Set to -1 to indicate EOS.
                        return false;
                }
            }
        }

        public void Trace(string message, [CallerMemberName] string? memberName = null) =>
            _connection.Trace(StreamId, message, memberName);

        private void AbortStream()
        {
            // If the request body isn't completed, cancel it now.
            if (_requestContentLengthRemaining != 0) // 0 is used for the end of content writing, -1 is used for unknown Content-Length
            {
                _stream.AbortWrite((long)Http3ErrorCode.RequestCancelled);
            }
            // If the response body isn't completed, cancel it now.
            if (_responseDataPayloadRemaining != -1) // -1 is used for EOF, 0 for consumed DATA frame payload before the next read
            {
                _stream.AbortRead((long)Http3ErrorCode.RequestCancelled);
            }
        }

        // TODO: it may be possible for Http3RequestStream to implement Stream directly and avoid this allocation.
        private sealed class Http3ReadStream : HttpBaseStream
        {
            private Http3RequestStream? _stream;
            private HttpResponseMessage? _response;

            public override bool CanRead => _stream != null;

            public override bool CanWrite => false;

            public Http3ReadStream(Http3RequestStream stream)
            {
                _stream = stream;
                _response = stream._response;
            }

            ~Http3ReadStream()
            {
                Dispose(false);
            }

            protected override void Dispose(bool disposing)
            {
                Http3RequestStream? stream = Interlocked.Exchange(ref _stream, null);
                if (stream is null)
                {
                    return;
                }

                if (disposing)
                {
                    // This will remove the stream from the connection properly.
                    stream.Dispose();
                }
                else
                {
                    // We shouldn't be using a managed instance here, but don't have much choice -- we
                    // need to remove the stream from the connection's GOAWAY collection and properly abort.
                    stream.AbortStream();
                    stream._connection.RemoveStream(stream._stream);
                    stream._connection = null!;
                }

                _response = null;

                base.Dispose(disposing);
            }

            public override async ValueTask DisposeAsync()
            {
                Http3RequestStream? stream = Interlocked.Exchange(ref _stream, null);
                if (stream is null)
                {
                    return;
                }

                await stream.DisposeAsync().ConfigureAwait(false);

                _response = null;

                await base.DisposeAsync().ConfigureAwait(false);
            }

            public override int Read(Span<byte> buffer)
            {
                Http3RequestStream? stream = _stream;

                if (stream is null)
                {
                    throw new ObjectDisposedException(nameof(Http3RequestStream));
                }

                Debug.Assert(_response != null);
                return stream.ReadResponseContent(_response, buffer);
            }

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
            {
                Http3RequestStream? stream = _stream;

                if (stream is null)
                {
                    return ValueTask.FromException<int>(new ObjectDisposedException(nameof(Http3RequestStream)));
                }

                Debug.Assert(_response != null);
                return stream.ReadResponseContentAsync(_response, buffer, cancellationToken);
            }

            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }
        }

        // TODO: it may be possible for Http3RequestStream to implement Stream directly and avoid this allocation.
        private sealed class Http3WriteStream : HttpBaseStream
        {
            private Http3RequestStream? _stream;

            public long BytesWritten { get; private set; }

            public override bool CanRead => false;

            public override bool CanWrite => _stream != null;

            public Http3WriteStream(Http3RequestStream stream)
            {
                _stream = stream;
            }

            protected override void Dispose(bool disposing)
            {
                _stream = null;
                base.Dispose(disposing);
            }

            public override int Read(Span<byte> buffer)
            {
                throw new NotSupportedException();
            }

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }

            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
            {
                BytesWritten += buffer.Length;

                Http3RequestStream? stream = _stream;

                if (stream is null)
                {
                    return ValueTask.FromException(new ObjectDisposedException(nameof(Http3WriteStream)));
                }

                return stream.WriteRequestContentAsync(buffer, cancellationToken);
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                Http3RequestStream? stream = _stream;

                if (stream is null)
                {
                    return Task.FromException(new ObjectDisposedException(nameof(Http3WriteStream)));
                }

                return stream.FlushSendBufferAsync(endStream: false, cancellationToken).AsTask();
            }
        }

        private enum HeaderState
        {
            StatusHeader,
            SkipExpect100Headers,
            ResponseHeaders,
            TrailingHeaders
        }
    }
}
