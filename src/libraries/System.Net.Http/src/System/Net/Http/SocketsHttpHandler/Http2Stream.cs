// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Net.Http
{
    internal sealed partial class Http2Connection
    {
        private sealed class Http2Stream : IValueTaskSource, IDisposable
        {
            private enum StreamState : byte
            {
                ExpectingHeaders,
                ExpectingData,
                ExpectingTrailingHeaders,
                Complete,
                Aborted
            }

            private const int InitialStreamBufferSize =
#if DEBUG
                10;
#else
                1024;
#endif

            private readonly Http2Connection _connection;
            private readonly int _streamId;
            private readonly CreditManager _streamWindow;
            private readonly HttpRequestMessage _request;
            private HttpResponseMessage _response;

            private ArrayBuffer _responseBuffer; // mutable struct, do not make this readonly
            private int _pendingWindowUpdate;

            private StreamState _state;
            private bool _disposed;
            private Exception _abortException;

            /// <summary>The core logic for the IValueTaskSource implementation.</summary>
            private ManualResetValueTaskSourceCore<bool> _waitSource = new ManualResetValueTaskSourceCore<bool> { RunContinuationsAsynchronously = true }; // mutable struct, do not make this readonly
            /// <summary>
            /// Whether code has requested or is about to request a wait be performed and thus requires a call to SetResult to complete it.
            /// This is read and written while holding the lock so that most operations on _waitSourceCore don't need to be.
            /// </summary>
            private bool _hasWaiter;

            private const int StreamWindowSize = DefaultInitialWindowSize;

            // See comment on ConnectionWindowThreshold.
            private const int StreamWindowThreshold = StreamWindowSize / 8;

            public Http2Stream(HttpRequestMessage request, Http2Connection connection, int streamId, int initialWindowSize)
            {
                _connection = connection;
                _streamId = streamId;

                _state = StreamState.ExpectingHeaders;

                _request = request;

                _disposed = false;

                _responseBuffer = new ArrayBuffer(InitialStreamBufferSize, usePool: true);

                _pendingWindowUpdate = 0;

                _streamWindow = new CreditManager(initialWindowSize);
            }

            private object SyncObject => _streamWindow;

            public int StreamId => _streamId;
            public HttpRequestMessage Request => _request;
            public HttpResponseMessage Response => _response;

            public async Task SendRequestBodyAsync(CancellationToken cancellationToken)
            {
                // TODO: ISSUE 31312: Expect: 100-continue and early response handling
                // Note that in an "early response" scenario, where we get a response before we've finished sending the request body
                // (either with a 100-continue that timed out, or without 100-continue),
                // we can stop send a RST_STREAM on the request stream and stop sending the request without tearing down the entire connection.

                // Send request body, if any
                if (_request.Content != null)
                {
                    using (Http2WriteStream writeStream = new Http2WriteStream(this))
                    {
                        await _request.Content.CopyToAsync(writeStream, null, cancellationToken).ConfigureAwait(false);
                    }

                    // Don't wait for completion, which could happen asynchronously.
                    Task ignored = _connection.SendEndStreamAsync(_streamId);
                }
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
                Debug.Assert(name != null && name.Length > 0);
                // TODO: ISSUE 31309: Optimize HPACK static table decoding

                lock (SyncObject)
                {
                    if (_state != StreamState.ExpectingHeaders && _state != StreamState.ExpectingTrailingHeaders)
                    {
                        throw new Http2ProtocolException(Http2ProtocolErrorCode.ProtocolError);
                    }

                    if (name[0] == (byte)':')
                    {
                        if (_state == StreamState.ExpectingTrailingHeaders)
                        {
                            // Pseudo-headers not allowed in trailers.
                            if (NetEventSource.IsEnabled) _connection.Trace("Pseudo-header in trailer headers.");
                            throw new HttpRequestException(SR.net_http_invalid_response_pseudo_header_in_trailer);
                        }

                        if (name.SequenceEqual(s_statusHeaderName))
                        {
                            if (_response != null)
                            {
                                if (NetEventSource.IsEnabled) _connection.Trace("Received duplicate status headers.");
                                throw new Http2ProtocolException(Http2ProtocolErrorCode.ProtocolError);
                            }

                            byte status1, status2, status3;
                            if (value.Length != 3 ||
                                !IsDigit(status1 = value[0]) ||
                                !IsDigit(status2 = value[1]) ||
                                !IsDigit(status3 = value[2]))
                            {
                                throw new HttpRequestException(SR.Format(SR.net_http_invalid_response_status_code, Encoding.ASCII.GetString(value)));
                            }

                            int statusValue = (100 * (status1 - '0') + 10 * (status2 - '0') + (status3 - '0'));
                            _response = new HttpResponseMessage()
                            {
                                Version = HttpVersion.Version20,
                                RequestMessage = _request,
                                Content = new HttpConnectionResponseContent(),
                                StatusCode = (HttpStatusCode)statusValue
                            };
                        }
                        else
                        {
                            if (NetEventSource.IsEnabled) _connection.Trace("Invalid response pseudo-header '{System.Text.Encoding.ASCII.GetString(name)}'.");
                            throw new HttpRequestException(SR.net_http_invalid_response);
                        }
                    }
                    else
                    {
                        if (_response == null)
                        {
                            if (NetEventSource.IsEnabled) _connection.Trace($"Received header before status pseudo-header.");
                            throw new HttpRequestException(SR.net_http_invalid_response);
                        }

                        if (!HeaderDescriptor.TryGet(name, out HeaderDescriptor descriptor))
                        {
                            // Invalid header name
                            throw new HttpRequestException(SR.Format(SR.net_http_invalid_response_header_name, Encoding.ASCII.GetString(name)));
                        }

                        string headerValue = descriptor.GetHeaderValue(value);

                        // Note we ignore the return value from TryAddWithoutValidation;
                        // if the header can't be added, we silently drop it.
                        if (_state == StreamState.ExpectingTrailingHeaders)
                        {
                            _response.TrailingHeaders.TryAddWithoutValidation(descriptor.HeaderType == HttpHeaderType.Request ? descriptor.AsCustomHeader() : descriptor, headerValue);
                        }
                        else if (descriptor.HeaderType == HttpHeaderType.Content)
                        {
                            _response.Content.Headers.TryAddWithoutValidation(descriptor, headerValue);
                        }
                        else
                        {
                            _response.Headers.TryAddWithoutValidation(descriptor.HeaderType == HttpHeaderType.Request ? descriptor.AsCustomHeader() : descriptor, headerValue);
                        }
                    }
                }
            }

            public void OnResponseHeadersStart()
            {
                lock (SyncObject)
                {
                    if (_state != StreamState.ExpectingHeaders && _state != StreamState.ExpectingData)
                    {
                        throw new Http2ProtocolException(Http2ProtocolErrorCode.ProtocolError);
                    }

                    if (_state == StreamState.ExpectingData)
                    {
                        _state = StreamState.ExpectingTrailingHeaders;
                    }
                }
            }

            public void OnResponseHeadersComplete(bool endStream)
            {
                bool signalWaiter;
                lock (SyncObject)
                {
                    if (_state != StreamState.ExpectingHeaders && _state != StreamState.ExpectingTrailingHeaders)
                    {
                        throw new Http2ProtocolException(Http2ProtocolErrorCode.ProtocolError);
                    }

                    if (_state == StreamState.ExpectingTrailingHeaders || endStream)
                    {
                        _state = StreamState.Complete;
                    }
                    else
                    {
                        _state = StreamState.ExpectingData;
                    }

                    signalWaiter = _hasWaiter;
                    _hasWaiter = false;
                }

                if (signalWaiter)
                {
                    _waitSource.SetResult(true);
                }
            }

            public void OnResponseData(ReadOnlySpan<byte> buffer, bool endStream)
            {
                bool signalWaiter;
                lock (SyncObject)
                {
                    if (_disposed)
                    {
                        return;
                    }

                    if (_state != StreamState.ExpectingData)
                    {
                        throw new Http2ProtocolException(Http2ProtocolErrorCode.ProtocolError);
                    }

                    if (_responseBuffer.ActiveSpan.Length + buffer.Length > StreamWindowSize)
                    {
                        // Window size exceeded.
                        throw new Http2ProtocolException(Http2ProtocolErrorCode.FlowControlError);
                    }

                    _responseBuffer.EnsureAvailableSpace(buffer.Length);
                    buffer.CopyTo(_responseBuffer.AvailableSpan);
                    _responseBuffer.Commit(buffer.Length);

                    if (endStream)
                    {
                        _state = StreamState.Complete;
                    }

                    signalWaiter = _hasWaiter;
                    _hasWaiter = false;
                }

                if (signalWaiter)
                {
                    _waitSource.SetResult(true);
                }
            }

            public void OnResponseAbort(Exception abortException)
            {
                bool signalWaiter;
                lock (SyncObject)
                {
                    if (_disposed)
                    {
                        return;
                    }

                    if (_state == StreamState.Aborted)
                    {
                        return;
                    }

                    _abortException = abortException;
                    _state = StreamState.Aborted;

                    signalWaiter = _hasWaiter;
                    _hasWaiter = false;
                }

                if (signalWaiter)
                {
                    _waitSource.SetResult(true);
                }
            }

            private (bool wait, bool isEmptyResponse) TryEnsureHeaders()
            {
                lock (SyncObject)
                {
                    if (_disposed)
                    {
                        throw new ObjectDisposedException(nameof(Http2Stream));
                    }

                    if (_state == StreamState.Aborted)
                    {
                        throw new IOException(SR.net_http_request_aborted, _abortException);
                    }
                    else if (_state == StreamState.ExpectingHeaders)
                    {
                        Debug.Assert(!_hasWaiter);
                        _hasWaiter = true;
                        _waitSource.Reset();
                        return (true, false);
                    }
                    else if (_state == StreamState.ExpectingData || _state == StreamState.ExpectingTrailingHeaders)
                    {
                        return (false, false);
                    }
                    else
                    {
                        Debug.Assert(_state == StreamState.Complete);
                        return (false, _responseBuffer.ActiveSpan.Length == 0);
                    }
                }
            }

            public async Task ReadResponseHeadersAsync()
            {
                // Wait for response headers to be read.
                (bool wait, bool emptyResponse) = TryEnsureHeaders();
                if (wait)
                {
                    await GetWaiterTask().ConfigureAwait(false);
                    (wait, emptyResponse) = TryEnsureHeaders();
                    Debug.Assert(!wait);
                }

                // Start to process the response body.
                ((HttpConnectionResponseContent)_response.Content).SetStream(emptyResponse ?
                    EmptyReadStream.Instance :
                    (Stream)new Http2ReadStream(this));

                // Process Set-Cookie headers.
                if (_connection._pool.Settings._useCookies)
                {
                    CookieHelper.ProcessReceivedCookies(_response, _connection._pool.Settings._cookieContainer);
                }
            }

            private void ExtendWindow(int amount)
            {
                Debug.Assert(amount > 0);
                Debug.Assert(_pendingWindowUpdate < StreamWindowThreshold);

                if (_state != StreamState.ExpectingData)
                {
                    // We are not expecting any more data (because we've either completed or aborted).
                    // So no need to send any more WINDOW_UPDATEs.
                    return;
                }

                _pendingWindowUpdate += amount;
                if (_pendingWindowUpdate < StreamWindowThreshold)
                {
                    return;
                }

                int windowUpdateSize = _pendingWindowUpdate;
                _pendingWindowUpdate = 0;

                Task ignored = _connection.SendWindowUpdateAsync(_streamId, windowUpdateSize);
            }

            private (bool wait, int bytesRead) TryReadFromBuffer(Span<byte> buffer)
            {
                Debug.Assert(buffer.Length > 0);

                lock (SyncObject)
                {
                    if (_disposed)
                    {
                        throw new ObjectDisposedException(nameof(Http2Stream));
                    }

                    if (_responseBuffer.ActiveSpan.Length > 0)
                    {
                        int bytesRead = Math.Min(buffer.Length, _responseBuffer.ActiveSpan.Length);
                        _responseBuffer.ActiveSpan.Slice(0, bytesRead).CopyTo(buffer);
                        _responseBuffer.Discard(bytesRead);

                        return (false, bytesRead);
                    }
                    else if (_state == StreamState.Complete)
                    {
                        return (false, 0);
                    }
                    else if (_state == StreamState.Aborted)
                    {
                        throw new IOException(SR.net_http_request_aborted, _abortException);
                    }

                    Debug.Assert(_state == StreamState.ExpectingData || _state == StreamState.ExpectingTrailingHeaders);

                    Debug.Assert(!_hasWaiter);
                    _hasWaiter = true;
                    _waitSource.Reset();
                    return (true, 0);
                }
            }

            public int ReadData(Span<byte> buffer)
            {
                if (buffer.Length == 0)
                {
                    return 0;
                }

                (bool wait, int bytesRead) = TryReadFromBuffer(buffer);
                if (wait)
                {
                    // Synchronously block waiting for data to be produced.
                    Debug.Assert(bytesRead == 0);
                    GetWaiterTask().AsTask().GetAwaiter().GetResult();
                    (wait, bytesRead) = TryReadFromBuffer(buffer);
                    Debug.Assert(!wait);
                }

                if (bytesRead != 0)
                {
                    ExtendWindow(bytesRead);
                    _connection.ExtendWindow(bytesRead);
                }

                return bytesRead;
            }

            public async ValueTask<int> ReadDataAsync(Memory<byte> buffer, CancellationToken cancellationToken)
            {
                if (buffer.Length == 0)
                {
                    return 0;
                }

                (bool wait, int bytesRead) = TryReadFromBuffer(buffer.Span);
                if (wait)
                {
                    Debug.Assert(bytesRead == 0);
                    await GetWaiterTask().ConfigureAwait(false);
                    (wait, bytesRead) = TryReadFromBuffer(buffer.Span);
                    Debug.Assert(!wait);
                }

                if (bytesRead != 0)
                {
                    ExtendWindow(bytesRead);
                    _connection.ExtendWindow(bytesRead);
                }

                return bytesRead;
            }

            private async Task SendDataAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
            {
                ReadOnlyMemory<byte> remaining = buffer;

                while (remaining.Length > 0)
                {
                    int sendSize = await _streamWindow.RequestCreditAsync(remaining.Length, cancellationToken).ConfigureAwait(false);

                    ReadOnlyMemory<byte> current;
                    (current, remaining) = SplitBuffer(remaining, sendSize);

                    await _connection.SendStreamDataAsync(_streamId, current, cancellationToken).ConfigureAwait(false);
                }
            }

            public void Dispose()
            {
                lock (SyncObject)
                {
                    if (!_disposed)
                    {
                        _disposed = true;

                        _streamWindow.Dispose();
                        _responseBuffer.Dispose();

                        // TODO: ISSUE 31310: If the stream is not complete, we should send RST_STREAM
                    }
                }
            }

            public void Cancel()
            {
                bool signalWaiter;
                lock (SyncObject)
                {
                    Task ignored = _connection.SendRstStreamAsync(_streamId, Http2ProtocolErrorCode.Cancel);
                    _abortException = new OperationCanceledException();
                    _state = StreamState.Aborted;

                    signalWaiter = _hasWaiter;
                    _hasWaiter = false;
                }
                if (signalWaiter)
                {
                    _waitSource.SetResult(true);
                }

                _connection.RemoveStream(this);
            }

            // This object is itself usable as a backing source for ValueTask.  Since there's only ever one awaiter
            // for this object's state transitions at a time, we allow the object to be awaited directly. All functionality
            // associated with the implementation is just delegated to the ManualResetValueTaskSourceCore.
            private ValueTask GetWaiterTask() => new ValueTask(this, _waitSource.Version);
            ValueTaskSourceStatus IValueTaskSource.GetStatus(short token) => _waitSource.GetStatus(token);
            void IValueTaskSource.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) => _waitSource.OnCompleted(continuation, state, token, flags);
            void IValueTaskSource.GetResult(short token) => _waitSource.GetResult(token);

            private sealed class Http2ReadStream : HttpBaseStream
            {
                private Http2Stream _http2Stream;

                public Http2ReadStream(Http2Stream http2Stream)
                {
                    Debug.Assert(http2Stream != null);
                    _http2Stream = http2Stream;
                }

                protected override void Dispose(bool disposing)
                {
                    Http2Stream http2Stream = Interlocked.Exchange(ref _http2Stream, null);
                    if (http2Stream == null)
                    {
                        return;
                    }

                    if (disposing)
                    {
                        http2Stream.Dispose();
                    }

                    base.Dispose(disposing);
                }

                public override bool CanRead => true;
                public override bool CanWrite => false;

                public override int Read(Span<byte> destination)
                {
                    Http2Stream http2Stream = _http2Stream ?? throw new ObjectDisposedException(nameof(Http2ReadStream));
                    return http2Stream.ReadData(destination);
                }

                public override ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken)
                {
                    Http2Stream http2Stream = _http2Stream;
                    if (http2Stream == null)
                    {
                        return new ValueTask<int>(Task.FromException<int>(new ObjectDisposedException(nameof(Http2ReadStream))));
                    }

                    return http2Stream.ReadDataAsync(destination, cancellationToken);
                }

                public override void Write(ReadOnlySpan<byte> buffer) => throw new NotSupportedException(SR.net_http_content_readonly_stream);

                public override ValueTask WriteAsync(ReadOnlyMemory<byte> destination, CancellationToken cancellationToken) => throw new NotSupportedException();
            }

            private sealed class Http2WriteStream : HttpBaseStream
            {
                private Http2Stream _http2Stream;

                public Http2WriteStream(Http2Stream http2Stream)
                {
                    Debug.Assert(http2Stream != null);
                    _http2Stream = http2Stream;
                }

                protected override void Dispose(bool disposing)
                {
                    Http2Stream http2Stream = Interlocked.Exchange(ref _http2Stream, null);
                    if (http2Stream == null)
                    {
                        return;
                    }

                    base.Dispose(disposing);
                }

                public override bool CanRead => false;
                public override bool CanWrite => true;

                public override int Read(Span<byte> buffer) => throw new NotSupportedException();

                public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken) => throw new NotSupportedException();

                public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
                {
                    Http2Stream http2Stream = _http2Stream;
                    Task t = http2Stream != null ?
                        http2Stream.SendDataAsync(buffer, cancellationToken) :
                        Task.FromException(new ObjectDisposedException(nameof(Http2WriteStream)));
                    return new ValueTask(t);
                }
            }
        }
    }
}
