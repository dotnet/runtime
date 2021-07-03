// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
            private enum Operation : byte
            {
                None,
                Read,
                Write,
                ReadScatter,
                WriteGather
            }

            private ManualResetValueTaskSourceCore<long> _source;
            private readonly SafeFileHandle _fileHandle;
            private Operation _operation = Operation.None;

            // These fields store the parameters for the operation.
            // The first two are common for all kinds of operations.
            private long _fileOffset;
            private CancellationToken _cancellationToken;
            // Used by simple reads and writes. Will be unsafely cast to a memory when performing a read.
            private ReadOnlyMemory<byte> _singleSegment;
            // Used by vectored reads and writes. Is an IReadOnlyList of either Memory or ReadOnlyMemory of bytes.
            private object? _multiSegmentCollection;

            internal ThreadPoolValueTaskSource(SafeFileHandle fileHandle)
            {
                _fileHandle = fileHandle;
                _source.RunContinuationsAsynchronously = true;
            }

            private long GetResultAndRelease(short token)
            {
                try
                {
                    return _source.GetResult(token);
                } finally
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

            void IThreadPoolWorkItem.Execute()
            {
                Debug.Assert(_operation >= Operation.Read && _operation <= Operation.WriteGather);

                long result = 0;
                try {
                    bool notifyWhenUnblocked = false;
                    try
                    {
                        // This is the operation's last chance to be cancelled.
                        if (_cancellationToken.IsCancellationRequested)
                        {
                            _source.SetException(new OperationCanceledException(_cancellationToken));
                            return;
                        }

                        notifyWhenUnblocked = ThreadPool.NotifyThreadBlocked();
                        switch (_operation)
                        {
                            case Operation.Read:
                                Memory<byte> writableSingleSegment = Unsafe.As<ReadOnlyMemory<byte>, Memory<byte>>(ref _singleSegment);
                                result = RandomAccess.ReadAtOffset(_fileHandle, writableSingleSegment.Span, _fileOffset);
                                break;
                            case Operation.Write:
                                result = RandomAccess.WriteAtOffset(_fileHandle, _singleSegment.Span, _fileOffset);
                                break;
                            case Operation.ReadScatter:
                                Debug.Assert(_multiSegmentCollection != null && _multiSegmentCollection is IReadOnlyList<Memory<byte>>);
                                result = RandomAccess.ReadScatterAtOffset(_fileHandle, (IReadOnlyList<Memory<byte>>) _multiSegmentCollection, _fileOffset);
                                break;
                            case Operation.WriteGather:
                                Debug.Assert(_multiSegmentCollection != null && _multiSegmentCollection is IReadOnlyList<ReadOnlyMemory<byte>>);
                                result = RandomAccess.WriteGatherAtOffset(_fileHandle, (IReadOnlyList<ReadOnlyMemory<byte>>)_multiSegmentCollection, _fileOffset);
                                break;
                        }
                    } finally
                    {
                        if (notifyWhenUnblocked)
                            ThreadPool.NotifyThreadUnblocked();
                        _operation = Operation.None;
                        _fileOffset = 0;
                        _cancellationToken = default;
                        _singleSegment = default;
                        _multiSegmentCollection = null;
                    }
                } catch (Exception e)
                {
                    _source.SetException(e);
                }
                _source.SetResult(result);
            }

            public ValueTask<int> QueueRead(Memory<byte> buffer, long fileOffset, CancellationToken cancellationToken)
            {
                Debug.Assert(_operation == Operation.None, "An operation was queued before the previous one's completion.");

                _operation = Operation.Read;
                _singleSegment = buffer;
                _fileOffset = fileOffset;
                _cancellationToken = cancellationToken;
                ThreadPool.UnsafeQueueUserWorkItem(this, false);

                return new ValueTask<int>(this, _source.Version);
            }

            public ValueTask<int> QueueWrite(ReadOnlyMemory<byte> buffer, long fileOffset, CancellationToken cancellationToken)
            {
                Debug.Assert(_operation == Operation.None, "An operation was queued before the previous one's completion.");

                _operation = Operation.Write;
                _singleSegment = buffer;
                _fileOffset = fileOffset;
                _cancellationToken = cancellationToken;
                ThreadPool.UnsafeQueueUserWorkItem(this, false);

                return new ValueTask<int>(this, _source.Version);
            }

            public ValueTask<long> QueueReadScatter(IReadOnlyList<Memory<byte>> buffers, long fileOffset, CancellationToken cancellationToken)
            {
                Debug.Assert(_operation == Operation.None, "An operation was queued before the previous one's completion.");

                _operation = Operation.ReadScatter;
                _multiSegmentCollection = buffers;
                _fileOffset = fileOffset;
                _cancellationToken = cancellationToken;
                ThreadPool.UnsafeQueueUserWorkItem(this, false);

                return new ValueTask<long>(this, _source.Version);
            }

            public ValueTask<long> QueueWriteGather(IReadOnlyList<ReadOnlyMemory<byte>> buffers, long fileOffset, CancellationToken cancellationToken)
            {
                Debug.Assert(_operation == Operation.None, "An operation was queued before the previous one's completion.");

                _operation = Operation.WriteGather;
                _multiSegmentCollection = buffers;
                _fileOffset = fileOffset;
                _cancellationToken = cancellationToken;
                ThreadPool.UnsafeQueueUserWorkItem(this, false);

                return new ValueTask<long>(this, _source.Version);
            }
        }
    }
}
