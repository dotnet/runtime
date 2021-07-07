// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeFileHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private ThreadPoolValueTaskSource? _reusableThreadPoolValueTaskSource; // reusable ThreadPoolValueTaskSource that is currently NOT being used

        // Rent the reusable ThreadPoolValueTaskSource, or create a new one to use if we couldn't get one (which
        // should only happen on first use or if the SafeFileHandle is being used concurrently).
        internal ThreadPoolValueTaskSource GetThreadPoolValueTaskSource() =>
            Interlocked.Exchange(ref _reusableThreadPoolValueTaskSource, null) ?? new ThreadPoolValueTaskSource(this);

        private void TryToReuse(ThreadPoolValueTaskSource source)
        {
            Interlocked.CompareExchange(ref _reusableThreadPoolValueTaskSource, source, null);
        }

        /// <summary>
        /// A reusable <see cref="IValueTaskSource"/> implementation that
        /// queues asynchronous <see cref="RandomAccess"/> operations to
        /// be completed synchronously on the thread pool.
        /// </summary>
        internal sealed class ThreadPoolValueTaskSource : IThreadPoolWorkItem, IValueTaskSource<int>, IValueTaskSource<long>
        {
            private readonly SafeFileHandle _fileHandle;
            private ManualResetValueTaskSourceCore<long> _source;
            private Operation _operation = Operation.None;
            private ExecutionContext? _context;

            // These fields store the parameters for the operation.
            // The first two are common for all kinds of operations.
            private long _fileOffset;
            private CancellationToken _cancellationToken;
            // Used by simple reads and writes. Will be unsafely cast to a memory when performing a read.
            private ReadOnlyMemory<byte> _singleSegment;
            private IReadOnlyList<Memory<byte>>? _readScatterBuffers;
            private IReadOnlyList<ReadOnlyMemory<byte>>? _writeGatherBuffers;

            internal ThreadPoolValueTaskSource(SafeFileHandle fileHandle)
            {
                _fileHandle = fileHandle;
            }

            [Conditional("DEBUG")]
            private void ValidateInvariants()
            {
                Operation op = _operation;
                Debug.Assert(op == Operation.None, $"An operation was queued before the previous {op}'s completion.");
            }

            private long GetResultAndRelease(short token)
            {
                try
                {
                    return _source.GetResult(token);
                }
                finally
                {
                    _source.Reset();
                    _fileHandle.TryToReuse(this);
                }
            }

            public ValueTaskSourceStatus GetStatus(short token) => _source.GetStatus(token);
            public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) =>
                _source.OnCompleted(continuation, state, token, flags);
            int IValueTaskSource<int>.GetResult(short token) => (int) GetResultAndRelease(token);
            long IValueTaskSource<long>.GetResult(short token) => GetResultAndRelease(token);

            private void ExecuteInternal()
            {
                Debug.Assert(_operation >= Operation.Read && _operation <= Operation.WriteGather);

                long result = 0;
                Exception? exception = null;
                try
                {
                    // This is the operation's last chance to be canceled.
                    if (_cancellationToken.IsCancellationRequested)
                    {
                        exception = new OperationCanceledException(_cancellationToken);
                    }
                    else
                    {
                        switch (_operation)
                        {
                            case Operation.Read:
                                Memory<byte> writableSingleSegment = MemoryMarshal.AsMemory(_singleSegment);
                                result = RandomAccess.ReadAtOffset(_fileHandle, writableSingleSegment.Span, _fileOffset);
                                break;
                            case Operation.Write:
                                result = RandomAccess.WriteAtOffset(_fileHandle, _singleSegment.Span, _fileOffset);
                                break;
                            case Operation.ReadScatter:
                                Debug.Assert(_readScatterBuffers != null);
                                result = RandomAccess.ReadScatterAtOffset(_fileHandle, _readScatterBuffers, _fileOffset);
                                break;
                            case Operation.WriteGather:
                                Debug.Assert(_writeGatherBuffers != null);
                                result = RandomAccess.WriteGatherAtOffset(_fileHandle, _writeGatherBuffers, _fileOffset);
                                break;
                        }
                    }
                }
                catch (Exception e)
                {
                    exception = e;
                }
                finally
                {
                    _operation = Operation.None;
                    _context = null;
                    _cancellationToken = default;
                    _singleSegment = default;
                    _readScatterBuffers = null;
                    _writeGatherBuffers = null;
                }

                if (exception == null)
                {
                    _source.SetResult(result);
                }
                else
                {
                    _source.SetException(exception);
                }
            }

            void IThreadPoolWorkItem.Execute()
            {
                if (_context == null || _context.IsDefault)
                {
                    ExecuteInternal();
                }
                else
                {
                    ExecutionContext.RunForThreadPoolUnsafe(_context, static x => x.ExecuteInternal(), this);
                }
            }

            private void QueueToThreadPool()
            {
                _context = ExecutionContext.Capture();
                ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: true);
            }

            public ValueTask<int> QueueRead(Memory<byte> buffer, long fileOffset, CancellationToken cancellationToken)
            {
                ValidateInvariants();

                _operation = Operation.Read;
                _singleSegment = buffer;
                _fileOffset = fileOffset;
                _cancellationToken = cancellationToken;
                QueueToThreadPool();

                return new ValueTask<int>(this, _source.Version);
            }

            public ValueTask<int> QueueWrite(ReadOnlyMemory<byte> buffer, long fileOffset, CancellationToken cancellationToken)
            {
                ValidateInvariants();

                _operation = Operation.Write;
                _singleSegment = buffer;
                _fileOffset = fileOffset;
                _cancellationToken = cancellationToken;
                QueueToThreadPool();

                return new ValueTask<int>(this, _source.Version);
            }

            public ValueTask<long> QueueReadScatter(IReadOnlyList<Memory<byte>> buffers, long fileOffset, CancellationToken cancellationToken)
            {
                ValidateInvariants();

                _operation = Operation.ReadScatter;
                _readScatterBuffers = buffers;
                _fileOffset = fileOffset;
                _cancellationToken = cancellationToken;
                QueueToThreadPool();

                return new ValueTask<long>(this, _source.Version);
            }

            public ValueTask<long> QueueWriteGather(IReadOnlyList<ReadOnlyMemory<byte>> buffers, long fileOffset, CancellationToken cancellationToken)
            {
                ValidateInvariants();

                _operation = Operation.WriteGather;
                _writeGatherBuffers = buffers;
                _fileOffset = fileOffset;
                _cancellationToken = cancellationToken;
                QueueToThreadPool();

                return new ValueTask<long>(this, _source.Version);
            }

            private enum Operation : byte
            {
                None,
                Read,
                Write,
                ReadScatter,
                WriteGather
            }
        }
    }
}
