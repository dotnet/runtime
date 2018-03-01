// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http.HPack;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// Some stuff to do:

// * Handle errors/shutdown, both graceful and abortive, at connection level and per-stream
// * Handle request cancellation
// * Handle request body
// * Handle frame limit and sending CONTINUATION on request headers
// * Flow control (both inbound and outbound)
// * Various TODOs below

namespace System.Net.Http
{
    // TODO: This should probably implement IDisposable.
    internal sealed class Http2Connection : HttpConnectionBase
    {
        private readonly SslStream _stream;

        private ArrayBuffer _incomingBuffer;
        private ArrayBuffer _outgoingBuffer;

        private readonly HPackDecoder _hpackDecoder;

        private readonly ConcurrentDictionary<int, Http2Stream> _httpStreams;

        private readonly SemaphoreSlim _writerLock;

        private int _nextStream;
        private bool _expectingSettingsAck;

        private const int MaxStreamId = int.MaxValue;

        private static readonly byte[] s_http2ConnectionPreface = Encoding.ASCII.GetBytes("PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n");

        private const int InitialBufferSize = 4096;

        public Http2Connection(SslStream stream)
        {
            _stream = stream;
            _incomingBuffer = new ArrayBuffer(InitialBufferSize);
            _outgoingBuffer = new ArrayBuffer(InitialBufferSize);

            _hpackDecoder = new HPackDecoder();

            _httpStreams = new ConcurrentDictionary<int, Http2Stream>();

            _writerLock = new SemaphoreSlim(1);

            _nextStream = 1;
        }

        public async Task SetupAsync()
        {
            // Send connection preface
            _outgoingBuffer.EnsureWriteSpace(s_http2ConnectionPreface.Length);
            s_http2ConnectionPreface.AsSpan().CopyTo(_outgoingBuffer.WriteBytes);
            _outgoingBuffer.Commit(s_http2ConnectionPreface.Length);

            // Send empty settings frame
            WriteFrameHeader(new FrameHeader(0, FrameType.Settings, FrameFlags.None, 0));

            // TODO: We should disable PUSH_PROMISE here.
            // TODO: We should send a connection-level WINDOW_UPDATE to allow
            // a large amount of data to be received on the connection.  
            // We don't care that much about connection-level flow control, we'll manage it per-stream.

            await _stream.WriteAsync(_outgoingBuffer.ReadMemory).ConfigureAwait(false);
            _outgoingBuffer.Consume(_outgoingBuffer.ReadMemory.Length);

            _expectingSettingsAck = true;

            // Receive the initial SETTINGS frame from the peer.
            FrameHeader frameHeader = await ReadFrameAsync().ConfigureAwait(false);
            if (frameHeader.Type != FrameType.Settings || frameHeader.AckFlag)
            {
                throw new Http2ProtocolException(Http2ProtocolErrorCode.ProtocolError);
            }

            // Process the SETTINGS frame.  This will send an ACK.
            await ProcessSettingsFrame(frameHeader).ConfigureAwait(false);

            ProcessIncomingFrames();
        }

        private int GetNewStreamId()
        {
            int newStream = _nextStream;
            while (true)
            {
                if (newStream == MaxStreamId)
                {
                    // No more stream Ids available
                    return -1;
                }

                int original = Interlocked.CompareExchange(ref _nextStream, newStream + 2, newStream);
                if (original == newStream)
                {
                    return newStream;
                }

                newStream = original;
            }
        }

        private async Task EnsureIncomingBytesAsync(int minReadBytes)
        {
            if (_incomingBuffer.ReadBytes.Length >= minReadBytes)
            {
                return;
            }

            int bytesNeeded = minReadBytes - _incomingBuffer.ReadBytes.Length;
            _incomingBuffer.EnsureWriteSpace(bytesNeeded);
            int bytesRead = await ReadAtLeastAsync(_stream, _incomingBuffer.WriteMemory, bytesNeeded).ConfigureAwait(false);
            _incomingBuffer.Commit(bytesRead);
        }

        private async Task FlushOutgoingBytesAsync()
        {
            Debug.Assert(_outgoingBuffer.ReadBytes.Length > 0);

            await SendFramesAsync(_outgoingBuffer.ReadMemory).ConfigureAwait(false);
            _outgoingBuffer.Consume(_outgoingBuffer.ReadMemory.Length);
        }

        private async Task<FrameHeader> ReadFrameAsync()
        {
            // Read frame header
            await EnsureIncomingBytesAsync(FrameHeader.Size).ConfigureAwait(false);
            FrameHeader frameHeader = FrameHeader.Read(_incomingBuffer.ReadBytes);
            _incomingBuffer.Consume(FrameHeader.Size);

            if (frameHeader.Length > FrameHeader.MaxLength)
            {
                throw new Http2ProtocolException(Http2ProtocolErrorCode.FrameSizeError);
            }

            // Read frame contents
            await EnsureIncomingBytesAsync(frameHeader.Length).ConfigureAwait(false);

            return frameHeader;
        }

        private async void ProcessIncomingFrames()
        {
            try
            {
                while (true)
                {
                    FrameHeader frameHeader = await ReadFrameAsync();

                    switch (frameHeader.Type)
                    {
                        case FrameType.Headers:
                            await ProcessHeadersFrame(frameHeader).ConfigureAwait(false);
                            break;

                        case FrameType.Data:
                            ProcessDataFrame(frameHeader);
                            break;

                        case FrameType.Settings:
                            await ProcessSettingsFrame(frameHeader).ConfigureAwait(false);
                            break;

                        case FrameType.Priority:
                            ProcessPriorityFrame(frameHeader);
                            break;

                        case FrameType.Ping:
                            await ProcessPingFrame(frameHeader).ConfigureAwait(false);
                            break;

                        case FrameType.WindowUpdate:
                            ProcessWindowUpdateFrame(frameHeader);
                            break;

                        case FrameType.GoAway:          // TODO
                        case FrameType.RstStream:       // TODO
                            throw new NotImplementedException();

                        case FrameType.PushPromise:     // Should not happen, since we turn this off in our settings
                        case FrameType.Continuation:    // Should only be received while processing headers in ProcessHeadersFrame
                        default:
                            throw new Http2ProtocolException(Http2ProtocolErrorCode.ProtocolError);
                    }
                }
            }
            catch (Exception)
            {
                // TODO: Shutdown handling 
                // Send GOAWAY to the peer, unless they sent us a GOAWAY first
                // Fail any pending requests
                // Clear out reference to connection so that the next request will create a new connection
            }
        }

        private Http2Stream GetStream(int streamId)
        {
            if (streamId <= 0 ||
                !_httpStreams.TryGetValue(streamId, out Http2Stream http2Stream))
            {
                throw new Http2ProtocolException(Http2ProtocolErrorCode.ProtocolError);
            }

            return http2Stream;
        }

        private async Task ProcessHeadersFrame(FrameHeader frameHeader)
        {
            Debug.Assert(frameHeader.Type == FrameType.Headers);

            bool endStream = frameHeader.EndStreamFlag;

            int streamId = frameHeader.StreamId;
            Http2Stream http2Stream = GetStream(streamId);

            // TODO: Figure out how to cache this delegate.
            // Probably want to pass a state object to Decode.
            HPackDecoder.HeaderCallback headerCallback = http2Stream.OnResponseHeader;

            _hpackDecoder.Decode(GetFrameData(_incomingBuffer.ReadBytes.Slice(0, frameHeader.Length), frameHeader.PaddedFlag, frameHeader.PriorityFlag), headerCallback);
            _incomingBuffer.Consume(frameHeader.Length);

            while (!frameHeader.EndHeadersFlag)
            {
                frameHeader = await ReadFrameAsync().ConfigureAwait(false);
                if (frameHeader.Type != FrameType.Continuation ||
                    frameHeader.StreamId != streamId)
                {
                    throw new Http2ProtocolException(Http2ProtocolErrorCode.ProtocolError);
                }

                _hpackDecoder.Decode(_incomingBuffer.ReadBytes.Slice(0, frameHeader.Length), headerCallback);
                _incomingBuffer.Consume(frameHeader.Length);
            }

            _hpackDecoder.CompleteDecode();

            http2Stream.OnResponseHeadersComplete(endStream);
        }

        private ReadOnlySpan<byte> GetFrameData(ReadOnlySpan<byte> frameData, bool hasPad, bool hasPriority)
        {
            if (hasPad)
            {
                if (frameData.Length == 0)
                {
                    throw new Http2ProtocolException(Http2ProtocolErrorCode.ProtocolError);
                }

                int padLength = frameData[0];
                frameData = frameData.Slice(1);

                if (frameData.Length < padLength)
                {
                    throw new Http2ProtocolException(Http2ProtocolErrorCode.ProtocolError);
                }

                frameData = frameData.Slice(0, frameData.Length - padLength);
            }

            if (hasPriority)
            {
                if (frameData.Length < FrameHeader.PriorityInfoLength)
                {
                    throw new Http2ProtocolException(Http2ProtocolErrorCode.ProtocolError);
                }

                // We ignore priority info.
                frameData = frameData.Slice(FrameHeader.PriorityInfoLength);
            }

            return frameData;
        }

        private void ProcessDataFrame(FrameHeader frameHeader)
        {
            Debug.Assert(frameHeader.Type == FrameType.Data);

            Http2Stream http2Stream = GetStream(frameHeader.StreamId);

            ReadOnlySpan<byte> frameData = GetFrameData(_incomingBuffer.ReadBytes.Slice(0, frameHeader.Length), hasPad: frameHeader.PaddedFlag, hasPriority: false);

            http2Stream.OnResponseData(frameData, frameHeader.EndStreamFlag);

            _incomingBuffer.Consume(frameHeader.Length);
        }

        private async Task ProcessSettingsFrame(FrameHeader frameHeader)
        {
            Debug.Assert(frameHeader.Type == FrameType.Settings);

            if (frameHeader.StreamId != 0)
            {
                throw new Http2ProtocolException(Http2ProtocolErrorCode.ProtocolError);
            }

            if (frameHeader.AckFlag)
            {
                if (frameHeader.Length != 0)
                {
                    throw new Http2ProtocolException(Http2ProtocolErrorCode.FrameSizeError);
                }

                if (!_expectingSettingsAck)
                {
                    throw new Http2ProtocolException(Http2ProtocolErrorCode.ProtocolError);
                }

                // We only send SETTINGS once initially, so we don't need to do anything in response to the ACK.
                // Just remember that we received one and we won't be expecting any more.
                _expectingSettingsAck = false;
            }
            else
            {
                if ((frameHeader.Length % 6) != 0)
                {
                    throw new Http2ProtocolException(Http2ProtocolErrorCode.FrameSizeError);
                }

                // Just eat settings for now
                // TODO: We should at least validate the settings and fail on invalid settings.
                // TODO: We should handle SETTINGS_MAX_CONCURRENT_STREAMS.
                // Others we don't care about, or are advisory.
                _incomingBuffer.Consume(frameHeader.Length);

                // Send acknowledgement
                WriteFrameHeader(new FrameHeader(0, FrameType.Settings, FrameFlags.Ack, 0));
                await FlushOutgoingBytesAsync().ConfigureAwait(false);
            }
        }

        private void ProcessPriorityFrame(FrameHeader frameHeader)
        {
            Debug.Assert(frameHeader.Type == FrameType.Priority);

            if (frameHeader.StreamId == 0 || frameHeader.Length != FrameHeader.PriorityInfoLength)
            {
                throw new Http2ProtocolException(Http2ProtocolErrorCode.ProtocolError);
            }

            // Ignore priority info.

            _incomingBuffer.Consume(frameHeader.Length);
        }

        private async Task ProcessPingFrame(FrameHeader frameHeader)
        {
            Debug.Assert(frameHeader.Type == FrameType.Ping);

            if (frameHeader.StreamId != 0)
            {
                throw new Http2ProtocolException(Http2ProtocolErrorCode.ProtocolError);
            }

            if (frameHeader.AckFlag)
            {
                // We never send PING, so an ACK indicates a protocol error
                throw new Http2ProtocolException(Http2ProtocolErrorCode.ProtocolError);
            }

            if (frameHeader.Length != FrameHeader.PingLength)
            {
                throw new Http2ProtocolException(Http2ProtocolErrorCode.FrameSizeError);
            }

            // Send PING ACK
            WriteFrameHeader(new FrameHeader(FrameHeader.PingLength, FrameType.Ping, FrameFlags.Ack, 0));
            _outgoingBuffer.EnsureWriteSpace(FrameHeader.PingLength);
            _incomingBuffer.ReadBytes.Slice(0, FrameHeader.PingLength).CopyTo(_outgoingBuffer.WriteBytes);
            _outgoingBuffer.Commit(FrameHeader.PingLength);
            await FlushOutgoingBytesAsync().ConfigureAwait(false);

            _incomingBuffer.Consume(frameHeader.Length);
        }

        private void ProcessWindowUpdateFrame(FrameHeader frameHeader)
        {
            Debug.Assert(frameHeader.Type == FrameType.WindowUpdate);

            if (frameHeader.Length != FrameHeader.WindowUpdateLength)
            {
                throw new Http2ProtocolException(Http2ProtocolErrorCode.FrameSizeError);
            }

            int windowUpdate = (int)((uint)((_incomingBuffer.ReadBytes[0] << 24) | (_incomingBuffer.ReadBytes[1] << 16) | (_incomingBuffer.ReadBytes[2] << 8) | _incomingBuffer.ReadBytes[3]) & 0x7FFFFFFF);

            Debug.Assert(windowUpdate >= 0);
            if (windowUpdate == 0)
            {
                throw new Http2ProtocolException(Http2ProtocolErrorCode.ProtocolError);
            }

            // TODO: Window accounting

            _incomingBuffer.Consume(frameHeader.Length);
        }

        private void WriteFrameHeader(FrameHeader frameHeader)
        {
            _outgoingBuffer.EnsureWriteSpace(FrameHeader.Size);
            frameHeader.Write(_outgoingBuffer.WriteBytes);
            _outgoingBuffer.Commit(FrameHeader.Size);
        }

        private enum FrameType : byte
        {
            Data = 0,
            Headers = 1,
            Priority = 2,
            RstStream = 3,
            Settings = 4,
            PushPromise = 5,
            Ping = 6,
            GoAway = 7,
            WindowUpdate = 8,
            Continuation = 9
        }

        private struct FrameHeader
        {
            public readonly int Length;
            public readonly FrameType Type;
            public readonly FrameFlags Flags;
            public readonly int StreamId;

            public const int Size = 9;
            public const int MaxLength = 16384;

            public const int PriorityInfoLength = 5;       // for both PRIORITY frame and priority info within HEADERS
            public const int PingLength = 8;
            public const int WindowUpdateLength = 4;

            public FrameHeader(int length, FrameType type, FrameFlags flags, int streamId)
            {
                Debug.Assert(length <= MaxLength);
                Debug.Assert(streamId >= 0);

                Length = length;
                Type = type;
                Flags = flags;
                StreamId = streamId;
            }

            public bool PaddedFlag => (Flags & FrameFlags.Padded) != 0;
            public bool AckFlag => (Flags & FrameFlags.Ack) != 0;
            public bool EndHeadersFlag => (Flags & FrameFlags.EndHeaders) != 0;
            public bool EndStreamFlag => (Flags & FrameFlags.EndStream) != 0;
            public bool PriorityFlag => (Flags & FrameFlags.Priority) != 0;

            public static FrameHeader Read(ReadOnlySpan<byte> buffer)
            {
                Debug.Assert(buffer.Length >= Size);

                return new FrameHeader(
                    (buffer[0] << 16) | (buffer[1] << 8) | buffer[2],
                    (FrameType)buffer[3],
                    (FrameFlags)buffer[4],
                    (int)((uint)((buffer[5] << 24) | (buffer[6] << 16) | (buffer[7] << 8) | buffer[8]) & 0x7FFFFFFF));
            }

            public void Write(Span<byte> buffer)
            {
                Debug.Assert(buffer.Length >= Size);

                buffer[0] = (byte)((Length & 0x00FF0000) >> 16);
                buffer[1] = (byte)((Length & 0x0000FF00) >> 8);
                buffer[2] = (byte)(Length & 0x000000FF);

                buffer[3] = (byte)Type;
                buffer[4] = (byte)Flags;

                buffer[5] = (byte)((StreamId & 0xFF000000) >> 24);
                buffer[6] = (byte)((StreamId & 0x00FF0000) >> 16);
                buffer[7] = (byte)((StreamId & 0x0000FF00) >> 8);
                buffer[8] = (byte)(StreamId & 0x000000FF);
            }
        }

        [Flags]
        private enum FrameFlags : byte
        {
            None = 0,
            
            // Some frame types define bits differently.  Define them all here for simplicity.

            EndStream =     0b00000001,
            Ack =           0b00000001,
            EndHeaders =    0b00000100,
            Padded =        0b00001000,
            Priority =      0b00100000,
        }

        private async Task SendFramesAsync(Memory<byte> frame)
        {
            // This can be called simultaneously by multiple Http2Streams sending their headers/request body,
            // or by the connection itself to send connection level frames like SETTINGS.
            // Serialize the writes.
            await _writerLock.WaitAsync().ConfigureAwait(false);

            try
            {
                await _stream.WriteAsync(frame).ConfigureAwait(false);
            }
            finally
            {
                _writerLock.Release();
            }
        }

        // Note that this is safe to be called concurrently by multiple threads.

        public sealed override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool doRequestAuth, CancellationToken cancellationToken)
        {
            int streamId = GetNewStreamId();
            if (streamId == -1)
            {
                // TODO: Gracefully shutdown connection and mark as not usable.
                // TODO: In debug builds, we should set the stream limit reasonably low (4K maybe?) so that we can hit it during stress testing.
                throw new Exception("out of stream IDs");
            }

            Http2Stream http2Stream = new Http2Stream(this, streamId);
            try
            {
                return await http2Stream.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                http2Stream.Dispose();
                throw;
            }
        }

        class Http2Stream : IDisposable
        {
            private readonly Http2Connection _connection;
            private readonly int _streamId;

            // TODO: In debug build, make initial size small (10)
            private ArrayBuffer _requestBuffer;

            private ArrayBuffer _responseBuffer;
            private object _responseBufferLock;
            private TaskCompletionSource<bool> _responseDataAvailable;
            private bool _responseComplete;

            private HttpResponseMessage _response;
            private bool _disposed = false;


            public Http2Stream(Http2Connection connection, int streamId)
            {
                _connection = connection;
                _streamId = streamId;

                if (!_connection._httpStreams.TryAdd(streamId, this))
                {
                    Debug.Fail("_httpStreams.TryAdd failed");
                    throw new InternalException();
                }

                _requestBuffer = new ArrayBuffer(InitialBufferSize);

                _responseBuffer = new ArrayBuffer(InitialBufferSize);
                _responseBufferLock = new object();
            }

            public int StreamId => _streamId;

            private void GrowWriteBuffer()
            {
                _requestBuffer.EnsureWriteSpace(_requestBuffer.WriteBytes.Length + 1);
            }

            private void WriteHeader(ref int bufferOffset, string name, string value)
            {
                Debug.Assert(bufferOffset <= _requestBuffer.WriteBytes.Length);

                // TODO: Enforce frame size limit

                int bytesWritten;
                while (!HPackEncoder.EncodeHeader(name, value, _requestBuffer.WriteBytes.Slice(bufferOffset), out bytesWritten))
                {
                    GrowWriteBuffer();
                }

                bufferOffset += bytesWritten;
            }

            private void WriteHeaders(ref int bufferOffset, HttpHeaders headers)
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
                        WriteHeader(ref bufferOffset, header.Key.Name, header.Value[i]);
                    }
                }
            }

            // TODO: Enforce frame size limit.
            // We need to detect when we have exceeded the frame size limit, then:
            // (1) Send as much as we can in the current frame, without FrameFlags.EndHeaders
            // (2) Start constructing a new CONTINUATION frame
            // Also note that we can't send any other frames in between these, so we must 
            // hold the connection's frame writer lock for the entire duration.
            private void WriteHeaders(HttpRequestMessage request)
            {
                // TODO: Special header handling
                // Connection and Transfer-Encoding should be disallowed and throw
                // Cookie -- handle container cookies (actually much easier for HTTP2)

                // TODO: Smart encoding for pseudo-headers

                // Reserve space in buffer for frame header.  
                // We will fill it in when we're done constructing the frame.
                _requestBuffer.EnsureWriteSpace(FrameHeader.Size);
                int bufferOffset = FrameHeader.Size;

                HttpMethod normalizedMethod = HttpMethod.Normalize(request.Method);

                WriteHeader(ref bufferOffset, ":method", normalizedMethod.Method);
                WriteHeader(ref bufferOffset, ":scheme", "https");

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

                WriteHeader(ref bufferOffset, ":authority", authority);
                WriteHeader(ref bufferOffset, ":path", request.RequestUri.GetComponents(UriComponents.PathAndQuery | UriComponents.Fragment, UriFormat.UriEscaped));

                if (request.HasHeaders)
                {
                    WriteHeaders(ref bufferOffset, request.Headers);
                }

                if (request.Content == null)
                {
                    // Write out Content-Length: 0 header to indicate no body,
                    // unless this is a method that never has a body.
                    // TODO: Optimize using static table
                    if (normalizedMethod.HasBody)
                    {
                        WriteHeader(ref bufferOffset, "Content-Length", "0");
                    }
                }
                else
                {
                    WriteHeaders(ref bufferOffset, request.Content.Headers);
                }

                FrameFlags flags = FrameFlags.EndHeaders;
                if (request.Content == null)
                {
                    flags |= FrameFlags.EndStream;
                }

                FrameHeader frameHeader = new FrameHeader(bufferOffset - FrameHeader.Size, FrameType.Headers, flags, _streamId);
                frameHeader.Write(_requestBuffer.WriteBytes);
                _requestBuffer.Commit(bufferOffset);
            }

            public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                // TODO: Handle window manangement

                // TODO: Cancellation support?
                // TODO: CONNECT handling?  I think we need to force a CONNECT to 1.1 before we get here.

                WriteHeaders(request);

                // Send request body, if any
                if (request.Content != null)
                {
                    throw new Exception("Request body not supported yet");
                }

                // Construct response

                HttpConnectionResponseContent responseContent = new HttpConnectionResponseContent();
                _response = new HttpResponseMessage() { Version = HttpVersion.Version20, RequestMessage = request, Content = responseContent };

                // TODO: Expect: 100-continue and early response handling
                // Note that in an "early response" scenario, where we get a response before we've finished sending the request body
                // (either with a 100-continue that timed out, or without 100-continue),
                // we can stop sending the request without tearing down the entire connection.

                Debug.Assert(_responseDataAvailable == null);
                _responseDataAvailable = new TaskCompletionSource<bool>();
                Task readDataAvailableTask = _responseDataAvailable.Task;

                await _connection.SendFramesAsync(_requestBuffer.ReadMemory).ConfigureAwait(false);
                _requestBuffer.Consume(_requestBuffer.ReadBytes.Length);

                // Wait for response headers to be read.
                await readDataAvailableTask.ConfigureAwait(false);

                // TODO: Handle empty response using EmptyReadStream

                responseContent.SetStream(new Http2ResponseStream(this));

                return _response;
            }

            private static readonly byte[] StatusHeaderName = Encoding.ASCII.GetBytes(":status");

            // Copied from HttpConnection
            // TODO: Consolidate this logic?
            private static bool IsDigit(byte c) => (uint)(c - '0') <= '9' - '0';

            public void OnResponseHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
            {
                if (name.SequenceEqual(StatusHeaderName))
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

                lock (_responseBufferLock)
                {
                    Debug.Assert(!_responseComplete);

                    _responseBuffer.EnsureWriteSpace(buffer.Length);
                    buffer.CopyTo(_responseBuffer.WriteBytes);
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

            private int ReadFromBuffer(Span<byte> buffer)
            {
                Debug.Assert(_responseBuffer.ReadBytes.Length > 0);
                Debug.Assert(buffer.Length > 0);

                int bytesToCopy = Math.Min(buffer.Length, _responseBuffer.ReadBytes.Length);
                _responseBuffer.ReadBytes.CopyTo(buffer);
                _responseBuffer.Consume(bytesToCopy);
                return bytesToCopy;
            }

            public async ValueTask<int> ReadResponseBodyAsyncCore(Task onDataAvailable, Memory<byte> buffer)
            {
                await onDataAvailable.ConfigureAwait(false);

                lock (_responseBufferLock)
                {
                    if (_responseBuffer.ReadBytes.Length > 0)
                    {
                        return ReadFromBuffer(buffer.Span);
                    }

                    // If no data was made available, we must be at the end of the stream
                    Debug.Assert(_responseComplete);
                    return 0;
                }
            }

            public ValueTask<int> ReadResponseBodyAsync(Memory<byte> buffer)
            {
                Debug.Assert(buffer.Length > 0);

                Task onDataAvailable;
                lock (_responseBufferLock)
                {
                    if (_responseBuffer.ReadBytes.Length > 0)
                    {
                        return new ValueTask<int>(ReadFromBuffer(buffer.Span));
                    }

                    if (_responseComplete)
                    {
                        return new ValueTask<int>(0);
                    }

                    Debug.Assert(_responseDataAvailable == null);
                    _responseDataAvailable = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    onDataAvailable = _responseDataAvailable.Task;
                }

                return ReadResponseBodyAsyncCore(onDataAvailable, buffer);
            }

            protected virtual void Dispose(bool disposing)
            {
                // TODO: Make this interlocked
                if (!_disposed)
                {
                    if (disposing)
                    {
                        if (!_connection._httpStreams.TryRemove(_streamId, out Http2Stream removed))
                        {
                            Debug.Fail("_httpStreams.TryRemove failed");
                        }

                        Debug.Assert(removed == this, "_httpStreams.TryRemove returned unexpected stream");
                    }

                    _disposed = true;
                }
            }

            public void Dispose()
            {
                Dispose(true);
            }
        }

        // TODO: Refactor this so that we can share a common base class with HTTP/1.1 content streams.
        class Http2ResponseStream : Stream
        {
            private readonly Http2Stream _http2Stream;

            public Http2ResponseStream(Http2Stream http2Stream)
            {
                _http2Stream = http2Stream;
            }

            public override bool CanRead => true;

            public override bool CanSeek => false;

            public override bool CanWrite => false;

            public override long Length => throw new NotImplementedException();

            public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public override void Flush()
            {
                throw new NotImplementedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
            {
                return _http2Stream.ReadResponseBodyAsync(destination);
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
            }
        }

        // TODO: Should this be public?
        internal enum Http2ProtocolErrorCode
        {
            NoError = 0x0,
            ProtocolError = 0x1,
            InternalError = 0x2,
            FlowControlError = 0x3,
            SettingsTimeout = 0x4,
            StreamClosed = 0x5,
            FrameSizeError = 0x6,
            RefusedStream = 0x7,
            Cancel = 0x8,
            CompressionError = 0x9,
            ConnectError = 0xa,
            EnhanceYourCalm = 0xb,
            InadequateSecurity = 0xc,
            Http11Required = 0xd
        }

        // TODO: Should this be public?
        internal class Http2ProtocolException : Exception
        {
            private readonly Http2ProtocolErrorCode _errorCode;

            public Http2ProtocolException(Http2ProtocolErrorCode errorCode)
                : base($"Http2 Protocol Error, errorCode = {errorCode}")
            {
                _errorCode = errorCode;
            }

            public Http2ProtocolErrorCode ErrorCode => _errorCode;
        }

        internal static async Task<int> ReadAtLeastAsync(Stream stream, Memory<byte> buffer, int minReadBytes)
        {
            Debug.Assert(buffer.Length >= minReadBytes);

            int totalBytesRead = 0;
            while (totalBytesRead < minReadBytes)
            {
                int bytesRead = await stream.ReadAsync(buffer).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    throw new IOException(SR.net_http_invalid_response);
                }

                totalBytesRead += bytesRead;
            }

            return totalBytesRead;
        }
    }
}
