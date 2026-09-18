// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Strategies;
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

        /// <summary>
        /// A reusable <see cref="IValueTaskSource"/> implementation that
        /// queues asynchronous <see cref="RandomAccess"/> operations to
        /// be completed synchronously on the thread pool, or (on Linux, when
        /// enabled and available) asynchronously via io_uring.
        /// </summary>
        internal sealed class ThreadPoolValueTaskSource : IThreadPoolWorkItem, IValueTaskSource<int>, IValueTaskSource<long>, IValueTaskSource, PortableThreadPool.IIoUringOperation
        {
            private readonly SafeFileHandle _fileHandle;
            private ManualResetValueTaskSourceCore<long> _source;
            private Operation _operation = Operation.None;
            private ExecutionContext? _context;
            private OSFileStreamStrategy? _strategy;

            // These fields store the parameters for the operation.
            // The first two are common for all kinds of operations.
            private long _fileOffset;
            private CancellationToken _cancellationToken;
            // Used by simple reads and writes. Will be unsafely cast to a memory when performing a read.
            // For writes completed (or partially completed and retried) via io_uring, this is re-sliced
            // to represent the remaining, not-yet-written data.
            private ReadOnlyMemory<byte> _singleSegment;
            private IReadOnlyList<Memory<byte>>? _readScatterBuffers;
            private IReadOnlyList<ReadOnlyMemory<byte>>? _writeGatherBuffers;

            // io_uring completion state. When _completedViaIoUring is true, ExecuteInternal finalizes
            // the operation using _ioUringResult instead of performing a blocking syscall.
            private bool _completedViaIoUring;
            private int _ioUringResult;

            // io_uring in-flight pinning/ref-counting state. These are populated only while an io_uring
            // submission for this instance is outstanding, and are always fully cleaned up (pins
            // disposed, SafeHandle ref released) before the operation is considered complete/reusable.
            private bool _fileHandleRefAdded;
            private MemoryHandle _singleSegmentPin;
            private MemoryHandle[]? _vectorPins;
            private Interop.Sys.IOVector[]? _vectors;
            private GCHandle _vectorsHandle;
            // For WriteGather partial-write retries: index of the first not-yet-fully-written vector,
            // and the number of bytes still left to write across the remaining vectors.
            private int _vectorsOffset;
            private long _remainingBytesToWrite;

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

            public ValueTaskSourceStatus GetStatus(short token) =>
                _source.GetStatus(token);

            public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) =>
                _source.OnCompleted(continuation, state, token, flags);

            void IValueTaskSource.GetResult(short token) => GetResult(token);
            int IValueTaskSource<int>.GetResult(short token) => (int)GetResult(token);
            public long GetResult(short token)
            {
                try
                {
                    return _source.GetResult(token);
                }
                finally
                {
                    _source.Reset();
                    Volatile.Write(ref _fileHandle._reusableThreadPoolValueTaskSource, this);
                }
            }

            private void ExecuteInternal()
            {
                Debug.Assert(_operation >= Operation.Read && _operation <= Operation.WriteGather);

                long result = 0;
                Exception? exception = null;
                try
                {
                    if (_completedViaIoUring)
                    {
                        // The kernel already read from / wrote to the pinned buffer(s) directly; release
                        // the pins/ref now that the operation has fully completed (successfully or not).
                        ReleaseIoUringState();

                        if (_ioUringResult < 0)
                        {
                            exception = Interop.GetExceptionForIoErrno(new Interop.ErrorInfo(-_ioUringResult), _fileHandle.Path);
                        }
                        else
                        {
                            result = _ioUringResult;
                        }
                    }
                    // This is the operation's last chance to be canceled.
                    else if (_cancellationToken.IsCancellationRequested)
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
                                RandomAccess.WriteAtOffset(_fileHandle, _singleSegment.Span, _fileOffset);
                                break;
                            case Operation.ReadScatter:
                                Debug.Assert(_readScatterBuffers != null);
                                result = RandomAccess.ReadScatterAtOffset(_fileHandle, _readScatterBuffers, _fileOffset);
                                break;
                            case Operation.WriteGather:
                                Debug.Assert(_writeGatherBuffers != null);
                                RandomAccess.WriteGatherAtOffset(_fileHandle, _writeGatherBuffers, _fileOffset);
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
                    if (_strategy is not null)
                    {
                        // WriteAtOffset returns void, so we need to fix position only in case of an exception
                        if (exception is not null)
                        {
                            _strategy.OnIncompleteOperation(_singleSegment.Length, 0);
                        }
                        else if (_operation == Operation.Read && result != _singleSegment.Length)
                        {
                            _strategy.OnIncompleteOperation(_singleSegment.Length, (int)result);
                        }
                    }

                    _operation = Operation.None;
                    _context = null;
                    _strategy = null;
                    _cancellationToken = default;
                    _singleSegment = default;
                    _readScatterBuffers = null;
                    _writeGatherBuffers = null;
                    _completedViaIoUring = false;
                    _ioUringResult = 0;
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

            /// <summary>
            /// Called by <see cref="PortableThreadPool.IoUringThreadPool"/>'s driver thread when this
            /// operation's io_uring completion is available. Must only perform minimal, non-blocking
            /// bookkeeping (per the <see cref="PortableThreadPool.IIoUringOperation"/> contract) and must
            /// not run continuations inline or queue them itself - the driver batches the returned work
            /// item together with others drained in the same pass.
            /// </summary>
            IThreadPoolWorkItem? PortableThreadPool.IIoUringOperation.CompleteFromIoUring(int result)
            {
                if (result >= 0 && (_operation == Operation.Write || _operation == Operation.WriteGather)
                    && TryContinuePartialWrite(result, out IThreadPoolWorkItem? fallbackWorkItem))
                {
                    // The write completed for fewer bytes than requested. Either we've already
                    // resubmitted an io_uring request for the remainder (fallbackWorkItem is null, this
                    // instance is still in flight, nothing to queue yet), or resubmission itself failed
                    // and fallbackWorkItem is this instance, to be finalized via the ordinary blocking
                    // path instead.
                    return fallbackWorkItem;
                }

                _ioUringResult = result;
                _completedViaIoUring = true;
                return this;
            }

            /// <summary>
            /// If <paramref name="bytesWritten"/> represents a partial write (fewer bytes than were
            /// requested by the most recent submission), advances the write state and attempts to
            /// resubmit an io_uring request for the remainder. Returns true if a resubmission was made
            /// (regardless of whether it itself succeeded). On success, <paramref name="fallbackWorkItem"/>
            /// is <see langword="null"/> (this instance is still in flight). On resubmission failure,
            /// <paramref name="fallbackWorkItem"/> is <see langword="this"/>, meaning the caller should
            /// still queue it (falling back to completing the remainder via the ordinary blocking path).
            /// Returns false if the write was already complete (or this isn't a write operation), in which
            /// case <paramref name="fallbackWorkItem"/> is <see langword="null"/> and the caller should
            /// finalize the operation as usual.
            /// </summary>
            private bool TryContinuePartialWrite(int bytesWritten, out IThreadPoolWorkItem? fallbackWorkItem)
            {
                fallbackWorkItem = null;

                if (_operation == Operation.Write)
                {
                    if (bytesWritten >= _singleSegment.Length)
                    {
                        return false;
                    }

                    // The old pin is no longer valid once we reslice; TrySubmitWrite re-pins the
                    // remainder. _context was already captured when the operation was originally queued.
                    _singleSegmentPin.Dispose();
                    _singleSegmentPin = default;
                    if (_fileHandleRefAdded)
                    {
                        _fileHandle.DangerousRelease();
                        _fileHandleRefAdded = false;
                    }

                    _singleSegment = _singleSegment.Slice(bytesWritten);
                    _fileOffset += bytesWritten;

                    if (!TrySubmitWrite())
                    {
                        // Fall back to the ordinary blocking path for just the remainder: _singleSegment
                        // and _fileOffset already reflect only the not-yet-written data.
                        fallbackWorkItem = this;
                    }

                    return true;
                }

                if (_operation == Operation.WriteGather)
                {
                    _remainingBytesToWrite -= bytesWritten;
                    if (_remainingBytesToWrite <= 0)
                    {
                        return false;
                    }

                    _fileOffset += bytesWritten;
                    AdvanceVectorsAfterPartialWrite(bytesWritten);

                    // Release just the file-handle ref added for the previous submission; the vector
                    // pins/array remain valid and are reused (with an adjusted window) for the resubmit.
                    if (_fileHandleRefAdded)
                    {
                        _fileHandle.DangerousRelease();
                        _fileHandleRefAdded = false;
                    }

                    if (!TrySubmitWriteGatherRemainder())
                    {
                        // Rare: the resubmission itself could not be queued (e.g., the submission queue
                        // is momentarily full). Fall back to the ordinary blocking path, but only for the
                        // remaining (not-yet-written) data.
                        SwapToRemainingWriteGatherBuffers();
                        ReleaseIoUringState();
                        fallbackWorkItem = this;
                    }

                    return true;
                }

                return false;
            }

            /// <summary>
            /// Replaces <see cref="_writeGatherBuffers"/> with just the not-yet-written remainder (based
            /// on <see cref="_vectorsOffset"/> and the current, possibly-adjusted, first remaining
            /// vector's length), so that the ordinary blocking <see cref="RandomAccess.WriteGatherAtOffset"/>
            /// fallback path writes only what's left, not the original buffers from the start.
            /// </summary>
            private void SwapToRemainingWriteGatherBuffers()
            {
                Debug.Assert(_writeGatherBuffers != null && _vectors != null);
                IReadOnlyList<ReadOnlyMemory<byte>> original = _writeGatherBuffers;
                Interop.Sys.IOVector[] vectors = _vectors;
                int offset = _vectorsOffset;
                int remainingCount = original.Count - offset;

                var remaining = new ReadOnlyMemory<byte>[remainingCount];
                for (int i = 0; i < remainingCount; i++)
                {
                    int srcIndex = offset + i;
                    ReadOnlyMemory<byte> buffer = original[srcIndex];
                    if (i == 0)
                    {
                        int consumed = buffer.Length - (int)vectors[srcIndex].Count;
                        buffer = buffer.Slice(consumed);
                    }
                    remaining[i] = buffer;
                }

                _writeGatherBuffers = remaining;
            }

            /// <summary>
            /// Mirrors the bookkeeping in the blocking <see cref="RandomAccess.WriteGatherAtOffset"/>
            /// implementation: advances <see cref="_vectorsOffset"/> past any vectors that were fully
            /// written, and adjusts the base/length of the first partially-written vector in place.
            /// </summary>
            private void AdvanceVectorsAfterPartialWrite(int bytesWritten)
            {
                Debug.Assert(_vectors != null);
                Interop.Sys.IOVector[] vectors = _vectors;
                int count = vectors.Length;

                while (_vectorsOffset < count && bytesWritten > 0)
                {
                    int n = (int)vectors[_vectorsOffset].Count;
                    if (n <= bytesWritten)
                    {
                        bytesWritten -= n;
                        _vectorsOffset++;
                    }
                    else
                    {
                        unsafe
                        {
                            Interop.Sys.IOVector current = vectors[_vectorsOffset];
                            vectors[_vectorsOffset] = new Interop.Sys.IOVector
                            {
                                Base = current.Base + bytesWritten,
                                Count = current.Count - (UIntPtr)bytesWritten
                            };
                        }
                        break;
                    }
                }
            }

            private void QueueToThreadPool()
            {
                _context = ExecutionContext.Capture();
                ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: true);
            }

            public ValueTask<int> QueueRead(Memory<byte> buffer, long fileOffset, CancellationToken cancellationToken, OSFileStreamStrategy? strategy)
            {
                ValidateInvariants();

                _operation = Operation.Read;
                _singleSegment = buffer;
                _fileOffset = fileOffset;
                _cancellationToken = cancellationToken;
                _strategy = strategy;
                _context = ExecutionContext.Capture();

                if (!TrySubmitRead())
                {
                    ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: true);
                }

                return new ValueTask<int>(this, _source.Version);
            }

            public ValueTask QueueWrite(ReadOnlyMemory<byte> buffer, long fileOffset, CancellationToken cancellationToken, OSFileStreamStrategy? strategy)
            {
                ValidateInvariants();

                _operation = Operation.Write;
                _singleSegment = buffer;
                _fileOffset = fileOffset;
                _cancellationToken = cancellationToken;
                _strategy = strategy;
                _context = ExecutionContext.Capture();

                if (!TrySubmitWrite())
                {
                    ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: true);
                }

                return new ValueTask(this, _source.Version);
            }

            public ValueTask<long> QueueReadScatter(IReadOnlyList<Memory<byte>> buffers, long fileOffset, CancellationToken cancellationToken)
            {
                ValidateInvariants();

                _operation = Operation.ReadScatter;
                _readScatterBuffers = buffers;
                _fileOffset = fileOffset;
                _cancellationToken = cancellationToken;
                _context = ExecutionContext.Capture();

                if (!TrySubmitReadScatter())
                {
                    ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: true);
                }

                return new ValueTask<long>(this, _source.Version);
            }

            public ValueTask QueueWriteGather(IReadOnlyList<ReadOnlyMemory<byte>> buffers, long fileOffset, CancellationToken cancellationToken)
            {
                ValidateInvariants();

                _operation = Operation.WriteGather;
                _writeGatherBuffers = buffers;
                _fileOffset = fileOffset;
                _cancellationToken = cancellationToken;
                _context = ExecutionContext.Capture();

                if (!TrySubmitWriteGather())
                {
                    ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: true);
                }

                return new ValueTask(this, _source.Version);
            }

            private unsafe bool TrySubmitRead()
            {
                if (!PortableThreadPool.IoUringThreadPool.IsEnabled)
                {
                    return false;
                }

                bool refAdded = false;
                try
                {
                    _fileHandle.DangerousAddRef(ref refAdded);
                    _singleSegmentPin = _singleSegment.Pin();

                    Interop.Sys.IoRingRequest request = default;
                    request.OpCode = Interop.Sys.IoRingOp.Read;
                    request.Fd = _fileHandle.DangerousGetHandle();
                    request.Offset = _fileHandle.SupportsRandomAccess ? _fileOffset : -1;
                    request.Buffer = (byte*)_singleSegmentPin.Pointer;
                    request.BufferLength = _singleSegment.Length;

                    if (PortableThreadPool.IoUringThreadPool.TrySubmit(this, in request))
                    {
                        _fileHandleRefAdded = refAdded;
                        return true;
                    }
                }
                catch
                {
                    // Fall through to cleanup and report failure to submit; caller falls back.
                }

                _singleSegmentPin.Dispose();
                _singleSegmentPin = default;
                if (refAdded)
                {
                    _fileHandle.DangerousRelease();
                }
                return false;
            }

            private unsafe bool TrySubmitWrite()
            {
                if (!PortableThreadPool.IoUringThreadPool.IsEnabled)
                {
                    return false;
                }

                bool refAdded = false;
                try
                {
                    _fileHandle.DangerousAddRef(ref refAdded);
                    _singleSegmentPin = _singleSegment.Pin();

                    Interop.Sys.IoRingRequest request = default;
                    request.OpCode = Interop.Sys.IoRingOp.Write;
                    request.Fd = _fileHandle.DangerousGetHandle();
                    request.Offset = _fileHandle.SupportsRandomAccess ? _fileOffset : -1;
                    request.Buffer = (byte*)_singleSegmentPin.Pointer;
                    request.BufferLength = _singleSegment.Length;

                    if (PortableThreadPool.IoUringThreadPool.TrySubmit(this, in request))
                    {
                        _fileHandleRefAdded = refAdded;
                        return true;
                    }
                }
                catch
                {
                }

                _singleSegmentPin.Dispose();
                _singleSegmentPin = default;
                if (refAdded)
                {
                    _fileHandle.DangerousRelease();
                }
                return false;
            }

            private unsafe bool TrySubmitReadScatter()
            {
                if (!PortableThreadPool.IoUringThreadPool.IsEnabled)
                {
                    return false;
                }

                Debug.Assert(_readScatterBuffers != null);
                int count = _readScatterBuffers.Count;
                if (count == 0)
                {
                    return false;
                }

                bool refAdded = false;
                MemoryHandle[] pins = new MemoryHandle[count];
                Interop.Sys.IOVector[] vectors = new Interop.Sys.IOVector[count];
                GCHandle vectorsHandle = default;
                int pinned = 0;
                try
                {
                    for (int i = 0; i < count; i++)
                    {
                        Memory<byte> buffer = _readScatterBuffers[i];
                        MemoryHandle pin = buffer.Pin();
                        pins[i] = pin;
                        pinned = i + 1;
                        vectors[i] = new Interop.Sys.IOVector { Base = (byte*)pin.Pointer, Count = (UIntPtr)buffer.Length };
                    }

                    vectorsHandle = GCHandle.Alloc(vectors, GCHandleType.Pinned);
                    _fileHandle.DangerousAddRef(ref refAdded);

                    Interop.Sys.IoRingRequest request = default;
                    request.OpCode = Interop.Sys.IoRingOp.ReadV;
                    request.Fd = _fileHandle.DangerousGetHandle();
                    request.Offset = _fileHandle.SupportsRandomAccess ? _fileOffset : -1;
                    request.Vectors = (Interop.Sys.IOVector*)vectorsHandle.AddrOfPinnedObject();
                    request.VectorCount = count;

                    if (PortableThreadPool.IoUringThreadPool.TrySubmit(this, in request))
                    {
                        _vectorPins = pins;
                        _vectors = vectors;
                        _vectorsHandle = vectorsHandle;
                        _fileHandleRefAdded = refAdded;
                        return true;
                    }
                }
                catch
                {
                }

                if (vectorsHandle.IsAllocated)
                {
                    vectorsHandle.Free();
                }
                for (int i = 0; i < pinned; i++)
                {
                    pins[i].Dispose();
                }
                if (refAdded)
                {
                    _fileHandle.DangerousRelease();
                }
                return false;
            }

            private unsafe bool TrySubmitWriteGather()
            {
                if (!PortableThreadPool.IoUringThreadPool.IsEnabled)
                {
                    return false;
                }

                Debug.Assert(_writeGatherBuffers != null);
                int count = _writeGatherBuffers.Count;
                if (count == 0)
                {
                    return false;
                }

                bool refAdded = false;
                MemoryHandle[] pins = new MemoryHandle[count];
                Interop.Sys.IOVector[] vectors = new Interop.Sys.IOVector[count];
                GCHandle vectorsHandle = default;
                int pinned = 0;
                try
                {
                    long totalBytesToWrite = 0;
                    for (int i = 0; i < count; i++)
                    {
                        ReadOnlyMemory<byte> buffer = _writeGatherBuffers[i];
                        totalBytesToWrite += buffer.Length;

                        MemoryHandle pin = buffer.Pin();
                        pins[i] = pin;
                        pinned = i + 1;
                        vectors[i] = new Interop.Sys.IOVector { Base = (byte*)pin.Pointer, Count = (UIntPtr)buffer.Length };
                    }

                    vectorsHandle = GCHandle.Alloc(vectors, GCHandleType.Pinned);
                    _fileHandle.DangerousAddRef(ref refAdded);

                    Interop.Sys.IoRingRequest request = default;
                    request.OpCode = Interop.Sys.IoRingOp.WriteV;
                    request.Fd = _fileHandle.DangerousGetHandle();
                    request.Offset = _fileHandle.SupportsRandomAccess ? _fileOffset : -1;
                    request.Vectors = (Interop.Sys.IOVector*)vectorsHandle.AddrOfPinnedObject();
                    request.VectorCount = count;

                    if (PortableThreadPool.IoUringThreadPool.TrySubmit(this, in request))
                    {
                        _vectorPins = pins;
                        _vectors = vectors;
                        _vectorsHandle = vectorsHandle;
                        _vectorsOffset = 0;
                        _remainingBytesToWrite = totalBytesToWrite;
                        _fileHandleRefAdded = refAdded;
                        return true;
                    }
                }
                catch
                {
                }

                if (vectorsHandle.IsAllocated)
                {
                    vectorsHandle.Free();
                }
                for (int i = 0; i < pinned; i++)
                {
                    pins[i].Dispose();
                }
                if (refAdded)
                {
                    _fileHandle.DangerousRelease();
                }
                return false;
            }

            /// <summary>
            /// Resubmits the remaining (not-yet-written) portion of a WriteGather operation, using the
            /// already-pinned vector array advanced by <see cref="AdvanceVectorsAfterPartialWrite"/>.
            /// Assumes the previous io_uring state has just been released via <see cref="ReleaseIoUringState"/>.
            /// </summary>
            /// <summary>
            /// Resubmits the remaining (not-yet-written) portion of a WriteGather operation. Reuses the
            /// already-pinned <see cref="_vectorsHandle"/>/<see cref="_vectorPins"/> from the original
            /// submission (never freed/re-pinned between partial-write retries - only the request's
            /// window into the same pinned array changes), advanced by
            /// <see cref="AdvanceVectorsAfterPartialWrite"/>.
            /// </summary>
            private unsafe bool TrySubmitWriteGatherRemainder()
            {
                if (!PortableThreadPool.IoUringThreadPool.IsEnabled)
                {
                    return false;
                }

                Debug.Assert(_vectors != null && _vectorPins != null && _vectorsHandle.IsAllocated);
                int remainingCount = _vectors.Length - _vectorsOffset;

                bool refAdded = false;
                try
                {
                    _fileHandle.DangerousAddRef(ref refAdded);

                    Interop.Sys.IoRingRequest request = default;
                    request.OpCode = Interop.Sys.IoRingOp.WriteV;
                    request.Fd = _fileHandle.DangerousGetHandle();
                    request.Offset = _fileHandle.SupportsRandomAccess ? _fileOffset : -1;
                    request.Vectors = (Interop.Sys.IOVector*)_vectorsHandle.AddrOfPinnedObject() + _vectorsOffset;
                    request.VectorCount = remainingCount;

                    if (PortableThreadPool.IoUringThreadPool.TrySubmit(this, in request))
                    {
                        _fileHandleRefAdded = refAdded;
                        return true;
                    }
                }
                catch
                {
                }

                if (refAdded)
                {
                    _fileHandle.DangerousRelease();
                }
                return false;
            }

            /// <summary>
            /// Releases all pinning/ref-counting state associated with an outstanding (or just-completed)
            /// io_uring submission. Safe to call even if no io_uring submission is currently outstanding.
            /// </summary>
            private void ReleaseIoUringState()
            {
                if (_vectorsHandle.IsAllocated)
                {
                    _vectorsHandle.Free();
                }

                if (_vectorPins != null)
                {
                    foreach (MemoryHandle pin in _vectorPins)
                    {
                        pin.Dispose();
                    }
                    _vectorPins = null;
                }
                _vectors = null;

                _singleSegmentPin.Dispose();
                _singleSegmentPin = default;

                if (_fileHandleRefAdded)
                {
                    _fileHandle.DangerousRelease();
                    _fileHandleRefAdded = false;
                }
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
