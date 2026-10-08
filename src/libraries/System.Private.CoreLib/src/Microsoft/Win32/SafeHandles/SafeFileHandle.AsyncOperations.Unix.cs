// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Strategies;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks.Sources;
using OnCompletedResult = System.Threading.UnixHandleAsyncContext.OnCompletedResult;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeFileHandle
    {
        private sealed class ReadOperation : UnixHandleAsyncContext.Operation, IValueTaskSource<int>, IValueTaskSource<long>
        {
            private readonly SafeFileHandle _owner;
            private ManualResetValueTaskSourceCore<long> _mrvtsc;
            private Memory<byte> _buffer;
            private IReadOnlyList<Memory<byte>>? _buffers;
            private long _offset;
            private bool _runOnThreadPool;
            private ExecutionContext? _executionContext;
            private CancellationToken _cancellationToken;
            private OSFileStreamStrategy? _strategy;

            internal long ReadResult;
            internal Exception? Exception;

            internal ReadOperation(SafeFileHandle owner)
                => _owner = owner;

            internal short Version
                => _mrvtsc.Version;

            internal void Init(long offset, Memory<byte> buffer, CancellationToken cancellationToken, OSFileStreamStrategy? strategy = null)
            {
                _offset = offset;
                _buffer = buffer;
                _cancellationToken = cancellationToken;
                _strategy = strategy;
            }

            internal void Init(long offset, IReadOnlyList<Memory<byte>> buffers)
            {
                _offset = offset;
                _buffers = buffers;
            }

            internal void Init(long offset, IReadOnlyList<Memory<byte>> buffers, CancellationToken cancellationToken)
            {
                _offset = offset;
                _buffers = buffers;
                _cancellationToken = cancellationToken;
            }

            internal void QueueToThreadPool()
            {
                _runOnThreadPool = true;
                _executionContext = ExecutionContext.Capture();
                ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: true);
            }

            private void ExecuteOnThreadPool()
            {
                try
                {
                    if (_cancellationToken.IsCancellationRequested)
                    {
                        OnCompleted(OnCompletedResult.Canceled);
                        return;
                    }

                    bool completed = TryCompleteOperation(_owner);
                    Debug.Assert(completed);
                    OnCompleted(OnCompletedResult.Completed);
                }
                catch (Exception e)
                {
                    Exception = e;
                    OnCompleted(e is ObjectDisposedException ? OnCompletedResult.Aborted : OnCompletedResult.Completed);
                }
            }

            protected override void ExecuteThreadPoolWorkItem()
            {
                if (!_runOnThreadPool)
                {
                    base.ExecuteThreadPoolWorkItem();
                    return;
                }

                if (_executionContext == null || _executionContext.IsDefault)
                {
                    ExecuteOnThreadPool();
                }
                else
                {
                    ExecutionContext.RunForThreadPoolUnsafe(_executionContext, static x => x.ExecuteOnThreadPool(), this);
                }
            }

            internal void Reset()
            {
                _buffer = default;
                _buffers = null;
                _offset = 0;
                _runOnThreadPool = false;
                _executionContext = null;
                _cancellationToken = default;
                _strategy = null;
                Exception = null;
                _mrvtsc.Reset();
            }

            protected internal override bool TryCompleteOperation(SafeHandle handle)
            {
                long readResult;
                Interop.ErrorInfo errorInfo;

                if (_buffers != null)
                {
                    if (!_owner.TryCompleteReadAt(_offset, _buffers, out readResult, out errorInfo))
                    {
                        return false;
                    }
                }
                else
                {
                    if (_buffer.Length == 0 && _owner.SupportsNonBlocking)
                    {
                        ReadResult = 0;
                        return true;
                    }

                    if (!_owner.TryCompleteReadAt(_offset, _buffer.Span, out int intReadResult, out errorInfo, _strategy))
                    {
                        return false;
                    }
                    readResult = intReadResult;
                }

                ReadResult = readResult;
                if (readResult == -1)
                {
                    Exception = Interop.GetExceptionForIoErrno(errorInfo, _owner.Path);
                }
                return true;
            }

            protected internal override void OnCompleted(OnCompletedResult result)
            {
                if (result == OnCompletedResult.Completed)
                {
                    if (Exception != null)
                    {
                        _mrvtsc.SetException(Exception);
                    }
                    else
                    {
                        _mrvtsc.SetResult(ReadResult);
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

            private long GetResultAndPool(short token)
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

            ValueTaskSourceStatus IValueTaskSource<int>.GetStatus(short token)
                => _mrvtsc.GetStatus(token);

            void IValueTaskSource<int>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
                => _mrvtsc.OnCompleted(continuation, state, token, flags);

            int IValueTaskSource<int>.GetResult(short token)
                => (int)GetResultAndPool(token);

            ValueTaskSourceStatus IValueTaskSource<long>.GetStatus(short token)
                => _mrvtsc.GetStatus(token);

            void IValueTaskSource<long>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
                => _mrvtsc.OnCompleted(continuation, state, token, flags);

            long IValueTaskSource<long>.GetResult(short token)
                => GetResultAndPool(token);
        }

        private sealed class WriteOperation : UnixHandleAsyncContext.Operation, IValueTaskSource
        {
            private readonly SafeFileHandle _owner;
            private ManualResetValueTaskSourceCore<bool> _mrvtsc;
            private ReadOnlyMemory<byte> _buffer;
            private IReadOnlyList<ReadOnlyMemory<byte>>? _buffers;
            private long _offset;
            private int _bufferIndex;
            private int _bufferOffset;
            private bool _runOnThreadPool;
            private ExecutionContext? _executionContext;
            private CancellationToken _cancellationToken;
            private OSFileStreamStrategy? _strategy;

            internal Exception? Exception;

            internal WriteOperation(SafeFileHandle owner)
                => _owner = owner;

            internal short Version
                => _mrvtsc.Version;

            internal void Init(long offset, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken, OSFileStreamStrategy? strategy = null)
            {
                _offset = offset;
                _buffer = buffer;
                _cancellationToken = cancellationToken;
                _strategy = strategy;
            }

            internal void Init(long offset, IReadOnlyList<ReadOnlyMemory<byte>> buffers, int bufferIndex, int bufferOffset)
            {
                _offset = offset;
                _buffers = buffers;
                _bufferIndex = bufferIndex;
                _bufferOffset = bufferOffset;
            }

            internal void Init(long offset, IReadOnlyList<ReadOnlyMemory<byte>> buffers, int bufferIndex, int bufferOffset, CancellationToken cancellationToken)
            {
                _offset = offset;
                _buffers = buffers;
                _bufferIndex = bufferIndex;
                _bufferOffset = bufferOffset;
                _cancellationToken = cancellationToken;
            }

            internal void QueueToThreadPool()
            {
                _runOnThreadPool = true;
                _executionContext = ExecutionContext.Capture();
                ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: true);
            }

            private void ExecuteOnThreadPool()
            {
                try
                {
                    if (_cancellationToken.IsCancellationRequested)
                    {
                        OnCompleted(OnCompletedResult.Canceled);
                        return;
                    }

                    while (!TryCompleteOperation(_owner))
                    { }
                    OnCompleted(OnCompletedResult.Completed);
                }
                catch (Exception e)
                {
                    Exception = e;
                    OnCompleted(e is ObjectDisposedException ? OnCompletedResult.Aborted : OnCompletedResult.Completed);
                }
            }

            protected override void ExecuteThreadPoolWorkItem()
            {
                if (!_runOnThreadPool)
                {
                    base.ExecuteThreadPoolWorkItem();
                    return;
                }

                if (_executionContext == null || _executionContext.IsDefault)
                {
                    ExecuteOnThreadPool();
                }
                else
                {
                    ExecutionContext.RunForThreadPoolUnsafe(_executionContext, static x => x.ExecuteOnThreadPool(), this);
                }
            }

            internal void Reset()
            {
                _buffer = default;
                _buffers = null;
                _offset = 0;
                _bufferIndex = 0;
                _bufferOffset = 0;
                _runOnThreadPool = false;
                _executionContext = null;
                _cancellationToken = default;
                _strategy = null;
                Exception = null;
                _mrvtsc.Reset();
            }

            protected internal override bool TryCompleteOperation(SafeHandle handle)
            {
                Interop.ErrorInfo errorInfo;

                if (_buffers != null)
                {
                    if (_owner.TryCompleteWriteAt(ref _offset, _buffers, ref _bufferIndex, ref _bufferOffset, out errorInfo))
                    {
                        if (errorInfo.Error != Interop.Error.SUCCESS)
                        {
                            Exception = Interop.GetExceptionForIoErrno(errorInfo, _owner.Path);
                        }
                        return true;
                    }
                    Debug.Assert(!_runOnThreadPool, "ThreadPool only used with non-blocking");
                    return false;
                }

                Debug.Assert(!_buffer.IsEmpty);

                if (_owner.TryCompleteWriteAt(_offset, _buffer.Span, out int bytesWritten, out errorInfo, _strategy))
                {
                    if (errorInfo.Error != Interop.Error.SUCCESS)
                    {
                        Exception = Interop.GetExceptionForIoErrno(errorInfo, _owner.Path);
                    }
                    return true;
                }

                _buffer = _buffer.Slice(bytesWritten);
                _offset += bytesWritten;

                Debug.Assert(!_runOnThreadPool, "ThreadPool only used with non-blocking");
                return false;
            }

            protected internal override void OnCompleted(OnCompletedResult result)
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
    }
}
