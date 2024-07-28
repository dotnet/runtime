// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    internal sealed partial class ManagedWebSocket : WebSocket
    {
        private bool IsUnsolicitedPongKeepAlive => _keepAlivePingState is null;
        private static bool IsValidSendState(WebSocketState state) => Array.IndexOf(s_validSendStates, state) != -1;
        private static bool IsValidReceiveState(WebSocketState state) => Array.IndexOf(s_validReceiveStates, state) != -1;

        private void HeartBeat()
        {
            if (IsUnsolicitedPongKeepAlive)
            {
                UnsolicitedPongHeartBeat();
            }
            else
            {
                KeepAlivePingHeartBeat();
            }
        }

        private void UnsolicitedPongHeartBeat()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this);

            // This exists purely to keep the connection alive; don't wait for the result, and ignore any failures.
            // The call will handle releasing the lock.  We send a pong rather than ping, since it's allowed by
            // the RFC as a unidirectional heartbeat and we're not interested in waiting for a response.
            this.Observe(
                TrySendKeepAliveFrameAsync(MessageOpcode.Pong));
        }

        private ValueTask TrySendKeepAliveFrameAsync(MessageOpcode opcode, ReadOnlyMemory<byte>? payload = null)
        {
            Debug.Assert(opcode is MessageOpcode.Pong || !IsUnsolicitedPongKeepAlive && opcode is MessageOpcode.Ping);

            payload ??= ReadOnlyMemory<byte>.Empty;

            if (!IsValidSendState(_state))
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, $"Cannot send keep-alive frame in {nameof(_state)}={_state}");

                // we can't send any frames, but no need to throw as we are not observing errors anyway
                return ValueTask.CompletedTask;
            }

            return SendFrameAsync(opcode, endOfMessage: true, disableCompression: true, payload.Value, CancellationToken.None);
        }

        private void KeepAlivePingHeartBeat()
        {
            Debug.Assert(_keepAlivePingState != null);
            Debug.Assert(_keepAlivePingState.Exception == null);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, $"{nameof(_keepAlivePingState.AwaitingPong)}={_keepAlivePingState.AwaitingPong}");

            try
            {
                if (_keepAlivePingState.AwaitingPong)
                {
                    KeepAlivePingThrowIfTimedOut();
                }
                else
                {
                    SendKeepAlivePingIfNeeded();
                }
            }
            catch (Exception e)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, $"Exception occurred during KeepAlive: {e}");

                if (TryTransitionToAborted())
                {
                    // We only save the exception in the keep-alive state if we actually triggered the abort
                    Interlocked.Exchange(ref _keepAlivePingState.Exception, e);
                    Abort(); // this will also dispose the timer
                }
            }
        }

        private void KeepAlivePingThrowIfTimedOut()
        {
            Debug.Assert(_keepAlivePingState != null);
            Debug.Assert(_keepAlivePingState.AwaitingPong);
            Debug.Assert(_keepAlivePingState.WillTimeoutTimestamp != Timeout.Infinite);

            long now = Environment.TickCount64;

            if (now > Interlocked.Read(ref _keepAlivePingState.WillTimeoutTimestamp))
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Trace(this, $"Keep-alive ping timed out after {_keepAlivePingState.TimeoutMs}ms. Expected pong with payload {_keepAlivePingState.PingPayload}");
                }

                throw new WebSocketException(WebSocketError.Faulted, SR.net_Websockets_KeepAlivePingTimeout);
            }

            TryIssueReadAhead();
        }

        private void TryIssueReadAhead()
        {
            Debug.Assert(_readAheadState != null);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this);

            if (!_receiveMutex.TryEnter())
            {
                // Read (either a user read, or a previous read-ahead)
                // is already in progress, so there's no need to issue a read-ahead.
                // If that read will not end up processing the pong response,
                // we'll try again on the next heartbeat
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, "Read-ahead not started: other read already in progress");
                return;
            }
            if (NetEventSource.Log.IsEnabled()) NetEventSource.MutexEntered(_receiveMutex);

            bool shouldExitMutex = true;

            try
            {
                if (!IsValidReceiveState(_state)) // we can't receive any frames, but no need to throw as we are not observing errors anyway
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, $"Cannot start read-ahead in {nameof(_state)}={_state}");
                    return;
                }

                if (_readAheadState!.ReadAheadTask is not null) // previous read-ahead is not consumed yet
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, "Read-ahead not started: previous read-ahead is not consumed yet");
                    return;
                }

                _readAheadState!.ReadAheadTask = DoReadAheadAndExitMutexAsync(); // the task will release the mutex when completed
                shouldExitMutex = false;
            }
            finally
            {
                if (shouldExitMutex)
                {
                    _receiveMutex.Exit();
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.MutexExited(_receiveMutex);
                }
            }
        }

        private async Task DoReadAheadAndExitMutexAsync() // this is assigned to ReadAheadTask
        {
            Debug.Assert(_receiveMutex.IsHeld);
            Debug.Assert(IsValidReceiveState(_state));

            try
            {
                // Issue a zero-byte read first.
                // Note that if the other side never sends any data frames at all, this single call
                // will continue draining all the (upcoming) pongs until the connection is closed

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, $"Starting zero-byte read-ahead");

                ValueWebSocketReceiveResult result = await ReceiveAsyncPrivate<ValueWebSocketReceiveResult>(
                    Array.Empty<byte>(),
                    shouldEnterMutex: false, // we are already in the mutex
                    shouldAbortOnCanceled: false, // we don't have a cancellation token
                    CancellationToken.None).ConfigureAwait(false);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, "Zero-byte read-ahead received close frame");

                    _readAheadState!.BufferedResult = result;
                    return;
                }

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, "Zero-byte read-ahead completed");

                Debug.Assert(IsValidReceiveState(_state));

                // If, during a zero-byte read, Pong was available before a data frame,
                // it will be already processed by now. However, let's still
                // do the actual read, as we're already in the mutex and
                // ReceiveAsyncPrivate has already read the next data frame header

                Debug.Assert(!_lastReceiveHeader.Processed);

                // (Remaining) PayloadLength can be 0 if the message is compressed and not fully inflated yet
                // UnconsumedCount doesn't give us any information about the inflated size, but it's as good guess as any
                int bufferSize = (int)Math.Min(
                    Math.Max(_lastReceiveHeader.PayloadLength, _lastReceiveHeader.Compressed ? _inflater!.UnconsumedCount + 1 : 0),
                    ReadAheadState.MaxReadAheadBufferSize);

                Debug.Assert(bufferSize > 0);

                // the buffer is returned to pool after read-ahead data is consumed by the user
                _readAheadState!.Buffer.EnsureAvailableSpace(bufferSize);

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, $"Starting read-ahead with buffer length={bufferSize}");

                _readAheadState.BufferedResult = await ReceiveAsyncPrivate<ValueWebSocketReceiveResult>(
                    _readAheadState.Buffer.ActiveMemory,
                    shouldEnterMutex: false, // we are already in the mutex
                    shouldAbortOnCanceled: false, // we don't have a cancellation token
                    CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                _receiveMutex.Exit();
                if (NetEventSource.Log.IsEnabled()) NetEventSource.MutexExited(_receiveMutex);
            }
        }

        private void SendKeepAlivePingIfNeeded()
        {
            Debug.Assert(_keepAlivePingState != null);
            Debug.Assert(!_keepAlivePingState.AwaitingPong);

            long now = Environment.TickCount64;

            // Check whether keep alive delay has passed since last frame received
            if (now > Interlocked.Read(ref _keepAlivePingState.NextPingTimestamp))
            {
                // Set the status directly to ping sent and set the timestamp
                Interlocked.Exchange(ref _keepAlivePingState.WillTimeoutTimestamp, now + _keepAlivePingState.TimeoutMs);
                _keepAlivePingState.AwaitingPong = true;

                long pingPayload = Interlocked.Increment(ref _keepAlivePingState.PingPayload);

                this.Observe(
                    SendPingAsync(pingPayload));
            }
        }

        private async ValueTask SendPingAsync(long pingPayload)
        {
            Debug.Assert(_keepAlivePingState != null);
            Debug.Assert(_readAheadState != null);

            byte[] pingPayloadBuffer = ArrayPool<byte>.Shared.Rent(sizeof(long));
            BinaryPrimitives.WriteInt64BigEndian(pingPayloadBuffer, pingPayload);
            try
            {
                await TrySendKeepAliveFrameAsync(
                    MessageOpcode.Ping,
                    pingPayloadBuffer.AsMemory(0, sizeof(long)))
                    .ConfigureAwait(false);

                if (NetEventSource.Log.IsEnabled()) NetEventSource.KeepAlivePingSent(this, pingPayload);

                TryIssueReadAhead();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(pingPayloadBuffer);
            }
        }

        private void OnDataReceived(int bytesRead)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this);

            if (_keepAlivePingState != null && bytesRead > 0)
            {
                _keepAlivePingState.OnDataReceived();
            }
        }

        private sealed class KeepAlivePingState
        {
            internal const int PingPayloadSize = sizeof(long);

            internal long DelayMs;
            internal long TimeoutMs;
            internal long NextPingTimestamp;
            internal long WillTimeoutTimestamp;

            internal long HeartBeatIntervalMs;

            internal bool AwaitingPong;
            internal long PingPayload;
            internal Exception? Exception;

            public KeepAlivePingState(TimeSpan keepAliveInterval, TimeSpan keepAliveTimeout)
            {
                DelayMs = TimeSpanToMs(keepAliveInterval);
                TimeoutMs = TimeSpanToMs(keepAliveTimeout);
                NextPingTimestamp = Environment.TickCount64 + DelayMs;
                WillTimeoutTimestamp = Timeout.Infinite;

                HeartBeatIntervalMs = Math.Min(DelayMs, TimeoutMs) / 4;

                static long TimeSpanToMs(TimeSpan value)
                {
                    double milliseconds = value.TotalMilliseconds;
                    return (long)(milliseconds > int.MaxValue ? int.MaxValue : milliseconds);
                }
            }

            internal void OnDataReceived()
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this);
                Interlocked.Exchange(ref NextPingTimestamp, Environment.TickCount64 + DelayMs);
            }

            internal void OnPongResponseReceived(Span<byte> pongPayload)
            {
                Debug.Assert(AwaitingPong);
                Debug.Assert(pongPayload.Length == sizeof(long));

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this);

                long pongPayloadValue = BinaryPrimitives.ReadInt64BigEndian(pongPayload);
                if (pongPayloadValue == Interlocked.Read(ref PingPayload))
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.PongResponseReceived(this, pongPayloadValue);

                    Interlocked.Exchange(ref WillTimeoutTimestamp, Timeout.Infinite);
                    AwaitingPong = false;
                }
                else if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Trace(this, $"Received pong with unexpected payload {pongPayloadValue}. Expected {Interlocked.Read(ref PingPayload)}. Skipping.");
                }
            }

            internal void ThrowIfFaulted()
            {
                if (Interlocked.Exchange(ref Exception, null) is Exception e)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, $"Throwing Keep-Alive exception {e.GetType().Name}: {e.Message}");

                    throw new OperationCanceledException(nameof(WebSocketState.Aborted), e);
                }
            }
        }

        private sealed class ReadAheadState : IDisposable
        {
            internal const int MaxReadAheadBufferSize = 16 * 1024 * 1024; // same as DefaultHttp2MaxStreamWindowSize
            internal ArrayBuffer Buffer;
            internal ValueWebSocketReceiveResult BufferedResult;
            internal Task? ReadAheadTask;
            private bool IsDisposed => Buffer.DangerousGetUnderlyingBuffer() is null;

            private readonly AsyncMutex _receiveMutex; // for Debug.Asserts

            internal ReadAheadState(AsyncMutex receiveMutex)
            {
#if DEBUG
                _receiveMutex = receiveMutex;
#else
                _receiveMutex = null!;
#endif
            }

            internal ValueWebSocketReceiveResult ConsumeResult(Span<byte> destination)
            {
                Debug.Assert(_receiveMutex.IsHeld, $"Caller should hold the {nameof(_receiveMutex)}");
                Debug.Assert(ReadAheadTask is not null);

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this);

                ObjectDisposedException.ThrowIf(IsDisposed, nameof(ReadAheadState));

                try
                {
                    if (!ReadAheadTask.IsCompleted)
                    {
                        // We are in a mutex. Read-ahead task also only executes within the mutex.
                        // If the task isn't null, it must be already completed.
                        // Throwing here instead of Debug.Assert, just in case to prevent hanging on GetAwaiter().GetResult()
                        throw new InvalidOperationException("Read-ahead task should be completed before consuming the result.");
                    }

                    ReadAheadTask.GetAwaiter().GetResult(); // throw exceptions, if any

                    int count = Math.Min(destination.Length, Buffer.ActiveLength);
                    Buffer.ActiveSpan.Slice(0, count).CopyTo(destination);
                    Buffer.Discard(count);

                    ValueWebSocketReceiveResult result;

                    if (Buffer.ActiveLength == 0) // we've consumed all of the data
                    {
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, $"All read-ahead data consumed. Last read: {count} bytes");

                        result = BufferedResult;
                        BufferedResult = default;
                        Buffer.ClearAndReturnBuffer();
                        return result;
                    }

                    // If we have more data in the read-ahead buffer, we need to construct a new result for the next read to consume it.
                    result = new ValueWebSocketReceiveResult(count, BufferedResult.MessageType, endOfMessage: false);
                    BufferedResult = new ValueWebSocketReceiveResult(Buffer.ActiveLength, BufferedResult.MessageType, BufferedResult.EndOfMessage);

                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, $"Read-ahead data partially consumed. Last read: {count} bytes, remaining: {Buffer.ActiveLength} bytes");

                    ReadAheadTask = Task.CompletedTask;
                    return result;
                }
                catch
                {
                    Dispose();
                    throw;
                }
            }

            public void Dispose()
            {
                Debug.Assert(_receiveMutex.IsHeld, $"Caller should hold the {nameof(_receiveMutex)}");

                if (ReadAheadTask is not null)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, "Read-ahead task is left unconsumed on dispose");

                    this.Observe(ReadAheadTask);
                    ReadAheadTask = null;
                }
                BufferedResult = default;
                Buffer.Dispose();
            }
        }
    }
}
