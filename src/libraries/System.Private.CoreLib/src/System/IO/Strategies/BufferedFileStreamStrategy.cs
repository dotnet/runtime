// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Strategies
{
    // this type exists so we can avoid duplicating the buffering logic in every FileStreamStrategy implementation
    internal sealed class BufferedFileStreamStrategy : FileStreamStrategy
    {
        private readonly FileStreamStrategy _strategy;
        private readonly int _bufferSize;

        private byte[]? _buffer;
        private int _writePos;
        private int _readPos;
        private int _readLen;
        // The last successful Task returned from ReadAsync (perf optimization for successive reads of the same size)
        private Task<int>? _lastSyncCompletedReadTask;

        internal BufferedFileStreamStrategy(FileStreamStrategy strategy, int bufferSize)
        {
            Debug.Assert(bufferSize > 1, "Buffering must not be enabled for smaller buffer sizes");

            _strategy = strategy;
            _bufferSize = bufferSize;
        }

        ~BufferedFileStreamStrategy()
        {
            try
            {
                // the finalizer must at least try to flush the write buffer
                // so we enforce it by passing always true
                Dispose(true);
            }
            catch (Exception e) when (FileStreamHelpers.IsIoRelatedException(e))
            {
                // On finalization, ignore failures from trying to flush the write buffer,
                // e.g. if this stream is wrapping a pipe and the pipe is now broken.
            }
        }

        public override bool CanRead => _strategy.CanRead;

        public override bool CanWrite => _strategy.CanWrite;

        public override bool CanSeek => _strategy.CanSeek;

        public override long Length
        {
            get
            {
                long len = _strategy.Length;

                // If we're writing near the end of the file, we must include our
                // internal buffer in our Length calculation.  Don't flush because
                // we use the length of the file in AsyncWindowsFileStreamStrategy.WriteAsync
                if (_writePos > 0 && _strategy.Position + _writePos > len)
                {
                    len = _writePos + _strategy.Position;
                }

                return len;
            }
        }

        public override long Position
        {
            get
            {
                Debug.Assert(!(_writePos > 0 && _readPos != _readLen), "Read and Write buffers cannot both have data in them at the same time.");

                return _strategy.Position + _readPos - _readLen + _writePos;
            }
            set
            {
                if (_writePos > 0)
                {
                    FlushWrite();
                }

                _readPos = 0;
                _readLen = 0;

                _strategy.Position = value;
            }
        }

        internal override bool IsAsync => _strategy.IsAsync;

        internal override bool IsClosed => _strategy.IsClosed;

        internal override string Name => _strategy.Name;

        internal override SafeFileHandle SafeFileHandle
        {
            get
            {
                // BufferedFileStreamStrategy must flush before the handle is exposed
                // so whoever uses SafeFileHandle to access disk data can see
                // the changes that were buffered in memory so far
                Flush();

                return _strategy.SafeFileHandle;
            }
        }

        public override async ValueTask DisposeAsync()
        {
            try
            {
                if (!_strategy.IsClosed)
                {
                    try
                    {
                        await FlushAsync().ConfigureAwait(false);
                    }
                    finally
                    {
                        await _strategy.DisposeAsync().ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                // Don't set the buffer to null, to avoid a NullReferenceException
                // when users have a race condition in their code (i.e. they call
                // FileStream.Close when calling another method on FileStream like Read).

                _writePos = 0; // WriteByte hot path relies on this
            }
        }

        internal override void DisposeInternal(bool disposing) => Dispose(disposing);

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && !_strategy.IsClosed)
                {
                    try
                    {
                        Flush();
                    }
                    finally
                    {
                        _strategy.Dispose();
                    }
                }
            }
            finally
            {
                // Don't set the buffer to null, to avoid a NullReferenceException
                // when users have a race condition in their code (i.e. they call
                // FileStream.Close when calling another method on FileStream like Read).

                // Call base.Dispose(bool) to cleanup async IO resources
                base.Dispose(disposing);

                _writePos = 0;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            AssertBufferArguments(buffer, offset, count);

            return ReadSpan(new Span<byte>(buffer, offset, count), new ArraySegment<byte>(buffer, offset, count));
        }

        public override int Read(Span<byte> destination)
        {
            EnsureNotClosed();

            return ReadSpan(destination, default);
        }

        private int ReadSpan(Span<byte> destination, ArraySegment<byte> arraySegment)
        {
            Debug.Assert((_readPos == 0 && _readLen == 0 && _writePos >= 0) || (_writePos == 0 && _readPos <= _readLen),
                "We're either reading or writing, but not both.");

            bool isBlocked = false;
            int n = _readLen - _readPos;
            // if the read buffer is empty, read into either user's array or our
            // buffer, depending on number of bytes user asked for and buffer size.
            if (n == 0)
            {
                EnsureCanRead();

                if (_writePos > 0)
                {
                    FlushWrite();
                }

                if (!_strategy.CanSeek || (destination.Length >= _bufferSize))
                {
                    // For async file stream strategies the call to Read(Span) is translated to Stream.Read(Span),
                    // which rents an array from the pool, copies the data, and then calls Read(Array). This is expensive!
                    // To avoid that (and code duplication), the Read(Array) method passes ArraySegment to this method
                    // which allows for calling Strategy.Read(Array) instead of Strategy.Read(Span).
                    n = arraySegment.Array != null
                        ? _strategy.Read(arraySegment.Array, arraySegment.Offset, arraySegment.Count)
                        : _strategy.Read(destination);

                    // Throw away read buffer.
                    _readPos = 0;
                    _readLen = 0;
                    return n;
                }

                EnsureBufferAllocated();
                n = _strategy.Read(_buffer, 0, _bufferSize);

                if (n == 0)
                {
                    return 0;
                }

                isBlocked = n < _bufferSize;
                _readPos = 0;
                _readLen = n;
            }
            // Now copy min of count or numBytesAvailable (i.e. near EOF) to array.
            if (n > destination.Length)
            {
                n = destination.Length;
            }
            new ReadOnlySpan<byte>(_buffer, _readPos, n).CopyTo(destination);
            _readPos += n;

            // We may have read less than the number of bytes the user asked
            // for, but that is part of the Stream contract.  Reading again for
            // more data may cause us to block if we're using a device with
            // no clear end of file, such as a serial port or pipe.  If we
            // blocked here & this code was used with redirected pipes for a
            // process's standard output, this can lead to deadlocks involving
            // two processes. But leave this here for files to avoid what would
            // probably be a breaking change.         --

            // If we are reading from a device with no clear EOF like a
            // serial port or a pipe, this will cause us to block incorrectly.
            if (_strategy.CanSeek)
            {
                // If we hit the end of the buffer and didn't have enough bytes, we must
                // read some more from the underlying stream.  However, if we got
                // fewer bytes from the underlying stream than we asked for (i.e. we're
                // probably blocked), don't ask for more bytes.
                if (n < destination.Length && !isBlocked)
                {
                    Debug.Assert(_readPos == _readLen, "Read buffer should be empty!");

                    int moreBytesRead = arraySegment.Array != null
                        ? _strategy.Read(arraySegment.Array, arraySegment.Offset + n, arraySegment.Count - n)
                        : _strategy.Read(destination.Slice(n));

                    n += moreBytesRead;
                    // We've just made our buffer inconsistent with our position
                    // pointer.  We must throw away the read buffer.
                    _readPos = 0;
                    _readLen = 0;
                }
            }

            return n;
        }

        public override int ReadByte() => _readPos != _readLen ? _buffer![_readPos++] : ReadByteSlow();

        private int ReadByteSlow()
        {
            Debug.Assert(_readPos == _readLen);

            // We want to check for whether the underlying stream has been closed and whether
            // it's readable, but we only need to do so if we don't have data in our buffer,
            // as any data we have came from reading it from an open stream, and we don't
            // care if the stream has been closed or become unreadable since. Further, if
            // the stream is closed, its read buffer is flushed, so we'll take this slow path.
            EnsureNotClosed();
            EnsureCanRead();

            if (_writePos > 0)
            {
                FlushWrite();
            }

            EnsureBufferAllocated();
            _readLen = _strategy.Read(_buffer, 0, _bufferSize);
            _readPos = 0;

            if (_readLen == 0)
            {
                return -1;
            }

            return _buffer[_readPos++];
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            AssertBufferArguments(buffer, offset, count);

            ValueTask<int> readResult = ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken);

            return readResult.IsCompletedSuccessfully
                ? LastSyncCompletedReadTask(readResult.Result)
                : readResult.AsTask();

            Task<int> LastSyncCompletedReadTask(int val)
            {
                Task<int>? t = _lastSyncCompletedReadTask;
                Debug.Assert(t == null || t.IsCompletedSuccessfully);

                if (t != null && t.Result == val)
                    return t;

                t = Task.FromResult<int>(val);
                _lastSyncCompletedReadTask = t;
                return t;
            }
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<int>(cancellationToken);
            }

            EnsureCanRead();

            Debug.Assert(!_strategy.IsClosed, "FileStream ensures that strategy is not closed");
            Debug.Assert((_readPos == 0 && _readLen == 0 && _writePos >= 0) || (_writePos == 0 && _readPos <= _readLen),
                "We're either reading or writing, but not both.");

            if (!_strategy.CanSeek)
            {
                return ReadFromNonSeekableAsync(buffer, cancellationToken);
            }

            SemaphoreSlim semaphore = EnsureAsyncActiveSemaphoreInitialized();
            Task semaphoreLockTask = semaphore.WaitAsync(cancellationToken);

            if (semaphoreLockTask.IsCompletedSuccessfully // lock has been acquired
                && _writePos == 0) // there is nothing to flush
            {
                bool releaseTheLock = true;
                try
                {
                    if (_readLen == _readPos && buffer.Length >= _bufferSize)
                    {
                        // hot path #1: the read buffer is empty and buffering would not be beneficial
                        // To find out why we are bypassing cache here, please see WriteAsync comments.
                        return _strategy.ReadAsync(buffer, cancellationToken);
                    }
                    else if (_readLen - _readPos >= buffer.Length)
                    {
                        // hot path #2: there is enough data in the buffer
                        _buffer.AsSpan(_readPos, buffer.Length).CopyTo(buffer.Span);
                        _readPos += buffer.Length;
                        return new ValueTask<int>(buffer.Length);
                    }

                    releaseTheLock = false;
                }
                finally
                {
                    if (releaseTheLock)
                    {
                        semaphore.Release();
                    }
                    // the code is going to call ReadAsyncSlowPath which is going to release the lock
                }
            }

            return ReadAsyncSlowPath(semaphoreLockTask, buffer, cancellationToken);
        }

        private async ValueTask<int> ReadFromNonSeekableAsync(Memory<byte> destination, CancellationToken cancellationToken)
        {
            Debug.Assert(!_strategy.CanSeek);

            // Employ async waiting based on the same synchronization used in BeginRead of the abstract Stream.
            await EnsureAsyncActiveSemaphoreInitialized().WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Pipes are tricky, at least when you have 2 different pipes
                // that you want to use simultaneously.  When redirecting stdout
                // & stderr with the Process class, it's easy to deadlock your
                // parent & child processes when doing writes 4K at a time.  The
                // OS appears to use a 4K buffer internally.  If you write to a
                // pipe that is full, you will block until someone read from
                // that pipe.  If you try reading from an empty pipe and
                // Win32FileStream's ReadAsync blocks waiting for data to fill it's
                // internal buffer, you will be blocked.  In a case where a child
                // process writes to stdout & stderr while a parent process tries
                // reading from both, you can easily get into a deadlock here.
                // To avoid this deadlock, don't buffer when doing async IO on
                // pipes.  But don't completely ignore buffered data either.
                if (_readPos < _readLen)
                {
                    int n = Math.Min(_readLen - _readPos, destination.Length);
                    new Span<byte>(_buffer!, _readPos, n).CopyTo(destination.Span);
                    _readPos += n;
                    return n;
                }
                else
                {
                    Debug.Assert(_writePos == 0, "Win32FileStream must not have buffered write data here!  Pipes should be unidirectional.");
                    return await _strategy.ReadAsync(destination, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                _asyncActiveSemaphore.Release();
            }
        }

        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
        private async ValueTask<int> ReadAsyncSlowPath(Task semaphoreLockTask, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            Debug.Assert(_asyncActiveSemaphore != null);
            Debug.Assert(_strategy.CanSeek);

            // Employ async waiting based on the same synchronization used in BeginRead of the abstract Stream.
            await semaphoreLockTask.ConfigureAwait(false);
            try
            {
                int bytesFromBuffer = 0;
                int bytesAlreadySatisfied = 0;

                if (_readLen - _readPos > 0)
                {
                    // The buffer might have been changed by another async task while we were waiting on the semaphore.
                    // Check it now again.
                    bytesFromBuffer = Math.Min(buffer.Length, _readLen - _readPos);

                    if (bytesFromBuffer > 0) // don't try to copy 0 bytes
                    {
                        _buffer.AsSpan(_readPos, bytesFromBuffer).CopyTo(buffer.Span);
                        _readPos += bytesFromBuffer;
                    }

                    if (bytesFromBuffer == buffer.Length)
                    {
                        return bytesFromBuffer;
                    }

                    if (bytesFromBuffer > 0)
                    {
                        buffer = buffer.Slice(bytesFromBuffer);
                        bytesAlreadySatisfied += bytesFromBuffer;
                    }
                }

                Debug.Assert(_readLen == _readPos, "The read buffer must now be empty");
                _readPos = _readLen = 0;

                // If there was anything in the write buffer, clear it.
                if (_writePos > 0)
                {
                    await _strategy.WriteAsync(new ReadOnlyMemory<byte>(_buffer, 0, _writePos), cancellationToken).ConfigureAwait(false);
                    _writePos = 0;
                }

                // If the requested read is larger than buffer size, avoid the buffer and still use a single read:
                if (buffer.Length >= _bufferSize)
                {
                    return await _strategy.ReadAsync(buffer, cancellationToken).ConfigureAwait(false) + bytesAlreadySatisfied;
                }

                // Ok. We can fill the buffer:
                EnsureBufferAllocated();
                _readLen = await _strategy.ReadAsync(new Memory<byte>(_buffer, 0, _bufferSize), cancellationToken).ConfigureAwait(false);

                bytesFromBuffer = Math.Min(_readLen, buffer.Length);
                _buffer.AsSpan(0, bytesFromBuffer).CopyTo(buffer.Span);
                _readPos += bytesFromBuffer;
                return bytesAlreadySatisfied + bytesFromBuffer;
            }
            finally
            {
                _asyncActiveSemaphore.Release();
            }
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
            => TaskToApm.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), callback, state);

        public override int EndRead(IAsyncResult asyncResult)
            => TaskToApm.End<int>(asyncResult);

        public override void Write(byte[] buffer, int offset, int count)
        {
            AssertBufferArguments(buffer, offset, count);

            WriteSpan(new ReadOnlySpan<byte>(buffer, offset, count), new ArraySegment<byte>(buffer, offset, count));
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            EnsureNotClosed();

            WriteSpan(buffer, default);
        }

        private void WriteSpan(ReadOnlySpan<byte> source, ArraySegment<byte> arraySegment)
        {
            if (_writePos == 0)
            {
                EnsureCanWrite();
                ClearReadBufferBeforeWrite();
            }

            // If our buffer has data in it, copy data from the user's array into
            // the buffer, and if we can fit it all there, return.  Otherwise, write
            // the buffer to disk and copy any remaining data into our buffer.
            // The assumption here is memcpy is cheaper than disk (or net) IO.
            // (10 milliseconds to disk vs. ~20-30 microseconds for a 4K memcpy)
            // So the extra copying will reduce the total number of writes, in
            // non-pathological cases (i.e. write 1 byte, then write for the buffer
            // size repeatedly)
            if (_writePos > 0)
            {
                int numBytes = _bufferSize - _writePos;   // space left in buffer
                if (numBytes > 0)
                {
                    if (numBytes >= source.Length)
                    {
                        source.CopyTo(_buffer!.AsSpan(_writePos));
                        _writePos += source.Length;
                        return;
                    }
                    else
                    {
                        source.Slice(0, numBytes).CopyTo(_buffer!.AsSpan(_writePos));
                        _writePos += numBytes;
                        source = source.Slice(numBytes);
                        if (arraySegment.Array != null)
                        {
                            arraySegment = arraySegment.Slice(numBytes);
                        }
                    }
                }

                FlushWrite();
                Debug.Assert(_writePos == 0, "FlushWrite must set _writePos to 0");
            }

            // If the buffer would slow _bufferSize down, avoid buffer completely.
            if (source.Length >= _bufferSize)
            {
                Debug.Assert(_writePos == 0, "FileStream cannot have buffered data to write here!  Your stream will be corrupted.");

                // For async file stream strategies the call to Write(Span) is translated to Stream.Write(Span),
                // which rents an array from the pool, copies the data, and then calls Write(Array). This is expensive!
                // To avoid that (and code duplication), the Write(Array) method passes ArraySegment to this method
                // which allows for calling Strategy.Write(Array) instead of Strategy.Write(Span).
                if (arraySegment.Array != null)
                {
                    _strategy.Write(arraySegment.Array, arraySegment.Offset, arraySegment.Count);
                }
                else
                {
                    _strategy.Write(source);
                }

                return;
            }
            else if (source.Length == 0)
            {
                return;  // Don't allocate a buffer then call memcpy for 0 bytes.
            }

            // Copy remaining bytes into buffer, to write at a later date.
            EnsureBufferAllocated();
            source.CopyTo(_buffer.AsSpan(_writePos));
            _writePos = source.Length;
        }

        public override void WriteByte(byte value)
        {
            if (_writePos > 0 && _writePos < _bufferSize - 1)
            {
                _buffer![_writePos++] = value;
            }
            else
            {
                WriteByteSlow(value);
            }
        }

        private void WriteByteSlow(byte value)
        {
            if (_writePos == 0)
            {
                EnsureNotClosed();
                EnsureCanWrite();
                ClearReadBufferBeforeWrite();
                EnsureBufferAllocated();
            }
            else
            {
                Debug.Assert(_writePos <= _bufferSize);
                FlushWrite();
            }

            _buffer![_writePos++] = value;
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            AssertBufferArguments(buffer, offset, count);

            return WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled(cancellationToken);
            }

            EnsureCanWrite();

            Debug.Assert(!_strategy.IsClosed, "FileStream ensures that strategy is not closed");
            Debug.Assert((_readPos == 0 && _readLen == 0 && _writePos >= 0) || (_writePos == 0 && _readPos <= _readLen),
                "We're either reading or writing, but not both.");
            Debug.Assert(_strategy.CanSeek || (_readPos == 0 && _readLen == 0),
                "Win32FileStream must not have buffered data here!  Pipes should be unidirectional.");

            if (!_strategy.CanSeek)
            {
                // avoid async buffering with pipes, as doing so can lead to deadlocks (see comments in ReadFromPipeAsync)
                return WriteToNonSeekableAsync(buffer, cancellationToken);
            }

            SemaphoreSlim semaphore = EnsureAsyncActiveSemaphoreInitialized();
            Task semaphoreLockTask = semaphore.WaitAsync(cancellationToken);

            if (semaphoreLockTask.IsCompletedSuccessfully // lock has been acquired
                && _readPos == _readLen) // there is nothing to flush
            {
                bool releaseTheLock = true;
                try
                {
                    // hot path #1: the write buffer is empty and buffering would not be beneficial
                    if (_writePos == 0 && buffer.Length >= _bufferSize)
                    {
                        // The fact that Strategy can be wrapped by BufferedFileStreamStrategy
                        // is transparent to every Strategy implementation. It means, that
                        // every Strategy must work fine no matter if buffering is enabled or not.
                        // In case of AsyncWindowsFileStreamStrategy.WriteAsync,
                        // it updates it's private position BEFORE it enqueues the IO request.
                        // This combined with the fact that BufferedFileStreamStrategy state
                        // is not modified here, allows us to NOT await the call
                        // and release the lock BEFORE the IO request completes.
                        // It improves the performance of common scenario, where buffering is enabled (default)
                        // but the user provides buffers larger (or equal) to the internal buffer size.
                        return _strategy.WriteAsync(buffer, cancellationToken);
                    }
                    else if (_bufferSize - _writePos >= buffer.Length)
                    {
                        // hot path #2 if the write completely fits into the buffer, we can complete synchronously:
                        EnsureBufferAllocated();
                        buffer.Span.CopyTo(_buffer.AsSpan(_writePos));
                        _writePos += buffer.Length;
                        return default;
                    }

                    releaseTheLock = false;
                }
                finally
                {
                    if (releaseTheLock)
                    {
                        semaphore.Release();
                    }
                    // the code is going to call ReadAsyncSlowPath which is going to release the lock
                }
            }

            return WriteAsyncSlowPath(semaphoreLockTask, buffer, cancellationToken);
        }

        private async ValueTask WriteToNonSeekableAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
        {
            Debug.Assert(!_strategy.CanSeek);

            await EnsureAsyncActiveSemaphoreInitialized().WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await _strategy.WriteAsync(source, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _asyncActiveSemaphore.Release();
            }
        }

        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private async ValueTask WriteAsyncSlowPath(Task semaphoreLockTask, ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
        {
            Debug.Assert(_asyncActiveSemaphore != null);
            Debug.Assert(_strategy.CanSeek);

            await semaphoreLockTask.ConfigureAwait(false);
            try
            {
                if (_writePos == 0)
                {
                    ClearReadBufferBeforeWrite();
                }

                // If our buffer has data in it, copy data from the user's array into
                // the buffer, and if we can fit it all there, return.  Otherwise, write
                // the buffer to disk and copy any remaining data into our buffer.
                // The assumption here is memcpy is cheaper than disk (or net) IO.
                // (10 milliseconds to disk vs. ~20-30 microseconds for a 4K memcpy)
                // So the extra copying will reduce the total number of writes, in
                // non-pathological cases (i.e. write 1 byte, then write for the buffer
                // size repeatedly)
                if (_writePos > 0)
                {
                    int spaceLeft = _bufferSize - _writePos;
                    if (spaceLeft > 0)
                    {
                        if (spaceLeft >= source.Length)
                        {
                            source.Span.CopyTo(_buffer.AsSpan(_writePos));
                            _writePos += source.Length;
                            return;
                        }
                        else
                        {
                            source.Span.Slice(0, spaceLeft).CopyTo(_buffer.AsSpan(_writePos));
                            _writePos += spaceLeft;
                            source = source.Slice(spaceLeft);
                        }
                    }

                    await _strategy.WriteAsync(new ReadOnlyMemory<byte>(_buffer, 0, _writePos), cancellationToken).ConfigureAwait(false);
                    _writePos = 0;
                }

                // If the buffer would slow _bufferSize down, avoid buffer completely.
                if (source.Length >= _bufferSize)
                {
                    Debug.Assert(_writePos == 0, "FileStream cannot have buffered data to write here!  Your stream will be corrupted.");
                    await _strategy.WriteAsync(source, cancellationToken).ConfigureAwait(false);
                    return;
                }
                else if (source.Length == 0)
                {
                    return;  // Don't allocate a buffer then call memcpy for 0 bytes.
                }

                // Copy remaining bytes into buffer, to write at a later date.
                EnsureBufferAllocated();
                source.Span.CopyTo(_buffer.AsSpan(_writePos));
                _writePos = source.Length;
            }
            finally
            {
                _asyncActiveSemaphore.Release();
            }
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
            => TaskToApm.Begin(WriteAsync(buffer, offset, count, CancellationToken.None), callback, state);

        public override void EndWrite(IAsyncResult asyncResult)
            => TaskToApm.End(asyncResult);

        public override void SetLength(long value)
        {
            Flush();

            _strategy.SetLength(value);
        }

        public override void Flush() => Flush(flushToDisk: false);

        internal override void Flush(bool flushToDisk)
        {
            EnsureNotClosed();

            // Has write data in the buffer:
            if (_writePos > 0)
            {
                // EnsureNotClosed does not guarantee that the Stream has not been closed
                // an example could be a call to fileStream.SafeFileHandle.Dispose()
                // so to avoid getting exception here, we just ensure that we can Write before doing it
                if (_strategy.CanWrite)
                {
                    FlushWrite();
                    Debug.Assert(_writePos == 0 && _readPos == 0 && _readLen == 0);
                    return;
                }
            }

            // Has read data in the buffer:
            if (_readPos < _readLen)
            {
                // If the underlying strategy is not seekable AND we have something in the read buffer, then FlushRead would throw.
                // We can either throw away the buffer resulting in data loss (!) or ignore the Flush.
                // (We cannot throw because it would be a breaking change.) We opt into ignoring the Flush in that situation.
                if (_strategy.CanSeek)
                {
                    FlushRead();
                }

                // If the Stream was seekable, then we should have called FlushRead which resets _readPos & _readLen.
                Debug.Assert(_writePos == 0 && (!_strategy.CanSeek || (_readPos == 0 && _readLen == 0)));
                return;
            }

            // We had no data in the buffer, but we still need to tell the underlying strategy to flush.
            _strategy.Flush(flushToDisk);

            _writePos = _readPos = _readLen = 0;
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
            }

            EnsureNotClosed();

            return FlushAsyncInternal(cancellationToken);
        }

        private async Task FlushAsyncInternal(CancellationToken cancellationToken)
        {
            await EnsureAsyncActiveSemaphoreInitialized().WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_writePos > 0)
                {
                    await _strategy.WriteAsync(new ReadOnlyMemory<byte>(_buffer, 0, _writePos), cancellationToken).ConfigureAwait(false);
                    _writePos = 0;
                    Debug.Assert(_writePos == 0 && _readPos == 0 && _readLen == 0);
                    return;
                }

                if (_readPos < _readLen)
                {
                    // If the underlying strategy is not seekable AND we have something in the read buffer, then FlushRead would throw.
                    // We can either throw away the buffer resulting in date loss (!) or ignore the Flush. (We cannot throw because it
                    // would be a breaking change.) We opt into ignoring the Flush in that situation.
                    if (_strategy.CanSeek)
                    {
                        FlushRead();  // not async; it uses Seek, but there's no SeekAsync
                    }

                    // If the Strategy was seekable, then we should have called FlushRead which resets _readPos & _readLen.
                    Debug.Assert(_writePos == 0 && (!_strategy.CanSeek || (_readPos == 0 && _readLen == 0)));
                    return;
                }

                // There was nothing in the buffer:
                Debug.Assert(_writePos == 0 && _readPos == _readLen);
            }
            finally
            {
                _asyncActiveSemaphore.Release();
            }
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            EnsureNotClosed();
            EnsureCanRead();

            return cancellationToken.IsCancellationRequested ?
                Task.FromCanceled<int>(cancellationToken) :
                CopyToAsyncCore(destination, bufferSize, cancellationToken);
        }

        private async Task CopyToAsyncCore(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            // Synchronize async operations as does Read/WriteAsync.
            await EnsureAsyncActiveSemaphoreInitialized().WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                int readBytes = _readLen - _readPos;
                Debug.Assert(readBytes >= 0, $"Expected a non-negative number of bytes in buffer, got {readBytes}");

                if (readBytes > 0)
                {
                    // If there's any read data in the buffer, write it all to the destination stream.
                    Debug.Assert(_writePos == 0, "Write buffer must be empty if there's data in the read buffer");
                    await destination.WriteAsync(new ReadOnlyMemory<byte>(_buffer, _readPos, readBytes), cancellationToken).ConfigureAwait(false);
                    _readPos = _readLen = 0;
                }
                else if (_writePos > 0)
                {
                    // If there's write data in the buffer, flush it back to the underlying stream, as does ReadAsync.
                    await _strategy.WriteAsync(new ReadOnlyMemory<byte>(_buffer, 0, _writePos), cancellationToken).ConfigureAwait(false);
                    _writePos = 0;
                }

                // Our buffer is now clear. Copy data directly from the source stream to the destination stream.
                await _strategy.CopyToAsync(destination, bufferSize, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _asyncActiveSemaphore.Release();
            }
        }

        public override void CopyTo(Stream destination, int bufferSize)
        {
            EnsureNotClosed();
            EnsureCanRead();

            int readBytes = _readLen - _readPos;
            Debug.Assert(readBytes >= 0, $"Expected a non-negative number of bytes in buffer, got {readBytes}");

            if (readBytes > 0)
            {
                // If there's any read data in the buffer, write it all to the destination stream.
                Debug.Assert(_writePos == 0, "Write buffer must be empty if there's data in the read buffer");
                destination.Write(_buffer!, _readPos, readBytes);
                _readPos = _readLen = 0;
            }
            else if (_writePos > 0)
            {
                // If there's write data in the buffer, flush it back to the underlying stream, as does ReadAsync.
                FlushWrite();
            }

            // Our buffer is now clear. Copy data directly from the source stream to the destination stream.
            _strategy.CopyTo(destination, bufferSize);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            EnsureNotClosed();
            EnsureCanSeek();

            // If we have bytes in the write buffer, flush them out, seek and be done.
            if (_writePos > 0)
            {
                FlushWrite();
                return _strategy.Seek(offset, origin);
            }

            // The buffer is either empty or we have a buffered read.
            if (_readLen - _readPos > 0 && origin == SeekOrigin.Current)
            {
                // If we have bytes in the read buffer, adjust the seek offset to account for the resulting difference
                // between this stream's position and the underlying stream's position.
                offset -= (_readLen - _readPos);
            }

            long oldPos = Position;
            Debug.Assert(oldPos == _strategy.Position + (_readPos - _readLen));

            long newPos = _strategy.Seek(offset, origin);

            // If the seek destination is still within the data currently in the buffer, we want to keep the buffer data and continue using it.
            // Otherwise we will throw away the buffer. This can only happen on read, as we flushed write data above.

            // The offset of the new/updated seek pointer within _buffer:
            _readPos = (int)(newPos - (oldPos - _readPos));

            // If the offset of the updated seek pointer in the buffer is still legal, then we can keep using the buffer:
            if (0 <= _readPos && _readPos < _readLen)
            {
                // Adjust the seek pointer of the underlying stream to reflect the amount of useful bytes in the read buffer:
                _strategy.Seek(_readLen - _readPos, SeekOrigin.Current);
            }
            else
            {  // The offset of the updated seek pointer is not a legal offset. Loose the buffer.
                _readPos = _readLen = 0;
            }

            Debug.Assert(newPos == Position, $"newPos (={newPos}) == Position (={Position})");
            return newPos;
        }

        internal override void Lock(long position, long length) => _strategy.Lock(position, length);

        internal override void Unlock(long position, long length) => _strategy.Unlock(position, length);

        // Reading is done in blocks, but someone could read 1 byte from the buffer then write.
        // At that point, the underlying stream's pointer is out of sync with this stream's position.
        // All write functions should call this function to ensure that the buffered data is not lost.
        private void FlushRead()
        {
            Debug.Assert(_writePos == 0, "Write buffer must be empty in FlushRead!");

            if (_readPos - _readLen != 0)
            {
                _strategy.Seek(_readPos - _readLen, SeekOrigin.Current);
            }

            _readPos = 0;
            _readLen = 0;
        }

        private void FlushWrite()
        {
            Debug.Assert(_readPos == 0 && _readLen == 0, "Read buffer must be empty in FlushWrite!");
            Debug.Assert(_buffer != null && _bufferSize >= _writePos, "Write buffer must be allocated and write position must be in the bounds of the buffer in FlushWrite!");

            _strategy.Write(_buffer, 0, _writePos);
            _writePos = 0;
        }

        /// <summary>
        /// Called by Write methods to clear the Read Buffer
        /// </summary>
        private void ClearReadBufferBeforeWrite()
        {
            Debug.Assert(_readPos <= _readLen, $"_readPos <= _readLen [{_readPos} <= {_readLen}]");

            // No read data in the buffer:
            if (_readPos == _readLen)
            {
                _readPos = _readLen = 0;
                return;
            }

            // Must have read data.
            Debug.Assert(_readPos < _readLen);
            FlushRead();
        }

        private void EnsureNotClosed()
        {
            if (_strategy.IsClosed)
            {
                ThrowHelper.ThrowObjectDisposedException_StreamClosed(null);
            }
        }

        private void EnsureCanSeek()
        {
            if (!_strategy.CanSeek)
            {
                ThrowHelper.ThrowNotSupportedException_UnseekableStream();
            }
        }

        private void EnsureCanRead()
        {
            if (!_strategy.CanRead)
            {
                ThrowHelper.ThrowNotSupportedException_UnreadableStream();
            }
        }

        private void EnsureCanWrite()
        {
            if (!_strategy.CanWrite)
            {
                ThrowHelper.ThrowNotSupportedException_UnwritableStream();
            }
        }

        [MemberNotNull(nameof(_buffer))]
        private void EnsureBufferAllocated()
        {
            if (_buffer is null)
            {
                AllocateBuffer();
            }
        }

        // TODO https://github.com/dotnet/roslyn/issues/47896: should be local function in EnsureBufferAllocated above.
        [MemberNotNull(nameof(_buffer))]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AllocateBuffer()
        {
            Interlocked.CompareExchange(ref _buffer, GC.AllocateUninitializedArray<byte>(_bufferSize), null);
        }

        [Conditional("DEBUG")]
        private void AssertBufferArguments(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count); // FileStream is supposed to call this
            Debug.Assert(!_strategy.IsClosed, "FileStream ensures that strategy is not closed");
        }
    }
}
