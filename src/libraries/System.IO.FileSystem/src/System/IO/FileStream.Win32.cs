// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using System.Runtime.CompilerServices;

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

namespace System.IO
{
    public partial class FileStream : Stream
    {
        private bool _canSeek;
        private bool _isPipe;      // Whether to disable async buffering code.
        private long _appendStart; // When appending, prevent overwriting file.

        private static unsafe IOCompletionCallback s_ioCallback = FileStreamCompletionSource.IOCallback;

        private Task<int> _lastSynchronouslyCompletedTask = null;   // cached task for read ops that complete synchronously
        private Task _activeBufferOperation = null;                 // tracks in-progress async ops using the buffer
        private PreAllocatedOverlapped _preallocatedOverlapped;     // optimization for async ops to avoid per-op allocations
        private FileStreamCompletionSource _currentOverlappedOwner; // async op currently using the preallocated overlapped

        private SafeFileHandle OpenHandle(FileMode mode, FileShare share, FileOptions options)
        {
            Interop.mincore.SECURITY_ATTRIBUTES secAttrs = GetSecAttrs(share);

            int fAccess =
                ((_access & FileAccess.Read) == FileAccess.Read ? GENERIC_READ : 0) |
                ((_access & FileAccess.Write) == FileAccess.Write ? GENERIC_WRITE : 0);

            // Our Inheritable bit was stolen from Windows, but should be set in
            // the security attributes class.  Don't leave this bit set.
            share &= ~FileShare.Inheritable;

            // Must use a valid Win32 constant here...
            if (mode == FileMode.Append)
                mode = FileMode.OpenOrCreate;

            int flagsAndAttributes = (int)options;

            // For mitigating local elevation of privilege attack through named pipes
            // make sure we always call CreateFile with SECURITY_ANONYMOUS so that the
            // named pipe server can't impersonate a high privileged client security context
            flagsAndAttributes |= (Interop.mincore.SecurityOptions.SECURITY_SQOS_PRESENT | Interop.mincore.SecurityOptions.SECURITY_ANONYMOUS);

            // Don't pop up a dialog for reading from an empty floppy drive
            uint oldMode = Interop.mincore.SetErrorMode(Interop.mincore.SEM_FAILCRITICALERRORS);
            try
            {
                SafeFileHandle fileHandle = Interop.mincore.SafeCreateFile(_path, fAccess, share, ref secAttrs, mode, flagsAndAttributes, IntPtr.Zero);
                fileHandle.IsAsync = _useAsyncIO;

                if (fileHandle.IsInvalid)
                {
                    // Return a meaningful exception with the full path.

                    // NT5 oddity - when trying to open "C:\" as a Win32FileStream,
                    // we usually get ERROR_PATH_NOT_FOUND from the OS.  We should
                    // probably be consistent w/ every other directory.
                    int errorCode = Marshal.GetLastWin32Error();

                    if (errorCode == Interop.mincore.Errors.ERROR_PATH_NOT_FOUND && _path.Equals(Directory.InternalGetDirectoryRoot(_path)))
                        errorCode = Interop.mincore.Errors.ERROR_ACCESS_DENIED;

                    throw Win32Marshal.GetExceptionForWin32Error(errorCode, _path);
                }

                return fileHandle;
            }
            finally
            {
                Interop.mincore.SetErrorMode(oldMode);
            }
        }

        private void Init(FileMode mode, FileShare share)
        {
            // Disallow access to all non-file devices from the Win32FileStream
            // constructors that take a String.  Everyone else can call 
            // CreateFile themselves then use the constructor that takes an 
            // IntPtr.  Disallows "con:", "com1:", "lpt1:", etc.
            int fileType = Interop.mincore.GetFileType(_fileHandle);
            if (fileType != Interop.mincore.FileTypes.FILE_TYPE_DISK)
            {
                _fileHandle.Dispose();
                throw new NotSupportedException(SR.NotSupported_FileStreamOnNonFiles);
            }

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
                _appendStart = SeekCore(0, SeekOrigin.End);
            }
            else
            {
                _appendStart = -1;
            }
        }

        private void InitFromHandle(SafeFileHandle handle)
        {
            int handleType = Interop.mincore.GetFileType(_fileHandle);
            Debug.Assert(handleType == Interop.mincore.FileTypes.FILE_TYPE_DISK || handleType == Interop.mincore.FileTypes.FILE_TYPE_PIPE || handleType == Interop.mincore.FileTypes.FILE_TYPE_CHAR, "FileStream was passed an unknown file type!");

            _canSeek = handleType == Interop.mincore.FileTypes.FILE_TYPE_DISK;
            _isPipe = handleType == Interop.mincore.FileTypes.FILE_TYPE_PIPE;

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
            if (_useAsyncIO && !GetSuppressBindHandle(handle))
            {
                try
                {
                    _fileHandle.ThreadPoolBinding = ThreadPoolBoundHandle.BindHandle(_fileHandle);
                }
                catch (Exception ex)
                {
                    // If you passed in a synchronous handle and told us to use
                    // it asynchronously, throw here.
                    throw new ArgumentException(SR.Arg_HandleNotAsync, nameof(handle), ex);
                }
            }
            else if (!_useAsyncIO)
            {
                if (handleType != Interop.mincore.FileTypes.FILE_TYPE_PIPE)
                    VerifyHandleIsSync();
            }

            if (_canSeek)
                SeekCore(0, SeekOrigin.Current);
            else
                _filePosition = 0;
        }

        private static bool GetSuppressBindHandle(SafeFileHandle handle)
        {
            return handle.IsAsync.HasValue ? handle.IsAsync.Value : false;
        }

        private unsafe static Interop.mincore.SECURITY_ATTRIBUTES GetSecAttrs(FileShare share)
        {
            Interop.mincore.SECURITY_ATTRIBUTES secAttrs = default(Interop.mincore.SECURITY_ATTRIBUTES);
            if ((share & FileShare.Inheritable) != 0)
            {
                secAttrs = new Interop.mincore.SECURITY_ATTRIBUTES();
                secAttrs.nLength = (uint)sizeof(Interop.mincore.SECURITY_ATTRIBUTES);

                secAttrs.bInheritHandle = Interop.BOOL.TRUE;
            }
            return secAttrs;
        }

        // Verifies that this handle supports synchronous IO operations (unless you
        // didn't open it for either reading or writing).
        private unsafe void VerifyHandleIsSync()
        {
            Debug.Assert(!_useAsyncIO);

            // Do NOT use this method on pipes.  Reading or writing to a pipe may
            // cause an app to block incorrectly, introducing a deadlock (depending
            // on whether a write will wake up an already-blocked thread or this
            // Win32FileStream's thread).
            Debug.Assert(Interop.mincore.GetFileType(_fileHandle) != Interop.mincore.FileTypes.FILE_TYPE_PIPE);

            byte* bytes = stackalloc byte[1];
            int numBytesReadWritten;
            int r = -1;

            // If the handle is a pipe, ReadFile will block until there
            // has been a write on the other end.  We'll just have to deal with it,
            // For the read end of a pipe, you can mess up and 
            // accidentally read synchronously from an async pipe.
            if ((_access & FileAccess.Read) != 0) // don't use the virtual CanRead or CanWrite, as this may be used in the ctor
            {
                r = Interop.mincore.ReadFile(_fileHandle, bytes, 0, out numBytesReadWritten, IntPtr.Zero);
            }
            else if ((_access & FileAccess.Write) != 0) // don't use the virtual CanRead or CanWrite, as this may be used in the ctor
            {
                r = Interop.mincore.WriteFile(_fileHandle, bytes, 0, out numBytesReadWritten, IntPtr.Zero);
            }

            if (r == 0)
            {
                int errorCode = GetLastWin32ErrorAndDisposeHandleIfInvalid(throwIfInvalidHandle: true);
                if (errorCode == ERROR_INVALID_PARAMETER)
                    throw new ArgumentException(SR.Arg_HandleNotSync, "handle");
            }
        }

        private bool HasActiveBufferOperation
        {
            get { return _activeBufferOperation != null && !_activeBufferOperation.IsCompleted; }
        }

        public override bool CanSeek
        {
            get { return _canSeek; }
        }

        private long GetLengthInternal()
        {
            Interop.mincore.FILE_STANDARD_INFO info = new Interop.mincore.FILE_STANDARD_INFO();

            if (!Interop.mincore.GetFileInformationByHandleEx(_fileHandle, Interop.mincore.FILE_INFO_BY_HANDLE_CLASS.FileStandardInfo, out info, (uint)Marshal.SizeOf<Interop.mincore.FILE_STANDARD_INFO>()))
                throw Win32Marshal.GetExceptionForLastWin32Error();
            long len = info.EndOfFile;
            // If we're writing near the end of the file, we must include our
            // internal buffer in our Length calculation.  Don't flush because
            // we use the length of the file in our async write method.
            if (_writePos > 0 && _filePosition + _writePos > len)
                len = _writePos + _filePosition;
            return len;
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
                if (_fileHandle != null && !_fileHandle.IsClosed)
                {
                    // Flush data to disk iff we were writing.  After 
                    // thinking about this, we also don't need to flush
                    // our read position, regardless of whether the handle
                    // was exposed to the user.  They probably would NOT 
                    // want us to do this.
                    if (_writePos > 0)
                    {
                        FlushWriteBuffer(!disposing);
                    }
                }
            }
            finally
            {
                if (_fileHandle != null && !_fileHandle.IsClosed)
                {
                    if (_fileHandle.ThreadPoolBinding != null)
                        _fileHandle.ThreadPoolBinding.Dispose();

                    _fileHandle.Dispose();
                }

                if (_preallocatedOverlapped != null)
                    _preallocatedOverlapped.Dispose();

                _canSeek = false;

                // Don't set the buffer to null, to avoid a NullReferenceException
                // when users have a race condition in their code (i.e. they call
                // Close when calling another method on Stream like Read).
                //_buffer = null;
                base.Dispose(disposing);
            }
        }

        private void FlushOSBuffer()
        {
            if (!Interop.mincore.FlushFileBuffers(_fileHandle))
            {
                throw Win32Marshal.GetExceptionForLastWin32Error();
            }
        }

        // Returns a task that flushes the internal write buffer
        private Task FlushWriteAsync(CancellationToken cancellationToken)
        {
            Debug.Assert(_useAsyncIO);
            Debug.Assert(_readPos == 0 && _readLength == 0, "FileStream: Read buffer must be empty in FlushWriteAsync!");

            // If the buffer is already flushed, don't spin up the OS write
            if (_writePos == 0) return Task.CompletedTask;

            Task flushTask = WriteInternalCoreAsync(GetBuffer(), 0, _writePos, cancellationToken);
            _writePos = 0;

            // Update the active buffer operation
            _activeBufferOperation = HasActiveBufferOperation ? 
                Task.WhenAll(_activeBufferOperation, flushTask) : 
                flushTask;

            return flushTask;
        }

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
                WriteCore(GetBuffer(), 0, _writePos);
            }

            _writePos = 0;
        }

        private void SetLengthInternal(long value)
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
        private void SetLengthCore(long value)
        {
            Debug.Assert(value >= 0, "value >= 0");
            long origPos = _filePosition;

            VerifyOSHandlePosition();
            if (_filePosition != value)
                SeekCore(value, SeekOrigin.Begin);
            if (!Interop.mincore.SetEndOfFile(_fileHandle))
            {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode == Interop.mincore.Errors.ERROR_INVALID_PARAMETER)
                    throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_FileLengthTooBig);
                throw Win32Marshal.GetExceptionForWin32Error(errorCode);
            }
            // Return file pointer to where it was before setting length
            if (origPos != value)
            {
                if (origPos < value)
                    SeekCore(origPos, SeekOrigin.Begin);
                else
                    SeekCore(0, SeekOrigin.End);
            }
        }

        // Instance method to help code external to this MarshalByRefObject avoid
        // accessing its fields by ref.  This avoids a compiler warning.
        private FileStreamCompletionSource CompareExchangeCurrentOverlappedOwner(FileStreamCompletionSource newSource, FileStreamCompletionSource existingSource) => Interlocked.CompareExchange(ref _currentOverlappedOwner, newSource, existingSource);

        public override int Read(byte[] array, int offset, int count)
        {
            ValidateReadWriteArgs(array, offset, count);
            return ReadCore(array, offset, count);
        }

        private int ReadCore(byte[] array, int offset, int count)
        {
            Debug.Assert((_readPos == 0 && _readLength == 0 && _writePos >= 0) || (_writePos == 0 && _readPos <= _readLength),
                "We're either reading or writing, but not both.");

            bool isBlocked = false;
            int n = _readLength - _readPos;
            // if the read buffer is empty, read into either user's array or our
            // buffer, depending on number of bytes user asked for and buffer size.
            if (n == 0)
            {
                if (!CanRead) throw Error.GetReadNotSupported();
                if (_writePos > 0) FlushWriteBuffer();
                if (!CanSeek || (count >= _bufferLength))
                {
                    n = ReadNative(array, offset, count);
                    // Throw away read buffer.
                    _readPos = 0;
                    _readLength = 0;
                    return n;
                }
                n = ReadNative(GetBuffer(), 0, _bufferLength);
                if (n == 0) return 0;
                isBlocked = n < _bufferLength;
                _readPos = 0;
                _readLength = n;
            }
            // Now copy min of count or numBytesAvailable (i.e. near EOF) to array.
            if (n > count) n = count;
            Buffer.BlockCopy(GetBuffer(), _readPos, array, offset, n);
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
                if (n < count && !isBlocked)
                {
                    Debug.Assert(_readPos == _readLength, "Read buffer should be empty!");
                    int moreBytesRead = ReadNative(array, offset + n, count - n);
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
        private void AssertCanRead(byte[] buffer, int offset, int count)
        {
            Debug.Assert(!_fileHandle.IsClosed, "!_fileHandle.IsClosed");
            Debug.Assert(CanRead, "CanRead");
            Debug.Assert(buffer != null, "buffer != null");
            Debug.Assert(_writePos == 0, "_writePos == 0");
            Debug.Assert(offset >= 0, "offset is negative");
            Debug.Assert(count >= 0, "count is negative");
        }

        private unsafe int ReadNative(byte[] buffer, int offset, int count)
        {
            AssertCanRead(buffer, offset, count);

            if (_useAsyncIO)
                return ReadNativeAsync(buffer, offset, count, 0, CancellationToken.None).GetAwaiter().GetResult();

            // Make sure we are reading from the right spot
            VerifyOSHandlePosition();

            int errorCode = 0;
            int r = ReadFileNative(_fileHandle, buffer, offset, count, null, out errorCode);

            if (r == -1)
            {
                // For pipes, ERROR_BROKEN_PIPE is the normal end of the pipe.
                if (errorCode == ERROR_BROKEN_PIPE)
                {
                    r = 0;
                }
                else
                {
                    if (errorCode == ERROR_INVALID_PARAMETER)
                        throw new ArgumentException(SR.Arg_HandleNotSync, "_fileHandle");

                    throw Win32Marshal.GetExceptionForWin32Error(errorCode);
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
            if (_fileHandle.IsClosed) throw Error.GetFileNotOpen();
            if (!CanSeek) throw Error.GetSeekNotSupported();

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
            long pos = SeekCore(offset, origin);

            // Prevent users from overwriting data in a file that was opened in
            // append mode.
            if (_appendStart != -1 && pos < _appendStart)
            {
                SeekCore(oldPos, SeekOrigin.Begin);
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
                        //Console.WriteLine("Seek: seeked for 0, adjusting buffer back by: "+_readPos+"  _readLen: "+_readLen);
                        Buffer.BlockCopy(GetBuffer(), _readPos, GetBuffer(), 0, _readLength - _readPos);
                        _readLength -= _readPos;
                        _readPos = 0;
                    }
                    // If we still have buffered data, we must update the stream's 
                    // position so our Position property is correct.
                    if (_readLength > 0)
                        SeekCore(_readLength, SeekOrigin.Current);
                }
                else if (oldPos - _readPos < pos && pos < oldPos + _readLength - _readPos)
                {
                    int diff = (int)(pos - oldPos);
                    //Console.WriteLine("Seek: diff was "+diff+", readpos was "+_readPos+"  adjusting buffer - shrinking by "+ (_readPos + diff));
                    Buffer.BlockCopy(GetBuffer(), _readPos + diff, GetBuffer(), 0, _readLength - (_readPos + diff));
                    _readLength -= (_readPos + diff);
                    _readPos = 0;
                    if (_readLength > 0)
                        SeekCore(_readLength, SeekOrigin.Current);
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
        // This is called during construction so it should avoid any virtual
        // calls
        private long SeekCore(long offset, SeekOrigin origin)
        {
            Debug.Assert(!_fileHandle.IsClosed && _canSeek, "!_handle.IsClosed && _parent.CanSeek");
            Debug.Assert(origin >= SeekOrigin.Begin && origin <= SeekOrigin.End, "origin>=SeekOrigin.Begin && origin<=SeekOrigin.End");
            long ret = 0;

            if (!Interop.mincore.SetFilePointerEx(_fileHandle, offset, out ret, (uint)origin))
            {
                int errorCode = GetLastWin32ErrorAndDisposeHandleIfInvalid();
                throw Win32Marshal.GetExceptionForWin32Error(errorCode);
            }

            _filePosition = ret;
            return ret;
        }

        partial void OnBufferAllocated()
        {
            Debug.Assert(_buffer != null);
            Debug.Assert(_preallocatedOverlapped == null);

            if (_useAsyncIO)
                _preallocatedOverlapped = new PreAllocatedOverlapped(s_ioCallback, this, _buffer);
        }

        public override void Write(byte[] array, int offset, int count)
        {
            ValidateReadWriteArgs(array, offset, count);

            if (_writePos == 0)
            {
                // Ensure we can write to the stream, and ready buffer for writing.
                if (!CanWrite) throw Error.GetWriteNotSupported();
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
                    if (numBytes > count)
                        numBytes = count;
                    Buffer.BlockCopy(array, offset, GetBuffer(), _writePos, numBytes);
                    _writePos += numBytes;
                    if (count == numBytes) return;
                    offset += numBytes;
                    count -= numBytes;
                }
                // Reset our buffer.  We essentially want to call FlushWrite
                // without calling Flush on the underlying Stream.

                if (_useAsyncIO)
                {
                    WriteInternalCoreAsync(GetBuffer(), 0, _writePos, CancellationToken.None).GetAwaiter().GetResult();
                }
                else
                {
                    WriteCore(GetBuffer(), 0, _writePos);
                }
                _writePos = 0;
            }
            // If the buffer would slow writes down, avoid buffer completely.
            if (count >= _bufferLength)
            {
                Debug.Assert(_writePos == 0, "FileStream cannot have buffered data to write here!  Your stream will be corrupted.");
                WriteCore(array, offset, count);
                return;
            }
            else if (count == 0)
            {
                return;  // Don't allocate a buffer then call memcpy for 0 bytes.
            }

            // Copy remaining bytes into buffer, to write at a later date.
            Buffer.BlockCopy(array, offset, GetBuffer(), _writePos, count);
            _writePos = count;
            return;
        }

        private unsafe void WriteCore(byte[] buffer, int offset, int count)
        {
            Debug.Assert(!_fileHandle.IsClosed, "!_handle.IsClosed");
            Debug.Assert(CanWrite, "_parent.CanWrite");

            Debug.Assert(buffer != null, "buffer != null");
            Debug.Assert(_readPos == _readLength, "_readPos == _readLen");
            Debug.Assert(offset >= 0, "offset is negative");
            Debug.Assert(count >= 0, "count is negative");
            if (_useAsyncIO)
            {
                WriteInternalCoreAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
                return;
            }

            // Make sure we are writing to the position that we think we are
            VerifyOSHandlePosition();

            int errorCode = 0;
            int r = WriteFileNative(_fileHandle, buffer, offset, count, null, out errorCode);

            if (r == -1)
            {
                // For pipes, ERROR_NO_DATA is not an error, but the pipe is closing.
                if (errorCode == ERROR_NO_DATA)
                {
                    r = 0;
                }
                else
                {
                    // ERROR_INVALID_PARAMETER may be returned for writes
                    // where the position is too large (i.e. writing at Int64.MaxValue 
                    // on Win9x) OR for synchronous writes to a handle opened 
                    // asynchronously.
                    if (errorCode == ERROR_INVALID_PARAMETER)
                        throw new IOException(SR.IO_FileTooLongOrHandleNotSync);
                    throw Win32Marshal.GetExceptionForWin32Error(errorCode);
                }
            }
            Debug.Assert(r >= 0, "FileStream's WriteCore is likely broken.");
            _filePosition += r;
            return;
        }

        private Task<int> ReadAsyncInternal(byte[] array, int offset, int numBytes, CancellationToken cancellationToken)
        {
            // If async IO is not supported on this platform or 
            // if this Win32FileStream was not opened with FileOptions.Asynchronous.
            if (!_useAsyncIO)
            {
                return base.ReadAsync(array, offset, numBytes, cancellationToken);
            }

            if (!CanRead) throw Error.GetReadNotSupported();

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
                    int n = _readLength - _readPos;
                    if (n > numBytes) n = numBytes;
                    Buffer.BlockCopy(GetBuffer(), _readPos, array, offset, n);
                    _readPos += n;

                    // Return a completed task
                    return TaskFromResultOrCache(n);
                }
                else
                {
                    Debug.Assert(_writePos == 0, "Win32FileStream must not have buffered write data here!  Pipes should be unidirectional.");
                    return ReadNativeAsync(array, offset, numBytes, 0, cancellationToken);
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

                if (numBytes < _bufferLength)
                {
                    Task<int> readTask = ReadNativeAsync(GetBuffer(), 0, _bufferLength, 0, cancellationToken);
                    _readLength = readTask.GetAwaiter().GetResult();
                    int n = _readLength;
                    if (n > numBytes) n = numBytes;
                    Buffer.BlockCopy(GetBuffer(), 0, array, offset, n);
                    _readPos = n;

                    // Return a completed task (recycling the one above if possible)
                    return (_readLength == n ? readTask : TaskFromResultOrCache(n));
                }
                else
                {
                    // Here we're making our position pointer inconsistent
                    // with our read buffer.  Throw away the read buffer's contents.
                    _readPos = 0;
                    _readLength = 0;
                    return ReadNativeAsync(array, offset, numBytes, 0, cancellationToken);
                }
            }
            else
            {
                int n = _readLength - _readPos;
                if (n > numBytes) n = numBytes;
                Buffer.BlockCopy(GetBuffer(), _readPos, array, offset, n);
                _readPos += n;

                if (n >= numBytes)
                {
                    // Return a completed task
                    return TaskFromResultOrCache(n);
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
                    return ReadNativeAsync(array, offset + n, numBytes - n, n, cancellationToken);
                }
            }
        }

        unsafe private Task<int> ReadNativeAsync(byte[] bytes, int offset, int numBytes, int numBufferedBytesRead, CancellationToken cancellationToken)
        {
            AssertCanRead(bytes, offset, numBytes);
            Debug.Assert(_useAsyncIO, "ReadNativeAsync doesn't work on synchronous file streams!");

            // Create and store async stream class library specific data in the async result

            FileStreamCompletionSource completionSource = new FileStreamCompletionSource(this, numBufferedBytesRead, bytes, cancellationToken);
            NativeOverlapped* intOverlapped = completionSource.Overlapped;

            // Calculate position in the file we should be at after the read is done
            if (CanSeek)
            {
                long len = Length;

                // Make sure we are reading from the position that we think we are
                VerifyOSHandlePosition();

                if (_filePosition + numBytes > len)
                {
                    if (_filePosition <= len)
                        numBytes = (int)(len - _filePosition);
                    else
                        numBytes = 0;
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
                SeekCore(numBytes, SeekOrigin.Current);
            }

            // queue an async ReadFile operation and pass in a packed overlapped
            int errorCode = 0;
            int r = ReadFileNative(_fileHandle, bytes, offset, numBytes, intOverlapped, out errorCode);
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
            if (r == -1 && numBytes != -1)
            {
                // For pipes, when they hit EOF, they will come here.
                if (errorCode == ERROR_BROKEN_PIPE)
                {
                    // Not an error, but EOF.  AsyncFSCallback will NOT be 
                    // called.  Call the user callback here.

                    // We clear the overlapped status bit for this special case.
                    // Failure to do so looks like we are freeing a pending overlapped later.
                    intOverlapped->InternalLow = IntPtr.Zero;
                    completionSource.SetCompletedSynchronously(0);
                }
                else if (errorCode != ERROR_IO_PENDING)
                {
                    if (!_fileHandle.IsClosed && CanSeek)  // Update Position - It could be anywhere.
                    {
                        SeekCore(0, SeekOrigin.Current);
                    }

                    completionSource.ReleaseNativeResource();

                    if (errorCode == ERROR_HANDLE_EOF)
                    {
                        throw Error.GetEndOfFile();
                    }
                    else
                    {
                        throw Win32Marshal.GetExceptionForWin32Error(errorCode);
                    }
                }
                else
                {
                    // Only once the IO is pending do we register for cancellation
                    completionSource.RegisterForCancellation();
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
                //Console.WriteLine("ReadFile returned: "+r+" (0x"+Int32.Format(r, "x")+")  The IO completed synchronously, but the user callback was called on a separate thread");
            }

            return completionSource.Task;
        }

        // Reads a byte from the file stream.  Returns the byte cast to an int
        // or -1 if reading from the end of the stream.
        public override int ReadByte()
        {
            return ReadByteCore();
        }

        private Task WriteAsyncInternal(byte[] array, int offset, int numBytes, CancellationToken cancellationToken)
        {
            // If async IO is not supported on this platform or 
            // if this Win32FileStream was not opened with FileOptions.Asynchronous.
            if (!_useAsyncIO)
            {
                return base.WriteAsync(array, offset, numBytes, cancellationToken);
            }

            if (!CanWrite) throw Error.GetWriteNotSupported();

            Debug.Assert((_readPos == 0 && _readLength == 0 && _writePos >= 0) || (_writePos == 0 && _readPos <= _readLength), "We're either reading or writing, but not both.");
            Debug.Assert(!_isPipe || (_readPos == 0 && _readLength == 0), "Win32FileStream must not have buffered data here!  Pipes should be unidirectional.");

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
                if (numBytes < _bufferLength && !HasActiveBufferOperation && numBytes <= remainingBuffer)
                {
                    Buffer.BlockCopy(array, offset, GetBuffer(), _writePos, numBytes);
                    _writePos += numBytes;
                    writeDataStoredInBuffer = true;

                    // There is one special-but-common case, common because devs often use
                    // byte[] sizes that are powers of 2 and thus fit nicely into our buffer, which is
                    // also a power of 2. If after our write the buffer still has remaining space,
                    // then we're done and can return a completed task now.  But if we filled the buffer
                    // completely, we want to do the asynchronous flush/write as part of this operation 
                    // rather than waiting until the next write that fills the buffer.
                    if (numBytes != remainingBuffer)
                        return Task.CompletedTask;

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
            Task flushTask = null;
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
                    return flushTask;
                }
            }

            Debug.Assert(!writeDataStoredInBuffer);
            Debug.Assert(_writePos == 0);

            // Finally, issue the write asynchronously, and return a Task that logically
            // represents the write operation, including any flushing done.
            Task writeTask = WriteInternalCoreAsync(array, offset, numBytes, cancellationToken);
            return
                (flushTask == null || flushTask.Status == TaskStatus.RanToCompletion) ? writeTask :
                (writeTask.Status == TaskStatus.RanToCompletion) ? flushTask :
                Task.WhenAll(flushTask, writeTask);
        }

        private unsafe Task WriteInternalCoreAsync(byte[] bytes, int offset, int numBytes, CancellationToken cancellationToken)
        {
            Debug.Assert(!_fileHandle.IsClosed, "!_handle.IsClosed");
            Debug.Assert(CanWrite, "_parent.CanWrite");
            Debug.Assert(bytes != null, "bytes != null");
            Debug.Assert(_readPos == _readLength, "_readPos == _readLen");
            Debug.Assert(_useAsyncIO, "WriteInternalCoreAsync doesn't work on synchronous file streams!");
            Debug.Assert(offset >= 0, "offset is negative");
            Debug.Assert(numBytes >= 0, "numBytes is negative");

            // Create and store async stream class library specific data in the async result
            FileStreamCompletionSource completionSource = new FileStreamCompletionSource(this, 0, bytes, cancellationToken);
            NativeOverlapped* intOverlapped = completionSource.Overlapped;

            if (CanSeek)
            {
                // Make sure we set the length of the file appropriately.
                long len = Length;
                //Console.WriteLine("WriteInternalCoreAsync - Calculating end pos.  pos: "+pos+"  len: "+len+"  numBytes: "+numBytes);

                // Make sure we are writing to the position that we think we are
                VerifyOSHandlePosition();

                if (_filePosition + numBytes > len)
                {
                    //Console.WriteLine("WriteInternalCoreAsync - Setting length to: "+(pos + numBytes));
                    SetLengthCore(_filePosition + numBytes);
                }

                // Now set the position to read from in the NativeOverlapped struct
                // For pipes, we should leave the offset fields set to 0.
                intOverlapped->OffsetLow = (int)_filePosition;
                intOverlapped->OffsetHigh = (int)(_filePosition >> 32);

                // When using overlapped IO, the OS is not supposed to 
                // touch the file pointer location at all.  We will adjust it 
                // ourselves.  This isn't threadsafe.
                SeekCore(numBytes, SeekOrigin.Current);
            }

            //Console.WriteLine("WriteInternalCoreAsync finishing.  pos: "+pos+"  numBytes: "+numBytes+"  _pos: "+_pos+"  Position: "+Position);

            int errorCode = 0;
            // queue an async WriteFile operation and pass in a packed overlapped
            int r = WriteFileNative(_fileHandle, bytes, offset, numBytes, intOverlapped, out errorCode);

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
            if (r == -1 && numBytes != -1)
            {
                //Console.WriteLine("WriteFile returned 0;  Write will complete asynchronously (if errorCode==3e5)  errorCode: 0x{0:x}", errorCode);

                // For pipes, when they are closed on the other side, they will come here.
                if (errorCode == ERROR_NO_DATA)
                {
                    // Not an error, but EOF. AsyncFSCallback will NOT be called.
                    // Completing TCS and return cached task allowing the GC to collect TCS.
                    completionSource.SetCompletedSynchronously(0);
                    return Task.CompletedTask;
                }
                else if (errorCode != ERROR_IO_PENDING)
                {
                    if (!_fileHandle.IsClosed && CanSeek)  // Update Position - It could be anywhere.
                    {
                        SeekCore(0, SeekOrigin.Current);
                    }

                    completionSource.ReleaseNativeResource();

                    if (errorCode == ERROR_HANDLE_EOF)
                    {
                        throw Error.GetEndOfFile();
                    }
                    else
                    {
                        throw Win32Marshal.GetExceptionForWin32Error(errorCode);
                    }
                }
                else // ERROR_IO_PENDING
                {
                    // Only once the IO is pending do we register for cancellation
                    completionSource.RegisterForCancellation();
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
                //Console.WriteLine("WriteFile returned: "+r+" (0x"+Int32.Format(r, "x")+")  The IO completed synchronously, but the user callback was called on another thread.");
            }

            return completionSource.Task;
        }

        public override void WriteByte(byte value)
        {
            WriteByteCore(value);
        }

        // Windows API definitions, from winbase.h and others

        private const int FILE_ATTRIBUTE_NORMAL = 0x00000080;
        private const int FILE_ATTRIBUTE_ENCRYPTED = 0x00004000;
        private const int FILE_FLAG_OVERLAPPED = 0x40000000;
        internal const int GENERIC_READ = unchecked((int)0x80000000);
        private const int GENERIC_WRITE = 0x40000000;

        private const int FILE_BEGIN = 0;
        private const int FILE_CURRENT = 1;
        private const int FILE_END = 2;

        // Error codes (not HRESULTS), from winerror.h
        internal const int ERROR_BROKEN_PIPE = 109;
        internal const int ERROR_NO_DATA = 232;
        private const int ERROR_HANDLE_EOF = 38;
        private const int ERROR_INVALID_PARAMETER = 87;
        private const int ERROR_IO_PENDING = 997;

        // __ConsoleStream also uses this code. 
        private unsafe int ReadFileNative(SafeFileHandle handle, byte[] bytes, int offset, int count, NativeOverlapped* overlapped, out int errorCode)
        {
            Debug.Assert(handle != null, "handle != null");
            Debug.Assert(offset >= 0, "offset >= 0");
            Debug.Assert(count >= 0, "count >= 0");
            Debug.Assert(bytes != null, "bytes != null");
            // Don't corrupt memory when multiple threads are erroneously writing
            // to this stream simultaneously.
            if (bytes.Length - offset < count)
                throw new IndexOutOfRangeException(SR.IndexOutOfRange_IORaceCondition);

            Debug.Assert((_useAsyncIO && overlapped != null) || (!_useAsyncIO && overlapped == null), "Async IO and overlapped parameters inconsistent in call to ReadFileNative.");

            // You can't use the fixed statement on an array of length 0.
            if (bytes.Length == 0)
            {
                errorCode = 0;
                return 0;
            }

            int r = 0;
            int numBytesRead = 0;

            fixed (byte* p = bytes)
            {
                if (_useAsyncIO)
                    r = Interop.mincore.ReadFile(handle, p + offset, count, IntPtr.Zero, overlapped);
                else
                    r = Interop.mincore.ReadFile(handle, p + offset, count, out numBytesRead, IntPtr.Zero);
            }

            if (r == 0)
            {
                errorCode = GetLastWin32ErrorAndDisposeHandleIfInvalid();
                return -1;
            }
            else
            {
                errorCode = 0;
                return numBytesRead;
            }
        }

        private unsafe int WriteFileNative(SafeFileHandle handle, byte[] bytes, int offset, int count, NativeOverlapped* overlapped, out int errorCode)
        {
            Debug.Assert(handle != null, "handle != null");
            Debug.Assert(offset >= 0, "offset >= 0");
            Debug.Assert(count >= 0, "count >= 0");
            Debug.Assert(bytes != null, "bytes != null");
            // Don't corrupt memory when multiple threads are erroneously writing
            // to this stream simultaneously.  (the OS is reading from
            // the array we pass to WriteFile, but if we read beyond the end and
            // that memory isn't allocated, we could get an AV.)
            if (bytes.Length - offset < count)
                throw new IndexOutOfRangeException(SR.IndexOutOfRange_IORaceCondition);

            Debug.Assert((_useAsyncIO && overlapped != null) || (!_useAsyncIO && overlapped == null), "Async IO and overlapped parameters inconsistent in call to WriteFileNative.");

            // You can't use the fixed statement on an array of length 0.
            if (bytes.Length == 0)
            {
                errorCode = 0;
                return 0;
            }

            int numBytesWritten = 0;
            int r = 0;

            fixed (byte* p = bytes)
            {
                if (_useAsyncIO)
                    r = Interop.mincore.WriteFile(handle, p + offset, count, IntPtr.Zero, overlapped);
                else
                    r = Interop.mincore.WriteFile(handle, p + offset, count, out numBytesWritten, IntPtr.Zero);
            }

            if (r == 0)
            {
                errorCode = GetLastWin32ErrorAndDisposeHandleIfInvalid();
                return -1;
            }
            else
            {
                errorCode = 0;
                return numBytesWritten;
            }
        }

        private int GetLastWin32ErrorAndDisposeHandleIfInvalid(bool throwIfInvalidHandle = false)
        {
            int errorCode = Marshal.GetLastWin32Error();

            // If ERROR_INVALID_HANDLE is returned, it doesn't suffice to set
            // the handle as invalid; the handle must also be closed.
            //
            // Marking the handle as invalid but not closing the handle
            // resulted in exceptions during finalization and locked column
            // values (due to invalid but unclosed handle) in SQL Win32FileStream
            // scenarios.
            //
            // A more mainstream scenario involves accessing a file on a
            // network share. ERROR_INVALID_HANDLE may occur because the network
            // connection was dropped and the server closed the handle. However,
            // the client side handle is still open and even valid for certain
            // operations.
            //
            // Note that _parent.Dispose doesn't throw so we don't need to special case.
            // SetHandleAsInvalid only sets _closed field to true (without
            // actually closing handle) so we don't need to call that as well.
            if (errorCode == Interop.mincore.Errors.ERROR_INVALID_HANDLE)
            {
                _fileHandle.Dispose();

                if (throwIfInvalidHandle)
                    throw Win32Marshal.GetExceptionForWin32Error(errorCode);
            }

            return errorCode;
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            // If we're in sync mode, just use the shared CopyToAsync implementation that does
            // typical read/write looping.  We also need to take this path if this is a derived
            // instance from FileStream, as a derived type could have overridden ReadAsync, in which
            // case our custom CopyToAsync implementation isn't necessarily correct.
            if (!_useAsyncIO || GetType() != typeof(FileStream))
            {
                return base.CopyToAsync(destination, bufferSize, cancellationToken);
            }

            StreamHelpers.ValidateCopyToArgs(this, destination, bufferSize);

            // Bail early for cancellation if cancellation has been requested
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
            }

            // Fail if the file was closed
            if (_fileHandle.IsClosed)
            {
                throw Error.GetFileNotOpen();
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
                    await destination.WriteAsync(GetBuffer(), _readPos, bufferedBytes, cancellationToken).ConfigureAwait(false);
                    _readPos = _readLength = 0;
                }
            }

            // For efficiency, we avoid creating a new task and associated state for each asynchronous read.
            // Instead, we create a single reusable awaitable object that will be triggered when an await completes
            // and reset before going again.
            var readAwaitable = new AsyncCopyToAwaitable(this);

            // Make sure we are reading from the position that we think we are.
            // Only set the position in the awaitable if we can seek (e.g. not for pipes).
            bool canSeek = CanSeek;
            if (canSeek)
            {
                VerifyOSHandlePosition();
                readAwaitable._position = _filePosition;
            }

            // Get the buffer to use for the copy operation, as the base CopyToAsync does. We don't try to use
            // _buffer here, even if it's not null, as concurrent operations are allowed, and another operation may
            // actually be using the buffer already. Plus, it'll be rare for _buffer to be non-null, as typically
            // CopyToAsync is used as the only operation performed on the stream, and the buffer is lazily initialized.
            // Further, typically the CopyToAsync buffer size will be larger than that used by the FileStream, such that
            // we'd likely be unable to use it anyway.  Instead, we rent the buffer from a pool.
            byte[] copyBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            bufferSize = 0; // repurpose bufferSize to be the high water mark for the buffer, to avoid an extra field in the state machine

            // Allocate an Overlapped we can use repeatedly for all operations
            var awaitableOverlapped = new PreAllocatedOverlapped(AsyncCopyToAwaitable.s_callback, readAwaitable, copyBuffer);
            var cancellationReg = default(CancellationTokenRegistration);
            try
            {
                // Register for cancellation.  We do this once for the whole copy operation, and just try to cancel
                // whatever read operation may currently be in progress, if there is one.  It's possible the cancellation
                // request could come in between operations, in which case we flag that with explicit calls to ThrowIfCancellationRequested
                // in the read/write copy loop.
                if (cancellationToken.CanBeCanceled)
                {
                    cancellationReg = cancellationToken.Register(s =>
                    {
                        var innerAwaitable = (AsyncCopyToAwaitable)s;
                        unsafe
                        {
                            lock (innerAwaitable.CancellationLock) // synchronize with cleanup of the overlapped
                            {
                                if (innerAwaitable._nativeOverlapped != null)
                                {
                                    // Try to cancel the I/O.  We ignore the return value, as cancellation is opportunistic and we
                                    // don't want to fail the operation because we couldn't cancel it.
                                    Interop.mincore.CancelIoEx(innerAwaitable._fileStream._fileHandle, innerAwaitable._nativeOverlapped);
                                }
                            }
                        }
                    }, readAwaitable);
                }

                // Repeatedly read from this FileStream and write the results to the destination stream.
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    readAwaitable.ResetForNextOperation();

                    try
                    {
                        bool synchronousSuccess;
                        int errorCode;
                        unsafe
                        {
                            // Allocate a native overlapped for our reusable overlapped, and set position to read based on the next
                            // desired address stored in the awaitable.  (This position may be 0, if either we're at the beginning or
                            // if the stream isn't seekable.)
                            readAwaitable._nativeOverlapped = _fileHandle.ThreadPoolBinding.AllocateNativeOverlapped(awaitableOverlapped);
                            if (canSeek)
                            {
                                readAwaitable._nativeOverlapped->OffsetLow = unchecked((int)readAwaitable._position);
                                readAwaitable._nativeOverlapped->OffsetHigh = (int)(readAwaitable._position >> 32);
                            }

                            // Kick off the read.
                            synchronousSuccess = ReadFileNative(_fileHandle, copyBuffer, 0, copyBuffer.Length, readAwaitable._nativeOverlapped, out errorCode) >= 0;
                        }

                        // If the operation did not synchronously succeed, it either failed or initiated the asynchronous operation.
                        if (!synchronousSuccess)
                        {
                            switch (errorCode)
                            {
                                case ERROR_IO_PENDING:
                                    // Async operation in progress.
                                    break;
                                case ERROR_BROKEN_PIPE:
                                case ERROR_HANDLE_EOF:
                                    // We're at or past the end of the file, and the overlapped callback
                                    // won't be raised in these cases. Mark it as completed so that the await
                                    // below will see it as such.
                                    readAwaitable.MarkCompleted();
                                    break;
                                default:
                                    // Everything else is an error (and there won't be a callback).
                                    throw Win32Marshal.GetExceptionForWin32Error(errorCode);
                            }
                        }

                        // Wait for the async operation (which may or may not have already completed), then throw if it failed.
                        await readAwaitable;
                        switch (readAwaitable._errorCode)
                        {
                            case 0: // success
                                Debug.Assert(readAwaitable._numBytes >= 0, $"Expected non-negative numBytes, got {readAwaitable._numBytes}");
                                break;
                            case ERROR_BROKEN_PIPE: // logically success with 0 bytes read (write end of pipe closed)
                            case ERROR_HANDLE_EOF:  // logically success with 0 bytes read (read at end of file)
                                Debug.Assert(readAwaitable._numBytes == 0, $"Expected 0 bytes read, got {readAwaitable._numBytes}");
                                break;
                            case Interop.mincore.Errors.ERROR_OPERATION_ABORTED: // canceled
                                throw new OperationCanceledException(cancellationToken.IsCancellationRequested ? cancellationToken : new CancellationToken(true));
                            default: // error
                                throw Win32Marshal.GetExceptionForWin32Error((int)readAwaitable._errorCode);
                        }

                        // Successful operation.  If we got zero bytes, we're done: exit the read/write loop.
                        int numBytesRead = (int)readAwaitable._numBytes;
                        if (numBytesRead == 0)
                        {
                            break;
                        }

                        // Otherwise, update the read position for next time accordingly.
                        if (canSeek)
                        {
                            readAwaitable._position += numBytesRead;
                        }

                        // (and keep track of the maximum number of bytes in the buffer we used, to avoid excessive and unnecessary
                        // clearing of the buffer before we return it to the pool)
                        if (numBytesRead > bufferSize)
                        {
                            bufferSize = numBytesRead;
                        }
                    }
                    finally
                    {
                        // Free the resources for this read operation
                        unsafe
                        {
                            NativeOverlapped* overlapped;
                            lock (readAwaitable.CancellationLock) // just an Exchange, but we need this to be synchronized with cancellation, so using the same lock
                            {
                                overlapped = readAwaitable._nativeOverlapped;
                                readAwaitable._nativeOverlapped = null;
                            }
                            if (overlapped != null)
                            {
                                _fileHandle.ThreadPoolBinding.FreeNativeOverlapped(overlapped);
                            }
                        }
                    }

                    // Write out the read data.
                    await destination.WriteAsync(copyBuffer, 0, (int)readAwaitable._numBytes, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                // Cleanup from the whole copy operation
                cancellationReg.Dispose();
                awaitableOverlapped.Dispose();

                Array.Clear(copyBuffer, 0, bufferSize);
                ArrayPool<byte>.Shared.Return(copyBuffer, clearArray: false);

                // Make sure the stream's current position reflects where we ended up
                if (!_fileHandle.IsClosed && CanSeek)
                {
                    SeekCore(0, SeekOrigin.End);
                }
            }
        }

        /// <summary>Used by CopyToAsync to enable awaiting the result of an overlapped I/O operation with minimal overhead.</summary>
        private sealed unsafe class AsyncCopyToAwaitable : ICriticalNotifyCompletion
        {
            /// <summary>Sentinel object used to indicate that the I/O operation has completed before being awaited.</summary>
            private readonly static Action s_sentinel = () => { };
            /// <summary>Cached delegate to IOCallback.</summary>
            internal static readonly IOCompletionCallback s_callback = IOCallback;

            /// <summary>The FileStream that owns this instance.</summary>
            internal readonly FileStream _fileStream;

            /// <summary>Tracked position representing the next location from which to read.</summary>
            internal long _position;
            /// <summary>The current native overlapped pointer.  This changes for each operation.</summary>
            internal NativeOverlapped* _nativeOverlapped;
            /// <summary>
            /// null if the operation is still in progress,
            /// s_sentinel if the I/O operation completed before the await,
            /// s_callback if it completed after the await yielded.
            /// </summary>
            internal Action _continuation;
            /// <summary>Last error code from completed operation.</summary>
            internal uint _errorCode;
            /// <summary>Last number of read bytes from completed operation.</summary>
            internal uint _numBytes;

            /// <summary>Lock object used to protect cancellation-related access to _nativeOverlapped.</summary>
            internal object CancellationLock => this;

            /// <summary>Initialize the awaitable.</summary>
            internal unsafe AsyncCopyToAwaitable(FileStream fileStream)
            {
                _fileStream = fileStream;
            }

            /// <summary>Reset state to prepare for the next read operation.</summary>
            internal void ResetForNextOperation()
            {
                Debug.Assert(_position >= 0, $"Expected non-negative position, got {_position}");
                _continuation = null;
                _errorCode = 0;
                _numBytes = 0;
            }

            /// <summary>Overlapped callback: store the results, then invoke the continuation delegate.</summary>
            internal unsafe static void IOCallback(uint errorCode, uint numBytes, NativeOverlapped* pOVERLAP)
            {
                var awaitable = (AsyncCopyToAwaitable)ThreadPoolBoundHandle.GetNativeOverlappedState(pOVERLAP);

                Debug.Assert(awaitable._continuation != s_sentinel, "Sentinel must not have already been set as the continuation");
                awaitable._errorCode = errorCode;
                awaitable._numBytes = numBytes;

                (awaitable._continuation ?? Interlocked.CompareExchange(ref awaitable._continuation, s_sentinel, null))?.Invoke();
            }

            /// <summary>
            /// Called when it's known that the I/O callback for an operation will not be invoked but we'll
            /// still be awaiting the awaitable.
            /// </summary>
            internal void MarkCompleted()
            {
                Debug.Assert(_continuation == null, "Expected null continuation");
                _continuation = s_sentinel;
            }

            public AsyncCopyToAwaitable GetAwaiter() => this;
            public bool IsCompleted => _continuation == s_sentinel;
            public void GetResult() { }
            public void OnCompleted(Action continuation) => UnsafeOnCompleted(continuation);
            public void UnsafeOnCompleted(Action continuation)
            {
                if (_continuation == s_sentinel ||
                    Interlocked.CompareExchange(ref _continuation, continuation, null) != null)
                {
                    Debug.Assert(_continuation == s_sentinel, $"Expected continuation set to s_sentinel, got ${_continuation}");
                    Task.Run(continuation);
                }
            }
        }

        // Unlike Flush(), FlushAsync() always flushes to disk. This is intentional.
        // Legend is that we chose not to flush the OS file buffers in Flush() in fear of 
        // perf problems with frequent, long running FlushFileBuffers() calls. But we don't 
        // have that problem with FlushAsync() because we will call FlushFileBuffers() in the background.
        private Task FlushAsyncInternal(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            if (_fileHandle.IsClosed)
                throw Error.GetFileNotOpen();

            // The always synchronous data transfer between the OS and the internal buffer is intentional 
            // because this is needed to allow concurrent async IO requests. Concurrent data transfer
            // between the OS and the internal buffer will result in race conditions. Since FlushWrite and
            // FlushRead modify internal state of the stream and transfer data between the OS and the 
            // internal buffer, they cannot be truly async. We will, however, flush the OS file buffers
            // asynchronously because it doesn't modify any internal state of the stream and is potentially 
            // a long running process.
            try
            {
                FlushInternalBuffer();
            }
            catch (Exception e)
            {
                return Task.FromException(e);
            }

            if (CanWrite)
            {
                return Task.Factory.StartNew(
                    state => FlushOSBuffer(),
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
        
        private Task<int> TaskFromResultOrCache(int result)
        {
            Task<int> completedTask = _lastSynchronouslyCompletedTask;
            Debug.Assert(completedTask == null || completedTask.Status == TaskStatus.RanToCompletion, "Cached task should have completed successfully");

            if ((completedTask == null) || (completedTask.Result != result))
            {
                completedTask = Task.FromResult(result);
                _lastSynchronouslyCompletedTask = completedTask;
            }

            return completedTask;
        }

        private void LockInternal(long position, long length)
        {
            int positionLow = unchecked((int)(position));
            int positionHigh = unchecked((int)(position >> 32));
            int lengthLow = unchecked((int)(length));
            int lengthHigh = unchecked((int)(length >> 32));

            if (!Interop.mincore.LockFile(_fileHandle, positionLow, positionHigh, lengthLow, lengthHigh))
            {
                throw Win32Marshal.GetExceptionForLastWin32Error();
            }
        }

        private void UnlockInternal(long position, long length)
        {
            int positionLow = unchecked((int)(position));
            int positionHigh = unchecked((int)(position >> 32));
            int lengthLow = unchecked((int)(length));
            int lengthHigh = unchecked((int)(length >> 32));

            if (!Interop.mincore.UnlockFile(_fileHandle, positionLow, positionHigh, lengthLow, lengthHigh))
            {
                throw Win32Marshal.GetExceptionForLastWin32Error();
            }
        }
    }
}
