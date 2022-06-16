// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Quic;
using static Microsoft.Quic.MsQuic;

namespace System.Net.Quic.Implementations.MsQuic
{
    internal sealed class MsQuicStream : QuicStreamProvider
    {
        // The state is passed to msquic and then it's passed back by msquic to the callback handler.
        private readonly State _state = new State();

        private readonly bool _canRead;
        private readonly bool _canWrite;

        private int _disposed;

        private sealed class State
        {
            public SafeMsQuicStreamHandle Handle = null!; // set in ctor.
            // Roots the state in GC and it won't get collected while this exist.
            // It must be kept alive until we receive SHUTDOWN_COMPLETE event
            public GCHandle StateGCHandle;

            public long StreamId = -1;

            public MsQuicStream? Stream; // roots the stream in the pinned state to prevent GC during an async read I/O.
            public MsQuicConnection.State ConnectionState = null!; // set in ctor.

            public ReadState ReadState;

            // set when ReadState.Aborted:
            public long ReadErrorCode = -1;

            // filled when ReadState.BuffersAvailable:
            public QUIC_BUFFER[] ReceiveQuicBuffers = Array.Empty<QUIC_BUFFER>();
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

            public MsQuicBuffers SendBuffers;

            // Resettable completions to be used for multiple calls to send.
            public readonly ResettableCompletionSource<int> SendResettableCompletionSource = new ResettableCompletionSource<int>();

            public ShutdownWriteState ShutdownWriteState;

            // Set once writes have been shutdown.
            public readonly TaskCompletionSource ShutdownWriteCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            // Set once stream has been started and within peer's advertised stream limits
            public readonly TaskCompletionSource StartCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            public ShutdownState ShutdownState;

            // The value makes sure that we release the handles only once.
            public int ShutdownDone;
            public const int ShutdownDone_Disposed = 1;
            public const int ShutdownDone_NotificationReceived = 2;

            // Set once stream have been shutdown.
            public readonly TaskCompletionSource ShutdownCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            public State()
            {
                SendBuffers = new MsQuicBuffers();
            }

            public void Cleanup()
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"{Handle} releasing handles.");

                ShutdownState = ShutdownState.Finished;
                CleanupSendState(this);
                Handle?.Dispose();
                SendBuffers.Dispose();
                if (StateGCHandle.IsAllocated) StateGCHandle.Free();
                ConnectionState?.RemoveStream(null);
            }
        }

        // inbound.
        internal unsafe MsQuicStream(MsQuicConnection.State connectionState, SafeMsQuicStreamHandle streamHandle, QUIC_STREAM_OPEN_FLAGS flags)
        {
            if (!connectionState.TryAddStream(this))
            {
                throw new ObjectDisposedException(nameof(QuicConnection));
            }
            // this assignment should be done before SetCallbackHandlerDelegate to prevent NRE in HandleEventConnectionClose
            // but after TryAddStream to prevent unnecessary RemoveStream in finalizer
            _state.ConnectionState = connectionState;

            // Inbound streams are already started
            _state.StartCompletionSource.SetResult();
            _state.Handle = streamHandle;
            _state.StreamId = GetStreamId(streamHandle);

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
                MsQuicApi.Api.ApiTable->SetStreamCallback(_state.Handle.QuicHandle, &NativeCallback, (void*)GCHandle.ToIntPtr(_state.StateGCHandle));
            }
            catch
            {
                _state.StateGCHandle.Free();
                // don't free the streamHandle, it will be freed by the caller
                throw;
            }

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(
                    _state,
                    $"{_state.Handle} Inbound {(flags.HasFlag(QUIC_STREAM_OPEN_FLAGS.UNIDIRECTIONAL) ? "uni" : "bi")}directional stream created " +
                        $"in connection {_state.ConnectionState.Handle} with StreamId {_state.StreamId}.");
            }
        }

        // outbound.
        internal unsafe MsQuicStream(MsQuicConnection.State connectionState, QUIC_STREAM_OPEN_FLAGS flags)
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
                QUIC_HANDLE* handle;
                Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
                int status = MsQuicApi.Api.ApiTable->StreamOpen(
                    connectionState.Handle.QuicHandle,
                    flags,
                    &NativeCallback,
                    (void*)GCHandle.ToIntPtr(_state.StateGCHandle),
                    &handle);

                if (status == QUIC_STATUS_ABORTED)
                {
                    // connection already aborted by peer, throw relevant exception
                    throw ThrowHelper.GetConnectionAbortedException(connectionState.AbortErrorCode);
                }

                ThrowIfFailure(status, "Failed to open stream to peer");
                _state.Handle = new SafeMsQuicStreamHandle(handle);
            }
            catch
            {
                _state.Handle?.Dispose();
                _state.StateGCHandle.Free();
                throw;
            }

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(
                    _state,
                    $"{_state.Handle} Outbound {(flags.HasFlag(QUIC_STREAM_OPEN_FLAGS.UNIDIRECTIONAL) ? "uni" : "bi")}directional stream created " +
                        $"in connection {_state.ConnectionState.Handle}.");
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
                Debug.Assert(_state.StreamId != -1);
                return _state.StreamId;
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

        internal override ValueTask WriteAsync(ReadOnlySequence<byte> buffers, bool endStream, CancellationToken cancellationToken = default)
        {
            return WriteAsync(static (state, buffers) => state.SendBuffers.Initialize(buffers), buffers, buffers.IsEmpty, endStream, cancellationToken);
        }

        internal override ValueTask WriteAsync(ReadOnlyMemory<ReadOnlyMemory<byte>> buffers, CancellationToken cancellationToken = default)
        {
            return WriteAsync(buffers, endStream: false, cancellationToken);
        }

        internal override ValueTask WriteAsync(ReadOnlyMemory<ReadOnlyMemory<byte>> buffers, bool endStream, CancellationToken cancellationToken = default)
        {
            return WriteAsync(static (state, buffers) => state.SendBuffers.Initialize(buffers), buffers, buffers.IsEmpty, endStream, cancellationToken);
        }

        internal override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, bool endStream, CancellationToken cancellationToken = default)
        {
            return WriteAsync(static (state, buffer) => state.SendBuffers.Initialize(buffer), buffer, buffer.IsEmpty, endStream, cancellationToken);
        }

        private async ValueTask WriteAsync<TBuffer>(Action<State, TBuffer> stateSetup, TBuffer buffer, bool isEmpty, bool endStream, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

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

            if (_state.SendState == SendState.Closed)
            {
                throw new InvalidOperationException(SR.net_quic_writing_notallowed);
            }
            // Use Volatile.Read to ensure we read the actual SendErrorCode set by the racing callback thread.
            if ((SendState)Volatile.Read(ref Unsafe.As<SendState, int>(ref _state.SendState)) == SendState.Aborted)
            {
                if (_state.SendErrorCode != -1)
                {
                    // aborted by peer
                    throw new QuicStreamAbortedException(_state.SendErrorCode);
                }

                // aborted locally
                throw new QuicOperationAbortedException(SR.net_quic_sending_aborted);
            }

            // if token was already cancelled, this would execute synchronously
            using CancellationTokenRegistration registration = cancellationToken.UnsafeRegister(static (s, token) =>
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
                        // aborted by peer
                        throw new QuicStreamAbortedException(_state.SendErrorCode);
                    }

                    // aborted locally
                    throw new QuicOperationAbortedException(SR.net_quic_sending_aborted);
                }
                if (_state.SendState == SendState.ConnectionClosed)
                {
                    throw GetConnectionAbortedException(_state);
                }

                if (_state.SendState == SendState.Pending || _state.SendState == SendState.Finished)
                {
                    throw new InvalidOperationException(SR.Format(SR.net_io_invalidnestedcall, "write"));
                }

                // Change the state in the same lock where we check for final states to prevent coming back from Aborted/ConnectionClosed.
                Debug.Assert(_state.SendState != SendState.Pending);
                _state.SendState = isEmpty ? SendState.Finished : SendState.Pending;
            }

            await WriteAsyncCore<TBuffer>(stateSetup, buffer, isEmpty, endStream).ConfigureAwait(false);

            lock (_state)
            {
                if (_state.SendState == SendState.Finished)
                {
                    _state.SendState = SendState.None;
                }
            }
        }

        private unsafe ValueTask WriteAsyncCore<TBuffer>(Action<State, TBuffer> stateSetup, TBuffer buffer, bool isEmpty, bool endStream)
        {
            if (isEmpty)
            {
                if (endStream)
                {
                    // Start graceful shutdown sequence if passed in the fin flag and there is an empty buffer.
                    StartShutdown(QUIC_STREAM_SHUTDOWN_FLAGS.GRACEFUL, errorCode: 0);
                }
                return default;
            }

            stateSetup(_state, buffer);

            Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
            int status = MsQuicApi.Api.ApiTable->StreamSend(
                _state.Handle.QuicHandle,
                _state.SendBuffers.Buffers,
                (uint)_state.SendBuffers.Count,
                endStream ? QUIC_SEND_FLAGS.FIN : QUIC_SEND_FLAGS.NONE,
                (void*)IntPtr.Zero);

            if (StatusFailed(status))
            {
                lock (_state)
                {
                    if (_state.SendState == SendState.Pending)
                    {
                        _state.SendState = SendState.Finished;
                    }
                }

                CleanupSendState(_state);

                if (status == QUIC_STATUS_ABORTED)
                {
                    throw ThrowHelper.GetConnectionAbortedException(_state.ConnectionState.AbortErrorCode);
                }
                ThrowIfFailure(status, "Could not send data to peer.");
            }

            return _state.SendResettableCompletionSource.GetTypelessValueTask();
        }

        internal override async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
        {
            //
            // If MsQuic indicated that some data were received (QUIC_STREAM_EVENT_RECEIVE), we use it to complete the request
            // synchronously. Otherwise we setup the request to be completed by the HandleEventReceive handler.
            //

            ThrowIfDisposed();

            if (_state.ReadState == ReadState.Closed)
            {
                throw new InvalidOperationException(SR.net_quic_reading_notallowed);
            }

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(_state, $"{_state.Handle} Stream reading into Memory of '{destination.Length}' bytes.");
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
                // PendingRead or PendingReadFinished state indicates there is another concurrent read operation in flight
                // which is forbidden, so it is handled separately
                if (initialReadState != ReadState.PendingRead && initialReadState != ReadState.PendingReadFinished && cancellationToken.IsCancellationRequested)
                {
                    initialReadState = ReadState.Aborted;
                    CleanupReadStateAndCheckPending(_state, ReadState.Aborted);
                    preCanceled = true;
                }

                // Success scenario: EOS already reached, completing synchronously. No transition (final state)
                if (initialReadState == ReadState.ReadsCompleted)
                {
                    return 0;
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

            if (initialReadState == ReadState.None)
            {
                // wait for the incoming data to finish the read.
                bytesRead = await _state.ReceiveResettableCompletionSource.GetValueTask().ConfigureAwait(false);

                // Reset the read state
                lock (_state)
                {
                    if (_state.ReadState == ReadState.PendingReadFinished)
                    {
                        _state.ReadState = ReadState.None;
                    }
                }

                return bytesRead;
            }

            // methods below need to be called outside of the lock
            if (bytesRead > -1)
            {
                ReceiveComplete(bytesRead);

                if (reenableReceive)
                {
                    EnableReceive();
                }

                return bytesRead;
            }

            // All success scenarios returned at this point. Failure scenarios below:

            Exception? ex = null;

            switch (initialReadState)
            {
                case ReadState.PendingRead:
                case ReadState.PendingReadFinished:
                    ex = new InvalidOperationException(SR.Format(SR.net_io_invalidnestedcall, "read"));
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

            throw ex;
        }

        /// <returns>The number of bytes copied.</returns>
        private static unsafe int CopyMsQuicBuffersToUserBuffer(ReadOnlySpan<QUIC_BUFFER> sourceBuffers, Span<byte> destinationBuffer)
        {
            if (sourceBuffers.Length == 0)
            {
                return 0;
            }

            int originalDestinationLength = destinationBuffer.Length;
            QUIC_BUFFER nativeBuffer;
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
            bool shouldCompleteSends = false;

            lock (_state)
            {
                if (_state.SendState == SendState.None || _state.SendState == SendState.Pending)
                {
                    shouldCompleteSends = true;
                }

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

            if (shouldCompleteSends)
            {
                _state.SendResettableCompletionSource.CompleteException(
                    ExceptionDispatchInfo.SetCurrentStackTrace(new QuicOperationAbortedException("Write was aborted.")));
            }

            StartShutdown(QUIC_STREAM_SHUTDOWN_FLAGS.ABORT_SEND, errorCode);
        }

        private unsafe void StartShutdown(QUIC_STREAM_SHUTDOWN_FLAGS flags, long errorCode)
        {
            Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
            ThrowIfFailure(MsQuicApi.Api.ApiTable->StreamShutdown(
                _state.Handle.QuicHandle,
                flags,
                (uint)errorCode), "StreamShutdown failed");
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


            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(_state, $"{_state.Handle} Stream disposing {disposing}");

            bool callShutdown = false;
            bool abortRead = false;
            bool completeRead = false;
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
            }

            if (_state.Handle != null && !_state.Handle.IsInvalid && !_state.Handle.IsClosed)
            {
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
                        // TODO: error code used here MUST be specified by the application layer
                        StartShutdown(QUIC_STREAM_SHUTDOWN_FLAGS.ABORT_RECEIVE, 0xffffffff);
                    }
                    catch (ObjectDisposedException) { };
                }
            }

            if (completeRead)
            {
                _state.ReceiveResettableCompletionSource.CompleteException(
                    ExceptionDispatchInfo.SetCurrentStackTrace(new QuicOperationAbortedException("Read was canceled")));
            }


            // Check if we already got final event.
            bool releaseHandles = Interlocked.Exchange(ref _state.ShutdownDone, State.ShutdownDone_Disposed) == State.ShutdownDone_NotificationReceived;
            if (releaseHandles)
            {
                _state.Cleanup();
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(_state, $"{_state.Handle} Stream disposed");
        }

        private unsafe void EnableReceive()
        {
            Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
            ThrowIfFailure(MsQuicApi.Api.ApiTable->StreamReceiveSetEnabled(_state.Handle.QuicHandle, 1), "StreamReceiveSetEnabled failed");
        }

        /// <summary>
        /// Callback calls for a single instance of a stream are serialized by msquic.
        /// They happen on a msquic thread and shouldn't take too long to not to block msquic.
        /// </summary>
#pragma warning disable CS3016
        [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvCdecl) })]
#pragma warning restore CS3016
        private static unsafe int NativeCallback(QUIC_HANDLE* stream, void* context, QUIC_STREAM_EVENT* streamEvent)
        {
            GCHandle gcHandle = GCHandle.FromIntPtr((IntPtr)context);
            Debug.Assert(gcHandle.IsAllocated);
            Debug.Assert(gcHandle.Target is not null);
            var state = (State)gcHandle.Target;

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(state, $"{state.Handle} Stream received event {streamEvent->Type}");
            }

            try
            {
                switch (streamEvent->Type)
                {
                    // Stream has started.
                    // Will only be done for outbound streams (inbound streams have already started)
                    case QUIC_STREAM_EVENT_TYPE.START_COMPLETE:
                        return HandleEventStartComplete(state, ref *streamEvent);
                    // Received data on the stream
                    case QUIC_STREAM_EVENT_TYPE.RECEIVE:
                        return HandleEventReceive(state, ref *streamEvent);
                    // Send has completed.
                    // Contains a canceled bool to indicate if the send was canceled.
                    case QUIC_STREAM_EVENT_TYPE.SEND_COMPLETE:
                        return HandleEventSendComplete(state, ref *streamEvent);
                    // Peer has told us to shutdown the reading side of the stream.
                    case QUIC_STREAM_EVENT_TYPE.PEER_SEND_SHUTDOWN:
                        return HandleEventPeerSendShutdown(state);
                    // Peer has told us to abort the reading side of the stream.
                    case QUIC_STREAM_EVENT_TYPE.PEER_SEND_ABORTED:
                        return HandleEventPeerSendAborted(state, ref *streamEvent);
                    // Peer has stopped receiving data, don't send anymore.
                    case QUIC_STREAM_EVENT_TYPE.PEER_RECEIVE_ABORTED:
                        return HandleEventPeerRecvAborted(state, ref *streamEvent);
                    // Occurs when shutdown is completed for the send side.
                    // This only happens for shutdown on sending, not receiving
                    // Receive shutdown can only be abortive.
                    case QUIC_STREAM_EVENT_TYPE.SEND_SHUTDOWN_COMPLETE:
                        return HandleEventSendShutdownComplete(state, ref *streamEvent);
                    // Shutdown for both sending and receiving is completed.
                    case QUIC_STREAM_EVENT_TYPE.SHUTDOWN_COMPLETE:
                        return HandleEventShutdownComplete(state, ref *streamEvent);
                    // Asynchronous open finished, the stream is now within advertised stream limits.
                    case QUIC_STREAM_EVENT_TYPE.PEER_ACCEPTED:
                        return HandleEventPeerAccepted(state);
                    default:
                        return QUIC_STATUS_SUCCESS;
                }
            }
            catch (Exception ex)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Error(state, $"{state.Handle} Exception occurred during handling Stream {streamEvent->Type} event: {ex}");
                }

                Debug.Fail($"{state.Handle} Exception occurred during handling Stream {streamEvent->Type} event: {ex}");

                return QUIC_STATUS_INTERNAL_ERROR;
            }
        }

        private static unsafe int HandleEventReceive(State state, ref QUIC_STREAM_EVENT streamEvent)
        {
            //
            // Handle MsQuic QUIC_STREAM_EVENT_RECEIVE event
            //
            // If there is a pending ReadAsync call, then we complete it. Otherwise we keep a pointer to the received data
            // and use it to complete the next ReadAsync operation synchronously.
            //

            ref var receiveEvent = ref streamEvent.RECEIVE;

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(state, $"{state.Handle} Stream received {receiveEvent.TotalBufferLength} bytes{(receiveEvent.Flags.HasFlag(QUIC_RECEIVE_FLAGS.FIN) ? " with FIN flag" : "")}");
            }

            int readLength;

            bool shouldComplete = false;
            lock (state)
            {
                switch (state.ReadState)
                {
                    // ReadAsync() hasn't been called yet.
                    case ReadState.None:
                    // A pending read has just been finished, and this is a second event in a row (before reading thread
                    // managed to clear the state)
                    case ReadState.PendingReadFinished:
                        // Stash the buffer so the next ReadAsync call completes synchronously.

                        // We are overwriting state.ReceiveQuicBuffers here even if we only partially consumed them
                        // and it is intended, because unconsumed data will arrive again from the point we've stopped.
                        // New RECEIVE event wouldn't come until we call EnableReceive(), and we call it only after we've consumed
                        // as much as we could and said so to msquic in ReceiveComplete(taken), so new event will have all the
                        // remaining data.

                        if ((uint)state.ReceiveQuicBuffers.Length < receiveEvent.BufferCount)
                        {
                            QUIC_BUFFER[] oldReceiveBuffers = state.ReceiveQuicBuffers;
                            state.ReceiveQuicBuffers = ArrayPool<QUIC_BUFFER>.Shared.Rent((int)receiveEvent.BufferCount);

                            if (oldReceiveBuffers.Length != 0) // don't return Array.Empty.
                            {
                                ArrayPool<QUIC_BUFFER>.Shared.Return(oldReceiveBuffers);
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
                            return QUIC_STATUS_SUCCESS;
                        }
                        else
                        {
                            // Normal RECEIVE - data will be buffered until user calls ReadAsync() and no new event will be issued until EnableReceive()
                            state.ReadState = ReadState.IndividualReadComplete;
                            return QUIC_STATUS_PENDING;
                        }

                    case ReadState.PendingRead:
                        // There is a pending ReadAsync().

                        state.ReceiveCancellationRegistration.Unregister();
                        shouldComplete = true;
                        state.Stream = null;
                        state.ReadState = ReadState.PendingReadFinished;
                        // state.ReadState will be set to None later once the ReceiveResettableCompletionSource is awaited.

                        readLength = CopyMsQuicBuffersToUserBuffer(new ReadOnlySpan<QUIC_BUFFER>(receiveEvent.Buffers, (int)receiveEvent.BufferCount), state.ReceiveUserBuffer.Span);

                        // This was a final message and we've consumed everything. We can complete the state without waiting for PEER_SEND_SHUTDOWN
                        if (receiveEvent.Flags.HasFlag(QUIC_RECEIVE_FLAGS.FIN) && (uint)readLength == receiveEvent.TotalBufferLength)
                        {
                            state.ReadState = ReadState.ReadsCompleted;
                        }
                        // Else, if this was a final message, but we haven't consumed it fully, FIN flag will arrive again in the next RECEIVE event

                        state.ReceiveUserBuffer = null;
                        break;

                    default:
                        Debug.Assert(state.ReadState is ReadState.Aborted or ReadState.ConnectionClosed, $"Unexpected {nameof(ReadState)} '{state.ReadState}' in {nameof(HandleEventReceive)}.");

                        // There was a race between a user aborting the read stream and the callback being ran.
                        // This will eat any received data.
                        return QUIC_STATUS_SUCCESS;
                }
            }

            if (shouldComplete)
            {
                state.ReceiveResettableCompletionSource.Complete(readLength);
                // _state.ReadState will be reset to None on the reading thread.
            }

            // Returning Success when the entire buffer hasn't been consumed will cause MsQuic to disable further receive events until EnableReceive() is called.
            // Returning Continue will cause a second receive event to fire immediately after this returns, but allows MsQuic to clean up its buffers.

            int ret = (uint)readLength == receiveEvent.TotalBufferLength
                ? QUIC_STATUS_SUCCESS
                : QUIC_STATUS_CONTINUE;

            receiveEvent.TotalBufferLength = (uint)readLength;
            return ret;
        }

        private static int HandleEventPeerRecvAborted(State state, ref QUIC_STREAM_EVENT streamEvent)
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

                state.SendErrorCode = (long)streamEvent.PEER_RECEIVE_ABORTED.ErrorCode;
                // make sure the SendErrorCode above is commited to memory before we assign the state. This
                // ensures that the code is read correctly in SetupWriteStartState when checking without lock
                Volatile.Write(ref Unsafe.As<SendState, int>(ref state.SendState), (int)SendState.Aborted);
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

            return QUIC_STATUS_SUCCESS;
        }

        private static int HandleEventStartComplete(State state, ref QUIC_STREAM_EVENT streamEvent)
        {
            int status = streamEvent.START_COMPLETE.Status;

            // The way we expose Open(Uni|Bi)directionalStreamAsync operations is that the stream
            // is also accepted by the peer (i.e. it is within advertised stream limits). However,
            // We may receive START_COMPLETE notification before the stream is accepted, so we defer
            // completing the StartcompletionSource until we get PeerAccepted notification.

            if (StatusSucceeded(status))
            {
                state.StreamId = (long)streamEvent.START_COMPLETE.ID;
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(state, $"{state.Handle} StreamId = {state.StreamId}");

                if (streamEvent.START_COMPLETE.PeerAccepted != 0)
                {
                    // Start succeeded and we were within stream limits, stream already usable.
                    state.StartCompletionSource.TrySetResult();
                }
                // if PeerAccepted == 0, we will later receive PEER_ACCEPTED event, which will
                // complete the StartCompletionSource
            }
            else
            {
                // Start irrecoverably failed. The possible status codes are:
                //   - Aborted - connection aborted by peer
                //   - InvalidState - stream already started before, or connection aborted locally
                //   - StreamLimitReached - only if QUIC_STREAM_START_FLAG_FAIL_BLOCKED was specified (not in our case).
                //
                if (status == QUIC_STATUS_ABORTED)
                {
                    state.StartCompletionSource.TrySetException(
                        ExceptionDispatchInfo.SetCurrentStackTrace(GetConnectionAbortedException(state)));
                }
                else
                {
                    // TODO: Should we throw QuicOperationAbortedException when status is InvalidState?
                    // [ActiveIssue("https://github.com/dotnet/runtime/issues/55619")]
                    state.StartCompletionSource.TrySetException(
                        ExceptionDispatchInfo.SetCurrentStackTrace(new MsQuicException(status, "StreamStart failed")));
                }
            }

            return QUIC_STATUS_SUCCESS;
        }

        private static int HandleEventSendShutdownComplete(State state, ref QUIC_STREAM_EVENT streamEvent)
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
            if (streamEvent.SEND_SHUTDOWN_COMPLETE.Graceful != 0)
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

            return QUIC_STATUS_SUCCESS;
        }

        private static int HandleEventShutdownComplete(State state, ref QUIC_STREAM_EVENT streamEvent)
        {
            var shutdownCompleteEvent = streamEvent.SHUTDOWN_COMPLETE;

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
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(state, $"{state.Handle} Stream completing resettable event source.");

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
                if (state.StartCompletionSource.Task.IsCompletedSuccessfully)
                {
                    state.ReceiveResettableCompletionSource.Complete(0);
                }
                else
                {
                    state.ReceiveResettableCompletionSource.CompleteException(
                        ExceptionDispatchInfo.SetCurrentStackTrace(new QuicOperationAbortedException($"Stream start failed")));
                }
            }

            if (shouldShutdownWriteComplete)
            {
                if (state.StartCompletionSource.Task.IsCompletedSuccessfully)
                {
                    state.ShutdownWriteCompletionSource.SetResult();
                }
                else
                {
                    state.ShutdownWriteCompletionSource.SetException(
                        ExceptionDispatchInfo.SetCurrentStackTrace(new QuicOperationAbortedException($"Stream start failed")));
                }
            }

            if (shouldShutdownComplete)
            {
                state.ShutdownCompletionSource.SetResult();
            }

            // If we are receiving stream shutdown notification, the start comletion source must have been already completed
            // eihter by StreamOpen or PeerAccepted event, Connection closing, or it was cancelled by user.
            Debug.Assert(state.StartCompletionSource.Task.IsCompleted);

            // Dispose was called before complete event.
            bool releaseHandles = Interlocked.Exchange(ref state.ShutdownDone, State.ShutdownDone_NotificationReceived) == State.ShutdownDone_Disposed;
            if (releaseHandles)
            {
                state.Cleanup();
            }

            return QUIC_STATUS_SUCCESS;
        }

        private static int HandleEventPeerAccepted(State state)
        {
            state.StartCompletionSource.TrySetResult();
            return QUIC_STATUS_SUCCESS;
        }

        private static int HandleEventPeerSendAborted(State state, ref QUIC_STREAM_EVENT streamEvent)
        {
            bool shouldComplete = false;
            lock (state)
            {
                shouldComplete = CleanupReadStateAndCheckPending(state, ReadState.Aborted);
                state.ReadErrorCode = (long)streamEvent.PEER_SEND_ABORTED.ErrorCode;
            }

            if (shouldComplete)
            {
                state.ReceiveResettableCompletionSource.CompleteException(
                    ExceptionDispatchInfo.SetCurrentStackTrace(new QuicStreamAbortedException(state.ReadErrorCode)));
            }

            return QUIC_STATUS_SUCCESS;
        }

        private static int HandleEventPeerSendShutdown(State state)
        {
            bool shouldComplete = false;

            lock (state)
            {
                // This event won't occur within the middle of a receive.
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(state, $"{state.Handle} Stream completing resettable event source.");

                shouldComplete = CleanupReadStateAndCheckPending(state, ReadState.ReadsCompleted);
            }

            if (shouldComplete)
            {
                state.ReceiveResettableCompletionSource.Complete(0);
            }

            return QUIC_STATUS_SUCCESS;
        }

        private static int HandleEventSendComplete(State state, ref QUIC_STREAM_EVENT streamEvent)
        {
            var sendCompleteEvent = streamEvent.SEND_COMPLETE;
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
                    state.SendResettableCompletionSource.Complete(QUIC_STATUS_SUCCESS);
                }
                else
                {
                    //
                    // There are multiple reasons the send could have been cancelled:
                    //   - Connection was aborted (either by transport or peer) => error-code already provided on the connection-level event
                    //   - Stream's receive side was aborted by peer => already handled by HandleEventPeerRecvAborted
                    //     and we will not set the exception due to complete == false
                    //   - Stream's send side was aborted locally => no connection-level abort code and we return QuicOperationAbortException
                    //
                    state.SendResettableCompletionSource.CompleteException(
                        ExceptionDispatchInfo.SetCurrentStackTrace(
                            ThrowHelper.GetConnectionAbortedException(state.ConnectionState.AbortErrorCode)));
                }
            }

            return QUIC_STATUS_SUCCESS;
        }

        private static void CleanupSendState(State state)
        {
            lock (state)
            {
                Debug.Assert(state.SendState != SendState.Pending);
                state.SendBuffers.Reset();
            }
        }

        private unsafe void ReceiveComplete(int bufferLength)
        {
            Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
            MsQuicApi.Api.ApiTable->StreamReceiveComplete(_state.Handle.QuicHandle, (ulong)bufferLength);
        }

        // This can fail if the stream isn't started.
        private static long GetStreamId(SafeMsQuicStreamHandle handle)
        {
            return (long)MsQuicParameterHelpers.GetULongParam(MsQuicApi.Api, handle, QUIC_PARAM_STREAM_ID);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed == 1)
            {
                throw new ObjectDisposedException(nameof(MsQuicStream));
            }
        }

        private static int HandleEventConnectionClose(State state)
        {
            long errorCode = state.ConnectionState.AbortErrorCode;
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(state, $"{state.Handle} Stream handling connection {state.ConnectionState.Handle} close" +
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

            if (!state.StartCompletionSource.Task.IsCompleted)
            {
                state.StartCompletionSource.TrySetException(
                    ExceptionDispatchInfo.SetCurrentStackTrace(GetConnectionAbortedException(state)));
            }

            // Dispose was called before complete event.
            bool releaseHandles = Interlocked.Exchange(ref state.ShutdownDone, State.ShutdownDone_NotificationReceived) == State.ShutdownDone_Disposed;
            if (releaseHandles)
            {
                state.Cleanup();
            }

            return QUIC_STATUS_SUCCESS;
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

        internal async ValueTask StartAsync(CancellationToken cancellationToken)
        {
            Debug.Assert(!Monitor.IsEntered(_state));

            using var registration = cancellationToken.UnsafeRegister((state, token) =>
            {
                ((State)state!).StartCompletionSource.TrySetCanceled(token);
            }, _state);

            int status;
            unsafe
            {
                status = MsQuicApi.Api.ApiTable->StreamStart(
                    _state.Handle.QuicHandle,
                    QUIC_STREAM_START_FLAGS.SHUTDOWN_ON_FAIL | QUIC_STREAM_START_FLAGS.INDICATE_PEER_ACCEPT);
            }

            if (!StatusSucceeded(status))
            {
                Exception exception = new MsQuicException(status, "Could not start stream");
                _state.StartCompletionSource.TrySetException(ExceptionDispatchInfo.SetCurrentStackTrace(exception));
                throw exception;
            }

            await _state.StartCompletionSource.Task.ConfigureAwait(false);
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
        // PendingRead  --(data arrives in event RECV & completes user's ReadAsync())->  PendingReadFinished
        // PendingRead  --(data arrives in event RECV with FIN flag & completes user's ReadAsync() with only partial data)->  PendingReadFinished
        // PendingRead  --(data arrives in event RECV with FIN flag & completes user's ReadAsync() with full data)->  ReadsCompleted
        //
        // PendingReadFinished --(reading thread awaits ReceiveResettableCompletionSource)-> None
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

            /// <summary>
            /// Read was completed from the MsQuic callback.
            /// </summary>
            PendingReadFinished,

            // following states are terminal:

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
            /// Stream is closed for reading (is send-only).
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

        // Send state transitions:
        //
        // None  --(user calls WriteAsync() & waits)->  Pending
        //
        // Pending  --(event SEND_COMPLETE.Canceled == 0)->  Finished
        // Pending  --(event SEND_COMPLETE.Canceled == 1)->  Aborted
        //
        // Finished  --(user awaits WriteAsync)->  None
        //
        // Any non-final state  --(event PEER_RECEIVE_ABORTED)->  Aborted (With SendErrorCode)
        // Any non-final state  --(user calls AbortWrite())->  Aborted
        // Any non-final state  --(CancellationToken's cancellation for WriteAsync())->  Aborted
        // Any non-final state  --(event SHUTDOWN_COMPLETED with ConnectionClosed=true)->  ConnectionClosed
        //
        // Closed - no transitions, set for Unidirectional read-only streams
        private enum SendState
        {
            /// <summary>
            /// The stream is open and there are no pending write operations.
            /// </summary>
            None = 0,

            /// <summary>
            /// There is a pending WriteAsync operation awaiting completion notification from MsQuic.
            /// </summary>
            Pending,

            /// <summary>
            /// Send completion notification from MsQuic was received.
            /// </summary>
            Finished,

            // following states are terminal:

            /// <summary>
            /// User has aborted the stream, either via a cancellation token on WriteAsync(), or via AbortWrite().
            /// </summary>
            Aborted,

            /// <summary>
            /// Connection was closed, either by user or by the peer.
            /// </summary>
            ConnectionClosed,

            /// <summary>
            /// Stream is closed for writing (is receive-only).
            /// </summary>
            Closed
        }
    }
}
