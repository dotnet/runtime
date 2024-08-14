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

            bool shouldSendPing = false;
            long pingPayload = -1;

            try
            {
                lock (StateUpdateLock)
                {
                    if (_keepAlivePingState.Exception is not null)
                    {
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, $"KeepAlive already faulted, skipping... (exception: {_keepAlivePingState.Exception.Message})");
                        return;
                    }

                    long now = Environment.TickCount64;

                    if (_keepAlivePingState.PingSent)
                    {
                        if (Environment.TickCount64 > _keepAlivePingState.PingTimeoutTimestamp)
                        {
                            if (NetEventSource.Log.IsEnabled())
                            {
                                NetEventSource.Trace(this, $"Keep-alive ping timed out after {_keepAlivePingState.TimeoutMs}ms. Expected pong with payload {_keepAlivePingState.PingPayload}");
                            }

                            Exception exc = ExceptionDispatchInfo.SetCurrentStackTrace(
                                new WebSocketException(WebSocketError.Faulted, SR.net_Websockets_KeepAlivePingTimeout));

                            _keepAlivePingState.OnKeepAliveFaultedCore(exc); // we are holding the lock
                            return;
                        }
                    }
                    else
                    {
                        if (Environment.TickCount64 > _keepAlivePingState.NextPingRequestTimestamp)
                        {
                            _keepAlivePingState.OnNextPingRequestCore(); // we are holding the lock
                            shouldSendPing = true;
                            pingPayload = _keepAlivePingState.PingPayload;
                        }
                    }
                }

                if (shouldSendPing)
                {
                    Observe(
                        SendPingAsync(pingPayload));
                }
            }
            catch (Exception e)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.TraceException(this, e);

                _keepAlivePingState.OnKeepAliveFaulted(e);
            }
        }

        private async ValueTask SendPingAsync(long pingPayload)
        {
            Debug.Assert(_keepAlivePingState != null);

            byte[] pingPayloadBuffer = ArrayPool<byte>.Shared.Rent(sizeof(long));
            BinaryPrimitives.WriteInt64BigEndian(pingPayloadBuffer, pingPayload);
            try
            {
                await TrySendKeepAliveFrameAsync(MessageOpcode.Ping, pingPayloadBuffer.AsMemory(0, sizeof(long))).ConfigureAwait(false);

                if (NetEventSource.Log.IsEnabled()) NetEventSource.KeepAlivePingSent(this, pingPayload);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(pingPayloadBuffer);
            }
        }

        // "Observe" either a ValueTask result, or any exception, ignoring it
        // to prevent the unobserved exception event from being raised.
        private void Observe(ValueTask t)
        {
            if (t.IsCompletedSuccessfully)
            {
                t.GetAwaiter().GetResult();
            }
            else
            {
                Observe(t.AsTask());
            }
        }

        // "Observe" any exception, ignoring it to prevent the unobserved task
        // exception event from being raised.
        private void Observe(Task t)
        {
            if (t.IsCompleted)
            {
                if (t.IsFaulted)
                {
                    LogFaulted(t, this);
                }
            }
            else
            {
                t.ContinueWith(
                    LogFaulted,
                    this,
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }

            static void LogFaulted(Task task, object? thisObj)
            {
                Debug.Assert(task.IsFaulted);

                // accessing exception to observe it regardless of whether the tracing is enabled
                Exception e = task.Exception!.InnerException!;

                if (NetEventSource.Log.IsEnabled()) NetEventSource.TraceException(thisObj, e);
            }
        }

        private sealed class KeepAlivePingState
        {
            internal const int PingPayloadSize = sizeof(long);
            private const int MinIntervalMs = 1;

            private readonly ManagedWebSocket _parent;
            private object StateUpdateLock => _parent.StateUpdateLock;

            internal int DelayMs { get; }
            internal int TimeoutMs { get; }
            internal int HeartBeatIntervalMs => Math.Max(Math.Min(DelayMs, TimeoutMs) / 4, MinIntervalMs);

            internal long PingPayload { get; private set; }
            internal bool PingSent { get; private set; }
            internal long PingTimeoutTimestamp { get; private set; }
            internal long NextPingRequestTimestamp { get; private set; }
            internal Exception? Exception { get; private set; }

            public KeepAlivePingState(TimeSpan keepAliveInterval, TimeSpan keepAliveTimeout, ManagedWebSocket parent)
            {
                DelayMs = TimeSpanToMs(keepAliveInterval);
                TimeoutMs = TimeSpanToMs(keepAliveTimeout);
                NextPingRequestTimestamp = Environment.TickCount64 + DelayMs;
                PingTimeoutTimestamp = Timeout.Infinite;
                _parent = parent;

                static int TimeSpanToMs(TimeSpan value) => (int)Math.Clamp((long)value.TotalMilliseconds, MinIntervalMs, int.MaxValue);
            }

            internal void OnDataReceived()
            {
                lock (StateUpdateLock)
                {
                    NextPingRequestTimestamp = Environment.TickCount64 + DelayMs;
                }
            }

            internal void OnPongResponseReceived(long pongPayload)
            {
                lock (StateUpdateLock)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, $"pongPayload={pongPayload}");

                    if (!PingSent)
                    {
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, $"Not waiting for Pong. Skipping.");
                        return;
                    }

                    if (pongPayload == PingPayload)
                    {
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.PongResponseReceived(this, pongPayload);

                        PingTimeoutTimestamp = long.MaxValue;
                        PingSent = false;
                    }
                    else
                    {
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, $"Expected payload {PingPayload}. Skipping.");
                    }
                }
            }

            internal void OnNextPingRequestCore()
            {
                Debug.Assert(Monitor.IsEntered(StateUpdateLock));

                PingSent = true;
                PingTimeoutTimestamp = Environment.TickCount64 + TimeoutMs;
                ++PingPayload;
            }

            internal void OnKeepAliveFaulted(Exception exc)
            {
                lock (StateUpdateLock)
                {
                    OnKeepAliveFaultedCore(exc);
                }
            }

            internal void OnKeepAliveFaultedCore(Exception exc)
            {
                Debug.Assert(Monitor.IsEntered(StateUpdateLock));

                if (NetEventSource.Log.IsEnabled()) NetEventSource.TraceErrorMsg(this, exc);

                if (_parent._disposed)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, $"WebSocket already disposed, skipping...");
                    return;
                }

                if (_parent.State is WebSocketState.Closed)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, $"WebSocket is already closed, skipping...");
                    // We've transferred into the Closed state, but didn't dispose yet
                    // This can happen in e.g. HandleReceivedCloseAsync where we first change the state
                    // but then still do some operations with the stream.
                    // No need to do anything as we've already completed the Closing Handshake
                    return;
                }

                if (_parent.State is WebSocketState.Aborted)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Trace(this, $"WebSocket is already aborted, skipping...");
                    // Something else already aborted the websocket, but didn't dispose it (yet?)?
                    // This can happen either
                    //  (1) in the Abort() method, e.g. on cancellation, if we interjected between the state
                    //      change and the Dispose() call; or
                    //  (2) in the catch block of ReceiveAsyncPrivate (which doesn't do the dispose after??).
                    //      This most possibly happens if we've hit a premature EOF from the server.
                    // Websocket is not usable in the Aborted state anyway, so let's free the resources while we're at it?
                    _parent.Dispose();
                    return;
                }

                // we were the ones who triggered the abort, let's save the exception
                Exception = exc;

                _parent.OnAbortedCore();
                _parent.DisposeCore();
            }
        }
    }
}
