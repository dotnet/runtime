// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Quic.Implementations.MsQuic.Internal.MsQuicNativeMethods;

namespace System.Net.Quic.Implementations.MsQuic
{
    internal sealed class MsQuicStream : QuicStreamProvider
    {
        // Delegate that wraps the static function that will be called when receiving an event.
        internal static readonly StreamCallbackDelegate s_streamDelegate = new StreamCallbackDelegate(NativeCallbackHandler);

        private readonly State _state = new State();

        // Backing for StreamId
        private long _streamId = -1;

        // Used to check if StartAsync has been called.
        private bool _started;

        private int _disposed;

        private sealed class State
        {
            public SafeMsQuicStreamHandle Handle = null!; // set in ctor.
            public GCHandle StateGCHandle;
            public MsQuicConnection.State ConnectionState = null!; // set in ctor.

            public ReadState ReadState;
            public long ReadErrorCode = -1;
            public readonly List<QuicBuffer> ReceiveQuicBuffers = new List<QuicBuffer>();

            // Resettable completions to be used for multiple calls to receive.
            public readonly ResettableCompletionSource<uint> ReceiveResettableCompletionSource = new ResettableCompletionSource<uint>();

            public SendState SendState;
            public long SendErrorCode = -1;

            // Buffers to hold during a call to send.
            public MemoryHandle[] BufferArrays = new MemoryHandle[1];
            public IntPtr SendQuicBuffers;
            public int SendBufferMaxCount;
            public int SendBufferCount;

            // Resettable completions to be used for multiple calls to send, start, and shutdown.
            public readonly ResettableCompletionSource<uint> SendResettableCompletionSource = new ResettableCompletionSource<uint>();

            public ShutdownWriteState ShutdownWriteState;

            // Set once writes have been shutdown.
            public readonly TaskCompletionSource ShutdownWriteCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            public ShutdownState ShutdownState;
            public int ShutdownDone;

            // Set once stream have been shutdown.
            public readonly TaskCompletionSource ShutdownCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            public void Cleanup()
            {
                ShutdownState = ShutdownState.Finished;
                CleanupSendState(this);
                Handle?.Dispose();
                Marshal.FreeHGlobal(SendQuicBuffers);
                SendQuicBuffers = IntPtr.Zero;
                if (StateGCHandle.IsAllocated) StateGCHandle.Free();
                ConnectionState?.RemoveStream(null);
            }
        }

        // inbound.
        internal MsQuicStream(MsQuicConnection.State connectionState, SafeMsQuicStreamHandle streamHandle, QUIC_STREAM_OPEN_FLAGS flags)
        {
            _state.Handle = streamHandle;
            _started = true;
            if (flags.HasFlag(QUIC_STREAM_OPEN_FLAGS.UNIDIRECTIONAL))
            {
                _state.SendState = SendState.Closed;
            }

            _state.StateGCHandle = GCHandle.Alloc(_state);
            try
            {
                MsQuicApi.Api.SetCallbackHandlerDelegate(
                    _state.Handle,
                    s_streamDelegate,
                    GCHandle.ToIntPtr(_state.StateGCHandle));
            }
            catch
            {
                _state.StateGCHandle.Free();
                throw;
            }

            if (!connectionState.TryAddStream(this))
            {
                _state.StateGCHandle.Free();
                throw new ObjectDisposedException(nameof(QuicConnection));
            }

            _state.ConnectionState = connectionState;

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(
                    _state,
                    $"[Stream#{_state.GetHashCode()}] inbound {(flags.HasFlag(QUIC_STREAM_OPEN_FLAGS.UNIDIRECTIONAL) ? "uni" : "bi")}directional stream created " +
                        $"in Connection#{_state.ConnectionState.GetHashCode()}.");
            }
        }

        // outbound.
        internal MsQuicStream(MsQuicConnection.State connectionState, QUIC_STREAM_OPEN_FLAGS flags)
        {
            Debug.Assert(connectionState.Handle != null);

            _state.StateGCHandle = GCHandle.Alloc(_state);
            if (flags.HasFlag(QUIC_STREAM_OPEN_FLAGS.UNIDIRECTIONAL))
            {
                _state.ReadState = ReadState.Closed;
            }

            try
            {
                uint status = MsQuicApi.Api.StreamOpenDelegate(
                    connectionState.Handle,
                    flags,
                    s_streamDelegate,
                    GCHandle.ToIntPtr(_state.StateGCHandle),
                    out _state.Handle);

                QuicExceptionHelpers.ThrowIfFailed(status, "Failed to open stream to peer.");

                status = MsQuicApi.Api.StreamStartDelegate(_state.Handle, QUIC_STREAM_START_FLAGS.FAIL_BLOCKED);
                QuicExceptionHelpers.ThrowIfFailed(status, "Could not start stream.");
            }
            catch
            {
                _state.Handle?.Dispose();
                _state.StateGCHandle.Free();
                throw;
            }

            if (!connectionState.TryAddStream(this))
            {
                _state.Handle?.Dispose();
                _state.StateGCHandle.Free();
                throw new ObjectDisposedException(nameof(QuicConnection));
            }

            _state.ConnectionState = connectionState;

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(
                    _state,
                    $"[Stream#{_state.GetHashCode()}] outbound {(flags.HasFlag(QUIC_STREAM_OPEN_FLAGS.UNIDIRECTIONAL) ? "uni" : "bi")}directional stream created " +
                        $"in Connection#{_state.ConnectionState.GetHashCode()}.");
            }
        }

        internal override bool CanRead => _disposed == 0 && _state.ReadState < ReadState.Aborted;

        internal override bool CanWrite => _disposed == 0 && _state.SendState < SendState.Aborted;

        internal override long StreamId
        {
            get
            {
                ThrowIfDisposed();

                if (_streamId == -1)
                {
                    _streamId = GetStreamId();
                }

                return _streamId;
            }
        }

        internal override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return WriteAsync(buffer, endStream: false, cancellationToken);
        }

        internal override ValueTask WriteAsync(ReadOnlySequence<byte> buffers, CancellationToken cancellationToken = default)
        {
            return WriteAsync(buffers, endStream: false, cancellationToken);
        }

        internal override async ValueTask WriteAsync(ReadOnlySequence<byte> buffers, bool endStream, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            using CancellationTokenRegistration registration = await HandleWriteStartState(cancellationToken).ConfigureAwait(false);

            await SendReadOnlySequenceAsync(buffers, endStream ? QUIC_SEND_FLAGS.FIN : QUIC_SEND_FLAGS.NONE).ConfigureAwait(false);

            HandleWriteCompletedState();
        }

        internal override ValueTask WriteAsync(ReadOnlyMemory<ReadOnlyMemory<byte>> buffers, CancellationToken cancellationToken = default)
        {
            return WriteAsync(buffers, endStream: false, cancellationToken);
        }

        internal override async ValueTask WriteAsync(ReadOnlyMemory<ReadOnlyMemory<byte>> buffers, bool endStream, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            using CancellationTokenRegistration registration = await HandleWriteStartState(cancellationToken).ConfigureAwait(false);

            await SendReadOnlyMemoryListAsync(buffers, endStream ? QUIC_SEND_FLAGS.FIN : QUIC_SEND_FLAGS.NONE).ConfigureAwait(false);

            HandleWriteCompletedState();
        }

        internal override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, bool endStream, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            using CancellationTokenRegistration registration = await HandleWriteStartState(cancellationToken).ConfigureAwait(false);

            await SendReadOnlyMemoryAsync(buffer, endStream ? QUIC_SEND_FLAGS.FIN : QUIC_SEND_FLAGS.NONE).ConfigureAwait(false);

            HandleWriteCompletedState();
        }

        private async ValueTask<CancellationTokenRegistration> HandleWriteStartState(CancellationToken cancellationToken)
        {
            if (_state.SendState == SendState.Closed)
            {
                throw new InvalidOperationException(SR.net_quic_writing_notallowed);
            }
            else if ( _state.SendState == SendState.Aborted)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                lock (_state)
                {
                    if (_state.SendState == SendState.None || _state.SendState == SendState.Pending)
                    {
                        _state.SendState = SendState.Aborted;
                    }
                }

                throw new System.OperationCanceledException(cancellationToken);
            }

            // Make sure start has completed
            if (!_started)
            {
                await _state.SendResettableCompletionSource.GetTypelessValueTask().ConfigureAwait(false);
                _started = true;
            }

            // if token was already cancelled, this would execute synchronously
            CancellationTokenRegistration registration = cancellationToken.UnsafeRegister(static (s, token) =>
            {
                var state = (State)s!;
                bool shouldComplete = false;

                lock (state)
                {
                    if (state.SendState == SendState.None || state.SendState == SendState.Pending)
                    {
                        state.SendState = SendState.Aborted;
                        shouldComplete = true;
                    }
                }

                if (shouldComplete)
                {
                    state.SendResettableCompletionSource.CompleteException(
                        ExceptionDispatchInfo.SetCurrentStackTrace(new OperationCanceledException("Write was canceled", token)));
                }
            }, _state);

            lock (_state)
            {
                if (_state.SendState == SendState.Aborted)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    throw new OperationCanceledException(SR.net_quic_sending_aborted);
                }
                else if (_state.SendState == SendState.ConnectionClosed)
                {
                    throw GetConnectionAbortedException(_state);
                }
            }

            return registration;
        }

        private void HandleWriteCompletedState()
        {
            lock (_state)
            {
                if (_state.SendState == SendState.Finished)
                {
                    _state.SendState = SendState.None;
                }
            }
        }

        private void HandleWriteFailedState()
        {
            lock (_state)
            {
                if (_state.SendState == SendState.Pending)
                {
                    _state.SendState = SendState.Finished;
                }
            }
        }

        internal override async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (_state.ReadState == ReadState.Closed)
            {
                throw new InvalidOperationException(SR.net_quic_reading_notallowed);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                lock (_state)
                {
                    if (_state.ReadState == ReadState.None)
                    {
                        _state.ReadState = ReadState.Aborted;
                    }
                }

                throw new System.OperationCanceledException(cancellationToken);
            }

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(_state, $"[Stream#{_state.GetHashCode()}] reading into Memory of '{destination.Length}' bytes.");
            }

            lock (_state)
            {
                if (_state.ReadState == ReadState.ReadsCompleted)
                {
                    return 0;
                }
                else if (_state.ReadState == ReadState.Aborted)
                {
                    throw ThrowHelper.GetStreamAbortedException(_state.ReadErrorCode);
                }
                else if (_state.ReadState == ReadState.ConnectionClosed)
                {
                    throw GetConnectionAbortedException(_state);
                }
            }

            using CancellationTokenRegistration registration = cancellationToken.UnsafeRegister(static (s, token) =>
            {
                var state = (State)s!;
                bool shouldComplete = false;
                lock (state)
                {
                    if (state.ReadState == ReadState.None)
                    {
                        shouldComplete = true;
                    }
                    state.ReadState = ReadState.Aborted;
                }

                if (shouldComplete)
                {
                    state.ReceiveResettableCompletionSource.CompleteException(
                        ExceptionDispatchInfo.SetCurrentStackTrace(new OperationCanceledException("Read was canceled", token)));
                }
            }, _state);

            // TODO there could potentially be a perf gain by storing the buffer from the initial read
            // This reduces the amount of async calls, however it makes it so MsQuic holds onto the buffers
            // longer than it needs to. We will need to benchmark this.
            int length = (int)await _state.ReceiveResettableCompletionSource.GetValueTask().ConfigureAwait(false);

            int actual = Math.Min(length, destination.Length);

            static unsafe void CopyToBuffer(Span<byte> destinationBuffer, List<QuicBuffer> sourceBuffers)
            {
                Span<byte> slicedBuffer = destinationBuffer;
                for (int i = 0; i < sourceBuffers.Count; i++)
                {
                    QuicBuffer nativeBuffer = sourceBuffers[i];
                    int length = Math.Min((int)nativeBuffer.Length, slicedBuffer.Length);
                    new Span<byte>(nativeBuffer.Buffer, length).CopyTo(slicedBuffer);
                    if (length < nativeBuffer.Length)
                    {
                        // The buffer passed in was larger that the received data, return
                        return;
                    }
                    slicedBuffer = slicedBuffer.Slice(length);
                }
            }

            CopyToBuffer(destination.Span, _state.ReceiveQuicBuffers);

            lock (_state)
            {
                if (_state.ReadState == ReadState.IndividualReadComplete)
                {
                    _state.ReceiveQuicBuffers.Clear();
                    ReceiveComplete(actual);
                    EnableReceive();
                    _state.ReadState = ReadState.None;
                }
            }

            return actual;
        }

        // TODO do we want this to be a synchronization mechanism to cancel a pending read
        // If so, we need to complete the read here as well.
        internal override void AbortRead(long errorCode)
        {
            ThrowIfDisposed();

            lock (_state)
            {
                _state.ReadState = ReadState.Aborted;
            }

            StartShutdown(QUIC_STREAM_SHUTDOWN_FLAGS.ABORT_RECEIVE, errorCode);
        }

        internal override void AbortWrite(long errorCode)
        {
            ThrowIfDisposed();

            bool shouldComplete = false;

            lock (_state)
            {
                if (_state.SendState < SendState.Aborted)
                {
                    _state.SendState = SendState.Aborted;
                }

                if (_state.ShutdownWriteState == ShutdownWriteState.None)
                {
                    _state.ShutdownWriteState = ShutdownWriteState.Canceled;
                    shouldComplete = true;
                }
            }

            if (shouldComplete)
            {
                _state.ShutdownWriteCompletionSource.SetException(
                    ExceptionDispatchInfo.SetCurrentStackTrace(new QuicStreamAbortedException("Shutdown was aborted.", errorCode)));
            }

            StartShutdown(QUIC_STREAM_SHUTDOWN_FLAGS.ABORT_SEND, errorCode);
        }

        private void StartShutdown(QUIC_STREAM_SHUTDOWN_FLAGS flags, long errorCode)
        {
            uint status = MsQuicApi.Api.StreamShutdownDelegate(_state.Handle, flags, errorCode);
            QuicExceptionHelpers.ThrowIfFailed(status, "StreamShutdown failed.");
        }

        internal override async ValueTask ShutdownWriteCompleted(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            lock (_state)
            {
                if (_state.ShutdownWriteState == ShutdownWriteState.ConnectionClosed)
                {
                    throw GetConnectionAbortedException(_state);
                }
            }

            // TODO do anything to stop writes?
            using CancellationTokenRegistration registration = cancellationToken.UnsafeRegister(static (s, token) =>
            {
                var state = (State)s!;
                bool shouldComplete = false;
                lock (state)
                {
                    if (state.ShutdownWriteState == ShutdownWriteState.None)
                    {
                        state.ShutdownWriteState = ShutdownWriteState.Canceled; // TODO: should we separate states for cancelling here vs calling Abort?
                        shouldComplete = true;
                    }
                }

                if (shouldComplete)
                {
                    state.ShutdownWriteCompletionSource.SetException(
                        ExceptionDispatchInfo.SetCurrentStackTrace(new OperationCanceledException("Wait for shutdown write was canceled", token)));
                }
            }, _state);

            await _state.ShutdownWriteCompletionSource.Task.ConfigureAwait(false);
        }

        internal override async ValueTask ShutdownCompleted(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            lock (_state)
            {
                if (_state.ShutdownState == ShutdownState.ConnectionClosed)
                {
                    throw GetConnectionAbortedException(_state);
                }
            }

            // TODO do anything to stop writes?
            using CancellationTokenRegistration registration = cancellationToken.UnsafeRegister(static (s, token) =>
            {
                var state = (State)s!;
                bool shouldComplete = false;
                lock (state)
                {
                    if (state.ShutdownState == ShutdownState.None)
                    {
                        state.ShutdownState = ShutdownState.Canceled;
                        shouldComplete = true;
                    }
                }

                if (shouldComplete)
                {
                    state.ShutdownWriteCompletionSource.SetException(
                        ExceptionDispatchInfo.SetCurrentStackTrace(new OperationCanceledException("Wait for shutdown was canceled", token)));
                }
            }, _state);

            await _state.ShutdownCompletionSource.Task.ConfigureAwait(false);
        }

        internal override void Shutdown()
        {
            ThrowIfDisposed();

            lock (_state)
            {
                _state.SendState = SendState.Finished;
            }

            // it is ok to send shutdown several times, MsQuic will ignore it
            StartShutdown(QUIC_STREAM_SHUTDOWN_FLAGS.GRACEFUL, errorCode: 0);
        }

        // TODO consider removing sync-over-async with blocking calls.
        internal override int Read(Span<byte> buffer)
        {
            ThrowIfDisposed();
            byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
            try
            {
                int readLength = ReadAsync(new Memory<byte>(rentedBuffer, 0, buffer.Length)).AsTask().GetAwaiter().GetResult();
                rentedBuffer.AsSpan(0, readLength).CopyTo(buffer);
                return readLength;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        }

        internal override void Write(ReadOnlySpan<byte> buffer)
        {
            ThrowIfDisposed();

            // TODO: optimize this.
            WriteAsync(buffer.ToArray()).AsTask().GetAwaiter().GetResult();
        }

        // MsQuic doesn't support explicit flushing
        internal override void Flush()
        {
            ThrowIfDisposed();
        }

        // MsQuic doesn't support explicit flushing
        internal override Task FlushAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            return Task.CompletedTask;
        }

        public override ValueTask DisposeAsync()
        {
            // TODO: perform a graceful shutdown and wait for completion?

            Dispose(true);
            return default;
        }

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MsQuicStream()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            int disposed = Interlocked.Exchange(ref _disposed, 1);
            if (disposed != 0)
            {
                return;
            }

            bool callShutdown = false;
            bool abortRead = false;
            bool releaseHandles = false;
            lock (_state)
            {
                if (_state.SendState < SendState.Aborted)
                {
                    callShutdown = true;
                }

                if (_state.ReadState < ReadState.ReadsCompleted)
                {
                    abortRead = true;
                    _state.ReadState = ReadState.Aborted;
                }

                if (_state.ShutdownState == ShutdownState.None)
                {
                    _state.ShutdownState = ShutdownState.Pending;
                }

                // Check if we already got final event.
                releaseHandles = Interlocked.Exchange(ref _state.ShutdownDone, 1) == 2;
                if (releaseHandles)
                {
                    _state.ShutdownState = ShutdownState.Finished;
                }
            }

            if (callShutdown)
            {
                try
                {
                    // Handle race condition when stream can be closed handling SHUTDOWN_COMPLETE.
                    StartShutdown(QUIC_STREAM_SHUTDOWN_FLAGS.GRACEFUL, errorCode: 0);
                } catch (ObjectDisposedException) { };
            }

            if (abortRead)
            {
                try
                {
                    StartShutdown(QUIC_STREAM_SHUTDOWN_FLAGS.ABORT_RECEIVE, 0xffffffff);
                } catch (ObjectDisposedException) { };
            }

            if (releaseHandles)
            {
                _state.Cleanup();
            }

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(_state, $"[Stream#{_state.GetHashCode()}] disposed");
            }
        }

        private void EnableReceive()
        {
            MsQuicApi.Api.StreamReceiveSetEnabledDelegate(_state.Handle, enabled: true);
        }

        private static uint NativeCallbackHandler(
            IntPtr stream,
            IntPtr context,
            ref StreamEvent streamEvent)
        {
            var state = (State)GCHandle.FromIntPtr(context).Target!;
            return HandleEvent(state, ref streamEvent);
        }

        private static uint HandleEvent(State state, ref StreamEvent evt)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(state, $"[Stream#{state.GetHashCode()}] received event {evt.Type}");
            }

            try
            {
                switch ((QUIC_STREAM_EVENT_TYPE)evt.Type)
                {
                    // Stream has started.
                    // Will only be done for outbound streams (inbound streams have already started)
                    case QUIC_STREAM_EVENT_TYPE.START_COMPLETE:
                        return HandleEventStartComplete(state);
                    // Received data on the stream
                    case QUIC_STREAM_EVENT_TYPE.RECEIVE:
                        return HandleEventRecv(state, ref evt);
                    // Send has completed.
                    // Contains a canceled bool to indicate if the send was canceled.
                    case QUIC_STREAM_EVENT_TYPE.SEND_COMPLETE:
                        return HandleEventSendComplete(state, ref evt);
                    // Peer has told us to shutdown the reading side of the stream.
                    case QUIC_STREAM_EVENT_TYPE.PEER_SEND_SHUTDOWN:
                        return HandleEventPeerSendShutdown(state);
                    // Peer has told us to abort the reading side of the stream.
                    case QUIC_STREAM_EVENT_TYPE.PEER_SEND_ABORTED:
                        return HandleEventPeerSendAborted(state, ref evt);
                    // Peer has stopped receiving data, don't send anymore.
                    case QUIC_STREAM_EVENT_TYPE.PEER_RECEIVE_ABORTED:
                        return HandleEventPeerRecvAborted(state, ref evt);
                    // Occurs when shutdown is completed for the send side.
                    // This only happens for shutdown on sending, not receiving
                    // Receive shutdown can only be abortive.
                    case QUIC_STREAM_EVENT_TYPE.SEND_SHUTDOWN_COMPLETE:
                        return HandleEventSendShutdownComplete(state, ref evt);
                    // Shutdown for both sending and receiving is completed.
                    case QUIC_STREAM_EVENT_TYPE.SHUTDOWN_COMPLETE:
                        return HandleEventShutdownComplete(state, ref evt);
                    default:
                        return MsQuicStatusCodes.Success;
                }
            }
            catch (Exception ex)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Error(state, $"[Stream#{state.GetHashCode()}] Exception occurred during handling {(QUIC_STREAM_EVENT_TYPE)evt.Type} event: {ex.Message}");
                }

                return MsQuicStatusCodes.InternalError;
            }
        }

        private static unsafe uint HandleEventRecv(State state, ref StreamEvent evt)
        {
            StreamEventDataReceive receiveEvent = evt.Data.Receive;
            for (int i = 0; i < receiveEvent.BufferCount; i++)
            {
                state.ReceiveQuicBuffers.Add(receiveEvent.Buffers[i]);
            }

            bool shouldComplete = false;
            lock (state)
            {
                if (state.ReadState == ReadState.None)
                {
                    shouldComplete = true;
                }
                if (state.ReadState != ReadState.ConnectionClosed)
                {
                    state.ReadState = ReadState.IndividualReadComplete;
                }
            }

            if (shouldComplete)
            {
                state.ReceiveResettableCompletionSource.Complete((uint)receiveEvent.TotalBufferLength);
            }

            return MsQuicStatusCodes.Pending;
        }

        private static uint HandleEventPeerRecvAborted(State state, ref StreamEvent evt)
        {
            bool shouldComplete = false;
            lock (state)
            {
                if (state.SendState == SendState.None || state.SendState == SendState.Pending)
                {
                    shouldComplete = true;
                }
                state.SendState = SendState.Aborted;
                state.SendErrorCode = (long)evt.Data.PeerSendAborted.ErrorCode;
            }

            if (shouldComplete)
            {
                state.SendResettableCompletionSource.CompleteException(
                    ExceptionDispatchInfo.SetCurrentStackTrace(new QuicStreamAbortedException(state.SendErrorCode)));
            }

            return MsQuicStatusCodes.Success;
        }

        private static uint HandleEventStartComplete(State state)
        {
            bool shouldComplete = false;
            lock (state)
            {
                // Check send state before completing as send cancellation is shared between start and send.
                if (state.SendState == SendState.None)
                {
                    shouldComplete = true;
                }
            }

            if (shouldComplete)
            {
                state.SendResettableCompletionSource.Complete(MsQuicStatusCodes.Success);
            }

            return MsQuicStatusCodes.Success;
        }

        private static uint HandleEventSendShutdownComplete(State state, ref StreamEvent evt)
        {
            bool shouldComplete = false;
            lock (state)
            {
                if (state.ShutdownWriteState == ShutdownWriteState.None)
                {
                    state.ShutdownWriteState = ShutdownWriteState.Finished;
                    shouldComplete = true;
                }
            }

            if (shouldComplete)
            {
                state.ShutdownWriteCompletionSource.SetResult();
            }

            return MsQuicStatusCodes.Success;
        }

        private static uint HandleEventShutdownComplete(State state, ref StreamEvent evt)
        {
            StreamEventDataShutdownComplete shutdownCompleteEvent = evt.Data.ShutdownComplete;

            if (shutdownCompleteEvent.ConnectionShutdown != 0)
            {
                return HandleEventConnectionClose(state);
            }

            bool shouldReadComplete = false;
            bool shouldShutdownWriteComplete = false;
            bool shouldShutdownComplete = false;

            lock (state)
            {
                // This event won't occur within the middle of a receive.
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(state, $"[Stream#{state.GetHashCode()}] completing resettable event source.");

                if (state.ReadState == ReadState.None)
                {
                    shouldReadComplete = true;
                }

                if (state.ReadState != ReadState.ConnectionClosed)
                {
                    state.ReadState = ReadState.ReadsCompleted;
                }

                if (state.ShutdownWriteState == ShutdownWriteState.None)
                {
                    state.ShutdownWriteState = ShutdownWriteState.Finished;
                    shouldShutdownWriteComplete = true;
                }

                if (state.ShutdownState == ShutdownState.None)
                {
                    state.ShutdownState = ShutdownState.Finished;
                    shouldShutdownComplete = true;
                }
            }

            if (shouldReadComplete)
            {
                state.ReceiveResettableCompletionSource.Complete(0);
            }

            if (shouldShutdownWriteComplete)
            {
                state.ShutdownWriteCompletionSource.SetResult();
            }

            if (shouldShutdownComplete)
            {
                state.ShutdownCompletionSource.SetResult();
            }

            // Dispose was called before complete event.
            bool releaseHandles = Interlocked.Exchange(ref state.ShutdownDone, 2) == 1;
            if (releaseHandles)
            {
                state.Cleanup();
            }

            return MsQuicStatusCodes.Success;
        }

        private static uint HandleEventPeerSendAborted(State state, ref StreamEvent evt)
        {
            bool shouldComplete = false;
            lock (state)
            {
                if (state.ReadState == ReadState.None)
                {
                    shouldComplete = true;
                }
                state.ReadState = ReadState.Aborted;
                state.ReadErrorCode = (long)evt.Data.PeerSendAborted.ErrorCode;
            }

            if (shouldComplete)
            {
                state.ReceiveResettableCompletionSource.CompleteException(
                    ExceptionDispatchInfo.SetCurrentStackTrace(new QuicStreamAbortedException(state.ReadErrorCode)));
            }

            return MsQuicStatusCodes.Success;
        }

        private static uint HandleEventPeerSendShutdown(State state)
        {
            bool shouldComplete = false;

            lock (state)
            {
                // This event won't occur within the middle of a receive.
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(state, $"[Stream#{state.GetHashCode()}] completing resettable event source.");

                if (state.ReadState == ReadState.None)
                {
                    shouldComplete = true;
                }

                if (state.ReadState != ReadState.ConnectionClosed)
                {
                    state.ReadState = ReadState.ReadsCompleted;
                }
            }

            if (shouldComplete)
            {
                state.ReceiveResettableCompletionSource.Complete(0);
            }

            return MsQuicStatusCodes.Success;
        }

        private static uint HandleEventSendComplete(State state, ref StreamEvent evt)
        {
            StreamEventDataSendComplete sendCompleteEvent = evt.Data.SendComplete;
            bool canceled = sendCompleteEvent.Canceled != 0;

            bool complete = false;

            lock (state)
            {
                if (state.SendState == SendState.Pending)
                {
                    state.SendState = SendState.Finished;
                    complete = true;
                }

                if (canceled)
                {
                    state.SendState = SendState.Aborted;
                }
            }

            if (complete)
            {
                CleanupSendState(state);

                if (!canceled)
                {
                    state.SendResettableCompletionSource.Complete(MsQuicStatusCodes.Success);
                }
                else
                {
                    state.SendResettableCompletionSource.CompleteException(
                        ExceptionDispatchInfo.SetCurrentStackTrace(new OperationCanceledException("Write was canceled")));
                }
            }

            return MsQuicStatusCodes.Success;
        }

        private static void CleanupSendState(State state)
        {
            lock (state)
            {
                Debug.Assert(state.SendState != SendState.Pending);
                Debug.Assert(state.SendBufferCount <= state.BufferArrays.Length);

                for (int i = 0; i < state.SendBufferCount; i++)
                {
                    state.BufferArrays[i].Dispose();
                }
            }
        }

        // TODO prevent overlapping sends or consider supporting it.
        private unsafe ValueTask SendReadOnlyMemoryAsync(
           ReadOnlyMemory<byte> buffer,
           QUIC_SEND_FLAGS flags)
        {
            lock (_state)
            {
                Debug.Assert(_state.SendState != SendState.Pending);
                _state.SendState = buffer.IsEmpty ? SendState.Finished : SendState.Pending;
            }

            if (buffer.IsEmpty)
            {
                if ((flags & QUIC_SEND_FLAGS.FIN) == QUIC_SEND_FLAGS.FIN)
                {
                    // Start graceful shutdown sequence if passed in the fin flag and there is an empty buffer.
                    StartShutdown(QUIC_STREAM_SHUTDOWN_FLAGS.GRACEFUL, errorCode: 0);
                }
                return default;
            }

            MemoryHandle handle = buffer.Pin();
            if (_state.SendQuicBuffers == IntPtr.Zero)
            {
                _state.SendQuicBuffers = Marshal.AllocHGlobal(sizeof(QuicBuffer));
                _state.SendBufferMaxCount = 1;
            }

            QuicBuffer* quicBuffers = (QuicBuffer*)_state.SendQuicBuffers;
            quicBuffers->Length = (uint)buffer.Length;
            quicBuffers->Buffer = (byte*)handle.Pointer;

            _state.BufferArrays[0] = handle;
            _state.SendBufferCount = 1;

            uint status = MsQuicApi.Api.StreamSendDelegate(
                _state.Handle,
                quicBuffers,
                bufferCount: 1,
                flags,
                IntPtr.Zero);

            if (!MsQuicStatusHelper.SuccessfulStatusCode(status))
            {
                HandleWriteFailedState();
                CleanupSendState(_state);

                // TODO this may need to be an aborted exception.
                QuicExceptionHelpers.ThrowIfFailed(status,
                    "Could not send data to peer.");
            }

            return _state.SendResettableCompletionSource.GetTypelessValueTask();
        }

        private unsafe ValueTask SendReadOnlySequenceAsync(
           ReadOnlySequence<byte> buffers,
           QUIC_SEND_FLAGS flags)
        {

            lock (_state)
            {
                Debug.Assert(_state.SendState != SendState.Pending);
                _state.SendState = buffers.IsEmpty ? SendState.Finished : SendState.Pending;
            }

            if (buffers.IsEmpty)
            {
                if ((flags & QUIC_SEND_FLAGS.FIN) == QUIC_SEND_FLAGS.FIN)
                {
                    // Start graceful shutdown sequence if passed in the fin flag and there is an empty buffer.
                    StartShutdown(QUIC_STREAM_SHUTDOWN_FLAGS.GRACEFUL, errorCode: 0);
                }
                return default;
            }

            int count = 0;

            foreach (ReadOnlyMemory<byte> buffer in buffers)
            {
                ++count;
            }

            if (_state.SendBufferMaxCount < count)
            {
                Marshal.FreeHGlobal(_state.SendQuicBuffers);
                _state.SendQuicBuffers = IntPtr.Zero;
                _state.SendQuicBuffers = Marshal.AllocHGlobal(sizeof(QuicBuffer) * count);
                _state.SendBufferMaxCount = count;
                _state.BufferArrays = new MemoryHandle[count];
            }

            _state.SendBufferCount = count;
            count = 0;

            QuicBuffer* quicBuffers = (QuicBuffer*)_state.SendQuicBuffers;
            foreach (ReadOnlyMemory<byte> buffer in buffers)
            {
                MemoryHandle handle = buffer.Pin();
                quicBuffers[count].Length = (uint)buffer.Length;
                quicBuffers[count].Buffer = (byte*)handle.Pointer;
                _state.BufferArrays[count] = handle;
                ++count;
            }

            uint status = MsQuicApi.Api.StreamSendDelegate(
                _state.Handle,
                quicBuffers,
                (uint)count,
                flags,
                IntPtr.Zero);

            if (!MsQuicStatusHelper.SuccessfulStatusCode(status))
            {
                HandleWriteFailedState();
                CleanupSendState(_state);

                // TODO this may need to be an aborted exception.
                QuicExceptionHelpers.ThrowIfFailed(status,
                    "Could not send data to peer.");
            }

            return _state.SendResettableCompletionSource.GetTypelessValueTask();
        }

        private unsafe ValueTask SendReadOnlyMemoryListAsync(
           ReadOnlyMemory<ReadOnlyMemory<byte>> buffers,
           QUIC_SEND_FLAGS flags)
        {
            lock (_state)
            {
                Debug.Assert(_state.SendState != SendState.Pending);
                _state.SendState = buffers.IsEmpty ? SendState.Finished : SendState.Pending;
            }

            if (buffers.IsEmpty)
            {
                if ((flags & QUIC_SEND_FLAGS.FIN) == QUIC_SEND_FLAGS.FIN)
                {
                    // Start graceful shutdown sequence if passed in the fin flag and there is an empty buffer.
                    StartShutdown(QUIC_STREAM_SHUTDOWN_FLAGS.GRACEFUL, errorCode: 0);
                }
                return default;
            }

            ReadOnlyMemory<byte>[] array = buffers.ToArray();

            uint length = (uint)array.Length;

            if (_state.SendBufferMaxCount < array.Length)
            {
                Marshal.FreeHGlobal(_state.SendQuicBuffers);
                _state.SendQuicBuffers = IntPtr.Zero;
                _state.SendQuicBuffers = Marshal.AllocHGlobal(sizeof(QuicBuffer) * array.Length);
                _state.SendBufferMaxCount = array.Length;
                _state.BufferArrays = new MemoryHandle[array.Length];
            }

            _state.SendBufferCount = array.Length;
            QuicBuffer* quicBuffers = (QuicBuffer*)_state.SendQuicBuffers;
            for (int i = 0; i < length; i++)
            {
                ReadOnlyMemory<byte> buffer = array[i];
                MemoryHandle handle = buffer.Pin();

                quicBuffers[i].Length = (uint)buffer.Length;
                quicBuffers[i].Buffer = (byte*)handle.Pointer;

                _state.BufferArrays[i] = handle;
            }

            uint status = MsQuicApi.Api.StreamSendDelegate(
                _state.Handle,
                quicBuffers,
                length,
                flags,
                IntPtr.Zero);

            if (!MsQuicStatusHelper.SuccessfulStatusCode(status))
            {
                HandleWriteFailedState();
                CleanupSendState(_state);

                // TODO this may need to be an aborted exception.
                QuicExceptionHelpers.ThrowIfFailed(status,
                    "Could not send data to peer.");
            }

            return _state.SendResettableCompletionSource.GetTypelessValueTask();
        }

        private void ReceiveComplete(int bufferLength)
        {
            uint status = MsQuicApi.Api.StreamReceiveCompleteDelegate(_state.Handle, (ulong)bufferLength);
            QuicExceptionHelpers.ThrowIfFailed(status, "Could not complete receive call.");
        }

        // This can fail if the stream isn't started.
        private long GetStreamId()
        {
            return (long)MsQuicParameterHelpers.GetULongParam(MsQuicApi.Api, _state.Handle, QUIC_PARAM_LEVEL.STREAM, (uint)QUIC_PARAM_STREAM.ID);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed == 1)
            {
                throw new ObjectDisposedException(nameof(MsQuicStream));
            }
        }

        private static uint HandleEventConnectionClose(State state)
        {
            long errorCode = state.ConnectionState.AbortErrorCode;
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(state, $"[Stream#{state.GetHashCode()}] handling Connection#{state.ConnectionState.GetHashCode()} close" +
                    (errorCode != -1 ? $" with code {errorCode}" : ""));
            }

            bool shouldCompleteRead = false;
            bool shouldCompleteSend = false;
            bool shouldCompleteShutdownWrite = false;
            bool shouldCompleteShutdown = false;

            lock (state)
            {
                if (state.ReadState == ReadState.None)
                {
                    shouldCompleteRead = true;
                }
                state.ReadState = ReadState.ConnectionClosed;

                if (state.SendState == SendState.None || state.SendState == SendState.Pending)
                {
                    shouldCompleteSend = true;
                }
                state.SendState = SendState.ConnectionClosed;

                if (state.ShutdownWriteState == ShutdownWriteState.None)
                {
                    shouldCompleteShutdownWrite = true;
                }
                state.ShutdownWriteState = ShutdownWriteState.ConnectionClosed;

                if (state.ShutdownState == ShutdownState.None)
                {
                    shouldCompleteShutdown = true;
                }
                state.ShutdownState = ShutdownState.ConnectionClosed;
            }

            if (shouldCompleteRead)
            {
                state.ReceiveResettableCompletionSource.CompleteException(
                    ExceptionDispatchInfo.SetCurrentStackTrace(GetConnectionAbortedException(state)));
            }

            if (shouldCompleteSend)
            {
                state.SendResettableCompletionSource.CompleteException(
                    ExceptionDispatchInfo.SetCurrentStackTrace(GetConnectionAbortedException(state)));
            }

            if (shouldCompleteShutdownWrite)
            {
                state.ShutdownWriteCompletionSource.SetException(
                    ExceptionDispatchInfo.SetCurrentStackTrace(GetConnectionAbortedException(state)));
            }

            if (shouldCompleteShutdown)
            {
                state.ShutdownCompletionSource.SetException(
                    ExceptionDispatchInfo.SetCurrentStackTrace(GetConnectionAbortedException(state)));
            }

            return MsQuicStatusCodes.Success;
        }

        private static Exception GetConnectionAbortedException(State state) =>
            ThrowHelper.GetConnectionAbortedException(state.ConnectionState.AbortErrorCode);

        private enum ReadState
        {
            /// <summary>
            /// The stream is open, but there is no data available.
            /// </summary>
            None,

            /// <summary>
            /// Data is available in <see cref="State.ReceiveQuicBuffers"/>.
            /// </summary>
            IndividualReadComplete,

            /// <summary>
            /// The peer has gracefully shutdown their sends / our receives; the stream's reads are complete.
            /// </summary>
            ReadsCompleted,

            /// <summary>
            /// User has aborted the stream, either via a cancellation token on ReadAsync(), or via AbortRead().
            /// </summary>
            Aborted,

            /// <summary>
            /// Connection was closed, either by user or by the peer.
            /// </summary>
            ConnectionClosed,

            /// <summary>
            /// Stream is closed for reading.
            /// </summary>
            Closed
        }

        private enum ShutdownWriteState
        {
            None,
            Canceled,
            Finished,
            ConnectionClosed
        }

        private enum ShutdownState
        {
            None,
            Canceled,
            Pending,
            Finished,
            ConnectionClosed
        }

        private enum SendState
        {
            None,
            Pending,
            Aborted,
            Finished,
            ConnectionClosed,
            Closed
        }
    }
}
