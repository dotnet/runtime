// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using AsyncResult = System.Threading.UnixHandleAsyncContext.AsyncResult;
using OnCompletedResult = System.Threading.UnixHandleAsyncContext.OnCompletedResult;
using SyncResult = System.Threading.UnixHandleAsyncContext.SyncResult;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafePipeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private const int DefaultInvalidHandle = -1;

        private NullableBool _isBlocking;
        private UnixHandleAsyncContext? _asyncContext;
        private ReadOperation? _cachedReadOp;
        private WriteOperation? _cachedWriteOp;

        private ReadOperation RentReadOperation()
            => Interlocked.Exchange(ref _cachedReadOp, null) ?? new ReadOperation(this);

        private WriteOperation RentWriteOperation()
            => Interlocked.Exchange(ref _cachedWriteOp, null) ?? new WriteOperation(this);

        private void ReturnReadOperation(ReadOperation op)
        {
            op.Reset();
            Volatile.Write(ref _cachedReadOp, op);
        }

        private void ReturnWriteOperation(WriteOperation op)
        {
            op.Reset();
            Volatile.Write(ref _cachedWriteOp, op);
        }

        private bool IsBlocking
        {
            get
            {
                NullableBool isBlocking = _isBlocking;
                if (isBlocking == NullableBool.Undefined)
                {
                    if (Interop.Sys.Fcntl.GetIsNonBlocking(this, out bool nonBlocking) != 0)
                    {
                        throw Interop.GetExceptionForIoErrno(Interop.Sys.GetLastErrorInfo());
                    }

                    _isBlocking = isBlocking = nonBlocking ? NullableBool.False : NullableBool.True;
                }

                return isBlocking == NullableBool.True;
            }
        }

        private void SetHandleNonBlocking()
        {
            if (_isBlocking != NullableBool.False)
            {
                if (Interop.Sys.Fcntl.SetIsNonBlocking(this, 1) != 0)
                {
                    throw Interop.GetExceptionForIoErrno(Interop.Sys.GetLastErrorInfo());
                }
                _isBlocking = NullableBool.False;
            }
        }

        private UnixHandleAsyncContext AsyncContext
        {
            get
            {
                if (_asyncContext == null)
                {
                    SetHandleNonBlocking();
                    Interlocked.CompareExchange(ref _asyncContext, CreateAsyncContext(this), null);
                }
                return _asyncContext!;
            }
        }

        internal SafePipeHandle(Socket namedPipeSocket) : base(ownsHandle: true)
        {
            Debug.Assert(namedPipeSocket != null);

            _isBlocking = namedPipeSocket.Blocking ? NullableBool.True : NullableBool.False;

            // Transfer ownership of the file descriptor from the Socket to this SafeHandle.
            SafeHandle socketHandle = namedPipeSocket.SafeHandle;
            base.SetHandle(socketHandle.DangerousGetHandle());
            socketHandle.SetHandleAsInvalid();
            namedPipeSocket.Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _asyncContext?.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override bool ReleaseHandle()
        {
            return (long)handle >= 0 && Interop.Sys.Close(handle) == 0;
        }

        public override bool IsInvalid
        {
            get { return (long)handle < 0; }
        }

        internal new void SetHandle(IntPtr descriptor)
        {
            base.SetHandle(descriptor);
        }

        // Named pipes on Unix are implemented using Unix domain sockets.
        // Returns 0 for non-socket handles (getsockopt returns ENOTSOCK).
        internal unsafe int GetSocketBufferSize(SocketOptionName optionName)
        {
            int value;
            int optLen = sizeof(int);
            Interop.Error error = Interop.Sys.GetSockOpt(this, SocketOptionLevel.Socket, optionName, (byte*)&value, &optLen);
            return error == Interop.Error.SUCCESS ? value : 0;
        }

        internal unsafe (int BytesRead, Interop.ErrorInfo ErrorInfo) Read(Span<byte> buffer)
        {
            int sequenceNumber = 0;
            bool isBlocking = IsBlocking;

            bool doSync = isBlocking || AsyncContext.IsReadReady(out sequenceNumber);
            if (doSync)
            {
                if (TryCompleteRead(buffer, out var result, out bool pending))
                {
                    return result;
                }
                if (isBlocking)
                {
                    // The handle changed to non-blocking due to a concurrent operation.
                    Debug.Assert(pending);
                    if (AsyncContext.IsReadReady(out sequenceNumber) && TryCompleteRead(buffer, out result, out _))
                    {
                        return result;
                    }
                }
            }

            ReadOperation op = RentReadOperation();
            fixed (byte* bufPtr = &MemoryMarshal.GetReference(buffer))
            {
                op.Init(bufPtr, buffer.Length);

                SyncResult result = AsyncContext.ReadSync(op, sequenceNumber, timeout: -1);

                if (result == SyncResult.Completed)
                {
                    var readResult = op.Result;

                    ReturnReadOperation(op);

                    return readResult;
                }

                return (-1, new Interop.ErrorInfo(Interop.Error.ECANCELED));
            }
        }

        internal ValueTask<(int BytesRead, Interop.ErrorInfo ErrorInfo)> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken)
        {
            if (AsyncContext.IsReadReady(out int sequenceNumber) &&
                TryCompleteRead(destination.Span, out var readResult, out _))
            {
                return new ValueTask<(int, Interop.ErrorInfo)>(readResult);
            }

            ReadOperation op = RentReadOperation();
            op.Init(destination, cancellationToken);

            AsyncResult result = AsyncContext.ReadAsync(op, sequenceNumber, cancellationToken);

            if (result == AsyncResult.Pending)
            {
                return new ValueTask<(int, Interop.ErrorInfo)>(op, op.Version);
            }
            else if (result == AsyncResult.Completed)
            {
                readResult = op.Result;
                ReturnReadOperation(op);
                return new ValueTask<(int, Interop.ErrorInfo)>(readResult);
            }

            return new ValueTask<(int, Interop.ErrorInfo)>((-1, new Interop.ErrorInfo(Interop.Error.ECANCELED)));
        }

        internal unsafe Interop.ErrorInfo Write(ReadOnlySpan<byte> buffer)
        {
            int sequenceNumber = 0;
            bool isBlocking = IsBlocking;

            bool doSync = isBlocking || AsyncContext.IsWriteReady(out sequenceNumber);
            while (doSync)
            {
                if (TryCompleteWrite(buffer, out int bytesWritten, out Interop.ErrorInfo errorInfo, out bool pending))
                {
                    return errorInfo;
                }

                buffer = buffer.Slice(bytesWritten); // Write may be partial.

                Debug.Assert(pending);
                if (!isBlocking)
                {
                    break;
                }
                // The handle changed to non-blocking due to a concurrent operation.
                isBlocking = false;
                doSync = AsyncContext.IsWriteReady(out sequenceNumber);
            }

            WriteOperation op = RentWriteOperation();
            fixed (byte* bufPtr = &MemoryMarshal.GetReference(buffer))
            {
                op.Init(bufPtr, buffer.Length);

                SyncResult result = AsyncContext.WriteSync(op, sequenceNumber, timeout: -1);

                if (result == SyncResult.Completed)
                {
                    Interop.ErrorInfo errorInfo = op.WriteResult;

                    ReturnWriteOperation(op);

                    return errorInfo;
                }

                return new Interop.ErrorInfo(Interop.Error.ECANCELED);
            }
        }

        internal ValueTask<Interop.ErrorInfo> WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
        {
            int bytesWritten = 0;
            if (AsyncContext.IsWriteReady(out int sequenceNumber) &&
                TryCompleteWrite(source.Span, out bytesWritten, out Interop.ErrorInfo writeResult, out _))
            {
                return new ValueTask<Interop.ErrorInfo>(writeResult);
            }

            WriteOperation op = RentWriteOperation();
            op.Init(source.Slice(bytesWritten), cancellationToken);

            AsyncResult result = AsyncContext.WriteAsync(op, sequenceNumber, cancellationToken);

            if (result == AsyncResult.Pending)
            {
                return new ValueTask<Interop.ErrorInfo>(op, op.Version);
            }
            else if (result == AsyncResult.Completed)
            {
                writeResult = op.WriteResult;
                ReturnWriteOperation(op);
                return new ValueTask<Interop.ErrorInfo>(writeResult);
            }

            return new ValueTask<Interop.ErrorInfo>(new Interop.ErrorInfo(Interop.Error.ECANCELED));
        }

        private sealed unsafe class ReadOperation : UnixHandleAsyncContext.Operation, IValueTaskSource<(int BytesRead, Interop.ErrorInfo ErrorInfo)>
        {
            private readonly SafePipeHandle _owner;
            internal (int BytesRead, Interop.ErrorInfo ErrorInfo) Result;
            private ManualResetValueTaskSourceCore<(int, Interop.ErrorInfo)> _mrvtsc;
            private Memory<byte> _buffer;
            private byte* _syncBuffer;
            private int _syncBufferLength;
            private CancellationToken _cancellationToken;

            internal ReadOperation(SafePipeHandle owner)
                => _owner = owner;

            internal short Version
                => _mrvtsc.Version;

            internal void Init(byte* syncBuffer, int syncBufferLength)
            {
                _syncBuffer = syncBuffer;
                _syncBufferLength = syncBufferLength;
            }

            internal void Init(Memory<byte> buffer, CancellationToken cancellationToken)
            {
                _buffer = buffer;
                _cancellationToken = cancellationToken;
            }

            internal void Reset()
            {
                _buffer = default;
                _syncBuffer = null;
                _cancellationToken = default;
                _mrvtsc.Reset();
            }

            protected override bool TryCompleteOperation(SafeHandle handle)
            {
                if (_syncBuffer != null)
                {
                    Debug.Assert(_syncBufferLength > 0);
                    return _owner.TryCompleteRead(_syncBuffer, _syncBufferLength, out Result, out _);
                }

                Span<byte> span = _buffer.Span;
                Debug.Assert(!span.IsEmpty);

                fixed (byte* bufPtr = &MemoryMarshal.GetReference(span))
                {
                    return _owner.TryCompleteRead(bufPtr, span.Length, out Result, out _);
                }
            }

            protected override void OnCompleted(OnCompletedResult result)
            {
                if (result == OnCompletedResult.Completed)
                {
                    _mrvtsc.SetResult(Result);
                }
                else if (result == OnCompletedResult.Canceled)
                {
                    _mrvtsc.SetException(new OperationCanceledException(_cancellationToken));
                }
                else
                {
                    Debug.Assert(result == OnCompletedResult.Aborted);
                    _mrvtsc.SetException(new OperationCanceledException());
                }
            }

            ValueTaskSourceStatus IValueTaskSource<(int BytesRead, Interop.ErrorInfo ErrorInfo)>.GetStatus(short token)
                => _mrvtsc.GetStatus(token);

            void IValueTaskSource<(int BytesRead, Interop.ErrorInfo ErrorInfo)>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
                => _mrvtsc.OnCompleted(continuation, state, token, flags);

            (int BytesRead, Interop.ErrorInfo ErrorInfo) IValueTaskSource<(int BytesRead, Interop.ErrorInfo ErrorInfo)>.GetResult(short token)
            {
                bool canPool = _mrvtsc.GetStatus(token) != ValueTaskSourceStatus.Canceled;
                try
                {
                    return _mrvtsc.GetResult(token);
                }
                finally
                {
                    if (canPool)
                    {
                        _owner.ReturnReadOperation(this);
                    }
                }
            }
        }

        private sealed unsafe class WriteOperation : UnixHandleAsyncContext.Operation, IValueTaskSource<Interop.ErrorInfo>
        {
            private readonly SafePipeHandle _owner;
            internal Interop.ErrorInfo WriteResult;
            private ManualResetValueTaskSourceCore<Interop.ErrorInfo> _mrvtsc;
            private ReadOnlyMemory<byte> _buffer;
            private byte* _syncBuffer;
            private int _syncRemaining;
            private CancellationToken _cancellationToken;

            internal WriteOperation(SafePipeHandle owner)
                => _owner = owner;

            internal short Version
                => _mrvtsc.Version;

            internal void Init(byte* syncBuffer, int syncRemaining)
            {
                _syncBuffer = syncBuffer;
                _syncRemaining = syncRemaining;
            }

            internal void Init(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
            {
                _buffer = buffer;
                _cancellationToken = cancellationToken;
            }

            internal void Reset()
            {
                _buffer = default;
                _syncBuffer = null;
                _cancellationToken = default;
                _mrvtsc.Reset();
            }

            protected override bool TryCompleteOperation(SafeHandle handle)
            {
                if (_syncBuffer != null)
                {
                    Debug.Assert(_syncRemaining > 0);

                    if (_owner.TryCompleteWrite(_syncBuffer, _syncRemaining, out int bytesWritten, out WriteResult, out _))
                    {
                        return true;
                    }
                    _syncBuffer += bytesWritten;
                    _syncRemaining -= bytesWritten;
                    return false;
                }

                ReadOnlySpan<byte> span = _buffer.Span;
                Debug.Assert(!span.IsEmpty);

                fixed (byte* bufPtr = &MemoryMarshal.GetReference(span))
                {
                    if (_owner.TryCompleteWrite(bufPtr, span.Length, out int bytesWritten, out WriteResult, out _))
                    {
                        return true;
                    }
                    _buffer = _buffer.Slice(bytesWritten);
                    return false;
                }
            }

            protected override void OnCompleted(OnCompletedResult result)
            {
                if (result == OnCompletedResult.Completed)
                {
                    _mrvtsc.SetResult(WriteResult);
                }
                else if (result == OnCompletedResult.Canceled)
                {
                    _mrvtsc.SetException(new OperationCanceledException(_cancellationToken));
                }
                else
                {
                    Debug.Assert(result == OnCompletedResult.Aborted);
                    _mrvtsc.SetException(new OperationCanceledException());
                }
            }

            ValueTaskSourceStatus IValueTaskSource<Interop.ErrorInfo>.GetStatus(short token)
                => _mrvtsc.GetStatus(token);

            void IValueTaskSource<Interop.ErrorInfo>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
                => _mrvtsc.OnCompleted(continuation, state, token, flags);

            Interop.ErrorInfo IValueTaskSource<Interop.ErrorInfo>.GetResult(short token)
            {
                bool canPool = _mrvtsc.GetStatus(token) != ValueTaskSourceStatus.Canceled;
                try
                {
                    return _mrvtsc.GetResult(token);
                }
                finally
                {
                    if (canPool)
                    {
                        _owner.ReturnWriteOperation(this);
                    }
                }
            }
        }

        private unsafe bool TryCompleteRead(Span<byte> buffer, out (int BytesRead, Interop.ErrorInfo ErrorInfo) result, out bool pending)
        {
            fixed (byte* bufPtr = &MemoryMarshal.GetReference(buffer))
            {
                return TryCompleteRead(bufPtr, buffer.Length, out result, out pending);
            }
        }

        private unsafe bool TryCompleteRead(byte* buffer, int length, out (int BytesRead, Interop.ErrorInfo ErrorInfo) result, out bool pending)
        {
            int bytesRead = Interop.Sys.Read(this, buffer, length);
            if (bytesRead < 0)
            {
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                if (IsPending(errorInfo))
                {
                    pending = true;
                    result = default;
                    return false;
                }
                pending = false;
                result = (-1, errorInfo);
                return true;
            }

            pending = false;
            result = (bytesRead, default);
            return true;
        }

        private unsafe bool TryCompleteWrite(ReadOnlySpan<byte> buffer, out int bytesWritten, out Interop.ErrorInfo errorInfo, out bool pending)
        {
            fixed (byte* bufPtr = &MemoryMarshal.GetReference(buffer))
            {
                return TryCompleteWrite(bufPtr, buffer.Length, out bytesWritten, out errorInfo, out pending);
            }
        }

        private unsafe bool TryCompleteWrite(byte* buffer, int length, out int bytesWritten, out Interop.ErrorInfo errorInfo, out bool pending)
        {
            int totalBytesWritten = 0;
            while (true)
            {
                int written = Interop.Sys.Write(this, buffer, length);
                if (written < 0)
                {
                    errorInfo = Interop.Sys.GetLastErrorInfo();
                    bytesWritten = totalBytesWritten;
                    pending = IsPending(errorInfo);
                    return !pending;
                }

                totalBytesWritten += written;
                length -= written;
                if (length == 0)
                {
                    pending = false;
                    errorInfo = default;
                    bytesWritten = totalBytesWritten;
                    return true;
                }

                buffer += written;
            }
        }

        private static bool IsPending(Interop.ErrorInfo errorInfo)
            => errorInfo.Error is Interop.Error.EAGAIN or Interop.Error.EWOULDBLOCK;

        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        private static extern UnixHandleAsyncContext CreateAsyncContext(SafeHandle handle);
    }
}
