// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    /// <summary>Provides an implementation of a file stream for Unix files.</summary>
    public partial class FileStream : Stream
    {
        /// <summary>File mode.</summary>
        private FileMode _mode;

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
        private AsyncState _asyncState;

        /// <summary>Lazily-initialized value for whether the file supports seeking.</summary>
        private bool? _canSeek;

        private SafeFileHandle OpenHandle(FileMode mode, FileShare share, FileOptions options)
        {
            // FileStream performs most of the general argument validation.  We can assume here that the arguments
            // are all checked and consistent (e.g. non-null-or-empty path; valid enums in mode, access, share, and options; etc.)
            // Store the arguments
            _mode = mode;
            _options = options;

            if (_useAsyncIO)
                _asyncState = new AsyncState();

            // Translate the arguments into arguments for an open call.
            Interop.Sys.OpenFlags openFlags = PreOpenConfigurationFromOptions(mode, _access, options); // FileShare currently ignored

            // If the file gets created a new, we'll select the permissions for it.  Most utilities by default use 666 (read and 
            // write for all). However, on Windows it's possible to write out a file and then execute it.  To maintain that similarity, 
            // we use 766, so that in addition the user has execute privileges. No matter what we choose, it'll be subject to the umask 
            // applied by the system, such that the actual permissions will typically be less than what we select here.
            const Interop.Sys.Permissions openPermissions =
                Interop.Sys.Permissions.S_IRWXU |
                Interop.Sys.Permissions.S_IRGRP | Interop.Sys.Permissions.S_IWGRP |
                Interop.Sys.Permissions.S_IROTH | Interop.Sys.Permissions.S_IWOTH;

            // Open the file and store the safe handle.
            return SafeFileHandle.Open(_path, openFlags, (int)openPermissions);
        }

        /// <summary>Initializes a stream for reading or writing a Unix file.</summary>
        /// <param name="mode">How the file should be opened.</param>
        /// <param name="share">What other access to the file should be allowed.  This is currently ignored.</param>
        private void Init(FileMode mode, FileShare share)
        {
            _fileHandle.IsAsync = _useAsyncIO;

            // Lock the file if requested via FileShare.  This is only advisory locking. FileShare.None implies an exclusive 
            // lock on the file and all other modes use a shared lock.  While this is not as granular as Windows, not mandatory, 
            // and not atomic with file opening, it's better than nothing.
            Interop.Sys.LockOperations lockOperation = (share == FileShare.None) ? Interop.Sys.LockOperations.LOCK_EX : Interop.Sys.LockOperations.LOCK_SH;
            if (Interop.Sys.FLock(_fileHandle, lockOperation | Interop.Sys.LockOperations.LOCK_NB) < 0)
            {
                // The only error we care about is EWOULDBLOCK, which indicates that the file is currently locked by someone
                // else and we would block trying to access it.  Other errors, such as ENOTSUP (locking isn't supported) or
                // EACCES (the file system doesn't allow us to lock), will only hamper FileStream's usage without providing value,
                // given again that this is only advisory / best-effort.
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                if (errorInfo.Error == Interop.Error.EWOULDBLOCK)
                {
                    throw Interop.GetExceptionForIoErrno(errorInfo, _path, isDirectory: false);
                }
            }

            // These provide hints around how the file will be accessed.  Specifying both RandomAccess
            // and Sequential together doesn't make sense as they are two competing options on the same spectrum,
            // so if both are specified, we prefer RandomAccess (behavior on Windows is unspecified if both are provided).
            Interop.Sys.FileAdvice fadv =
                (_options & FileOptions.RandomAccess) != 0 ? Interop.Sys.FileAdvice.POSIX_FADV_RANDOM :
                (_options & FileOptions.SequentialScan) != 0 ? Interop.Sys.FileAdvice.POSIX_FADV_SEQUENTIAL :
                0;
            if (fadv != 0)
            {
                CheckFileCall(Interop.Sys.PosixFAdvise(_fileHandle, 0, 0, fadv), 
                    ignoreNotSupported: true); // just a hint.
            }

            // Jump to the end of the file if opened as Append.
            if (_mode == FileMode.Append)
            {
                _appendStart = SeekCore(0, SeekOrigin.End);
            }
        }

        /// <summary>Initializes a stream from an already open file handle (file descriptor).</summary>
        /// <param name="handle">The handle to the file.</param>
        /// <param name="bufferSize">The size of the buffer to use when buffering.</param>
        /// <param name="useAsyncIO">Whether access to the stream is performed asynchronously.</param>
        private void InitFromHandle(SafeFileHandle handle)
        {
            if (_useAsyncIO)
                _asyncState = new AsyncState();

            if (CanSeekCore) // use non-virtual CanSeekCore rather than CanSeek to avoid making virtual call during ctor
                SeekCore(0, SeekOrigin.Current);
        }

        /// <summary>Translates the FileMode, FileAccess, and FileOptions values into flags to be passed when opening the file.</summary>
        /// <param name="mode">The FileMode provided to the stream's constructor.</param>
        /// <param name="access">The FileAccess provided to the stream's constructor</param>
        /// <param name="options">The FileOptions provided to the stream's constructor</param>
        /// <returns>The flags value to be passed to the open system call.</returns>
        private static Interop.Sys.OpenFlags PreOpenConfigurationFromOptions(FileMode mode, FileAccess access, FileOptions options)
        {
            // Translate FileMode.  Most of the values map cleanly to one or more options for open.
            Interop.Sys.OpenFlags flags = default(Interop.Sys.OpenFlags);
            switch (mode)
            {
                default:
                case FileMode.Open: // Open maps to the default behavior for open(...).  No flags needed.
                    break;

                case FileMode.Append: // Append is the same as OpenOrCreate, except that we'll also separately jump to the end later
                case FileMode.OpenOrCreate:
                    flags |= Interop.Sys.OpenFlags.O_CREAT;
                    break;

                case FileMode.Create:
                    flags |= (Interop.Sys.OpenFlags.O_CREAT | Interop.Sys.OpenFlags.O_TRUNC);
                    break;

                case FileMode.CreateNew:
                    flags |= (Interop.Sys.OpenFlags.O_CREAT | Interop.Sys.OpenFlags.O_EXCL);
                    break;

                case FileMode.Truncate:
                    flags |= Interop.Sys.OpenFlags.O_TRUNC;
                    break;
            }

            // Translate FileAccess.  All possible values map cleanly to corresponding values for open.
            switch (access)
            {
                case FileAccess.Read:
                    flags |= Interop.Sys.OpenFlags.O_RDONLY;
                    break;

                case FileAccess.ReadWrite:
                    flags |= Interop.Sys.OpenFlags.O_RDWR;
                    break;

                case FileAccess.Write:
                    flags |= Interop.Sys.OpenFlags.O_WRONLY;
                    break;
            }

            // Translate some FileOptions; some just aren't supported, and others will be handled after calling open.
            // - Asynchronous: Handled in ctor, setting _useAsync and SafeFileHandle.IsAsync to true
            // - DeleteOnClose: Doesn't have a Unix equivalent, but we approximate it in Dispose
            // - Encrypted: No equivalent on Unix and is ignored
            // - RandomAccess: Implemented after open if posix_fadvise is available
            // - SequentialScan: Implemented after open if posix_fadvise is available
            // - WriteThrough: Handled here
            if ((options & FileOptions.WriteThrough) != 0)
            {
                flags |= Interop.Sys.OpenFlags.O_SYNC;
            }

            return flags;
        }

        /// <summary>Gets a value indicating whether the current stream supports seeking.</summary>
        public override bool CanSeek => CanSeekCore;

        /// <summary>Gets a value indicating whether the current stream supports seeking.</summary>
        /// <remarks>Separated out of CanSeek to enable making non-virtual call to this logic.</remarks>
        private bool CanSeekCore
        {
            get
            {
                if (_fileHandle.IsClosed)
                {
                    return false;
                }

                if (!_canSeek.HasValue)
                {
                    // Lazily-initialize whether we're able to seek, tested by seeking to our current location.
                    _canSeek = Interop.Sys.LSeek(_fileHandle, 0, Interop.Sys.SeekWhence.SEEK_CUR) >= 0;
                }
                return _canSeek.Value;
            }
        }

        private long GetLengthInternal()
        {
            // Get the length of the file as reported by the OS
            Interop.Sys.FileStatus status;
            CheckFileCall(Interop.Sys.FStat(_fileHandle, out status));
            long length = status.Size;

            // But we may have buffered some data to be written that puts our length
            // beyond what the OS is aware of.  Update accordingly.
            if (_writePos > 0 && _filePosition + _writePos > length)
            {
                length = _writePos + _filePosition;
            }

            return length;
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
                    FlushWriteBuffer();

                    // If DeleteOnClose was requested when constructed, delete the file now.
                    // (Unix doesn't directly support DeleteOnClose, so we mimic it here.)
                    if (_path != null && (_options & FileOptions.DeleteOnClose) != 0)
                    {
                        // Since we still have the file open, this will end up deleting
                        // it (assuming we're the only link to it) once it's closed, but the
                        // name will be removed immediately.
                        Interop.Sys.Unlink(_path); // ignore errors; it's valid that the path may no longer exist
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
                        throw Interop.GetExceptionForIoErrno(errorInfo, _path, isDirectory: false);
                }
            }
        }

        /// <summary>Writes any data in the write buffer to the underlying stream and resets the buffer.</summary>
        private void FlushWriteBuffer()
        {
            AssertBufferInvariants();
            if (_writePos > 0)
            {
                WriteNative(GetBuffer(), 0, _writePos);
                _writePos = 0;
            }
        }

        /// <summary>Asynchronously clears all buffers for this stream, causing any buffered data to be written to the underlying device.</summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous flush operation.</returns>
        private Task FlushAsyncInternal(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }
            if (_fileHandle.IsClosed)
            {
                throw Error.GetFileNotOpen();
            }

            // As with Win32FileStream, flush the buffers synchronously to avoid race conditions.
            try
            {
                FlushInternalBuffer();
            }
            catch (Exception e)
            {
                return Task.FromException(e);
            }

            // We then separately flush to disk asynchronously.  This is only 
            // necessary if we support writing; otherwise, we're done.
            if (CanWrite)
            {
                return Task.Factory.StartNew(
                    state => ((FileStream)state).FlushOSBuffer(),
                    this,
                    cancellationToken,
                    TaskCreationOptions.DenyChildAttach,
                    TaskScheduler.Default);
            }
            else
            {
                return Task.CompletedTask;
            }
        }

        /// <summary>Sets the length of this stream to the given value.</summary>
        /// <param name="value">The new length of the stream.</param>
        private  void SetLengthInternal(long value)
        {
            FlushInternalBuffer();

            if (_appendStart != -1 && value < _appendStart)
            {
                throw new IOException(SR.IO_SetLengthAppendTruncate);
            }

            long origPos = _filePosition;

            VerifyOSHandlePosition();

            if (_filePosition != value)
            {
                SeekCore(value, SeekOrigin.Begin);
            }

            CheckFileCall(Interop.Sys.FTruncate(_fileHandle, value));

            // Return file pointer to where it was before setting length
            if (origPos != value)
            {
                if (origPos < value)
                {
                    SeekCore(origPos, SeekOrigin.Begin);
                }
                else
                {
                    SeekCore(0, SeekOrigin.End);
                }
            }
        }

        /// <summary>Reads a block of bytes from the stream and writes the data in a given buffer.</summary>
        /// <param name="array">
        /// When this method returns, contains the specified byte array with the values between offset and 
        /// (offset + count - 1) replaced by the bytes read from the current source.
        /// </param>
        /// <param name="offset">The byte offset in array at which the read bytes will be placed.</param>
        /// <param name="count">The maximum number of bytes to read. </param>
        /// <returns>
        /// The total number of bytes read into the buffer. This might be less than the number of bytes requested 
        /// if that number of bytes are not currently available, or zero if the end of the stream is reached.
        /// </returns>
        public override int Read(byte[] array, int offset, int count)
        {
            ValidateReadWriteArgs(array, offset, count);

            if (_useAsyncIO)
            {
                _asyncState.Wait();
                try { return ReadCore(array, offset, count); }
                finally { _asyncState.Release(); }
            }
            else
            {
                return ReadCore(array, offset, count);
            }
        }

        /// <summary>Reads a block of bytes from the stream and writes the data in a given buffer.</summary>
        /// <param name="array">
        /// When this method returns, contains the specified byte array with the values between offset and 
        /// (offset + count - 1) replaced by the bytes read from the current source.
        /// </param>
        /// <param name="offset">The byte offset in array at which the read bytes will be placed.</param>
        /// <param name="count">The maximum number of bytes to read. </param>
        /// <returns>
        /// The total number of bytes read into the buffer. This might be less than the number of bytes requested 
        /// if that number of bytes are not currently available, or zero if the end of the stream is reached.
        /// </returns>
        private int ReadCore(byte[] array, int offset, int count)
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
                if (!CanSeek || (count >= _bufferLength))
                {
                    // Read directly into the user's buffer
                    _readPos = _readLength = 0;
                    return ReadNative(array, offset, count);
                }
                else
                {
                    // Read into our buffer.
                    _readLength = numBytesAvailable = ReadNative(GetBuffer(), 0, _bufferLength);
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
            int bytesRead = Math.Min(numBytesAvailable, count);
            Buffer.BlockCopy(GetBuffer(), _readPos, array, offset, bytesRead);
            _readPos += bytesRead;

            // We may not have had enough data in the buffer to completely satisfy the user's request.
            // While Read doesn't require that we return as much data as the user requested (any amount
            // up to the requested count is fine), FileStream on Windows tries to do so by doing a 
            // subsequent read from the file if we tried to satisfy the request with what was in the 
            // buffer but the buffer contained less than the requested count. To be consistent with that 
            // behavior, we do the same thing here on Unix.  Note that we may still get less the requested 
            // amount, as the OS may give us back fewer than we request, either due to reaching the end of 
            // file, or due to its own whims.
            if (!readFromOS && bytesRead < count)
            {
                Debug.Assert(_readPos == _readLength, "bytesToRead should only be < count if numBytesAvailable < count");
                _readPos = _readLength = 0; // no data left in the read buffer
                bytesRead += ReadNative(array, offset + bytesRead, count - bytesRead);
            }

            return bytesRead;
        }

        /// <summary>Unbuffered, reads a block of bytes from the stream and writes the data in a given buffer.</summary>
        /// <param name="array">
        /// When this method returns, contains the specified byte array with the values between offset and 
        /// (offset + count - 1) replaced by the bytes read from the current source.
        /// </param>
        /// <param name="offset">The byte offset in array at which the read bytes will be placed.</param>
        /// <param name="count">The maximum number of bytes to read. </param>
        /// <returns>
        /// The total number of bytes read into the buffer. This might be less than the number of bytes requested 
        /// if that number of bytes are not currently available, or zero if the end of the stream is reached.
        /// </returns>
        private unsafe int ReadNative(byte[] array, int offset, int count)
        {
            FlushWriteBuffer(); // we're about to read; dump the write buffer

            VerifyOSHandlePosition();

            int bytesRead;
            fixed (byte* bufPtr = array)
            {
                bytesRead = CheckFileCall(Interop.Sys.Read(_fileHandle, bufPtr + offset, count));
                Debug.Assert(bytesRead <= count);
            }
            _filePosition += bytesRead;
            return bytesRead;
        }

        /// <summary>
        /// Asynchronously reads a sequence of bytes from the current stream and advances
        /// the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">The buffer to write the data into.</param>
        /// <param name="offset">The byte offset in buffer at which to begin writing data from the stream.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        private Task<int> ReadAsyncInternal(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_useAsyncIO)
            {
                if (!CanRead) // match Windows behavior; this gets thrown synchronously
                {
                    throw Error.GetReadNotSupported();
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
                    if (numBytesAvailable >= count)
                    {
                        try
                        {
                            PrepareForReading();

                            Buffer.BlockCopy(GetBuffer(), _readPos, buffer, offset, count);
                            _readPos += count;

                            return _asyncState._lastSuccessfulReadTask != null && _asyncState._lastSuccessfulReadTask.Result == count ?
                                _asyncState._lastSuccessfulReadTask :
                                (_asyncState._lastSuccessfulReadTask = Task.FromResult(count));
                        }
                        catch (Exception exc)
                        {
                            return Task.FromException<int>(exc);
                        }
                        finally
                        {
                            _asyncState.Release();
                        }
                    }
                }

                // Otherwise, issue the whole request asynchronously.
                _asyncState.Update(buffer, offset, count);
                return waitTask.ContinueWith((t, s) =>
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
                    var thisRef = (FileStream)s;
                    try
                    {
                        byte[] b = thisRef._asyncState._buffer;
                        thisRef._asyncState._buffer = null; // remove reference to user's buffer
                        return thisRef.ReadCore(b, thisRef._asyncState._offset, thisRef._asyncState._count);
                    }
                    finally { thisRef._asyncState.Release(); }
                }, this, CancellationToken.None, TaskContinuationOptions.DenyChildAttach, TaskScheduler.Default);
            }
            else
            {
                return base.ReadAsync(buffer, offset, count, cancellationToken);
            }
        }

        /// <summary>
        /// Reads a byte from the stream and advances the position within the stream
        /// by one byte, or returns -1 if at the end of the stream.
        /// </summary>
        /// <returns>The unsigned byte cast to an Int32, or -1 if at the end of the stream.</returns>
        public override int ReadByte()
        {
            if (_useAsyncIO)
            {
                _asyncState.Wait();
                try { return ReadByteCore(); }
                finally { _asyncState.Release(); }
            }
            else
            {
                return ReadByteCore();
            }
        }

        /// <summary>Writes a block of bytes to the file stream.</summary>
        /// <param name="array">The buffer containing data to write to the stream.</param>
        /// <param name="offset">The zero-based byte offset in array from which to begin copying bytes to the stream.</param>
        /// <param name="count">The maximum number of bytes to write.</param>
        public override void Write(byte[] array, int offset, int count)
        {
            ValidateReadWriteArgs(array, offset, count);

            if (_useAsyncIO)
            {
                _asyncState.Wait();
                try { WriteCore(array, offset, count); }
                finally { _asyncState.Release(); }
            }
            else
            {
                WriteCore(array, offset, count);
            }
        }

        /// <summary>Writes a block of bytes to the file stream.</summary>
        /// <param name="array">The buffer containing data to write to the stream.</param>
        /// <param name="offset">The zero-based byte offset in array from which to begin copying bytes to the stream.</param>
        /// <param name="count">The maximum number of bytes to write.</param>
        private void WriteCore(byte[] array, int offset, int count)
        {
            PrepareForWriting();

            // If no data is being written, nothing more to do.
            if (count == 0)
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
                if (spaceRemaining > 0)
                {
                    int bytesToCopy = Math.Min(spaceRemaining, count);
                    Buffer.BlockCopy(array, offset, GetBuffer(), _writePos, bytesToCopy);
                    _writePos += bytesToCopy;

                    // If we've successfully copied all of the user's data, we're done.
                    if (count == bytesToCopy)
                    {
                        return;
                    }

                    // Otherwise, keep track of how much more data needs to be handled.
                    offset += bytesToCopy;
                    count -= bytesToCopy;
                }

                // At this point, the buffer is full, so flush it out.
                FlushWriteBuffer();
            }

            // Our buffer is now empty.  If using the buffer would slow things down (because
            // the user's looking to write more data than we can store in the buffer),
            // skip the buffer.  Otherwise, put the remaining data into the buffer.
            Debug.Assert(_writePos == 0);
            if (count >= _bufferLength)
            {
                WriteNative(array, offset, count);
            }
            else
            {
                Buffer.BlockCopy(array, offset, GetBuffer(), _writePos, count);
                _writePos = count;
            }
        }

        /// <summary>Unbuffered, writes a block of bytes to the file stream.</summary>
        /// <param name="array">The buffer containing data to write to the stream.</param>
        /// <param name="offset">The zero-based byte offset in array from which to begin copying bytes to the stream.</param>
        /// <param name="count">The maximum number of bytes to write.</param>
        private unsafe void WriteNative(byte[] array, int offset, int count)
        {
            VerifyOSHandlePosition();

            fixed (byte* bufPtr = array)
            {
                while (count > 0)
                {
                    int bytesWritten = CheckFileCall(Interop.Sys.Write(_fileHandle, bufPtr + offset, count));
                    Debug.Assert(bytesWritten <= count);

                    _filePosition += bytesWritten;
                    count -= bytesWritten;
                    offset += bytesWritten;
                }
            }
        }

        /// <summary>
        /// Asynchronously writes a sequence of bytes to the current stream, advances
        /// the current position within this stream by the number of bytes written, and
        /// monitors cancellation requests.
        /// </summary>
        /// <param name="buffer">The buffer to write data from.</param>
        /// <param name="offset">The zero-based byte offset in buffer from which to begin copying bytes to the stream.</param>
        /// <param name="count">The maximum number of bytes to write.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        private Task WriteAsyncInternal(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            if (_fileHandle.IsClosed)
                throw Error.GetFileNotOpen();

            if (_useAsyncIO)
            {
                if (!CanWrite) // match Windows behavior; this gets thrown synchronously
                {
                    throw Error.GetWriteNotSupported();
                }

                // Serialize operations using the semaphore.
                Task waitTask = _asyncState.WaitAsync();

                // If we got ownership immediately, and if there's enough space in our buffer
                // to buffer the entire write request, then do so and we're done.
                if (waitTask.Status == TaskStatus.RanToCompletion)
                {
                    int spaceRemaining = _bufferLength - _writePos;
                    if (spaceRemaining >= count)
                    {
                        try
                        {
                            PrepareForWriting();

                            Buffer.BlockCopy(buffer, offset, GetBuffer(), _writePos, count);
                            _writePos += count;

                            return Task.CompletedTask;
                        }
                        catch (Exception exc)
                        {
                            return Task.FromException(exc);
                        }
                        finally
                        {
                            _asyncState.Release();
                        }
                    }
                }

                // Otherwise, issue the whole request asynchronously.
                _asyncState.Update(buffer, offset, count);
                return waitTask.ContinueWith((t, s) =>
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
                    var thisRef = (FileStream)s;
                    try
                    {
                        byte[] b = thisRef._asyncState._buffer;
                        thisRef._asyncState._buffer = null; // remove reference to user's buffer
                        thisRef.WriteCore(b, thisRef._asyncState._offset, thisRef._asyncState._count);
                    }
                    finally { thisRef._asyncState.Release(); }
                }, this, CancellationToken.None, TaskContinuationOptions.DenyChildAttach, TaskScheduler.Default);
            }
            else
            {
                return base.WriteAsync(buffer, offset, count, cancellationToken);
            }
        }

        /// <summary>
        /// Writes a byte to the current position in the stream and advances the position
        /// within the stream by one byte.
        /// </summary>
        /// <param name="value">The byte to write to the stream.</param>
        public override void WriteByte(byte value) // avoids an array allocation in the base implementation
        {
            if (_useAsyncIO)
            {
                _asyncState.Wait();
                try { WriteByteCore(value); }
                finally { _asyncState.Release(); }
            }
            else
            {
                WriteByteCore(value);
            }
        }

        /// <summary>Prevents other processes from reading from or writing to the FileStream.</summary>
        /// <param name="position">The beginning of the range to lock.</param>
        /// <param name="length">The range to be locked.</param>
        private void LockInternal(long position, long length)
        {
            // TODO #5964: Implement this with fcntl and F_SETLK in System.Native
            throw new PlatformNotSupportedException();
        }

        /// <summary>Allows access by other processes to all or part of a file that was previously locked.</summary>
        /// <param name="position">The beginning of the range to unlock.</param>
        /// <param name="length">The range to be unlocked.</param>
        private void UnlockInternal(long position, long length)
        {
            // TODO #5964: Implement this with fcntl and F_SETLK in System.Native
            throw new PlatformNotSupportedException();
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
                throw Error.GetFileNotOpen();
            }
            if (!CanSeek)
            {
                throw Error.GetSeekNotSupported();
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
                oldPos = SeekCore(0, SeekOrigin.Current);
            }

            // Jump to the new location
            long pos = SeekCore(offset, origin);

            // Prevent users from overwriting data in a file that was opened in append mode.
            if (_appendStart != -1 && pos < _appendStart)
            {
                SeekCore(oldPos, SeekOrigin.Begin);
                throw new IOException(SR.IO_SeekAppendOverwrite);
            }

            // Return the new position
            return pos;
        }

        /// <summary>Sets the current position of this stream to the given value.</summary>
        /// <param name="offset">The point relative to origin from which to begin seeking. </param>
        /// <param name="origin">
        /// Specifies the beginning, the end, or the current position as a reference 
        /// point for offset, using a value of type SeekOrigin.
        /// </param>
        /// <returns>The new position in the stream.</returns>
        private long SeekCore(long offset, SeekOrigin origin)
        {
            Debug.Assert(!_fileHandle.IsClosed && (GetType() != typeof(FileStream) || CanSeek)); // verify that we can seek, but only if CanSeek won't be a virtual call (which could happen in the ctor)
            Debug.Assert(origin >= SeekOrigin.Begin && origin <= SeekOrigin.End);

            long pos = CheckFileCall(Interop.Sys.LSeek(_fileHandle, offset, (Interop.Sys.SeekWhence)(int)origin)); // SeekOrigin values are the same as Interop.libc.SeekWhence values
            _filePosition = pos;
            return pos;
        }

        private long CheckFileCall(long result, bool ignoreNotSupported = false)
        {
            if (result < 0)
            {
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                if (!(ignoreNotSupported && errorInfo.Error == Interop.Error.ENOTSUP))
                {
                    throw Interop.GetExceptionForIoErrno(errorInfo, _path, isDirectory: false);
                }
            }

            return result;
        }

        private int CheckFileCall(int result, bool ignoreNotSupported = false)
        {
            CheckFileCall((long)result, ignoreNotSupported);

            return result;
        }

        /// <summary>State used when the stream is in async mode.</summary>
        private sealed class AsyncState : SemaphoreSlim
        {
            /// <summary>The caller's buffer currently being used by the active async operation.</summary>
            internal byte[] _buffer;
            /// <summary>The caller's offset currently being used by the active async operation.</summary>
            internal int _offset;
            /// <summary>The caller's count currently being used by the active async operation.</summary>
            internal int _count;
            /// <summary>The last task successfully, synchronously returned task from ReadAsync.</summary>
            internal Task<int> _lastSuccessfulReadTask;

            /// <summary>Initialize the AsyncState.</summary>
            internal AsyncState() : base(initialCount: 1, maxCount: 1) { }

            /// <summary>Sets the active buffer, offset, and count.</summary>
            internal void Update(byte[] buffer, int offset, int count)
            {
                _buffer = buffer;
                _offset = offset;
                _count = count;
            }
        }
    }
}
