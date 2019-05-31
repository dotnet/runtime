// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http.HPack;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed partial class Http2Connection : HttpConnectionBase, IDisposable
    {
        private readonly HttpConnectionPool _pool;
        private readonly Stream _stream;

        // NOTE: These are mutable structs; do not make these readonly.
        private ArrayBuffer _incomingBuffer;
        private ArrayBuffer _outgoingBuffer;
        private ArrayBuffer _headerBuffer;

        private readonly HPackDecoder _hpackDecoder;

        private readonly Dictionary<int, Http2Stream> _httpStreams;

        private readonly SemaphoreSlim _writerLock;
        private readonly SemaphoreSlim _headerSerializationLock;

        private readonly CreditManager _connectionWindow;
        private readonly CreditManager _concurrentStreams;

        private int _nextStream;
        private bool _expectingSettingsAck;
        private int _initialWindowSize;
        private int _maxConcurrentStreams;
        private int _pendingWindowUpdate;
        private long _idleSinceTickCount;
        private int _pendingWriters;

        private bool _disposed;
        private Exception _abortException;

        // If an in-progress write is canceled we need to be able to immediately
        // report a cancellation to the user, but also block the connection until
        // the write completes. We avoid actually canceling the write, as we would
        // then have to close the whole connection.
        private Task _inProgressWrite = null;

        private const int MaxStreamId = int.MaxValue;

        private static readonly byte[] s_http2ConnectionPreface = Encoding.ASCII.GetBytes("PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n");

        private const int InitialConnectionBufferSize = 4096;

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

        public Http2Connection(HttpConnectionPool pool, Stream stream)
        {
            _pool = pool;
            _stream = stream;
            _incomingBuffer = new ArrayBuffer(InitialConnectionBufferSize);
            _outgoingBuffer = new ArrayBuffer(InitialConnectionBufferSize);
            _headerBuffer = new ArrayBuffer(InitialConnectionBufferSize);

            _hpackDecoder = new HPackDecoder();

            _httpStreams = new Dictionary<int, Http2Stream>();

            _writerLock = new SemaphoreSlim(1, 1);
            _headerSerializationLock = new SemaphoreSlim(1, 1);
            _connectionWindow = new CreditManager(DefaultInitialWindowSize);
            _concurrentStreams = new CreditManager(int.MaxValue);

            _nextStream = 1;
            _initialWindowSize = DefaultInitialWindowSize;
            _maxConcurrentStreams = int.MaxValue;
            _pendingWindowUpdate = 0;
        }

        private object SyncObject => _httpStreams;

        public async Task SetupAsync()
        {
            _outgoingBuffer.EnsureAvailableSpace(s_http2ConnectionPreface.Length +
                FrameHeader.Size + (FrameHeader.SettingLength * 2) +
                FrameHeader.Size + FrameHeader.WindowUpdateLength);

            // Send connection preface
            s_http2ConnectionPreface.AsSpan().CopyTo(_outgoingBuffer.AvailableSpan);
            _outgoingBuffer.Commit(s_http2ConnectionPreface.Length);

            // Send SETTINGS frame 
            WriteFrameHeader(new FrameHeader(FrameHeader.SettingLength * 2, FrameType.Settings, FrameFlags.None, 0));

            // First setting: Disable push promise
            BinaryPrimitives.WriteUInt16BigEndian(_outgoingBuffer.AvailableSpan, (ushort)SettingId.EnablePush);
            _outgoingBuffer.Commit(2);
            BinaryPrimitives.WriteUInt32BigEndian(_outgoingBuffer.AvailableSpan, 0);
            _outgoingBuffer.Commit(4);

            // Second setting: Set header table size to 0 to disable dynamic header compression
            BinaryPrimitives.WriteUInt16BigEndian(_outgoingBuffer.AvailableSpan, (ushort)SettingId.HeaderTableSize);
            _outgoingBuffer.Commit(2);
            BinaryPrimitives.WriteUInt32BigEndian(_outgoingBuffer.AvailableSpan, 0);
            _outgoingBuffer.Commit(4);

            // Send initial connection-level WINDOW_UPDATE
            WriteFrameHeader(new FrameHeader(FrameHeader.WindowUpdateLength, FrameType.WindowUpdate, FrameFlags.None, 0));
            BinaryPrimitives.WriteUInt32BigEndian(_outgoingBuffer.AvailableSpan, (ConnectionWindowSize - DefaultInitialWindowSize));
            _outgoingBuffer.Commit(4);

            await _stream.WriteAsync(_outgoingBuffer.ActiveMemory).ConfigureAwait(false);
            _outgoingBuffer.Discard(_outgoingBuffer.ActiveMemory.Length);

            _expectingSettingsAck = true;

            _ = ProcessIncomingFramesAsync();
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

            try
            {
                await _stream.WriteAsync(_outgoingBuffer.ActiveMemory).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Abort(e);
                throw;
            }
            finally
            {
                _outgoingBuffer.Discard(_outgoingBuffer.ActiveMemory.Length);
            }
        }

        private async ValueTask<FrameHeader> ReadFrameAsync(bool initialFrame = false)
        {
            // Read frame header
            await EnsureIncomingBytesAsync(FrameHeader.Size).ConfigureAwait(false);
            FrameHeader frameHeader = FrameHeader.ReadFrom(_incomingBuffer.ActiveSpan);

            if (frameHeader.Length > FrameHeader.MaxLength)
            {
                if (initialFrame && NetEventSource.IsEnabled)
                {
                    string response = System.Text.Encoding.ASCII.GetString(_incomingBuffer.ActiveSpan.Slice(0, Math.Min(20, _incomingBuffer.ActiveSpan.Length)));
                    Trace($"HTTP/2 handshake failed. Server returned {response}");
                }

                _incomingBuffer.Discard(FrameHeader.Size);
                throw new Http2ProtocolException(initialFrame ? Http2ProtocolErrorCode.ProtocolError : Http2ProtocolErrorCode.FrameSizeError);
            }
            _incomingBuffer.Discard(FrameHeader.Size);

            // Read frame contents
            await EnsureIncomingBytesAsync(frameHeader.Length).ConfigureAwait(false);

            return frameHeader;
        }

        private async Task ProcessIncomingFramesAsync()
        {
            try
            {
                FrameHeader frameHeader = await ReadFrameAsync(initialFrame: true).ConfigureAwait(false);
                if (frameHeader.Type != FrameType.Settings || frameHeader.AckFlag)
                {
                    throw new Http2ProtocolException(Http2ProtocolErrorCode.ProtocolError);
                }

                // Process the initial SETTINGS frame. This will send an ACK.
                ProcessSettingsFrame(frameHeader);

                // Keep processing frames as they arrive.
                while (true)
                {
                    frameHeader = await ReadFrameAsync().ConfigureAwait(false);

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

                        case FrameType.PushPromise:     // Should not happen, since we disable this in our initial SETTINGS (TODO: ISSUE 31295: We aren't currently, but we should)
                        case FrameType.Continuation:    // Should only be received while processing headers in ProcessHeadersFrame
                        default:
                            throw new Http2ProtocolException(Http2ProtocolErrorCode.ProtocolError);
                    }
                }
            }
            catch (Exception e)
            {
                if (NetEventSource.IsEnabled) Trace($"ProcessIncomingFramesAsync: {e.Message}");

                if (!_disposed)
                {
                    Abort(e);
                }
            }
        }

        // Note, this will return null for a streamId that's no longer in use.
        // Callers must check for this and send a RST_STREAM or ignore as appropriate.
        // If the streamId is invalid or the stream is idle, calling this function
        // will result in a connection level error.
        private Http2Stream GetStream(int streamId)
        {
            // TODO: ISSUE 34192: If we implement support for Push Promise, this will
            // need to be updated to track the highest stream ID used by the server in
            // addition to the highest ID used by the client.
            if (streamId <= 0 || streamId >= _nextStream)
            {
                throw new Http2ProtocolException(Http2ProtocolErrorCode.ProtocolError);
            }

            lock (SyncObject)
            {
                if (!_httpStreams.TryGetValue(streamId, out Http2Stream http2Stream))
                {
                    return null;
                }

                return http2Stream;
            }
        }

        private static readonly HPackDecoder.HeaderCallback s_http2StreamOnResponseHeader =
            (state, name, value) => ((Http2Stream)state).OnResponseHeader(name, value);

        private async Task ProcessHeadersFrame(FrameHeader frameHeader)
        {
            Debug.Assert(frameHeader.Type == FrameType.Headers);

            bool endStream = frameHeader.EndStreamFlag;

            int streamId = frameHeader.StreamId;
            Http2Stream http2Stream = GetStream(streamId);
            if (http2Stream == null)
            {
                _incomingBuffer.Discard(frameHeader.Length);

                throw new Http2ProtocolException(Http2ProtocolErrorCode.StreamClosed);
            }

            http2Stream.OnResponseHeadersStart();

            _hpackDecoder.Decode(
                GetFrameData(_incomingBuffer.ActiveSpan.Slice(0, frameHeader.Length), frameHeader.PaddedFlag, frameHeader.PriorityFlag),
                s_http2StreamOnResponseHeader,
                http2Stream);
            _incomingBuffer.Discard(frameHeader.Length);

            while (!frameHeader.EndHeadersFlag)
            {
                frameHeader = await ReadFrameAsync().ConfigureAwait(false);
                if (frameHeader.Type != FrameType.Continuation ||
                    frameHeader.StreamId != streamId)
                {
                    throw new Http2ProtocolException(Http2ProtocolErrorCode.ProtocolError);
                }

                _hpackDecoder.Decode(
                    _incomingBuffer.ActiveSpan.Slice(0, frameHeader.Length),
                    s_http2StreamOnResponseHeader,
                    http2Stream);
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

        private void ProcessDataFrame(FrameHeader frameHeader)
        {
            Debug.Assert(frameHeader.Type == FrameType.Data);

            Http2Stream http2Stream = GetStream(frameHeader.StreamId);
            if (http2Stream == null)
            {
                _incomingBuffer.Discard(frameHeader.Length);

                throw new Http2ProtocolException(Http2ProtocolErrorCode.StreamClosed);
            }

            ReadOnlySpan<byte> frameData = GetFrameData(_incomingBuffer.ActiveSpan.Slice(0, frameHeader.Length), hasPad: frameHeader.PaddedFlag, hasPriority: false);

            bool endStream = frameHeader.EndStreamFlag;

            http2Stream.OnResponseData(frameData, endStream);

            _incomingBuffer.Discard(frameHeader.Length);

            if (endStream)
            {
                RemoveStream(http2Stream);
            }
        }

        private void ProcessSettingsFrame(FrameHeader frameHeader)
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

                // Parse settings and process the ones we care about.
                ReadOnlySpan<byte> settings = _incomingBuffer.ActiveSpan.Slice(0, frameHeader.Length);
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
                                throw new Http2ProtocolException(Http2ProtocolErrorCode.FlowControlError);
                            }

                            ChangeInitialWindowSize((int)settingValue);
                            break;

                        case SettingId.MaxFrameSize:
                            if (settingValue < 16384 || settingValue > 16777215)
                            {
                                throw new Http2ProtocolException(Http2ProtocolErrorCode.ProtocolError);
                            }

                            // We don't actually store this value; we always send frames of the minimum size (16K).
                            break;

                        default:
                            // All others are ignored because we don't care about them.
                            // Note, per RFC, unknown settings IDs should be ignored.
                            break;
                    }
                }

                _incomingBuffer.Discard(frameHeader.Length);

                // Send acknowledgement
                // Don't wait for completion, which could happen asynchronously.
                Task ignored = SendSettingsAckAsync();
            }
        }

        private void ChangeMaxConcurrentStreams(uint newValue)
        {
            // The value is provided as a uint.
            // Limit this to int.MaxValue since the CreditManager implementation only supports singed values.
            // In practice, we should never reach this value.
            int effectiveValue = (newValue > (uint)int.MaxValue ? int.MaxValue : (int)newValue);
            int delta = effectiveValue - _maxConcurrentStreams;
            _maxConcurrentStreams = effectiveValue;

            _concurrentStreams.AdjustCredit(delta);
        }

        private void ChangeInitialWindowSize(int newSize)
        {
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

            if (frameHeader.StreamId == 0 || frameHeader.Length != FrameHeader.PriorityInfoLength)
            {
                throw new Http2ProtocolException(Http2ProtocolErrorCode.ProtocolError);
            }

            // Ignore priority info.

            _incomingBuffer.Discard(frameHeader.Length);
        }

        private void ProcessPingFrame(FrameHeader frameHeader)
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
            // Don't wait for completion, which could happen asynchronously.
            Task ignored = SendPingAckAsync(_incomingBuffer.ActiveMemory.Slice(0, FrameHeader.PingLength));

            _incomingBuffer.Discard(frameHeader.Length);
        }

        private void ProcessWindowUpdateFrame(FrameHeader frameHeader)
        {
            Debug.Assert(frameHeader.Type == FrameType.WindowUpdate);

            if (frameHeader.Length != FrameHeader.WindowUpdateLength)
            {
                throw new Http2ProtocolException(Http2ProtocolErrorCode.FrameSizeError);
            }

            int amount = BinaryPrimitives.ReadInt32BigEndian(_incomingBuffer.ActiveSpan) & 0x7FFFFFFF;

            Debug.Assert(amount >= 0);
            if (amount == 0)
            {
                throw new Http2ProtocolException(Http2ProtocolErrorCode.ProtocolError);
            }

            _incomingBuffer.Discard(frameHeader.Length);

            if (frameHeader.StreamId == 0)
            {
                _connectionWindow.AdjustCredit(amount);
            }
            else
            {
                Http2Stream http2Stream = GetStream(frameHeader.StreamId);
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

            var protocolError = (Http2ProtocolErrorCode)BinaryPrimitives.ReadInt32BigEndian(_incomingBuffer.ActiveSpan);

            _incomingBuffer.Discard(frameHeader.Length);

            http2Stream.OnResponseAbort(new Http2ProtocolException(protocolError));

            RemoveStream(http2Stream);
        }

        private void ProcessGoAwayFrame(FrameHeader frameHeader)
        {
            Debug.Assert(frameHeader.Type == FrameType.GoAway);

            if (frameHeader.Length < FrameHeader.GoAwayMinLength)
            {
                throw new Http2ProtocolException(Http2ProtocolErrorCode.FrameSizeError);
            }

            // GoAway frames always apply to the whole connection, never to a single stream.
            // According to RFC 7540 section 6.8, this should be a connection error.
            if (frameHeader.StreamId != 0)
            {
                throw new Http2ProtocolException(Http2ProtocolErrorCode.ProtocolError);
            }

            int lastValidStream = (int)(BinaryPrimitives.ReadUInt32BigEndian(_incomingBuffer.ActiveSpan) & 0x7FFFFFFF);
            var errorCode = (Http2ProtocolErrorCode)BinaryPrimitives.ReadInt32BigEndian(_incomingBuffer.ActiveSpan.Slice(sizeof(int)));

            AbortStreams(lastValidStream, new Http2ProtocolException(errorCode));

            _incomingBuffer.Discard(frameHeader.Length);
        }

        private async Task StartWriteAsync(int writeBytes, CancellationToken cancellationToken = default)
        {
            await AcquireWriteLockAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                // If there is a pending write that was canceled while in progress, wait for it to complete.
                if (_inProgressWrite != null)
                {
                    await _inProgressWrite.ConfigureAwait(false);
                    _inProgressWrite = null;
                }

                int totalBufferLength = _outgoingBuffer.Capacity;
                int activeBufferLength = _outgoingBuffer.ActiveSpan.Length;

                if (totalBufferLength >= UnflushedOutgoingBufferSize &&
                    writeBytes >= totalBufferLength - activeBufferLength &&
                    activeBufferLength > 0)
                {
                    // If the buffer has already grown to 32k, does not have room for the next request,
                    // and is non-empty, flush the current contents to the wire.
                    await FlushOutgoingBytesAsync().ConfigureAwait(false);
                }

                _outgoingBuffer.EnsureAvailableSpace(writeBytes);
            }
            catch
            {
                _writerLock.Release();
                throw;
            }
        }

        // This method handles flushing bytes to the wire. Writes here need to be atomic, so as to avoid
        // killing the whole connection. Callers must hold the write lock, but can specify whether or not
        // they want to release it.
        private void FinishWrite(bool mustFlush)
        {
            // We can't validate that we hold the semaphore, but we can at least validate that someone is
            // holding it.
            Debug.Assert(_writerLock.CurrentCount == 0);

            try
            {
                // We must flush if the caller requires it, or if there are no other pending writes.
                if (mustFlush || _pendingWriters == 0)
                {
                    Debug.Assert(_inProgressWrite == null);

                    _inProgressWrite = FlushOutgoingBytesAsync();
                }
            }
            finally
            {
                _writerLock.Release();
            }
        }

        private async Task AcquireWriteLockAsync(CancellationToken cancellationToken)
        {
            Task acquireLockTask = _writerLock.WaitAsync(cancellationToken);
            if (!acquireLockTask.IsCompletedSuccessfully)
            {
                Interlocked.Increment(ref _pendingWriters);
                try
                {
                    await acquireLockTask.ConfigureAwait(false);
                }
                finally
                {
                    Interlocked.Decrement(ref _pendingWriters);
                }
            }

            // If the connection has been aborted, then fail now instead of trying to send more data.
            if (_disposed)
            {
                throw new IOException(SR.net_http_request_aborted);
            }
        }

        private async Task SendSettingsAckAsync()
        {
            await StartWriteAsync(FrameHeader.Size).ConfigureAwait(false);
            WriteFrameHeader(new FrameHeader(0, FrameType.Settings, FrameFlags.Ack, 0));

            FinishWrite(mustFlush: true);
        }

        private async Task SendPingAckAsync(ReadOnlyMemory<byte> pingContent)
        {
            Debug.Assert(pingContent.Length == FrameHeader.PingLength);

            await StartWriteAsync(FrameHeader.Size + FrameHeader.PingLength).ConfigureAwait(false);
            WriteFrameHeader(new FrameHeader(FrameHeader.PingLength, FrameType.Ping, FrameFlags.Ack, 0));
            pingContent.CopyTo(_outgoingBuffer.AvailableMemory);
            _outgoingBuffer.Commit(FrameHeader.PingLength);

            FinishWrite(mustFlush: false);
        }

        private async Task SendRstStreamAsync(int streamId, Http2ProtocolErrorCode errorCode)
        {
            await StartWriteAsync(FrameHeader.Size + FrameHeader.RstStreamLength).ConfigureAwait(false);
            WriteFrameHeader(new FrameHeader(FrameHeader.RstStreamLength, FrameType.RstStream, FrameFlags.None, streamId));
            BinaryPrimitives.WriteInt32BigEndian(_outgoingBuffer.AvailableSpan, (int)errorCode);
            _outgoingBuffer.Commit(FrameHeader.RstStreamLength);

            FinishWrite(mustFlush: true);
        }

        private static (ReadOnlyMemory<byte> first, ReadOnlyMemory<byte> rest) SplitBuffer(ReadOnlyMemory<byte> buffer, int maxSize) =>
            buffer.Length > maxSize ?
                (buffer.Slice(0, maxSize), buffer.Slice(maxSize)) :
                (buffer, Memory<byte>.Empty);

        private void WriteIndexedHeader(int index)
        {
            int bytesWritten;
            while (!HPackEncoder.EncodeIndexedHeaderField(index, _headerBuffer.AvailableSpan, out bytesWritten))
            {
                _headerBuffer.EnsureAvailableSpace(_headerBuffer.AvailableLength + 1);
            }

            _headerBuffer.Commit(bytesWritten);
        }

        private void WriteIndexedHeader(int index, string value)
        {
            int bytesWritten;
            while (!HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexing(index, value, _headerBuffer.AvailableSpan, out bytesWritten))
            {
                _headerBuffer.EnsureAvailableSpace(_headerBuffer.AvailableLength + 1);
            }

            _headerBuffer.Commit(bytesWritten);
        }

        private void WriteLiteralHeader(string name, string[] values)
        {
            int bytesWritten;
            while (!HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexingNewName(name, values, HttpHeaderParser.DefaultSeparator, _headerBuffer.AvailableSpan, out bytesWritten))
            {
                _headerBuffer.EnsureAvailableSpace(_headerBuffer.AvailableLength + 1);
            }

            _headerBuffer.Commit(bytesWritten);
        }

        private void WriteLiteralHeaderValues(string[] values, string separator)
        {
            int bytesWritten;
            while (!HPackEncoder.EncodeStringLiterals(values, separator, _headerBuffer.AvailableSpan, out bytesWritten))
            {
                _headerBuffer.EnsureAvailableSpace(_headerBuffer.AvailableLength + 1);
            }

            _headerBuffer.Commit(bytesWritten);
        }

        private void WriteLiteralHeaderValue(string value)
        {
            int bytesWritten;
            while (!HPackEncoder.EncodeStringLiteral(value, _headerBuffer.AvailableSpan, out bytesWritten))
            {
                _headerBuffer.EnsureAvailableSpace(_headerBuffer.AvailableLength + 1);
            }

            _headerBuffer.Commit(bytesWritten);
        }

        private void WriteBytes(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length > _headerBuffer.AvailableLength)
            {
                _headerBuffer.EnsureAvailableSpace(bytes.Length);
            }

            bytes.CopyTo(_headerBuffer.AvailableSpan);
            _headerBuffer.Commit(bytes.Length);
        }

        private void WriteHeaderCollection(HttpHeaders headers)
        {
            foreach (KeyValuePair<HeaderDescriptor, string[]> header in headers.GetHeaderDescriptorsAndValues())
            {
                Debug.Assert(header.Value.Length > 0, "No values for header??");

                KnownHeader knownHeader = header.Key.KnownHeader;
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
                            foreach (string value in header.Value)
                            {
                                if (string.Equals(value, "trailers", StringComparison.OrdinalIgnoreCase))
                                {
                                    WriteBytes(knownHeader.Http2EncodedName);
                                    WriteLiteralHeaderValue(value);
                                    break;
                                }
                            }
                            continue;
                        }

                        // For all other known headers, send them via their pre-encoded name and the associated value.
                        WriteBytes(knownHeader.Http2EncodedName);
                        string separator = null;
                        if (header.Value.Length > 1)
                        {
                            HttpHeaderParser parser = header.Key.Parser;
                            if (parser != null && parser.SupportsMultipleValues)
                            {
                                separator = parser.Separator;
                            }
                            else
                            {
                                separator = HttpHeaderParser.DefaultSeparator;
                            }
                        }

                        WriteLiteralHeaderValues(header.Value, separator);
                    }
                }
                else
                {
                    // The header is not known: fall back to just encoding the header name and value(s).
                    WriteLiteralHeader(header.Key.Name, header.Value);
                }
            }
        }

        private void WriteHeaders(HttpRequestMessage request)
        {
            Debug.Assert(_headerBuffer.ActiveMemory.Length == 0);

            // HTTP2 does not support Transfer-Encoding: chunked, so disable this on the request.
            request.Headers.TransferEncodingChunked = false;

            HttpMethod normalizedMethod = HttpMethod.Normalize(request.Method);

            // Method is normalized so we can do reference equality here.
            if (ReferenceEquals(normalizedMethod, HttpMethod.Get))
            {
                WriteIndexedHeader(StaticTable.MethodGet);
            }
            else if (ReferenceEquals(normalizedMethod, HttpMethod.Post))
            {
                WriteIndexedHeader(StaticTable.MethodPost);
            }
            else
            {
                WriteIndexedHeader(StaticTable.MethodGet, normalizedMethod.Method);
            }

            WriteIndexedHeader(_stream is SslStream ? StaticTable.SchemeHttps : StaticTable.SchemeHttp);

            if (request.HasHeaders && request.Headers.Host != null)
            {
                WriteIndexedHeader(StaticTable.Authority, request.Headers.Host);
            }
            else
            {
                WriteBytes(_pool._encodedAuthorityHostHeader);
            }

            string pathAndQuery = request.RequestUri.PathAndQuery;
            if (pathAndQuery == "/")
            {
                WriteIndexedHeader(StaticTable.PathSlash);
            }
            else
            {
                WriteIndexedHeader(StaticTable.PathSlash, pathAndQuery);
            }

            if (request.HasHeaders)
            {
                WriteHeaderCollection(request.Headers);
            }

            // Determine cookies to send.
            if (_pool.Settings._useCookies)
            {
                string cookiesFromContainer = _pool.Settings._cookieContainer.GetCookieHeader(request.RequestUri);
                if (cookiesFromContainer != string.Empty)
                {
                    WriteBytes(KnownHeaders.Cookie.Http2EncodedName);
                    WriteLiteralHeaderValue(cookiesFromContainer);
                }
            }

            if (request.Content == null)
            {
                // Write out Content-Length: 0 header to indicate no body,
                // unless this is a method that never has a body.
                if (normalizedMethod.MustHaveRequestBody)
                {
                    WriteBytes(KnownHeaders.ContentLength.Http2EncodedName);
                    WriteLiteralHeaderValue("0");
                }
            }
            else
            {
                WriteHeaderCollection(request.Content.Headers);
            }
        }

        private async ValueTask<Http2Stream> SendHeadersAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Ensure we don't exceed the max concurrent streams setting.
            await _concurrentStreams.RequestCreditAsync(1, cancellationToken).ConfigureAwait(false);

            // We serialize usage of the header encoder and the header buffer separately from the
            // write lock
            await _headerSerializationLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            Http2Stream http2Stream = null;

            try
            {
                http2Stream = AddStream(request);
                int streamId = http2Stream.StreamId;

                // Generate the entire header block, without framing, into the connection header buffer.
                WriteHeaders(request);

                ReadOnlyMemory<byte> remaining = _headerBuffer.ActiveMemory;
                Debug.Assert(remaining.Length > 0);

                // Calculate the total number of bytes we're going to use (content + headers).
                int totalSize = remaining.Length + (remaining.Length / FrameHeader.MaxLength) * FrameHeader.Size +
                                (remaining.Length % FrameHeader.MaxLength == 0 ? FrameHeader.Size : 0);

                // Split into frames and send.
                ReadOnlyMemory<byte> current;
                (current, remaining) = SplitBuffer(remaining, FrameHeader.MaxLength);

                FrameFlags flags =
                    (remaining.Length == 0 ? FrameFlags.EndHeaders : FrameFlags.None) |
                    (request.Content == null ? FrameFlags.EndStream : FrameFlags.None);

                // Note, HEADERS and CONTINUATION frames must be together, so hold the writer lock across sending all of them.
                await StartWriteAsync(totalSize).ConfigureAwait(false);

                WriteFrameHeader(new FrameHeader(current.Length, FrameType.Headers, flags, streamId));
                current.CopyTo(_outgoingBuffer.AvailableMemory);
                _outgoingBuffer.Commit(current.Length);

                while (remaining.Length > 0)
                {
                    (current, remaining) = SplitBuffer(remaining, FrameHeader.MaxLength);

                    flags = (remaining.Length == 0 ? FrameFlags.EndHeaders : FrameFlags.None);

                    WriteFrameHeader(new FrameHeader(current.Length, FrameType.Continuation, flags, streamId));
                    current.CopyTo(_outgoingBuffer.AvailableMemory);
                    _outgoingBuffer.Commit(current.Length);
                }

                // If this is not the end of the stream, we can put off flushing the buffer
                // since we know that there are going to be data frames following.
                FinishWrite(mustFlush: (flags & FrameFlags.EndStream) != 0);
            }
            catch
            {
                if (http2Stream != null)
                {
                    RemoveStream(http2Stream);
                    http2Stream.Dispose();
                }
                throw;
            }
            finally
            {
                _headerBuffer.Discard(_headerBuffer.ActiveMemory.Length);
                _headerSerializationLock.Release();
            }

            return http2Stream;
        }

        private async Task SendStreamDataAsync(int streamId, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            ReadOnlyMemory<byte> remaining = buffer;

            while (remaining.Length > 0)
            {
                int frameSize = Math.Min(remaining.Length, FrameHeader.MaxLength);

                // Once credit had been granted, we want to actually consume those bytes.
                frameSize = await _connectionWindow.RequestCreditAsync(frameSize, cancellationToken).ConfigureAwait(false);

                ReadOnlyMemory<byte> current;
                (current, remaining) = SplitBuffer(remaining, frameSize);

                // It's possible that a cancellation will occur while we wait for the write lock. In that case, we need to
                // return the credit that we have acquired and don't plan to use.
                try
                {
                    await StartWriteAsync(FrameHeader.Size + current.Length, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    _connectionWindow.AdjustCredit(frameSize);
                    throw;
                }

                WriteFrameHeader(new FrameHeader(current.Length, FrameType.Data, FrameFlags.None, streamId));
                current.CopyTo(_outgoingBuffer.AvailableMemory);
                _outgoingBuffer.Commit(current.Length);

                FinishWrite(mustFlush: false);
            }
        }

        private async Task SendEndStreamAsync(int streamId)
        {
            await StartWriteAsync(FrameHeader.Size).ConfigureAwait(false);

            WriteFrameHeader(new FrameHeader(0, FrameType.Data, FrameFlags.EndStream, streamId));

            FinishWrite(mustFlush: true);
        }

        private async Task SendWindowUpdateAsync(int streamId, int amount)
        {
            Debug.Assert(amount > 0);

            // We update both the connection-level and stream-level windows at the same time
            await StartWriteAsync(FrameHeader.Size + FrameHeader.WindowUpdateLength).ConfigureAwait(false);

            WriteFrameHeader(new FrameHeader(FrameHeader.WindowUpdateLength, FrameType.WindowUpdate, FrameFlags.None, streamId));
            BinaryPrimitives.WriteInt32BigEndian(_outgoingBuffer.AvailableSpan, amount);
            _outgoingBuffer.Commit(FrameHeader.WindowUpdateLength);

            FinishWrite(mustFlush: true);
        }

        private void ExtendWindow(int amount)
        {
            Debug.Assert(amount > 0);

            int windowUpdateSize;
            lock (SyncObject)
            {
                Debug.Assert(_pendingWindowUpdate < ConnectionWindowThreshold);

                _pendingWindowUpdate += amount;
                if (_pendingWindowUpdate < ConnectionWindowThreshold)
                {
                    return;
                }

                windowUpdateSize = _pendingWindowUpdate;
                _pendingWindowUpdate = 0;
            }

            Task ignored = SendWindowUpdateAsync(0, windowUpdateSize);
        }

        private void WriteFrameHeader(FrameHeader frameHeader)
        {
            Debug.Assert(_outgoingBuffer.AvailableMemory.Length >= FrameHeader.Size);

            frameHeader.WriteTo(_outgoingBuffer.AvailableSpan);
            _outgoingBuffer.Commit(FrameHeader.Size);
        }

         /// <summary>Abort all streams and cause further processing to fail.</summary>
         /// <param name="abortException">Exception causing Abort to be called.</param>
        private void Abort(Exception abortException)
        {
            // The connection has failed, e.g. failed IO or a connection-level frame error.
            if (_abortException == null)
            {
                _abortException = abortException;
            }
            AbortStreams(0, abortException);
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
                if (NetEventSource.IsEnabled) Trace($"Connection no longer usable. Idle {TimeSpan.FromMilliseconds((nowTicks - _idleSinceTickCount))} > {connectionIdleTimeout}.");

                return true;
            }

            return LifetimeExpired(nowTicks, connectionLifetime);
        }

        private void AbortStreams(int lastValidStream, Exception abortException)
        {
            lock (SyncObject)
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
                        kvp.Value.OnResponseAbort(abortException);

                        _httpStreams.Remove(kvp.Value.StreamId);
                    }
                }

                CheckForShutdown();
            }
        }

        private void CheckForShutdown()
        {
            Debug.Assert(_disposed);
            Debug.Assert(Monitor.IsEntered(SyncObject));

            // Check if dictionary has become empty
            if (_httpStreams.Count != 0)
            {
                return;
            }

            // Do shutdown.
            _stream.Close();

            _connectionWindow.Dispose();
            _concurrentStreams.Dispose();

            // ISSUE #35466
            // We can't dispose the writer lock here, because there's a timing issue where this can occur 
            // before a writer that has completed writing has actually release the lock.
            // It's not clear that we actually need to dispose this object, since it shouldn't
            // actually hold any unmanaged resources. However, we should ensure we have a clear understanding
            // of shutdown semantics and object lifetimes in general, even if we don't actually dispose this.
            // _writerLock.Dispose();
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

            public const int SettingLength = 6;            // per setting (total SETTINGS length must be a multiple of this)
            public const int PriorityInfoLength = 5;       // for both PRIORITY frame and priority info within HEADERS
            public const int PingLength = 8;
            public const int WindowUpdateLength = 4;
            public const int RstStreamLength = 4;
            public const int GoAwayMinLength = 8;

            public FrameHeader(int length, FrameType type, FrameFlags flags, int streamId)
            {
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
                Debug.Assert(Length <= MaxLength);

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

        public sealed override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Http2Stream http2Stream = null;
            try
            {
                // Send headers
                http2Stream = await SendHeadersAsync(request, cancellationToken).ConfigureAwait(false);

                // Send request body, if any
                await http2Stream.SendRequestBodyAsync(cancellationToken).ConfigureAwait(false);

                // Wait for response headers to be read.
                await http2Stream.ReadResponseHeadersAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Exception replacementException = null;

                if (e is IOException ||
                    e is ObjectDisposedException ||
                    e is Http2ProtocolException)
                {
                    replacementException = new HttpRequestException(SR.net_http_client_execution_error, _abortException ?? e);
                }
                else if (e is OperationCanceledException oce)
                {
                    // If the operation has been canceled after the stream was allocated an ID, send a RST_STREAM.
                    if (http2Stream != null && http2Stream.StreamId != 0)
                    {
                        http2Stream.Cancel();
                    }

                    if (oce.CancellationToken != cancellationToken)
                    {
                        replacementException = new OperationCanceledException(oce.Message, oce, cancellationToken);
                    }
                }

                http2Stream?.Dispose();

                if (replacementException != null)
                {
                    throw replacementException;
                }
                throw;
            }

            return http2Stream.Response;
        }

        private Http2Stream AddStream(HttpRequestMessage request)
        {
            lock (SyncObject)
            {
                if (_nextStream == MaxStreamId || _disposed)
                {
                    if (_abortException != null)
                    {
                        // Aborted because protocol error or IO.
                        throw new ObjectDisposedException(nameof(Http2Connection));
                    }

                    // We run out of IDs or we have race condition between receiving GOAWAY and processing requests.
                    // Throw a retryable request exception. This will cause retry logic to kick in
                    // and perform another connection attempt. The user should never see this exception.
                    throw new HttpRequestException(null, null, allowRetry: true);
                }

                int streamId = _nextStream;

                // Client-initiated streams are always odd-numbered, so increase by 2.
                _nextStream += 2;

                Http2Stream http2Stream = new Http2Stream(request, this, streamId, _initialWindowSize);

                _httpStreams.Add(streamId, http2Stream);

                return http2Stream;
            }
        }

        private void RemoveStream(Http2Stream http2Stream)
        {
            lock (SyncObject)
            {
                if (!_httpStreams.Remove(http2Stream.StreamId, out Http2Stream removed))
                {
                    // Stream can be removed from background ProcessIncomingFramesAsync() when endOfStream is set
                    // or when we hit various error conditions in SendAsync() call.
                    // Skip logic below if stream was already removed.
                    return;
                }

                _concurrentStreams.AdjustCredit(1);

                Debug.Assert(removed == http2Stream, "_httpStreams.TryRemove returned unexpected stream");

                if (_httpStreams.Count == 0)
                {
                    // If this was last pending request, get timestamp so we can monitor idle time.
                    _idleSinceTickCount = Environment.TickCount64;
                }

                if (_disposed)
                {
                    CheckForShutdown();
                }
            }
        }

        private static async ValueTask<int> ReadAtLeastAsync(Stream stream, Memory<byte> buffer, int minReadBytes)
        {
            Debug.Assert(buffer.Length >= minReadBytes);

            int totalBytesRead = 0;
            while (totalBytesRead < minReadBytes)
            {
                int bytesRead = await stream.ReadAsync(buffer.Slice(totalBytesRead)).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    throw new IOException(SR.Format(SR.net_http_invalid_response_premature_eof_bytecount, minReadBytes));
                }

                totalBytesRead += bytesRead;
            }

            return totalBytesRead;
        }

        public sealed override string ToString() => $"{nameof(Http2Connection)}({_pool})"; // Description for diagnostic purposes

        internal override void Trace(string message, [CallerMemberName] string memberName = null) =>
            NetEventSource.Log.HandlerMessage(
                _pool?.GetHashCode() ?? 0,    // pool ID
                GetHashCode(),                // connection ID
                _stream?.GetHashCode() ?? 0,  // stream ID
                memberName,                   // method name
                ToString() + ": " + message); // message

    }
}
