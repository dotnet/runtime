// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed partial class Http2Connection
    {
        /// <summary>
        /// Responsible for serializing HTTP/2 frames to the wire and managing the connection window.
        /// </summary>
        private sealed class Http2FrameWriter
        {
            // When buffering outgoing writes, we will automatically buffer up to this number of bytes.
            // Single writes that are larger than the buffer can cause the buffer to expand beyond
            // this value, so this is not a hard maximum size.
            private const int UnflushedOutgoingBufferSize = 32 * 1024;
            private const int RentedOutgoingBufferSize = UnflushedOutgoingBufferSize * 2;

            private static readonly UnboundedChannelOptions s_channelOptions = new() { SingleReader = true };

            private readonly Http2Connection _parent;
            private ArrayBuffer _outgoingBuffer = new(initialSize: 0, usePool: true);
            private int _shouldFlush;
            private int _flushCounter;

            // Fire and forget frames are very small (dozen or so bytes).
            // This buffer will be regularly cleared and is therefore very likely to stay small.
            // As it won't have a meaningful impact on the memory footprint of a connection,
            // we save some cycles by not renting and returning it.
            // This is currently used for sending WINDOW_UPDATE, PING, RST_STREAM, DATA with EndStream, and SETTINGS ack frames.
            private ArrayBuffer _fireAndForgetBuffer = new(initialSize: 32, usePool: false);

            // We use null as a sentinel value to wake up the writer loop. It means that either:
            // - there is data available in _fireAndForgetBuffer,
            // - a connection window update was received after we were at 0, or
            // - we should flush the outgoing buffer.
            // A non-null value is a Http2StreamWriteAwaitable representing a write of stream data or headers.
            private readonly Channel<Http2StreamWriteAwaitable?> _channel = Channel.CreateUnbounded<Http2StreamWriteAwaitable?>(s_channelOptions);

            private int _connectionWindow;

            // If we run out of connection window, we'll queue stream data writes here to wait until a new window update is received.
            private readonly Queue<Http2StreamWriteAwaitable> _streamsWaitingForMoreConnectionWindow = new();

            private object FireAndForgetLock => _streamsWaitingForMoreConnectionWindow;

            public Http2FrameWriter(Http2Connection parent, int initialConnectionWindowSize)
            {
                _parent = parent;
                _connectionWindow = initialConnectionWindowSize;
            }

            public void StartWriteLoop()
            {
                using (ExecutionContext.SuppressFlow())
                {
                    Task.Run(ProcessOutgoingFramesAsync);
                }
            }

            public void Complete()
            {
                bool success = _channel.Writer.TryComplete();
                Debug.Assert(success);
            }

            public void AddConnectionWindow(int amount)
            {
                Debug.Assert(amount > 0);

                int newValue = Interlocked.Add(ref _connectionWindow, amount);
                if (newValue < 0)
                {
                    // Server sent a window update that overflowed the current window size.
                    ThrowProtocolError();
                }

                if (newValue == amount)
                {
                    // The previous window was 0.
                    // Wake up the writer loop now that we have some connection window available.
                    WakeUpWriterLoop();
                }
            }

            private void WakeUpWriterLoop() =>
                _channel.Writer.TryWrite(null);

            private async Task ProcessOutgoingFramesAsync()
            {
                try
                {
                    while (await _channel.Reader.WaitToReadAsync().ConfigureAwait(false))
                    {
                        // We rent a larger buffer to avoid resizing too often for many small writes.
                        _outgoingBuffer.EnsureAvailableSpace(RentedOutgoingBufferSize);

                        while (true)
                        {
                            if (_channel.Reader.TryRead(out Http2StreamWriteAwaitable? stream))
                            {
                                if (stream is null)
                                {
                                    // A null stream is a sentinel value meaning either:
                                    // - there is data available in _fireAndForgetBuffer,
                                    // - a connection window update was received after we were at 0, or
                                    // - we should flush the outgoing buffer.
                                    CopyFireAndForgetFramesToOutgoingBuffer();

                                    // If we now have connection window available, the next loop iterations will process any pending writes.
                                    continue;
                                }
                            }
                            // If we have any connection window left, we should check for any pending writes.
                            else if (Volatile.Read(ref _connectionWindow) == 0 || !_streamsWaitingForMoreConnectionWindow.TryDequeue(out stream))
                            {
                                break;
                            }

                            // Flush the buffer if we've accumulated enough data.
                            // Do this before disabling the cancellation on the writer in case the write takes a while.
                            if (_outgoingBuffer.ActiveLength > UnflushedOutgoingBufferSize)
                            {
                                _flushCounter++;
                                try
                                {
                                    if (NetEventSource.Log.IsEnabled()) LogFlushingBuffer();

                                    await _parent._stream.WriteAsync(_outgoingBuffer.ActiveMemory, CancellationToken.None).ConfigureAwait(false);
                                }
                                catch (Exception ex)
                                {
                                    _parent.Abort(ex);
                                }

                                _outgoingBuffer.Discard(_outgoingBuffer.ActiveLength);
                                _shouldFlush = 0;
                            }

                            if (!stream.TryDisableCancellation())
                            {
                                continue;
                            }

                            if (stream.WritingHeaders)
                            {
                                WriteHeadersCore(stream);
                            }
                            else
                            {
                                WriteStreamDataCore(stream);
                            }
                        }

                        // Nothing left in the queue to process. Flush the buffer if needed.
                        if (Volatile.Read(ref _shouldFlush) != 0 && _outgoingBuffer.ActiveLength > 0)
                        {
                            _flushCounter++;
                            try
                            {
                                if (NetEventSource.Log.IsEnabled()) LogFlushingBuffer();

                                await _parent._stream.WriteAsync(_outgoingBuffer.ActiveMemory, CancellationToken.None).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                _parent.Abort(ex);
                            }

                            _outgoingBuffer.Discard(_outgoingBuffer.ActiveLength);
                        }

                        _shouldFlush = 0;

                        if (_outgoingBuffer.ActiveLength == 0)
                        {
                            // Return the buffer to the pool if it's empty as the connection may stay idle for a while.
                            _outgoingBuffer.ClearAndReturnBuffer();
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (NetEventSource.Log.IsEnabled()) LogUnexpectedException(ex);

                    Debug.Fail($"Unexpected exception in {nameof(ProcessOutgoingFramesAsync)}: {ex}");
                }
                finally
                {
                    _outgoingBuffer.Dispose();
                }

                void LogFlushingBuffer() =>
                    _parent.Trace($"Flushing {_outgoingBuffer.ActiveLength} bytes. {nameof(_shouldFlush)}={_shouldFlush}, {nameof(_flushCounter)}={_flushCounter}, {nameof(_connectionWindow)}={_connectionWindow}");

                void LogUnexpectedException(Exception ex) =>
                    _parent.Trace($"Unexpected exception in {nameof(ProcessOutgoingFramesAsync)}: {ex}");
            }

            public void ScheduleStreamWrite(Http2StreamWriteAwaitable stream)
            {
                if (!_channel.Writer.TryWrite(stream))
                {
                    HandleConnectionShutdown(stream);
                }

                void HandleConnectionShutdown(Http2StreamWriteAwaitable stream)
                {
                    if (!stream.TryDisableCancellation())
                    {
                        return;
                    }

                    if (_parent._abortException is not null)
                    {
                        stream.SetException(_parent._abortException);
                        return;
                    }

                    // We must be trying to send something asynchronously and it has raced with the connection tear down.
                    // As such, it should not matter that we were not able to actually send the frame.
                    // But just in case, throw ObjectDisposedException. Asynchronous callers will ignore the failure.
                    Debug.Assert(_parent._shutdown && _parent._streamsInUse == 0);
                    stream.SetException(new ObjectDisposedException(nameof(Http2Connection)));
                }
            }

            public void QueueStreamDataFlushIfNeeded(Http2StreamWriteAwaitable stream)
            {
                // Avoid scheduling a flush if one had already been scheduled since the last write on this stream.
                if (_flushCounter == stream.FlushCounterAtLastDataWrite &&
                    Interlocked.Exchange(ref _shouldFlush, 1) == 0)
                {
                    WakeUpWriterLoop();
                }
            }

            private void CopyFireAndForgetFramesToOutgoingBuffer()
            {
                lock (FireAndForgetLock)
                {
                    if (_fireAndForgetBuffer.ActiveLength == 0)
                    {
                        // Nothing to copy.
                        return;
                    }

                    if (NetEventSource.Log.IsEnabled()) _parent.Trace($"Copying {_fireAndForgetBuffer.ActiveLength} fire-and-forget bytes");

                    ReadOnlySpan<byte> bytes = _fireAndForgetBuffer.ActiveSpan;

                    _outgoingBuffer.EnsureAvailableSpace(bytes.Length);
                    bytes.CopyTo(_outgoingBuffer.AvailableSpan);
                    _outgoingBuffer.Commit(bytes.Length);

                    _fireAndForgetBuffer.Discard(bytes.Length);
                }

                // All fire and forget frames should be flushed immediately.
                _shouldFlush = 1;
                _flushCounter++;
            }

            private void WriteHeadersCore(Http2StreamWriteAwaitable stream)
            {
                Debug.Assert(stream.WritingHeaders);
                Debug.Assert(!stream.DataRemaining.IsEmpty);

                try
                {
                    _parent.AddStream(stream.Stream);

                    ReadOnlySpan<byte> headerBytes = stream.DataRemaining.Span;

                    if (NetEventSource.Log.IsEnabled()) _parent.Trace(stream.Stream.StreamId, $"Started writing. Total header bytes={headerBytes.Length}");

                    // Calculate the total number of bytes we're going to use (content + headers).
                    int frameCount = ((headerBytes.Length - 1) / FrameHeader.MaxPayloadLength) + 1;
                    int totalSize = headerBytes.Length + (frameCount * FrameHeader.Size);

                    _outgoingBuffer.EnsureAvailableSpace(totalSize);

                    Span<byte> output = _outgoingBuffer.AvailableSpan;

                    // Copy the HEADERS frame.
                    ReadOnlySpan<byte> current = headerBytes.Slice(0, Math.Min(headerBytes.Length, FrameHeader.MaxPayloadLength));
                    headerBytes = headerBytes.Slice(current.Length);
                    FrameFlags flags = headerBytes.IsEmpty ? FrameFlags.EndHeaders : FrameFlags.None;

                    HttpRequestMessage request = stream.Stream.Request;

                    if (request.Content is null && !request.IsExtendedConnectRequest)
                    {
                        flags |= FrameFlags.EndStream;
                        _shouldFlush = 1;
                    }
                    else if (stream.Stream.ExpectContinue || request.IsExtendedConnectRequest)
                    {
                        _shouldFlush = 1;
                    }

                    FrameHeader.WriteTo(output, current.Length, FrameType.Headers, flags, stream.Stream.StreamId);
                    output = output.Slice(FrameHeader.Size);
                    current.CopyTo(output);
                    output = output.Slice(current.Length);

                    if (NetEventSource.Log.IsEnabled()) _parent.Trace(stream.Stream.StreamId, $"Wrote HEADERS frame. Length={current.Length}, flags={flags}");

                    // Copy CONTINUATION frames, if any.
                    while (!headerBytes.IsEmpty)
                    {
                        current = headerBytes.Slice(0, Math.Min(headerBytes.Length, FrameHeader.MaxPayloadLength));
                        headerBytes = headerBytes.Slice(current.Length);

                        flags = headerBytes.IsEmpty ? FrameFlags.EndHeaders : FrameFlags.None;

                        FrameHeader.WriteTo(output, current.Length, FrameType.Continuation, flags, stream.Stream.StreamId);
                        output = output.Slice(FrameHeader.Size);
                        current.CopyTo(output);
                        output = output.Slice(current.Length);

                        if (NetEventSource.Log.IsEnabled()) _parent.Trace(stream.Stream.StreamId, $"Wrote CONTINUATION frame. Length={current.Length}, flags={flags}");
                    }

                    Debug.Assert(headerBytes.IsEmpty);
                    _outgoingBuffer.Commit(totalSize);

                    Debug.Assert(!stream.ShouldFlushAfterData);
                    stream.FlushCounterAtLastDataWrite = _flushCounter;

                    // We're making forward progress. If a flush is already scheduled, we can pretend we've
                    // already flushed to prevent the stream from wasting time trying to schedule another flush later.
                    _flushCounter += _shouldFlush;

                    stream.SetResult();
                }
                catch (Exception ex)
                {
                    stream.SetException(ex);
                }
            }

            private void WriteStreamDataCore(Http2StreamWriteAwaitable stream)
            {
                if (_parent._abortException is not null)
                {
                    stream.SetException(_parent._abortException);
                    return;
                }

                Debug.Assert(!stream.DataRemaining.IsEmpty);

                // The available connection window may only be reduced by a single thread (the current one).
                // It is okay to read a stale value here, as the real amount is always >= what we read.
                if (_connectionWindow != 0)
                {
                    int toWrite = Math.Min(_connectionWindow, stream.DataRemaining.Length);
                    Debug.Assert(toWrite > 0);

                    if (_streamsWaitingForMoreConnectionWindow.Count != 0)
                    {
                        // We have streams waiting for more connection window.
                        // Avoid consuming all of it on a single stream, give others a chance to make progress.
                        toWrite = Math.Min(toWrite, FrameHeader.MaxPayloadLength);
                    }

                    int connectionWindowRemaining = Interlocked.Add(ref _connectionWindow, -toWrite);
                    Debug.Assert(connectionWindowRemaining >= 0);

                    if (NetEventSource.Log.IsEnabled()) _parent.Trace(stream.Stream.StreamId, $"Writing {toWrite} DATA bytes. {nameof(connectionWindowRemaining)}={connectionWindowRemaining}");

                    ReadOnlySpan<byte> dataLeftToWrite = stream.DataRemaining.Span.Slice(0, toWrite);
                    stream.DataRemaining = stream.DataRemaining.Slice(toWrite);

                    int frameCount = (int)((uint)(dataLeftToWrite.Length - 1) / FrameHeader.MaxPayloadLength) + 1;
                    int totalSize = dataLeftToWrite.Length + (frameCount * FrameHeader.Size);

                    _outgoingBuffer.EnsureAvailableSpace(totalSize);

                    do
                    {
                        ReadOnlySpan<byte> chunk = dataLeftToWrite.Slice(0, Math.Min(dataLeftToWrite.Length, FrameHeader.MaxPayloadLength));
                        dataLeftToWrite = dataLeftToWrite.Slice(chunk.Length);

                        FrameHeader.WriteTo(_outgoingBuffer.AvailableSpan, chunk.Length, FrameType.Data, FrameFlags.None, stream.Stream.StreamId);
                        _outgoingBuffer.Commit(FrameHeader.Size);

                        chunk.CopyTo(_outgoingBuffer.AvailableSpan);
                        _outgoingBuffer.Commit(chunk.Length);
                    }
                    while (!dataLeftToWrite.IsEmpty);

                    if (stream.DataRemaining.IsEmpty)
                    {
                        // We were able to send the last of the stream data.
                        if (stream.ShouldFlushAfterData)
                        {
                            _shouldFlush = 1;
                        }
                        stream.FlushCounterAtLastDataWrite = _flushCounter;

                        // We're making forward progress. If a flush is already scheduled, we can pretend we've
                        // already flushed to prevent the stream from wasting time trying to schedule another flush later.
                        _flushCounter += _shouldFlush;

                        stream.SetResult();
                        return;
                    }
                }

                // There's still more data to send, but we've exhausted the connection window.
                // We'll need to wait for a window update before we can send more.
                _shouldFlush = 1;

                // Until then the stream should still observe cancellation attempts.
                if (!stream.TryReRegisterForCancellation())
                {
                    // The write was cancelled.
                    return;
                }

                _streamsWaitingForMoreConnectionWindow.Enqueue(stream);
            }

            public void SendWindowUpdate(int streamId, int amount)
            {
                if (NetEventSource.Log.IsEnabled()) _parent.Trace(streamId, $"{nameof(amount)}={amount}");

                Span<byte> frame = stackalloc byte[FrameHeader.Size + FrameHeader.WindowUpdateLength];

                FrameHeader.WriteTo(frame, FrameHeader.WindowUpdateLength, FrameType.WindowUpdate, FrameFlags.None, streamId);
                BinaryPrimitives.WriteInt32BigEndian(frame.Slice(FrameHeader.Size), amount);

                WriteFireAndForgetFrame(frame);
            }

            public void SendEndStream(int streamId)
            {
                if (NetEventSource.Log.IsEnabled()) _parent.Trace(streamId, "");

                Span<byte> frame = stackalloc byte[FrameHeader.Size];

                FrameHeader.WriteTo(frame, 0, FrameType.Data, FrameFlags.EndStream, streamId);

                WriteFireAndForgetFrame(frame);
            }

            public void SendPing(long content, bool isAck)
            {
                if (NetEventSource.Log.IsEnabled()) _parent.Trace($"{nameof(content)}={content}, {nameof(isAck)}={isAck}");

                Debug.Assert(sizeof(long) == FrameHeader.PingLength);

                Span<byte> frame = stackalloc byte[FrameHeader.Size + FrameHeader.PingLength];

                FrameHeader.WriteTo(frame, FrameHeader.PingLength, FrameType.Ping, isAck ? FrameFlags.Ack : FrameFlags.None, streamId: 0);
                BinaryPrimitives.WriteInt64BigEndian(frame.Slice(FrameHeader.Size), content);

                WriteFireAndForgetFrame(frame);
            }

            public void SendSettingsAck()
            {
                if (NetEventSource.Log.IsEnabled()) _parent.Trace("");

                Span<byte> frame = stackalloc byte[FrameHeader.Size];

                FrameHeader.WriteTo(frame, 0, FrameType.Settings, FrameFlags.Ack, streamId: 0);

                WriteFireAndForgetFrame(frame);
            }

            public void SendRstStream(int streamId, Http2ProtocolErrorCode errorCode)
            {
                if (NetEventSource.Log.IsEnabled()) _parent.Trace(streamId, $"{nameof(errorCode)}={errorCode}");

                Span<byte> frame = stackalloc byte[FrameHeader.Size + FrameHeader.RstStreamLength];

                FrameHeader.WriteTo(frame, FrameHeader.RstStreamLength, FrameType.RstStream, FrameFlags.None, streamId);
                BinaryPrimitives.WriteInt32BigEndian(frame.Slice(FrameHeader.Size), (int)errorCode);

                WriteFireAndForgetFrame(frame);
            }

            private void WriteFireAndForgetFrame(ReadOnlySpan<byte> frame)
            {
                lock (FireAndForgetLock)
                {
                    _fireAndForgetBuffer.EnsureAvailableSpace(frame.Length);
                    frame.CopyTo(_fireAndForgetBuffer.AvailableSpan);
                    _fireAndForgetBuffer.Commit(frame.Length);

                    if (_fireAndForgetBuffer.ActiveLength != frame.Length)
                    {
                        // The buffer wasn't empty so this wasn't the first write to it.
                        // A previous write already scheduled the bytes to be copied to the outgoing buffer.
                        return;
                    }
                }

                // This was the first write to the buffer, so we schedule it to be copied to the outgoing buffer.
                WakeUpWriterLoop();
            }
        }
    }
}
