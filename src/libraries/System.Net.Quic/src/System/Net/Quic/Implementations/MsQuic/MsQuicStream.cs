// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Quic.Implementations.MsQuic.Internal.MsQuicNativeMethods;

namespace System.Net.Quic.Implementations.MsQuic
{
    internal sealed class MsQuicStream : QuicStreamProvider
    {
        // Pointer to the underlying stream
        // TODO replace all IntPtr with SafeHandles
        private readonly IntPtr _ptr;

        // Handle to this object for native callbacks.
        private GCHandle _handle;

        // Delegate that wraps the static function that will be called when receiving an event.
        private StreamCallbackDelegate _callback;

        // Backing for StreamId
        private long _streamId = -1;

        // Resettable completions to be used for multiple calls to send, start, and shutdown.
        private readonly ResettableCompletionSource<uint> _sendResettableCompletionSource;

        // Resettable completions to be used for multiple calls to receive.
        private readonly ResettableCompletionSource<uint> _receiveResettableCompletionSource;

        private readonly ResettableCompletionSource<uint> _shutdownWriteResettableCompletionSource;

        // Buffers to hold during a call to send.
        private readonly MemoryHandle[] _bufferArrays = new MemoryHandle[1];
        private readonly QuicBuffer[] _sendQuicBuffers = new QuicBuffer[1];

        // Handle to hold when sending.
        private GCHandle _sendHandle;

        // Used to check if StartAsync has been called.
        private StartState _started;

        private ReadState _readState;

        private ShutdownWriteState _shutdownState;

        private SendState _sendState;

        // Used by the class to indicate that the stream is m_Readable.
        private readonly bool _canRead;

        // Used by the class to indicate that the stream is writable.
        private readonly bool _canWrite;

        private volatile bool _disposed = false;

        private List<QuicBuffer> _receiveQuicBuffers = new List<QuicBuffer>();

        // TODO consider using Interlocked.Exchange instead of a sync if we can avoid it.
        private object _sync = new object();

        // Creates a new MsQuicStream
        internal MsQuicStream(MsQuicConnection connection, QUIC_STREAM_OPEN_FLAG flags, IntPtr nativeObjPtr, bool inbound)
        {
            Debug.Assert(connection != null);

            _ptr = nativeObjPtr;

            if (inbound)
            {
                _started = StartState.Finished;
                _canWrite = !flags.HasFlag(QUIC_STREAM_OPEN_FLAG.UNIDIRECTIONAL);
                _canRead = true;
            }
            else
            {
                _started = StartState.None;
                _canWrite = true;
                _canRead = !flags.HasFlag(QUIC_STREAM_OPEN_FLAG.UNIDIRECTIONAL);
            }

            _sendResettableCompletionSource = new ResettableCompletionSource<uint>();
            _receiveResettableCompletionSource = new ResettableCompletionSource<uint>();
            _shutdownWriteResettableCompletionSource = new ResettableCompletionSource<uint>();

            SetCallbackHandler();
        }

        internal override bool CanRead => _canRead;

        internal override bool CanWrite => _canWrite;

        internal override long StreamId
        {
            get
            {
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

        internal override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, bool endStream, CancellationToken cancellationToken = default)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            ThrowIfDisposed();

            if (!_canWrite)
            {
                throw new InvalidOperationException("Writing is not allowed on stream.");
            }

            lock (_sync)
            {
                if (_sendState == SendState.Aborted)
                {
                    throw new OperationCanceledException("Sending has already been aborted on the stream");
                }
            }

            using CancellationTokenRegistration registration = cancellationToken.Register(() =>
            {
                bool shouldComplete = false;
                lock (_sync)
                {
                    if (_sendState == SendState.None)
                    {
                        _sendState = SendState.Aborted;
                        shouldComplete = true;
                    }
                }

                if (shouldComplete)
                {
                    _sendResettableCompletionSource.CompleteException(new OperationCanceledException("Write was canceled"));
                }
            });

            // Implicit start on first write.
            if (_started == StartState.None)
            {
                _started = StartState.Started;
                await StartWritesAsync();
            }

            await SendAsync(buffer, endStream ? QUIC_SEND_FLAG.FIN : QUIC_SEND_FLAG.NONE);

            lock (_sync)
            {
                if (_sendState == SendState.Finished)
                {
                    _sendState = SendState.None;
                }
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        internal override async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            ThrowIfDisposed();

            if (!_canRead)
            {
                throw new InvalidOperationException("Reading is not allowed on stream.");
            }

            lock (_sync)
            {
                if (_readState == ReadState.ReadsCompleted)
                {
                    if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
                    return 0;
                }
                else if (_readState == ReadState.Aborted)
                {
                    throw new IOException("Reading has been aborted by the peer.");
                }
            }

            using CancellationTokenRegistration registration = cancellationToken.Register(() =>
            {
                bool shouldComplete = false;
                lock (_sync)
                {
                    if (_readState == ReadState.None)
                    {
                        shouldComplete = true;
                    }

                    _readState = ReadState.Aborted;
                }

                if (shouldComplete)
                {
                    _receiveResettableCompletionSource.CompleteException(new OperationCanceledException("Read was canceled"));
                }
            });

            // TODO there could potentially be a perf gain by storing the buffer from the inital read
            // This reduces the amount of async calls, however it makes it so MsQuic holds onto the buffers
            // longer than it needs to. We will need to benchmark this.
            int length = (int)await _receiveResettableCompletionSource.GetValueTask();

            int actual = Math.Min(length, destination.Length);

            static unsafe void CopyToBuffer(Span<byte> destinationBuffer, List<QuicBuffer> sourceBuffers)
            {
                Span<byte> slicedBuffer = destinationBuffer;
                for (int i = 0; i < sourceBuffers.Count; i++)
                {
                    QuicBuffer nativeBuffer = sourceBuffers[i];
                    int length = Math.Min((int)nativeBuffer.Length, slicedBuffer.Length);
                    new Span<byte>(nativeBuffer.Buffer, length).CopyTo(slicedBuffer);
                    if (length < slicedBuffer.Length)
                    {
                        return;
                    }
                    slicedBuffer = slicedBuffer.Slice(length);
                }
            }

            CopyToBuffer(destination.Span, _receiveQuicBuffers);

            lock (_sync)
            {
                if (_readState == ReadState.IndividualReadComplete)
                {
                    ReceiveComplete(actual);
                    EnableReceive();
                    _readState = ReadState.None;
                }
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);

            return actual;
        }

        // TODO do we want this to be a synchronization mechanism to cancel a pending read
        // If so, we need to complete the read here as well.
        internal override void AbortRead()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            ThrowIfDisposed();

            lock (_sync)
            {
                _readState = ReadState.Aborted;
            }

            MsQuicApi.Api.StreamShutdownDelegate(_ptr, (uint)QUIC_STREAM_SHUTDOWN_FLAG.ABORT_RECV, errorCode: 0);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        internal override ValueTask ShutdownWriteCompleted(CancellationToken cancellationToken = default)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            ThrowIfDisposed();

            // TODO do anything to stop writes?
            using CancellationTokenRegistration registration = cancellationToken.Register(() =>
            {
                bool shouldComplete = false;
                lock (_sync)
                {
                    if (_shutdownState == ShutdownWriteState.None)
                    {
                        _shutdownState = ShutdownWriteState.Canceled;
                        shouldComplete = true;
                    }
                }

                if (shouldComplete)
                {
                    _shutdownWriteResettableCompletionSource.CompleteException(new OperationCanceledException("Shutdown was canceled"));
                }
            });

            //var status = MsQuicApi.Api._streamShutdownDelegate(_ptr, (uint)QUIC_STREAM_SHUTDOWN_FLAG.GRACEFUL, errorCode: 0);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);

            return _shutdownWriteResettableCompletionSource.GetTypelessValueTask();
        }

        // TODO consider removing sync-over-async with blocking calls.
        internal override int Read(Span<byte> buffer)
        {
            ThrowIfDisposed();

            return ReadAsync(buffer.ToArray()).GetAwaiter().GetResult();
        }

        internal override void Write(ReadOnlySpan<byte> buffer)
        {
            ThrowIfDisposed();

            WriteAsync(buffer.ToArray()).GetAwaiter().GetResult();
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

            return default;
        }

        public override ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return default;
            }

            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            CleanupSendState();

            if (_ptr != IntPtr.Zero)
            {
                // TODO resolve graceful vs abortive dispose here. Will file a separate issue.
                //MsQuicApi.Api._streamShutdownDelegate(_ptr, (uint)QUIC_STREAM_SHUTDOWN_FLAG.ABORT, 1);
                MsQuicApi.Api.StreamCloseDelegate?.Invoke(_ptr);
            }

            _handle.Free();

            _disposed = true;
            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);

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
            if (_disposed)
            {
                return;
            }

            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            CleanupSendState();

            if (_ptr != IntPtr.Zero)
            {
                // TODO resolve graceful vs abortive dispose here. Will file a separate issue.
                //MsQuicApi.Api._streamShutdownDelegate(_ptr, (uint)QUIC_STREAM_SHUTDOWN_FLAG.ABORT, 1);
                MsQuicApi.Api.StreamCloseDelegate?.Invoke(_ptr);
            }

            _handle.Free();

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);

            _disposed = true;
        }

        private void EnableReceive()
        {
            MsQuicApi.Api.StreamReceiveSetEnabledDelegate(_ptr, enabled: true);
        }

        internal static uint NativeCallbackHandler(
           IntPtr stream,
           IntPtr context,
           StreamEvent connectionEventStruct)
        {
            var handle = GCHandle.FromIntPtr(context);
            var quicStream = (MsQuicStream)handle.Target;

            return quicStream.HandleEvent(ref connectionEventStruct);
        }

        private uint HandleEvent(ref StreamEvent evt)
        {
            uint status = MsQuicStatusCodes.Success;

            try
            {
                switch (evt.Type)
                {
                    // Stream has started.
                    // Will only be done for outbound streams (inbound streams have already started)
                    case QUIC_STREAM_EVENT.START_COMPLETE:
                        status = HandleStartComplete();
                        break;
                    // Received data on the stream
                    case QUIC_STREAM_EVENT.RECEIVE:
                        {
                            status = HandleEventRecv(ref evt);
                        }
                        break;
                    // Send has completed.
                    // Contains a canceled bool to indicate if the send was canceled.
                    case QUIC_STREAM_EVENT.SEND_COMPLETE:
                        {
                            status = HandleEventSendComplete(ref evt);
                        }
                        break;
                    // Peer has told us to shutdown the reading side of the stream.
                    case QUIC_STREAM_EVENT.PEER_SEND_SHUTDOWN:
                        {
                            status = HandleEventPeerSendShutdown();
                        }
                        break;
                    // Peer has told us to abort the reading side of the stream.
                    case QUIC_STREAM_EVENT.PEER_SEND_ABORTED:
                        {
                            status = HandleEventPeerSendAborted();
                        }
                        break;
                    // Peer has stopped receiving data, don't send anymore.
                    // Potentially throw when WriteAsync/FlushAsync.
                    case QUIC_STREAM_EVENT.PEER_RECEIVE_ABORTED:
                        {
                            status = HandleEventPeerRecvAbort();
                        }
                        break;
                    // Occurs when shutdown is completed for the send side.
                    // This only happens for shutdown on sending, not receiving
                    // Receive shutdown can only be abortive.
                    case QUIC_STREAM_EVENT.SEND_SHUTDOWN_COMPLETE:
                        {
                            status = HandleEventSendShutdownComplete(ref evt);
                        }
                        break;
                    // Shutdown for both sending and receiving is completed.
                    case QUIC_STREAM_EVENT.SHUTDOWN_COMPLETE:
                        {
                            status = HandleEventShutdownComplete();
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception)
            {
                return MsQuicStatusCodes.InternalError;
            }

            return status;
        }

        private unsafe uint HandleEventRecv(ref MsQuicNativeMethods.StreamEvent evt)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            StreamEventDataRecv receieveEvent = evt.Data.Recv;
            for (int i = 0; i < receieveEvent.BufferCount; i++)
            {
                _receiveQuicBuffers.Add(receieveEvent.Buffers[i]);
            }

            bool shouldComplete = false;
            lock (_sync)
            {
                if (_readState == ReadState.None)
                {
                    shouldComplete = true;
                }
                _readState = ReadState.IndividualReadComplete;
            }

            if (shouldComplete)
            {
                _receiveResettableCompletionSource.Complete((uint)receieveEvent.TotalBufferLength);
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);

            return MsQuicStatusCodes.Pending;
        }

        private uint HandleEventPeerRecvAbort()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);

            return MsQuicStatusCodes.Success;
        }

        private uint HandleStartComplete()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            bool shouldComplete = false;
            lock (_sync)
            {
                _started = StartState.Finished;

                // Check send state before completing as send cancellation is shared between start and send.
                if (_sendState == SendState.None)
                {
                    shouldComplete = true;
                }
            }

            if (shouldComplete)
            {
                _sendResettableCompletionSource.Complete(MsQuicStatusCodes.Success);
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);

            return MsQuicStatusCodes.Success;
        }

        private uint HandleEventSendShutdownComplete(ref MsQuicNativeMethods.StreamEvent evt)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            bool shouldComplete = false;
            lock (_sync)
            {
                if (_shutdownState == ShutdownWriteState.None)
                {
                    _shutdownState = ShutdownWriteState.Finished;
                    shouldComplete = true;
                }
            }

            if (shouldComplete)
            {
                _shutdownWriteResettableCompletionSource.Complete(MsQuicStatusCodes.Success);
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);

            return MsQuicStatusCodes.Success;
        }

        private uint HandleEventShutdownComplete()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            bool shouldReadComplete = false;
            bool shouldShutdownWriteComplete = false;

            lock (_sync)
            {
                // This event won't occur within the middle of a receive.
                if (NetEventSource.IsEnabled) NetEventSource.Info("Completing resettable event source.");

                if (_readState == ReadState.None)
                {
                    shouldReadComplete = true;
                }

                _readState = ReadState.ReadsCompleted;

                if (_shutdownState == ShutdownWriteState.None)
                {
                    _shutdownState = ShutdownWriteState.Finished;
                    shouldShutdownWriteComplete = true;
                }
            }

            if (shouldReadComplete)
            {
                _receiveResettableCompletionSource.Complete(0);
            }

            if (shouldShutdownWriteComplete)
            {
                _shutdownWriteResettableCompletionSource.Complete(MsQuicStatusCodes.Success);
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);

            return MsQuicStatusCodes.Success;
        }

        private uint HandleEventPeerSendAborted()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            bool shouldComplete = false;
            lock (_sync)
            {
                if (_readState == ReadState.None)
                {
                    shouldComplete = true;
                }
                _readState = ReadState.Aborted;
            }

            if (shouldComplete)
            {
                _receiveResettableCompletionSource.CompleteException(new IOException("Reading has been aborted by the peer."));
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);

            return MsQuicStatusCodes.Success;
        }

        private uint HandleEventPeerSendShutdown()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            bool shouldComplete = false;

            lock (_sync)
            {
                // This event won't occur within the middle of a receive.
                if (NetEventSource.IsEnabled) NetEventSource.Info("Completing resettable event source.");

                if (_readState == ReadState.None)
                {
                    shouldComplete = true;
                }

                _readState = ReadState.ReadsCompleted;
            }

            if (shouldComplete)
            {
                _receiveResettableCompletionSource.Complete(0);
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);

            return MsQuicStatusCodes.Success;
        }

        private uint HandleEventSendComplete(ref StreamEvent evt)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            CleanupSendState();

            // TODO throw if a write was canceled.
            uint errorCode = evt.Data.SendComplete.Canceled;

            bool shouldComplete = false;
            lock (_sync)
            {
                if (_sendState == SendState.None)
                {
                    _sendState = SendState.Finished;
                    shouldComplete = true;
                }
            }

            if (shouldComplete)
            {
                _sendResettableCompletionSource.Complete(MsQuicStatusCodes.Success);
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);

            return MsQuicStatusCodes.Success;
        }

        private void CleanupSendState()
        {
            if (_sendHandle.IsAllocated)
            {
                _sendHandle.Free();
            }
            // Callings dispose twice on a memory handle should be okay
            _bufferArrays[0].Dispose();
        }

        private void SetCallbackHandler()
        {
            _handle = GCHandle.Alloc(this);

            _callback = new StreamCallbackDelegate(NativeCallbackHandler);
            MsQuicApi.Api.SetCallbackHandlerDelegate(
                _ptr,
                _callback,
                GCHandle.ToIntPtr(_handle));
        }

        // TODO prevent overlapping sends or consider supporting it.
        private unsafe ValueTask SendAsync(
           ReadOnlyMemory<byte> buffer,
           QUIC_SEND_FLAG flags)
        {
            if (buffer.IsEmpty)
            {
                if ((flags & QUIC_SEND_FLAG.FIN) == QUIC_SEND_FLAG.FIN)
                {
                    // Start graceful shutdown sequence if passed in the fin flag and there is an empty buffer.
                    MsQuicApi.Api.StreamShutdownDelegate(_ptr, (uint)QUIC_STREAM_SHUTDOWN_FLAG.GRACEFUL, errorCode: 0);
                }
                return default;
            }

            MemoryHandle handle = buffer.Pin();
            _sendQuicBuffers[0].Length = (uint)buffer.Length;
            _sendQuicBuffers[0].Buffer = (byte*)handle.Pointer;

            _bufferArrays[0] = handle;

            _sendHandle = GCHandle.Alloc(_sendQuicBuffers, GCHandleType.Pinned);

            var quicBufferPointer = (QuicBuffer*)Marshal.UnsafeAddrOfPinnedArrayElement(_sendQuicBuffers, 0);

            uint status = MsQuicApi.Api.StreamSendDelegate(
                _ptr,
                quicBufferPointer,
                bufferCount: 1,
                (uint)flags,
                _ptr);

            if (!MsQuicStatusHelper.SuccessfulStatusCode(status))
            {
                CleanupSendState();
                MsQuicStatusException.ThrowIfFailed(status);
            }

            return _sendResettableCompletionSource.GetTypelessValueTask();
        }

        private ValueTask<uint> StartWritesAsync()
        {
            uint status = MsQuicApi.Api.StreamStartDelegate(
              _ptr,
              (uint)QUIC_STREAM_START_FLAG.ASYNC);

            MsQuicStatusException.ThrowIfFailed(status);
            return _sendResettableCompletionSource.GetValueTask();
        }

        private void ReceiveComplete(int bufferLength)
        {
            uint status = MsQuicApi.Api.StreamReceiveCompleteDelegate(_ptr, (ulong)bufferLength);
            MsQuicStatusException.ThrowIfFailed(status);
        }

        // This can fail if the stream isn't started.
        private unsafe long GetStreamId()
        {
            return (long)MsQuicParameterHelpers.GetULongParam(MsQuicApi.Api, _ptr, (uint)QUIC_PARAM_LEVEL.STREAM, (uint)QUIC_PARAM_STREAM.ID);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(MsQuicStream));
            }
        }

        private enum StartState
        {
            None,
            Started,
            Finished
        }

        private enum ReadState
        {
            None,
            IndividualReadComplete,
            ReadsCompleted,
            Aborted
        }

        private enum ShutdownWriteState
        {
            None,
            Canceled,
            Finished
        }

        private enum SendState
        {
            None,
            Aborted,
            Finished
        }
    }
}
