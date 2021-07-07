// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Strategies
{
    /// <summary>Provides an implementation of a file stream for Unix files.</summary>
    internal sealed partial class Net5CompatFileStreamStrategy : FileStreamStrategy
    {
        /// <summary>Advanced options requested when opening the file.</summary>
        private FileOptions _options;

        /// <summary>If the file was opened with FileMode.Append, the length of the file when opened; otherwise, -1.</summary>
        private long _appendStart = -1;

        /// <summary>
        /// Extra state used by the file stream when _useAsyncIO is true.  This includes
        /// the semaphore used to serialize all operation, the buffer/offset/count provided by the
        /// caller for ReadAsync/WriteAsync operations, and the last successful task returned
        /// synchronously from ReadAsync which can be reused if the count matches the next request.
        /// Only initialized when <see cref="_useAsyncIO"/> is true.
        /// </summary>
        private AsyncState? _asyncState;

        private void Init(FileMode mode, string originalPath, FileOptions options)
        {
            // FileStream performs most of the general argument validation.  We can assume here that the arguments
            // are all checked and consistent (e.g. non-null-or-empty path; valid enums in mode, access, share, and options; etc.)
            // Store the arguments
            _options = options;

            if (_useAsyncIO)
            {
                _asyncState = new AsyncState();
            }

            if (mode == FileMode.Append)
            {
                // Jump to the end of the file if opened as Append.
                _appendStart = SeekCore(_fileHandle, 0, SeekOrigin.End);
            }

            Debug.Assert(_fileHandle.IsAsync == _useAsyncIO);
        }

        /// <summary>Initializes a stream from an already open file handle (file descriptor).</summary>
        private void InitFromHandle(SafeFileHandle handle, FileAccess access, bool useAsyncIO)
        {
            if (useAsyncIO)
                _asyncState = new AsyncState();

            if (handle.CanSeek)
                SeekCore(handle, 0, SeekOrigin.Current);
        }

        public override bool CanSeek => _fileHandle.CanSeek;

        public override long Length
        {
            get
            {
                // Get the length of the file as reported by the OS
                long length = RandomAccess.GetFileLength(_fileHandle);

                // But we may have buffered some data to be written that puts our length
                // beyond what the OS is aware of.  Update accordingly.
                if (_writePos > 0 && _filePosition + _writePos > length)
                {
                    length = _writePos + _filePosition;
                }

                return length;
            }
        }

        /// <summary>Releases the unmanaged resources used by the stream.</summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (_fileHandle != null && !_fileHandle.IsClosed)
                {
                    // Flush any remaining data in the file
                    try
                    {
                        FlushWriteBuffer();
                    }
                    catch (Exception e) when (!disposing && FileStreamHelpers.IsIoRelatedException(e))
                    {
                        // On finalization, ignore failures from trying to flush the write buffer,
                        // e.g. if this stream is wrapping a pipe and the pipe is now broken.
                    }

                    // Closing the file handle can fail, e.g. due to out of disk space
                    // Throw these errors as exceptions when disposing
                    if (_fileHandle != null && !_fileHandle.IsClosed && disposing)
                    {
                        SafeFileHandle.t_lastCloseErrorInfo = null;

                        _fileHandle.Dispose();

                        if (SafeFileHandle.t_lastCloseErrorInfo != null)
                        {
                            throw Interop.GetExceptionForIoErrno(SafeFileHandle.t_lastCloseErrorInfo.GetValueOrDefault(), _fileHandle.Path, isDirectory: false);
                        }
                    }
                }
            }
            finally
            {
                if (_fileHandle != null && !_fileHandle.IsClosed)
                {
                    _fileHandle.Dispose();
                }
                base.Dispose(disposing);
            }
        }

        public override ValueTask DisposeAsync()
        {
            // On Unix, we don't have any special support for async I/O, simply queueing writes
            // rather than doing them synchronously.  As such, if we're "using async I/O" and we
            // have something to flush, queue the call to Dispose, so that we end up queueing whatever
            // write work happens to flush the buffer.  Otherwise, just delegate to the base implementation,
            // which will synchronously invoke Dispose.  We don't need to factor in the current type
            // as we're using the virtual Dispose either way, and therefore factoring in whatever
            // override may already exist on a derived type.
            if (_useAsyncIO && _writePos > 0)
            {
                return new ValueTask(Task.Factory.StartNew(static s => ((Net5CompatFileStreamStrategy)s!).Dispose(), this,
                    CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default));
            }

            return base.DisposeAsync();
        }

        /// <summary>Flushes the OS buffer.  This does not flush the internal read/write buffer.</summary>
        private void FlushOSBuffer()
        {
            if (Interop.Sys.FSync(_fileHandle) < 0)
            {
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                switch (errorInfo.Error)
                {
                    case Interop.Error.EROFS:
                    case Interop.Error.EINVAL:
                    case Interop.Error.ENOTSUP:
                        // Ignore failures due to the FileStream being bound to a special file that
                        // doesn't support synchronization.  In such cases there's nothing to flush.
                        break;
                    default:
                        throw Interop.GetExceptionForIoErrno(errorInfo, _fileHandle.Path, isDirectory: false);
                }
            }
        }

        private void FlushWriteBufferForWriteByte()
        {
#pragma warning disable CA1416 // Validate platform compatibility, issue: https://github.com/dotnet/runtime/issues/44542
            _asyncState?.Wait();
#pragma warning restore CA1416
            try { FlushWriteBuffer(); }
            finally { _asyncState?.Release(); }
        }

        /// <summary>Writes any data in the write buffer to the underlying stream and resets the buffer.</summary>
        private void FlushWriteBuffer(bool calledFromFinalizer = false)
        {
            AssertBufferInvariants();
            if (_writePos > 0)
            {
                WriteNative(new ReadOnlySpan<byte>(GetBuffer(), 0, _writePos));
                _writePos = 0;
            }
        }

        /// <summary>Sets the length of this stream to the given value.</summary>
        /// <param name="value">The new length of the stream.</param>
        public override void SetLength(long value)
        {
            FlushInternalBuffer();

            if (_appendStart != -1 && value < _appendStart)
            {
                throw new IOException(SR.IO_SetLengthAppendTruncate);
            }

            VerifyOSHandlePosition();

            CheckFileCall(Interop.Sys.FTruncate(_fileHandle, value));

            // Set file pointer to end of file
            if (_filePosition > value)
            {
                SeekCore(_fileHandle, 0, SeekOrigin.End);
            }
        }

        /// <summary>Reads a block of bytes from the stream and writes the data in a given buffer.</summary>
        private int ReadSpan(Span<byte> destination)
        {
            PrepareForReading();

            // Are there any bytes available in the read buffer? If yes,
            // we can just return from the buffer.  If the buffer is empty
            // or has no more available data in it, we can either refill it
            // (and then read from the buffer into the user's buffer) or
            // we can just go directly into the user's buffer, if they asked
            // for more data than we'd otherwise buffer.
            int numBytesAvailable = _readLength - _readPos;
            bool readFromOS = false;
            if (numBytesAvailable == 0)
            {
                // If we're not able to seek, then we're not able to rewind the stream (i.e. flushing
                // a read buffer), in which case we don't want to use a read buffer.  Similarly, if
                // the user has asked for more data than we can buffer, we also want to skip the buffer.
                if (!CanSeek || (destination.Length >= _bufferLength))
                {
                    // Read directly into the user's buffer
                    _readPos = _readLength = 0;
                    return ReadNative(destination);
                }
                else
                {
                    // Read into our buffer.
                    _readLength = numBytesAvailable = ReadNative(GetBuffer());
                    _readPos = 0;
                    if (numBytesAvailable == 0)
                    {
                        return 0;
                    }

                    // Note that we did an OS read as part of this Read, so that later
                    // we don't try to do one again if what's in the buffer doesn't
                    // meet the user's request.
                    readFromOS = true;
                }
            }

            // Now that we know there's data in the buffer, read from it into the user's buffer.
            Debug.Assert(numBytesAvailable > 0, "Data must be in the buffer to be here");
            int bytesRead = Math.Min(numBytesAvailable, destination.Length);
            new Span<byte>(GetBuffer(), _readPos, bytesRead).CopyTo(destination);
            _readPos += bytesRead;

            // We may not have had enough data in the buffer to completely satisfy the user's request.
            // While Read doesn't require that we return as much data as the user requested (any amount
            // up to the requested count is fine), FileStream on Windows tries to do so by doing a
            // subsequent read from the file if we tried to satisfy the request with what was in the
            // buffer but the buffer contained less than the requested count. To be consistent with that
            // behavior, we do the same thing here on Unix.  Note that we may still get less the requested
            // amount, as the OS may give us back fewer than we request, either due to reaching the end of
            // file, or due to its own whims.
            if (!readFromOS && bytesRead < destination.Length)
            {
                Debug.Assert(_readPos == _readLength, "bytesToRead should only be < destination.Length if numBytesAvailable < destination.Length");
                _readPos = _readLength = 0; // no data left in the read buffer
                bytesRead += ReadNative(destination.Slice(bytesRead));
            }

            return bytesRead;
        }

        /// <summary>Unbuffered, reads a block of bytes from the file handle into the given buffer.</summary>
        /// <param name="buffer">The buffer into which data from the file is read.</param>
        /// <returns>
        /// The total number of bytes read into the buffer. This might be less than the number of bytes requested
        /// if that number of bytes are not currently available, or zero if the end of the stream is reached.
        /// </returns>
        private unsafe int ReadNative(Span<byte> buffer)
        {
            FlushWriteBuffer(); // we're about to read; dump the write buffer

            VerifyOSHandlePosition();

            int bytesRead;
            fixed (byte* bufPtr = &MemoryMarshal.GetReference(buffer))
            {
                bytesRead = CheckFileCall(Interop.Sys.Read(_fileHandle, bufPtr, buffer.Length));
                Debug.Assert(bytesRead <= buffer.Length);
            }
            _filePosition += bytesRead;
            return bytesRead;
        }

        /// <summary>
        /// Asynchronously reads a sequence of bytes from the current stream and advances
        /// the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="destination">The buffer to write the data into.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <param name="synchronousResult">If the operation completes synchronously, the number of bytes read.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        private Task<int>? ReadAsyncInternal(Memory<byte> destination, CancellationToken cancellationToken, out int synchronousResult)
        {
            Debug.Assert(_useAsyncIO);
            Debug.Assert(_asyncState != null);

            if (!CanRead) // match Windows behavior; this gets thrown synchronously
            {
                ThrowHelper.ThrowNotSupportedException_UnreadableStream();
            }

            // Serialize operations using the semaphore.
            Task waitTask = _asyncState.WaitAsync();

            // If we got ownership immediately, and if there's enough data in our buffer
            // to satisfy the full request of the caller, hand back the buffered data.
            // While it would be a legal implementation of the Read contract, we don't
            // hand back here less than the amount requested so as to match the behavior
            // in ReadCore that will make a native call to try to fulfill the remainder
            // of the request.
            if (waitTask.Status == TaskStatus.RanToCompletion)
            {
                int numBytesAvailable = _readLength - _readPos;
                if (numBytesAvailable >= destination.Length)
                {
                    try
                    {
                        PrepareForReading();

                        new Span<byte>(GetBuffer(), _readPos, destination.Length).CopyTo(destination.Span);
                        _readPos += destination.Length;

                        synchronousResult = destination.Length;
                        return null;
                    }
                    catch (Exception exc)
                    {
                        synchronousResult = 0;
                        return Task.FromException<int>(exc);
                    }
                    finally
                    {
                        _asyncState.Release();
                    }
                }
            }

            // Otherwise, issue the whole request asynchronously.
            synchronousResult = 0;
            _asyncState.Memory = destination;
            return waitTask.ContinueWith(static (t, s) =>
            {
                // The options available on Unix for writing asynchronously to an arbitrary file
                // handle typically amount to just using another thread to do the synchronous write,
                // which is exactly  what this implementation does. This does mean there are subtle
                // differences in certain FileStream behaviors between Windows and Unix when multiple
                // asynchronous operations are issued against the stream to execute concurrently; on
                // Unix the operations will be serialized due to the usage of a semaphore, but the
                // position /length information won't be updated until after the write has completed,
                // whereas on Windows it may happen before the write has completed.

                Debug.Assert(t.Status == TaskStatus.RanToCompletion);
                var thisRef = (Net5CompatFileStreamStrategy)s!;
                Debug.Assert(thisRef._asyncState != null);
                try
                {
                    Memory<byte> memory = thisRef._asyncState.Memory;
                    thisRef._asyncState.Memory = default;
                    return thisRef.ReadSpan(memory.Span);
                }
                finally { thisRef._asyncState.Release(); }
            }, this, CancellationToken.None, TaskContinuationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        /// <summary>Reads from the file handle into the buffer, overwriting anything in it.</summary>
        private int FillReadBufferForReadByte()
        {
#pragma warning disable CA1416 // Validate platform compatibility, issue: https://github.com/dotnet/runtime/issues/44542
            _asyncState?.Wait();
#pragma warning restore CA1416
            try { return ReadNative(_buffer); }
            finally { _asyncState?.Release(); }
        }

        /// <summary>Writes a block of bytes to the file stream.</summary>
        /// <param name="source">The buffer containing data to write to the stream.</param>
        private void WriteSpan(ReadOnlySpan<byte> source)
        {
            PrepareForWriting();

            // If no data is being written, nothing more to do.
            if (source.Length == 0)
            {
                return;
            }

            // If there's already data in our write buffer, then we need to go through
            // our buffer to ensure data isn't corrupted.
            if (_writePos > 0)
            {
                // If there's space remaining in the buffer, then copy as much as
                // we can from the user's buffer into ours.
                int spaceRemaining = _bufferLength - _writePos;
                if (spaceRemaining >= source.Length)
                {
                    source.CopyTo(GetBuffer().AsSpan(_writePos));
                    _writePos += source.Length;
                    return;
                }
                else if (spaceRemaining > 0)
                {
                    source.Slice(0, spaceRemaining).CopyTo(GetBuffer().AsSpan(_writePos));
                    _writePos += spaceRemaining;
                    source = source.Slice(spaceRemaining);
                }

                // At this point, the buffer is full, so flush it out.
                FlushWriteBuffer();
            }

            // Our buffer is now empty.  If using the buffer would slow things down (because
            // the user's looking to write more data than we can store in the buffer),
            // skip the buffer.  Otherwise, put the remaining data into the buffer.
            Debug.Assert(_writePos == 0);
            if (source.Length >= _bufferLength)
            {
                WriteNative(source);
            }
            else
            {
                source.CopyTo(new Span<byte>(GetBuffer()));
                _writePos = source.Length;
            }
        }

        /// <summary>Unbuffered, writes a block of bytes to the file stream.</summary>
        /// <param name="source">The buffer containing data to write to the stream.</param>
        private unsafe void WriteNative(ReadOnlySpan<byte> source)
        {
            VerifyOSHandlePosition();

            fixed (byte* bufPtr = &MemoryMarshal.GetReference(source))
            {
                int offset = 0;
                int count = source.Length;
                while (count > 0)
                {
                    int bytesWritten = CheckFileCall(Interop.Sys.Write(_fileHandle, bufPtr + offset, count));
                    _filePosition += bytesWritten;
                    offset += bytesWritten;
                    count -= bytesWritten;
                }
            }
        }

        /// <summary>
        /// Asynchronously writes a sequence of bytes to the current stream, advances
        /// the current position within this stream by the number of bytes written, and
        /// monitors cancellation requests.
        /// </summary>
        /// <param name="source">The buffer to write data from.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        private ValueTask WriteAsyncInternal(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
        {
            Debug.Assert(_useAsyncIO);
            Debug.Assert(_asyncState != null);

            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled(cancellationToken);

            if (_fileHandle.IsClosed)
                ThrowHelper.ThrowObjectDisposedException_FileClosed();

            if (!CanWrite) // match Windows behavior; this gets thrown synchronously
            {
                ThrowHelper.ThrowNotSupportedException_UnwritableStream();
            }

            // Serialize operations using the semaphore.
            Task waitTask = _asyncState.WaitAsync(cancellationToken);

            // If we got ownership immediately, and if there's enough space in our buffer
            // to buffer the entire write request, then do so and we're done.
            if (waitTask.Status == TaskStatus.RanToCompletion)
            {
                int spaceRemaining = _bufferLength - _writePos;
                if (spaceRemaining >= source.Length)
                {
                    try
                    {
                        PrepareForWriting();

                        source.Span.CopyTo(new Span<byte>(GetBuffer(), _writePos, source.Length));
                        _writePos += source.Length;

                        return default;
                    }
                    catch (Exception exc)
                    {
                        return ValueTask.FromException(exc);
                    }
                    finally
                    {
                        _asyncState.Release();
                    }
                }
            }

            // Otherwise, issue the whole request asynchronously.
            _asyncState.ReadOnlyMemory = source;
            return new ValueTask(waitTask.ContinueWith(static (t, s) =>
            {
                // The options available on Unix for writing asynchronously to an arbitrary file
                // handle typically amount to just using another thread to do the synchronous write,
                // which is exactly  what this implementation does. This does mean there are subtle
                // differences in certain FileStream behaviors between Windows and Unix when multiple
                // asynchronous operations are issued against the stream to execute concurrently; on
                // Unix the operations will be serialized due to the usage of a semaphore, but the
                // position/length information won't be updated until after the write has completed,
                // whereas on Windows it may happen before the write has completed.

                Debug.Assert(t.Status == TaskStatus.RanToCompletion);
                var thisRef = (Net5CompatFileStreamStrategy)s!;
                Debug.Assert(thisRef._asyncState != null);
                try
                {
                    ReadOnlyMemory<byte> readOnlyMemory = thisRef._asyncState.ReadOnlyMemory;
                    thisRef._asyncState.ReadOnlyMemory = default;
                    thisRef.WriteSpan(readOnlyMemory.Span);
                }
                finally { thisRef._asyncState.Release(); }
            }, this, CancellationToken.None, TaskContinuationOptions.DenyChildAttach, TaskScheduler.Default));
        }

        /// <summary>Sets the current position of this stream to the given value.</summary>
        /// <param name="offset">The point relative to origin from which to begin seeking. </param>
        /// <param name="origin">
        /// Specifies the beginning, the end, or the current position as a reference
        /// point for offset, using a value of type SeekOrigin.
        /// </param>
        /// <returns>The new position in the stream.</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin < SeekOrigin.Begin || origin > SeekOrigin.End)
            {
                throw new ArgumentException(SR.Argument_InvalidSeekOrigin, nameof(origin));
            }
            if (_fileHandle.IsClosed)
            {
                ThrowHelper.ThrowObjectDisposedException_FileClosed();
            }
            if (!CanSeek)
            {
                ThrowHelper.ThrowNotSupportedException_UnseekableStream();
            }

            VerifyOSHandlePosition();

            // Flush our write/read buffer.  FlushWrite will output any write buffer we have and reset _bufferWritePos.
            // We don't call FlushRead, as that will do an unnecessary seek to rewind the read buffer, and since we're
            // about to seek and update our position, we can simply update the offset as necessary and reset our read
            // position and length to 0. (In the future, for some simple cases we could potentially add an optimization
            // here to just move data around in the buffer for short jumps, to avoid re-reading the data from disk.)
            FlushWriteBuffer();
            if (origin == SeekOrigin.Current)
            {
                offset -= (_readLength - _readPos);
            }
            _readPos = _readLength = 0;

            // Keep track of where we were, in case we're in append mode and need to verify
            long oldPos = 0;
            if (_appendStart >= 0)
            {
                oldPos = SeekCore(_fileHandle, 0, SeekOrigin.Current);
            }

            // Jump to the new location
            long pos = SeekCore(_fileHandle, offset, origin);

            // Prevent users from overwriting data in a file that was opened in append mode.
            if (_appendStart != -1 && pos < _appendStart)
            {
                SeekCore(_fileHandle, oldPos, SeekOrigin.Begin);
                throw new IOException(SR.IO_SeekAppendOverwrite);
            }

            // Return the new position
            return pos;
        }

        /// <summary>Sets the current position of this stream to the given value.</summary>
        /// <param name="fileHandle">The file handle on which to seek.</param>
        /// <param name="offset">The point relative to origin from which to begin seeking. </param>
        /// <param name="origin">
        /// Specifies the beginning, the end, or the current position as a reference
        /// point for offset, using a value of type SeekOrigin.
        /// </param>
        /// <param name="closeInvalidHandle">not used in Unix implementation</param>
        /// <returns>The new position in the stream.</returns>
        private long SeekCore(SafeFileHandle fileHandle, long offset, SeekOrigin origin, bool closeInvalidHandle = false)
        {
            Debug.Assert(!fileHandle.IsInvalid);
            Debug.Assert(fileHandle.CanSeek);
            Debug.Assert(origin >= SeekOrigin.Begin && origin <= SeekOrigin.End);

            long pos = FileStreamHelpers.CheckFileCall(Interop.Sys.LSeek(fileHandle, offset, (Interop.Sys.SeekWhence)(int)origin), fileHandle.Path); // SeekOrigin values are the same as Interop.libc.SeekWhence values
            _filePosition = pos;
            return pos;
        }

        private int CheckFileCall(int result, bool ignoreNotSupported = false)
        {
            FileStreamHelpers.CheckFileCall(result, _fileHandle?.Path, ignoreNotSupported);

            return result;
        }

        /// <summary>State used when the stream is in async mode.</summary>
        private sealed class AsyncState : SemaphoreSlim
        {
            internal ReadOnlyMemory<byte> ReadOnlyMemory;
            internal Memory<byte> Memory;

            /// <summary>Initialize the AsyncState.</summary>
            internal AsyncState() : base(initialCount: 1, maxCount: 1) { }
        }
    }
}
