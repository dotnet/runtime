// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Microsoft.Win32.SafeHandles;
using AsyncResult = System.Threading.UnixHandleAsyncContext.AsyncResult;
using OnCompletedResult = System.Threading.UnixHandleAsyncContext.OnCompletedResult;
using SyncResult = System.Threading.UnixHandleAsyncContext.SyncResult;

namespace System.IO.Ports
{
    internal sealed partial class SafeSerialDeviceHandle : SafeHandleMinusOneIsInvalid
    {
        internal enum ReceiveThresholdResult
        {
            Success,
            Eof,
            Error,
            Disposed,
        }

        private UnixHandleAsyncContext? _asyncContext;
        private ReadOperation? _cachedReadOp;
        private WriteOperation? _cachedWriteOp;
        private WaitReceiveThresholdOperation? _cachedWaitReceiveThresholdOp;
        private byte[]? _thresholdBuffer;
        private int _thresholdBufferCount;

        // This guards the _thresholdBuffer which different concurrent readers may try to use.
        private object ReadLock => DisposeLock; // Use the same lock as the dispose lock (no potential ordering issues).

        partial void GetBufferedCount(ref int count)
            => count = Volatile.Read(ref _thresholdBufferCount);

        internal void DiscardInBuffer()
        {
            lock (ReadLock)
            {
                // This may or may not work depending on hardware.
                Interop.Termios.TermiosDiscard(this, Interop.Termios.Queue.ReceiveQueue);

                _thresholdBufferCount = 0;
            }
        }

        private unsafe bool ReadIntoThresholdBuffer(int threshold, out int bytesAvailable, out ReceiveThresholdResult result)
        {
            // Our caller is already holding the ReadLock.
            Debug.Assert(Monitor.IsEntered(ReadLock));

            int currentCount = _thresholdBufferCount;

            if (_thresholdBuffer == null || _thresholdBuffer.Length < threshold)
            {
                Array.Resize(ref _thresholdBuffer, threshold);
            }

            fixed (byte* bufPtr = &_thresholdBuffer[currentCount])
            {
                int bytesRead = Interop.Serial.Read(this, bufPtr, threshold - currentCount);
                bytesAvailable = _thresholdBufferCount = currentCount + Math.Max(bytesRead, 0);
                if (bytesRead < 0 && !IsPending(Interop.Sys.GetLastErrorInfo()))
                {
                    result = ReceiveThresholdResult.Error;
                    return true;
                }
                bool eof = bytesRead == 0;
                result = eof ? ReceiveThresholdResult.Eof : ReceiveThresholdResult.Success;
                return eof || bytesAvailable >= threshold;
            }
        }

        private int DrainThresholdBuffer(Span<byte> destination)
        {
            int count = _thresholdBufferCount;
            if (count == 0)
            {
                return 0;
            }

            int toCopy = Math.Min(count, destination.Length);
            _thresholdBuffer.AsSpan(0, toCopy).CopyTo(destination);

            int remaining = count - toCopy;
            if (remaining > 0)
            {
                Buffer.BlockCopy(_thresholdBuffer!, toCopy, _thresholdBuffer!, 0, remaining);
            }
            _thresholdBufferCount = remaining;

            return toCopy;
        }

        private UnixHandleAsyncContext AsyncContext
        {
            get
            {
                if (_asyncContext == null)
                {
                    Interlocked.CompareExchange(ref _asyncContext, CreateAsyncContext(this), null);
                }
                return _asyncContext!;
            }
        }

        private ReadOperation RentReadOperation()
            => Interlocked.Exchange(ref _cachedReadOp, null) ?? new ReadOperation(this);

        private WriteOperation RentWriteOperation()
            => Interlocked.Exchange(ref _cachedWriteOp, null) ?? new WriteOperation(this);

        private WaitReceiveThresholdOperation RentWaitReceiveThresholdOperation()
            => Interlocked.Exchange(ref _cachedWaitReceiveThresholdOp, null) ?? new WaitReceiveThresholdOperation(this);

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

        private void ReturnWaitReceiveThresholdOperation(WaitReceiveThresholdOperation op)
        {
            op.Reset();
            Volatile.Write(ref _cachedWaitReceiveThresholdOp, op);
        }

        protected override void Dispose(bool disposing)
        {
            _asyncContext?.AbortAndDispose();
            lock (DisposeLock)
            {
                base.Dispose(disposing);
            }
        }

        internal unsafe int Read(Span<byte> buffer, int timeout, SerialStream? serialStream = null)
        {
            if (AsyncContext.IsReadReady(out int sequenceNumber) &&
                TryCompleteRead(buffer, out var result, serialStream))
            {
                CheckIOResult(result.ErrorInfo);
                return result.BytesRead;
            }

            ReadOperation op = RentReadOperation();
            fixed (byte* bufPtr = &MemoryMarshal.GetReference(buffer))
            {
                op.InitSync(bufPtr, buffer.Length, serialStream);

                SyncResult syncResult = AsyncContext.ReadSync(op, sequenceNumber, timeout);

                if (syncResult == SyncResult.Completed)
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

                if (syncResult == SyncResult.TimedOut)
                {
                    ReturnReadOperation(op);
                    throw new TimeoutException();
                }

                throw new OperationCanceledException();
            }
        }

        internal ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken, SerialStream? serialStream = null)
        {
            if (AsyncContext.IsReadReady(out int sequenceNumber) &&
                TryCompleteRead(destination.Span, out var readResult, serialStream))
            {
                CheckIOResult(readResult.ErrorInfo);
                return new ValueTask<int>(readResult.BytesRead);
            }

            ReadOperation op = RentReadOperation();
            op.InitAsync(destination, cancellationToken, serialStream);

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

        internal unsafe void Write(ReadOnlySpan<byte> buffer, int timeout)
        {
            if (AsyncContext.IsWriteReady(out int sequenceNumber) &&
                TryCompleteWrite(buffer, out int bytesWritten, out Interop.ErrorInfo errorInfo))
            {
                CheckIOResult(errorInfo);
                return;
            }

            WriteOperation op = RentWriteOperation();
            fixed (byte* bufPtr = &MemoryMarshal.GetReference(buffer))
            {
                op.InitSync(bufPtr, buffer.Length);

                SyncResult syncResult = AsyncContext.WriteSync(op, sequenceNumber, timeout);

                if (syncResult == SyncResult.Completed)
                {
                    Exception? exception = op.Exception;

                    ReturnWriteOperation(op);

                    if (exception != null)
                    {
                        throw exception;
                    }
                    return;
                }

                if (syncResult == SyncResult.TimedOut)
                {
                    ReturnWriteOperation(op);
                    throw new TimeoutException();
                }

                throw new OperationCanceledException();
            }
        }

        internal ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
        {
            int bytesWritten = 0;
            if (AsyncContext.IsWriteReady(out int sequenceNumber) &&
                TryCompleteWrite(source.Span, out bytesWritten, out Interop.ErrorInfo writeResult))
            {
                CheckIOResult(writeResult);
                return default;
            }

            WriteOperation op = RentWriteOperation();
            op.InitAsync(source.Slice(bytesWritten), cancellationToken);

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

        internal void WaitForReceiveThreshold(SerialStream serialStream)
        {
            if (AsyncContext.IsReadReady(out int sequenceNumber) &&
                TryCompleteWaitReceiveThreshold(serialStream.ReceivedBytesThreshold, out int bytesAvailable, out ReceiveThresholdResult thresholdResult))
            {
                serialStream.OnReceiveThreshold(bytesAvailable, thresholdResult);
                return;
            }

            WaitReceiveThresholdOperation op = RentWaitReceiveThresholdOperation();
            op.Init(serialStream);

            AsyncResult result = AsyncContext.ReadAsync(op, sequenceNumber, CancellationToken.None);

            if (result == AsyncResult.Completed)
            {
                op.CompleteOperation(OnCompletedResult.Completed);
            }
        }

        private bool TryCompleteWaitReceiveThreshold(int receivedBytesThreshold, out int bytesAvailable, out ReceiveThresholdResult result)
        {
            lock (DisposeLock)
            {
                if (!IsClosed)
                {
                    try
                    {
                        bytesAvailable = BytesToRead;
                        if (bytesAvailable >= receivedBytesThreshold)
                        {
                            result = ReceiveThresholdResult.Success;
                            return true;
                        }

                        // Drain kernel data into the threshold buffer
                        // so epoll fires when new data arrives.
                        return ReadIntoThresholdBuffer(receivedBytesThreshold, out bytesAvailable, out result);
                    }
                    catch (ObjectDisposedException)
                    { }
                }

                bytesAvailable = 0;
                result = ReceiveThresholdResult.Disposed;
                return true;
            }
        }

        private unsafe bool TryCompleteRead(Span<byte> buffer, out (int BytesRead, Interop.ErrorInfo ErrorInfo) result, SerialStream? serialStream = null)
        {
            fixed (byte* bufPtr = &MemoryMarshal.GetReference(buffer))
            {
                return TryCompleteRead(bufPtr, buffer.Length, out result, serialStream);
            }
        }

        private unsafe bool TryCompleteRead(byte* buffer, int length, out (int BytesRead, Interop.ErrorInfo ErrorInfo) result, SerialStream? serialStream = null)
        {
            lock (ReadLock)
            {
                int fromBuffer = DrainThresholdBuffer(new Span<byte>(buffer, length));
                if (fromBuffer > 0)
                {
                    buffer += fromBuffer;
                    length -= fromBuffer;
                }

                int bytesRead = length == 0 ? 0 : Interop.Serial.Read(this, buffer, length);
                if (bytesRead < 0 && fromBuffer == 0)
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

                int totalRead = fromBuffer + Math.Max(bytesRead, 0);
                if (totalRead > 0)
                {
                    serialStream?.OnBytesRead(totalRead);
                }
                result = (totalRead, default);
                return true;
            }
        }

        private unsafe bool TryCompleteWrite(ReadOnlySpan<byte> buffer, out int bytesWritten, out Interop.ErrorInfo errorInfo)
        {
            fixed (byte* bufPtr = &MemoryMarshal.GetReference(buffer))
            {
                return TryCompleteWrite(bufPtr, buffer.Length, out bytesWritten, out errorInfo);
            }
        }

        private unsafe bool TryCompleteWrite(byte* buffer, int length, out int bytesWritten, out Interop.ErrorInfo errorInfo)
        {
            int totalBytesWritten = 0;
            while (length > 0)
            {
                int written = Interop.Serial.Write(this, buffer, length);
                if (written < 0)
                {
                    errorInfo = Interop.Sys.GetLastErrorInfo();
                    bytesWritten = totalBytesWritten;
                    if (!IsPending(errorInfo))
                    {
                        return true;
                    }
                    return false;
                }

                totalBytesWritten += written;
                length -= written;
                buffer += written;
            }

            errorInfo = default;
            bytesWritten = totalBytesWritten;
            return true;
        }

        private static void CheckIOResult(Interop.ErrorInfo errorInfo)
        {
            if (errorInfo.Error != Interop.Error.SUCCESS)
            {
                throw Interop.GetIOException(errorInfo);
            }
        }

        private static bool IsPending(Interop.ErrorInfo errorInfo)
            => errorInfo.Error is Interop.Error.EAGAIN or Interop.Error.EWOULDBLOCK;

        private sealed unsafe class ReadOperation : UnixHandleAsyncContext.Operation, IValueTaskSource<int>
        {
            private readonly SafeSerialDeviceHandle _owner;
            internal int BytesRead;
            internal Exception? Exception;
            private ManualResetValueTaskSourceCore<int> _mrvtsc;
            private SerialStream? _serialStream;
            private Memory<byte> _buffer;
            private byte* _syncBuffer;
            private int _syncBufferLength;
            private CancellationToken _cancellationToken;

            internal ReadOperation(SafeSerialDeviceHandle owner)
                => _owner = owner;

            internal short Version
                => _mrvtsc.Version;

            internal void InitSync(byte* syncBuffer, int syncBufferLength, SerialStream? serialStream = null)
            {
                _serialStream = serialStream;
                _syncBuffer = syncBuffer;
                _syncBufferLength = syncBufferLength;
            }

            internal void InitAsync(Memory<byte> buffer, CancellationToken cancellationToken, SerialStream? serialStream = null)
            {
                _serialStream = serialStream;
                _buffer = buffer;
                _cancellationToken = cancellationToken;
            }

            internal void Reset()
            {
                _serialStream = null;
                _buffer = default;
                _syncBuffer = null;
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
                    if (!_owner.TryCompleteRead(_syncBuffer, _syncBufferLength, out readResult, _serialStream))
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
                        if (!_owner.TryCompleteRead(bufPtr, span.Length, out readResult, _serialStream))
                        {
                            return false;
                        }
                    }
                }

                BytesRead = readResult.BytesRead;
                if (readResult.ErrorInfo.Error != Interop.Error.SUCCESS)
                {
                    Exception = Interop.GetIOException(readResult.ErrorInfo);
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
            private readonly SafeSerialDeviceHandle _owner;
            internal Exception? Exception;
            private ManualResetValueTaskSourceCore<bool> _mrvtsc;
            private ReadOnlyMemory<byte> _buffer;
            private byte* _syncBuffer;
            private int _syncRemaining;
            private CancellationToken _cancellationToken;

            internal WriteOperation(SafeSerialDeviceHandle owner)
                => _owner = owner;

            internal short Version
                => _mrvtsc.Version;

            internal void InitSync(byte* syncBuffer, int syncRemaining)
            {
                _syncBuffer = syncBuffer;
                _syncRemaining = syncRemaining;
            }

            internal void InitAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
            {
                _buffer = buffer;
                _cancellationToken = cancellationToken;
            }

            internal void Reset()
            {
                _buffer = default;
                _syncBuffer = null;
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

                    if (!_owner.TryCompleteWrite(_syncBuffer, _syncRemaining, out int bytesWritten, out errorInfo))
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
                        if (!_owner.TryCompleteWrite(bufPtr, span.Length, out int bytesWritten, out errorInfo))
                        {
                            _buffer = _buffer.Slice(bytesWritten);
                            return false;
                        }
                    }
                }

                if (errorInfo.Error != Interop.Error.SUCCESS)
                {
                    Exception = Interop.GetIOException(errorInfo);
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

        private sealed class WaitReceiveThresholdOperation : UnixHandleAsyncContext.Operation
        {
            private readonly SafeSerialDeviceHandle _owner;
            private SerialStream? _serialStream;
            internal int BytesAvailable;
            internal ReceiveThresholdResult ThresholdResult;
            internal bool HasCompleted;

            internal WaitReceiveThresholdOperation(SafeSerialDeviceHandle owner)
                => _owner = owner;

            internal void Init(SerialStream serialStream)
            {
                _serialStream = serialStream;
            }

            internal void Reset()
            {
                _serialStream = null;
            }

            protected override bool TryCompleteOperation(SafeHandle handle)
            {
                SerialStream? serialStream = _serialStream;
                if (serialStream == null)
                {
                    return true;
                }

                HasCompleted = _owner.TryCompleteWaitReceiveThreshold(serialStream.ReceivedBytesThreshold, out BytesAvailable, out ThresholdResult);

                // We'll enqueue at the end of the read queue in OnCompleted if we haven't completed.
                // This enables other operations to read data instead of getting blocked on this operation waiting to reach the threshold.
                return true;
            }

            internal void CompleteOperation(OnCompletedResult result)
                => OnCompleted(result);

            protected override void OnCompleted(OnCompletedResult result)
            {
                if (result == OnCompletedResult.Completed)
                {
                    SerialStream? serialStream = _serialStream;
                    int bytesAvailable = BytesAvailable;
                    ReceiveThresholdResult thresholdResult = ThresholdResult;
                    _owner.ReturnWaitReceiveThresholdOperation(this);
                    if (HasCompleted)
                    {
                        serialStream?.OnReceiveThreshold(bytesAvailable, thresholdResult);
                    }
                    else
                    {
                        // We haven't met the threshold, wait again.
                        _owner.WaitForReceiveThreshold(serialStream);
                    }
                }
            }
        }

        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        private static extern UnixHandleAsyncContext CreateAsyncContext(SafeHandle handle);
    }
}
