// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    // this type exists so we can avoid duplicating the buffering logic in every FileStreamStrategy implementation
    internal sealed class BufferedFileStreamStrategy : FileStreamStrategy
    {
        private const int MaxShadowBufferSize = 81920;  // Make sure not to get to the Large Object Heap.

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
            Debug.Assert(bufferSize > 1);

            _strategy = strategy;
            _bufferSize = bufferSize;
        }

        ~BufferedFileStreamStrategy() => DisposeInternal(false);

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

                return _strategy.Position + (_readPos - _readLen + _writePos);
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

                Debug.Assert(_writePos == 0, "Everything must have been already flushed");
            }
        }

        internal override void DisposeInternal(bool disposing)
        {
            try
            {
                // the finalizer must at least try to flush the write buffer
                // so we enforce it by passing always true
                Dispose(true);
            }
            catch (Exception e) when (!disposing && FileStream.IsIoRelatedException(e))
            {
                // On finalization, ignore failures from trying to flush the write buffer,
                // e.g. if this stream is wrapping a pipe and the pipe is now broken.
            }

            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
        }

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

                Debug.Assert(_writePos == 0, "Everything must have been already flushed");
            }
        }

        public override void SetLength(long value)
        {
            Flush();

            _strategy.SetLength(value);
        }

        // the Read(Array) overload does not just create a Span and call Read(Span)
        // because for async file stream strategies the call to Read(Span)
        // is translated to Stream.Read(Span), which rents an array from the pool
        // copies the data, and then calls Read(Array)
        public override int Read(byte[] buffer, int offset, int count)
        {
            AssertBufferArguments(buffer, offset, count);
            EnsureCanRead();

            int bytesFromBuffer = ReadFromBuffer(buffer, offset, count);

            // We may have read less than the number of bytes the user asked for, but that is part of the Stream Debug.
            // Reading again for more data may cause us to block if we're using a device with no clear end of file,
            // such as a serial port or pipe. If we blocked here and this code was used with redirected pipes for a
            // process's standard output, this can lead to deadlocks involving two processes.
            // BUT - this is a breaking change.
            // So: If we could not read all bytes the user asked for from the buffer, we will try once from the underlying
            // stream thus ensuring the same blocking behaviour as if the underlying stream was not wrapped in this BufferedStream.
            if (bytesFromBuffer == count
                && !(count == 0 && _readLen == _readPos)) // blocking 0 bytes reads are OK only when the read buffer is empty
            {
                return bytesFromBuffer;
            }

            int alreadySatisfied = bytesFromBuffer;
            if (bytesFromBuffer > 0)
            {
                count -= bytesFromBuffer;
                offset += bytesFromBuffer;
            }

            Debug.Assert(_readLen == _readPos, "The read buffer must now be empty");
            _readPos = _readLen = 0;

            // If there was anything in the write buffer, clear it.
            if (_writePos > 0)
            {
                FlushWrite();
            }

            // If the requested read is larger than buffer size, avoid the buffer and still use a single read:
            if (count >= _bufferSize)
            {
                return _strategy.Read(buffer, offset, count) + alreadySatisfied;
            }

            // Ok. We can fill the buffer:
            EnsureBufferAllocated();
            _readLen = _strategy.Read(_buffer!, 0, _bufferSize);

            bytesFromBuffer = ReadFromBuffer(buffer, offset, count);

            // We may have read less than the number of bytes the user asked for, but that is part of the Stream Debug.
            // Reading again for more data may cause us to block if we're using a device with no clear end of stream,
            // such as a serial port or pipe.  If we blocked here & this code was used with redirected pipes for a process's
            // standard output, this can lead to deadlocks involving two processes. Additionally, translating one read on the
            // BufferedStream to more than one read on the underlying Stream may defeat the whole purpose of buffering of the
            // underlying reads are significantly more expensive.

            return bytesFromBuffer + alreadySatisfied;
        }

        public override int Read(Span<byte> destination)
        {
            EnsureNotClosed();
            EnsureCanRead();

            // Try to read from the buffer.
            int bytesFromBuffer = ReadFromBuffer(destination);
            if (bytesFromBuffer == destination.Length
                && !(destination.Length == 0 && _readLen == _readPos)) // 0 bytes reads are OK only for FileStream when the read buffer is empty
            {
                // We got as many bytes as were asked for; we're done.
                return bytesFromBuffer;
            }

            // We didn't get as many bytes as were asked for from the buffer, so try filling the buffer once.

            if (bytesFromBuffer > 0)
            {
                destination = destination.Slice(bytesFromBuffer);
            }

            Debug.Assert(_readLen == _readPos, "The read buffer must now be empty");
            _readPos = _readLen = 0;

            // If there was anything in the write buffer, clear it.
            if (_writePos > 0)
            {
                FlushWrite();
            }

            if (destination.Length >= _bufferSize)
            {
                // If the requested read is larger than buffer size, avoid the buffer and just read
                // directly into the destination.
                return _strategy.Read(destination) + bytesFromBuffer;
            }
            else
            {
                // Otherwise, fill the buffer, then read from that.
                EnsureBufferAllocated();
                _readLen = _strategy.Read(_buffer!, 0, _bufferSize);
                return ReadFromBuffer(destination) + bytesFromBuffer;
            }
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
            _readLen = _strategy.Read(_buffer!, 0, _bufferSize);
            _readPos = 0;

            if (_readLen == 0)
            {
                return -1;
            }

            return _buffer![_readPos++];
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            AssertBufferArguments(buffer, offset, count);

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
            }

            ValueTask<int> readResult = ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken);

            return readResult.IsCompletedSuccessfully
                ? LastSyncCompletedReadTask(readResult.Result)
                : readResult.AsTask();
        }

        private Task<int> LastSyncCompletedReadTask(int val)
        {
            Task<int>? t = _lastSyncCompletedReadTask;
            Debug.Assert(t == null || t.IsCompletedSuccessfully);

            if (t != null && t.Result == val)
                return t;

            t = Task.FromResult<int>(val);
            _lastSyncCompletedReadTask = t;
            return t;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<int>(cancellationToken);
            }

            Debug.Assert(!_strategy.IsClosed, "Strategy.IsClosed was supposed to be validated by FileStream itself");
            EnsureCanRead();

            int bytesFromBuffer = 0;
            SemaphoreSlim sem = EnsureAsyncActiveSemaphoreInitialized();
            Task semaphoreLockTask = sem.WaitAsync(cancellationToken);
            if (semaphoreLockTask.IsCompletedSuccessfully)
            {
                // hot path #1: there is data in the buffer
                if (_readLen - _readPos > 0)
                {
                    bytesFromBuffer = ReadFromBuffer(buffer.Span);

                    if (bytesFromBuffer == buffer.Length)
                    {
                        // if above is FALSE, we will be entering ReadFromUnderlyingStreamAsync and releasing there.
                        sem.Release();

                        // If we satisfied enough data from the buffer, we can complete synchronously.
                        return new ValueTask<int>(bytesFromBuffer);
                    }

                    buffer = buffer.Slice(bytesFromBuffer);
                }
                // hot path #2: there is nothing to Flush and buffering would not be beneficial
                else if (_writePos == 0 && buffer.Length >= _bufferSize)
                {
                    Debug.Assert(_readLen == _readPos, "The read buffer must now be empty");
                    _readPos = _readLen = 0;

                    try
                    {
                        return _strategy.ReadAsync(buffer, cancellationToken);
                    }
                    finally
                    {
                        sem.Release();
                    }
                }
            }

            // Delegate to the async implementation.
            return ReadFromUnderlyingStreamAsync(buffer, cancellationToken, bytesFromBuffer, semaphoreLockTask);
        }

        /// <summary>BufferedStream should be as thin a wrapper as possible. We want ReadAsync to delegate to
        /// ReadAsync of the underlying _stream rather than calling the base Stream which implements the one in terms of the other.
        /// This allows BufferedStream to affect the semantics of the stream it wraps as little as possible. </summary>
        /// <returns>-2 if _bufferSize was set to 0 while waiting on the semaphore; otherwise num of bytes read.</returns>
        private async ValueTask<int> ReadFromUnderlyingStreamAsync(
            Memory<byte> buffer, CancellationToken cancellationToken, int bytesAlreadySatisfied, Task semaphoreLockTask)
        {
            // Same conditions validated with exceptions in ReadAsync:
            Debug.Assert(_strategy.CanRead);
            Debug.Assert(_bufferSize > 0);
            Debug.Assert(_asyncActiveSemaphore != null);
            Debug.Assert(semaphoreLockTask != null);

            // Employ async waiting based on the same synchronization used in BeginRead of the abstract Stream.
            await semaphoreLockTask.ConfigureAwait(false);

            try
            {
                int bytesFromBuffer = 0;

                if (_readLen - _readPos > 0)
                {
                    // The buffer might have been changed by another async task while we were waiting on the semaphore.
                    // Check it now again.
                    bytesFromBuffer = ReadFromBuffer(buffer.Span);
                    if (bytesFromBuffer == buffer.Length)
                    {
                        return bytesAlreadySatisfied + bytesFromBuffer;
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
                    await FlushWriteAsync(cancellationToken).ConfigureAwait(false);  // no Begin-End read version for Flush. Use Async.
                }

                // If the requested read is larger than buffer size, avoid the buffer and still use a single read:
                if (buffer.Length >= _bufferSize)
                {
                    return bytesAlreadySatisfied + await _strategy.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                }

                // Ok. We can fill the buffer:
                EnsureBufferAllocated();
                _readLen = await _strategy.ReadAsync(new Memory<byte>(_buffer, 0, _bufferSize), cancellationToken).ConfigureAwait(false);

                bytesFromBuffer = ReadFromBuffer(buffer.Span);
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
            EnsureCanWrite();

            if (_writePos == 0)
                ClearReadBufferBeforeWrite();

            #region Write algorithm comment
            // We need to use the buffer, while avoiding unnecessary buffer usage / memory copies.
            // We ASSUME that memory copies are much cheaper than writes to the underlying stream, so if an extra copy is
            // guaranteed to reduce the number of writes, we prefer it.
            // We pick a simple strategy that makes degenerate cases rare if our assumptions are right.
            //
            // For ever write, we use a simple heuristic (below) to decide whether to use the buffer.
            // The heuristic has the desirable property (*) that if the specified user data can fit into the currently available
            // buffer space without filling it up completely, the heuristic will always tell us to use the buffer. It will also
            // tell us to use the buffer in cases where the current write would fill the buffer, but the remaining data is small
            // enough such that subsequent operations can use the buffer again.
            //
            // Algorithm:
            // Determine whether or not to buffer according to the heuristic (below).
            // If we decided to use the buffer:
            //     Copy as much user data as we can into the buffer.
            //     If we consumed all data: We are finished.
            //     Otherwise, write the buffer out.
            //     Copy the rest of user data into the now cleared buffer (no need to write out the buffer again as the heuristic
            //     will prevent it from being filled twice).
            // If we decided not to use the buffer:
            //     Can the data already in the buffer and current user data be combines to a single write
            //     by allocating a "shadow" buffer of up to twice the size of _bufferSize (up to a limit to avoid LOH)?
            //     Yes, it can:
            //         Allocate a larger "shadow" buffer and ensure the buffered  data is moved there.
            //         Copy user data to the shadow buffer.
            //         Write shadow buffer to the underlying stream in a single operation.
            //     No, it cannot (amount of data is still too large):
            //         Write out any data possibly in the buffer.
            //         Write out user data directly.
            //
            // Heuristic:
            // If the subsequent write operation that follows the current write operation will result in a write to the
            // underlying stream in case that we use the buffer in the current write, while it would not have if we avoided
            // using the buffer in the current write (by writing current user data to the underlying stream directly), then we
            // prefer to avoid using the buffer since the corresponding memory copy is wasted (it will not reduce the number
            // of writes to the underlying stream, which is what we are optimising for).
            // ASSUME that the next write will be for the same amount of bytes as the current write (most common case) and
            // determine if it will cause a write to the underlying stream. If the next write is actually larger, our heuristic
            // still yields the right behaviour, if the next write is actually smaller, we may making an unnecessary write to
            // the underlying stream. However, this can only occur if the current write is larger than half the buffer size and
            // we will recover after one iteration.
            // We have:
            //     useBuffer = (_writePos + count + count < _bufferSize + _bufferSize)
            //
            // Example with _bufferSize = 20, _writePos = 6, count = 10:
            //
            //     +---------------------------------------+---------------------------------------+
            //     |             current buffer            | next iteration's "future" buffer      |
            //     +---------------------------------------+---------------------------------------+
            //     |0| | | | | | | | | |1| | | | | | | | | |2| | | | | | | | | |3| | | | | | | | | |
            //     |0|1|2|3|4|5|6|7|8|9|0|1|2|3|4|5|6|7|8|9|0|1|2|3|4|5|6|7|8|9|0|1|2|3|4|5|6|7|8|9|
            //     +-----------+-------------------+-------------------+---------------------------+
            //     | _writePos |  current count    | assumed next count|avail buff after next write|
            //     +-----------+-------------------+-------------------+---------------------------+
            //
            // A nice property (*) of this heuristic is that it will always succeed if the user data completely fits into the
            // available buffer, i.e. if count < (_bufferSize - _writePos).
            #endregion Write algorithm comment

            Debug.Assert(_writePos < _bufferSize);

            int totalUserbytes;
            bool useBuffer;
            checked
            {  // We do not expect buffer sizes big enough for an overflow, but if it happens, lets fail early:
                totalUserbytes = _writePos + count;
                useBuffer = (totalUserbytes + count < (_bufferSize + _bufferSize));
            }

            if (useBuffer)
            {
                WriteToBuffer(buffer, ref offset, ref count);

                if (_writePos < _bufferSize)
                {
                    Debug.Assert(count == 0);
                    return;
                }

                Debug.Assert(count >= 0);
                Debug.Assert(_writePos == _bufferSize);
                Debug.Assert(_buffer != null);

                _strategy.Write(_buffer, 0, _writePos);
                _writePos = 0;

                WriteToBuffer(buffer, ref offset, ref count);

                Debug.Assert(count == 0);
                Debug.Assert(_writePos < _bufferSize);
            }
            else
            {  // if (!useBuffer)
               // Write out the buffer if necessary.
                if (_writePos > 0)
                {
                    Debug.Assert(_buffer != null);
                    Debug.Assert(totalUserbytes >= _bufferSize);

                    // Try avoiding extra write to underlying stream by combining previously buffered data with current user data:
                    if (totalUserbytes <= (_bufferSize + _bufferSize) && totalUserbytes <= MaxShadowBufferSize)
                    {
                        EnsureShadowBufferAllocated();
                        Buffer.BlockCopy(buffer, offset, _buffer, _writePos, count);
                        _strategy.Write(_buffer, 0, totalUserbytes);
                        _writePos = 0;
                        return;
                    }

                    _strategy.Write(_buffer, 0, _writePos);
                    _writePos = 0;
                }

                // Write out user data.
                _strategy.Write(buffer, offset, count);
            }
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            EnsureNotClosed();
            EnsureCanWrite();

            if (_writePos == 0)
            {
                ClearReadBufferBeforeWrite();
            }
            Debug.Assert(_writePos < _bufferSize, $"Expected {_writePos} < {_bufferSize}");

            int totalUserbytes;
            bool useBuffer;
            checked
            {
                // We do not expect buffer sizes big enough for an overflow, but if it happens, lets fail early:
                totalUserbytes = _writePos + buffer.Length;
                useBuffer = (totalUserbytes + buffer.Length < (_bufferSize + _bufferSize));
            }

            if (useBuffer)
            {
                // Copy as much data to the buffer as will fit.  If there's still room in the buffer,
                // everything must have fit.
                int bytesWritten = WriteToBuffer(buffer);
                if (_writePos < _bufferSize)
                {
                    Debug.Assert(bytesWritten == buffer.Length);
                    return;
                }
                buffer = buffer.Slice(bytesWritten);

                Debug.Assert(_writePos == _bufferSize);
                Debug.Assert(_buffer != null);

                // Output the buffer to the underlying strategy.
                _strategy.Write(_buffer, 0, _writePos);
                _writePos = 0;

                // Now write the remainder.  It must fit, as we're only on this path if that's true.
                bytesWritten = WriteToBuffer(buffer);
                Debug.Assert(bytesWritten == buffer.Length);

                Debug.Assert(_writePos < _bufferSize);
            }
            else // skip the buffer
            {
                // Flush anything existing in the buffer.
                if (_writePos > 0)
                {
                    Debug.Assert(_buffer != null);
                    Debug.Assert(totalUserbytes >= _bufferSize);

                    // Try avoiding extra write to underlying stream by combining previously buffered data with current user data:
                    if (totalUserbytes <= (_bufferSize + _bufferSize) && totalUserbytes <= MaxShadowBufferSize)
                    {
                        EnsureShadowBufferAllocated();
                        buffer.CopyTo(new Span<byte>(_buffer, _writePos, buffer.Length));
                        _strategy.Write(_buffer, 0, totalUserbytes);
                        _writePos = 0;
                        return;
                    }

                    _strategy.Write(_buffer, 0, _writePos);
                    _writePos = 0;
                }

                // Write out user data.
                _strategy.Write(buffer);
            }
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
            EnsureNotClosed();

            if (_writePos == 0)
            {
                EnsureCanWrite();
                ClearReadBufferBeforeWrite();
                EnsureBufferAllocated();
            }

            if (_writePos >= _bufferSize - 1)
                FlushWrite();

            _buffer![_writePos++] = value;

            Debug.Assert(_writePos < _bufferSize);
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

            EnsureNotClosed();
            EnsureCanWrite();

            // Try to satisfy the request from the buffer synchronously.
            SemaphoreSlim sem = EnsureAsyncActiveSemaphoreInitialized();
            Task semaphoreLockTask = sem.WaitAsync(cancellationToken);
            if (semaphoreLockTask.IsCompletedSuccessfully)
            {
                bool completeSynchronously = true;
                try
                {
                    if (_writePos == 0)
                    {
                        ClearReadBufferBeforeWrite();
                    }

                    Debug.Assert(_writePos < _bufferSize);

                    // hot path #1 If the write completely fits into the buffer, we can complete synchronously:
                    completeSynchronously = buffer.Length < _bufferSize - _writePos;
                    if (completeSynchronously)
                    {
                        int bytesWritten = WriteToBuffer(buffer.Span);
                        Debug.Assert(bytesWritten == buffer.Length);
                        return default;
                    }
                }
                finally
                {
                    if (completeSynchronously)  // if this is FALSE, we will be entering WriteToUnderlyingStreamAsync and releasing there.
                    {
                        sem.Release();
                    }
                }

                // hot path #2: there is nothing to Flush and buffering would not be beneficial
                if (_writePos == 0 && buffer.Length >= _bufferSize)
                {
                    try
                    {
                        return _strategy.WriteAsync(buffer, cancellationToken);
                    }
                    finally
                    {
                        sem.Release();
                    }
                }
            }

            // Delegate to the async implementation.
            return WriteToUnderlyingStreamAsync(buffer, cancellationToken, semaphoreLockTask);
        }

        /// <summary>BufferedStream should be as thin a wrapper as possible. We want WriteAsync to delegate to
        /// WriteAsync of the underlying _stream rather than calling the base Stream which implements the one
        /// in terms of the other. This allows BufferedStream to affect the semantics of the stream it wraps as
        /// little as possible.
        /// </summary>
        private async ValueTask WriteToUnderlyingStreamAsync(
            ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken, Task semaphoreLockTask)
        {
            Debug.Assert(_strategy.CanWrite);
            Debug.Assert(_bufferSize > 0);
            Debug.Assert(_asyncActiveSemaphore != null);
            Debug.Assert(semaphoreLockTask != null);

            // See the LARGE COMMENT in Write(..) for the explanation of the write buffer algorithm.

            await semaphoreLockTask.ConfigureAwait(false);
            try
            {
                // The buffer might have been changed by another async task while we were waiting on the semaphore.
                // However, note that if we recalculate the sync completion condition to TRUE, then useBuffer will also be TRUE.

                if (_writePos == 0)
                {
                    ClearReadBufferBeforeWrite();
                }

                int totalUserBytes;
                bool useBuffer;
                checked
                {
                    // We do not expect buffer sizes big enough for an overflow, but if it happens, lets fail early:
                    totalUserBytes = _writePos + buffer.Length;
                    useBuffer = (totalUserBytes + buffer.Length < (_bufferSize + _bufferSize));
                }

                if (useBuffer)
                {
                    buffer = buffer.Slice(WriteToBuffer(buffer.Span));

                    if (_writePos < _bufferSize)
                    {
                        Debug.Assert(buffer.Length == 0);
                        return;
                    }

                    Debug.Assert(buffer.Length >= 0);
                    Debug.Assert(_writePos == _bufferSize);
                    Debug.Assert(_buffer != null);

                    await _strategy.WriteAsync(new ReadOnlyMemory<byte>(_buffer, 0, _writePos), cancellationToken).ConfigureAwait(false);
                    _writePos = 0;

                    int bytesWritten = WriteToBuffer(buffer.Span);
                    Debug.Assert(bytesWritten == buffer.Length);

                    Debug.Assert(_writePos < _bufferSize);

                }
                else // !useBuffer
                {
                    // Write out the buffer if necessary.
                    if (_writePos > 0)
                    {
                        Debug.Assert(_buffer != null);
                        Debug.Assert(totalUserBytes >= _bufferSize);

                        // Try avoiding extra write to underlying stream by combining previously buffered data with current user data:
                        if (totalUserBytes <= (_bufferSize + _bufferSize) && totalUserBytes <= MaxShadowBufferSize)
                        {
                            EnsureShadowBufferAllocated();
                            buffer.Span.CopyTo(new Span<byte>(_buffer, _writePos, buffer.Length));

                            await _strategy.WriteAsync(new ReadOnlyMemory<byte>(_buffer, 0, totalUserBytes), cancellationToken).ConfigureAwait(false);
                            _writePos = 0;
                            return;
                        }

                        await _strategy.WriteAsync(new ReadOnlyMemory<byte>(_buffer, 0, _writePos), cancellationToken).ConfigureAwait(false);
                        _writePos = 0;
                    }

                    // Write out user data.
                    await _strategy.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
                }
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
                    await FlushWriteAsync(cancellationToken).ConfigureAwait(false);
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
            ValidateCopyToArguments(destination, bufferSize);
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
                    await FlushWriteAsync(cancellationToken).ConfigureAwait(false);
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
            ValidateCopyToArguments(destination, bufferSize);
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

            Debug.Assert(newPos == Position, "newPos (=" + newPos + ") == Position (=" + Position + ")");
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

        private async ValueTask FlushWriteAsync(CancellationToken cancellationToken)
        {
            Debug.Assert(_readPos == 0 && _readLen == 0, "Read buffer must be empty in FlushWriteAsync!");
            Debug.Assert(_buffer != null && _bufferSize >= _writePos, "Write buffer must be allocated and write position must be in the bounds of the buffer in FlushWriteAsync!");

            // TODO: we might get rid of the await
            await _strategy.WriteAsync(new ReadOnlyMemory<byte>(_buffer, 0, _writePos), cancellationToken).ConfigureAwait(false);
            _writePos = 0;
        }

        private int ReadFromBuffer(byte[] buffer, int offset, int count)
        {
            int readbytes = _readLen - _readPos;
            Debug.Assert(readbytes >= 0);

            if (readbytes == 0)
            {
                return 0;
            }

            if (readbytes > count)
            {
                readbytes = count;
            }

            Buffer.BlockCopy(_buffer!, _readPos, buffer, offset, readbytes);
            _readPos += readbytes;

            return readbytes;
        }

        private int ReadFromBuffer(Span<byte> destination)
        {
            int readbytes = Math.Min(_readLen - _readPos, destination.Length);
            Debug.Assert(readbytes >= 0);
            if (readbytes > 0)
            {
                new ReadOnlySpan<byte>(_buffer, _readPos, readbytes).CopyTo(destination);
                _readPos += readbytes;
            }
            return readbytes;
        }

        private void WriteToBuffer(byte[] buffer, ref int offset, ref int count)
        {
            int bytesToWrite = Math.Min(_bufferSize - _writePos, count);

            if (bytesToWrite <= 0)
            {
                return;
            }

            EnsureBufferAllocated();
            Buffer.BlockCopy(buffer, offset, _buffer!, _writePos, bytesToWrite);

            _writePos += bytesToWrite;
            count -= bytesToWrite;
            offset += bytesToWrite;
        }

        private int WriteToBuffer(ReadOnlySpan<byte> buffer)
        {
            int bytesToWrite = Math.Min(_bufferSize - _writePos, buffer.Length);
            if (bytesToWrite > 0)
            {
                EnsureBufferAllocated();
                buffer.Slice(0, bytesToWrite).CopyTo(new Span<byte>(_buffer, _writePos, bytesToWrite));
                _writePos += bytesToWrite;
            }
            return bytesToWrite;
        }

        /// <summary>
        /// Called by Write methods to clear the Read Buffer
        /// </summary>
        private void ClearReadBufferBeforeWrite()
        {
            Debug.Assert(_readPos <= _readLen, "_readPos <= _readLen [" + _readPos + " <= " + _readLen + "]");

            // No read data in the buffer:
            if (_readPos == _readLen)
            {
                _readPos = _readLen = 0;
                return;
            }

            // Must have read data.
            Debug.Assert(_readPos < _readLen);

            // If the underlying stream cannot seek, FlushRead would end up throwing NotSupported.
            // However, since the user did not call a method that is intuitively expected to seek, a better message is in order.
            // Ideally, we would throw an InvalidOperation here, but for backward compat we have to stick with NotSupported.
            if (!_strategy.CanSeek)
                ThrowNotSupported_CannotWriteToBufferedStreamIfReadBufferCannotBeFlushed();

            FlushRead();

            static void ThrowNotSupported_CannotWriteToBufferedStreamIfReadBufferCannotBeFlushed()
                => throw new NotSupportedException(SR.NotSupported_CannotWriteToBufferedStreamIfReadBufferCannotBeFlushed);
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

        private void EnsureBufferAllocated()
        {
            Debug.Assert(_bufferSize > 0);

            // BufferedFileStreamStrategy is not intended for multi-threaded use, so no worries about the get/set race on _buffer.
            if (_buffer == null)
            {
                AllocateBuffer();
            }

            void AllocateBuffer() // logic kept in a separate method to get EnsureBufferAllocated() inlined
            {
                _strategy.OnBufferAllocated(_buffer = new byte[_bufferSize]);
            }
        }

        private void EnsureShadowBufferAllocated()
        {
            Debug.Assert(_buffer != null);
            Debug.Assert(_bufferSize > 0);

            // Already have a shadow buffer?
            // Or is the user-specified buffer size already so large that we don't want to create one?
            if (_buffer.Length != _bufferSize || _bufferSize >= MaxShadowBufferSize)
                return;

            byte[] shadowBuffer = new byte[Math.Min(_bufferSize + _bufferSize, MaxShadowBufferSize)];
            Buffer.BlockCopy(_buffer, 0, shadowBuffer, 0, _writePos);
            _buffer = shadowBuffer;
        }

        [Conditional("DEBUG")]
        private void AssertBufferArguments(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count); // FileStream is supposed to call this
            Debug.Assert(!_strategy.IsClosed, "Strategy.IsClosed was supposed to be validated by FileStream itself");
        }
    }
}
