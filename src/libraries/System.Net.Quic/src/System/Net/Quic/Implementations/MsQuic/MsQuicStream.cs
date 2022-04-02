// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO;
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
        internal static unsafe readonly StreamCallbackDelegate s_streamDelegate = new StreamCallbackDelegate(NativeCallbackHandler);

        // The state is passed to msquic and then it's passed back by msquic to the callback handler.
        private readonly State _state = new State();

        private readonly bool _canRead;
        private readonly bool _canWrite;

        // Backing for StreamId
        private long _streamId = -1;

        private int _disposed;

        private sealed class State
        {
            public SafeMsQuicStreamHandle Handle = null!; // set in ctor.
            // Roots the state in GC and it won't get collected while this exist.
            // It must be kept alive until we receive SHUTDOWN_COMPLETE event
            public GCHandle StateGCHandle;

            public MsQuicStream? Stream; // roots the stream in the pinned state to prevent GC during an async read I/O.
            public MsQuicConnection.State ConnectionState = null!; // set in ctor.
            public string TraceId = null!; // set in ctor.

            public uint StartStatus = MsQuicStatusCodes.Success;

            public ReadState ReadState;

            // set when ReadState.Aborted:
            public long ReadErrorCode = -1;

            // filled when ReadState.BuffersAvailable:
            public QuicBuffer[] ReceiveQuicBuffers = Array.Empty<QuicBuffer>();
            public int ReceiveQuicBuffersCount;
            public int ReceiveQuicBuffersTotalBytes;
            public bool ReceiveIsFinal;

            // set when ReadState.PendingRead:
            public Memory<byte> ReceiveUserBuffer;
            public CancellationTokenRegistration ReceiveCancellationRegistration;
            // Resettable completions to be used for multiple calls to receive.
            public readonly ResettableCompletionSource<int> ReceiveResettableCompletionSource = new ResettableCompletionSource<int>();

            public SendState SendState;
            public long SendErrorCode = -1;

            // Buffers to hold during a call to send.
            public MemoryHandle[] BufferArrays = new MemoryHandle[1];
            public IntPtr SendQuicBuffers;
            public int SendBufferMaxCount;
            public int SendBufferCount;

            // Resettable completions to be used for multiple calls to send.
            public readonly ResettableCompletionSource<uint> SendResettableCompletionSource = new ResettableCompletionSource<uint>();

            public ShutdownWriteState ShutdownWriteState;

            // Set once writes have been shutdown.
            public readonly TaskCompletionSource ShutdownWriteCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            public ShutdownState ShutdownState;
            // The value makes sure that we release the handles only once.
            public int ShutdownDone;

            // Set once stream have been shutdown.
            public readonly TaskCompletionSource ShutdownCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            public void Cleanup()
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"{TraceId} releasing handles.");

                ShutdownState = ShutdownState.Finished;
                CleanupSendState(this);
                Handle?.Dispose();
                Marshal.FreeHGlobal(SendQuicBuffers);
                SendQuicBuffers = IntPtr.Zero;
                if (StateGCHandle.IsAllocated) StateGCHandle.Free();
                ConnectionState?.RemoveStream(null);
            }
        }

        internal string TraceId() => _state.TraceId;

        // inbound.
        internal MsQuicStream(MsQuicConnection.State connectionState, SafeMsQuicStreamHandle streamHandle, QUIC_STREAM_OPEN_FLAGS flags)
        {
            if (!connectionState.TryAddStream(this))
            {
                throw new ObjectDisposedException(nameof(QuicConnection));
            }
            // this assignment should be done before SetCallbackHandlerDelegate to prevent NRE in HandleEventConnectionClose
            // but after TryAddStream to prevent unnecessary RemoveStream in finalizer
            _state.ConnectionState = connectionState;

            _state.Handle = streamHandle;
            _canRead = true;
            _canWrite = !flags.HasFlag(QUIC_STREAM_OPEN_FLAGS.UNIDIRECTIONAL);
            if (!_canWrite)
            {
                _state.SendState = SendState.Closed;
            }

            _state.StateGCHandle = GCHandle.Alloc(_state);
            try
            {
                Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
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

            _state.TraceId = MsQuicTraceHelper.GetTraceId(_state.Handle);
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(
                    _state,
                    $"{TraceId()} Inbound {(flags.HasFlag(QUIC_STREAM_OPEN_FLAGS.UNIDIRECTIONAL) ? "uni" : "bi")}directional stream created " +
                        $"in connection {_state.ConnectionState.TraceId}.");
            }
        }

        // outbound.
        internal MsQuicStream(MsQuicConnection.State connectionState, QUIC_STREAM_OPEN_FLAGS flags)
        {
            Debug.Assert(connectionState.Handle != null);

            if (!connectionState.TryAddStream(this))
            {
                throw new ObjectDisposedException(nameof(QuicConnection));
            }
            // this assignment should be done before StreamOpenDelegate to prevent NRE in HandleEventConnectionClose
            // but after TryAddStream to prevent unnecessary RemoveStream in finalizer
            _state.ConnectionState = connectionState;

            _canRead = !flags.HasFlag(QUIC_STREAM_OPEN_FLAGS.UNIDIRECTIONAL);
            _canWrite = true;

            _state.StateGCHandle = GCHandle.Alloc(_state);
            if (!_canRead)
            {
                _state.ReadState = ReadState.Closed;
            }

            try
            {
                Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
                uint status = MsQuicApi.Api.StreamOpenDelegate(
                    connectionState.Handle,
                    flags,
                    s_streamDelegate,
                    GCHandle.ToIntPtr(_state.StateGCHandle),
                    out _state.Handle);

                if (status == MsQuicStatusCodes.Aborted)
                {
                    // connection already aborted by peer, throw relevant exception
                    throw ThrowHelper.GetConnectionAbortedException(connectionState.AbortErrorCode);
                }

                QuicExceptionHelpers.ThrowIfFailed(status, "Failed to open stream to peer.");

                Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
                status = MsQuicApi.Api.StreamStartDelegate(_state.Handle, QUIC_STREAM_START_FLAGS.FAIL_BLOCKED | QUIC_STREAM_START_FLAGS.SHUTDOWN_ON_FAIL);
                QuicExceptionHelpers.ThrowIfFailed(status, "Could not start stream.");
            }
            catch
            {
                _state.Handle?.Dispose();
                _state.StateGCHandle.Free();
                throw;
            }

            _state.TraceId = MsQuicTraceHelper.GetTraceId(_state.Handle);
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(
                    _state,
                    $"{_state.TraceId} Outbound {(flags.HasFlag(QUIC_STREAM_OPEN_FLAGS.UNIDIRECTIONAL) ? "uni" : "bi")}directional stream created " +
                        $"in connection {_state.ConnectionState.TraceId}.");
            }
        }

        internal override bool CanRead => _disposed == 0 && _canRead;

        internal override bool CanWrite => _disposed == 0 && _canWrite;

        internal override bool ReadsCompleted => _state.ReadState == ReadState.ReadsCompleted;

        internal override bool CanTimeout => true;

        private int _readTimeout = Timeout.Infinite;

        internal override int ReadTimeout
        {
            get
            {
                ThrowIfDisposed();
                return _readTimeout;
            }
            set
            {
                ThrowIfDisposed();
                if (value <= 0 && value != System.Threading.Timeout.Infinite)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.net_quic_timeout_use_gt_zero);
                }
                _readTimeout = value;
            }
        }

        private int _writeTimeout = Timeout.Infinite;
        internal override int WriteTimeout
        {
            get
            {
                ThrowIfDisposed();
                return _writeTimeout;
            }
            set
            {
                ThrowIfDisposed();
                if (value <= 0 && value != System.Threading.Timeout.Infinite)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.net_quic_timeout_use_gt_zero);
                }
                _writeTimeout = value;
            }
        }

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

            using CancellationTokenRegistration registration = HandleWriteStartState(buffers.IsEmpty, cancellationToken);

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

            using CancellationTokenRegistration registration = HandleWriteStartState(buffers.IsEmpty, cancellationToken);

            await SendReadOnlyMemoryListAsync(buffers, endStream ? QUIC_SEND_FLAGS.FIN : QUIC_SEND_FLAGS.NONE).ConfigureAwait(false);

            HandleWriteCompletedState();
        }

        internal override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, bool endStream, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            using CancellationTokenRegistration registration = HandleWriteStartState(buffer.IsEmpty, cancellationToken);

            await SendReadOnlyMemoryAsync(buffer, endStream ? QUIC_SEND_FLAGS.FIN : QUIC_SEND_FLAGS.NONE).ConfigureAwait(false);

            HandleWriteCompletedState();
        }

        private CancellationTokenRegistration HandleWriteStartState(bool emptyBuffer, CancellationToken cancellationToken)
        {
            if (_state.SendState == SendState.Closed)
            {
                throw new InvalidOperationException(SR.net_quic_writing_notallowed);
            }
            if (_state.SendState == SendState.Aborted)
            {
                if (_state.SendErrorCode != -1)
                {
                    throw new QuicStreamAbortedException(_state.SendErrorCode);
                }

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

                throw new OperationCanceledException(cancellationToken);
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

                    if (_state.SendErrorCode != -1)
                    {
                        throw new QuicStreamAbortedException(_state.SendErrorCode);
                    }

                    throw new OperationCanceledException(SR.net_quic_sending_aborted);
                }
                if (_state.SendState == SendState.ConnectionClosed)
                {
                    throw GetConnectionAbortedException(_state);
                }

                // Change the state in the same lock where we check for final states to prevent coming back from Aborted/ConnectionClosed.
                Debug.Assert(_state.SendState != SendState.Pending);
                _state.SendState = emptyBuffer ? SendState.Finished : SendState.Pending;
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

            if (_state.ReadState == ReadState.Closed)
            {
                throw new InvalidOperationException(SR.net_quic_reading_notallowed);
            }

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(_state, $"{TraceId()} Stream reading into Memory of '{destination.Length}' bytes.");
            }

            ReadState initialReadState;  // value before transitions
            long abortError;
            bool preCanceled = false;

            int bytesRead = -1;
            bool reenableReceive = false;
            lock (_state)
            {
                initialReadState = _state.ReadState;
                abortError = _state.ReadErrorCode;

                // Failure scenario: pre-canceled token. Transition: Any non-final -> Aborted
                // PendingRead state indicates there is another concurrent read operation in flight
                // which is forbidden, so it is handled separately
                if (initialReadState != ReadState.PendingRead && cancellationToken.IsCancellationRequested)
                {
                    initialReadState = ReadState.Aborted;
                    CleanupReadStateAndCheckPending(_state, ReadState.Aborted);
                    preCanceled = true;
                }

                // Success scenario: EOS already reached, completing synchronously. No transition (final state)
                if (initialReadState == ReadState.ReadsCompleted)
                {
                    return new ValueTask<int>(0);
                }

                // Success scenario: no data available yet, will return a task to wait on. Transition None->PendingRead
                if (initialReadState == ReadState.None)
                {
                    Debug.Assert(_state.Stream is null);

                    _state.ReceiveUserBuffer = destination;
                    _state.Stream = this;
                    _state.ReadState = ReadState.PendingRead;

                    if (cancellationToken.CanBeCanceled)
                    {
                        // Failure scenario: cancellation. Transition: Any non-final -> Aborted
                        _state.ReceiveCancellationRegistration = cancellationToken.UnsafeRegister(static (obj, token) =>
                        {
                            var state = (State)obj!;
                            bool completePendingRead;
                            lock (state)
                            {
                                completePendingRead = CleanupReadStateAndCheckPending(state, ReadState.Aborted);
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

                // Success scenario: data already available, completing synchronously.
                // Transition IndividualReadComplete->None, or IndividualReadComplete->ReadsCompleted, if it was the last message and we fully consumed it
                if (initialReadState == ReadState.IndividualReadComplete)
                {
                    _state.ReadState = ReadState.None;

                    bytesRead = CopyMsQuicBuffersToUserBuffer(_state.ReceiveQuicBuffers.AsSpan(0, _state.ReceiveQuicBuffersCount), destination.Span);

                    if (bytesRead != _state.ReceiveQuicBuffersTotalBytes)
                    {
                        // Need to re-enable receives because MsQuic will pause them when we don't consume the entire buffer.
                        reenableReceive = true;
                    }
                    else if (_state.ReceiveIsFinal)
                    {
                        // This was a final message and we've consumed everything. We can complete the state without waiting for PEER_SEND_SHUTDOWN
                        _state.ReadState = ReadState.ReadsCompleted;
                    }
                }
            }

            // methods below need to be called outside of the lock
            if (bytesRead > -1)
            {
                ReceiveComplete(bytesRead);

                if (reenableReceive)
                {
                    EnableReceive();
                }

                return new ValueTask<int>(bytesRead);
            }

            // All success scenarios returned at this point. Failure scenarios below:

            Exception? ex = null;

            switch (initialReadState)
            {
                case ReadState.PendingRead:
                    ex = new InvalidOperationException("Only one read is supported at a time.");
                    break;
                case ReadState.Aborted:
                    ex = preCanceled ? new OperationCanceledException(cancellationToken) :
                          ThrowHelper.GetStreamAbortedException(abortError);
                    break;
                case ReadState.ConnectionClosed:
                default:
                    Debug.Assert(initialReadState == ReadState.ConnectionClosed, $"{nameof(ReadState)} of '{initialReadState}' is unaccounted for in {nameof(ReadAsync)}.");
                    ex = GetConnectionAbortedException(_state);
                    break;
            }

            return ValueTask.FromException<int>(ExceptionDispatchInfo.SetCurrentStackTrace(ex!));
        }

        /// <returns>The number of bytes copied.</returns>
        private static unsafe int CopyMsQuicBuffersToUserBuffer(ReadOnlySpan<QuicBuffer> sourceBuffers, Span<byte> destinationBuffer)
        {
            if (sourceBuffers.Length == 0)
            {
                return 0;
            }

            int originalDestinationLength = destinationBuffer.Length;
            QuicBuffer nativeBuffer;
            int takeLength;
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

        internal override void AbortRead(long errorCode)
        {
            if (_disposed == 1)
            {
                // Dispose called AbortRead already
                return;
            }

            bool shouldComplete = false;
            lock (_state)
            {
                shouldComplete = CleanupReadStateAndCheckPending(_state, ReadState.Aborted);
            }

            if (shouldComplete)
            {
                _state.ReceiveResettableCompletionSource.CompleteException(
                    ExceptionDispatchInfo.SetCurrentStackTrace(new QuicOperationAbortedException("Read was aborted")));
            }

            StartShutdown(QUIC_STREAM_SHUTDOWN_FLAGS.ABORT_RECEIVE, errorCode);
        }

        internal override void AbortWrite(long errorCode)
        {
            if (_disposed == 1)
            {
                // Dispose already triggered graceful shutdown
                // It is unsafe to try to trigger abortive shutdown now, because final event arriving after Dispose releases SafeHandle
                // so if it arrives after our check but before we call msquic, me might end up with access violation
                return;
            }

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
                    ExceptionDispatchInfo.SetCurrentStackTrace(new QuicOperationAbortedException("Write was aborted.")));
            }

            StartShutdown(QUIC_STREAM_SHUTDOWN_FLAGS.ABORT_SEND, errorCode);
        }

        private void StartShutdown(QUIC_STREAM_SHUTDOWN_FLAGS flags, long errorCode)
        {
            Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
            uint status = MsQuicApi.Api.StreamShutdownDelegate(_state.Handle, flags, errorCode);
            QuicExceptionHelpers.ThrowIfFailed(status, "StreamShutdown failed.");
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
                    state.ShutdownCompletionSource.SetException(
                        ExceptionDispatchInfo.SetCurrentStackTrace(new OperationCanceledException("Wait for shutdown was canceled", token)));
                }
            }, _state);

            await _state.ShutdownCompletionSource.Task.ConfigureAwait(false);
        }

        internal override ValueTask WaitForWriteCompletionAsync(CancellationToken cancellationToken = default)
        {
            // TODO: What should happen if this is called for a unidirectional stream and there are no writes?

            ThrowIfDisposed();

            lock (_state)
            {
                if (_state.ShutdownWriteState == ShutdownWriteState.ConnectionClosed)
                {
                    throw GetConnectionAbortedException(_state);
                }
            }

            return new ValueTask(_state.ShutdownWriteCompletionSource.Task.WaitAsync(cancellationToken));
        }

        internal override void Shutdown()
        {
            ThrowIfDisposed();

            lock (_state)
            {
                if (_state.SendState < SendState.Finished)
                {
                    _state.SendState = SendState.Finished;
                }
            }

            // it is ok to send shutdown several times, MsQuic will ignore it
            StartShutdown(QUIC_STREAM_SHUTDOWN_FLAGS.GRACEFUL, errorCode: 0);
        }

        // TODO consider removing sync-over-async with blocking calls.
        internal override int Read(Span<byte> buffer)
        {
            ThrowIfDisposed();
            byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
            CancellationTokenSource? cts = null;
            try
            {
                if (_readTimeout > 0)
                {
                    cts = new CancellationTokenSource(_readTimeout);
                }
                int readLength = ReadAsync(new Memory<byte>(rentedBuffer, 0, buffer.Length), cts != null ? cts.Token : default).AsTask().GetAwaiter().GetResult();
                rentedBuffer.AsSpan(0, readLength).CopyTo(buffer);
                return readLength;
            }
            catch (OperationCanceledException) when (cts != null && cts.IsCancellationRequested)
            {
                // sync operations do not have Cancellation
                throw new IOException(SR.net_quic_timeout);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
                cts?.Dispose();
            }
        }

        internal override void Write(ReadOnlySpan<byte> buffer)
        {
            ThrowIfDisposed();
            CancellationTokenSource? cts = null;


            if (_writeTimeout > 0)
            {
                cts = new CancellationTokenSource(_writeTimeout);
            }

            // TODO: optimize this.
            try
            {
                WriteAsync(buffer.ToArray()).AsTask().GetAwaiter().GetResult();
            }
            catch (OperationCanceledException) when (cts != null && cts.IsCancellationRequested)
            {
                // sync operations do not have Cancellation
                throw new IOException(SR.net_quic_timeout);
            }
            finally
            {
                cts?.Dispose();
            }
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


            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(_state, $"{TraceId()} Stream disposing {disposing}");

            bool callShutdown = false;
            bool abortRead = false;
            bool completeRead = false;
            bool releaseHandles = false;
            lock (_state)
            {
                if (_state.SendState < SendState.Aborted)
                {
                    callShutdown = true;
                }

                // We can enter Aborted state from both AbortRead call (aborts on the wire) and a Cancellation callback (only changes state)
                // We need to ensure read is aborted on the wire here. We let msquic handle a second call to abort as a no-op
                if (_state.ReadState < ReadState.ReadsCompleted || _state.ReadState == ReadState.Aborted)
                {
                    abortRead = true;
                    completeRead = CleanupReadStateAndCheckPending(_state, ReadState.Aborted);
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
                }
                catch (ObjectDisposedException) { };
            }

            if (abortRead)
            {
                try
                {
                    StartShutdown(QUIC_STREAM_SHUTDOWN_FLAGS.ABORT_RECEIVE, 0xffffffff);
                }
                catch (ObjectDisposedException) { };
            }

            if (completeRead)
            {
                _state.ReceiveResettableCompletionSource.CompleteException(
                    ExceptionDispatchInfo.SetCurrentStackTrace(new QuicOperationAbortedException("Read was canceled")));
            }

            if (releaseHandles)
            {
                _state.Cleanup();
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(_state, $"{TraceId()} Stream disposed");
        }

        private void EnableReceive()
        {
            Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
            uint status = MsQuicApi.Api.StreamReceiveSetEnabledDelegate(_state.Handle, enabled: true);
            QuicExceptionHelpers.ThrowIfFailed(status, "StreamReceiveSetEnabled failed.");
        }

        /// <summary>
        /// Callback calls for a single instance of a stream are serialized by msquic.
        /// They happen on a msquic thread and shouldn't take too long to not to block msquic.
        /// </summary>
        private static unsafe uint NativeCallbackHandler(
            IntPtr stream,
            IntPtr context,
            StreamEvent* streamEvent)
        {
            GCHandle gcHandle = GCHandle.FromIntPtr(context);
            Debug.Assert(gcHandle.IsAllocated);
            Debug.Assert(gcHandle.Target is not null);
            var state = (State)gcHandle.Target;

            return HandleEvent(state, ref *streamEvent);
        }

        private static uint HandleEvent(State state, ref StreamEvent evt)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(state, $"{state.TraceId} Stream received event {evt.Type}");
            }

            try
            {
                switch (evt.Type)
                {
                    // Stream has started.
                    // Will only be done for outbound streams (inbound streams have already started)
                    case QUIC_STREAM_EVENT_TYPE.START_COMPLETE:
                        return HandleEventStartComplete(state, ref evt);
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
                    NetEventSource.Error(state, $"{state.TraceId} Exception occurred during handling Stream {evt.Type} event: {ex}");
                }

                Debug.Fail($"{state.TraceId} Exception occurred during handling Stream {evt.Type} event: {ex}");

                return MsQuicStatusCodes.InternalError;
            }
        }

        private static unsafe uint HandleEventRecv(State state, ref StreamEvent evt)
        {
            ref StreamEventDataReceive receiveEvent = ref evt.Data.Receive;

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(state, $"{state.TraceId} Stream received {receiveEvent.TotalBufferLength} bytes{(receiveEvent.Flags.HasFlag(QUIC_RECEIVE_FLAGS.FIN) ? " with FIN flag" : "")}");
            }

            int readLength;

            bool shouldComplete = false;
            lock (state)
            {
                switch (state.ReadState)
                {
                    case ReadState.None:
                        // ReadAsync() hasn't been called yet. Stash the buffer so the next ReadAsync call completes synchronously.

                        // We are overwriting state.ReceiveQuicBuffers here even if we only partially consumed them
                        // and it is intended, because unconsumed data will arrive again from the point we've stopped.
                        // New RECEIVE event wouldn't come until we call EnableReceive(), and we call it only after we've consumed
                        // as much as we could and said so to msquic in ReceiveComplete(taken), so new event will have all the
                        // remaining data.

                        if ((uint)state.ReceiveQuicBuffers.Length < receiveEvent.BufferCount)
                        {
                            QuicBuffer[] oldReceiveBuffers = state.ReceiveQuicBuffers;
                            state.ReceiveQuicBuffers = ArrayPool<QuicBuffer>.Shared.Rent((int)receiveEvent.BufferCount);

                            if (oldReceiveBuffers.Length != 0) // don't return Array.Empty.
                            {
                                ArrayPool<QuicBuffer>.Shared.Return(oldReceiveBuffers);
                            }
                        }

                        for (uint i = 0; i < receiveEvent.BufferCount; ++i)
                        {
                            state.ReceiveQuicBuffers[i] = receiveEvent.Buffers[i];
                        }

                        state.ReceiveQuicBuffersCount = (int)receiveEvent.BufferCount;
                        state.ReceiveQuicBuffersTotalBytes = checked((int)receiveEvent.TotalBufferLength);
                        state.ReceiveIsFinal = receiveEvent.Flags.HasFlag(QUIC_RECEIVE_FLAGS.FIN);

                        // 0-length receive can happens once reads are finished (gracefully or otherwise).
                        if (state.ReceiveQuicBuffersTotalBytes == 0)
                        {
                            if (state.ReceiveIsFinal)
                            {
                                // We can complete the state without waiting for PEER_SEND_SHUTDOWN
                                state.ReadState = ReadState.ReadsCompleted;
                            }

                            // if it was not a graceful shutdown, we defer aborting to PEER_SEND_ABORT event handler
                            return MsQuicStatusCodes.Success;
                        }
                        else
                        {
                            // Normal RECEIVE - data will be buffered until user calls ReadAsync() and no new event will be issued until EnableReceive()
                            state.ReadState = ReadState.IndividualReadComplete;
                            return MsQuicStatusCodes.Pending;
                        }

                    case ReadState.PendingRead:
                        // There is a pending ReadAsync().

                        state.ReceiveCancellationRegistration.Unregister();
                        shouldComplete = true;
                        state.Stream = null;
                        state.ReadState = ReadState.None;

                        readLength = CopyMsQuicBuffersToUserBuffer(new ReadOnlySpan<QuicBuffer>(receiveEvent.Buffers, (int)receiveEvent.BufferCount), state.ReceiveUserBuffer.Span);

                        // This was a final message and we've consumed everything. We can complete the state without waiting for PEER_SEND_SHUTDOWN
                        if (receiveEvent.Flags.HasFlag(QUIC_RECEIVE_FLAGS.FIN) && (uint)readLength == receiveEvent.TotalBufferLength)
                        {
                            state.ReadState = ReadState.ReadsCompleted;
                        }
                        // Else, if this was a final message, but we haven't consumed it fully, FIN flag will arrive again in the next RECEIVE event

                        state.ReceiveUserBuffer = null;
                        break;

                    default:
                        Debug.Assert(state.ReadState is ReadState.Aborted or ReadState.ConnectionClosed, $"Unexpected {nameof(ReadState)} '{state.ReadState}' in {nameof(HandleEventRecv)}.");

                        // There was a race between a user aborting the read stream and the callback being ran.
                        // This will eat any received data.
                        return MsQuicStatusCodes.Success;
                }
            }

            // We're completing a pending read.
            if (shouldComplete)
            {
                state.ReceiveResettableCompletionSource.Complete(readLength);
            }

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
            bool shouldSendComplete = false;
            bool shouldShutdownWriteComplete = false;
            lock (state)
            {
                if (state.SendState == SendState.None || state.SendState == SendState.Pending)
                {
                    shouldSendComplete = true;
                }

                if (state.ShutdownWriteState == ShutdownWriteState.None)
                {
                    state.ShutdownWriteState = ShutdownWriteState.Canceled;
                    shouldShutdownWriteComplete = true;
                }

                state.SendState = SendState.Aborted;
                state.SendErrorCode = (long)evt.Data.PeerReceiveAborted.ErrorCode;
            }

            if (shouldSendComplete)
            {
                state.SendResettableCompletionSource.CompleteException(
                    ExceptionDispatchInfo.SetCurrentStackTrace(new QuicStreamAbortedException(state.SendErrorCode)));
            }

            if (shouldShutdownWriteComplete)
            {
                state.ShutdownWriteCompletionSource.SetException(
                    ExceptionDispatchInfo.SetCurrentStackTrace(new QuicStreamAbortedException(state.SendErrorCode)));
            }

            return MsQuicStatusCodes.Success;
        }

        private static uint HandleEventStartComplete(State state, ref StreamEvent evt)
        {
            // Store the start status code and check it when propagating shutdown event, which we'll get since we set SHUTDOWN_ON_FAIL in StreamStart.
            state.StartStatus = evt.Data.StartComplete.Status;
            return MsQuicStatusCodes.Success;
        }

        private static uint HandleEventSendShutdownComplete(State state, ref StreamEvent evt)
        {
            // Graceful will be false in three situations:
            // 1. The peer aborted reads and the PEER_RECEIVE_ABORTED event was raised.
            //    ShutdownWriteCompletionSource is already complete with an error.
            // 2. We aborted writes.
            //    ShutdownWriteCompletionSource is already complete with an error.
            // 3. The connection was closed.
            //    SHUTDOWN_COMPLETE event will be raised immediately after this event. It will handle completing with an error.
            //
            // Only use this event with sends gracefully completed.
            if (evt.Data.SendShutdownComplete.Graceful != 0)
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
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(state, $"{state.TraceId} Stream completing resettable event source.");

                shouldReadComplete = CleanupReadStateAndCheckPending(state, ReadState.ReadsCompleted);

                if (state.ShutdownWriteState == ShutdownWriteState.None)
                {
                    // TODO: We can get to this point if the stream is unidirectional and there are no writes.
                    // Consider what is the best behavior here with write shutdown and the read side of
                    // unidirecitonal streams in the future.
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
                if (state.StartStatus == MsQuicStatusCodes.Success)
                {
                    state.ReceiveResettableCompletionSource.Complete(0);
                }
                else
                {
                    state.ReceiveResettableCompletionSource.CompleteException(
                        ExceptionDispatchInfo.SetCurrentStackTrace(new QuicOperationAbortedException($"Stream start failed with {MsQuicStatusCodes.GetError(state.StartStatus)}")));
                }
            }

            if (shouldShutdownWriteComplete)
            {
                if (state.StartStatus == MsQuicStatusCodes.Success)
                {
                    state.ShutdownWriteCompletionSource.SetResult();
                }
                else
                {
                    state.ShutdownWriteCompletionSource.SetException(
                        ExceptionDispatchInfo.SetCurrentStackTrace(new QuicOperationAbortedException($"Stream start failed with {MsQuicStatusCodes.GetError(state.StartStatus)}")));
                }
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
                shouldComplete = CleanupReadStateAndCheckPending(state, ReadState.Aborted);
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
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(state, $"{state.TraceId} Stream completing resettable event source.");

                shouldComplete = CleanupReadStateAndCheckPending(state, ReadState.ReadsCompleted);
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

            Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
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

                if (status == MsQuicStatusCodes.Aborted)
                {
                    throw ThrowHelper.GetConnectionAbortedException(_state.ConnectionState.AbortErrorCode);
                }
                QuicExceptionHelpers.ThrowIfFailed(status,
                    "Could not send data to peer.");
            }

            return _state.SendResettableCompletionSource.GetTypelessValueTask();
        }

        private unsafe ValueTask SendReadOnlySequenceAsync(
           ReadOnlySequence<byte> buffers,
           QUIC_SEND_FLAGS flags)
        {
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

            Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
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

                if (status == MsQuicStatusCodes.Aborted)
                {
                    throw ThrowHelper.GetConnectionAbortedException(_state.ConnectionState.AbortErrorCode);
                }
                QuicExceptionHelpers.ThrowIfFailed(status,
                    "Could not send data to peer.");
            }

            return _state.SendResettableCompletionSource.GetTypelessValueTask();
        }

        private unsafe ValueTask SendReadOnlyMemoryListAsync(
           ReadOnlyMemory<ReadOnlyMemory<byte>> buffers,
           QUIC_SEND_FLAGS flags)
        {
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

            Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
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

                if (status == MsQuicStatusCodes.Aborted)
                {
                    throw ThrowHelper.GetConnectionAbortedException(_state.ConnectionState.AbortErrorCode);
                }
                QuicExceptionHelpers.ThrowIfFailed(status,
                    "Could not send data to peer.");
            }

            return _state.SendResettableCompletionSource.GetTypelessValueTask();
        }

        private void ReceiveComplete(int bufferLength)
        {
            Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
            MsQuicApi.Api.StreamReceiveCompleteDelegate(_state.Handle, (ulong)bufferLength);
        }

        // This can fail if the stream isn't started.
        private long GetStreamId()
        {
            return (long)MsQuicParameterHelpers.GetULongParam(MsQuicApi.Api, _state.Handle, (uint)QUIC_PARAM_STREAM.ID);
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
                NetEventSource.Info(state, $"{state.TraceId} Stream handling connection {state.ConnectionState.TraceId} close" +
                    (errorCode != -1 ? $" with code {errorCode}" : ""));
            }

            bool shouldCompleteRead = false;
            bool shouldCompleteSend = false;
            bool shouldCompleteShutdownWrite = false;
            bool shouldCompleteShutdown = false;

            lock (state)
            {
                shouldCompleteRead = CleanupReadStateAndCheckPending(state, ReadState.ConnectionClosed);

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

            // Dispose was called before complete event.
            bool releaseHandles = Interlocked.Exchange(ref state.ShutdownDone, 2) == 1;
            if (releaseHandles)
            {
                state.Cleanup();
            }

            return MsQuicStatusCodes.Success;
        }

        private static Exception GetConnectionAbortedException(State state) =>
            ThrowHelper.GetConnectionAbortedException(state.ConnectionState.AbortErrorCode);

        private static bool CleanupReadStateAndCheckPending(State state, ReadState finalState)
        {
            Debug.Assert(finalState >= ReadState.ReadsCompleted, $"Expected final read state, got {finalState}");
            Debug.Assert(Monitor.IsEntered(state));

            bool shouldComplete = false;
            if (state.ReadState == ReadState.PendingRead)
            {
                shouldComplete = true;
                state.Stream = null;
                state.ReceiveUserBuffer = null;
                state.ReceiveCancellationRegistration.Unregister();
            }
            if (state.ReadState < ReadState.ReadsCompleted)
            {
                state.ReadState = finalState;
            }
            return shouldComplete;
        }

        // Read state transitions:
        //
        // None  --(data arrives in event RECV)->  IndividualReadComplete
        // None  --(data arrives in event RECV with FIN flag)->  IndividualReadComplete(+FIN)
        // None  --(0-byte data arrives in event RECV with FIN flag)->  ReadsCompleted
        // None  --(user calls ReadAsync() & waits)->  PendingRead
        //
        // IndividualReadComplete  --(user calls ReadAsync())->  None
        // IndividualReadComplete(+FIN)  --(user calls ReadAsync() & consumes only partial data)->  None
        // IndividualReadComplete(+FIN)  --(user calls ReadAsync() & consumes full data)->  ReadsCompleted
        //
        // PendingRead  --(data arrives in event RECV & completes user's ReadAsync())->  None
        // PendingRead  --(data arrives in event RECV with FIN flag & completes user's ReadAsync() with only partial data)->  None
        // PendingRead  --(data arrives in event RECV with FIN flag & completes user's ReadAsync() with full data)->  ReadsCompleted
        //
        // Any non-final state  --(event PEER_SEND_SHUTDOWN or SHUTDOWN_COMPLETED with ConnectionClosed=false)->  ReadsCompleted
        // Any non-final state  --(event PEER_SEND_ABORT)->  Aborted
        // Any non-final state  --(user calls AbortRead())->  Aborted
        // Any non-final state  --(CancellationToken's cancellation for ReadAsync())->  Aborted
        // Any non-final state  --(event SHUTDOWN_COMPLETED with ConnectionClosed=true)->  ConnectionClosed
        //
        // Closed - no transitions, set for Unidirectional write-only streams
        private enum ReadState
        {
            /// <summary>
            /// The stream is open, but there is no data available.
            /// </summary>
            None = 0,

            /// <summary>
            /// Data is available in <see cref="State.ReceiveQuicBuffers"/>.
            /// </summary>
            IndividualReadComplete,

            /// <summary>
            /// User called ReadAsync()
            /// </summary>
            PendingRead,

            // following states are final:

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
            None = 0,
            Canceled,
            Finished,
            ConnectionClosed
        }

        private enum ShutdownState
        {
            None = 0,
            Canceled,
            Pending,
            Finished,
            ConnectionClosed
        }

        private enum SendState
        {
            None = 0,
            Pending,
            Finished,

            // Terminal states
            Aborted,
            ConnectionClosed,
            Closed
        }
    }
}
