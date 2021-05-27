// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    public abstract partial class Stream : MarshalByRefObject, IDisposable, IAsyncDisposable
    {
        public static readonly Stream Null = new NullStream();

        public abstract bool CanRead { get; }
        public abstract bool CanWrite { get; }
        public abstract bool CanSeek { get; }
        public virtual bool CanTimeout => false;

        public abstract long Length { get; }
        public abstract long Position { get; set; }

        public virtual int ReadTimeout
        {
            get => throw new InvalidOperationException(SR.InvalidOperation_TimeoutsNotSupported);
            set => throw new InvalidOperationException(SR.InvalidOperation_TimeoutsNotSupported);
        }

        public virtual int WriteTimeout
        {
            get => throw new InvalidOperationException(SR.InvalidOperation_TimeoutsNotSupported);
            set => throw new InvalidOperationException(SR.InvalidOperation_TimeoutsNotSupported);
        }

        public void CopyTo(Stream destination) => CopyTo(destination, GetCopyBufferSize());

        public virtual void CopyTo(Stream destination, int bufferSize)
        {
            ValidateCopyToArguments(destination, bufferSize);
            if (!CanRead)
            {
                if (CanWrite)
                {
                    ThrowHelper.ThrowNotSupportedException_UnreadableStream();
                }

                ThrowHelper.ThrowObjectDisposedException_StreamClosed(GetType().Name);
            }

            byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            try
            {
                int bytesRead;
                while ((bytesRead = Read(buffer, 0, buffer.Length)) != 0)
                {
                    destination.Write(buffer, 0, bytesRead);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public Task CopyToAsync(Stream destination) => CopyToAsync(destination, GetCopyBufferSize());

        public Task CopyToAsync(Stream destination, int bufferSize) => CopyToAsync(destination, bufferSize, CancellationToken.None);

        public Task CopyToAsync(Stream destination, CancellationToken cancellationToken) => CopyToAsync(destination, GetCopyBufferSize(), cancellationToken);

        public virtual Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            ValidateCopyToArguments(destination, bufferSize);
            if (!CanRead)
            {
                if (CanWrite)
                {
                    ThrowHelper.ThrowNotSupportedException_UnreadableStream();
                }

                ThrowHelper.ThrowObjectDisposedException_StreamClosed(GetType().Name);
            }

            return Core(this, destination, bufferSize, cancellationToken);

            static async Task Core(Stream source, Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                try
                {
                    int bytesRead;
                    while ((bytesRead = await source.ReadAsync(new Memory<byte>(buffer), cancellationToken).ConfigureAwait(false)) != 0)
                    {
                        await destination.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesRead), cancellationToken).ConfigureAwait(false);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }

        private int GetCopyBufferSize()
        {
            // This value was originally picked to be the largest multiple of 4096 that is still smaller than the large object heap threshold (85K).
            // The CopyTo{Async} buffer is short-lived and is likely to be collected at Gen0, and it offers a significant improvement in Copy
            // performance.  Since then, the base implementations of CopyTo{Async} have been updated to use ArrayPool, which will end up rounding
            // this size up to the next power of two (131,072), which will by default be on the large object heap.  However, most of the time
            // the buffer should be pooled, the LOH threshold is now configurable and thus may be different than 85K, and there are measurable
            // benefits to using the larger buffer size.  So, for now, this value remains.
            const int DefaultCopyBufferSize = 81920;

            int bufferSize = DefaultCopyBufferSize;

            if (CanSeek)
            {
                long length = Length;
                long position = Position;
                if (length <= position) // Handles negative overflows
                {
                    // There are no bytes left in the stream to copy.
                    // However, because CopyTo{Async} is virtual, we need to
                    // ensure that any override is still invoked to provide its
                    // own validation, so we use the smallest legal buffer size here.
                    bufferSize = 1;
                }
                else
                {
                    long remaining = length - position;
                    if (remaining > 0)
                    {
                        // In the case of a positive overflow, stick to the default size
                        bufferSize = (int)Math.Min(bufferSize, remaining);
                    }
                }
            }

            return bufferSize;
        }

        public void Dispose() => Close();

        public virtual void Close()
        {
            // When initially designed, Stream required that all cleanup logic went into Close(),
            // but this was thought up before IDisposable was added and never revisited. All subclasses
            // should put their cleanup now in Dispose(bool).
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Note: Never change this to call other virtual methods on Stream
            // like Write, since the state on subclasses has already been
            // torn down.  This is the last code to run on cleanup for a stream.
        }

        public virtual ValueTask DisposeAsync()
        {
            try
            {
                Dispose();
                return default;
            }
            catch (Exception exc)
            {
                return ValueTask.FromException(exc);
            }
        }

        public abstract void Flush();

        public Task FlushAsync() => FlushAsync(CancellationToken.None);

        public virtual Task FlushAsync(CancellationToken cancellationToken) =>
            Task.Factory.StartNew(
                static state => ((Stream)state!).Flush(), this,
                cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

        [Obsolete("CreateWaitHandle will be removed eventually.  Please use \"new ManualResetEvent(false)\" instead.")]
        protected virtual WaitHandle CreateWaitHandle() => new ManualResetEvent(false);

        public virtual IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
            BeginReadInternal(buffer, offset, count, callback, state);

        internal Task<int> BeginReadInternal(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            ValidateBufferArguments(buffer, offset, count);
            if (!CanRead)
            {
                ThrowHelper.ThrowNotSupportedException_UnreadableStream();
            }

            // Create the task to asynchronously do a Read.  This task serves both
            // as the asynchronous work item and as the IAsyncResult returned to the user.
            var task = new ReadWriteTask(isRead: true, delegate
            {
                // The ReadWriteTask stores all of the parameters to pass to Read.
                // As we're currently inside of it, we can get the current task
                // and grab the parameters from it.
                var thisTask = (ReadWriteTask)Task.InternalCurrent!;
                Debug.Assert(thisTask._stream != null, "Inside ReadWriteTask, InternalCurrent should be the ReadWriteTask, and stream should be set");

                Stream stream = thisTask._stream;
                byte[] buffer = thisTask._buffer!;
                thisTask._stream = null; // help alleviate some memory pressure
                thisTask._buffer = null;
                return stream.Read(buffer, thisTask._offset, thisTask._count);
            }, state, this, buffer, offset, count, callback);

            QueueReadWriteTask(task);

            return task;
        }

        public virtual int EndRead(IAsyncResult asyncResult)
        {
            if (asyncResult is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.asyncResult);
            }

            ReadWriteTask? readTask = asyncResult as ReadWriteTask;

            if (readTask is null || !readTask._isRead)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.InvalidOperation_WrongAsyncResultOrEndCalledMultiple);
            }
            else if (readTask._endCalled)
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_WrongAsyncResultOrEndCalledMultiple);
            }

            readTask._endCalled = true;
            return readTask.GetAwaiter().GetResult(); // block until completion, then get result / propagate any exception
        }

        public Task<int> ReadAsync(byte[] buffer, int offset, int count) => ReadAsync(buffer, offset, count, CancellationToken.None);

        public virtual Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
            }

            if (!HasOverriddenBeginEndRead())
            {
                // If the Stream does not override Begin/EndRead, then we can take an optimized path
                // that skips an extra layer of tasks / IAsyncResults.
                return BeginReadInternal(buffer, offset, count, null, null);
            }

            // Otherwise, we need to wrap calls to Begin/EndWrite to ensure we use the derived type's functionality.
            return TaskFactory<int>.FromAsyncTrim(
                this, new ReadWriteParameters { Buffer = buffer, Offset = offset, Count = count },
                static (stream, args, callback, state) => stream.BeginRead(args.Buffer, args.Offset, args.Count, callback, state),
                static (stream, asyncResult) => stream.EndRead(asyncResult));
        }

        public virtual ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> array))
            {
                return new ValueTask<int>(ReadAsync(array.Array!, array.Offset, array.Count, cancellationToken));
            }

            byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
            return FinishReadAsync(ReadAsync(sharedBuffer, 0, buffer.Length, cancellationToken), sharedBuffer, buffer);

            static async ValueTask<int> FinishReadAsync(Task<int> readTask, byte[] localBuffer, Memory<byte> localDestination)
            {
                try
                {
                    int result = await readTask.ConfigureAwait(false);
                    new ReadOnlySpan<byte>(localBuffer, 0, result).CopyTo(localDestination.Span);
                    return result;
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(localBuffer);
                }
            }
        }

        private struct ReadWriteParameters // struct for arguments to Read and Write calls
        {
            internal byte[] Buffer;
            internal int Offset;
            internal int Count;
        }

        public virtual IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
            BeginWriteInternal(buffer, offset, count, callback, state);

        internal Task BeginWriteInternal(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            ValidateBufferArguments(buffer, offset, count);
            if (!CanWrite)
            {
                ThrowHelper.ThrowNotSupportedException_UnwritableStream();
            }

            // Create the task to asynchronously do a Write.  This task serves both
            // as the asynchronous work item and as the IAsyncResult returned to the user.
            var task = new ReadWriteTask(isRead: false, delegate
            {
                // The ReadWriteTask stores all of the parameters to pass to Write.
                // As we're currently inside of it, we can get the current task
                // and grab the parameters from it.
                var thisTask = (ReadWriteTask)Task.InternalCurrent!;
                Debug.Assert(thisTask._stream != null, "Inside ReadWriteTask, InternalCurrent should be the ReadWriteTask, and stream should be set");

                Stream stream = thisTask._stream;
                byte[] buffer = thisTask._buffer!;
                thisTask._stream = null; // help alleviate some memory pressure
                thisTask._buffer = null;
                stream.Write(buffer, thisTask._offset, thisTask._count);
                return 0; // not used, but signature requires a value be returned
            }, state, this, buffer, offset, count, callback);

            QueueReadWriteTask(task);

            return task;
        }

        private static void QueueReadWriteTask(ReadWriteTask readWriteTask)
        {
            Debug.Assert(readWriteTask != null);

            // Schedule the task.  ScheduleAndStart must happen after the write to _activeReadWriteTask to avoid a race.
            // Internally, we're able to directly call ScheduleAndStart rather than Start, avoiding
            // two interlocked operations.  However, if ReadWriteTask is ever changed to use
            // a cancellation token, this should be changed to use Start.
            readWriteTask.m_taskScheduler = TaskScheduler.Default;
            readWriteTask.ScheduleAndStart(needsProtection: false);
        }

        public virtual void EndWrite(IAsyncResult asyncResult)
        {
            if (asyncResult is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.asyncResult);
            }

            ReadWriteTask? writeTask = asyncResult as ReadWriteTask;
            if (writeTask is null || writeTask._isRead)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.InvalidOperation_WrongAsyncResultOrEndCalledMultiple);
            }
            else if (writeTask._endCalled)
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_WrongAsyncResultOrEndCalledMultiple);
            }

            writeTask._endCalled = true;
            writeTask.GetAwaiter().GetResult(); // block until completion, then propagate any exceptions
            Debug.Assert(writeTask.Status == TaskStatus.RanToCompletion);
        }

        // Task used by BeginRead / BeginWrite to do Read / Write asynchronously.
        // A single instance of this task serves four purposes:
        // 1. The work item scheduled to run the Read / Write operation
        // 2. The state holding the arguments to be passed to Read / Write
        // 3. The IAsyncResult returned from BeginRead / BeginWrite
        // 4. The completion action that runs to invoke the user-provided callback.
        // This last item is a bit tricky.  Before the AsyncCallback is invoked, the
        // IAsyncResult must have completed, so we can't just invoke the handler
        // from within the task, since it is the IAsyncResult, and thus it's not
        // yet completed.  Instead, we use AddCompletionAction to install this
        // task as its own completion handler.  That saves the need to allocate
        // a separate completion handler, it guarantees that the task will
        // have completed by the time the handler is invoked, and it allows
        // the handler to be invoked synchronously upon the completion of the
        // task.  This all enables BeginRead / BeginWrite to be implemented
        // with a single allocation.
        private sealed class ReadWriteTask : Task<int>, ITaskCompletionAction
        {
            internal readonly bool _isRead;
            internal bool _endCalled;
            internal Stream? _stream;
            internal byte[]? _buffer;
            internal readonly int _offset;
            internal readonly int _count;
            private AsyncCallback? _callback;
            private ExecutionContext? _context;

            public ReadWriteTask(
                bool isRead,
                Func<object?, int> function, object? state,
                Stream stream, byte[] buffer, int offset, int count, AsyncCallback? callback) :
                base(function, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach)
            {
                Debug.Assert(function != null);
                Debug.Assert(stream != null);

                // Store the arguments
                _isRead = isRead;
                _stream = stream;
                _buffer = buffer;
                _offset = offset;
                _count = count;

                // If a callback was provided, we need to:
                // - Store the user-provided handler
                // - Capture an ExecutionContext under which to invoke the handler
                // - Add this task as its own completion handler so that the Invoke method
                //   will run the callback when this task completes.
                if (callback != null)
                {
                    _callback = callback;
                    _context = ExecutionContext.Capture();
                    base.AddCompletionAction(this);
                }
            }

            void ITaskCompletionAction.Invoke(Task _)
            {
                // Get the ExecutionContext.  If there is none, just run the callback directly, passing in this
                // task as the IAsyncResult. If there is one, process it with ExecutionContext.Run.
                ExecutionContext? context = _context;
                if (context is not null)
                {
                    AsyncCallback? callback = _callback;
                    _callback = null;
                    Debug.Assert(callback != null);
                    callback(this);
                }
                else
                {
                    _context = null;
                    ExecutionContext.RunInternal(context, static state => ((ITaskCompletionAction)state!).Invoke(null!), this);
                }
            }

            bool ITaskCompletionAction.InvokeMayRunArbitraryCode => true;
        }

        public Task WriteAsync(byte[] buffer, int offset, int count) => WriteAsync(buffer, offset, count, CancellationToken.None);

        public virtual Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // If cancellation was requested, bail early with an already completed task.
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            if (!HasOverriddenBeginEndWrite())
            {
                // If the Stream does not override Begin/EndWrite, then we can take an optimized path
                // that skips an extra layer of tasks / IAsyncResults.
                return BeginWriteInternal(buffer, offset, count, null, null);
            }

            // Otherwise, we need to wrap calls to Begin/EndWrite to ensure we use the derived type's functionality.
            return TaskFactory<VoidTaskResult>.FromAsyncTrim(
                this, new ReadWriteParameters { Buffer = buffer, Offset = offset, Count = count },
                static (stream, args, callback, state) => stream.BeginWrite(args.Buffer, args.Offset, args.Count, callback, state),
                static (stream, asyncResult) =>
                {
                    stream.EndWrite(asyncResult);
                    return default;
                });
        }

        public virtual ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> array))
            {
                return new ValueTask(WriteAsync(array.Array!, array.Offset, array.Count, cancellationToken));
            }

            byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
            buffer.Span.CopyTo(sharedBuffer);
            return new ValueTask(FinishWriteAsync(WriteAsync(sharedBuffer, 0, buffer.Length, cancellationToken), sharedBuffer));
        }

        private static async Task FinishWriteAsync(Task writeTask, byte[] localBuffer)
        {
            try
            {
                await writeTask.ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(localBuffer);
            }
        }

        public abstract long Seek(long offset, SeekOrigin origin);

        public abstract void SetLength(long value);

        public abstract int Read(byte[] buffer, int offset, int count);

        public virtual int Read(Span<byte> buffer)
        {
            byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
            try
            {
                int numRead = Read(sharedBuffer, 0, buffer.Length);
                if ((uint)numRead > (uint)buffer.Length)
                {
                    throw new IOException(SR.IO_StreamTooLong);
                }

                new ReadOnlySpan<byte>(sharedBuffer, 0, numRead).CopyTo(buffer);
                return numRead;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(sharedBuffer);
            }
        }

        public virtual int ReadByte()
        {
            var oneByteArray = new byte[1];
            int r = Read(oneByteArray, 0, 1);
            return r == 0 ? -1 : oneByteArray[0];
        }

        public abstract void Write(byte[] buffer, int offset, int count);

        public virtual void Write(ReadOnlySpan<byte> buffer)
        {
            byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
            try
            {
                buffer.CopyTo(sharedBuffer);
                Write(sharedBuffer, 0, buffer.Length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(sharedBuffer);
            }
        }

        public virtual void WriteByte(byte value) => Write(new byte[1] { value }, 0, 1);

        public static Stream Synchronized(Stream stream) =>
            stream is null ? throw new ArgumentNullException(nameof(stream)) :
            stream is SyncStream ? stream :
            new SyncStream(stream);

        [Obsolete("Do not call or override this method.")]
        protected virtual void ObjectInvariant() { }

        /// <summary>Validates arguments provided to reading and writing methods on <see cref="Stream"/>.</summary>
        /// <param name="buffer">The array "buffer" argument passed to the reading or writing method.</param>
        /// <param name="offset">The integer "offset" argument passed to the reading or writing method.</param>
        /// <param name="count">The integer "count" argument passed to the reading or writing method.</param>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> was null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="offset"/> was outside the bounds of <paramref name="buffer"/>, or
        /// <paramref name="count"/> was negative, or the range specified by the combination of
        /// <paramref name="offset"/> and <paramref name="count"/> exceed the length of <paramref name="buffer"/>.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void ValidateBufferArguments(byte[] buffer, int offset, int count)
        {
            if (buffer is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.buffer);
            }

            if (offset < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.offset, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            if ((uint)count > buffer.Length - offset)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.Argument_InvalidOffLen);
            }
        }

        /// <summary>Validates arguments provided to the <see cref="CopyTo(Stream, int)"/> or <see cref="CopyToAsync(Stream, int, CancellationToken)"/> methods.</summary>
        /// <param name="destination">The <see cref="Stream"/> "destination" argument passed to the copy method.</param>
        /// <param name="bufferSize">The integer "bufferSize" argument passed to the copy method.</param>
        /// <exception cref="ArgumentNullException"><paramref name="destination"/> was null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bufferSize"/> was not a positive value.</exception>
        /// <exception cref="NotSupportedException"><paramref name="destination"/> does not support writing.</exception>
        /// <exception cref="ObjectDisposedException"><paramref name="destination"/> does not support writing or reading.</exception>
        protected static void ValidateCopyToArguments(Stream destination, int bufferSize)
        {
            if (destination is null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, SR.ArgumentOutOfRange_NeedPosNum);
            }

            if (!destination.CanWrite)
            {
                if (destination.CanRead)
                {
                    ThrowHelper.ThrowNotSupportedException_UnwritableStream();
                }

                ThrowHelper.ThrowObjectDisposedException_StreamClosed(destination.GetType().Name);
            }
        }

        /// <summary>Provides a nop stream.</summary>
        private sealed class NullStream : Stream
        {
            internal NullStream() { }

            public override bool CanRead => true;
            public override bool CanWrite => true;
            public override bool CanSeek => true;
            public override long Length => 0;
            public override long Position { get => 0; set { } }

            public override void CopyTo(Stream destination, int bufferSize) { }

            public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) =>
                cancellationToken.IsCancellationRequested ?
                    Task.FromCanceled(cancellationToken) :
                    Task.CompletedTask;

            protected override void Dispose(bool disposing)
            {
                // Do nothing - we don't want NullStream singleton (static) to be closable
            }

            public override void Flush() { }

            public override Task FlushAsync(CancellationToken cancellationToken) =>
                cancellationToken.IsCancellationRequested ?
                    Task.FromCanceled(cancellationToken) :
                    Task.CompletedTask;

            public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
                TaskToApm.Begin(Task<int>.s_defaultResultTask, callback, state);

            public override int EndRead(IAsyncResult asyncResult) =>
                TaskToApm.End<int>(asyncResult);

            public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
                TaskToApm.Begin(Task.CompletedTask, callback, state);

            public override void EndWrite(IAsyncResult asyncResult) =>
                TaskToApm.End(asyncResult);

            public override int Read(byte[] buffer, int offset, int count) => 0;

            public override int Read(Span<byte> buffer) => 0;

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
                cancellationToken.IsCancellationRequested ?
                    Task.FromCanceled<int>(cancellationToken) :
                    Task.FromResult(0);

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken) =>
                cancellationToken.IsCancellationRequested ?
                    ValueTask.FromCanceled<int>(cancellationToken) :
                    default;

            public override int ReadByte() => -1;

            public override void Write(byte[] buffer, int offset, int count) { }

            public override void Write(ReadOnlySpan<byte> buffer) { }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
                cancellationToken.IsCancellationRequested ?
                    Task.FromCanceled(cancellationToken) :
                    Task.CompletedTask;

            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
                cancellationToken.IsCancellationRequested ?
                    ValueTask.FromCanceled(cancellationToken) :
                    default;

            public override void WriteByte(byte value) { }

            public override long Seek(long offset, SeekOrigin origin) => 0;

            public override void SetLength(long length) { }
        }

        /// <summary>Provides a wrapper around a stream that takes a lock for every operation.</summary>
        private sealed class SyncStream : Stream, IDisposable
        {
            private readonly Stream _stream;

            internal SyncStream(Stream stream) => _stream = stream ?? throw new ArgumentNullException(nameof(stream));

            public override bool CanRead => _stream.CanRead;
            public override bool CanWrite => _stream.CanWrite;
            public override bool CanSeek => _stream.CanSeek;
            public override bool CanTimeout => _stream.CanTimeout;

            public override long Length
            {
                get
                {
                    lock (_stream)
                    {
                        return _stream.Length;
                    }
                }
            }

            public override long Position
            {
                get
                {
                    lock (_stream)
                    {
                        return _stream.Position;
                    }
                }
                set
                {
                    lock (_stream)
                    {
                        _stream.Position = value;
                    }
                }
            }

            public override int ReadTimeout
            {
                get => _stream.ReadTimeout;
                set => _stream.ReadTimeout = value;
            }

            public override int WriteTimeout
            {
                get => _stream.WriteTimeout;
                set => _stream.WriteTimeout = value;
            }

            public override void Close()
            {
                lock (_stream)
                {
                    // On the off chance that some wrapped stream has different
                    // semantics for Close vs. Dispose, let's preserve that.
                    try
                    {
                        _stream.Close();
                    }
                    finally
                    {
                        base.Dispose(true);
                    }
                }
            }

            protected override void Dispose(bool disposing)
            {
                lock (_stream)
                {
                    try
                    {
                        // Explicitly pick up a potentially methodimpl'ed Dispose
                        if (disposing)
                        {
                            ((IDisposable)_stream).Dispose();
                        }
                    }
                    finally
                    {
                        base.Dispose(disposing);
                    }
                }
            }

            public override ValueTask DisposeAsync()
            {
                lock (_stream)
                {
                    return _stream.DisposeAsync();
                }
            }

            public override void Flush()
            {
                lock (_stream)
                {
                    _stream.Flush();
                }
            }

            public override int Read(byte[] bytes, int offset, int count)
            {
                lock (_stream)
                {
                    return _stream.Read(bytes, offset, count);
                }
            }

            public override int Read(Span<byte> buffer)
            {
                lock (_stream)
                {
                    return _stream.Read(buffer);
                }
            }

            public override int ReadByte()
            {
                lock (_stream)
                {
                    return _stream.ReadByte();
                }
            }

            public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
            {
#if CORERT
                throw new NotImplementedException(); // TODO: https://github.com/dotnet/corert/issues/3251
#else
                bool overridesBeginRead = _stream.HasOverriddenBeginEndRead();

                lock (_stream)
                {
                    // If the Stream does have its own BeginRead implementation, then we must use that override.
                    return overridesBeginRead ?
                        _stream.BeginRead(buffer, offset, count, callback, state) :
                        _stream.BeginReadInternal(buffer, offset, count, callback, state);
                }
#endif
            }

            public override int EndRead(IAsyncResult asyncResult)
            {
                if (asyncResult is null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.asyncResult);
                }

                lock (_stream)
                {
                    return _stream.EndRead(asyncResult);
                }
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                lock (_stream)
                {
                    return _stream.Seek(offset, origin);
                }
            }

            public override void SetLength(long length)
            {
                lock (_stream)
                {
                    _stream.SetLength(length);
                }
            }

            public override void Write(byte[] bytes, int offset, int count)
            {
                lock (_stream)
                {
                    _stream.Write(bytes, offset, count);
                }
            }

            public override void Write(ReadOnlySpan<byte> buffer)
            {
                lock (_stream)
                {
                    _stream.Write(buffer);
                }
            }

            public override void WriteByte(byte b)
            {
                lock (_stream)
                {
                    _stream.WriteByte(b);
                }
            }

            public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
            {
#if CORERT
                throw new NotImplementedException(); // TODO: https://github.com/dotnet/corert/issues/3251
#else
                bool overridesBeginWrite = _stream.HasOverriddenBeginEndWrite();

                lock (_stream)
                {
                    // If the Stream does have its own BeginWrite implementation, then we must use that override.
                    return overridesBeginWrite ?
                        _stream.BeginWrite(buffer, offset, count, callback, state) :
                        _stream.BeginWriteInternal(buffer, offset, count, callback, state);
                }
#endif
            }

            public override void EndWrite(IAsyncResult asyncResult)
            {
                if (asyncResult is null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.asyncResult);
                }

                lock (_stream)
                {
                    _stream.EndWrite(asyncResult);
                }
            }
        }
    }
}
