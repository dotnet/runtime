using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using static System.Net.Quic.Implementations.MsQuic.Internal.MsQuicNativeMethods;

namespace System.Net.Quic.Implementations.MsQuic
{
    internal sealed class MsQuicStream : QuicStreamProvider
    {
        private readonly CancellationTokenSource _streamClosedTokenSource = new CancellationTokenSource();
        private bool _disposed;
        private IntPtr _nativeObjPtr;
        private GCHandle _handle;
        private StreamCallbackDelegate _delegate;
        private long _streamId = -1;

        internal ResettableCompletionSource<uint> _sendResettableCompletionSource;
        internal ResettableCompletionSource<int> _receiveResettableCompletionSource;
        private MemoryHandle[] _bufferArrays;
        private GCHandle _sendBuffer;

        private Memory<byte>_transferBuffer;
        private int _transferBufferLength;
        private object _sync = new object();
        private bool _started;
        private string _logBaseString;

        // Outbound
        public MsQuicStream(MsQuicApi api, MsQuicConnection connection, QUIC_STREAM_OPEN_FLAG flags, IntPtr nativeObjPtr)
        {
            Debug.Assert(connection != null);

            Api = api;
            _nativeObjPtr = nativeObjPtr;
            _started = false;
            _sendResettableCompletionSource = new UIntResettableCompletionSource(this);
            _receiveResettableCompletionSource = new IntResettableCompletionSource(this);
            SetCallbackHandler();
            _logBaseString = "[Stream Client]";
        }

        public MsQuicStream(MsQuicApi api, MsQuicConnection connection, IntPtr nativeObjPtr)
        {
            Debug.Assert(connection != null);

            Api = api;
            _nativeObjPtr = nativeObjPtr;
            _started = true;
            _sendResettableCompletionSource = new UIntResettableCompletionSource(this);
            _receiveResettableCompletionSource = new IntResettableCompletionSource(this);
            SetCallbackHandler();

            _logBaseString = "[Stream Server]";
        }

        public MemoryPool<byte> MemoryPool { get; }

        public bool IsUnidirectional { get; }

        public MsQuicApi Api { get; set; }

        internal override bool CanRead => throw new NotImplementedException();

        internal override bool CanWrite => throw new NotImplementedException();

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
            if (!_started)
            {
                await StartAsync();
                _started = true;
            }

            await SendAsync(new ReadOnlySequence<byte>(buffer), QUIC_SEND_FLAG.NONE);
        }

        internal uint HandleEvent(ref MsQuicNativeMethods.StreamEvent evt)
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

        private uint HandleEventPeerRecvAbort()
        {
            Log("Peer recv abort");
            return MsQuicConstants.Success;
        }

        private void Log(string log)
        {
            Console.WriteLine($"{_logBaseString} {GetStreamId()} {log}");
        }

        private uint HandleEventPeerSendAbort()
        {
            Log("Peer send abort");
            return MsQuicConstants.Success;
        }

        private uint HandleStartComplete()
        {
            Log("Start complete");
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
            _sendResettableCompletionSource.Complete(MsQuicConstants.Success);
            return MsQuicConstants.Success;
        }

        private uint HandleEventPeerSendClose()
        {
            // TODO make stream return -1?
            //Input.Complete();
            Log("Peer send close");
            return MsQuicConstants.Success;
        }

        public uint HandleEventSendComplete(ref MsQuicNativeMethods.StreamEvent evt)
        {
            // send canceled?
            Log("Send complete");
            _sendBuffer.Free();
            foreach (MemoryHandle gchBufferArray in _bufferArrays)
            {
                gchBufferArray.Dispose();
            }
            _sendResettableCompletionSource.Complete(evt.Data.PeerRecvAbort.ErrorCode);
            return MsQuicConstants.Success;
        }

        internal override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                if (_transferBuffer.Length == 0)
                {
                    _transferBuffer = buffer;
                    return _receiveResettableCompletionSource.GetValueTask();
                }

                Memory<byte> readableBuffer = _transferBuffer.Slice(0, _transferBufferLength);

                int actual = Math.Min(readableBuffer.Length, buffer.Length);
                readableBuffer = readableBuffer.Slice(0, actual);
                readableBuffer.CopyTo(buffer);

                ArrayPool<byte>.Shared.Return(_transferBuffer.ToArray());

                //EnableReceive();
                //ReceiveComplete(actual);
                return new ValueTask<int>(actual);
            }
        }

        internal unsafe void HandleEventRecv(ref MsQuicNativeMethods.StreamEvent evt)
        {
            lock (_sync)
            {
                Log($"Received data {evt.Data.Recv.TotalBufferLength}");
                int length = (int)evt.Data.Recv.TotalBufferLength;
                Span<byte> buffer = new Span<byte>(evt.Data.Recv.Buffers[0].Buffer, length);

                if (_transferBuffer.Length == 0)
                {
                    // TODO figure out if I need to disable the send here.
                    Memory<byte> rentedBuffer = ArrayPool<byte>.Shared.Rent(length);
                    _transferBufferLength = length;
                    buffer.CopyTo(rentedBuffer.Span);
                    _transferBuffer = rentedBuffer;
                    //DisableReceive();
                    return;
                }

                Memory<byte> destinationBuffer = _transferBuffer;

                int actual = Math.Min(destinationBuffer.Length, buffer.Length);
                buffer = buffer.Slice(0, actual);
                buffer.CopyTo(destinationBuffer.Span);

                //EnableReceive();
                //ReceiveComplete(actual);

                _receiveResettableCompletionSource.Complete(actual);
            }
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

        public void SetCallbackHandler()
        {
            _handle = GCHandle.Alloc(this);

            _delegate = new StreamCallbackDelegate(NativeCallbackHandler);
            Api.SetCallbackHandlerDelegate(
                _nativeObjPtr,
                _delegate,
                GCHandle.ToIntPtr(_handle));
        }

        public unsafe ValueTask<uint> SendAsync(
           ReadOnlySequence<byte> buffers,
           QUIC_SEND_FLAG flags)
        {
            Log("Send start");
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

            _sendBuffer = GCHandle.Alloc(quicBufferArray, GCHandleType.Pinned);

            var quicBufferPointer = (QuicBuffer*)Marshal.UnsafeAddrOfPinnedArrayElement(quicBufferArray, 0);

            var status = Api.StreamSendDelegate(
                _nativeObjPtr,
                quicBufferPointer,
                (uint)bufferCount,
                (uint)flags,
                _nativeObjPtr);

            MsQuicStatusException.ThrowIfFailed(status);

            return _sendResettableCompletionSource.GetValueTask();
        }

        public ValueTask<uint> StartAsync()
        {
            uint status = Api.StreamStartDelegate(
              _nativeObjPtr,
              (uint)QUIC_STREAM_START_FLAG.ASYNC);

            MsQuicStatusException.ThrowIfFailed(status);
            return _sendResettableCompletionSource.GetValueTask();
        }

        public void ReceiveComplete(int bufferLength)
        {
            uint status = (uint)Api.StreamReceiveComplete(_nativeObjPtr, (ulong)bufferLength);
            MsQuicStatusException.ThrowIfFailed(status);
        }

        public Task<uint> ShutdownAsync(
            QUIC_STREAM_SHUTDOWN_FLAG flags,
            ushort errorCode)
        {
            uint status = (uint)Api.StreamShutdownDelegate(
                _nativeObjPtr,
                (uint)flags,
                errorCode);
            MsQuicStatusException.ThrowIfFailed(status);
            return _sendResettableCompletionSource.GetValueTask().AsTask();
        }

        public void Close()
        {
            uint status = (uint)Api.StreamCloseDelegate?.Invoke(_nativeObjPtr);
            MsQuicStatusException.ThrowIfFailed(status);
        }

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public unsafe void EnableReceive()
        {
            bool val = true;
            var buffer = new QuicBuffer()
            {
                Length = sizeof(bool),
                Buffer = (byte*)&val
            };
            SetParam(QUIC_PARAM_STREAM.RECEIVE_ENABLED, buffer);
        }

        public unsafe void DisableReceive()
        {
            bool val = false;
            var buffer = new QuicBuffer()
            {
                Length = sizeof(bool),
                Buffer = (byte*)&val
            };
            SetParam(QUIC_PARAM_STREAM.RECEIVE_ENABLED, buffer);
        }

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
            MsQuicStatusException.ThrowIfFailed(Api.UnsafeGetParam(
                _nativeObjPtr,
                (uint)QUIC_PARAM_LEVEL.STREAM,
                (uint)param,
                ref buf));
        }

        private void SetParam(
              QUIC_PARAM_STREAM param,
              QuicBuffer buf)
        {
            MsQuicStatusException.ThrowIfFailed(Api.UnsafeSetParam(
                _nativeObjPtr,
                (uint)QUIC_PARAM_LEVEL.STREAM,
                (uint)param,
                buf));
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

            if (_nativeObjPtr != IntPtr.Zero)
            {
                ShutdownAsync(QUIC_STREAM_SHUTDOWN_FLAG.GRACEFUL, 0).GetAwaiter().GetResult();
                Api.StreamCloseDelegate?.Invoke(_nativeObjPtr);
            }

            _handle.Free();
            _nativeObjPtr = IntPtr.Zero;
            Api = null;

            _disposed = true;
        }

        internal override void ShutdownRead()
        {
            throw new NotImplementedException();
        }

        internal override void ShutdownWrite()
        {
            throw new NotImplementedException();
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

            if (_nativeObjPtr != IntPtr.Zero)
            {
                // TODO can shutdown hang?
                await ShutdownAsync(QUIC_STREAM_SHUTDOWN_FLAG.GRACEFUL, 0).ConfigureAwait(false);
                Api.StreamCloseDelegate?.Invoke(_nativeObjPtr);
            }

            _handle.Free();
            _nativeObjPtr = IntPtr.Zero;
            Api = null;

            _disposed = true;
        }
    }
}
