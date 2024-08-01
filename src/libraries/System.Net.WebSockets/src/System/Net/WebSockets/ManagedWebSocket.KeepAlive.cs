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

            byte[] pingPayloadBuffer = ArrayPool<byte>.Shared.Rent(sizeof(long));
            BinaryPrimitives.WriteInt64BigEndian(pingPayloadBuffer, pingPayload);
            try
            {
                await TrySendKeepAliveFrameAsync(
                    MessageOpcode.Ping,
                    pingPayloadBuffer.AsMemory(0, sizeof(long)))
                    .ConfigureAwait(false);

                if (NetEventSource.Log.IsEnabled()) NetEventSource.KeepAlivePingSent(this, pingPayload);
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
            internal const long MinIntervalMs = 1;

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

                HeartBeatIntervalMs = Math.Max(Math.Min(DelayMs, TimeoutMs) / 4, MinIntervalMs);

                static long TimeSpanToMs(TimeSpan value)
                {
                    double milliseconds = value.TotalMilliseconds;
                    long ms = (long)(milliseconds > int.MaxValue ? int.MaxValue : milliseconds);
                    return Math.Max(ms, MinIntervalMs);
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
    }
}
