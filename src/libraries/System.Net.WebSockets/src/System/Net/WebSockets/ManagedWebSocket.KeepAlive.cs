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

            static void LogFaulted(Task task, object? thisObj)
            {
                Debug.Assert(task.IsFaulted);

                Exception? innerException = task.Exception!.InnerException; // accessing exception anyway, to observe it regardless of whether the tracing is enabled

                if (NetEventSource.Log.IsEnabled()) NetEventSource.TraceException(thisObj, innerException ?? task.Exception!);
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

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this);

            try
            {
                bool timedOut = false;
                bool sendPing = false;
                long pingPayload = -1;

                lock (StateUpdateLock)
                {
                    if (_keepAlivePingState.Exception is not null)
                    {
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, $"KeepAlive already faulted, skipping... (exception: {_keepAlivePingState.Exception.Message})");
                        return;
                    }

                    long now = Environment.TickCount64;

                    if (_keepAlivePingState.AwaitingPong)
                    {
                        Debug.Assert(_keepAlivePingState.WillTimeoutTimestamp != Timeout.Infinite);

                        if (now > _keepAlivePingState.WillTimeoutTimestamp)
                        {
                            timedOut = true;
                            pingPayload = _keepAlivePingState.PingPayload;
                        }
                    }
                    else
                    {
                        if (now > _keepAlivePingState.NextPingTimestamp)
                        {
                            sendPing = true;
                            pingPayload = ++_keepAlivePingState.PingPayload;

                            _keepAlivePingState.AwaitingPong = true;
                            _keepAlivePingState.WillTimeoutTimestamp = now + _keepAlivePingState.TimeoutMs;
                        }
                    }
                }

                if (timedOut)
                {
                    if (NetEventSource.Log.IsEnabled())
                    {
                        NetEventSource.Trace(this, $"Keep-alive ping timed out after {_keepAlivePingState.TimeoutMs}ms. Expected pong with payload {pingPayload}");
                    }

                    throw new WebSocketException(WebSocketError.Faulted, SR.net_Websockets_KeepAlivePingTimeout);
                }
                else if (sendPing)
                {
                    Observe(
                        SendPingAsync(pingPayload));
                }
            }
            catch (Exception e)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.TraceException(this, e);

                bool shouldAbort = false;
                lock (StateUpdateLock)
                {
                    if (!_disposed)
                    {
                        // We only save the exception in the keep-alive state if we will actually trigger the abort/disposal
                        // The exception needs to be assigned before _disposed is set to true
                        _keepAlivePingState.Exception = e;
                        shouldAbort = true;
                    }
                }

                if (shouldAbort)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, $"Exception saved in _keepAlivePingState, aborting...");
                    Abort();
                }
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
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, $"bytesRead={bytesRead}");

            if (_keepAlivePingState != null && bytesRead > 0)
            {
                lock (StateUpdateLock)
                {
                    _keepAlivePingState.OnDataReceived();
                }
            }
        }

        private void ThrowIfDisposedOrKeepAliveFaulted()
            => ThrowIfInvalidStateOrKeepAliveFaulted(validStates: null);

        private void ThrowIfInvalidStateOrKeepAliveFaulted(WebSocketState[]? validStates)
        {
            Debug.Assert(_keepAlivePingState is not null);

            // Exception order: WebSocketException -> OperationCanceledException -> ObjectDisposedException
            //
            // If keepAlive exception present:
            //    1. WebSocketException(InvalidState), keepAlive exception as inner -- if invalid state
            //    2. OperationCanceledException, keepAlive exception as inner
            //
            // If keepAlive exception not present:
            //    1. WebSocketException(InvalidState) -- if invalid state
            //    2. ObjectDisposedException

            bool disposed;
            WebSocketState state;
            Exception? keepAliveException;
            lock (StateUpdateLock)
            {
                disposed = _disposed;
                state = _state;
                keepAliveException = _keepAlivePingState.Exception;
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, $"_disposed={disposed}, _state={state}, _keepAlivePingState.Exception={keepAliveException?.Message}");

            string? invalidStateMessage = validStates is not null ? WebSocketValidate.GetInvalidStateMessage(state, validStates) : null;
            if (invalidStateMessage is not null)
            {
                // Surface keepAliveException as inner exception, if present
                throw new WebSocketException(WebSocketError.InvalidState, invalidStateMessage, keepAliveException);
            }

            // If keepAliveException is not null, it triggered the abort which also disposed the websocket
            // We only save the exception if it actually triggered the abort
            if (keepAliveException is not null)
            {
                throw new OperationCanceledException(nameof(WebSocketState.Aborted), keepAliveException);
            }

            // Ordering is important to maintain .NET 4.5 WebSocket implementation exception behavior.
            ObjectDisposedException.ThrowIf(disposed, this);
        }

        private sealed class KeepAlivePingState
        {
            internal const int PingPayloadSize = sizeof(long);
            internal const int MinIntervalMs = 1;

            internal readonly int DelayMs;
            internal readonly int TimeoutMs;
            internal readonly int HeartBeatIntervalMs;

            internal long NextPingTimestamp;
            internal long WillTimeoutTimestamp;

            internal bool AwaitingPong;
            internal long PingPayload;
            internal Exception? Exception;

            internal object Debug_WebSocket_StateUpdateLock = null!; // for Debug.Asserts

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
                    (int)Math.Clamp((long)value.TotalMilliseconds, MinIntervalMs, int.MaxValue);
            }

            internal void OnDataReceived()
            {
                Debug.Assert(Monitor.IsEntered(Debug_WebSocket_StateUpdateLock));

                NextPingTimestamp = Environment.TickCount64 + DelayMs;
            }

            internal void OnPongResponseReceived(long pongPayload)
            {
                Debug.Assert(Monitor.IsEntered(Debug_WebSocket_StateUpdateLock));

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, $"pongPayload={pongPayload}");

                if (!AwaitingPong)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, $"Not waiting for Pong. Skipping.");
                    return;
                }

                if (pongPayload == PingPayload)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.PongResponseReceived(this, pongPayload);

                    WillTimeoutTimestamp = Timeout.Infinite;
                    AwaitingPong = false;
                }
                else
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, $"Expected payload {PingPayload}. Skipping.");
                }
            }
        }
    }
}
