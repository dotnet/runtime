// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http.HPack;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed partial class Http2Connection 
    {
        // TODO: ISSUE 31301: Should we pool Http2Stream objects?
        sealed class Http2Stream : IDisposable
        {
            private readonly Http2Connection _connection;
            private readonly int _streamId;
            private readonly object _syncObject;

            // TODO: In debug build, make initial size small (10)
            private ArrayBuffer _requestBuffer;

            private ArrayBuffer _responseBuffer;
            private TaskCompletionSource<bool> _responseDataAvailable;
            private bool _responseComplete;
            private bool _responseAborted;

            private HttpResponseMessage _response;
            private bool _disposed;

            public Http2Stream(Http2Connection connection)
            {
                _connection = connection;

                _streamId = connection.AddStream(this);

                _syncObject = new object();
                _disposed = false;

                _requestBuffer = new ArrayBuffer(InitialBufferSize);
                _responseBuffer = new ArrayBuffer(InitialBufferSize);
            }

            public int StreamId => _streamId;

            private void GrowWriteBuffer()
            {
                _requestBuffer.EnsureAvailableSpace(_requestBuffer.AvailableSpan.Length + 1);
            }

            struct HeaderEncodingState
            {
                public bool IsFirstFrame;
                public bool IsEmptyResponse;
                public int CurrentFrameOffset;
            }

            private void WriteCurrentFrameHeader(ref HeaderEncodingState state, int frameLength, bool isLastFrame)
            {
                Debug.Assert(frameLength > 0);

                FrameHeader frameHeader = new FrameHeader();
                frameHeader.Length = frameLength;
                frameHeader.StreamId = _streamId;

                if (state.IsFirstFrame)
                {
                    frameHeader.Type = FrameType.Headers;
                    frameHeader.Flags = (state.IsEmptyResponse ? FrameFlags.EndStream : FrameFlags.None);
                }
                else
                {
                    frameHeader.Type = FrameType.Continuation;
                    frameHeader.Flags = FrameFlags.None;
                }

                if (isLastFrame)
                {
                    frameHeader.Flags |= FrameFlags.EndHeaders;
                }

                // Update the curent HEADERS or CONTINUATION frame with length, and write it to the buffer.
                frameHeader.WriteTo(_requestBuffer.ActiveSpan.Slice(state.CurrentFrameOffset));
            }

            private void WriteHeader(ref HeaderEncodingState state, string name, string value)
            {
                // TODO: ISSUE 31307: Use static table for known headers

                int bytesWritten;
                while (!HPackEncoder.EncodeHeader(name, value, _requestBuffer.AvailableSpan, out bytesWritten))
                {
                    GrowWriteBuffer();
                }

                _requestBuffer.Commit(bytesWritten);

                while (_requestBuffer.ActiveSpan.Slice(state.CurrentFrameOffset).Length > FrameHeader.Size + FrameHeader.MaxLength)
                {
                    // We've exceeded the frame size limit.

                    // Fill in the current frame header.
                    WriteCurrentFrameHeader(ref state, FrameHeader.MaxLength, false);

                    state.IsFirstFrame = false;
                    state.CurrentFrameOffset += FrameHeader.Size + FrameHeader.MaxLength;

                    // Reserve space for new frame header
                    _requestBuffer.Commit(FrameHeader.Size);

                    Span<byte> currentFrameSpan = _requestBuffer.ActiveSpan.Slice(state.CurrentFrameOffset);

                    // Shift the remainder down to make room for the new frame header.
                    // We'll fill this in when the frame is complete.
                    currentFrameSpan.Slice(0, currentFrameSpan.Length - FrameHeader.Size).CopyTo(currentFrameSpan.Slice(FrameHeader.Size));
                }
            }

            private void WriteHeaders(ref HeaderEncodingState state, HttpHeaders headers)
            {
                foreach (KeyValuePair<HeaderDescriptor, string[]> header in headers.GetHeaderDescriptorsAndValues())
                {
                    if (header.Key.KnownHeader == KnownHeaders.Host)
                    {
                        continue;
                    }

                    Debug.Assert(header.Value.Length > 0, "No values for header??");
                    for (int i = 0; i < header.Value.Length; i++)
                    {
                        WriteHeader(ref state, header.Key.Name, header.Value[i]);
                    }
                }
            }

            private void WriteHeaders(HttpRequestMessage request)
            {
                // TODO: ISSUE 31305: Disallow sending Connection: and Transfer-Encoding: chunked

                HeaderEncodingState state = new HeaderEncodingState() { IsFirstFrame = true, IsEmptyResponse = (request.Content == null), CurrentFrameOffset = 0 };

                // Initialize the HEADERS frame header.
                // We will write it to the buffer later, when the frame is complete.
                FrameHeader currentFrameHeader = new FrameHeader(0, FrameType.Headers, (request.Content == null ? FrameFlags.EndStream : FrameFlags.None), _streamId);

                // Reserve space for the frame header.
                // We will fill it in later, when the frame is complete.
                _requestBuffer.EnsureAvailableSpace(FrameHeader.Size);
                _requestBuffer.Commit(FrameHeader.Size);

                HttpMethod normalizedMethod = HttpMethod.Normalize(request.Method);

                // TODO: ISSUE 31307: Use static table for pseudo-headers
                WriteHeader(ref state, ":method", normalizedMethod.Method);
                WriteHeader(ref state, ":scheme", "https");

                string authority;
                if (request.HasHeaders && request.Headers.Host != null)
                {
                    authority = request.Headers.Host;
                }
                else
                {
                    authority = request.RequestUri.IdnHost;
                    if (!request.RequestUri.IsDefaultPort)
                    {
                        // TODO: Avoid allocation here by caching this on the connection or pool
                        authority += ":" + request.RequestUri.Port;
                    }
                }

                WriteHeader(ref state, ":authority", authority);
                WriteHeader(ref state, ":path", request.RequestUri.GetComponents(UriComponents.PathAndQuery | UriComponents.Fragment, UriFormat.UriEscaped));

                if (request.HasHeaders)
                {
                    WriteHeaders(ref state, request.Headers);
                }

                // Determine cookies to send.
                if (_connection._pool.Settings._useCookies)
                {
                    string cookiesFromContainer = _connection._pool.Settings._cookieContainer.GetCookieHeader(request.RequestUri);
                    if (cookiesFromContainer != string.Empty)
                    {
                        WriteHeader(ref state, HttpKnownHeaderNames.Cookie, cookiesFromContainer);
                    }
                }

                if (request.Content == null)
                {
                    // Write out Content-Length: 0 header to indicate no body,
                    // unless this is a method that never has a body.
                    if (normalizedMethod.MustHaveRequestBody)
                    {
                        // TODO: ISSUE 31307: Use static table for Content-Length
                        WriteHeader(ref state, "Content-Length", "0");
                    }
                }
                else
                {
                    WriteHeaders(ref state, request.Content.Headers);
                }

                // Update the last frame header and write it to the buffer.
                WriteCurrentFrameHeader(ref state, _requestBuffer.ActiveSpan.Slice(state.CurrentFrameOffset).Length - FrameHeader.Size, true);
            }

            public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                // TODO: ISSUE 31310: Cancellation support

                WriteHeaders(request);

                // Send request body, if any
                if (request.Content != null)
                {
                    throw new NotImplementedException("Request body not supported yet");
                }

                // Construct response

                HttpConnectionResponseContent responseContent = new HttpConnectionResponseContent();
                _response = new HttpResponseMessage() { Version = HttpVersion.Version20, RequestMessage = request, Content = responseContent };

                // TODO: ISSUE 31312: Expect: 100-continue and early response handling
                // Note that in an "early response" scenario, where we get a response before we've finished sending the request body
                // (either with a 100-continue that timed out, or without 100-continue),
                // we can stop send a RST_STREAM on the request stream and stop sending the request without tearing down the entire connection.

                // TODO: ISSUE 31313: Avoid allocating a TaskCompletionSource repeatedly by using a resettable ValueTaskSource.
                // See: https://github.com/dotnet/corefx/blob/master/src/Common/tests/System/Threading/Tasks/Sources/ManualResetValueTaskSource.cs
                Debug.Assert(_responseDataAvailable == null);
                _responseDataAvailable = new TaskCompletionSource<bool>();
                Task readDataAvailableTask = _responseDataAvailable.Task;

                await _connection.SendFramesAsync(_requestBuffer.ActiveMemory).ConfigureAwait(false);
                _requestBuffer.Discard(_requestBuffer.ActiveSpan.Length);

                // Wait for response headers to be read.
                await readDataAvailableTask.ConfigureAwait(false);

                // Start to process the response body.
                bool emptyResponse = false;
                lock (_syncObject)
                {
                    if (_responseComplete && _responseBuffer.ActiveSpan.Length == 0)
                    {
                        if (_responseAborted)
                        {
                            throw new IOException(SR.net_http_invalid_response);
                        }

                        emptyResponse = true;
                    }
                }

                if (emptyResponse)
                {
                    responseContent.SetStream(EmptyReadStream.Instance);
                }
                else
                {
                    responseContent.SetStream(new Http2ReadStream(this));
                }

                // Process Set-Cookie headers.
                if (_connection._pool.Settings._useCookies)
                {
                    CookieHelper.ProcessReceivedCookies(_response, _connection._pool.Settings._cookieContainer);
                }

                return _response;
            }

            private static readonly byte[] s_statusHeaderName = Encoding.ASCII.GetBytes(":status");

            // Copied from HttpConnection
            // TODO: Consolidate this logic?
            private static bool IsDigit(byte c) => (uint)(c - '0') <= '9' - '0';

            public void OnResponseHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
            {
                // TODO: ISSUE 31309: Optimize HPACK static table decoding

                if (name.SequenceEqual(s_statusHeaderName))
                {
                    if (value.Length != 3)
                        throw new Exception("Invalid status code");

                    // Copied from HttpConnection
                    byte status1 = value[0], status2 = value[1], status3 = value[2];
                    if (!IsDigit(status1) || !IsDigit(status2) || !IsDigit(status3))
                    {
                        throw new HttpRequestException(SR.net_http_invalid_response);
                    }

                    _response.SetStatusCodeWithoutValidation((HttpStatusCode)(100 * (status1 - '0') + 10 * (status2 - '0') + (status3 - '0')));
                }
                else
                {
                    if (!HeaderDescriptor.TryGet(name, out HeaderDescriptor descriptor))
                    {
                        // Invalid header name
                        throw new HttpRequestException(SR.net_http_invalid_response);
                    }

                    string headerValue = descriptor.GetHeaderValue(value);

                    // Note we ignore the return value from TryAddWithoutValidation; 
                    // if the header can't be added, we silently drop it.
                    if (descriptor.HeaderType == HttpHeaderType.Content)
                    {
                        _response.Content.Headers.TryAddWithoutValidation(descriptor, headerValue);
                    }
                    else
                    {
                        _response.Headers.TryAddWithoutValidation(descriptor, headerValue);
                    }
                }
            }

            public void OnResponseHeadersComplete(bool endStream)
            {
                if (endStream)
                {
                    _responseComplete = true;
                }

                TaskCompletionSource<bool> readDataAvailable = _responseDataAvailable;
                _responseDataAvailable = null;
                readDataAvailable.SetResult(true);
            }

            public void OnResponseData(ReadOnlySpan<byte> buffer, bool endStream)
            {
                TaskCompletionSource<bool> readDataAvailable = null;

                lock (_syncObject)
                {
                    if (_disposed)
                    {
                        return;
                    }

                    Debug.Assert(!_responseComplete);

                    _responseBuffer.EnsureAvailableSpace(buffer.Length);
                    buffer.CopyTo(_responseBuffer.AvailableSpan);
                    _responseBuffer.Commit(buffer.Length);

                    if (endStream)
                    {
                        _responseComplete = true;
                    }

                    if (_responseDataAvailable != null)
                    {
                        readDataAvailable = _responseDataAvailable;
                        _responseDataAvailable = null;
                    }
                }

                if (readDataAvailable != null)
                {
                    readDataAvailable.SetResult(true);
                }
            }

            public void OnResponseAbort()
            {
                TaskCompletionSource<bool> readDataAvailable = null;

                lock (_syncObject)
                {
                    if (_disposed)
                    {
                        return;
                    }

                    Debug.Assert(!_responseComplete);

                    _responseComplete = true;
                    _responseAborted = true;

                    if (_responseDataAvailable != null)
                    {
                        readDataAvailable = _responseDataAvailable;
                        _responseDataAvailable = null;
                    }
                }

                if (readDataAvailable != null)
                {
                    readDataAvailable.SetResult(true);
                }
            }

            private int ReadFromBuffer(Span<byte> buffer)
            {
                Debug.Assert(_responseBuffer.ActiveSpan.Length > 0);
                Debug.Assert(buffer.Length > 0);

                int bytesToCopy = Math.Min(buffer.Length, _responseBuffer.ActiveSpan.Length);
                _responseBuffer.ActiveSpan.Slice(0, bytesToCopy).CopyTo(buffer);
                _responseBuffer.Discard(bytesToCopy);
                return bytesToCopy;
            }

            public async ValueTask<int> ReadDataAsyncCore(Task onDataAvailable, Memory<byte> buffer)
            {
                await onDataAvailable.ConfigureAwait(false);

                lock (_syncObject)
                {
                    if (_responseBuffer.ActiveSpan.Length > 0)
                    {
                        return ReadFromBuffer(buffer.Span);
                    }

                    // If no data was made available, we must be at the end of the stream
                    Debug.Assert(_responseComplete);
                    return 0;
                }
            }

            // TODO: ISSUE 31298: Window manangement
            // TODO: ISSUE 31310: Cancellation support

            public ValueTask<int> ReadDataAsync(Memory<byte> buffer, CancellationToken cancellationToken)
            {
                if (buffer.Length == 0)
                {
                    return new ValueTask<int>(0);
                }

                Task onDataAvailable;
                lock (_syncObject)
                {
                    if (_responseBuffer.ActiveSpan.Length > 0)
                    {
                        return new ValueTask<int>(ReadFromBuffer(buffer.Span));
                    }

                    if (_responseComplete)
                    {
                        if (_responseAborted)
                        {
                            return new ValueTask<int>(Task.FromException<int>(new IOException(SR.net_http_invalid_response)));
                        }

                        return new ValueTask<int>(0);
                    }

                    Debug.Assert(_responseDataAvailable == null);
                    Debug.Assert(!_responseAborted);
                    _responseDataAvailable = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    onDataAvailable = _responseDataAvailable.Task;
                }

                return ReadDataAsyncCore(onDataAvailable, buffer);
            }

            public void Dispose()
            {
                lock (_syncObject)
                {
                    if (!_disposed)
                    {
                        _disposed = true;

                        // TODO: ISSUE 31310: If the stream is not complete, we should send RST_STREAM
                    }
                }
            }

            sealed class Http2ReadStream : BaseAsyncStream
            {
                private readonly Http2Stream _http2Stream;
                private int _disposed; // 0==no, 1==yes

                public Http2ReadStream(Http2Stream http2Stream)
                {
                    Debug.Assert(http2Stream != null);
                    _http2Stream = http2Stream;
                }

                protected override void Dispose(bool disposing)
                {
                    if (Interlocked.Exchange(ref _disposed, 1) != 0)
                    {
                        return;
                    }

                    if (disposing)
                    {
                        Debug.Assert(_http2Stream != null);
                        _http2Stream.Dispose();
                    }

                    base.Dispose(disposing);
                }

                public override bool CanRead => true;
                public override bool CanWrite => false;

                public override ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken)
                {
                    return _http2Stream.ReadDataAsync(destination, cancellationToken);
                }

                public override ValueTask WriteAsync(ReadOnlyMemory<byte> destination, CancellationToken cancellationToken) => throw new NotSupportedException();

                public override Task FlushAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
            }
        }
    }
}
