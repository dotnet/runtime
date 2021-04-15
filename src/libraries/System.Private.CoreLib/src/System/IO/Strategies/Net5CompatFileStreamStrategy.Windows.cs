// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

/*
 * Win32FileStream supports different modes of accessing the disk - async mode
 * and sync mode.  They are two completely different codepaths in the
 * sync & async methods (i.e. Read/Write vs. ReadAsync/WriteAsync).  File
 * handles in NT can be opened in only sync or overlapped (async) mode,
 * and we have to deal with this pain.  Stream has implementations of
 * the sync methods in terms of the async ones, so we'll
 * call through to our base class to get those methods when necessary.
 *
 * Also buffering is added into Win32FileStream as well. Folded in the
 * code from BufferedStream, so all the comments about it being mostly
 * aggressive (and the possible perf improvement) apply to Win32FileStream as
 * well.  Also added some buffering to the async code paths.
 *
 * Class Invariants:
 * The class has one buffer, shared for reading & writing.  It can only be
 * used for one or the other at any point in time - not both.  The following
 * should be true:
 *   0 <= _readPos <= _readLen < _bufferSize
 *   0 <= _writePos < _bufferSize
 *   _readPos == _readLen && _readPos > 0 implies the read buffer is valid,
 *     but we're at the end of the buffer.
 *   _readPos == _readLen == 0 means the read buffer contains garbage.
 *   Either _writePos can be greater than 0, or _readLen & _readPos can be
 *     greater than zero, but neither can be greater than zero at the same time.
 *
 */

namespace System.IO.Strategies
{
    internal sealed partial class Net5CompatFileStreamStrategy : FileStreamStrategy
    {
        private bool _canSeek;
        private bool _isPipe;      // Whether to disable async buffering code.
        private long _appendStart; // When appending, prevent overwriting file.

        private Task _activeBufferOperation = Task.CompletedTask;    // tracks in-progress async ops using the buffer
        private PreAllocatedOverlapped? _preallocatedOverlapped;     // optimization for async ops to avoid per-op allocations
        private CompletionSource? _currentOverlappedOwner; // async op currently using the preallocated overlapped

        private void Init(FileMode mode, FileShare share, string originalPath, FileOptions options)
        {
            FileStreamHelpers.ValidateFileTypeForNonExtendedPaths(_fileHandle, originalPath);

            // This is necessary for async IO using IO Completion ports via our
            // managed Threadpool API's.  This (theoretically) calls the OS's
            // BindIoCompletionCallback method, and passes in a stub for the
            // LPOVERLAPPED_COMPLETION_ROUTINE.  This stub looks at the Overlapped
            // struct for this request and gets a delegate to a managed callback
            // from there, which it then calls on a threadpool thread.  (We allocate
            // our native OVERLAPPED structs 2 pointers too large and store EE state
            // & GC handles there, one to an IAsyncResult, the other to a delegate.)
            if (_useAsyncIO)
            {
                try
                {
                    _fileHandle.ThreadPoolBinding = ThreadPoolBoundHandle.BindHandle(_fileHandle);
                }
                catch (ArgumentException ex)
                {
                    throw new IOException(SR.IO_BindHandleFailed, ex);
                }
                finally
                {
                    if (_fileHandle.ThreadPoolBinding == null)
                    {
                        // We should close the handle so that the handle is not open until SafeFileHandle GC
                        Debug.Assert(!_exposedHandle, "Are we closing handle that we exposed/not own, how?");
                        _fileHandle.Dispose();
                    }
                }
            }

            _canSeek = true;

            // For Append mode...
            if (mode == FileMode.Append)
            {
                _appendStart = SeekCore(_fileHandle, 0, SeekOrigin.End);
            }
            else
            {
                _appendStart = -1;
            }
        }

        private void InitFromHandle(SafeFileHandle handle, FileAccess access, bool useAsyncIO)
        {
#if DEBUG
            bool hadBinding = handle.ThreadPoolBinding != null;

            try
            {
#endif
                InitFromHandleImpl(handle, useAsyncIO);
#if DEBUG
            }
            catch
            {
                Debug.Assert(hadBinding || handle.ThreadPoolBinding == null, "We should never error out with a ThreadPoolBinding we've added");
                throw;
            }
#endif
        }

        private void InitFromHandleImpl(SafeFileHandle handle, bool useAsyncIO)
        {
            FileStreamHelpers.GetFileTypeSpecificInformation(handle, out _canSeek, out _isPipe);

            // This is necessary for async IO using IO Completion ports via our
            // managed Threadpool API's.  This calls the OS's
            // BindIoCompletionCallback method, and passes in a stub for the
            // LPOVERLAPPED_COMPLETION_ROUTINE.  This stub looks at the Overlapped
            // struct for this request and gets a delegate to a managed callback
            // from there, which it then calls on a threadpool thread.  (We allocate
            // our native OVERLAPPED structs 2 pointers too large and store EE
            // state & a handle to a delegate there.)
            //
            // If, however, we've already bound this file handle to our completion port,
            // don't try to bind it again because it will fail.  A handle can only be
            // bound to a single completion port at a time.
            if (useAsyncIO && !(handle.IsAsync ?? false))
            {
                try
                {
                    handle.ThreadPoolBinding = ThreadPoolBoundHandle.BindHandle(handle);
                }
                catch (Exception ex)
                {
                    // If you passed in a synchronous handle and told us to use
                    // it asynchronously, throw here.
                    throw new ArgumentException(SR.Arg_HandleNotAsync, nameof(handle), ex);
                }
            }
            else if (!useAsyncIO)
            {
                FileStreamHelpers.VerifyHandleIsSync(handle);
            }

            if (_canSeek)
                SeekCore(handle, 0, SeekOrigin.Current);
            else
                _filePosition = 0;
        }

        private bool HasActiveBufferOperation => !_activeBufferOperation.IsCompleted;

        public override bool CanSeek => _canSeek;

        public unsafe override long Length
        {
            get
            {
                long len = FileStreamHelpers.GetFileLength(_fileHandle, _path);

                // If we're writing near the end of the file, we must include our
                // internal buffer in our Length calculation.  Don't flush because
                // we use the length of the file in our async write method.
                if (_writePos > 0 && _filePosition + _writePos > len)
                    len = _writePos + _filePosition;

                return len;
            }
        }

        protected override void Dispose(bool disposing)
        {
            // Nothing will be done differently based on whether we are
            // disposing vs. finalizing.  This is taking advantage of the
            // weak ordering between normal finalizable objects & critical
            // finalizable objects, which I included in the SafeHandle
            // design for Win32FileStream, which would often "just work" when
            // finalized.
            try
            {
                if (_fileHandle != null && !_fileHandle.IsClosed && _writePos > 0)
                {
                    // Flush data to disk iff we were writing.  After
                    // thinking about this, we also don't need to flush
                    // our read position, regardless of whether the handle
                    // was exposed to the user.  They probably would NOT
                    // want us to do this.
                    try
                    {
                        FlushWriteBuffer(!disposing);
                    }
                    catch (Exception e) when (!disposing && FileStreamHelpers.IsIoRelatedException(e))
                    {
                        // On finalization, ignore failures from trying to flush the write buffer,
                        // e.g. if this stream is wrapping a pipe and the pipe is now broken.
                    }
                }
            }
            finally
            {
                if (_fileHandle != null && !_fileHandle.IsClosed)
                {
                    _fileHandle.ThreadPoolBinding?.Dispose();
                    _fileHandle.Dispose();
                }

                _preallocatedOverlapped?.Dispose();
                _canSeek = false;

                // Don't set the buffer to null, to avoid a NullReferenceException
                // when users have a race condition in their code (i.e. they call
                // Close when calling another method on Stream like Read).
            }
        }

        public override async ValueTask DisposeAsync()
        {
            // Same logic as in Dispose(), except with async counterparts.
            // TODO: https://github.com/dotnet/runtime/issues/27643: FlushAsync does synchronous work.
            try
            {
                if (_fileHandle != null && !_fileHandle.IsClosed && _writePos > 0)
                {
                    await FlushAsync(default).ConfigureAwait(false);
                }
            }
            finally
            {
                if (_fileHandle != null && !_fileHandle.IsClosed)
                {
                    _fileHandle.ThreadPoolBinding?.Dispose();
                    _fileHandle.Dispose();
                }

                _preallocatedOverlapped?.Dispose();
                _canSeek = false;
                GC.SuppressFinalize(this); // the handle is closed; nothing further for the finalizer to do
            }
        }

        private void FlushOSBuffer() => FileStreamHelpers.FlushToDisk(_fileHandle, _path);

        // Returns a task that flushes the internal write buffer
        private Task FlushWriteAsync(CancellationToken cancellationToken)
        {
            Debug.Assert(_useAsyncIO);
            Debug.Assert(_readPos == 0 && _readLength == 0, "FileStream: Read buffer must be empty in FlushWriteAsync!");

            // If the buffer is already flushed, don't spin up the OS write
            if (_writePos == 0) return Task.CompletedTask;

            Task flushTask = WriteAsyncInternalCore(new ReadOnlyMemory<byte>(GetBuffer(), 0, _writePos), cancellationToken);
            _writePos = 0;

            // Update the active buffer operation
            _activeBufferOperation = HasActiveBufferOperation ?
                Task.WhenAll(_activeBufferOperation, flushTask) :
                flushTask;

            return flushTask;
        }

        private void FlushWriteBufferForWriteByte() => FlushWriteBuffer();

        // Writes are buffered.  Anytime the buffer fills up
        // (_writePos + delta > _bufferSize) or the buffer switches to reading
        // and there is left over data (_writePos > 0), this function must be called.
        private void FlushWriteBuffer(bool calledFromFinalizer = false)
        {
            if (_writePos == 0) return;
            Debug.Assert(_readPos == 0 && _readLength == 0, "FileStream: Read buffer must be empty in FlushWrite!");

            if (_useAsyncIO)
            {
                Task writeTask = FlushWriteAsync(CancellationToken.None);
                // With our Whidbey async IO & overlapped support for AD unloads,
                // we don't strictly need to block here to release resources
                // since that support takes care of the pinning & freeing the
                // overlapped struct.  We need to do this when called from
                // Close so that the handle is closed when Close returns, but
                // we don't need to call EndWrite from the finalizer.
                // Additionally, if we do call EndWrite, we block forever
                // because AD unloads prevent us from running the managed
                // callback from the IO completion port.  Blocking here when
                // called from the finalizer during AD unload is clearly wrong,
                // but we can't use any sort of test for whether the AD is
                // unloading because if we weren't unloading, an AD unload
                // could happen on a separate thread before we call EndWrite.
                if (!calledFromFinalizer)
                {
                    writeTask.GetAwaiter().GetResult();
                }
            }
            else
            {
                WriteCore(new ReadOnlySpan<byte>(GetBuffer(), 0, _writePos));
            }

            _writePos = 0;
        }

        public override void SetLength(long value)
        {
            // Handle buffering updates.
            if (_writePos > 0)
            {
                FlushWriteBuffer();
            }
            else if (_readPos < _readLength)
            {
                FlushReadBuffer();
            }
            _readPos = 0;
            _readLength = 0;

            if (_appendStart != -1 && value < _appendStart)
                throw new IOException(SR.IO_SetLengthAppendTruncate);
            SetLengthCore(value);
        }

        // We absolutely need this method broken out so that WriteInternalCoreAsync can call
        // a method without having to go through buffering code that might call FlushWrite.
        private unsafe void SetLengthCore(long value)
        {
            Debug.Assert(value >= 0, "value >= 0");
            VerifyOSHandlePosition();

            FileStreamHelpers.SetFileLength(_fileHandle, _path, value);

            if (_filePosition > value)
            {
                SeekCore(_fileHandle, 0, SeekOrigin.End);
            }
        }

        private int ReadSpan(Span<byte> destination)
        {
            Debug.Assert(!_useAsyncIO, "Must only be used when in synchronous mode");
            Debug.Assert((_readPos == 0 && _readLength == 0 && _writePos >= 0) || (_writePos == 0 && _readPos <= _readLength),
                "We're either reading or writing, but not both.");

            bool isBlocked = false;
            int n = _readLength - _readPos;
            // if the read buffer is empty, read into either user's array or our
            // buffer, depending on number of bytes user asked for and buffer size.
            if (n == 0)
            {
                if (!CanRead) ThrowHelper.ThrowNotSupportedException_UnreadableStream();
                if (_writePos > 0) FlushWriteBuffer();
                if (!CanSeek || (destination.Length >= _bufferLength))
                {
                    n = ReadNative(destination);
                    // Throw away read buffer.
                    _readPos = 0;
                    _readLength = 0;
                    return n;
                }
                n = ReadNative(GetBuffer());
                if (n == 0) return 0;
                isBlocked = n < _bufferLength;
                _readPos = 0;
                _readLength = n;
            }
            // Now copy min of count or numBytesAvailable (i.e. near EOF) to array.
            if (n > destination.Length) n = destination.Length;
            new ReadOnlySpan<byte>(GetBuffer(), _readPos, n).CopyTo(destination);
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
            if (!_isPipe)
            {
                // If we hit the end of the buffer and didn't have enough bytes, we must
                // read some more from the underlying stream.  However, if we got
                // fewer bytes from the underlying stream than we asked for (i.e. we're
                // probably blocked), don't ask for more bytes.
                if (n < destination.Length && !isBlocked)
                {
                    Debug.Assert(_readPos == _readLength, "Read buffer should be empty!");
                    int moreBytesRead = ReadNative(destination.Slice(n));
                    n += moreBytesRead;
                    // We've just made our buffer inconsistent with our position
                    // pointer.  We must throw away the read buffer.
                    _readPos = 0;
                    _readLength = 0;
                }
            }

            return n;
        }

        [Conditional("DEBUG")]
        private void AssertCanRead()
        {
            Debug.Assert(!_fileHandle.IsClosed, "!_fileHandle.IsClosed");
            Debug.Assert(CanRead, "CanRead");
        }

        /// <summary>Reads from the file handle into the buffer, overwriting anything in it.</summary>
        private int FillReadBufferForReadByte() =>
            _useAsyncIO ?
                ReadNativeAsync(new Memory<byte>(_buffer), 0, CancellationToken.None).GetAwaiter().GetResult() :
                ReadNative(_buffer);

        private unsafe int ReadNative(Span<byte> buffer)
        {
            Debug.Assert(!_useAsyncIO, $"{nameof(ReadNative)} doesn't work on asynchronous file streams.");
            AssertCanRead();

            // Make sure we are reading from the right spot
            VerifyOSHandlePosition();

            int r = ReadFileNative(_fileHandle, buffer, null, out int errorCode);

            if (r == -1)
            {
                // For pipes, ERROR_BROKEN_PIPE is the normal end of the pipe.
                if (errorCode == Interop.Errors.ERROR_BROKEN_PIPE)
                {
                    r = 0;
                }
                else
                {
                    if (errorCode == Interop.Errors.ERROR_INVALID_PARAMETER)
                        ThrowHelper.ThrowArgumentException_HandleNotSync(nameof(_fileHandle));

                    throw Win32Marshal.GetExceptionForWin32Error(errorCode, _path);
                }
            }
            Debug.Assert(r >= 0, "FileStream's ReadNative is likely broken.");
            _filePosition += r;

            return r;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin < SeekOrigin.Begin || origin > SeekOrigin.End)
                throw new ArgumentException(SR.Argument_InvalidSeekOrigin, nameof(origin));
            if (_fileHandle.IsClosed) ThrowHelper.ThrowObjectDisposedException_FileClosed();
            if (!CanSeek) ThrowHelper.ThrowNotSupportedException_UnseekableStream();

            Debug.Assert((_readPos == 0 && _readLength == 0 && _writePos >= 0) || (_writePos == 0 && _readPos <= _readLength), "We're either reading or writing, but not both.");

            // If we've got bytes in our buffer to write, write them out.
            // If we've read in and consumed some bytes, we'll have to adjust
            // our seek positions ONLY IF we're seeking relative to the current
            // position in the stream.  This simulates doing a seek to the new
            // position, then a read for the number of bytes we have in our buffer.
            if (_writePos > 0)
            {
                FlushWriteBuffer();
            }
            else if (origin == SeekOrigin.Current)
            {
                // Don't call FlushRead here, which would have caused an infinite
                // loop.  Simply adjust the seek origin.  This isn't necessary
                // if we're seeking relative to the beginning or end of the stream.
                offset -= (_readLength - _readPos);
            }
            _readPos = _readLength = 0;

            // Verify that internal position is in sync with the handle
            VerifyOSHandlePosition();

            long oldPos = _filePosition + (_readPos - _readLength);
            long pos = SeekCore(_fileHandle, offset, origin);

            // Prevent users from overwriting data in a file that was opened in
            // append mode.
            if (_appendStart != -1 && pos < _appendStart)
            {
                SeekCore(_fileHandle, oldPos, SeekOrigin.Begin);
                throw new IOException(SR.IO_SeekAppendOverwrite);
            }

            // We now must update the read buffer.  We can in some cases simply
            // update _readPos within the buffer, copy around the buffer so our
            // Position property is still correct, and avoid having to do more
            // reads from the disk.  Otherwise, discard the buffer's contents.
            if (_readLength > 0)
            {
                // We can optimize the following condition:
                // oldPos - _readPos <= pos < oldPos + _readLen - _readPos
                if (oldPos == pos)
                {
                    if (_readPos > 0)
                    {
                        Buffer.BlockCopy(GetBuffer(), _readPos, GetBuffer(), 0, _readLength - _readPos);
                        _readLength -= _readPos;
                        _readPos = 0;
                    }
                    // If we still have buffered data, we must update the stream's
                    // position so our Position property is correct.
                    if (_readLength > 0)
                        SeekCore(_fileHandle, _readLength, SeekOrigin.Current);
                }
                else if (oldPos - _readPos < pos && pos < oldPos + _readLength - _readPos)
                {
                    int diff = (int)(pos - oldPos);
                    Buffer.BlockCopy(GetBuffer(), _readPos + diff, GetBuffer(), 0, _readLength - (_readPos + diff));
                    _readLength -= (_readPos + diff);
                    _readPos = 0;
                    if (_readLength > 0)
                        SeekCore(_fileHandle, _readLength, SeekOrigin.Current);
                }
                else
                {
                    // Lose the read buffer.
                    _readPos = 0;
                    _readLength = 0;
                }
                Debug.Assert(_readLength >= 0 && _readPos <= _readLength, "_readLen should be nonnegative, and _readPos should be less than or equal _readLen");
                Debug.Assert(pos == Position, "Seek optimization: pos != Position!  Buffer math was mangled.");
            }
            return pos;
        }

        // This doesn't do argument checking.  Necessary for SetLength, which must
        // set the file pointer beyond the end of the file. This will update the
        // internal position
        private long SeekCore(SafeFileHandle fileHandle, long offset, SeekOrigin origin, bool closeInvalidHandle = false)
        {
            Debug.Assert(!fileHandle.IsClosed && _canSeek, "!fileHandle.IsClosed && _canSeek");

            return _filePosition = FileStreamHelpers.Seek(fileHandle, _path, offset, origin, closeInvalidHandle);
        }

        partial void OnBufferAllocated()
        {
            Debug.Assert(_buffer != null);
            Debug.Assert(_preallocatedOverlapped == null);

            if (_useAsyncIO)
                _preallocatedOverlapped = new PreAllocatedOverlapped(CompletionSource.s_ioCallback, this, _buffer);
        }

        private CompletionSource? CompareExchangeCurrentOverlappedOwner(CompletionSource? newSource, CompletionSource? existingSource)
            => Interlocked.CompareExchange(ref _currentOverlappedOwner, newSource, existingSource);

        private void WriteSpan(ReadOnlySpan<byte> source)
        {
            Debug.Assert(!_useAsyncIO, "Must only be used when in synchronous mode");

            if (_writePos == 0)
            {
                // Ensure we can write to the stream, and ready buffer for writing.
                if (!CanWrite) ThrowHelper.ThrowNotSupportedException_UnwritableStream();
                if (_readPos < _readLength) FlushReadBuffer();
                _readPos = 0;
                _readLength = 0;
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
                int numBytes = _bufferLength - _writePos;   // space left in buffer
                if (numBytes > 0)
                {
                    if (numBytes >= source.Length)
                    {
                        source.CopyTo(GetBuffer().AsSpan(_writePos));
                        _writePos += source.Length;
                        return;
                    }
                    else
                    {
                        source.Slice(0, numBytes).CopyTo(GetBuffer().AsSpan(_writePos));
                        _writePos += numBytes;
                        source = source.Slice(numBytes);
                    }
                }
                // Reset our buffer.  We essentially want to call FlushWrite
                // without calling Flush on the underlying Stream.

                WriteCore(new ReadOnlySpan<byte>(GetBuffer(), 0, _writePos));
                _writePos = 0;
            }

            // If the buffer would slow writes down, avoid buffer completely.
            if (source.Length >= _bufferLength)
            {
                Debug.Assert(_writePos == 0, "FileStream cannot have buffered data to write here!  Your stream will be corrupted.");
                WriteCore(source);
                return;
            }
            else if (source.Length == 0)
            {
                return;  // Don't allocate a buffer then call memcpy for 0 bytes.
            }

            // Copy remaining bytes into buffer, to write at a later date.
            source.CopyTo(GetBuffer().AsSpan(_writePos));
            _writePos = source.Length;
            return;
        }

        private unsafe void WriteCore(ReadOnlySpan<byte> source)
        {
            Debug.Assert(!_useAsyncIO);
            Debug.Assert(!_fileHandle.IsClosed, "!_handle.IsClosed");
            Debug.Assert(CanWrite, "_parent.CanWrite");
            Debug.Assert(_readPos == _readLength, "_readPos == _readLen");

            // Make sure we are writing to the position that we think we are
            VerifyOSHandlePosition();

            int r = WriteFileNative(_fileHandle, source, null, out int errorCode);

            if (r == -1)
            {
                // For pipes, ERROR_NO_DATA is not an error, but the pipe is closing.
                if (errorCode == Interop.Errors.ERROR_NO_DATA)
                {
                    r = 0;
                }
                else
                {
                    // ERROR_INVALID_PARAMETER may be returned for writes
                    // where the position is too large or for synchronous writes
                    // to a handle opened asynchronously.
                    if (errorCode == Interop.Errors.ERROR_INVALID_PARAMETER)
                        throw new IOException(SR.IO_FileTooLongOrHandleNotSync);
                    throw Win32Marshal.GetExceptionForWin32Error(errorCode, _path);
                }
            }
            Debug.Assert(r >= 0, "FileStream's WriteCore is likely broken.");
            _filePosition += r;
            return;
        }

        private Task<int>? ReadAsyncInternal(Memory<byte> destination, CancellationToken cancellationToken, out int synchronousResult)
        {
            Debug.Assert(_useAsyncIO);
            if (!CanRead) ThrowHelper.ThrowNotSupportedException_UnreadableStream();

            Debug.Assert((_readPos == 0 && _readLength == 0 && _writePos >= 0) || (_writePos == 0 && _readPos <= _readLength), "We're either reading or writing, but not both.");

            if (_isPipe)
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
                if (_readPos < _readLength)
                {
                    int n = Math.Min(_readLength - _readPos, destination.Length);
                    new Span<byte>(GetBuffer(), _readPos, n).CopyTo(destination.Span);
                    _readPos += n;
                    synchronousResult = n;
                    return null;
                }
                else
                {
                    Debug.Assert(_writePos == 0, "Win32FileStream must not have buffered write data here!  Pipes should be unidirectional.");
                    synchronousResult = 0;
                    return ReadNativeAsync(destination, 0, cancellationToken);
                }
            }

            Debug.Assert(!_isPipe, "Should not be a pipe.");

            // Handle buffering.
            if (_writePos > 0) FlushWriteBuffer();
            if (_readPos == _readLength)
            {
                // I can't see how to handle buffering of async requests when
                // filling the buffer asynchronously, without a lot of complexity.
                // The problems I see are issuing an async read, we do an async
                // read to fill the buffer, then someone issues another read
                // (either synchronously or asynchronously) before the first one
                // returns.  This would involve some sort of complex buffer locking
                // that we probably don't want to get into, at least not in V1.
                // If we did a sync read to fill the buffer, we could avoid the
                // problem, and any async read less than 64K gets turned into a
                // synchronous read by NT anyways...       --

                if (destination.Length < _bufferLength)
                {
                    Task<int> readTask = ReadNativeAsync(new Memory<byte>(GetBuffer()), 0, cancellationToken);
                    _readLength = readTask.GetAwaiter().GetResult();
                    int n = Math.Min(_readLength, destination.Length);
                    new Span<byte>(GetBuffer(), 0, n).CopyTo(destination.Span);
                    _readPos = n;

                    synchronousResult = n;
                    return null;
                }
                else
                {
                    // Here we're making our position pointer inconsistent
                    // with our read buffer.  Throw away the read buffer's contents.
                    _readPos = 0;
                    _readLength = 0;
                    synchronousResult = 0;
                    return ReadNativeAsync(destination, 0, cancellationToken);
                }
            }
            else
            {
                int n = Math.Min(_readLength - _readPos, destination.Length);
                new Span<byte>(GetBuffer(), _readPos, n).CopyTo(destination.Span);
                _readPos += n;

                if (n == destination.Length)
                {
                    // Return a completed task
                    synchronousResult = n;
                    return null;
                }
                else
                {
                    // For streams with no clear EOF like serial ports or pipes
                    // we cannot read more data without causing an app to block
                    // incorrectly.  Pipes don't go down this path
                    // though.  This code needs to be fixed.
                    // Throw away read buffer.
                    _readPos = 0;
                    _readLength = 0;
                    synchronousResult = 0;
                    return ReadNativeAsync(destination.Slice(n), n, cancellationToken);
                }
            }
        }

        private unsafe Task<int> ReadNativeAsync(Memory<byte> destination, int numBufferedBytesRead, CancellationToken cancellationToken)
        {
            AssertCanRead();
            Debug.Assert(_useAsyncIO, "ReadNativeAsync doesn't work on synchronous file streams!");

            // Create and store async stream class library specific data in the async result
            CompletionSource completionSource = CompletionSource.Create(this, _preallocatedOverlapped, numBufferedBytesRead, destination);
            NativeOverlapped* intOverlapped = completionSource.Overlapped;

            // Calculate position in the file we should be at after the read is done
            if (CanSeek)
            {
                long len = Length;

                // Make sure we are reading from the position that we think we are
                VerifyOSHandlePosition();

                if (_filePosition + destination.Length > len)
                {
                    if (_filePosition <= len)
                    {
                        destination = destination.Slice(0, (int)(len - _filePosition));
                    }
                    else
                    {
                        destination = default;
                    }
                }

                // Now set the position to read from in the NativeOverlapped struct
                // For pipes, we should leave the offset fields set to 0.
                intOverlapped->OffsetLow = unchecked((int)_filePosition);
                intOverlapped->OffsetHigh = (int)(_filePosition >> 32);

                // When using overlapped IO, the OS is not supposed to
                // touch the file pointer location at all.  We will adjust it
                // ourselves. This isn't threadsafe.

                // WriteFile should not update the file pointer when writing
                // in overlapped mode, according to MSDN.  But it does update
                // the file pointer when writing to a UNC path!
                // So changed the code below to seek to an absolute
                // location, not a relative one.  ReadFile seems consistent though.
                SeekCore(_fileHandle, destination.Length, SeekOrigin.Current);
            }

            // queue an async ReadFile operation and pass in a packed overlapped
            int r = ReadFileNative(_fileHandle, destination.Span, intOverlapped, out int errorCode);

            // ReadFile, the OS version, will return 0 on failure.  But
            // my ReadFileNative wrapper returns -1.  My wrapper will return
            // the following:
            // On error, r==-1.
            // On async requests that are still pending, r==-1 w/ errorCode==ERROR_IO_PENDING
            // on async requests that completed sequentially, r==0
            // You will NEVER RELIABLY be able to get the number of bytes
            // read back from this call when using overlapped structures!  You must
            // not pass in a non-null lpNumBytesRead to ReadFile when using
            // overlapped structures!  This is by design NT behavior.
            if (r == -1)
            {
                // For pipes, when they hit EOF, they will come here.
                if (errorCode == Interop.Errors.ERROR_BROKEN_PIPE)
                {
                    // Not an error, but EOF.  AsyncFSCallback will NOT be
                    // called.  Call the user callback here.

                    // We clear the overlapped status bit for this special case.
                    // Failure to do so looks like we are freeing a pending overlapped later.
                    intOverlapped->InternalLow = IntPtr.Zero;
                    completionSource.SetCompletedSynchronously(0);
                }
                else if (errorCode != Interop.Errors.ERROR_IO_PENDING)
                {
                    if (!_fileHandle.IsClosed && CanSeek)  // Update Position - It could be anywhere.
                    {
                        SeekCore(_fileHandle, 0, SeekOrigin.Current);
                    }

                    completionSource.ReleaseNativeResource();

                    if (errorCode == Interop.Errors.ERROR_HANDLE_EOF)
                    {
                        ThrowHelper.ThrowEndOfFileException();
                    }
                    else
                    {
                        throw Win32Marshal.GetExceptionForWin32Error(errorCode, _path);
                    }
                }
                else if (cancellationToken.CanBeCanceled) // ERROR_IO_PENDING
                {
                    // Only once the IO is pending do we register for cancellation
                    completionSource.RegisterForCancellation(cancellationToken);
                }
            }
            else
            {
                // Due to a workaround for a race condition in NT's ReadFile &
                // WriteFile routines, we will always be returning 0 from ReadFileNative
                // when we do async IO instead of the number of bytes read,
                // irregardless of whether the operation completed
                // synchronously or asynchronously.  We absolutely must not
                // set asyncResult._numBytes here, since will never have correct
                // results.
            }

            return completionSource.Task;
        }

        private ValueTask WriteAsyncInternal(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
        {
            Debug.Assert(_useAsyncIO);
            Debug.Assert((_readPos == 0 && _readLength == 0 && _writePos >= 0) || (_writePos == 0 && _readPos <= _readLength), "We're either reading or writing, but not both.");
            Debug.Assert(!_isPipe || (_readPos == 0 && _readLength == 0), "Win32FileStream must not have buffered data here!  Pipes should be unidirectional.");

            if (!CanWrite) ThrowHelper.ThrowNotSupportedException_UnwritableStream();

            bool writeDataStoredInBuffer = false;
            if (!_isPipe) // avoid async buffering with pipes, as doing so can lead to deadlocks (see comments in ReadInternalAsyncCore)
            {
                // Ensure the buffer is clear for writing
                if (_writePos == 0)
                {
                    if (_readPos < _readLength)
                    {
                        FlushReadBuffer();
                    }
                    _readPos = 0;
                    _readLength = 0;
                }

                // Determine how much space remains in the buffer
                int remainingBuffer = _bufferLength - _writePos;
                Debug.Assert(remainingBuffer >= 0);

                // Simple/common case:
                // - The write is smaller than our buffer, such that it's worth considering buffering it.
                // - There's no active flush operation, such that we don't have to worry about the existing buffer being in use.
                // - And the data we're trying to write fits in the buffer, meaning it wasn't already filled by previous writes.
                // In that case, just store it in the buffer.
                if (source.Length < _bufferLength && !HasActiveBufferOperation && source.Length <= remainingBuffer)
                {
                    source.Span.CopyTo(new Span<byte>(GetBuffer(), _writePos, source.Length));
                    _writePos += source.Length;
                    writeDataStoredInBuffer = true;

                    // There is one special-but-common case, common because devs often use
                    // byte[] sizes that are powers of 2 and thus fit nicely into our buffer, which is
                    // also a power of 2. If after our write the buffer still has remaining space,
                    // then we're done and can return a completed task now.  But if we filled the buffer
                    // completely, we want to do the asynchronous flush/write as part of this operation
                    // rather than waiting until the next write that fills the buffer.
                    if (source.Length != remainingBuffer)
                        return default;

                    Debug.Assert(_writePos == _bufferLength);
                }
            }

            // At this point, at least one of the following is true:
            // 1. There was an active flush operation (it could have completed by now, though).
            // 2. The data doesn't fit in the remaining buffer (or it's a pipe and we chose not to try).
            // 3. We wrote all of the data to the buffer, filling it.
            //
            // If there's an active operation, we can't touch the current buffer because it's in use.
            // That gives us a choice: we can either allocate a new buffer, or we can skip the buffer
            // entirely (even if the data would otherwise fit in it).  For now, for simplicity, we do
            // the latter; it could also have performance wins due to OS-level optimizations, and we could
            // potentially add support for PreAllocatedOverlapped due to having a single buffer. (We can
            // switch to allocating a new buffer, potentially experimenting with buffer pooling, should
            // performance data suggest it's appropriate.)
            //
            // If the data doesn't fit in the remaining buffer, it could be because it's so large
            // it's greater than the entire buffer size, in which case we'd always skip the buffer,
            // or it could be because there's more data than just the space remaining.  For the latter
            // case, we need to issue an asynchronous write to flush that data, which then turns this into
            // the first case above with an active operation.
            //
            // If we already stored the data, then we have nothing additional to write beyond what
            // we need to flush.
            //
            // In any of these cases, we have the same outcome:
            // - If there's data in the buffer, flush it by writing it out asynchronously.
            // - Then, if there's any data to be written, issue a write for it concurrently.
            // We return a Task that represents one or both.

            // Flush the buffer asynchronously if there's anything to flush
            Task? flushTask = null;
            if (_writePos > 0)
            {
                flushTask = FlushWriteAsync(cancellationToken);

                // If we already copied all of the data into the buffer,
                // simply return the flush task here.  Same goes for if the task has
                // already completed and was unsuccessful.
                if (writeDataStoredInBuffer ||
                    flushTask.IsFaulted ||
                    flushTask.IsCanceled)
                {
                    return new ValueTask(flushTask);
                }
            }

            Debug.Assert(!writeDataStoredInBuffer);
            Debug.Assert(_writePos == 0);

            // Finally, issue the write asynchronously, and return a Task that logically
            // represents the write operation, including any flushing done.
            Task writeTask = WriteAsyncInternalCore(source, cancellationToken);
            return new ValueTask(
                (flushTask == null || flushTask.Status == TaskStatus.RanToCompletion) ? writeTask :
                (writeTask.Status == TaskStatus.RanToCompletion) ? flushTask :
                Task.WhenAll(flushTask, writeTask));
        }

        private unsafe Task WriteAsyncInternalCore(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
        {
            Debug.Assert(!_fileHandle.IsClosed, "!_handle.IsClosed");
            Debug.Assert(CanWrite, "_parent.CanWrite");
            Debug.Assert(_readPos == _readLength, "_readPos == _readLen");
            Debug.Assert(_useAsyncIO, "WriteInternalCoreAsync doesn't work on synchronous file streams!");

            // Create and store async stream class library specific data in the async result
            CompletionSource completionSource = CompletionSource.Create(this, _preallocatedOverlapped, 0, source);
            NativeOverlapped* intOverlapped = completionSource.Overlapped;

            if (CanSeek)
            {
                // Make sure we set the length of the file appropriately.
                long len = Length;

                // Make sure we are writing to the position that we think we are
                VerifyOSHandlePosition();

                if (_filePosition + source.Length > len)
                {
                    SetLengthCore(_filePosition + source.Length);
                }

                // Now set the position to read from in the NativeOverlapped struct
                // For pipes, we should leave the offset fields set to 0.
                intOverlapped->OffsetLow = (int)_filePosition;
                intOverlapped->OffsetHigh = (int)(_filePosition >> 32);

                // When using overlapped IO, the OS is not supposed to
                // touch the file pointer location at all.  We will adjust it
                // ourselves.  This isn't threadsafe.
                SeekCore(_fileHandle, source.Length, SeekOrigin.Current);
            }

            // queue an async WriteFile operation and pass in a packed overlapped
            int r = WriteFileNative(_fileHandle, source.Span, intOverlapped, out int errorCode);

            // WriteFile, the OS version, will return 0 on failure.  But
            // my WriteFileNative wrapper returns -1.  My wrapper will return
            // the following:
            // On error, r==-1.
            // On async requests that are still pending, r==-1 w/ errorCode==ERROR_IO_PENDING
            // On async requests that completed sequentially, r==0
            // You will NEVER RELIABLY be able to get the number of bytes
            // written back from this call when using overlapped IO!  You must
            // not pass in a non-null lpNumBytesWritten to WriteFile when using
            // overlapped structures!  This is ByDesign NT behavior.
            if (r == -1)
            {
                // For pipes, when they are closed on the other side, they will come here.
                if (errorCode == Interop.Errors.ERROR_NO_DATA)
                {
                    // Not an error, but EOF. AsyncFSCallback will NOT be called.
                    // Completing TCS and return cached task allowing the GC to collect TCS.
                    completionSource.SetCompletedSynchronously(0);
                    return Task.CompletedTask;
                }
                else if (errorCode != Interop.Errors.ERROR_IO_PENDING)
                {
                    if (!_fileHandle.IsClosed && CanSeek)  // Update Position - It could be anywhere.
                    {
                        SeekCore(_fileHandle, 0, SeekOrigin.Current);
                    }

                    completionSource.ReleaseNativeResource();

                    if (errorCode == Interop.Errors.ERROR_HANDLE_EOF)
                    {
                        ThrowHelper.ThrowEndOfFileException();
                    }
                    else
                    {
                        throw Win32Marshal.GetExceptionForWin32Error(errorCode, _path);
                    }
                }
                else if (cancellationToken.CanBeCanceled) // ERROR_IO_PENDING
                {
                    // Only once the IO is pending do we register for cancellation
                    completionSource.RegisterForCancellation(cancellationToken);
                }
            }
            else
            {
                // Due to a workaround for a race condition in NT's ReadFile &
                // WriteFile routines, we will always be returning 0 from WriteFileNative
                // when we do async IO instead of the number of bytes written,
                // irregardless of whether the operation completed
                // synchronously or asynchronously.  We absolutely must not
                // set asyncResult._numBytes here, since will never have correct
                // results.
            }

            return completionSource.Task;
        }

        // __ConsoleStream also uses this code.
        private unsafe int ReadFileNative(SafeFileHandle handle, Span<byte> bytes, NativeOverlapped* overlapped, out int errorCode)
        {
            Debug.Assert((_useAsyncIO && overlapped != null) || (!_useAsyncIO && overlapped == null), "Async IO and overlapped parameters inconsistent in call to ReadFileNative.");

            return FileStreamHelpers.ReadFileNative(handle, bytes, false, overlapped, out errorCode);
        }

        private unsafe int WriteFileNative(SafeFileHandle handle, ReadOnlySpan<byte> buffer, NativeOverlapped* overlapped, out int errorCode)
        {
            Debug.Assert((_useAsyncIO && overlapped != null) || (!_useAsyncIO && overlapped == null), "Async IO and overlapped parameters inconsistent in call to WriteFileNative.");

            return FileStreamHelpers.WriteFileNative(handle, buffer, false, overlapped, out errorCode);
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            // If we're in sync mode, just use the shared CopyToAsync implementation that does
            // typical read/write looping.
            if (!_useAsyncIO)
            {
                return base.CopyToAsync(destination, bufferSize, cancellationToken);
            }

            ValidateCopyToArguments(destination, bufferSize);

            // Fail if the file was closed
            if (_fileHandle.IsClosed)
            {
                ThrowHelper.ThrowObjectDisposedException_FileClosed();
            }
            if (!CanRead)
            {
                ThrowHelper.ThrowNotSupportedException_UnreadableStream();
            }

            // Bail early for cancellation if cancellation has been requested
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
            }

            // Do the async copy, with differing implementations based on whether the FileStream was opened as async or sync
            Debug.Assert((_readPos == 0 && _readLength == 0 && _writePos >= 0) || (_writePos == 0 && _readPos <= _readLength), "We're either reading or writing, but not both.");
            return AsyncModeCopyToAsync(destination, bufferSize, cancellationToken);
        }

        private async Task AsyncModeCopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            Debug.Assert(_useAsyncIO, "This implementation is for async mode only");
            Debug.Assert(!_fileHandle.IsClosed, "!_handle.IsClosed");
            Debug.Assert(CanRead, "_parent.CanRead");

            // Make sure any pending writes have been flushed before we do a read.
            if (_writePos > 0)
            {
                await FlushWriteAsync(cancellationToken).ConfigureAwait(false);
            }

            // Typically CopyToAsync would be invoked as the only "read" on the stream, but it's possible some reading is
            // done and then the CopyToAsync is issued.  For that case, see if we have any data available in the buffer.
            if (GetBuffer() != null)
            {
                int bufferedBytes = _readLength - _readPos;
                if (bufferedBytes > 0)
                {
                    await destination.WriteAsync(new ReadOnlyMemory<byte>(GetBuffer(), _readPos, bufferedBytes), cancellationToken).ConfigureAwait(false);
                    _readPos = _readLength = 0;
                }
            }

            bool canSeek = CanSeek;
            if (canSeek)
            {
                VerifyOSHandlePosition();
            }

            try
            {
                await FileStreamHelpers
                    .AsyncModeCopyToAsync(_fileHandle, _path, canSeek, _filePosition, destination, bufferSize, cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                // Make sure the stream's current position reflects where we ended up
                if (!_fileHandle.IsClosed && CanSeek)
                {
                    SeekCore(_fileHandle, 0, SeekOrigin.End);
                }
            }
        }

        internal override void Lock(long position, long length) => FileStreamHelpers.Lock(_fileHandle, _path, position, length);

        internal override void Unlock(long position, long length) => FileStreamHelpers.Unlock(_fileHandle, _path, position, length);
    }
}
