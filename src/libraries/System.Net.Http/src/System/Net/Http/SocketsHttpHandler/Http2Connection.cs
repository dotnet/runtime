// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Connections;
using System.Net.Http.Headers;
using System.Net.Http.HPack;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed partial class Http2Connection : HttpConnectionBase, IDisposable
    {
        private readonly HttpConnectionPool _pool;
        private readonly Stream _stream;
        private readonly Connection _connection;

        // NOTE: These are mutable structs; do not make these readonly.
        private ArrayBuffer _incomingBuffer;
        private ArrayBuffer _outgoingBuffer;

        /// <summary>Reusable array used to get the values for each header being written to the wire.</summary>
        [ThreadStatic]
        private static string[]? t_headerValues;

        private readonly HPackDecoder _hpackDecoder;

        private readonly Dictionary<int, Http2Stream> _httpStreams;

        private readonly CreditManager _connectionWindow;
        private readonly CreditManager _concurrentStreams;

        private int _nextStream;
        private bool _expectingSettingsAck;
        private int _initialWindowSize;
        private int _maxConcurrentStreams;
        private int _pendingWindowUpdate;
        private long _idleSinceTickCount;

        private readonly Channel<WriteQueueEntry> _writeChannel;
        private bool _lastPendingWriterShouldFlush;

        // This means that the pool has disposed us, but there may still be
        // requests in flight that will continue to be processed.
        private bool _disposed;

        // This will be set when:
        // (1) We receive GOAWAY -- will be set to the value sent in the GOAWAY frame
        // (2) A connection IO error occurs -- will be set to int.MaxValue
        //     (meaning we must assume all streams have been processed by the server)
        private int _lastStreamId = -1;

        // This will be set when a connection IO error occurs
        private Exception? _abortException;

        private const int MaxStreamId = int.MaxValue;

        private static readonly byte[] s_http2ConnectionPreface = Encoding.ASCII.GetBytes("PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n");

#if DEBUG
        // In debug builds, start with a very small buffer to induce buffer growing logic.
        private const int InitialConnectionBufferSize = 4;
#else
        private const int InitialConnectionBufferSize = 4096;
#endif

        private const int DefaultInitialWindowSize = 65535;

        // We don't really care about limiting control flow at the connection level.
        // We limit it per stream, and the user controls how many streams are created.
        // So set the connection window size to a large value.
        private const int ConnectionWindowSize = 64 * 1024 * 1024;

        // We hold off on sending WINDOW_UPDATE until we hit thi minimum threshold.
        // This value is somewhat arbitrary; the intent is to ensure it is much smaller than
        // the window size itself, or we risk stalling the server because it runs out of window space.
        // If we want to further reduce the frequency of WINDOW_UPDATEs, it's probably better to
        // increase the window size (and thus increase the threshold proportionally)
        // rather than just increase the threshold.
        private const int ConnectionWindowThreshold = ConnectionWindowSize / 8;

        // When buffering outgoing writes, we will automatically buffer up to this number of bytes.
        // Single writes that are larger than the buffer can cause the buffer to expand beyond
        // this value, so this is not a hard maximum size.
        private const int UnflushedOutgoingBufferSize = 32 * 1024;

        // Channel options for creating _writeChannel
        private static readonly UnboundedChannelOptions s_channelOptions = new UnboundedChannelOptions() { SingleReader = true };

        public Http2Connection(HttpConnectionPool pool, Connection connection)
        {
            _pool = pool;
            _stream = connection.Stream;
            _connection = connection;
            _incomingBuffer = new ArrayBuffer(InitialConnectionBufferSize);
            _outgoingBuffer = new ArrayBuffer(InitialConnectionBufferSize);

            _hpackDecoder = new HPackDecoder(maxHeadersLength: pool.Settings._maxResponseHeadersLength * 1024);

            _httpStreams = new Dictionary<int, Http2Stream>();

            _connectionWindow = new CreditManager(this, nameof(_connectionWindow), DefaultInitialWindowSize);
            _concurrentStreams = new CreditManager(this, nameof(_concurrentStreams), int.MaxValue);

            _writeChannel = Channel.CreateUnbounded<WriteQueueEntry>(s_channelOptions);

            _nextStream = 1;
            _initialWindowSize = DefaultInitialWindowSize;
            _maxConcurrentStreams = int.MaxValue;
            _pendingWindowUpdate = 0;
            _idleSinceTickCount = Environment.TickCount64;

            if (NetEventSource.Log.IsEnabled()) TraceConnection(_stream);
        }

        private object SyncObject => _httpStreams;

        public bool CanAddNewStream => _concurrentStreams.IsCreditAvailable;

        public async ValueTask SetupAsync()
        {
            _outgoingBuffer.EnsureAvailableSpace(s_http2ConnectionPreface.Length +
                FrameHeader.Size + FrameHeader.SettingLength +
                FrameHeader.Size + FrameHeader.WindowUpdateLength);

            // Send connection preface
            s_http2ConnectionPreface.AsSpan().CopyTo(_outgoingBuffer.AvailableSpan);
            _outgoingBuffer.Commit(s_http2ConnectionPreface.Length);

            // Send SETTINGS frame.  Disable push promise.
            FrameHeader.WriteTo(_outgoingBuffer.AvailableSpan, FrameHeader.SettingLength, FrameType.Settings, FrameFlags.None, streamId: 0);
            _outgoingBuffer.Commit(FrameHeader.Size);
            BinaryPrimitives.WriteUInt16BigEndian(_outgoingBuffer.AvailableSpan, (ushort)SettingId.EnablePush);
            _outgoingBuffer.Commit(2);
            BinaryPrimitives.WriteUInt32BigEndian(_outgoingBuffer.AvailableSpan, 0);
            _outgoingBuffer.Commit(4);

            // Send initial connection-level WINDOW_UPDATE
            FrameHeader.WriteTo(_outgoingBuffer.AvailableSpan, FrameHeader.WindowUpdateLength, FrameType.WindowUpdate, FrameFlags.None, streamId: 0);
            _outgoingBuffer.Commit(FrameHeader.Size);
            BinaryPrimitives.WriteUInt32BigEndian(_outgoingBuffer.AvailableSpan, ConnectionWindowSize - DefaultInitialWindowSize);
            _outgoingBuffer.Commit(4);

            await _stream.WriteAsync(_outgoingBuffer.ActiveMemory).ConfigureAwait(false);
            _outgoingBuffer.Discard(_outgoingBuffer.ActiveLength);

            _expectingSettingsAck = true;

            _ = ProcessIncomingFramesAsync();
            _ = ProcessOutgoingFramesAsync();
        }

        private async Task FlushOutgoingBytesAsync()
        {
            if (NetEventSource.Log.IsEnabled()) Trace($"{nameof(_outgoingBuffer.ActiveLength)}={_outgoingBuffer.ActiveLength}");

            if (_outgoingBuffer.ActiveLength > 0)
            {
                try
                {
                    await _stream.WriteAsync(_outgoingBuffer.ActiveMemory).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Abort(e);
                }

                _lastPendingWriterShouldFlush = false;
                _outgoingBuffer.Discard(_outgoingBuffer.ActiveLength);
            }
        }

        private async ValueTask<FrameHeader> ReadFrameAsync(bool initialFrame = false)
        {
            if (NetEventSource.Log.IsEnabled()) Trace($"{nameof(initialFrame)}={initialFrame}");

            // Ensure we've read enough data for the frame header.
            if (_incomingBuffer.ActiveLength < FrameHeader.Size)
            {
                _incomingBuffer.EnsureAvailableSpace(FrameHeader.Size - _incomingBuffer.ActiveLength);
                do
                {
                    int bytesRead = await _stream.ReadAsync(_incomingBuffer.AvailableMemory).ConfigureAwait(false);
                    _incomingBuffer.Commit(bytesRead);
                    if (bytesRead == 0)
                    {
                        if (_incomingBuffer.ActiveLength == 0)
                        {
                            ThrowMissingFrame();
                        }
                        else
                        {
                            ThrowPrematureEOF(FrameHeader.Size);
                        }
                    }
                }
                while (_incomingBuffer.ActiveLength < FrameHeader.Size);
            }

            // Parse the frame header from our read buffer and validate it.
            FrameHeader frameHeader = FrameHeader.ReadFrom(_incomingBuffer.ActiveSpan);
            if (frameHeader.PayloadLength > FrameHeader.MaxPayloadLength)
            {
                if (initialFrame && NetEventSource.Log.IsEnabled())
                {
                    string response = Encoding.ASCII.GetString(_incomingBuffer.ActiveSpan.Slice(0, Math.Min(20, _incomingBuffer.ActiveLength)));
                    Trace($"HTTP/2 handshake failed. Server returned {response}");
                }

                _incomingBuffer.Discard(FrameHeader.Size);
                ThrowProtocolError(initialFrame ? Http2ProtocolErrorCode.ProtocolError : Http2ProtocolErrorCode.FrameSizeError);
            }
            _incomingBuffer.Discard(FrameHeader.Size);

            // Ensure we've read the frame contents into our buffer.
            if (_incomingBuffer.ActiveLength < frameHeader.PayloadLength)
            {
                _incomingBuffer.EnsureAvailableSpace(frameHeader.PayloadLength - _incomingBuffer.ActiveLength);
                do
                {
                    int bytesRead = await _stream.ReadAsync(_incomingBuffer.AvailableMemory).ConfigureAwait(false);
                    _incomingBuffer.Commit(bytesRead);
                    if (bytesRead == 0) ThrowPrematureEOF(frameHeader.PayloadLength);
                }
                while (_incomingBuffer.ActiveLength < frameHeader.PayloadLength);
            }

            // Return the read frame header.
            return frameHeader;

            void ThrowPrematureEOF(int requiredBytes) =>
                throw new IOException(SR.Format(SR.net_http_invalid_response_premature_eof_bytecount, requiredBytes - _incomingBuffer.ActiveLength));

            void ThrowMissingFrame() =>
                throw new IOException(SR.net_http_invalid_response_missing_frame);

        }

        private async Task ProcessIncomingFramesAsync()
        {
            try
            {
                FrameHeader frameHeader;
                try
                {
                    // Read the initial settings frame.
                    frameHeader = await ReadFrameAsync(initialFrame: true).ConfigureAwait(false);
                    if (frameHeader.Type != FrameType.Settings || frameHeader.AckFlag)
                    {
                        ThrowProtocolError();
                    }

                    if (NetEventSource.Log.IsEnabled()) Trace($"Frame 0: {frameHeader}.");

                    // Process the initial SETTINGS frame. This will send an ACK.
                    ProcessSettingsFrame(frameHeader);
                }
                catch (IOException e)
                {
                    throw new IOException(SR.net_http_http2_connection_not_established, e);
                }

                // Keep processing frames as they arrive.
                for (long frameNum = 1; ; frameNum++)
                {
                    // We could just call ReadFrameAsync here, but we add this code
                    // to avoid another state machine allocation in the relatively common case where we
                    // currently don't have enough data buffered and issuing a read for the frame header
                    // completes asynchronously, but that read ends up also reading enough data to fulfill
                    // the entire frame's needs (not just the header).
                    if (_incomingBuffer.ActiveLength < FrameHeader.Size)
                    {
                        _incomingBuffer.EnsureAvailableSpace(FrameHeader.Size - _incomingBuffer.ActiveLength);
                        do
                        {
                            int bytesRead = await _stream.ReadAsync(_incomingBuffer.AvailableMemory).ConfigureAwait(false);
                            Debug.Assert(bytesRead >= 0);
                            _incomingBuffer.Commit(bytesRead);
                            if (bytesRead == 0)
                            {
                                // ReadFrameAsync below will detect that the frame is incomplete and throw the appropriate error.
                                break;
                            }
                        }
                        while (_incomingBuffer.ActiveLength < FrameHeader.Size);
                    }

                    // Read the frame.
                    frameHeader = await ReadFrameAsync().ConfigureAwait(false);
                    if (NetEventSource.Log.IsEnabled()) Trace($"Frame {frameNum}: {frameHeader}.");

                    // Process the frame.
                    switch (frameHeader.Type)
                    {
                        case FrameType.Headers:
                            await ProcessHeadersFrame(frameHeader).ConfigureAwait(false);
                            break;

                        case FrameType.Data:
                            ProcessDataFrame(frameHeader);
                            break;

                        case FrameType.Settings:
                            ProcessSettingsFrame(frameHeader);
                            break;

                        case FrameType.Priority:
                            ProcessPriorityFrame(frameHeader);
                            break;

                        case FrameType.Ping:
                            ProcessPingFrame(frameHeader);
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

                        case FrameType.AltSvc:
                            ProcessAltSvcFrame(frameHeader);
                            break;

                        case FrameType.PushPromise:     // Should not happen, since we disable this in our initial SETTINGS
                        case FrameType.Continuation:    // Should only be received while processing headers in ProcessHeadersFrame
                        default:
                            ThrowProtocolError();
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                if (NetEventSource.Log.IsEnabled()) Trace($"{nameof(ProcessIncomingFramesAsync)}: {e.Message}");

                Abort(e);
            }
        }

        // Note, this will return null for a streamId that's no longer in use.
        // Callers must check for this and send a RST_STREAM or ignore as appropriate.
        // If the streamId is invalid or the stream is idle, calling this function
        // will result in a connection level error.
        private Http2Stream? GetStream(int streamId)
        {
            if (streamId <= 0 || streamId >= _nextStream)
            {
                ThrowProtocolError();
            }

            lock (SyncObject)
            {
                if (!_httpStreams.TryGetValue(streamId, out Http2Stream? http2Stream))
                {
                    return null;
                }

                return http2Stream;
            }
        }

        private async ValueTask ProcessHeadersFrame(FrameHeader frameHeader)
        {
            if (NetEventSource.Log.IsEnabled()) Trace($"{frameHeader}");
            Debug.Assert(frameHeader.Type == FrameType.Headers);

            bool endStream = frameHeader.EndStreamFlag;

            int streamId = frameHeader.StreamId;
            Http2Stream? http2Stream = GetStream(streamId);

            IHttpHeadersHandler headersHandler;
            if (http2Stream != null)
            {
                http2Stream.OnHeadersStart();
                headersHandler = http2Stream;
            }
            else
            {
                // http2Stream will be null if this is a closed stream. We will still process the headers,
                // to ensure the header decoding state is up-to-date, but we will discard the decoded headers.
                headersHandler = NopHeadersHandler.Instance;
            }

            _hpackDecoder.Decode(
                GetFrameData(_incomingBuffer.ActiveSpan.Slice(0, frameHeader.PayloadLength), frameHeader.PaddedFlag, frameHeader.PriorityFlag),
                frameHeader.EndHeadersFlag,
                headersHandler);
            _incomingBuffer.Discard(frameHeader.PayloadLength);

            while (!frameHeader.EndHeadersFlag)
            {
                frameHeader = await ReadFrameAsync().ConfigureAwait(false);
                if (frameHeader.Type != FrameType.Continuation ||
                    frameHeader.StreamId != streamId)
                {
                    ThrowProtocolError();
                }

                _hpackDecoder.Decode(
                    _incomingBuffer.ActiveSpan.Slice(0, frameHeader.PayloadLength),
                    frameHeader.EndHeadersFlag,
                    headersHandler);
                _incomingBuffer.Discard(frameHeader.PayloadLength);
            }

            _hpackDecoder.CompleteDecode();

            http2Stream?.OnHeadersComplete(endStream);
        }

        /// <summary>Nop implementation of <see cref="IHttpHeadersHandler"/> used by <see cref="ProcessHeadersFrame"/>.</summary>
        private sealed class NopHeadersHandler : IHttpHeadersHandler
        {
            public static readonly NopHeadersHandler Instance = new NopHeadersHandler();
            void IHttpHeadersHandler.OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value) { }
            void IHttpHeadersHandler.OnHeadersComplete(bool endStream) { }
            void IHttpHeadersHandler.OnStaticIndexedHeader(int index) { }
            void IHttpHeadersHandler.OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value) { }
        }

        private ReadOnlySpan<byte> GetFrameData(ReadOnlySpan<byte> frameData, bool hasPad, bool hasPriority)
        {
            if (hasPad)
            {
                if (frameData.Length == 0)
                {
                    ThrowProtocolError();
                }

                int padLength = frameData[0];
                frameData = frameData.Slice(1);

                if (frameData.Length < padLength)
                {
                    ThrowProtocolError();
                }

                frameData = frameData.Slice(0, frameData.Length - padLength);
            }

            if (hasPriority)
            {
                if (frameData.Length < FrameHeader.PriorityInfoLength)
                {
                    ThrowProtocolError();
                }

                // We ignore priority info.
                frameData = frameData.Slice(FrameHeader.PriorityInfoLength);
            }

            return frameData;
        }

        /// <summary>
        /// Parses an ALTSVC frame, defined by RFC 7838 Section 4.
        /// </summary>
        /// <remarks>
        /// The RFC states that any parse errors should result in ignoring the frame.
        /// </remarks>
        private void ProcessAltSvcFrame(FrameHeader frameHeader)
        {
            if (NetEventSource.Log.IsEnabled()) Trace($"{frameHeader}");
            Debug.Assert(frameHeader.Type == FrameType.AltSvc);

            ReadOnlySpan<byte> span = _incomingBuffer.ActiveSpan.Slice(0, frameHeader.PayloadLength);

            if (BinaryPrimitives.TryReadUInt16BigEndian(span, out ushort originLength))
            {
                span = span.Slice(2);

                // Check that this ALTSVC frame is valid for our pool's origin. ALTSVC frames can come in one of two ways:
                //  - On stream 0, the origin will be specified. HTTP/2 can service multiple origins per connection, and so the server can report origins other than what our pool is using.
                //  - Otherwise, the origin is implicitly defined by the request stream and must be of length 0.

                if ((frameHeader.StreamId != 0 && originLength == 0) || (frameHeader.StreamId == 0 && span.Length >= originLength && span.Slice(0, originLength).SequenceEqual(_pool.Http2AltSvcOriginUri)))
                {
                    span = span.Slice(originLength);

                    // The span now contains a string with the same format as Alt-Svc headers.

                    string altSvcHeaderValue = Encoding.ASCII.GetString(span);
                    _pool.HandleAltSvc(new[] { altSvcHeaderValue }, null);
                }
            }

            _incomingBuffer.Discard(frameHeader.PayloadLength);
        }

        private void ProcessDataFrame(FrameHeader frameHeader)
        {
            Debug.Assert(frameHeader.Type == FrameType.Data);

            Http2Stream? http2Stream = GetStream(frameHeader.StreamId);

            // Note, http2Stream will be null if this is a closed stream.
            // Just ignore the frame in this case.

            ReadOnlySpan<byte> frameData = GetFrameData(_incomingBuffer.ActiveSpan.Slice(0, frameHeader.PayloadLength), hasPad: frameHeader.PaddedFlag, hasPriority: false);

            if (http2Stream != null)
            {
                bool endStream = frameHeader.EndStreamFlag;

                http2Stream.OnResponseData(frameData, endStream);
            }

            if (frameData.Length > 0)
            {
                ExtendWindow(frameData.Length);
            }

            _incomingBuffer.Discard(frameHeader.PayloadLength);
        }

        private void ProcessSettingsFrame(FrameHeader frameHeader)
        {
            Debug.Assert(frameHeader.Type == FrameType.Settings);

            if (frameHeader.StreamId != 0)
            {
                ThrowProtocolError();
            }

            if (frameHeader.AckFlag)
            {
                if (frameHeader.PayloadLength != 0)
                {
                    ThrowProtocolError(Http2ProtocolErrorCode.FrameSizeError);
                }

                if (!_expectingSettingsAck)
                {
                    ThrowProtocolError();
                }

                // We only send SETTINGS once initially, so we don't need to do anything in response to the ACK.
                // Just remember that we received one and we won't be expecting any more.
                _expectingSettingsAck = false;
            }
            else
            {
                if ((frameHeader.PayloadLength % 6) != 0)
                {
                    ThrowProtocolError(Http2ProtocolErrorCode.FrameSizeError);
                }

                // Parse settings and process the ones we care about.
                ReadOnlySpan<byte> settings = _incomingBuffer.ActiveSpan.Slice(0, frameHeader.PayloadLength);
                while (settings.Length > 0)
                {
                    Debug.Assert((settings.Length % 6) == 0);

                    ushort settingId = BinaryPrimitives.ReadUInt16BigEndian(settings);
                    settings = settings.Slice(2);
                    uint settingValue = BinaryPrimitives.ReadUInt32BigEndian(settings);
                    settings = settings.Slice(4);

                    switch ((SettingId)settingId)
                    {
                        case SettingId.MaxConcurrentStreams:
                            ChangeMaxConcurrentStreams(settingValue);
                            break;

                        case SettingId.InitialWindowSize:
                            if (settingValue > 0x7FFFFFFF)
                            {
                                ThrowProtocolError(Http2ProtocolErrorCode.FlowControlError);
                            }

                            ChangeInitialWindowSize((int)settingValue);
                            break;

                        case SettingId.MaxFrameSize:
                            if (settingValue < 16384 || settingValue > 16777215)
                            {
                                ThrowProtocolError();
                            }

                            // We don't actually store this value; we always send frames of the minimum size (16K).
                            break;

                        default:
                            // All others are ignored because we don't care about them.
                            // Note, per RFC, unknown settings IDs should be ignored.
                            break;
                    }
                }

                _incomingBuffer.Discard(frameHeader.PayloadLength);

                // Send acknowledgement
                // Don't wait for completion, which could happen asynchronously.
                LogExceptions(SendSettingsAckAsync());
            }
        }

        private void ChangeMaxConcurrentStreams(uint newValue)
        {
            if (NetEventSource.Log.IsEnabled()) Trace($"{nameof(newValue)}={newValue}");

            // The value is provided as a uint.
            // Limit this to int.MaxValue since the CreditManager implementation only supports singed values.
            // In practice, we should never reach this value.
            int effectiveValue = newValue > (uint)int.MaxValue ? int.MaxValue : (int)newValue;
            int delta = effectiveValue - _maxConcurrentStreams;
            _maxConcurrentStreams = effectiveValue;

            _concurrentStreams.AdjustCredit(delta);
        }

        private void ChangeInitialWindowSize(int newSize)
        {
            if (NetEventSource.Log.IsEnabled()) Trace($"{nameof(newSize)}={newSize}");
            Debug.Assert(newSize >= 0);

            lock (SyncObject)
            {
                int delta = newSize - _initialWindowSize;
                _initialWindowSize = newSize;

                // Adjust existing streams
                foreach (KeyValuePair<int, Http2Stream> kvp in _httpStreams)
                {
                    kvp.Value.OnWindowUpdate(delta);
                }
            }
        }

        private void ProcessPriorityFrame(FrameHeader frameHeader)
        {
            Debug.Assert(frameHeader.Type == FrameType.Priority);

            if (frameHeader.StreamId == 0 || frameHeader.PayloadLength != FrameHeader.PriorityInfoLength)
            {
                ThrowProtocolError();
            }

            // Ignore priority info.

            _incomingBuffer.Discard(frameHeader.PayloadLength);
        }

        private void ProcessPingFrame(FrameHeader frameHeader)
        {
            Debug.Assert(frameHeader.Type == FrameType.Ping);

            if (frameHeader.StreamId != 0)
            {
                ThrowProtocolError();
            }

            if (frameHeader.AckFlag)
            {
                // We never send PING, so an ACK indicates a protocol error
                ThrowProtocolError();
            }

            if (frameHeader.PayloadLength != FrameHeader.PingLength)
            {
                ThrowProtocolError(Http2ProtocolErrorCode.FrameSizeError);
            }

            // We don't wait for SendPingAckAsync to complete before discarding
            // the incoming buffer, so we need to take a copy of the data. Read
            // it as a big-endian integer here to avoid allocating an array.
            Debug.Assert(sizeof(long) == FrameHeader.PingLength);
            ReadOnlySpan<byte> pingContent = _incomingBuffer.ActiveSpan.Slice(0, FrameHeader.PingLength);
            long pingContentLong = BinaryPrimitives.ReadInt64BigEndian(pingContent);

            LogExceptions(SendPingAckAsync(pingContentLong));

            _incomingBuffer.Discard(frameHeader.PayloadLength);
        }

        private void ProcessWindowUpdateFrame(FrameHeader frameHeader)
        {
            Debug.Assert(frameHeader.Type == FrameType.WindowUpdate);

            if (frameHeader.PayloadLength != FrameHeader.WindowUpdateLength)
            {
                ThrowProtocolError(Http2ProtocolErrorCode.FrameSizeError);
            }

            int amount = BinaryPrimitives.ReadInt32BigEndian(_incomingBuffer.ActiveSpan) & 0x7FFFFFFF;
            if (NetEventSource.Log.IsEnabled()) Trace($"{frameHeader}. {nameof(amount)}={amount}");

            Debug.Assert(amount >= 0);
            if (amount == 0)
            {
                ThrowProtocolError();
            }

            _incomingBuffer.Discard(frameHeader.PayloadLength);

            if (frameHeader.StreamId == 0)
            {
                _connectionWindow.AdjustCredit(amount);
            }
            else
            {
                Http2Stream? http2Stream = GetStream(frameHeader.StreamId);
                if (http2Stream == null)
                {
                    // Ignore invalid stream ID, as per RFC
                    return;
                }

                http2Stream.OnWindowUpdate(amount);
            }
        }

        private void ProcessRstStreamFrame(FrameHeader frameHeader)
        {
            Debug.Assert(frameHeader.Type == FrameType.RstStream);

            if (frameHeader.PayloadLength != FrameHeader.RstStreamLength)
            {
                ThrowProtocolError(Http2ProtocolErrorCode.FrameSizeError);
            }

            if (frameHeader.StreamId == 0)
            {
                ThrowProtocolError();
            }

            Http2Stream? http2Stream = GetStream(frameHeader.StreamId);
            if (http2Stream == null)
            {
                // Ignore invalid stream ID, as per RFC
                _incomingBuffer.Discard(frameHeader.PayloadLength);
                return;
            }

            var protocolError = (Http2ProtocolErrorCode)BinaryPrimitives.ReadInt32BigEndian(_incomingBuffer.ActiveSpan);
            if (NetEventSource.Log.IsEnabled()) Trace(frameHeader.StreamId, $"{nameof(protocolError)}={protocolError}");

            _incomingBuffer.Discard(frameHeader.PayloadLength);

            if (protocolError == Http2ProtocolErrorCode.RefusedStream)
            {
                http2Stream.OnReset(new Http2StreamException(protocolError), resetStreamErrorCode: protocolError, canRetry: true);
            }
            else
            {
                http2Stream.OnReset(new Http2StreamException(protocolError), resetStreamErrorCode: protocolError);
            }
        }

        private void ProcessGoAwayFrame(FrameHeader frameHeader)
        {
            Debug.Assert(frameHeader.Type == FrameType.GoAway);

            if (frameHeader.PayloadLength < FrameHeader.GoAwayMinLength)
            {
                ThrowProtocolError(Http2ProtocolErrorCode.FrameSizeError);
            }

            // GoAway frames always apply to the whole connection, never to a single stream.
            // According to RFC 7540 section 6.8, this should be a connection error.
            if (frameHeader.StreamId != 0)
            {
                ThrowProtocolError();
            }

            int lastValidStream = (int)(BinaryPrimitives.ReadUInt32BigEndian(_incomingBuffer.ActiveSpan) & 0x7FFFFFFF);
            var errorCode = (Http2ProtocolErrorCode)BinaryPrimitives.ReadInt32BigEndian(_incomingBuffer.ActiveSpan.Slice(sizeof(int)));
            if (NetEventSource.Log.IsEnabled()) Trace(frameHeader.StreamId, $"{nameof(lastValidStream)}={lastValidStream}, {nameof(errorCode)}={errorCode}");

            StartTerminatingConnection(lastValidStream, new Http2ConnectionException(errorCode));

            _incomingBuffer.Discard(frameHeader.PayloadLength);
        }

        internal Task FlushAsync(CancellationToken cancellationToken) =>
            PerformWriteAsync(0, 0, static (_, __) => true, cancellationToken);

        private abstract class WriteQueueEntry : TaskCompletionSource
        {
            private readonly CancellationToken _cancellationToken;
            private readonly CancellationTokenRegistration _cancellationRegistration;

            public WriteQueueEntry(int writeBytes, CancellationToken cancellationToken)
                : base(TaskCreationOptions.RunContinuationsAsynchronously)
            {
                WriteBytes = writeBytes;

                _cancellationToken = cancellationToken;
                _cancellationRegistration = cancellationToken.UnsafeRegister(static s => ((WriteQueueEntry)s!).OnCancellation(), this);
            }

            public int WriteBytes { get; }

            private void OnCancellation()
            {
                SetCanceled(_cancellationToken);
            }

            public bool TryDisableCancellation()
            {
                _cancellationRegistration.Dispose();
                return !Task.IsCanceled;
            }

            public abstract bool InvokeWriteAction(Memory<byte> writeBuffer);
        }

        private sealed class WriteQueueEntry<T> : WriteQueueEntry
        {
            private readonly T _state;
            private readonly Func<T, Memory<byte>, bool> _writeAction;

            public WriteQueueEntry(int writeBytes, T state, Func<T, Memory<byte>, bool> writeAction, CancellationToken cancellationToken)
                : base(writeBytes, cancellationToken)
            {
                _state = state;
                _writeAction = writeAction;
            }

            public override bool InvokeWriteAction(Memory<byte> writeBuffer)
            {
                return _writeAction(_state, writeBuffer);
            }
        }

        private Task PerformWriteAsync<T>(int writeBytes, T state, Func<T, Memory<byte>, bool> writeAction, CancellationToken cancellationToken = default)
        {
            WriteQueueEntry writeEntry = new WriteQueueEntry<T>(writeBytes, state, writeAction, cancellationToken);

            if (!_writeChannel.Writer.TryWrite(writeEntry))
            {
                Debug.Assert(_abortException is not null);
                return Task.FromException(GetRequestAbortedException(_abortException));
            }

            return writeEntry.Task;
        }

        private async Task ProcessOutgoingFramesAsync()
        {
            try
            {
                while (await _writeChannel.Reader.WaitToReadAsync().ConfigureAwait(false))
                {
                    while (_writeChannel.Reader.TryRead(out WriteQueueEntry? writeEntry))
                    {
                        if (writeEntry.TryDisableCancellation())
                        {
                            if (_abortException is not null)
                            {
                                writeEntry.SetException(_abortException);
                            }
                            else
                            {
                                int writeBytes = writeEntry.WriteBytes;

                                // If the buffer has already grown to 32k, does not have room for the next request,
                                // and is non-empty, flush the current contents to the wire.
                                int totalBufferLength = _outgoingBuffer.Capacity;
                                if (totalBufferLength >= UnflushedOutgoingBufferSize)
                                {
                                    int activeBufferLength = _outgoingBuffer.ActiveLength;
                                    if (writeBytes >= totalBufferLength - activeBufferLength)
                                    {
                                        await FlushOutgoingBytesAsync().ConfigureAwait(false);
                                    }
                                }

                                _outgoingBuffer.EnsureAvailableSpace(writeBytes);

                                try
                                {
                                    if (NetEventSource.Log.IsEnabled()) Trace($"{nameof(writeBytes)}={writeBytes}");

                                    // Invoke the callback with the supplied state and the target write buffer.
                                    bool flush = writeEntry.InvokeWriteAction(_outgoingBuffer.AvailableMemorySliced(writeBytes));

                                    writeEntry.SetResult();

                                    _outgoingBuffer.Commit(writeBytes);
                                    _lastPendingWriterShouldFlush |= flush;
                                }
                                catch (Exception e)
                                {
                                    writeEntry.SetException(e);
                                }
                            }

                        }
                    }

                    // Nothing left in the queue to process.
                    // Flush the write buffer if we need to.
                    if (_lastPendingWriterShouldFlush)
                    {
                        await FlushOutgoingBytesAsync().ConfigureAwait(false);
                    }
                }

                // Connection should be aborting at this point.
                Debug.Assert(_abortException is not null);
            }
            catch (Exception e)
            {
                if (NetEventSource.Log.IsEnabled()) Trace($"Unexpected exception in {nameof(ProcessOutgoingFramesAsync)}: {e}");

                Debug.Fail($"Unexpected exception in {nameof(ProcessOutgoingFramesAsync)}: {e}");
            }
        }

        private Task SendSettingsAckAsync() =>
            PerformWriteAsync(FrameHeader.Size, this, static (thisRef, writeBuffer) =>
            {
                if (NetEventSource.Log.IsEnabled()) thisRef.Trace("Started writing.");

                FrameHeader.WriteTo(writeBuffer.Span, 0, FrameType.Settings, FrameFlags.Ack, streamId: 0);

                return true;
            });

        /// <param name="pingContent">The 8-byte ping content to send, read as a big-endian integer.</param>
        private Task SendPingAckAsync(long pingContent) =>
            PerformWriteAsync(FrameHeader.Size + FrameHeader.PingLength, (thisRef: this, pingContent), static (state, writeBuffer) =>
            {
                if (NetEventSource.Log.IsEnabled()) state.thisRef.Trace("Started writing.");

                Debug.Assert(sizeof(long) == FrameHeader.PingLength);

                Span<byte> span = writeBuffer.Span;
                FrameHeader.WriteTo(span, FrameHeader.PingLength, FrameType.Ping, FrameFlags.Ack, streamId: 0);
                BinaryPrimitives.WriteInt64BigEndian(span.Slice(FrameHeader.Size), state.pingContent);

                return true;
            });

        private Task SendRstStreamAsync(int streamId, Http2ProtocolErrorCode errorCode) =>
            PerformWriteAsync(FrameHeader.Size + FrameHeader.RstStreamLength, (thisRef: this, streamId, errorCode), static (s, writeBuffer) =>
            {
                if (NetEventSource.Log.IsEnabled()) s.thisRef.Trace(s.streamId, $"Started writing. {nameof(s.errorCode)}={s.errorCode}");

                Span<byte> span = writeBuffer.Span;
                FrameHeader.WriteTo(span, FrameHeader.RstStreamLength, FrameType.RstStream, FrameFlags.None, s.streamId);
                BinaryPrimitives.WriteInt32BigEndian(span.Slice(FrameHeader.Size), (int)s.errorCode);

                return true;
            });

        private static (ReadOnlyMemory<byte> first, ReadOnlyMemory<byte> rest) SplitBuffer(ReadOnlyMemory<byte> buffer, int maxSize) =>
            buffer.Length > maxSize ?
                (buffer.Slice(0, maxSize), buffer.Slice(maxSize)) :
                (buffer, Memory<byte>.Empty);

        private void WriteIndexedHeader(int index, ref ArrayBuffer headerBuffer)
        {
            if (NetEventSource.Log.IsEnabled()) Trace($"{nameof(index)}={index}");

            int bytesWritten;
            while (!HPackEncoder.EncodeIndexedHeaderField(index, headerBuffer.AvailableSpan, out bytesWritten))
            {
                headerBuffer.EnsureAvailableSpace(headerBuffer.AvailableLength + 1);
            }

            headerBuffer.Commit(bytesWritten);
        }

        private void WriteIndexedHeader(int index, string value, ref ArrayBuffer headerBuffer)
        {
            if (NetEventSource.Log.IsEnabled()) Trace($"{nameof(index)}={index}, {nameof(value)}={value}");

            int bytesWritten;
            while (!HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexing(index, value, headerBuffer.AvailableSpan, out bytesWritten))
            {
                headerBuffer.EnsureAvailableSpace(headerBuffer.AvailableLength + 1);
            }

            headerBuffer.Commit(bytesWritten);
        }

        private void WriteLiteralHeader(string name, ReadOnlySpan<string> values, Encoding? valueEncoding, ref ArrayBuffer headerBuffer)
        {
            if (NetEventSource.Log.IsEnabled()) Trace($"{nameof(name)}={name}, {nameof(values)}={string.Join(", ", values.ToArray())}");

            int bytesWritten;
            while (!HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexingNewName(name, values, HttpHeaderParser.DefaultSeparator, valueEncoding, headerBuffer.AvailableSpan, out bytesWritten))
            {
                headerBuffer.EnsureAvailableSpace(headerBuffer.AvailableLength + 1);
            }

            headerBuffer.Commit(bytesWritten);
        }

        private void WriteLiteralHeaderValues(ReadOnlySpan<string> values, string? separator, Encoding? valueEncoding, ref ArrayBuffer headerBuffer)
        {
            if (NetEventSource.Log.IsEnabled()) Trace($"{nameof(values)}={string.Join(separator, values.ToArray())}");

            int bytesWritten;
            while (!HPackEncoder.EncodeStringLiterals(values, separator, valueEncoding, headerBuffer.AvailableSpan, out bytesWritten))
            {
                headerBuffer.EnsureAvailableSpace(headerBuffer.AvailableLength + 1);
            }

            headerBuffer.Commit(bytesWritten);
        }

        private void WriteLiteralHeaderValue(string value, Encoding? valueEncoding, ref ArrayBuffer headerBuffer)
        {
            if (NetEventSource.Log.IsEnabled()) Trace($"{nameof(value)}={value}");

            int bytesWritten;
            while (!HPackEncoder.EncodeStringLiteral(value, valueEncoding, headerBuffer.AvailableSpan, out bytesWritten))
            {
                headerBuffer.EnsureAvailableSpace(headerBuffer.AvailableLength + 1);
            }

            headerBuffer.Commit(bytesWritten);
        }

        private void WriteBytes(ReadOnlySpan<byte> bytes, ref ArrayBuffer headerBuffer)
        {
            if (NetEventSource.Log.IsEnabled()) Trace($"{nameof(bytes.Length)}={bytes.Length}");

            if (bytes.Length > headerBuffer.AvailableLength)
            {
                headerBuffer.EnsureAvailableSpace(bytes.Length);
            }

            bytes.CopyTo(headerBuffer.AvailableSpan);
            headerBuffer.Commit(bytes.Length);
        }

        private void WriteHeaderCollection(HttpRequestMessage request, HttpHeaders headers, ref ArrayBuffer headerBuffer)
        {
            if (NetEventSource.Log.IsEnabled()) Trace("");

            if (headers.HeaderStore is null)
            {
                return;
            }

            HeaderEncodingSelector<HttpRequestMessage>? encodingSelector = _pool.Settings._requestHeaderEncodingSelector;

            ref string[]? tmpHeaderValuesArray = ref t_headerValues;
            foreach (KeyValuePair<HeaderDescriptor, object> header in headers.HeaderStore)
            {
                int headerValuesCount = HttpHeaders.GetValuesAsStrings(header.Key, header.Value, ref tmpHeaderValuesArray);
                Debug.Assert(headerValuesCount > 0, "No values for header??");
                ReadOnlySpan<string> headerValues = tmpHeaderValuesArray.AsSpan(0, headerValuesCount);

                Encoding? valueEncoding = encodingSelector?.Invoke(header.Key.Name, request);

                KnownHeader? knownHeader = header.Key.KnownHeader;
                if (knownHeader != null)
                {
                    // The Host header is not sent for HTTP2 because we send the ":authority" pseudo-header instead
                    // (see pseudo-header handling below in WriteHeaders).
                    // The Connection, Upgrade and ProxyConnection headers are also not supported in HTTP2.
                    if (knownHeader != KnownHeaders.Host && knownHeader != KnownHeaders.Connection && knownHeader != KnownHeaders.Upgrade && knownHeader != KnownHeaders.ProxyConnection)
                    {
                        if (header.Key.KnownHeader == KnownHeaders.TE)
                        {
                            // HTTP/2 allows only 'trailers' TE header. rfc7540 8.1.2.2
                            foreach (string value in headerValues)
                            {
                                if (string.Equals(value, "trailers", StringComparison.OrdinalIgnoreCase))
                                {
                                    WriteBytes(knownHeader.Http2EncodedName, ref headerBuffer);
                                    WriteLiteralHeaderValue(value, valueEncoding, ref headerBuffer);
                                    break;
                                }
                            }
                            continue;
                        }

                        // For all other known headers, send them via their pre-encoded name and the associated value.
                        WriteBytes(knownHeader.Http2EncodedName, ref headerBuffer);
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

                        WriteLiteralHeaderValues(headerValues, separator, valueEncoding, ref headerBuffer);
                    }
                }
                else
                {
                    // The header is not known: fall back to just encoding the header name and value(s).
                    WriteLiteralHeader(header.Key.Name, headerValues, valueEncoding, ref headerBuffer);
                }
            }
        }

        private void WriteHeaders(HttpRequestMessage request, ref ArrayBuffer headerBuffer)
        {
            if (NetEventSource.Log.IsEnabled()) Trace("");

            // HTTP2 does not support Transfer-Encoding: chunked, so disable this on the request.
            if (request.HasHeaders && request.Headers.TransferEncodingChunked == true)
            {
                request.Headers.TransferEncodingChunked = false;
            }

            HttpMethod normalizedMethod = HttpMethod.Normalize(request.Method);

            // Method is normalized so we can do reference equality here.
            if (ReferenceEquals(normalizedMethod, HttpMethod.Get))
            {
                WriteIndexedHeader(H2StaticTable.MethodGet, ref headerBuffer);
            }
            else if (ReferenceEquals(normalizedMethod, HttpMethod.Post))
            {
                WriteIndexedHeader(H2StaticTable.MethodPost, ref headerBuffer);
            }
            else
            {
                WriteIndexedHeader(H2StaticTable.MethodGet, normalizedMethod.Method, ref headerBuffer);
            }

            WriteIndexedHeader(_stream is SslStream ? H2StaticTable.SchemeHttps : H2StaticTable.SchemeHttp, ref headerBuffer);

            if (request.HasHeaders && request.Headers.Host != null)
            {
                WriteIndexedHeader(H2StaticTable.Authority, request.Headers.Host, ref headerBuffer);
            }
            else
            {
                WriteBytes(_pool._http2EncodedAuthorityHostHeader, ref headerBuffer);
            }

            Debug.Assert(request.RequestUri != null);
            string pathAndQuery = request.RequestUri.PathAndQuery;
            if (pathAndQuery == "/")
            {
                WriteIndexedHeader(H2StaticTable.PathSlash, ref headerBuffer);
            }
            else
            {
                WriteIndexedHeader(H2StaticTable.PathSlash, pathAndQuery, ref headerBuffer);
            }

            if (request.HasHeaders)
            {
                WriteHeaderCollection(request, request.Headers, ref headerBuffer);
            }

            // Determine cookies to send.
            if (_pool.Settings._useCookies)
            {
                string cookiesFromContainer = _pool.Settings._cookieContainer!.GetCookieHeader(request.RequestUri);
                if (cookiesFromContainer != string.Empty)
                {
                    WriteBytes(KnownHeaders.Cookie.Http2EncodedName, ref headerBuffer);

                    Encoding? cookieEncoding = _pool.Settings._requestHeaderEncodingSelector?.Invoke(KnownHeaders.Cookie.Name, request);
                    WriteLiteralHeaderValue(cookiesFromContainer, cookieEncoding, ref headerBuffer);
                }
            }

            if (request.Content == null)
            {
                // Write out Content-Length: 0 header to indicate no body,
                // unless this is a method that never has a body.
                if (normalizedMethod.MustHaveRequestBody)
                {
                    WriteBytes(KnownHeaders.ContentLength.Http2EncodedName, ref headerBuffer);
                    WriteLiteralHeaderValue("0", valueEncoding: null, ref headerBuffer);
                }
            }
            else
            {
                WriteHeaderCollection(request, request.Content.Headers, ref headerBuffer);
            }
        }

        [DoesNotReturn]
        private void ThrowShutdownException()
        {
            Debug.Assert(Monitor.IsEntered(SyncObject));

            // Throw a retryable exception that will allow this unprocessed request to be processed on a new connection.
            // In rare cases, such as receiving GOAWAY immediately after connection establishment, we will not
            // actually retry the request, so we must give a useful exception here for these cases.

            Exception innerException;
            if (_abortException != null)
            {
                innerException = _abortException;
            }
            else if (_lastStreamId != -1)
            {
                // We must have received a GOAWAY.
                innerException = new IOException(SR.net_http_server_shutdown);
            }
            else
            {
                // We must either be disposed or out of stream IDs.
                // Note that in this case, the exception should never be visible to the user (it should be retried).
                Debug.Assert(_disposed || _nextStream == MaxStreamId);

                innerException = new ObjectDisposedException(nameof(Http2Connection));
            }

            ThrowRetry(SR.net_http_client_execution_error, innerException);
        }

        private async ValueTask<Http2Stream> SendHeadersAsync(HttpRequestMessage request, CancellationToken cancellationToken, bool mustFlush)
        {
            // Enforce MAX_CONCURRENT_STREAMS setting value.  We do this before anything else, e.g. renting buffers to serialize headers,
            // in order to avoid consuming resources in potentially many requests waiting for access.
            try
            {
                if (_pool.EnableMultipleHttp2Connections)
                {
                    if (!_concurrentStreams.TryRequestCreditNoWait(1))
                    {
                        throw new HttpRequestException(null, null, RequestRetryType.RetryOnNextConnection);
                    }
                }
                else
                {
                    await _concurrentStreams.RequestCreditAsync(1, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (ObjectDisposedException)
            {
                // We have race condition between shutting down and initiating new requests.
                // When we are shutting down the connection (e.g. due to receiving GOAWAY, etc)
                // we will wait until the stream count goes to 0, and then we will close the connetion
                // and perform clean up, including disposing _concurrentStreams.
                // So if we get ObjectDisposedException here, we must have shut down the connection.
                // Throw a retryable request exception if this is not result of some other error.
                // This will cause retry logic to kick in and perform another connection attempt.
                // The user should never see this exception.  See similar handling below.
                // Throw a retryable request exception if this is not result of some other error.
                // This will cause retry logic to kick in and perform another connection attempt.
                // The user should never see this exception.  See also below.
                lock (SyncObject)
                {
                    Debug.Assert(_disposed || _lastStreamId != -1);
                    Debug.Assert(_httpStreams.Count == 0);
                    ThrowShutdownException();
                    throw; // unreachable
                }
            }

            ArrayBuffer headerBuffer = default;
            try
            {
                // Serialize headers to a temporary buffer, and do as much work to prepare to send the headers as we can
                // before taking the write lock.
                headerBuffer = new ArrayBuffer(InitialConnectionBufferSize, usePool: true);
                WriteHeaders(request, ref headerBuffer);
                ReadOnlyMemory<byte> headerBytes = headerBuffer.ActiveMemory;
                Debug.Assert(headerBytes.Length > 0);

                // Calculate the total number of bytes we're going to use (content + headers).
                int frameCount = ((headerBytes.Length - 1) / FrameHeader.MaxPayloadLength) + 1;
                int totalSize = headerBytes.Length + (frameCount * FrameHeader.Size);

                // Construct and initialize the new Http2Stream instance.  It's stream ID must be set below
                // before the instance is used and stored into the dictionary.  However, we construct it here
                // so as to avoid the allocation and initialization expense while holding multiple locks.
                var http2Stream = new Http2Stream(request, this);

                // Start the write.  This serializes access to write to the connection, and ensures that HEADERS
                // and CONTINUATION frames stay together, as they must do. We use the lock as well to ensure new
                // streams are created and started in order.
                await PerformWriteAsync(totalSize, (thisRef: this, http2Stream, headerBytes, endStream: (request.Content == null), mustFlush), static (s, writeBuffer) =>
                {
                    if (NetEventSource.Log.IsEnabled()) s.thisRef.Trace(s.http2Stream.StreamId, $"Started writing. Total header bytes={s.headerBytes.Length}");

                    // Allocate the next available stream ID. Note that if we fail before sending the headers,
                    // we'll just skip this stream ID, which is fine.
                    lock (s.thisRef.SyncObject)
                    {
                        if (s.thisRef._nextStream == MaxStreamId || s.thisRef._disposed || s.thisRef._lastStreamId != -1)
                        {
                            // We ran out of stream IDs or we raced between acquiring the connection from the pool and shutting down.
                            // Throw a retryable request exception. This will cause retry logic to kick in
                            // and perform another connection attempt. The user should never see this exception.
                            s.thisRef.ThrowShutdownException();
                        }

                        // Now that we're holding the lock, configure the stream.  The lock must be held while
                        // assigning the stream ID to ensure only one stream gets an ID, and it must be held
                        // across setting the initial window size (available credit) and storing the stream into
                        // collection such that window size updates are able to atomically affect all known streams.
                        s.http2Stream.Initialize(s.thisRef._nextStream, s.thisRef._initialWindowSize);

                        // Client-initiated streams are always odd-numbered, so increase by 2.
                        s.thisRef._nextStream += 2;

                        // We're about to flush the HEADERS frame, so add the stream to the dictionary now.
                        // The lifetime of the stream is now controlled by the stream itself and the connection.
                        // This can fail if the connection is shutting down, in which case we will cancel sending this frame.
                        s.thisRef._httpStreams.Add(s.http2Stream.StreamId, s.http2Stream);
                    }

                    Span<byte> span = writeBuffer.Span;

                    // Copy the HEADERS frame.
                    ReadOnlyMemory<byte> current, remaining;
                    (current, remaining) = SplitBuffer(s.headerBytes, FrameHeader.MaxPayloadLength);
                    FrameFlags flags = (remaining.Length == 0 ? FrameFlags.EndHeaders : FrameFlags.None);
                    flags |= (s.endStream ? FrameFlags.EndStream : FrameFlags.None);
                    FrameHeader.WriteTo(span, current.Length, FrameType.Headers, flags, s.http2Stream.StreamId);
                    span = span.Slice(FrameHeader.Size);
                    current.Span.CopyTo(span);
                    span = span.Slice(current.Length);
                    if (NetEventSource.Log.IsEnabled()) s.thisRef.Trace(s.http2Stream.StreamId, $"Wrote HEADERS frame. Length={current.Length}, flags={flags}");

                    // Copy CONTINUATION frames, if any.
                    while (remaining.Length > 0)
                    {
                        (current, remaining) = SplitBuffer(remaining, FrameHeader.MaxPayloadLength);
                        flags = remaining.Length == 0 ? FrameFlags.EndHeaders : FrameFlags.None;

                        FrameHeader.WriteTo(span, current.Length, FrameType.Continuation, flags, s.http2Stream.StreamId);
                        span = span.Slice(FrameHeader.Size);
                        current.Span.CopyTo(span);
                        span = span.Slice(current.Length);
                        if (NetEventSource.Log.IsEnabled()) s.thisRef.Trace(s.http2Stream.StreamId, $"Wrote CONTINUATION frame. Length={current.Length}, flags={flags}");
                    }

                    Debug.Assert(span.Length == 0);

                    return s.mustFlush || s.endStream;
                }, cancellationToken).ConfigureAwait(false);
                return http2Stream;
            }
            catch
            {
                _concurrentStreams.AdjustCredit(1);
                throw;
            }
            finally
            {
                headerBuffer.Dispose();
            }
        }

        private async Task SendStreamDataAsync(int streamId, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            ReadOnlyMemory<byte> remaining = buffer;

            while (remaining.Length > 0)
            {
                // Once credit had been granted, we want to actually consume those bytes.
                int frameSize = Math.Min(remaining.Length, FrameHeader.MaxPayloadLength);
                frameSize = await _connectionWindow.RequestCreditAsync(frameSize, cancellationToken).ConfigureAwait(false);

                ReadOnlyMemory<byte> current;
                (current, remaining) = SplitBuffer(remaining, frameSize);
                try
                {
                    await PerformWriteAsync(FrameHeader.Size + current.Length, (thisRef: this, streamId, current), static (s, writeBuffer) =>
                    {
                        // Invoked while holding the lock:
                        if (NetEventSource.Log.IsEnabled()) s.thisRef.Trace(s.streamId, $"Started writing. {nameof(writeBuffer.Length)}={writeBuffer.Length}");

                        FrameHeader.WriteTo(writeBuffer.Span, s.current.Length, FrameType.Data, FrameFlags.None, s.streamId);
                        s.current.CopyTo(writeBuffer.Slice(FrameHeader.Size));

                        return false; // no need to flush, as the request content may do so explicitly, or worst case we'll do so as part of the end data frame
                    }, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    // Invoked if waiting for the lock is canceled (in that case, we need to return the credit that we have acquired and don't plan to use):
                    _connectionWindow.AdjustCredit(frameSize);
                    throw;
                }
            }
        }

        private Task SendEndStreamAsync(int streamId) =>
            PerformWriteAsync(FrameHeader.Size, (thisRef: this, streamId), static (s, writeBuffer) =>
            {
                if (NetEventSource.Log.IsEnabled()) s.thisRef.Trace(s.streamId, "Started writing.");

                FrameHeader.WriteTo(writeBuffer.Span, 0, FrameType.Data, FrameFlags.EndStream, s.streamId);

                return true; // finished sending request body, so flush soon (but ok to wait for pending packets)
            });

        private Task SendWindowUpdateAsync(int streamId, int amount)
        {
            // We update both the connection-level and stream-level windows at the same time
            Debug.Assert(amount > 0);
            return PerformWriteAsync(FrameHeader.Size + FrameHeader.WindowUpdateLength, (thisRef: this, streamId, amount), static (s, writeBuffer) =>
            {
                if (NetEventSource.Log.IsEnabled()) s.thisRef.Trace(s.streamId, $"Started writing. {nameof(s.amount)}={s.amount}");

                Span<byte> span = writeBuffer.Span;
                FrameHeader.WriteTo(span, FrameHeader.WindowUpdateLength, FrameType.WindowUpdate, FrameFlags.None, s.streamId);
                BinaryPrimitives.WriteInt32BigEndian(span.Slice(FrameHeader.Size), s.amount);

                return true;
            });
        }

        private void ExtendWindow(int amount)
        {
            if (NetEventSource.Log.IsEnabled()) Trace($"{nameof(amount)}={amount}");
            Debug.Assert(amount > 0);

            int windowUpdateSize;
            lock (SyncObject)
            {
                Debug.Assert(_pendingWindowUpdate < ConnectionWindowThreshold);

                _pendingWindowUpdate += amount;
                if (_pendingWindowUpdate < ConnectionWindowThreshold)
                {
                    if (NetEventSource.Log.IsEnabled()) Trace($"{nameof(_pendingWindowUpdate)} {_pendingWindowUpdate} < {ConnectionWindowThreshold}.");
                    return;
                }

                windowUpdateSize = _pendingWindowUpdate;
                _pendingWindowUpdate = 0;
            }

            LogExceptions(SendWindowUpdateAsync(0, windowUpdateSize));
        }

        /// <summary>Abort all streams and cause further processing to fail.</summary>
        /// <param name="abortException">Exception causing Abort to be called.</param>
        private void Abort(Exception abortException)
        {
            // The connection has failed, e.g. failed IO or a connection-level frame error.
            bool alreadyAborting = false;
            lock (SyncObject)
            {
                if (_abortException is null)
                {
                    _abortException = abortException;
                }
                else
                {
                    alreadyAborting = true;
                }
            }

            if (alreadyAborting)
            {
                if (NetEventSource.Log.IsEnabled()) Trace($"Abort called while already aborting. {nameof(abortException)}=={abortException}");

                return;
            }

            if (NetEventSource.Log.IsEnabled()) Trace($"Abort initiated. {nameof(abortException)}=={abortException}");

            _writeChannel.Writer.Complete();

            AbortStreams(_abortException);
        }

        /// <summary>Gets whether the connection exceeded any of the connection limits.</summary>
        /// <param name="nowTicks">The current tick count.  Passed in to amortize the cost of calling Environment.TickCount64.</param>
        /// <param name="connectionLifetime">How long a connection can be open to be considered reusable.</param>
        /// <param name="connectionIdleTimeout">How long a connection can have been idle in the pool to be considered reusable.</param>
        /// <returns>
        /// true if we believe the connection is expired; otherwise, false.  There is an inherent race condition here,
        /// in that the server could terminate the connection or otherwise make it unusable immediately after we check it,
        /// but there's not much difference between that and starting to use the connection and then having the server
        /// terminate it, which would be considered a failure, so this race condition is largely benign and inherent to
        /// the nature of connection pooling.
        /// </returns>
        public bool IsExpired(long nowTicks,
                              TimeSpan connectionLifetime,
                              TimeSpan connectionIdleTimeout)
        {
            if (_disposed)
            {
                return true;
            }

            // Check idle timeout when there are not pending requests for a while.
            if ((connectionIdleTimeout != Timeout.InfiniteTimeSpan) &&
                (_httpStreams.Count == 0) &&
                ((nowTicks - _idleSinceTickCount) > connectionIdleTimeout.TotalMilliseconds))
            {
                if (NetEventSource.Log.IsEnabled()) Trace($"Connection no longer usable. Idle {TimeSpan.FromMilliseconds(nowTicks - _idleSinceTickCount)} > {connectionIdleTimeout}.");

                return true;
            }

            return LifetimeExpired(nowTicks, connectionLifetime);
        }

        private void AbortStreams(Exception abortException)
        {
            if (NetEventSource.Log.IsEnabled()) Trace($"{nameof(abortException)}={abortException}");

            // Invalidate outside of lock to avoid race with HttpPool Dispose()
            // We should not try to grab pool lock while holding connection lock as on disposing pool,
            // we could hold pool lock while trying to grab connection lock in Dispose().
            _pool.InvalidateHttp2Connection(this);

            List<Http2Stream> streamsToAbort = new List<Http2Stream>();

            lock (SyncObject)
            {
                // Set _lastStreamId to int.MaxValue to indicate that we are shutting down
                // and we must assume all active streams have been processed by the server
                _lastStreamId = int.MaxValue;

                foreach (KeyValuePair<int, Http2Stream> kvp in _httpStreams)
                {
                    int streamId = kvp.Key;
                    Debug.Assert(streamId == kvp.Value.StreamId);

                    streamsToAbort.Add(kvp.Value);
                }

                CheckForShutdown();
            }

            // Avoid calling OnAbort under the lock, as it may cause the Http2Stream
            // to call back in to RemoveStream
            foreach (Http2Stream s in streamsToAbort)
            {
                s.OnReset(abortException);
            }
        }

        private void StartTerminatingConnection(int lastValidStream, Exception abortException)
        {
            Debug.Assert(lastValidStream >= 0);

            // Invalidate outside of lock to avoid race with HttpPool Dispose()
            // We should not try to grab pool lock while holding connection lock as on disposing pool,
            // we could hold pool lock while trying to grab connection lock in Dispose().
            _pool.InvalidateHttp2Connection(this);

            List<Http2Stream> streamsToAbort = new List<Http2Stream>();

            lock (SyncObject)
            {
                if (_lastStreamId == -1)
                {
                    _lastStreamId = lastValidStream;
                }
                else
                {
                    // We have already received GOAWAY before
                    // In this case the smaller valid stream is used
                    _lastStreamId = Math.Min(_lastStreamId, lastValidStream);
                }

                if (NetEventSource.Log.IsEnabled()) Trace($"{nameof(lastValidStream)}={lastValidStream}, {nameof(_lastStreamId)}={_lastStreamId}");

                foreach (KeyValuePair<int, Http2Stream> kvp in _httpStreams)
                {
                    int streamId = kvp.Key;
                    Debug.Assert(streamId == kvp.Value.StreamId);

                    if (streamId > _lastStreamId)
                    {
                        streamsToAbort.Add(kvp.Value);
                    }
                    else
                    {
                        if (NetEventSource.Log.IsEnabled()) Trace($"Found {nameof(streamId)} {streamId} <= {_lastStreamId}.");
                    }
                }

                CheckForShutdown();
            }

            // Avoid calling OnAbort under the lock, as it may cause the Http2Stream
            // to call back in to RemoveStream
            foreach (Http2Stream s in streamsToAbort)
            {
                s.OnReset(abortException, canRetry: true);
            }
        }

        private void CheckForShutdown()
        {
            Debug.Assert(_disposed || _lastStreamId != -1);
            Debug.Assert(Monitor.IsEntered(SyncObject));

            // Check if dictionary has become empty
            if (_httpStreams.Count != 0)
            {
                return;
            }

            // Do shutdown.
            _connection.Dispose();

            _connectionWindow.Dispose();
            _concurrentStreams.Dispose();
        }

        public void Dispose()
        {
            lock (SyncObject)
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
            AltSvc = 10,

            Last = 10
        }

        private readonly struct FrameHeader
        {
            public readonly int PayloadLength;
            public readonly FrameType Type;
            public readonly FrameFlags Flags;
            public readonly int StreamId;

            public const int Size = 9;
            public const int MaxPayloadLength = 16384;

            public const int SettingLength = 6;            // per setting (total SETTINGS length must be a multiple of this)
            public const int PriorityInfoLength = 5;       // for both PRIORITY frame and priority info within HEADERS
            public const int PingLength = 8;
            public const int WindowUpdateLength = 4;
            public const int RstStreamLength = 4;
            public const int GoAwayMinLength = 8;

            public FrameHeader(int payloadLength, FrameType type, FrameFlags flags, int streamId)
            {
                Debug.Assert(streamId >= 0);

                PayloadLength = payloadLength;
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

                FrameFlags flags = (FrameFlags)buffer[4]; // do first to avoid some bounds checks
                int payloadLength = (buffer[0] << 16) | (buffer[1] << 8) | buffer[2];
                FrameType type = (FrameType)buffer[3];
                int streamId = (int)(BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(5)) & 0x7FFFFFFF);

                return new FrameHeader(payloadLength, type, flags, streamId);
            }

            public static void WriteTo(Span<byte> destination, int payloadLength, FrameType type, FrameFlags flags, int streamId)
            {
                Debug.Assert(destination.Length >= Size);
                Debug.Assert(type <= FrameType.Last);
                Debug.Assert((flags & FrameFlags.ValidBits) == flags);
                Debug.Assert((uint)payloadLength <= MaxPayloadLength);

                // This ordering helps eliminate bounds checks.
                BinaryPrimitives.WriteInt32BigEndian(destination.Slice(5), streamId);
                destination[4] = (byte)flags;
                destination[0] = (byte)((payloadLength & 0x00FF0000) >> 16);
                destination[1] = (byte)((payloadLength & 0x0000FF00) >> 8);
                destination[2] = (byte)(payloadLength & 0x000000FF);
                destination[3] = (byte)type;
            }

            public override string ToString() => $"StreamId={StreamId}; Type={Type}; Flags={Flags}; PayloadLength={PayloadLength}"; // Description for diagnostic purposes
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

        private enum SettingId : ushort
        {
            HeaderTableSize = 0x1,
            EnablePush = 0x2,
            MaxConcurrentStreams = 0x3,
            InitialWindowSize = 0x4,
            MaxFrameSize = 0x5,
            MaxHeaderListSize = 0x6
        }

        // Note that this is safe to be called concurrently by multiple threads.

        public sealed override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
        {
            if (NetEventSource.Log.IsEnabled()) Trace($"{request}");

            try
            {
                // Send request headers
                bool shouldExpectContinue = request.Content != null && request.HasHeaders && request.Headers.ExpectContinue == true;
                Http2Stream http2Stream = await SendHeadersAsync(request, cancellationToken, mustFlush: shouldExpectContinue).ConfigureAwait(false);

                bool duplex = request.Content != null && request.Content.AllowDuplex;

                // If we have duplex content, then don't propagate the cancellation to the request body task.
                // If cancellation occurs before we receive the response headers, then we will cancel the request body anyway.
                CancellationToken requestBodyCancellationToken = duplex ? CancellationToken.None : cancellationToken;

                // Start sending request body, if any.
                Task requestBodyTask = http2Stream.SendRequestBodyAsync(requestBodyCancellationToken);

                // Start receiving the response headers.
                Task responseHeadersTask = http2Stream.ReadResponseHeadersAsync(cancellationToken);

                // Wait for either task to complete.  The best and most common case is when the request body completes
                // before the response headers, in which case we can fully process the sending of the request and then
                // fully process the sending of the response.  WhenAny is not free, so we do a fast-path check to see
                // if the request body completed synchronously, only progressing to do the WhenAny if it didn't. Then
                // if the WhenAny completes and either the WhenAny indicated that the request body completed or
                // both tasks completed, we can proceed to handle the request body as if it completed first.  We also
                // check whether the request content even allows for duplex communication; if it doesn't (none of
                // our built-in content types do), then we can just proceed to wait for the request body content to
                // complete before worrying about response headers completing.
                if (requestBodyTask.IsCompleted ||
                    duplex == false ||
                    await Task.WhenAny(requestBodyTask, responseHeadersTask).ConfigureAwait(false) == requestBodyTask ||
                    requestBodyTask.IsCompleted ||
                    http2Stream.SendRequestFinished)
                {
                    // The sending of the request body completed before receiving all of the request headers (or we're
                    // ok waiting for the request body even if it hasn't completed, e.g. because we're not doing duplex).
                    // This is the common and desirable case.
                    try
                    {
                        await requestBodyTask.ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        if (NetEventSource.Log.IsEnabled()) Trace($"Sending request content failed: {e}");
                        LogExceptions(responseHeadersTask); // Observe exception (if any) on responseHeadersTask.
                        throw;
                    }
                }
                else
                {
                    // We received the response headers but the request body hasn't yet finished; this most commonly happens
                    // when the protocol is being used to enable duplex communication. If the connection is aborted or if we
                    // get RST or GOAWAY from server, exception will be stored in stream._abortException and propagated up
                    // to caller if possible while processing response, but make sure that we log any exceptions from this task
                    // completing asynchronously).
                    LogExceptions(requestBodyTask);
                }

                // Wait for the response headers to complete if they haven't already, propagating any exceptions.
                await responseHeadersTask.ConfigureAwait(false);

                return http2Stream.GetAndClearResponse();
            }
            catch (Exception e)
            {
                if (e is IOException ||
                    e is ObjectDisposedException ||
                    e is Http2ProtocolException ||
                    e is InvalidOperationException)
                {
                    throw new HttpRequestException(SR.net_http_client_execution_error, e);
                }

                throw;
            }
        }

        private void RemoveStream(Http2Stream http2Stream)
        {
            if (NetEventSource.Log.IsEnabled()) Trace(http2Stream.StreamId, "");
            Debug.Assert(http2Stream != null);

            lock (SyncObject)
            {
                if (!_httpStreams.Remove(http2Stream.StreamId))
                {
                    Debug.Fail($"Stream {http2Stream.StreamId} not found in dictionary during RemoveStream???");
                    return;
                }

                if (_httpStreams.Count == 0)
                {
                    // If this was last pending request, get timestamp so we can monitor idle time.
                    _idleSinceTickCount = Environment.TickCount64;

                    if (_disposed || _lastStreamId != -1)
                    {
                        CheckForShutdown();
                    }
                }
            }

            _concurrentStreams.AdjustCredit(1);
        }

        public sealed override string ToString() => $"{nameof(Http2Connection)}({_pool})"; // Description for diagnostic purposes

        public override void Trace(string message, [CallerMemberName] string? memberName = null) =>
            Trace(0, message, memberName);

        internal void Trace(int streamId, string message, [CallerMemberName] string? memberName = null) =>
            NetEventSource.Log.HandlerMessage(
                _pool?.GetHashCode() ?? 0,    // pool ID
                GetHashCode(),                // connection ID
                streamId,                     // stream ID
                memberName,                   // method name
                message);                     // message

        [DoesNotReturn]
        private static void ThrowRetry(string message, Exception innerException) =>
            throw new HttpRequestException(message, innerException, allowRetry: RequestRetryType.RetryOnSameOrNextProxy);

        private static Exception GetRequestAbortedException(Exception? innerException = null) =>
            new IOException(SR.net_http_request_aborted, innerException);

        [DoesNotReturn]
        private static void ThrowRequestAborted(Exception? innerException = null) =>
            throw GetRequestAbortedException(innerException);

        [DoesNotReturn]
        private static void ThrowProtocolError() =>
            ThrowProtocolError(Http2ProtocolErrorCode.ProtocolError);

        [DoesNotReturn]
        private static void ThrowProtocolError(Http2ProtocolErrorCode errorCode) =>
            throw new Http2ConnectionException(errorCode);
    }
}
