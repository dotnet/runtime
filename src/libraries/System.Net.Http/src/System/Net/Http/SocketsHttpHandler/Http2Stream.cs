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

            private ArrayBuffer _responseBuffer;
            private TaskCompletionSource<bool> _responseDataAvailable;
            private bool _responseComplete;
            private bool _responseAborted;

            private readonly CreditManager _streamWindow;

            private HttpResponseMessage _response;
            private bool _disposed;

            public Http2Stream(Http2Connection connection)
            {
                _connection = connection;

                _streamId = connection.AddStream(this);

                _syncObject = new object();
                _disposed = false;

                _responseBuffer = new ArrayBuffer(InitialBufferSize);

                _streamWindow = new CreditManager(InitialWindowSize);
            }

            public int StreamId => _streamId;

            public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                // TODO: ISSUE 31310: Cancellation support

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

                // Send headers
                await _connection.SendHeadersAsync(_streamId, request).ConfigureAwait(false);

                // Send request body, if any
                if (request.Content != null)
                {
                    using (Http2WriteStream writeStream = new Http2WriteStream(this))
                    {
                        await request.Content.CopyToAsync(writeStream).ConfigureAwait(false);
                    }
                }

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

            public void OnWindowUpdate(int amount)
            {
                _streamWindow.AdjustCredit(amount);
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

                    if (_responseBuffer.ActiveSpan.Length + buffer.Length > InitialWindowSize)
                    {
                        // Window size exceeded.
                        throw new Http2ProtocolException(Http2ProtocolErrorCode.FlowControlError);
                    }

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

                int bytesToRead = Math.Min(buffer.Length, _responseBuffer.ActiveSpan.Length);
                _responseBuffer.ActiveSpan.Slice(0, bytesToRead).CopyTo(buffer);
                _responseBuffer.Discard(bytesToRead);

                // Send a window update to the peer.
                // Don't wait for completion, which could happen asynchronously.
                ValueTask ignored = _connection.SendWindowUpdateAsync(_streamId, bytesToRead);

                return bytesToRead;
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

            private async ValueTask SendDataAsync(ReadOnlyMemory<byte> buffer)
            {
                ReadOnlyMemory<byte> remaining = buffer;

                while (remaining.Length > 0)
                {
                    int sendSize = await _streamWindow.RequestCreditAsync(remaining.Length).ConfigureAwait(false);

                    ReadOnlyMemory<byte> current;
                    (current, remaining) = SplitBuffer(remaining, sendSize);

                    await _connection.SendStreamDataAsync(_streamId, current).ConfigureAwait(false);
                }
            }

            public void Dispose()
            {
                lock (_syncObject)
                {
                    if (!_disposed)
                    {
                        _disposed = true;

                        _streamWindow.Dispose();

                        // TODO: ISSUE 31310: If the stream is not complete, we should send RST_STREAM
                    }
                }
            }

            private sealed class Http2ReadStream : BaseAsyncStream
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


            private sealed class Http2WriteStream : BaseAsyncStream
            {
                private readonly Http2Stream _http2Stream;
                private int _disposed; // 0==no, 1==yes

                public Http2WriteStream(Http2Stream http2Stream)
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

                    // Don't wait for completion, which could happen asynchronously.
                    ValueTask ignored = _http2Stream._connection.SendEndStreamAsync(_http2Stream.StreamId);

                    base.Dispose(disposing);
                }

                public override bool CanRead => false;
                public override bool CanWrite => true;

                public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken) => throw new NotSupportedException();

                public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
                {
                    return _http2Stream.SendDataAsync(buffer);
                }

                public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            }
        }
    }
}
