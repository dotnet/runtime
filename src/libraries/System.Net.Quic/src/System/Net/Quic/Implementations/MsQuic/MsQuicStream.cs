// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
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
        // To signal to calls to read that the read has been canceled.
        private readonly CancellationTokenSource _streamClosedTokenSource = new CancellationTokenSource();


        // Pointer to the underlying stream
        private readonly IntPtr _ptr;

        // Handle to this object for native callbacks.
        private GCHandle _handle;

        // Delegate that wraps the static function that will be called when receiving an event.
        private StreamCallbackDelegate _callback;

        // Backing for StreamId
        private long _streamId = -1;

        private volatile bool _disposed = false;

        // Resettable completions to be used for multiple calls to send, receive, start, and shutdown.
        internal ResettableCompletionSource<uint> _sendResettableCompletionSource;

        // Buffers to hold during a call to send.
        private MemoryHandle[] _bufferArrays;

        // Handle to hold when sending.
        private GCHandle _sendHandle;

        // Used to check if StartAsync has been called.
        private StartState _started;

        private string _logBaseString;

        private MsQuicApi _api;

        // Used by the class to indicate that the stream is m_Readable.
        private bool _canRead;

        // Used by the class to indicate that the stream is writable.
        private bool _canWrite;

        // Pipe for reads
        private Pipe _requestPipe;

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
                _logBaseString = "[Stream Server]";
            }
            else
            {
                _started = StartState.None;

                _canWrite = true;
                _canRead = !flags.HasFlag(QUIC_STREAM_OPEN_FLAG.UNIDIRECTIONAL);
                _logBaseString = "[Stream Client]";
            }
            var inputOptions = new PipeOptions(pool: null, PipeScheduler.ThreadPool, PipeScheduler.Inline, pauseWriterThreshold: 1, resumeWriterThreshold: 1, useSynchronizationContext: false);

            _requestPipe = new Pipe(inputOptions);
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

        internal override int Read(Span<byte> buffer)
        {
            throw new NotImplementedException();
        }

        internal override void Write(ReadOnlySpan<byte> buffer)
        {
            throw new NotImplementedException();
        }

        internal override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_started == StartState.None)
            {
                _started = StartState.Started;
                await StartAsync();
            }

            await SendAsync(new ReadOnlySequence<byte>(buffer), QUIC_SEND_FLAG.NONE);
        }

        internal override async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken)
        {
            while (true)
            {
                var result = await _requestPipe.Reader.ReadAsync(cancellationToken);

                if (result.IsCanceled)
                {
                    throw new OperationCanceledException("The read was canceled");
                }

                ReadOnlySequence<byte> buffer = result.Buffer;
                var length = buffer.Length;

                var consumed = buffer.End;
                try
                {
                    if (length != 0)
                    {
                        var actual = (int)Math.Min(length, destination.Length);

                        var slice = actual == length ? buffer : buffer.Slice(0, actual);
                        consumed = slice.End;
                        slice.CopyTo(destination.Span);

                        return actual;
                    }

                    if (result.IsCompleted)
                    {
                        return 0;
                    }
                }
                finally
                {
                    _requestPipe.Reader.AdvanceTo(consumed);
                }
            }
        }

        private void EnableReceive()
        {
            var status = _api.StreamReceiveSetEnabledDelegate(_ptr, true);
        }

        private void DisableReceive()
        {
            var status = _api.StreamReceiveSetEnabledDelegate(_ptr, false);
        }

        internal override void ShutdownRead()
        {
            _requestPipe.Reader.Complete();
        }

        internal override void ShutdownWrite()
        {
            _api.StreamShutdownDelegate(_ptr, (uint)QUIC_STREAM_SHUTDOWN_FLAG.GRACEFUL, 0);
            // TODO do anything to stop writes?
        }

        internal override void Flush()
        {
            throw new NotImplementedException();
        }

        internal override async Task FlushAsync(CancellationToken cancellationToken)
        {
            await SendAsync(new ReadOnlySequence<byte>(Memory<byte>.Empty), QUIC_SEND_FLAG.NONE);
        }

        public override async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            if (_ptr != IntPtr.Zero)
            {
                // TODO can shutdown hang?
                await ShutdownAsync(QUIC_STREAM_SHUTDOWN_FLAG.GRACEFUL, 0).ConfigureAwait(false);
                _api.StreamCloseDelegate?.Invoke(_ptr);
            }

            _handle.Free();
            _api = null;

            _disposed = true;
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

            if (_ptr != IntPtr.Zero)
            {
                ShutdownAsync(QUIC_STREAM_SHUTDOWN_FLAG.GRACEFUL, 0).GetAwaiter().GetResult();
                _api.StreamCloseDelegate?.Invoke(_ptr);
            }

            _handle.Free();
            _api = null;

            _disposed = true;
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
                    case QUIC_STREAM_EVENT.START_COMPLETE:
                        status = HandleStartComplete();
                        break;
                    case QUIC_STREAM_EVENT.RECV:
                        {
                            HandleEventRecv(ref evt);
                        }
                        break;
                    case QUIC_STREAM_EVENT.SEND_COMPLETE:
                        {
                            status = HandleEventSendComplete(ref evt);
                        }
                        break;
                    case QUIC_STREAM_EVENT.PEER_SEND_CLOSE:
                        {
                            status = HandleEventPeerSendClose();
                        }
                        break;
                    // TODO figure out difference between SEND_ABORT and RECEIVE_ABORT
                    case QUIC_STREAM_EVENT.PEER_SEND_ABORT:
                        {
                            // TODO why is this firing.
                            _streamClosedTokenSource.Cancel();
                            status = HandleEventPeerSendAbort();
                        }
                        break;
                    case QUIC_STREAM_EVENT.PEER_RECV_ABORT:
                        {
                            _streamClosedTokenSource.Cancel();
                            status = HandleEventPeerRecvAbort();
                        }
                        break;
                    case QUIC_STREAM_EVENT.SEND_SHUTDOWN_COMPLETE:
                        {
                            status = HandleEventSendShutdownComplete(ref evt);
                        }
                        break;
                    case QUIC_STREAM_EVENT.SHUTDOWN_COMPLETE:
                        {
                            // TODO need to figure out if we should dispose here or not?
                            // I think not, but we want to make sure close is called.
                            status = HandleEventShutdownComplete();
                        }
                        break;
                    default:
                        Log($"Unexpected event {((QUIC_STREAM_EVENT)evt.Type).ToString()}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                return MsQuicConstants.InternalError;
            }

            return status;
        }

        private void HandleEventRecv(ref MsQuicNativeMethods.StreamEvent evt)
        {
            static unsafe void CopyToBuffer(Span<byte> buffer, StreamEvent evt)
            {
                var length = (int)evt.Data.Recv.Buffers[0].Length;
                new Span<byte>(evt.Data.Recv.Buffers[0].Buffer, length).CopyTo(buffer);
            }

            Log($"Received data {evt.Data.Recv.TotalBufferLength}");

            var input = _requestPipe.Writer;
            var length = (int)evt.Data.Recv.TotalBufferLength;
            var result = input.GetSpan(length);
            CopyToBuffer(result, evt);

            input.Advance(length);

            var flushTask = input.FlushAsync();

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
            Log("Peer recv abort");
            return MsQuicConstants.Success;
        }

        private void Log(string log)
        {
            Console.WriteLine($"{_logBaseString} {StreamId} {log}");
        }

        private uint HandleEventPeerSendAbort()
        {
            Log("Peer send abort");
            return MsQuicConstants.Success;
        }

        private uint HandleStartComplete()
        {
            Log("Start complete");
            _started = StartState.Finished;
            _sendResettableCompletionSource.Complete(MsQuicConstants.Success);
            return MsQuicConstants.Success;
        }

        private uint HandleEventSendShutdownComplete(ref MsQuicNativeMethods.StreamEvent evt)
        {
            Log("send shutdown complete");
            return MsQuicConstants.Success;
        }

        private uint HandleEventShutdownComplete()
        {
            Log("Shutdown complete");
            // TODO use another cts here.
            _sendResettableCompletionSource.Complete(MsQuicConstants.Success);
            return MsQuicConstants.Success;
        }

        private uint HandleEventPeerSendClose()
        {
            Log("Peer send close");
            _requestPipe.Writer.Complete();
            return MsQuicConstants.Success;
        }

        public uint HandleEventSendComplete(ref MsQuicNativeMethods.StreamEvent evt)
        {
            Log("Send complete");
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

        public void SetCallbackHandler()
        {
            _handle = GCHandle.Alloc(this);

            _callback = new StreamCallbackDelegate(NativeCallbackHandler);
            _api.SetCallbackHandlerDelegate(
                _ptr,
                _callback,
                GCHandle.ToIntPtr(_handle));
        }

        public unsafe ValueTask<uint> SendAsync(
           ReadOnlySequence<byte> buffers,
           QUIC_SEND_FLAG flags)
        {
            var bufferCount = 0;
            foreach (var memory in buffers)
            {
                bufferCount++;
            }

            var quicBufferArray = new QuicBuffer[bufferCount];
            _bufferArrays = new MemoryHandle[bufferCount];

            var i = 0;
            foreach (var memory in buffers)
            {
                var handle = memory.Pin();
                _bufferArrays[i] = handle;
                quicBufferArray[i].Length = (uint)memory.Length;
                quicBufferArray[i].Buffer = (byte*)handle.Pointer;
                i++;
            }

            _sendHandle = GCHandle.Alloc(quicBufferArray, GCHandleType.Pinned);

            var quicBufferPointer = (QuicBuffer*)Marshal.UnsafeAddrOfPinnedArrayElement(quicBufferArray, 0);

            var status = _api.StreamSendDelegate(
                _ptr,
                quicBufferPointer,
                (uint)bufferCount,
                (uint)flags,
                _ptr);

            MsQuicStatusException.ThrowIfFailed(status);

            return _sendResettableCompletionSource.GetValueTask();
        }

        public ValueTask<uint> StartAsync()
        {
            uint status = _api.StreamStartDelegate(
              _ptr,
              (uint)QUIC_STREAM_START_FLAG.ASYNC);

            MsQuicStatusException.ThrowIfFailed(status);
            return _sendResettableCompletionSource.GetValueTask();
        }

        public void ReceiveComplete(int bufferLength)
        {
            uint status = _api.StreamReceiveCompleteDelegate(_ptr, (ulong)bufferLength);
            MsQuicStatusException.ThrowIfFailed(status);
        }

        public Task<uint> ShutdownAsync(
            QUIC_STREAM_SHUTDOWN_FLAG flags,
            ushort errorCode)
        {
            uint status = _api.StreamShutdownDelegate(
                _ptr,
                (uint)flags,
                errorCode);
            MsQuicStatusException.ThrowIfFailed(status);
            return _sendResettableCompletionSource.GetValueTask().AsTask();
        }

        public void Close()
        {
            uint status = (uint)_api.StreamCloseDelegate?.Invoke(_ptr);
            MsQuicStatusException.ThrowIfFailed(status);
        }


        // This can fail if the stream isn't started.
        // TODO throw if it isn't started
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
                : base()
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
