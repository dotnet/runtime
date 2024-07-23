// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    internal sealed partial class ManagedWebSocket : WebSocket
    {
        private bool IsUnsolicitedPongKeepAlive => _keepAlivePingState is null;

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
            // This exists purely to keep the connection alive; don't wait for the result, and ignore any failures.
            // The call will handle releasing the lock.  We send a pong rather than ping, since it's allowed by
            // the RFC as a unidirectional heartbeat and we're not interested in waiting for a response.
            ObserveWhenCompleted(
                SendPongAsync());
        }

        private static void ObserveWhenCompleted(ValueTask t)
        {
            if (t.IsCompletedSuccessfully)
            {
                t.GetAwaiter().GetResult();
            }
            else
            {
                ObserveExceptionWhenCompleted(t.AsTask());
            }
        }

        // "Observe" any exception, ignoring it to prevent the unobserved exception event from being raised.
        private static void ObserveExceptionWhenCompleted(Task t)
        {
            t.ContinueWith(static p => { _ = p.Exception; },
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        private ValueTask SendPongAsync()
            => SendFrameAsync(
                MessageOpcode.Pong,
                endOfMessage: true,
                disableCompression: true,
                ReadOnlyMemory<byte>.Empty,
                CancellationToken.None);

        private void KeepAlivePingHeartBeat()
        {
            Debug.Assert(_keepAlivePingState != null);
            Debug.Assert(_keepAlivePingState.Exception == null);

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
                Interlocked.CompareExchange(ref _keepAlivePingState.Exception, e, null);
                Abort(); // this will also dispose the timer
            }
        }

        private void KeepAlivePingThrowIfTimedOut()
        {
            Debug.Assert(_keepAlivePingState != null);
            Debug.Assert(_keepAlivePingState.AwaitingPong);

            long now = Environment.TickCount64;

            if (now > Interlocked.Read(ref _keepAlivePingState.WillTimeoutTimestamp))
            {
                throw new WebSocketException(WebSocketError.Faulted, SR.net_Websockets_KeepAlivePingTimeout);
            }

            ObserveWhenCompleted(
                TryIssueReadAheadAsync());
        }

        private ValueTask TryIssueReadAheadAsync()
        {
            Debug.Assert(_readAheadState != null);

            if (_receiveMutex.IsHeld)
            {
                // Read (either user read, or previous read-ahead) is already in progress, no need to issue read-ahead
                return ValueTask.CompletedTask;
            }

            Task lockTask = _receiveMutex.EnterAsync(CancellationToken.None);

            if (lockTask.IsCompletedSuccessfully)
            {
                TryIssueReadAhead_LockTaken();
                return ValueTask.CompletedTask;
            }

            return TryIssueReadAhead_Async(lockTask);
        }

        private void TryIssueReadAhead_LockTaken()
        {
            try
            {
                if (_readAheadState!.ReadAheadTask is not null) // previous read-ahead is not consumed yet
                {
                    _receiveMutex.Exit();
                    return;
                }

                _readAheadState!.ReadAheadTask = DoReadAheadAsync().AsTask(); //todo optimize to use value task??
                // note: DoReadAheadAsync will handle releasing the mutex on this code path
            }
            catch
            {
                _receiveMutex.Exit();
                throw;
            }
        }

        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private async ValueTask DoReadAheadAsync() // this is assigned to ReadAheadTask
        {
            Debug.Assert(_receiveMutex.IsHeld);

            try
            {
                // issue zero-byte read first
                await ReceiveAsyncPrivate_LockAquired<ValueWebSocketReceiveResult>(Array.Empty<byte>(), CancellationToken.None).ConfigureAwait(false);

                _readAheadState!.Buffer.EnsureAvailableSpace(ReadAheadState.ReadAheadBufferSize);
                _readAheadState.BufferedResult = await ReceiveAsyncPrivate_LockAquired<ValueWebSocketReceiveResult>(_readAheadState.Buffer.ActiveMemory, CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                _receiveMutex.Exit();
            }
        }

        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private async ValueTask TryIssueReadAhead_Async(Task lockTask)
        {
            await lockTask.ConfigureAwait(false);
            TryIssueReadAhead_LockTaken();
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

                ObserveWhenCompleted(
                    SendPingAsync(pingPayload));

                ObserveWhenCompleted(
                    TryIssueReadAheadAsync());
            }
        }

        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private async ValueTask SendPingAsync(long pingPayload)
        {
            Debug.Assert(_keepAlivePingState != null);
            Debug.Assert(_readAheadState != null);

            byte[] pingPayloadBuffer = ArrayPool<byte>.Shared.Rent(sizeof(long));
            BinaryPrimitives.WriteInt64BigEndian(pingPayloadBuffer, pingPayload);
            try
            {
                await SendFrameAsync(
                    MessageOpcode.Ping,
                    endOfMessage: true,
                    disableCompression: true,
                    pingPayloadBuffer.AsMemory(0, sizeof(long)),
                    CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(pingPayloadBuffer);
            }
        }

        private void OnDataReceived(int bytesRead)
        {
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
                WillTimeoutTimestamp = long.MaxValue;

                HeartBeatIntervalMs = (long)Math.Max(1000, Math.Min(keepAliveInterval.TotalMilliseconds, keepAliveTimeout.TotalMilliseconds) / 4); // similar to HTTP/2

                static long TimeSpanToMs(TimeSpan value) // similar to HTTP/2
                {
                    double milliseconds = value.TotalMilliseconds;
                    return (long)(milliseconds > int.MaxValue ? int.MaxValue : milliseconds);
                }
            }

            internal void OnDataReceived()
                => Interlocked.Exchange(ref NextPingTimestamp, Environment.TickCount64 + DelayMs);

            internal void OnPongResponseReceived(Span<byte> pongPayload)
            {
                Debug.Assert(AwaitingPong);
                Debug.Assert(pongPayload.Length == sizeof(long));

                long pongPayloadValue = BinaryPrimitives.ReadInt64BigEndian(pongPayload);
                if (pongPayloadValue == Interlocked.Read(ref PingPayload))
                {
                    Interlocked.Exchange(ref WillTimeoutTimestamp, long.MaxValue);
                    AwaitingPong = false;
                }
            }

            internal void ThrowIfFaulted()
            {
                if (Interlocked.Exchange(ref Exception, null) is Exception e)
                {
                    throw e;
                }
            }
        }

        private sealed class ReadAheadState : IDisposable
        {
            internal const int ReadAheadBufferSize = 16384; // TODO: 4096 ?
            internal ArrayBuffer Buffer;
            internal ValueWebSocketReceiveResult BufferedResult;
            internal Task? ReadAheadTask;
            internal bool IsDisposed => Buffer.DangerousGetUnderlyingBuffer() is null;

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
                Debug.Assert(destination.Length > 0);
                Debug.Assert(_receiveMutex.IsHeld, $"Caller should hold the {nameof(_receiveMutex)}");
                Debug.Assert(ReadAheadTask is not null);

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
                        result = BufferedResult;
                        BufferedResult = default;
                        Buffer.ClearAndReturnBuffer(); // TODO: should we return or keep the buffer??
                        return result;
                    }

                    // If we have more data in the read-ahead buffer, we need to construct a new result for the next read to consume it.
                    result = new ValueWebSocketReceiveResult(count, BufferedResult.MessageType, endOfMessage: false);
                    BufferedResult = new ValueWebSocketReceiveResult(Buffer.ActiveLength, BufferedResult.MessageType, BufferedResult.EndOfMessage);

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

                if (IsDisposed)
                {
                    return;
                }

                if (ReadAheadTask is not null)
                {
                    ObserveExceptionWhenCompleted(ReadAheadTask);
                    ReadAheadTask = null;
                }
                BufferedResult = default;
                Buffer.Dispose();
            }
        }
    }
}
