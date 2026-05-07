// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO.Pipes;
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

        internal unsafe int Read(Span<byte> buffer, PipeStream? stream)
        {
            int sequenceNumber = 0;
            bool isBlocking = IsBlocking;

            bool doSync = isBlocking || AsyncContext.IsReadReady(out sequenceNumber);
            if (doSync)
            {
                if (TryCompleteRead(buffer, stream, out var result))
                {
                    CheckPipeCall(result.ErrorInfo);
                    return result.BytesRead;
                }
                if (isBlocking)
                {
                    // The handle changed to non-blocking due to a concurrent operation.
                    if (AsyncContext.IsReadReady(out sequenceNumber) && TryCompleteRead(buffer, stream, out result))
                    {
                        CheckPipeCall(result.ErrorInfo);
                        return result.BytesRead;
                    }
                }
            }

            ReadOperation op = RentReadOperation();
            fixed (byte* bufPtr = &MemoryMarshal.GetReference(buffer))
            {
                op.Init(bufPtr, buffer.Length, stream);

                SyncResult result = AsyncContext.ReadSync(op, sequenceNumber, timeout: -1);

                if (result == SyncResult.Completed)
                {
                    int bytesRead = op.BytesRead;
                    Exception? exception = op.Exception;

                    ReturnReadOperation(op);

                    if (exception != null)
                    {
                        throw exception;
                    }
                    return bytesRead;
                }

                throw new OperationCanceledException();
            }
        }

        internal ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken, PipeStream? stream)
        {
            if (AsyncContext.IsReadReady(out int sequenceNumber) &&
                TryCompleteRead(destination.Span, stream, out var readResult))
            {
                CheckPipeCall(readResult.ErrorInfo);
                return new ValueTask<int>(readResult.BytesRead);
            }

            ReadOperation op = RentReadOperation();
            op.Init(destination, cancellationToken, stream);

            AsyncResult result = AsyncContext.ReadAsync(op, sequenceNumber, cancellationToken);

            if (result == AsyncResult.Pending)
            {
                return new ValueTask<int>(op, op.Version);
            }
            else if (result == AsyncResult.Completed)
            {
                int bytesRead = op.BytesRead;
                Exception? exception = op.Exception;

                ReturnReadOperation(op);

                if (exception != null)
                {
                    throw exception;
                }
                return new ValueTask<int>(bytesRead);
            }

            throw new OperationCanceledException();
        }

        internal unsafe void Write(ReadOnlySpan<byte> buffer, PipeStream? stream)
        {
            int sequenceNumber = 0;
            bool isBlocking = IsBlocking;

            bool doSync = isBlocking || AsyncContext.IsWriteReady(out sequenceNumber);
            while (doSync)
            {
                if (TryCompleteWrite(buffer, stream, out int bytesWritten, out Interop.ErrorInfo errorInfo))
                {
                    CheckPipeCall(errorInfo);
                    return;
                }

                buffer = buffer.Slice(bytesWritten); // Write may be partial.

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
                op.Init(bufPtr, buffer.Length, stream);

                SyncResult result = AsyncContext.WriteSync(op, sequenceNumber, timeout: -1);

                if (result == SyncResult.Completed)
                {
                    Exception? exception = op.Exception;

                    ReturnWriteOperation(op);

                    if (exception != null)
                    {
                        throw exception;
                    }
                    return;
                }

                throw new OperationCanceledException();
            }
        }

        internal ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken, PipeStream? stream)
        {
            int bytesWritten = 0;
            if (AsyncContext.IsWriteReady(out int sequenceNumber) &&
                TryCompleteWrite(source.Span, stream, out bytesWritten, out Interop.ErrorInfo writeResult))
            {
                CheckPipeCall(writeResult);
                return default;
            }

            WriteOperation op = RentWriteOperation();
            op.Init(source.Slice(bytesWritten), cancellationToken, stream);

            AsyncResult result = AsyncContext.WriteAsync(op, sequenceNumber, cancellationToken);

            if (result == AsyncResult.Pending)
            {
                return new ValueTask(op, op.Version);
            }
            else if (result == AsyncResult.Completed)
            {
                Exception? exception = op.Exception;
                ReturnWriteOperation(op);
                if (exception != null)
                {
                    throw exception;
                }
                return default;
            }

            throw new OperationCanceledException();
        }

        private sealed unsafe class ReadOperation : UnixHandleAsyncContext.Operation, IValueTaskSource<int>
        {
            private readonly SafePipeHandle _owner;
            internal int BytesRead;
            internal Exception? Exception;
            private ManualResetValueTaskSourceCore<int> _mrvtsc;
            private Memory<byte> _buffer;
            private byte* _syncBuffer;
            private int _syncBufferLength;
            private PipeStream? _stream;
            private CancellationToken _cancellationToken;

            internal ReadOperation(SafePipeHandle owner)
                => _owner = owner;

            internal short Version
                => _mrvtsc.Version;

            internal void Init(byte* syncBuffer, int syncBufferLength, PipeStream? stream)
            {
                _syncBuffer = syncBuffer;
                _syncBufferLength = syncBufferLength;
                _stream = stream;
            }

            internal void Init(Memory<byte> buffer, CancellationToken cancellationToken, PipeStream? stream)
            {
                _buffer = buffer;
                _cancellationToken = cancellationToken;
                _stream = stream;
            }

            internal void Reset()
            {
                _buffer = default;
                _syncBuffer = null;
                _stream = null;
                _cancellationToken = default;
                Exception = null;
                _mrvtsc.Reset();
            }

            protected override bool TryCompleteOperation(SafeHandle handle)
            {
                (int BytesRead, Interop.ErrorInfo ErrorInfo) readResult;

                if (_syncBuffer != null)
                {
                    Debug.Assert(_syncBufferLength > 0);
                    if (!_owner.TryCompleteRead(_syncBuffer, _syncBufferLength, _stream, out readResult))
                    {
                        return false;
                    }
                }
                else
                {
                    Span<byte> span = _buffer.Span;
                    Debug.Assert(!span.IsEmpty);

                    fixed (byte* bufPtr = &MemoryMarshal.GetReference(span))
                    {
                        if (!_owner.TryCompleteRead(bufPtr, span.Length, _stream, out readResult))
                        {
                            return false;
                        }
                    }
                }

                BytesRead = readResult.BytesRead;
                if (readResult.ErrorInfo.Error != Interop.Error.SUCCESS)
                {
                    Exception = Interop.GetExceptionForIoErrno(readResult.ErrorInfo);
                }
                return true;
            }

            protected override void OnCompleted(OnCompletedResult result)
            {
                if (result == OnCompletedResult.Completed)
                {
                    if (Exception != null)
                    {
                        _mrvtsc.SetException(Exception);
                    }
                    else
                    {
                        _mrvtsc.SetResult(BytesRead);
                    }
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

            ValueTaskSourceStatus IValueTaskSource<int>.GetStatus(short token)
                => _mrvtsc.GetStatus(token);

            void IValueTaskSource<int>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
                => _mrvtsc.OnCompleted(continuation, state, token, flags);

            int IValueTaskSource<int>.GetResult(short token)
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

        private sealed unsafe class WriteOperation : UnixHandleAsyncContext.Operation, IValueTaskSource
        {
            private readonly SafePipeHandle _owner;
            internal Exception? Exception;
            private ManualResetValueTaskSourceCore<bool> _mrvtsc;
            private ReadOnlyMemory<byte> _buffer;
            private byte* _syncBuffer;
            private int _syncRemaining;
            private PipeStream? _stream;
            private CancellationToken _cancellationToken;

            internal WriteOperation(SafePipeHandle owner)
                => _owner = owner;

            internal short Version
                => _mrvtsc.Version;

            internal void Init(byte* syncBuffer, int syncRemaining, PipeStream? stream)
            {
                _syncBuffer = syncBuffer;
                _syncRemaining = syncRemaining;
                _stream = stream;
            }

            internal void Init(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken, PipeStream? stream)
            {
                _buffer = buffer;
                _cancellationToken = cancellationToken;
                _stream = stream;
            }

            internal void Reset()
            {
                _buffer = default;
                _syncBuffer = null;
                _stream = null;
                _cancellationToken = default;
                Exception = null;
                _mrvtsc.Reset();
            }

            protected override bool TryCompleteOperation(SafeHandle handle)
            {
                Interop.ErrorInfo errorInfo;

                if (_syncBuffer != null)
                {
                    Debug.Assert(_syncRemaining > 0);

                    if (!_owner.TryCompleteWrite(_syncBuffer, _syncRemaining, _stream, out int bytesWritten, out errorInfo))
                    {
                        _syncBuffer += bytesWritten;
                        _syncRemaining -= bytesWritten;
                        return false;
                    }
                }
                else
                {
                    ReadOnlySpan<byte> span = _buffer.Span;
                    Debug.Assert(!span.IsEmpty);

                    fixed (byte* bufPtr = &MemoryMarshal.GetReference(span))
                    {
                        if (!_owner.TryCompleteWrite(bufPtr, span.Length, _stream, out int bytesWritten, out errorInfo))
                        {
                            _buffer = _buffer.Slice(bytesWritten);
                            return false;
                        }
                    }
                }

                if (errorInfo.Error != Interop.Error.SUCCESS)
                {
                    Exception = Interop.GetExceptionForIoErrno(errorInfo);
                }
                return true;
            }

            protected override void OnCompleted(OnCompletedResult result)
            {
                if (result == OnCompletedResult.Completed)
                {
                    if (Exception != null)
                    {
                        _mrvtsc.SetException(Exception);
                    }
                    else
                    {
                        _mrvtsc.SetResult(default);
                    }
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

            ValueTaskSourceStatus IValueTaskSource.GetStatus(short token)
                => _mrvtsc.GetStatus(token);

            void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
                => _mrvtsc.OnCompleted(continuation, state, token, flags);

            void IValueTaskSource.GetResult(short token)
            {
                bool canPool = _mrvtsc.GetStatus(token) != ValueTaskSourceStatus.Canceled;
                try
                {
                    _mrvtsc.GetResult(token);
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

        private unsafe bool TryCompleteRead(Span<byte> buffer, PipeStream? stream, out (int BytesRead, Interop.ErrorInfo ErrorInfo) result)
        {
            fixed (byte* bufPtr = &MemoryMarshal.GetReference(buffer))
            {
                return TryCompleteRead(bufPtr, buffer.Length, stream, out result);
            }
        }

        private unsafe bool TryCompleteRead(byte* buffer, int length, PipeStream? stream, out (int BytesRead, Interop.ErrorInfo ErrorInfo) result)
        {
            int bytesRead = Interop.Sys.Read(this, buffer, length);
            if (bytesRead < 0)
            {
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                if (IsPending(errorInfo))
                {
                    result = default;
                    return false;
                }
                SetPipeStreamBrokenOnEPIPE(stream, errorInfo);
                result = (-1, errorInfo);
                return true;
            }

            result = (bytesRead, default);
            return true;
        }

        private unsafe bool TryCompleteWrite(ReadOnlySpan<byte> buffer, PipeStream? stream, out int bytesWritten, out Interop.ErrorInfo errorInfo)
        {
            fixed (byte* bufPtr = &MemoryMarshal.GetReference(buffer))
            {
                return TryCompleteWrite(bufPtr, buffer.Length, stream, out bytesWritten, out errorInfo);
            }
        }

        private unsafe bool TryCompleteWrite(byte* buffer, int length, PipeStream? stream, out int bytesWritten, out Interop.ErrorInfo errorInfo)
        {
            int totalBytesWritten = 0;
            while (true)
            {
                int written = Interop.Sys.Write(this, buffer, length);
                if (written < 0)
                {
                    errorInfo = Interop.Sys.GetLastErrorInfo();
                    bytesWritten = totalBytesWritten;
                    if (!IsPending(errorInfo))
                    {
                        SetPipeStreamBrokenOnEPIPE(stream, errorInfo);
                        return true;
                    }
                    return false;
                }

                totalBytesWritten += written;
                length -= written;
                if (length == 0)
                {
                    errorInfo = default;
                    bytesWritten = totalBytesWritten;
                    return true;
                }

                buffer += written;
            }
        }

        private static void SetPipeStreamBrokenOnEPIPE(PipeStream? stream, Interop.ErrorInfo errorInfo)
        {
            if (errorInfo.Error == Interop.Error.EPIPE && stream != null)
                stream.State = PipeState.Broken;
        }

        private static void CheckPipeCall(Interop.ErrorInfo errorInfo)
        {
            if (errorInfo.Error != Interop.Error.SUCCESS)
            {
                throw Interop.GetExceptionForIoErrno(errorInfo);
            }
        }

        private static bool IsPending(Interop.ErrorInfo errorInfo)
            => errorInfo.Error is Interop.Error.EAGAIN or Interop.Error.EWOULDBLOCK;

        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        private static extern UnixHandleAsyncContext CreateAsyncContext(SafeHandle handle);
    }
}
