// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
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

            this.Observe(
                TrySendKeepAliveFrameAsync(MessageOpcode.Pong));
        }

        private ValueTask TrySendKeepAliveFrameAsync(MessageOpcode opcode, ReadOnlyMemory<byte>? payload = null)
        {
            Debug.Assert(opcode is MessageOpcode.Pong || !IsUnsolicitedPongKeepAlive && opcode is MessageOpcode.Ping);

            if (!IsValidSendState(_state))
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, $"Cannot send keep-alive frame in {nameof(_state)}={_state}");

                // we can't send any frames, but no need to throw as we are not observing errors anyway
                return ValueTask.CompletedTask;
            }

            payload ??= ReadOnlyMemory<byte>.Empty;

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
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, $"Exception during Keep-Alive: {e}");

                lock (StateUpdateLock)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.DbgLockTaken(this);

                    if (!_disposed)
                    {
                        // We only save the exception in the keep-alive state if we will actually trigger the abort/disposal
                        // The exception needs to be assigned before _disposed is set to true
                        Volatile.Write(ref _keepAlivePingState.Exception, e);
                        Abort();
                    }

                    if (NetEventSource.Log.IsEnabled()) NetEventSource.DbgLockReleased(this);
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

                if (_readAheadState!.ReadAheadCompletedOrInProgress) // previous read-ahead is not consumed yet
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, "Read-ahead not started: previous read-ahead is not consumed yet");
                    return;
                }

                TaskCompletionSource readAheadTcs = _readAheadState.StartNewReadAhead();
                this.Observe(
                    DoReadAheadAndExitMutexAsync(readAheadTcs)); // the task will release the mutex when completed
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

        private async Task DoReadAheadAndExitMutexAsync(TaskCompletionSource readAheadTcs)
        {
            Debug.Assert(_receiveMutex.IsHeld);
            Debug.Assert(IsValidReceiveState(_state));

            if (NetEventSource.Log.IsEnabled()) NetEventSource.ReadAheadStarted(this);

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
                    _readAheadState.Buffer.AvailableMemory,
                    shouldEnterMutex: false, // we are already in the mutex
                    shouldAbortOnCanceled: false, // we don't have a cancellation token
                    CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.AsyncDbgLog(this, $"Completing TCS with exception {e.GetType().FullName}: {e.Message}");
                readAheadTcs.SetException(e); // TCS should be completed before exiting the mutex
            }
            finally
            {
                readAheadTcs.TrySetResult(); // TCS should be completed before exiting the mutex
                if (NetEventSource.Log.IsEnabled()) NetEventSource.AsyncDbgLog(this, $"TCS completed");

                _receiveMutex.Exit();

                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.MutexExited(_receiveMutex);
                    NetEventSource.ReadAheadCompleted(this);
                }
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

        private void ThrowIfDisposedOrKeepAliveFaulted()
        {
            Debug.Assert(_keepAlivePingState is not null);

            if (!Volatile.Read(ref _disposed))
            {
                return;
            }

            Exception? abortingException = Volatile.Read(ref _keepAlivePingState.Exception);

            // If abortException is not null, it triggered the abort which also disposed the websocket
            // We only save the abortException if it actually triggered the abort
            ObjectDisposedException.ThrowIf(abortingException is null, this);
            throw GetOperationCanceledException(abortingException, nameof(WebSocketState.Aborted));
        }

        private void ThrowIfInvalidStateOrKeepAliveFaulted(WebSocketState[] validStates)
        {
            Debug.Assert(_keepAlivePingState is not null);

            string? invalidStateMessage = WebSocketValidate.GetErrorMessageIfInvalidState(_state, validStates);

            if (!Volatile.Read(ref _disposed))
            {
                if (invalidStateMessage is not null)
                {
                    throw new WebSocketException(WebSocketError.InvalidState, invalidStateMessage);
                }
                return;
            }

            Exception? abortingException = Volatile.Read(ref _keepAlivePingState.Exception);
            ObjectDisposedException.ThrowIf(abortingException is null, this);

            ThrowOperationCanceledIf(invalidStateMessage is null, abortingException, nameof(WebSocketState.Aborted));
            throw new WebSocketException(WebSocketError.InvalidState, invalidStateMessage, abortingException);
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
        }

        private sealed class ReadAheadState : IDisposable
        {
            internal const int MaxReadAheadBufferSize = 16 * 1024 * 1024; // same as DefaultHttp2MaxStreamWindowSize
            internal ArrayBuffer Buffer = new ArrayBuffer(0, usePool: true);
            internal ValueWebSocketReceiveResult BufferedResult;
            private TaskCompletionSource? ReadAheadTcs;
            private bool IsDisposed => Buffer.DangerousGetUnderlyingBuffer() is null;

            internal bool ReadAheadCompleted => ReadAheadTcs?.Task?.IsCompleted ?? false;

            internal bool ReadAheadCompletedOrInProgress => ReadAheadTcs is not null;

            private readonly AsyncMutex _receiveMutex; // for Debug.Asserts

            internal ReadAheadState(AsyncMutex receiveMutex)
            {
#if DEBUG
                _receiveMutex = receiveMutex;
#else
                _receiveMutex = null!;
#endif
            }

            internal TaskCompletionSource StartNewReadAhead()
            {
                Debug.Assert(_receiveMutex.IsHeld, $"Caller should hold the {nameof(_receiveMutex)}");

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this);

                ObjectDisposedException.ThrowIf(IsDisposed, nameof(ReadAheadState));

                if (ReadAheadTcs is not null)
                {
                    throw new InvalidOperationException("Read-ahead task should be consumed before starting a new one.");
                }

                ReadAheadTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                return ReadAheadTcs;
            }

            internal ValueWebSocketReceiveResult ConsumeResult(Span<byte> destination)
            {
                Debug.Assert(_receiveMutex.IsHeld, $"Caller should hold the {nameof(_receiveMutex)}");
                Debug.Assert(ReadAheadTcs is not null);

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this);

                ObjectDisposedException.ThrowIf(IsDisposed, nameof(ReadAheadState));

                if (!ReadAheadCompleted)
                {
                    // We are in a mutex. Read-ahead task also only executes within the mutex.
                    // If the task isn't null, it must be already completed.
                    // Throwing here instead of Debug.Assert, just in case to prevent hanging on GetAwaiter().GetResult()
                    throw new InvalidOperationException("Read-ahead task should be completed before consuming the result.");
                }

                if (ReadAheadTcs.Task.IsFaulted) // throw exception if any
                {
                    ExceptionDispatchInfo.Throw(ReadAheadTcs.Task.Exception);
                }

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
                    ReadAheadTcs = null; // we're done with this task
                    return result;
                }

                // If we have more data in the read-ahead buffer, we need to construct a new result for the next read to consume it.
                result = new ValueWebSocketReceiveResult(count, BufferedResult.MessageType, endOfMessage: false);
                BufferedResult = new ValueWebSocketReceiveResult(Buffer.ActiveLength, BufferedResult.MessageType, BufferedResult.EndOfMessage);

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, $"Read-ahead data partially consumed. Last read: {count} bytes, remaining: {Buffer.ActiveLength} bytes");

                // leave ReadAheadTcs completed as is, so the next read will consume the remaining data
                return result;
            }

            public void Dispose()
            {
                Debug.Assert(_receiveMutex.IsHeld, $"Caller should hold the {nameof(_receiveMutex)}");

                Buffer.Dispose();
                BufferedResult = default;

                if (ReadAheadTcs is not null)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, "Read-ahead task is left unconsumed on dispose");

                    this.Observe(ReadAheadTcs.Task);
                    ReadAheadTcs = null;
                }
            }
        }
    }
}
