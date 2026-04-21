// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafePipeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private const int DefaultInvalidHandle = -1;

        private NullableBool _isBlocking;
        private PollableHandle? _pollHandle;
        private PipeReadOperation? _cachedReadOp;
        private PipeWriteOperation? _cachedWriteOp;

        private PipeReadOperation RentReadOperation()
            => Interlocked.Exchange(ref _cachedReadOp, null) ?? new PipeReadOperation(this);

        private PipeWriteOperation RentWriteOperation()
            => Interlocked.Exchange(ref _cachedWriteOp, null) ?? new PipeWriteOperation(this);

        private void ReturnReadOperation(PipeReadOperation op)
        {
            op.Reset();
            Volatile.Write(ref _cachedReadOp, op);
        }

        private void ReturnWriteOperation(PipeWriteOperation op)
        {
            op.Reset();
            Volatile.Write(ref _cachedWriteOp, op);
        }

        private bool IsBlocking
        {
            get
            {
                NullableBool isBlocking = _isBlocking;
                if (isBlocking == NullableBool.Undefined && !IsClosed)
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

        private void SetHandleBlocking(bool blocking)
        {
            NullableBool newValue = blocking ? NullableBool.True : NullableBool.False;
            if (_isBlocking != newValue)
            {
                if (Interop.Sys.Fcntl.SetIsNonBlocking(this, blocking ? 0 : 1) != 0)
                {
                    throw Interop.GetExceptionForIoErrno(Interop.Sys.GetLastErrorInfo());
                }
                _isBlocking = newValue;
            }
        }

        private PollableHandle PollHandle
        {
            get
            {
                if (_pollHandle == null)
                {
                    SetHandleBlocking(false);
                    PollableHandle.Create(this, ref _pollHandle);
                }
                return _pollHandle!;
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
                _pollHandle?.Dispose();
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

            bool doSync = isBlocking || PollHandle.IsReadReady(out sequenceNumber);
            if (doSync)
            {
                if (TryCompleteRead(buffer, out var result))
                {
                    return result;
                }
            }

            PipeReadOperation op = RentReadOperation();
            fixed (byte* bufPtr = &MemoryMarshal.GetReference(buffer))
            {
                op.SyncBuffer = bufPtr;
                op.SyncBufferLength = buffer.Length;

                PollOperationSyncResult result = PollHandle.ReadSync(op, sequenceNumber, timeout: -1);

                if (result == PollOperationSyncResult.Completed)
                {
                    var readResult = op.Result;

                    ReturnReadOperation(op);

                    return readResult;
                }

                return (-1, new Interop.ErrorInfo(Interop.Error.EPIPE));
            }
        }

        internal ValueTask<(int BytesRead, Interop.ErrorInfo ErrorInfo)> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken)
        {
            if (PollHandle.IsReadReady(out int sequenceNumber) &&
                TryCompleteRead(destination.Span, out var readResult))
            {
                return new ValueTask<(int, Interop.ErrorInfo)>(readResult);
            }

            PipeReadOperation op = RentReadOperation();
            op.Buffer = destination;
            op.CancellationToken = cancellationToken;

            PollOperationAsyncResult result = PollHandle.ReadAsync(op, sequenceNumber, cancellationToken);

            if (result == PollOperationAsyncResult.Pending)
            {
                return new ValueTask<(int, Interop.ErrorInfo)>(op, op.Version);
            }
            else if (result == PollOperationAsyncResult.Completed)
            {
                readResult = op.Result;
                ReturnReadOperation(op);
                return new ValueTask<(int, Interop.ErrorInfo)>(readResult);
            }

            return new ValueTask<(int, Interop.ErrorInfo)>((-1, new Interop.ErrorInfo(Interop.Error.EPIPE)));
        }

        internal unsafe Interop.ErrorInfo Write(ReadOnlySpan<byte> buffer)
        {
            int sequenceNumber = 0;
            bool isBlocking = IsBlocking;

            bool doSync = isBlocking || PollHandle.IsWriteReady(out sequenceNumber);
            while (doSync)
            {
                if (TryCompleteWrite(buffer, out int bytesWritten, out Interop.ErrorInfo errorInfo))
                {
                    return errorInfo;
                }
                buffer = buffer.Slice(bytesWritten);
                doSync = isBlocking;
            }

            PipeWriteOperation op = RentWriteOperation();
            fixed (byte* bufPtr = &MemoryMarshal.GetReference(buffer))
            {
                op.SyncBuffer = bufPtr;
                op.SyncRemaining = buffer.Length;

                PollOperationSyncResult result = PollHandle.WriteSync(op, sequenceNumber, timeout: -1);

                if (result == PollOperationSyncResult.Completed)
                {
                    Interop.ErrorInfo errorInfo = op.WriteResult;

                    ReturnWriteOperation(op);

                    return errorInfo;
                }

                return new Interop.ErrorInfo(Interop.Error.EPIPE);
            }
        }

        internal ValueTask<Interop.ErrorInfo> WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
        {
            int bytesWritten = 0;
            if (PollHandle.IsWriteReady(out int sequenceNumber) &&
                TryCompleteWrite(source.Span, out bytesWritten, out Interop.ErrorInfo writeResult))
            {
                return new ValueTask<Interop.ErrorInfo>(writeResult);
            }

            PipeWriteOperation op = RentWriteOperation();
            op.Buffer = source.Slice(bytesWritten);
            op.CancellationToken = cancellationToken;

            PollOperationAsyncResult result = PollHandle.WriteAsync(op, sequenceNumber, cancellationToken);

            if (result == PollOperationAsyncResult.Pending)
            {
                return new ValueTask<Interop.ErrorInfo>(op, op.Version);
            }
            else if (result == PollOperationAsyncResult.Completed)
            {
                writeResult = op.WriteResult;
                ReturnWriteOperation(op);
                return new ValueTask<Interop.ErrorInfo>(writeResult);
            }

            return new ValueTask<Interop.ErrorInfo>(new Interop.ErrorInfo(Interop.Error.EPIPE));
        }

        private sealed unsafe class PipeReadOperation : PollTriggeredOperation, IValueTaskSource<(int BytesRead, Interop.ErrorInfo ErrorInfo)>
        {
            private readonly SafePipeHandle _owner;
            internal (int BytesRead, Interop.ErrorInfo ErrorInfo) Result;
            private ManualResetValueTaskSourceCore<(int, Interop.ErrorInfo)> _mrvtsc;
            internal Memory<byte> Buffer;
            internal byte* SyncBuffer;
            internal int SyncBufferLength;
            internal CancellationToken CancellationToken;

            internal PipeReadOperation(SafePipeHandle owner)
                => _owner = owner;

            internal short Version
                => _mrvtsc.Version;

            internal void Reset()
            {
                Buffer = default;
                SyncBuffer = null;
                CancellationToken = default;
                _mrvtsc.Reset();
            }

            protected override bool TryCompleteOperation(SafeHandle handle)
            {
                if (SyncBuffer != null)
                {
                    Debug.Assert(SyncBufferLength > 0);
                    return TryCompleteRead((SafePipeHandle)handle, SyncBuffer, SyncBufferLength, out Result);
                }

                Span<byte> span = Buffer.Span;
                Debug.Assert(!span.IsEmpty);

                fixed (byte* bufPtr = &MemoryMarshal.GetReference(span))
                {
                    return TryCompleteRead((SafePipeHandle)handle, bufPtr, span.Length, out Result);
                }
            }

            protected override void OnCompleted(PollOperationOnCompletedResult result)
            {
                if (result == PollOperationOnCompletedResult.Completed)
                {
                    _mrvtsc.SetResult(Result);
                }
                else if (result == PollOperationOnCompletedResult.Canceled)
                {
                    _mrvtsc.SetException(new OperationCanceledException(CancellationToken));
                }
                else
                {
                    Debug.Assert(result == PollOperationOnCompletedResult.Aborted);
                    _mrvtsc.SetException(new ObjectDisposedException(typeof(SafePipeHandle).FullName));
                }
            }

            ValueTaskSourceStatus IValueTaskSource<(int BytesRead, Interop.ErrorInfo ErrorInfo)>.GetStatus(short token)
                => _mrvtsc.GetStatus(token);

            void IValueTaskSource<(int BytesRead, Interop.ErrorInfo ErrorInfo)>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
                => _mrvtsc.OnCompleted(continuation, state, token, flags);

            (int BytesRead, Interop.ErrorInfo ErrorInfo) IValueTaskSource<(int BytesRead, Interop.ErrorInfo ErrorInfo)>.GetResult(short token)
            {
                bool canPool = _mrvtsc.GetStatus(token) == ValueTaskSourceStatus.Succeeded;
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

        private sealed unsafe class PipeWriteOperation : PollTriggeredOperation, IValueTaskSource<Interop.ErrorInfo>
        {
            private readonly SafePipeHandle _owner;
            internal Interop.ErrorInfo WriteResult;
            private ManualResetValueTaskSourceCore<Interop.ErrorInfo> _mrvtsc;
            internal ReadOnlyMemory<byte> Buffer;
            internal byte* SyncBuffer;
            internal int SyncRemaining;
            internal CancellationToken CancellationToken;

            internal PipeWriteOperation(SafePipeHandle owner)
                => _owner = owner;

            internal short Version => _mrvtsc.Version;

            internal void Reset()
            {
                Buffer = default;
                SyncBuffer = null;
                CancellationToken = default;
                _mrvtsc.Reset();
            }

            protected override bool TryCompleteOperation(SafeHandle handle)
            {
                if (SyncBuffer != null)
                {
                    Debug.Assert(SyncRemaining > 0);

                    if (TryCompleteWrite((SafePipeHandle)handle, SyncBuffer, SyncRemaining, out int bytesWritten, out WriteResult))
                    {
                        return true;
                    }
                    SyncBuffer += bytesWritten;
                    SyncRemaining -= bytesWritten;
                    return false;
                }

                ReadOnlySpan<byte> span = Buffer.Span;
                Debug.Assert(!span.IsEmpty);

                fixed (byte* bufPtr = &MemoryMarshal.GetReference(span))
                {
                    if (TryCompleteWrite((SafePipeHandle)handle, bufPtr, span.Length, out int bytesWritten, out WriteResult))
                    {
                        return true;
                    }
                    Buffer = Buffer.Slice(bytesWritten);
                    return false;
                }
            }

            protected override void OnCompleted(PollOperationOnCompletedResult result)
            {
                if (result == PollOperationOnCompletedResult.Completed)
                {
                    _mrvtsc.SetResult(WriteResult);
                }
                else if (result == PollOperationOnCompletedResult.Canceled)
                {
                    _mrvtsc.SetException(new OperationCanceledException(CancellationToken));
                }
                else
                {
                    Debug.Assert(result == PollOperationOnCompletedResult.Aborted);
                    _mrvtsc.SetException(new ObjectDisposedException(typeof(SafePipeHandle).FullName));
                }
            }

            ValueTaskSourceStatus IValueTaskSource<Interop.ErrorInfo>.GetStatus(short token)
                => _mrvtsc.GetStatus(token);

            void IValueTaskSource<Interop.ErrorInfo>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
                => _mrvtsc.OnCompleted(continuation, state, token, flags);

            Interop.ErrorInfo IValueTaskSource<Interop.ErrorInfo>.GetResult(short token)
            {
                bool canPool = _mrvtsc.GetStatus(token) == ValueTaskSourceStatus.Succeeded;
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

        private static unsafe bool TryCompleteRead(SafePipeHandle handle, byte* buffer, int length, out (int BytesRead, Interop.ErrorInfo ErrorInfo) result)
        {
            int bytesRead = Interop.Sys.Read(handle, buffer, length);
            if (bytesRead < 0)
            {
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                if (IsPending(errorInfo))
                {
                    result = default;
                    return false;
                }
                result = (-1, errorInfo);
                return true;
            }

            result = (bytesRead, default);
            return true;
        }

        private unsafe bool TryCompleteRead(Span<byte> buffer, out (int BytesRead, Interop.ErrorInfo ErrorInfo) result)
        {
            fixed (byte* bufPtr = &MemoryMarshal.GetReference(buffer))
            {
                return TryCompleteRead(this, bufPtr, buffer.Length, out result);
            }
        }

        private unsafe bool TryCompleteWrite(ReadOnlySpan<byte> buffer, out int bytesWritten, out Interop.ErrorInfo errorInfo)
        {
            fixed (byte* bufPtr = &MemoryMarshal.GetReference(buffer))
            {
                return TryCompleteWrite(this, bufPtr, buffer.Length, out bytesWritten, out errorInfo);
            }
        }

        private static unsafe bool TryCompleteWrite(SafePipeHandle handle, byte* buffer, int length, out int bytesWritten, out Interop.ErrorInfo errorInfo)
        {
            bytesWritten = Interop.Sys.Write(handle, buffer, length);
            if (bytesWritten < 0)
            {
                errorInfo = Interop.Sys.GetLastErrorInfo();
                if (IsPending(errorInfo))
                {
                    bytesWritten = 0;
                    return false;
                }
                return true;
            }

            errorInfo = default;
            return bytesWritten == length;
        }

        private static bool IsPending(Interop.ErrorInfo errorInfo)
            => errorInfo.Error == Interop.Error.EAGAIN || errorInfo.Error == Interop.Error.EWOULDBLOCK;
    }
}
