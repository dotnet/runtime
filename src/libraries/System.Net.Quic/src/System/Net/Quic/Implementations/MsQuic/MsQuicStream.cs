// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Reflection;
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
        private GCHandle _stateHandle;

        // Backing for StreamId
        private long _streamId = -1;

        // Used to check if StartAsync has been called.
        private bool _started;

        // Used by the class to indicate that the stream is m_Readable.
        private readonly bool _canRead;

        // Used by the class to indicate that the stream is writable.
        private readonly bool _canWrite;

        private volatile bool _disposed;

        private sealed class State
        {
            public SafeMsQuicStreamHandle Handle = null!; // set in ctor.
            public MsQuicConnection.State ConnectionState = null!; // set in ctor.

            public ReadState ReadState;

            // set when ReadState.Aborted:
            public long ReadErrorCode = -1;

            // filled when ReadState.BuffersAvailable:
            public QuicBuffer[] ReceiveQuicBuffers = Array.Empty<QuicBuffer>();
            public int ReceiveQuicBuffersCount;
            public int ReceiveQuicBuffersTotalBytes;

            // set when ReadState.PendingRead:
            public Memory<byte> ReceiveUserBuffer;
            public CancellationTokenRegistration ReceiveCancellationRegistration;
            public MsQuicStream? RootedReceiveStream; // roots the stream in the pinned state to prevent GC during an async read I/O.
            public readonly ResettableCompletionSource<int> ReceiveResettableCompletionSource = new ResettableCompletionSource<int>();

            public SendState SendState;
            public long SendErrorCode = -1;

            // Buffers to hold during a call to send.
            public MemoryHandle[] BufferArrays = new MemoryHandle[1];
            public IntPtr SendQuicBuffers;
            public int SendBufferMaxCount;
            public int SendBufferCount;

            // Roots the stream in the pinned state to prevent GC during an async dispose.
            public MsQuicStream? RootedDisposeStream;

            // Resettable completions to be used for multiple calls to send, start.
            public readonly ResettableCompletionSource<uint> SendResettableCompletionSource = new ResettableCompletionSource<uint>();

            // Set once both peers have fully shut down their side of the stream.
            public readonly TaskCompletionSource ShutdownCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        // inbound.
        internal MsQuicStream(MsQuicConnection.State connectionState, SafeMsQuicStreamHandle streamHandle, QUIC_STREAM_OPEN_FLAGS flags)
        {
            _state.Handle = streamHandle;
            _canRead = true;
            _canWrite = !flags.HasFlag(QUIC_STREAM_OPEN_FLAGS.UNIDIRECTIONAL);
            _started = true;

            _stateHandle = GCHandle.Alloc(_state);
            try
            {
                MsQuicApi.Api.SetCallbackHandlerDelegate(
                    _state.Handle,
                    s_streamDelegate,
                    GCHandle.ToIntPtr(_stateHandle));
            }
            catch
            {
                _stateHandle.Free();
                throw;
            }

            if (!connectionState.TryAddStream(this))
            {
                _stateHandle.Free();
                throw new ObjectDisposedException(nameof(QuicConnection));
        }

            _state.ConnectionState = connectionState;

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(
                    _state,
                    $"[Stream#{_state.GetHashCode()}] inbound {(_canWrite ? "bi" : "uni")}directional stream created " +
                        $"in Connection#{_state.ConnectionState.GetHashCode()}.");
            }
        }

        // outbound.
        internal MsQuicStream(MsQuicConnection.State connectionState, QUIC_STREAM_OPEN_FLAGS flags)
        {
            Debug.Assert(connectionState.Handle != null);

            _canRead = !flags.HasFlag(QUIC_STREAM_OPEN_FLAGS.UNIDIRECTIONAL);
            _canWrite = true;
            _stateHandle = GCHandle.Alloc(_state);
            try
            {
                uint status = MsQuicApi.Api.StreamOpenDelegate(
                    connectionState.Handle,
                    flags,
                    s_streamDelegate,
                    GCHandle.ToIntPtr(_stateHandle),
                    out _state.Handle);

                QuicExceptionHelpers.ThrowIfFailed(status, "Failed to open stream to peer.");

                status = MsQuicApi.Api.StreamStartDelegate(_state.Handle, QUIC_STREAM_START_FLAGS.FAIL_BLOCKED);
                QuicExceptionHelpers.ThrowIfFailed(status, "Could not start stream.");
            }
            catch
            {
                _state.Handle?.Dispose();
                _stateHandle.Free();
                throw;
            }

            if (!connectionState.TryAddStream(this))
            {
                _state.Handle?.Dispose();
                _stateHandle.Free();
                throw new ObjectDisposedException(nameof(QuicConnection));
        }

            _state.ConnectionState = connectionState;

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(
                    _state,
                    $"[Stream#{_state.GetHashCode()}] outbound {(_canRead ? "bi" : "uni")}directional stream created " +
                        $"in Connection#{_state.ConnectionState.GetHashCode()}.");
            }
        }

        internal override bool CanRead => _canRead;

        internal override bool CanWrite => _canWrite;

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
            if (!_canWrite)
            {
                throw new InvalidOperationException(SR.net_quic_writing_notallowed);
            }

            // Make sure start has completed
            if (!_started)
            {
                await _state.SendResettableCompletionSource.GetTypelessValueTask().ConfigureAwait(false);
                _started = true;
                }

            // if token was already cancelled, this would execute syncronously
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

        internal override ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (!_canRead)
            {
                throw new InvalidOperationException(SR.net_quic_reading_notallowed);
            }

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(_state, $"[Stream#{_state.GetHashCode()}] reading into Memory of '{destination.Length}' bytes.");
            }

            ReadState readState;
            long abortError = -1;
            bool canceledSynchronously = false;

            lock (_state)
            {
                readState = _state.ReadState;
                abortError = _state.ReadErrorCode;

                if (readState != ReadState.PendingRead && cancellationToken.IsCancellationRequested)
                {
                    readState = ReadState.Aborted;
                    _state.ReadState = ReadState.Aborted;
                    canceledSynchronously = true;
                }
                else if (readState == ReadState.None)
                {
                    Debug.Assert(_state.RootedReceiveStream is null);

                    _state.ReceiveUserBuffer = destination;
                    _state.RootedReceiveStream = this;
                    _state.ReadState = ReadState.PendingRead;

                    if (cancellationToken.CanBeCanceled)
                    {
                        _state.ReceiveCancellationRegistration = cancellationToken.UnsafeRegister(static (obj, token) =>
                        {
                            var state = (State)obj!;
                            bool completePendingRead;

                            lock (state)
                            {
                                completePendingRead = state.ReadState == ReadState.PendingRead;
                                state.RootedReceiveStream = null;
                                state.ReadState = ReadState.Aborted;
                            }

                            if (completePendingRead)
                            {
                                state.ReceiveResettableCompletionSource.CompleteException(ExceptionDispatchInfo.SetCurrentStackTrace(new OperationCanceledException(token)));
                            }
                        }, _state);
                    }
                    else
                    {
                        _state.ReceiveCancellationRegistration = default;
                    }

                    return _state.ReceiveResettableCompletionSource.GetValueTask();
                }
                else if (readState == ReadState.BuffersAvailable)
                {
                    _state.ReadState = ReadState.None;

                    int taken = CopyMsQuicBuffersToUserBuffer(_state.ReceiveQuicBuffers.AsSpan(0, _state.ReceiveQuicBuffersCount), destination.Span);
                    ReceiveComplete(taken);

                    if (taken != _state.ReceiveQuicBuffersTotalBytes)
                    {
                        // Need to re-enable receives because MsQuic will pause them when we don't consume the entire buffer.
                        EnableReceive();
                    }

                    return new ValueTask<int>(taken);
                }
            }

            Exception? ex = null;

            switch (readState)
            {
                case ReadState.EndOfReadStream:
                    return new ValueTask<int>(0);
                case ReadState.PendingRead:
                    ex = new InvalidOperationException("Only one read is supported at a time.");
                    break;
                case ReadState.Aborted:
                default:
                    Debug.Assert(readState == ReadState.Aborted, $"{nameof(ReadState)} of '{readState}' is unaccounted for in {nameof(ReadAsync)}.");

                    ex =
                        canceledSynchronously ? new OperationCanceledException(cancellationToken) : // aborted by token being canceled before the async op started.
                        abortError == -1 ? new QuicOperationAbortedException() : // aborted by user via some other operation.
                        new QuicStreamAbortedException(abortError); // aborted by peer.
                    break;
            }

            return ValueTask.FromException<int>(ExceptionDispatchInfo.SetCurrentStackTrace(ex!));
        }

        /// <returns>The number of bytes copied.</returns>
        private static unsafe int CopyMsQuicBuffersToUserBuffer(ReadOnlySpan<QuicBuffer> sourceBuffers, Span<byte> destinationBuffer)
        {
            Debug.Assert(sourceBuffers.Length != 0);

            int originalDestinationLength = destinationBuffer.Length;
            QuicBuffer nativeBuffer;
            int takeLength = 0;
            int i = 0;

            do
            {
                nativeBuffer = sourceBuffers[i];
                takeLength = Math.Min((int)nativeBuffer.Length, destinationBuffer.Length);

                new Span<byte>(nativeBuffer.Buffer, takeLength).CopyTo(destinationBuffer);
                destinationBuffer = destinationBuffer.Slice(takeLength);
            }
            while (destinationBuffer.Length != 0 && ++i < sourceBuffers.Length);

            return originalDestinationLength - destinationBuffer.Length;
        }

        // We don't wait for QUIC_STREAM_EVENT_SEND_SHUTDOWN_COMPLETE event here,
        // because it is only sent to us once the peer has acknowledged the shutdown.
        // Instead, this method acts more like shutdown(SD_SEND) in that it only "queues"
        // the shutdown packet to be sent without any waiting for completion.
        public override void CompleteWrites()
        {
            ThrowIfDisposed();

            // Error code is ignored for graceful shutdown.
            StartShutdownOrAbort(QUIC_STREAM_SHUTDOWN_FLAGS.GRACEFUL, errorCode: 0);
        }

        internal override void Abort(long errorCode, QuicAbortDirection abortDirection = QuicAbortDirection.Both)
        {
            ThrowIfDisposed();

            QUIC_STREAM_SHUTDOWN_FLAGS flags = QUIC_STREAM_SHUTDOWN_FLAGS.NONE;
            bool completeWrites = false;
            bool completeReads = false;

            lock (_state)
            {
                if (abortDirection.HasFlag(QuicAbortDirection.Write))
                {
                    completeWrites = _state.SendState is SendState.None or SendState.Pending;
                    _state.SendState = SendState.Aborted;
                    flags |= QUIC_STREAM_SHUTDOWN_FLAGS.ABORT_SEND;
                }

                if (abortDirection.HasFlag(QuicAbortDirection.Read))
                {
                    completeReads = _state.ReadState == ReadState.PendingRead;
                    _state.RootedReceiveStream = null;
                    _state.ReadState = ReadState.Aborted;
                    flags |= QUIC_STREAM_SHUTDOWN_FLAGS.ABORT_RECEIVE;
                }
            }

            if ((abortDirection & QuicAbortDirection.Immediate) == QuicAbortDirection.Immediate)
            {
                flags |= QUIC_STREAM_SHUTDOWN_FLAGS.IMMEDIATE;
            }

            StartShutdownOrAbort(flags, errorCode);

            if (completeWrites)
            {
                _state.SendResettableCompletionSource.CompleteException(ExceptionDispatchInfo.SetCurrentStackTrace(new QuicOperationAbortedException()));
            }

            if (completeReads)
            {
                _state.ReceiveResettableCompletionSource.CompleteException(ExceptionDispatchInfo.SetCurrentStackTrace(new QuicOperationAbortedException()));
            }
        }

        /// <param name="flags"></param>
        /// <param name="errorCode">For abortive flags, the error code sent to peer. Otherwise, ignored.</param>
        private void StartShutdownOrAbort(QUIC_STREAM_SHUTDOWN_FLAGS flags, long errorCode)
        {
            Debug.Assert(!_disposed);

            uint status = MsQuicApi.Api.StreamShutdownDelegate(_state.Handle, flags, errorCode);
            QuicExceptionHelpers.ThrowIfFailed(status, "StreamShutdown failed.");
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

        public override ValueTask DisposeAsync(CancellationToken cancellationToken) =>
            DisposeAsync(cancellationToken, async: true);

        public override void Dispose()
        {
            ValueTask t = DisposeAsync(cancellationToken: default, async: false);
            Debug.Assert(t.IsCompleted);
            t.GetAwaiter().GetResult();
        }

        ~MsQuicStream()
        {
            DisposeAsyncThrowaway(this);

            // This is weird due to needing to keep _state alive for MsQuic's callback.
            // See DisposeAsync implementation for details.

            static async void DisposeAsyncThrowaway(MsQuicStream stream)
            {
                await stream.DisposeAsync(cancellationToken: default, async: true).ConfigureAwait(false);
            }
        }

        private async ValueTask DisposeAsync(CancellationToken cancellationToken, bool async)
        {
            if (_disposed)
            {
                return;
            }

            // MsQuic will ignore this call if it was already shutdown elsewhere.
            // PERF TODO: update write loop to make it so we don't need to call this. it queues an event to the MsQuic thread pool.
            StartShutdownOrAbort(QUIC_STREAM_SHUTDOWN_FLAGS.GRACEFUL, errorCode: 0);

            // MsQuic will continue sending us events, so we need to wait for shutdown
            // completion (the final event) before freeing _stateHandle's GCHandle.
            // If Abort() wasn't called with "immediate", this will wait for peer to shut down their write side.

            if (async)
            {
                _state.RootedDisposeStream = this;
                try
                {
                    await _state.ShutdownCompletionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    _state.RootedDisposeStream = null;
                }
            }
            else
            {
                _state.ShutdownCompletionSource.Task.GetAwaiter().GetResult();
            }

            _disposed = true;
            _state.Handle.Dispose();
            Marshal.FreeHGlobal(_state.SendQuicBuffers);
            if (_stateHandle.IsAllocated) _stateHandle.Free();
            CleanupSendState(_state);
            _state.ConnectionState?.RemoveStream(this);

            GC.SuppressFinalize(this);

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(_state, $"[Stream#{_state.GetHashCode()}] disposed");
            }

        }

        private void EnableReceive()
        {
            uint status = MsQuicApi.Api.StreamReceiveSetEnabledDelegate(_state.Handle, enabled: true);
            QuicExceptionHelpers.ThrowIfFailed(status, "StreamReceiveSetEnabled failed.");
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
                    // Shutdown for both sending and receiving is completed.
                    case QUIC_STREAM_EVENT_TYPE.SHUTDOWN_COMPLETE:
                        return HandleEventShutdownComplete(state, ref evt);
                    default:
                        return MsQuicStatusCodes.Success;
                }
            }
            catch (Exception)
            {
                return MsQuicStatusCodes.InternalError;
            }
        }

        private static unsafe uint HandleEventRecv(State state, ref StreamEvent evt)
        {
            ref StreamEventDataReceive receiveEvent = ref evt.Data.Receive;

            if (receiveEvent.BufferCount == 0)
            {
                // This is a 0-length receive that happens once reads are finished (via abort or otherwise).
                // State changes for this are handled elsewhere.
                return MsQuicStatusCodes.Success;
            }

            int readLength;

            lock (state)
            {
                switch (state.ReadState)
                {
                    case ReadState.None:
                        // ReadAsync() hasn't been called yet. Stash the buffer so the next ReadAsync call completes synchronously.

                        if ((uint)state.ReceiveQuicBuffers.Length < receiveEvent.BufferCount)
                        {
                            QuicBuffer[] oldReceiveBuffers = state.ReceiveQuicBuffers;
                            state.ReceiveQuicBuffers = ArrayPool<QuicBuffer>.Shared.Rent((int)receiveEvent.BufferCount);

                            if (oldReceiveBuffers.Length != 0) // don't return Array.Empty.
                            {
                                ArrayPool<QuicBuffer>.Shared.Return(oldReceiveBuffers);
                            }
                        }
            }

                        for (uint i = 0; i < receiveEvent.BufferCount; ++i)
                        {
                            state.ReceiveQuicBuffers[i] = receiveEvent.Buffers[i];
                        }

                        state.ReceiveQuicBuffersCount = (int)receiveEvent.BufferCount;
                        state.ReceiveQuicBuffersTotalBytes = checked((int)receiveEvent.TotalBufferLength);
                        state.ReadState = ReadState.BuffersAvailable;
                        return MsQuicStatusCodes.Pending;
                    case ReadState.PendingRead:
                        // There is a pending ReadAsync().

                        state.ReceiveCancellationRegistration.Unregister();
                        state.RootedReceiveStream = null;
                        state.ReadState = ReadState.None;

                        readLength = CopyMsQuicBuffersToUserBuffer(new ReadOnlySpan<QuicBuffer>(receiveEvent.Buffers, (int)receiveEvent.BufferCount), state.ReceiveUserBuffer.Span);
                        break;
                    case ReadState.Aborted:
                    default:
                        Debug.Assert(state.ReadState == ReadState.Aborted, $"Unexpected {nameof(ReadState)} '{state.ReadState}' in {nameof(HandleEventRecv)}.");

                        // There was a race between a user aborting the read stream and the callback being ran.
                        // This will eat any received data.
                        return MsQuicStatusCodes.Success;
                }
            }

            // We're completing a pending read.

            state.ReceiveResettableCompletionSource.Complete(readLength);

            // Returning Success when the entire buffer hasn't been consumed will cause MsQuic to disable further receive events until EnableReceive() is called.
            // Returning Continue will cause a second receive event to fire immediately after this returns, but allows MsQuic to clean up its buffers.

            uint ret = (uint)readLength == receiveEvent.TotalBufferLength
                ? MsQuicStatusCodes.Success
                : MsQuicStatusCodes.Continue;

            receiveEvent.TotalBufferLength = (uint)readLength;
            return ret;
        }

        private static uint HandleEventPeerRecvAborted(State state, ref StreamEvent evt)
        {
            bool shouldComplete;

            lock (state)
            {
                shouldComplete = state.SendState == SendState.None || state.SendState == SendState.Pending;
                state.SendState = SendState.Aborted;
                state.SendErrorCode = evt.Data.PeerSendAborted.ErrorCode;
            }

            if (shouldComplete)
            {
                state.SendResettableCompletionSource.CompleteException(new QuicStreamAbortedException(state.SendErrorCode));
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

        private static uint HandleEventShutdownComplete(State state, ref StreamEvent evt)
        {
            state.ShutdownCompletionSource.TrySetResult();
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
            bool completePendingRead = false;

            lock (state)
            {
                // This event won't occur within the middle of a receive.
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(state, $"[Stream#{state.GetHashCode()}] completing resettable event source.");


                if (state.ReadState == ReadState.PendingRead)
                {
                    completePendingRead = true;
                    state.RootedReceiveStream = null;
                    state.ReadState = ReadState.EndOfReadStream;
                }
                else if (state.ReadState == ReadState.None)
                {
                    state.ReadState = ReadState.EndOfReadStream;
                }
            }

            if (completePendingRead)
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
                    StartShutdownOrAbort(QUIC_STREAM_SHUTDOWN_FLAGS.GRACEFUL, errorCode: 0);
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
                    StartShutdownOrAbort(QUIC_STREAM_SHUTDOWN_FLAGS.GRACEFUL, errorCode: 0);
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
                    StartShutdownOrAbort(QUIC_STREAM_SHUTDOWN_FLAGS.GRACEFUL, errorCode: 0);
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
            if (_disposed)
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
            /// The stream is open, but there is no pending operation and no data available.
            /// </summary>
            None,

            /// <summary>
            /// There is a pending operation on the stream.
            /// </summary>
            PendingRead,

            /// <summary>
            /// There is data available.
            /// </summary>
            BuffersAvailable,

            /// <summary>
            /// The peer has gracefully shutdown their sends / our receives; the stream's reads are complete.
            /// </summary>
            EndOfReadStream,

            /// <summary>
            /// User has aborted the stream, either via a cancellation token on ReadAsync(), or via Abort(read).
            /// </summary>
            Aborted,

            /// <summary>
            /// Connection was closed, either by user or by the peer.
            /// </summary>
            ConnectionClosed
        }

        private enum SendState
        {
            None,
            Pending,
            Aborted,
            Finished,
            ConnectionClosed
        }
    }
}
