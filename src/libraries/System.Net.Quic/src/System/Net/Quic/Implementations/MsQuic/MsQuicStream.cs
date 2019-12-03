// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Quic.Implementations.MsQuic.Internal.MsQuicNativeMethods;

namespace System.Net.Quic.Implementations.MsQuic
{
    internal sealed class MsQuicStream : QuicStreamProvider
    {
        // Functions to invoke in MsQuic
        private MsQuicApi _api;

        // Pointer to the underlying stream
        private readonly IntPtr _ptr;

        // Handle to this object for native callbacks.
        private GCHandle _handle;

        // Delegate that wraps the static function that will be called when receiving an event.
        private StreamCallbackDelegate _callback;

        // Backing for StreamId
        private long _streamId = -1;

        // Resettable completions to be used for multiple calls to send, start, and shutdown.
        internal ResettableCompletionSource<uint> _sendResettableCompletionSource;

        // Buffers to hold during a call to send.
        private MemoryHandle[] _bufferArrays;

        // Handle to hold when sending.
        private GCHandle _sendHandle;

        // Used to check if StartAsync has been called.
        private StartState _started;

        // Used by the class to indicate that the stream is m_Readable.
        private bool _canRead;

        // Used by the class to indicate that the stream is writable.
        private bool _canWrite;

        // Pipe for reading.
        private Pipe _readingPipe;

        private volatile bool _disposed = false;

        // Creates a new MsQuicStream
        internal MsQuicStream(MsQuicApi api, MsQuicConnection connection, QUIC_STREAM_OPEN_FLAG flags, IntPtr nativeObjPtr, bool inbound)
        {
            Debug.Assert(connection != null);

            _api = api;
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

            // Options to effectively have a single buffer pipe
            // Effectively this is a synchronization mechanism between calls to ReadAsync and Recv callbacks
            // However, having a pipe here is nice for calling Complete,
            // handing buffering, and handling synchronization.
            var inputOptions = new PipeOptions(pool: null, PipeScheduler.ThreadPool, PipeScheduler.Inline, pauseWriterThreshold: 1, resumeWriterThreshold: 1, useSynchronizationContext: false);

            _readingPipe = new Pipe(inputOptions);
            _sendResettableCompletionSource = new UIntResettableCompletionSource(this);
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

        internal override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            if (_started == StartState.None)
            {
                _started = StartState.Started;
                await StartAsync();
            }

            await SendAsync(new ReadOnlySequence<byte>(buffer), QUIC_SEND_FLAG.NONE);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        internal override async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            ReadResult result = await _readingPipe.Reader.ReadAsync(cancellationToken);

            ReadOnlySequence<byte> buffer = result.Buffer;
            long length = buffer.Length;

            SequencePosition consumed = buffer.End;
            try
            {
                if (length != 0)
                {
                    int actual = (int)Math.Min(length, destination.Length);

                    ReadOnlySequence<byte> slice = actual == length ? buffer : buffer.Slice(0, actual);
                    consumed = slice.End;
                    slice.CopyTo(destination.Span);

                    if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
                    return actual;
                }

                Debug.Assert(result.IsCompleted);

                if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
                return 0;
            }
            finally
            {
                _readingPipe.Reader.AdvanceTo(consumed);
            }
        }

        internal override void ShutdownRead()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            // TODO this will cause ReadAsync to start throwing rather than returning 0.
            // Do we want this behavior?
            // This is abortive behavior.
            _readingPipe.Reader.Complete();

            // TODO do we need to check if we have already gotten PEER_SEND_SHUTDOWN.
            _api._streamShutdownDelegate(_ptr, (uint)QUIC_STREAM_SHUTDOWN_FLAG.ABORT_RECV, errorCode: 0);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        internal override void ShutdownWrite()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            // TODO do anything to stop writes?
            _api._streamShutdownDelegate(_ptr, (uint)QUIC_STREAM_SHUTDOWN_FLAG.GRACEFUL, errorCode: 0);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        // TODO consider removing sync-over-async with blocking calls.
        internal override int Read(Span<byte> buffer)
        {
            return ReadAsync(buffer.ToArray()).GetAwaiter().GetResult();
        }

        internal override void Write(ReadOnlySpan<byte> buffer)
        {
            WriteAsync(buffer.ToArray()).GetAwaiter().GetResult();
        }

        // MsQuic doesn't support explicit flushing
        internal override void Flush()
        {
        }

        // MsQuic doesn't support explicit flushing
        internal override Task FlushAsync(CancellationToken cancellationToken = default)
        {
            return default;
        }

        public override ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return default;
            }

            if (_ptr != IntPtr.Zero)
            {
                _api._streamCloseDelegate?.Invoke(_ptr);
            }

            _handle.Free();
            _api = null;

            _disposed = true;

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

        // Synchronous shutdown current does a graceful shutdown, which must go async
        // Close can be done synchronously, but there is not guarantee that all data will be sent to the client
        // We probably need to reconsider how to handle dispose/shutdown cases.
        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (_ptr != IntPtr.Zero)
            {
                // If we make Dispose not do a graceful shutdown, we can remove sync over async here
                // as abortive shutdown isn't async.
                _api._streamCloseDelegate?.Invoke(_ptr);
            }

            _handle.Free();
            _api = null;

            _disposed = true;
        }

        private void EnableReceive()
        {
            uint status = _api._streamReceiveSetEnabledDelegate(_ptr, enabled: true);
        }

        private void DisableReceive()
        {
            uint status = _api._streamReceiveSetEnabledDelegate(_ptr, enabled: false);
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
            uint status = MsQuicConstants.Success;

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
                            HandleEventRecv(ref evt);
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
                return MsQuicConstants.s_internalError;
            }

            return status;
        }

        private void HandleEventRecv(ref MsQuicNativeMethods.StreamEvent evt)
        {
            static unsafe void CopyToBuffer(Span<byte> buffer, StreamEvent evt)
            {
                Span<byte> slicedBuffer = buffer;
                for (int i = 0; i < evt.Data.Recv.BufferCount; i++)
                {
                    QuicBuffer nativeBuffer = evt.Data.Recv.Buffers[i];
                    int length = (int)nativeBuffer.Length;
                    new Span<byte>(nativeBuffer.Buffer, length).CopyTo(slicedBuffer);
                    slicedBuffer = slicedBuffer.Slice(length);
                }
            }

            // Need to think hard about backpressure here.
            // Can this pipe grow infinitely if the consumer isn't reading large chunks?
            // TODO add a test which does a bunch of large writes and small reads.
            PipeWriter input = _readingPipe.Writer;
            int length = (int)evt.Data.Recv.TotalBufferLength;
            Span<byte> result = input.GetSpan(length);
            CopyToBuffer(result, evt);

            input.Advance(length);

            ValueTask<FlushResult> flushTask = input.FlushAsync();

            if (!flushTask.IsCompletedSuccessfully)
            {
                DisableReceive();
                ReceiveComplete(0);
                _ = AwaitFlush(flushTask);
                return;
            }

            async Task AwaitFlush(ValueTask<FlushResult> ft)
            {
                await ft;
                EnableReceive();
                ReceiveComplete(length);
            }
        }

        private uint HandleEventPeerRecvAbort()
        {
            return MsQuicConstants.Success;
        }


        private uint HandleStartComplete()
        {
            _started = StartState.Finished;
            _sendResettableCompletionSource.Complete(MsQuicConstants.Success);
            return MsQuicConstants.Success;
        }

        private uint HandleEventSendShutdownComplete(ref MsQuicNativeMethods.StreamEvent evt)
        {
            _sendResettableCompletionSource.Complete(MsQuicConstants.Success);
            return MsQuicConstants.Success;
        }

        private uint HandleEventShutdownComplete()
        {
            // TODO use another cts here.
            _sendResettableCompletionSource.Complete(MsQuicConstants.Success);
            return MsQuicConstants.Success;
        }

        private uint HandleEventPeerSendAborted()
        {
            _readingPipe.Writer.Complete(new IOException("The stream has been aborted"));
            return MsQuicConstants.Success;
        }

        private uint HandleEventPeerSendShutdown()
        {
            _readingPipe.Writer.Complete();
            return MsQuicConstants.Success;
        }

        private uint HandleEventSendComplete(ref StreamEvent evt)
        {
            _sendHandle.Free();
            foreach (MemoryHandle gchBufferArray in _bufferArrays)
            {
                gchBufferArray.Dispose();
            }
            // TODO throw if a write failed?
            uint errorCode = evt.Data.SendComplete.Canceled;
            _sendResettableCompletionSource.Complete(MsQuicConstants.Success);

            return MsQuicConstants.Success;
        }

        private void SetCallbackHandler()
        {
            _handle = GCHandle.Alloc(this);

            _callback = new StreamCallbackDelegate(NativeCallbackHandler);
            _api._setCallbackHandlerDelegate(
                _ptr,
                _callback,
                GCHandle.ToIntPtr(_handle));
        }

        // TODO this probably can be ReadOnlyMemory
        public unsafe ValueTask<uint> SendAsync(
           ReadOnlySequence<byte> buffers,
           QUIC_SEND_FLAG flags)
        {
            int bufferCount = 0;
            foreach (ReadOnlyMemory<byte> memory in buffers)
            {
                bufferCount++;
            }

            var quicBufferArray = new QuicBuffer[bufferCount];
            _bufferArrays = new MemoryHandle[bufferCount];

            int i = 0;
            foreach (ReadOnlyMemory<byte> memory in buffers)
            {
                MemoryHandle handle = memory.Pin();
                _bufferArrays[i] = handle;
                quicBufferArray[i].Length = (uint)memory.Length;
                quicBufferArray[i].Buffer = (byte*)handle.Pointer;
                i++;
            }

            _sendHandle = GCHandle.Alloc(quicBufferArray, GCHandleType.Pinned);

            var quicBufferPointer = (QuicBuffer*)Marshal.UnsafeAddrOfPinnedArrayElement(quicBufferArray, 0);

            uint status = _api._streamSendDelegate(
                _ptr,
                quicBufferPointer,
                (uint)bufferCount,
                (uint)flags,
                _ptr);

            MsQuicStatusException.ThrowIfFailed(status);

            return _sendResettableCompletionSource.GetValueTask();
        }

        // StartAsync can optionally be called synchornously.
        // StartAsync doesn't do networking calls, however it needs to wait for all work items in
        // the connection queue to be processed. It's generally better to do start asynchronously
        // as it doesn't block a thread.
        private ValueTask<uint> StartAsync()
        {
            uint status = _api._streamStartDelegate(
              _ptr,
              (uint)QUIC_STREAM_START_FLAG.ASYNC);

            MsQuicStatusException.ThrowIfFailed(status);
            return _sendResettableCompletionSource.GetValueTask();
        }

        private void ReceiveComplete(int bufferLength)
        {
            uint status = _api._streamReceiveCompleteDelegate(_ptr, (ulong)bufferLength);
            MsQuicStatusException.ThrowIfFailed(status);
        }

        private Task<uint> ShutdownAsync(
            QUIC_STREAM_SHUTDOWN_FLAG flags,
            ushort errorCode)
        {
            uint status = _api._streamShutdownDelegate(
                _ptr,
                (uint)flags,
                errorCode);
            MsQuicStatusException.ThrowIfFailed(status);
            return _sendResettableCompletionSource.GetValueTask().AsTask();
        }

        // This can fail if the stream isn't started.
        private unsafe long GetStreamId()
        {
            byte* ptr = stackalloc byte[sizeof(long)];
            var buffer = new QuicBuffer
            {
                Length = sizeof(long),
                Buffer = ptr
            };
            GetParam(QUIC_PARAM_STREAM.ID, ref buffer);
            return *(long*)ptr;
        }

        private void GetParam(
            QUIC_PARAM_STREAM param,
            ref QuicBuffer buf)
        {
            MsQuicStatusException.ThrowIfFailed(_api.UnsafeGetParam(
                _ptr,
                (uint)QUIC_PARAM_LEVEL.STREAM,
                (uint)param,
                ref buf));
        }

        private class UIntResettableCompletionSource : ResettableCompletionSource<uint>
        {
            private readonly MsQuicStream _stream;

            internal UIntResettableCompletionSource(MsQuicStream stream)
            {
                _stream = stream;
            }

            public override uint GetResult(short token)
            {
                bool isValid = token == _valueTaskSource.Version;
                try
                {
                    return _valueTaskSource.GetResult(token);
                }
                finally
                {
                    if (isValid)
                    {
                        _valueTaskSource.Reset();
                        _stream._sendResettableCompletionSource = this;
                    }
                }
            }
        }

        private enum StartState
        {
            None,
            Started,
            Finished
        }
    }
}
