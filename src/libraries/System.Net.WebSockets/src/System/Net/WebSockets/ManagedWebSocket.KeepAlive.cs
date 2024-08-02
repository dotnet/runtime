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
        // "Observe" either a ValueTask result, or any exception, ignoring it
        // to prevent the unobserved exception event from being raised.
        public void Observe(ValueTask t)
        {
            if (t.IsCompletedSuccessfully)
            {
                t.GetAwaiter().GetResult();
            }
            else
            {
                ObserveException(t.AsTask());
            }
        }

        // "Observe" either a Task result, or any exception, ignoring it
        // to prevent the unobserved exception event from being raised.
        public void Observe(Task t)
        {
            if (t.IsCompletedSuccessfully)
            {
                t.GetAwaiter().GetResult();
            }
            else
            {
                ObserveException(t);
            }
        }

        private void ObserveException(Task task)
        {
            task.ContinueWith(
                LogFaulted,
                this,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

            static void LogFaulted(Task task, object? state)
            {
                Debug.Assert(task.IsFaulted);

                Exception? e = task.Exception!.InnerException; // accessing exception anyway, to observe it regardless of whether the tracing is enabled

                if (NetEventSource.Log.IsEnabled() && e != null) NetEventSource.TraceException((ManagedWebSocket)state!, e);
            }
        }

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

            Observe(
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
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.TraceException(this, e);
                    NetEventSource.Trace(this, $"_disposed={_disposed}");
                }

                if (!_disposed)
                {
                    // We only save the exception in the keep-alive state if we will actually trigger the abort/disposal
                    // The exception needs to be assigned before _disposed is set to true
                    _keepAlivePingState.Exception = e;

                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, $"Exception saved in _keepAlivePingState, aborting...");

                    Abort();
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

                Observe(
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

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, $"_disposed={_disposed}, _state={_state}, _keepAlivePingState.Exception={_keepAlivePingState.Exception?.Message}");

            if (_disposed && _keepAlivePingState.Exception is not null)
            {
                // If Exception is not null, it triggered the abort which also disposed the websocket
                // We only save the Exception if it actually triggered the abort
                throw new OperationCanceledException(nameof(WebSocketState.Aborted), _keepAlivePingState.Exception);
            }

            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        private void ThrowIfInvalidStateOrKeepAliveFaulted(WebSocketState[] validStates)
        {
            Debug.Assert(_keepAlivePingState is not null);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, $"_disposed={_disposed}, _state={_state}, _keepAlivePingState.Exception={_keepAlivePingState.Exception?.Message}");

            try
            {
                WebSocketValidate.ThrowIfInvalidState(_state, _disposed, validStates);
            }
            catch (Exception exc) when (_disposed && _keepAlivePingState.Exception is not null)
            {
                // If Exception is not null, it triggered the abort which also disposed the websocket
                // We only save the Exception if it actually triggered the abort
                if (exc is ObjectDisposedException ode && ode.ObjectName == typeof(ManagedWebSocket).FullName)
                {
                    throw new OperationCanceledException(nameof(WebSocketState.Aborted), _keepAlivePingState.Exception);
                }

                if (exc is WebSocketException we && we.WebSocketErrorCode == WebSocketError.InvalidState)
                {
                    throw new WebSocketException(WebSocketError.InvalidState, we.Message, _keepAlivePingState.Exception);
                }
            }
        }

        private sealed class KeepAlivePingState
        {
            internal const int PingPayloadSize = sizeof(long);
            internal const int MinIntervalMs = 1;

            internal int DelayMs;
            internal int TimeoutMs;
            internal int HeartBeatIntervalMs;

            internal long NextPingTimestamp;
            internal long WillTimeoutTimestamp;

            internal bool AwaitingPong;
            internal long PingPayload;
            internal Exception? Exception;

            public KeepAlivePingState(TimeSpan keepAliveInterval, TimeSpan keepAliveTimeout)
            {
                DelayMs = TimeSpanToMs(keepAliveInterval);
                TimeoutMs = TimeSpanToMs(keepAliveTimeout);
                NextPingTimestamp = Environment.TickCount64 + DelayMs;
                WillTimeoutTimestamp = Timeout.Infinite;

                HeartBeatIntervalMs = Math.Max(
                    Math.Min(DelayMs, TimeoutMs) / 4,
                    MinIntervalMs);

                static int TimeSpanToMs(TimeSpan value) =>
                    Math.Clamp((int)value.TotalMilliseconds, MinIntervalMs, int.MaxValue);
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
