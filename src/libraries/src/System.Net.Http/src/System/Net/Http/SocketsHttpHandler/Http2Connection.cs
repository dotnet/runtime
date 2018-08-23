// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http.HPack;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed partial class Http2Connection : HttpConnectionBase, IDisposable
    {
        private readonly HttpConnectionPool _pool;
        private readonly SslStream _stream;
        private readonly object _syncObject;

        // NOTE: These are mutable structs; do not make these readonly.
        private ArrayBuffer _incomingBuffer;
        private ArrayBuffer _outgoingBuffer;

        private readonly HPackDecoder _hpackDecoder;

        private readonly Dictionary<int, Http2Stream> _httpStreams;

        private readonly SemaphoreSlim _writerLock;

        private int _nextStream;
        private bool _expectingSettingsAck;

        private bool _disposed;

        private const int MaxStreamId = int.MaxValue;

        private static readonly byte[] s_http2ConnectionPreface = Encoding.ASCII.GetBytes("PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n");

        private const int InitialBufferSize = 4096;

        public Http2Connection(HttpConnectionPool pool, SslStream stream)
        {
            _pool = pool;
            _stream = stream;
            _syncObject = new object();
            _incomingBuffer = new ArrayBuffer(InitialBufferSize);
            _outgoingBuffer = new ArrayBuffer(InitialBufferSize);

            _hpackDecoder = new HPackDecoder();

            _httpStreams = new Dictionary<int, Http2Stream>();

            _writerLock = new SemaphoreSlim(1, 1);

            _nextStream = 1;
        }

        public async Task SetupAsync()
        {
            // Send connection preface
            _outgoingBuffer.EnsureAvailableSpace(s_http2ConnectionPreface.Length);
            s_http2ConnectionPreface.AsSpan().CopyTo(_outgoingBuffer.AvailableSpan);
            _outgoingBuffer.Commit(s_http2ConnectionPreface.Length);

            // Send empty settings frame
            WriteFrameHeader(new FrameHeader(0, FrameType.Settings, FrameFlags.None, 0));

            // TODO: ISSUE 31295: We should disable PUSH_PROMISE here.

            // TODO: ISSUE 31298: We should send a connection-level WINDOW_UPDATE to allow
            // a large amount of data to be received on the connection.  
            // We don't care that much about connection-level flow control, we'll manage it per-stream.

            await _stream.WriteAsync(_outgoingBuffer.ActiveMemory).ConfigureAwait(false);
            _outgoingBuffer.Discard(_outgoingBuffer.ActiveMemory.Length);

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

        private async Task EnsureIncomingBytesAsync(int minReadBytes)
        {
            if (_incomingBuffer.ActiveSpan.Length >= minReadBytes)
            {
                return;
            }

            int bytesNeeded = minReadBytes - _incomingBuffer.ActiveSpan.Length;
            _incomingBuffer.EnsureAvailableSpace(bytesNeeded);
            int bytesRead = await ReadAtLeastAsync(_stream, _incomingBuffer.AvailableMemory, bytesNeeded).ConfigureAwait(false);
            _incomingBuffer.Commit(bytesRead);
        }

        private async Task FlushOutgoingBytesAsync()
        {
            Debug.Assert(_outgoingBuffer.ActiveSpan.Length > 0);

            await SendFramesAsync(_outgoingBuffer.ActiveMemory).ConfigureAwait(false);
            _outgoingBuffer.Discard(_outgoingBuffer.ActiveMemory.Length);
        }

        private async Task<FrameHeader> ReadFrameAsync()
        {
            // Read frame header
            await EnsureIncomingBytesAsync(FrameHeader.Size).ConfigureAwait(false);
            FrameHeader frameHeader = FrameHeader.ReadFrom(_incomingBuffer.ActiveSpan);
            _incomingBuffer.Discard(FrameHeader.Size);

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
                    FrameHeader frameHeader = await ReadFrameAsync().ConfigureAwait(false);

                    switch (frameHeader.Type)
                    {
                        case FrameType.Headers:
                            await ProcessHeadersFrame(frameHeader).ConfigureAwait(false);
                            break;

                        case FrameType.Data:
                            await ProcessDataFrame(frameHeader).ConfigureAwait(false);
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

                        case FrameType.RstStream:
                            ProcessRstStreamFrame(frameHeader);
                            break;

                        case FrameType.GoAway:
                            ProcessGoAwayFrame(frameHeader);
                            break;

                        case FrameType.PushPromise:     // Should not happen, since we disable this in our initial SETTINGS (TODO: ISSUE 31295: We aren't currently, but we should)
                        case FrameType.Continuation:    // Should only be received while processing headers in ProcessHeadersFrame
                        default:
                            throw new Http2ProtocolException(Http2ProtocolErrorCode.ProtocolError);
                    }
                }
            }
            catch (Exception)
            {
                AbortStreams(0);
            }
        }

        // Note, this will return null for a streamId that's not in use.
        // Callers must check for this and send a RST_STREAM or ignore as appropriate.
        private Http2Stream GetStream(int streamId)
        {
            if (streamId <= 0)
            {
                throw new Http2ProtocolException(Http2ProtocolErrorCode.ProtocolError);
            }

            lock (_syncObject)
            {
                if (!_httpStreams.TryGetValue(streamId, out Http2Stream http2Stream))
                {
                    return null;
                }

                return http2Stream;
            }
        }

        private async Task SendRstStreamFrameAsync(int streamId, Http2ProtocolErrorCode errorCode)
        {
            WriteFrameHeader(new FrameHeader(FrameHeader.RstStreamLength, FrameType.RstStream, FrameFlags.None, streamId));

            _outgoingBuffer.AvailableSpan[0] = (byte)(((int)errorCode & 0xFF000000) >> 24);
            _outgoingBuffer.AvailableSpan[1] = (byte)(((int)errorCode & 0x00FF0000) >> 16);
            _outgoingBuffer.AvailableSpan[2] = (byte)(((int)errorCode & 0x0000FF00) >> 8);
            _outgoingBuffer.AvailableSpan[3] = (byte)((int)errorCode & 0x000000FF);
            _outgoingBuffer.Commit(FrameHeader.RstStreamLength);

            await FlushOutgoingBytesAsync().ConfigureAwait(false);
        }

        private async Task ProcessHeadersFrame(FrameHeader frameHeader)
        {
            Debug.Assert(frameHeader.Type == FrameType.Headers);

            bool endStream = frameHeader.EndStreamFlag;

            int streamId = frameHeader.StreamId;
            Http2Stream http2Stream = GetStream(streamId);
            if (http2Stream == null)
            {
                _incomingBuffer.Discard(frameHeader.Length);
                await SendRstStreamFrameAsync(streamId, Http2ProtocolErrorCode.StreamClosed);
                return;
            }

            // TODO: Figure out how to cache this delegate.
            // Probably want to pass a state object to Decode.
            HPackDecoder.HeaderCallback headerCallback = http2Stream.OnResponseHeader;

            _hpackDecoder.Decode(GetFrameData(_incomingBuffer.ActiveSpan.Slice(0, frameHeader.Length), frameHeader.PaddedFlag, frameHeader.PriorityFlag), headerCallback);
            _incomingBuffer.Discard(frameHeader.Length);

            while (!frameHeader.EndHeadersFlag)
            {
                frameHeader = await ReadFrameAsync().ConfigureAwait(false);
                if (frameHeader.Type != FrameType.Continuation ||
                    frameHeader.StreamId != streamId)
                {
                    throw new Http2ProtocolException(Http2ProtocolErrorCode.ProtocolError);
                }

                _hpackDecoder.Decode(_incomingBuffer.ActiveSpan.Slice(0, frameHeader.Length), headerCallback);
                _incomingBuffer.Discard(frameHeader.Length);
            }

            _hpackDecoder.CompleteDecode();

            http2Stream.OnResponseHeadersComplete(endStream);

            if (endStream)
            {
                RemoveStream(http2Stream);
            }
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

        private Task ProcessDataFrame(FrameHeader frameHeader)
        {
            Debug.Assert(frameHeader.Type == FrameType.Data);

            Http2Stream http2Stream = GetStream(frameHeader.StreamId);
            if (http2Stream == null)
            {
                _incomingBuffer.Discard(frameHeader.Length);
                return SendRstStreamFrameAsync(frameHeader.StreamId, Http2ProtocolErrorCode.StreamClosed);
            }

            ReadOnlySpan<byte> frameData = GetFrameData(_incomingBuffer.ActiveSpan.Slice(0, frameHeader.Length), hasPad: frameHeader.PaddedFlag, hasPriority: false);

            bool endStream = frameHeader.EndStreamFlag;

            http2Stream.OnResponseData(frameData, endStream);

            _incomingBuffer.Discard(frameHeader.Length);

            if (endStream)
            {
                RemoveStream(http2Stream);
            }

            return Task.CompletedTask;
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

                // TODO: ISSUE 31296: We should handle SETTINGS_MAX_CONCURRENT_STREAMS.
                // Others we don't care about, or are advisory.
                _incomingBuffer.Discard(frameHeader.Length);

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

            _incomingBuffer.Discard(frameHeader.Length);
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
            _outgoingBuffer.EnsureAvailableSpace(FrameHeader.PingLength);
            _incomingBuffer.ActiveSpan.Slice(0, FrameHeader.PingLength).CopyTo(_outgoingBuffer.AvailableSpan);
            _outgoingBuffer.Commit(FrameHeader.PingLength);
            await FlushOutgoingBytesAsync().ConfigureAwait(false);

            _incomingBuffer.Discard(frameHeader.Length);
        }

        private void ProcessWindowUpdateFrame(FrameHeader frameHeader)
        {
            Debug.Assert(frameHeader.Type == FrameType.WindowUpdate);

            if (frameHeader.Length != FrameHeader.WindowUpdateLength)
            {
                throw new Http2ProtocolException(Http2ProtocolErrorCode.FrameSizeError);
            }

            int windowUpdate = (int)((uint)((_incomingBuffer.ActiveSpan[0] << 24) | (_incomingBuffer.ActiveSpan[1] << 16) | (_incomingBuffer.ActiveSpan[2] << 8) | _incomingBuffer.ActiveSpan[3]) & 0x7FFFFFFF);

            Debug.Assert(windowUpdate >= 0);
            if (windowUpdate == 0)
            {
                throw new Http2ProtocolException(Http2ProtocolErrorCode.ProtocolError);
            }

            // TODO: ISSUE 31297: Window accounting

            _incomingBuffer.Discard(frameHeader.Length);
        }

        private void ProcessRstStreamFrame(FrameHeader frameHeader)
        {
            Debug.Assert(frameHeader.Type == FrameType.RstStream);

            if (frameHeader.Length != FrameHeader.RstStreamLength)
            {
                throw new Http2ProtocolException(Http2ProtocolErrorCode.FrameSizeError);
            }

            if (frameHeader.StreamId == 0)
            {
                throw new Http2ProtocolException(Http2ProtocolErrorCode.ProtocolError);
            }

            Http2Stream http2Stream = GetStream(frameHeader.StreamId);
            if (http2Stream == null)
            {
                // Ignore invalid stream ID, as per RFC
                _incomingBuffer.Discard(frameHeader.Length);
                return;
            }

            // CONSIDER: We ignore the error code in the RST_STREAM frame.
            // We could read this and report it to the user as part of the request exception.

            http2Stream.OnResponseAbort();

            _incomingBuffer.Discard(frameHeader.Length);
        }

        private void ProcessGoAwayFrame(FrameHeader frameHeader)
        {
            Debug.Assert(frameHeader.Type == FrameType.GoAway);

            if (frameHeader.Length < FrameHeader.GoAwayMinLength)
            {
                throw new Http2ProtocolException(Http2ProtocolErrorCode.FrameSizeError);
            }

            int lastValidStream = (int)((uint)((_incomingBuffer.ActiveSpan[0] << 24) | (_incomingBuffer.ActiveSpan[1] << 16) | (_incomingBuffer.ActiveSpan[2] << 8) | _incomingBuffer.ActiveSpan[3]) & 0x7FFFFFFF);

            AbortStreams(lastValidStream);

            _incomingBuffer.Discard(frameHeader.Length);
        }

        private void WriteFrameHeader(FrameHeader frameHeader)
        {
            _outgoingBuffer.EnsureAvailableSpace(FrameHeader.Size);
            frameHeader.WriteTo(_outgoingBuffer.AvailableSpan);
            _outgoingBuffer.Commit(FrameHeader.Size);
        }

        private void AbortStreams(int lastValidStream)
        {
            lock (_syncObject)
            {
                if (!_disposed)
                {
                    _pool.InvalidateHttp2Connection(this);

                    _disposed = true;
                }

                foreach (KeyValuePair<int, Http2Stream> kvp in _httpStreams)
                {
                    int streamId = kvp.Key;
                    Debug.Assert(streamId == kvp.Value.StreamId);

                    if (streamId > lastValidStream)
                    {
                        kvp.Value.OnResponseAbort();

                        _httpStreams.Remove(kvp.Value.StreamId);
                    }
                }

                CheckForShutdown();
            }
        }

        private void CheckForShutdown()
        {
            Debug.Assert(_disposed);
            Debug.Assert(Monitor.IsEntered(_syncObject));

            // Check if dictionary has become empty
            if (_httpStreams.Count != 0)
            {
                return;
            }

            // Do shutdown.
            _stream.Close();
        }

        public void Dispose()
        {
            lock (_syncObject)
            {
                if (!_disposed)
                {
                    _disposed = true;

                    CheckForShutdown();
                }
            }
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
            Continuation = 9,

            Last = 9
        }

        private struct FrameHeader
        {
            public int Length;
            public FrameType Type;
            public FrameFlags Flags;
            public int StreamId;

            public const int Size = 9;
            public const int MaxLength = 16384;

            public const int PriorityInfoLength = 5;       // for both PRIORITY frame and priority info within HEADERS
            public const int PingLength = 8;
            public const int WindowUpdateLength = 4;
            public const int RstStreamLength = 4;
            public const int GoAwayMinLength = 8;

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

            public static FrameHeader ReadFrom(ReadOnlySpan<byte> buffer)
            {
                Debug.Assert(buffer.Length >= Size);

                return new FrameHeader(
                    (buffer[0] << 16) | (buffer[1] << 8) | buffer[2],
                    (FrameType)buffer[3],
                    (FrameFlags)buffer[4],
                    (int)((uint)((buffer[5] << 24) | (buffer[6] << 16) | (buffer[7] << 8) | buffer[8]) & 0x7FFFFFFF));
            }

            public void WriteTo(Span<byte> buffer)
            {
                Debug.Assert(buffer.Length >= Size);
                Debug.Assert(Type <= FrameType.Last);
                Debug.Assert((Flags & FrameFlags.ValidBits) == Flags);

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

            ValidBits =     0b00101101
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

        public sealed override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Http2Stream http2Stream = new Http2Stream(this);
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

        private int AddStream(Http2Stream http2Stream)
        {
            int streamId;

            lock (_syncObject)
            {
                if (_disposed || _nextStream == MaxStreamId)
                {
                    // Throw a retryable request exception. This will cause retry logic to kick in
                    // and perform another connection attempt. The user should never see this exception.
                    throw new HttpRequestException(null, null, true);
                }

                streamId = _nextStream;

                // Client-initiated streams are always odd-numbered, so increase by 2.
                _nextStream += 2;

                _httpStreams.Add(streamId, http2Stream);
            }

            return streamId;
        }

        private void RemoveStream(Http2Stream http2Stream)
        {
            lock (_syncObject)
            {
                if (!_httpStreams.Remove(http2Stream.StreamId, out Http2Stream removed))
                {
                    Debug.Fail("_httpStreams.Remove failed");
                }

                Debug.Assert(removed == http2Stream, "_httpStreams.TryRemove returned unexpected stream");

                if (_disposed)
                {
                    CheckForShutdown();
                }
            }
        }

        // TODO: ISSUE 31315: Should this be public?
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

        // TODO: ISSUE 31315: Should this be public?
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

        internal static async ValueTask<int> ReadAtLeastAsync(Stream stream, Memory<byte> buffer, int minReadBytes)
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
