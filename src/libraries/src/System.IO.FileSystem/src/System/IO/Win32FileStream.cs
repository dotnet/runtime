// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

/*
 * Win32FileStream supports different modes of accessing the disk - async mode
 * and sync mode.  They are two completely different codepaths in the
 * sync & async methods (ie, Read/Write vs. BeginRead/BeginWrite).  File
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
    internal sealed partial class Win32FileStream : FileStreamBase
    {
        internal const int DefaultBufferSize = 4096;
#if USE_OVERLAPPED
        internal const bool DefaultUseAsync = true;
#else
        internal const bool DefaultUseAsync = false;
#endif
        internal const bool DefaultIsAsync = false;

        private byte[] _buffer;   // Shared read/write buffer.  Alloc on first use.
        private String _fileName; // Fully qualified file name.
#if USE_OVERLAPPED
        private bool _isAsync;    // Whether we opened the handle for overlapped IO
#endif
        private bool _canRead;
        private bool _canWrite;
        private bool _canSeek;
        private bool _exposedHandle; // Could other code be using this handle?
        private bool _isPipe;     // Whether to disable async buffering code.
        private int _readPos;     // Read pointer within shared buffer.
        private int _readLen;     // Number of bytes read in buffer from file.
        private int _writePos;    // Write pointer within shared buffer.
        private int _bufferSize;  // Length of internal buffer, if it's allocated.
        private SafeFileHandle _handle;
        private long _pos;        // Cache current location in the file.
        private long _appendStart;// When appending, prevent overwriting file.

#if USE_OVERLAPPED
        private static AsyncCallback s_endReadTask;
        private static AsyncCallback s_endWriteTask;
        private static Action<object> s_cancelReadHandler;
        private static Action<object> s_cancelWriteHandler;
#endif

        private StreamAsyncHelper _streamAsyncHelper; // We delegate to this helper for
                                                      // behavior previously implemented in Stream

        [System.Security.SecuritySafeCritical]
        public Win32FileStream(String path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, FileStream parent) : base(parent)
        {
            Interop.SECURITY_ATTRIBUTES secAttrs = GetSecAttrs(share);
            Init(path, mode, access, share, bufferSize, options, secAttrs);
        }

        [System.Security.SecuritySafeCritical]
        private void Init(String path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, Interop.SECURITY_ATTRIBUTES secAttrs)
        {
            _exposedHandle = false;

            int fAccess =
                ((access & FileAccess.Read) == FileAccess.Read ? GENERIC_READ : 0) |
                ((access & FileAccess.Write) == FileAccess.Write ? GENERIC_WRITE : 0);

            _fileName = path;

            // Our Inheritable bit was stolen from Windows, but should be set in
            // the security attributes class.  Don't leave this bit set.
            share &= ~FileShare.Inheritable;

            bool seekToEnd = (mode == FileMode.Append);
            // Must use a valid Win32 constant here...
            if (mode == FileMode.Append)
                mode = FileMode.OpenOrCreate;

#if USE_OVERLAPPED
            if ((options & FileOptions.Asynchronous) != 0)
                _isAsync = true;
#else
            // Disallow overlapped IO.
            options &= ~FileOptions.Asynchronous;
#endif

#if USE_WINRT
            // FILE_ATTRIBUTE_ENCRYPTED is not allowed from a store app per documentation.
            // On ARM we see that CreateFile2 will fail with ACCESS_DENIED, other architectures 
            // it works but we should not rely on this behavior.
            // Instead of failing we just ignore this attribute as is done on filesystems that
            // don't support encryption (FAT).
            options &= ~FileOptions.Encrypted;
#endif

            int flagsAndAttributes = (int)options;

            // For mitigating local elevation of privilege attack through named pipes
            // make sure we always call CreateFile with SECURITY_ANONYMOUS so that the
            // named pipe server can't impersonate a high privileged client security context
            flagsAndAttributes |= (Interop.SECURITY_SQOS_PRESENT | Interop.SECURITY_ANONYMOUS);

            // Don't pop up a dialog for reading from an empty floppy drive
            uint oldMode = Interop.mincore.SetErrorMode(Interop.SEM_FAILCRITICALERRORS);
            try
            {
                _handle = Interop.mincore.SafeCreateFile(path, fAccess, share, ref secAttrs, mode, flagsAndAttributes, IntPtr.Zero);
#if USE_OVERLAPPED
                _handle.IsAsync = _isAsync;
#endif

                if (_handle.IsInvalid)
                {
                    // Return a meaningful exception with the full path.

                    // NT5 oddity - when trying to open "C:\" as a Win32FileStream,
                    // we usually get ERROR_PATH_NOT_FOUND from the OS.  We should
                    // probably be consistent w/ every other directory.
                    int errorCode = Marshal.GetLastWin32Error();

                    if (errorCode == Interop.ERROR_PATH_NOT_FOUND && path.Equals(Directory.InternalGetDirectoryRoot(path)))
                        errorCode = Interop.ERROR_ACCESS_DENIED;

                    throw Win32Marshal.GetExceptionForWin32Error(errorCode, _fileName);
                }
            }
            finally
            {
                Interop.mincore.SetErrorMode(oldMode);
            }

            // Disallow access to all non-file devices from the Win32FileStream
            // constructors that take a String.  Everyone else can call 
            // CreateFile themselves then use the constructor that takes an 
            // IntPtr.  Disallows "con:", "com1:", "lpt1:", etc.
            int fileType = Interop.mincore.GetFileType(_handle);
            if (fileType != Interop.FILE_TYPE_DISK)
            {
                _handle.Dispose();
                throw new NotSupportedException(SR.NotSupported_FileStreamOnNonFiles);
            }

#if USE_OVERLAPPED
            // This is necessary for async IO using IO Completion ports via our 
            // managed Threadpool API's.  This (theoretically) calls the OS's 
            // BindIoCompletionCallback method, and passes in a stub for the 
            // LPOVERLAPPED_COMPLETION_ROUTINE.  This stub looks at the Overlapped
            // struct for this request and gets a delegate to a managed callback 
            // from there, which it then calls on a threadpool thread.  (We allocate
            // our native OVERLAPPED structs 2 pointers too large and store EE state
            // & GC handles there, one to an IAsyncResult, the other to a delegate.)
            if (_isAsync)
            {
                bool b = false;
                try
                {
                    b = ThreadPool.BindHandle(_handle);
                }
                finally
                {
                    if (!b)
                    {
                        // We should close the handle so that the handle is not open until SafeFileHandle GC
                        Debug.Assert(!_exposedHandle, "Are we closing handle that we exposed/not own, how?");
                        _handle.Dispose();
                    }
                }
                if (!b)
                    throw new IOException(SR.IO_BindHandleFailed);
            }
#endif

            _canRead = (access & FileAccess.Read) != 0;
            _canWrite = (access & FileAccess.Write) != 0;
            _canSeek = true;
            _isPipe = false;
            _pos = 0;
            _bufferSize = bufferSize;
            _readPos = 0;
            _readLen = 0;
            _writePos = 0;

            // We need to pass _parent here to ensure the async implementation falls
            // back to a derived sync implementation (if it exists).
            _streamAsyncHelper = new StreamAsyncHelper(_parent);

            // For Append mode...
            if (seekToEnd)
            {
                _appendStart = SeekCore(0, SeekOrigin.End);
            }
            else
            {
                _appendStart = -1;
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public Win32FileStream(SafeFileHandle handle, FileAccess access, FileStream parent)
            : this(handle, access, DefaultBufferSize, GetDefaultIsAsync(handle), GetSuppressBindHandle(handle), parent)
        {
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public Win32FileStream(SafeFileHandle handle, FileAccess access, int bufferSize, FileStream parent)
            : this(handle, access, bufferSize, GetDefaultIsAsync(handle), GetSuppressBindHandle(handle), parent)
        {
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public Win32FileStream(SafeFileHandle handle, FileAccess access, int bufferSize, bool isAsync, FileStream parent)
            : this(handle, access, bufferSize, isAsync, GetSuppressBindHandle(handle), parent)
        {
        }

        private Win32FileStream(SafeFileHandle handle, FileAccess access, int bufferSize, bool isAsync, bool suppressBindHandle, FileStream parent)
            : base(parent) 
        {
            // To ensure we don't leak a handle, put it in a SafeFileHandle first
            if (handle.IsInvalid)
                throw new ArgumentException(SR.Arg_InvalidHandle, "handle");
            Contract.EndContractBlock();

            _handle = handle;
            _exposedHandle = true;

            // Now validate arguments.
            if (access < FileAccess.Read || access > FileAccess.ReadWrite)
                throw new ArgumentOutOfRangeException("access", SR.ArgumentOutOfRange_Enum);
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException("bufferSize", SR.ArgumentOutOfRange_NeedPosNum);

            int handleType = Interop.mincore.GetFileType(_handle);
            Debug.Assert(handleType == Interop.FILE_TYPE_DISK || handleType == Interop.FILE_TYPE_PIPE || handleType == Interop.FILE_TYPE_CHAR, "FileStream was passed an unknown file type!");

#if USE_OVERLAPPED
            _isAsync = isAsync;
#endif
            _canRead = 0 != (access & FileAccess.Read);
            _canWrite = 0 != (access & FileAccess.Write);
            _canSeek = handleType == Interop.FILE_TYPE_DISK;
            _bufferSize = bufferSize;
            _readPos = 0;
            _readLen = 0;
            _writePos = 0;
            _fileName = null;
            _isPipe = handleType == Interop.FILE_TYPE_PIPE;

#if USE_OVERLAPPED
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
            if (_isAsync && !suppressBindHandle)
            {
                bool b = false;
                try
                {
                    b = ThreadPool.BindHandle(_handle);
                }
                catch (Exception ex)
                {
                    // If you passed in a synchronous handle and told us to use
                    // it asynchronously, throw here.
                    throw new ArgumentException(SR.Arg_HandleNotAsync, ex);
                }
                if (!b)
                {
                    throw new IOException(SR.IO_BindHandleFailed);
                }
            }
            else if (!_isAsync)
#endif
            {
                if (handleType != Interop.FILE_TYPE_PIPE)
                    VerifyHandleIsSync();
            }

#if !USE_OVERLAPPED
            // When USE_OVERLAPPED is not supported we ignore the isAsync property and always call
            // VerifyHandleIsSync above.  We'll then throw here if we were told it was Async.

            if (isAsync)
            {
                // Passed in a synchronous handle and told us isAsync
                throw new ArgumentException(SR.Arg_HandleNotAsync);
            }
#endif

            if (_canSeek)
                SeekCore(0, SeekOrigin.Current);
            else
                _pos = 0;
        }

        private static bool GetDefaultIsAsync(SafeFileHandle handle)
        {
            return handle.IsAsync.HasValue ? handle.IsAsync.Value : DefaultIsAsync;
        }

        private static bool GetSuppressBindHandle(SafeFileHandle handle)
        {
            return handle.IsAsync.HasValue ? handle.IsAsync.Value : false;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        private static Interop.SECURITY_ATTRIBUTES GetSecAttrs(FileShare share)
        {
            Interop.SECURITY_ATTRIBUTES secAttrs = default(Interop.SECURITY_ATTRIBUTES);
            if ((share & FileShare.Inheritable) != 0)
            {
                secAttrs = new Interop.SECURITY_ATTRIBUTES();
                secAttrs.nLength = (uint)Marshal.SizeOf(secAttrs);

                secAttrs.bInheritHandle = true;
            }
            return secAttrs;
        }

        // Verifies that this handle supports synchronous IO operations (unless you
        // didn't open it for either reading or writing).
        [System.Security.SecuritySafeCritical]  // auto-generated
        private unsafe void VerifyHandleIsSync()
        {
            // Do NOT use this method on pipes.  Reading or writing to a pipe may
            // cause an app to block incorrectly, introducing a deadlock (depending
            // on whether a write will wake up an already-blocked thread or this
            // Win32FileStream's thread).

            // Do NOT change this to use a byte[] of length 0, or test test won't
            // work.  Our ReadFile & WriteFile methods are special cased to return
            // for arrays of length 0, since we'd get an IndexOutOfRangeException 
            // while using C#'s fixed syntax.
            byte[] bytes = new byte[1];
            int errorCode = 0;
            int r = 0;

            // If the handle is a pipe, ReadFile will block until there
            // has been a write on the other end.  We'll just have to deal with it,
            // For the read end of a pipe, you can mess up and 
            // accidentally read synchronously from an async pipe.
            if (_canRead)
            {
#if USE_OVERLAPPED
                r = ReadFileNative(_handle, bytes, 0, 0, null, out errorCode);
#else
                r = ReadFileNative(_handle, bytes, 0, 0, out errorCode);
#endif
            }
            else if (_canWrite)
            {
#if USE_OVERLAPPED
                r = WriteFileNative(_handle, bytes, 0, 0, null, out errorCode);
#else
                r = WriteFileNative(_handle, bytes, 0, 0, out errorCode);
#endif
            }

            if (errorCode == ERROR_INVALID_PARAMETER)
                throw new ArgumentException(SR.Arg_HandleNotSync);
            if (errorCode == Interop.ERROR_INVALID_HANDLE)
                throw Win32Marshal.GetExceptionForWin32Error(errorCode, "<OS handle>");
        }


        public override bool CanRead
        {
            [Pure]
            get
            { return _canRead; }
        }

        public override bool CanWrite
        {
            [Pure]
            get
            { return _canWrite; }
        }

        public override bool CanSeek
        {
            [Pure]
            get
            { return _canSeek; }
        }

        public override bool IsAsync
        {
#if USE_OVERLAPPED
            get { return _isAsync; }
#else
            get { return false; }
#endif
        }

        public override long Length
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                if (_handle.IsClosed) throw __Error.GetFileNotOpen();
                if (!_parent.CanSeek) throw __Error.GetSeekNotSupported();
                Interop.FILE_STANDARD_INFO info = new Interop.FILE_STANDARD_INFO();

                if (!Interop.mincore.GetFileInformationByHandleEx(_handle, Interop.FILE_INFO_BY_HANDLE_CLASS.FileStandardInfo, out info, (uint)Marshal.SizeOf<Interop.FILE_STANDARD_INFO>()))
                    throw Win32Marshal.GetExceptionForLastWin32Error();
                long len = info.EndOfFile;
                // If we're writing near the end of the file, we must include our
                // internal buffer in our Length calculation.  Don't flush because
                // we use the length of the file in our async write method.
                if (_writePos > 0 && _pos + _writePos > len)
                    len = _writePos + _pos;
                return len;
            }
        }

        public override String Name
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (_fileName == null)
                    return SR.IO_UnknownFileName;
                return _fileName;
            }
        }

        public override long Position
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                if (_handle.IsClosed) throw __Error.GetFileNotOpen();
                if (!_parent.CanSeek) throw __Error.GetSeekNotSupported();

                Debug.Assert((_readPos == 0 && _readLen == 0 && _writePos >= 0) || (_writePos == 0 && _readPos <= _readLen), "We're either reading or writing, but not both.");

                // Verify that internal position is in sync with the handle
                if (_exposedHandle)
                    VerifyOSHandlePosition();

                // Compensate for buffer that we read from the handle (_readLen) Vs what the user
                // read so far from the internal buffer (_readPos). Of course add any unwritten  
                // buffered data
                return _pos + (_readPos - _readLen + _writePos);
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_NeedNonNegNum);
                Contract.EndContractBlock();
                if (_writePos > 0) FlushWrite(false);
                _readPos = 0;
                _readLen = 0;
                _parent.Seek(value, SeekOrigin.Begin);
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
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
                if (_handle != null && !_handle.IsClosed)
                {
                    // Flush data to disk iff we were writing.  After 
                    // thinking about this, we also don't need to flush
                    // our read position, regardless of whether the handle
                    // was exposed to the user.  They probably would NOT 
                    // want us to do this.
                    if (_writePos > 0)
                    {
                        FlushWrite(!disposing);
                    }
                }
            }
            finally
            {
                if (_handle != null && !_handle.IsClosed)
                    _handle.Dispose();

                _canRead = false;
                _canWrite = false;
                _canSeek = false;
                // Don't set the buffer to null, to avoid a NullReferenceException
                // when users have a race condition in their code (ie, they call
                // Close when calling another method on Stream like Read).
                //_buffer = null;
                base.Dispose(disposing);
            }
        }

        public override void Flush()
        {
            // Make sure that we call through the public virtual API
            _parent.Flush(false);
        }

        [System.Security.SecuritySafeCritical]
        public override void Flush(Boolean flushToDisk)
        {
            // This code is duplicated in _parent.Dispose
            if (_handle.IsClosed) throw __Error.GetFileNotOpen();

            FlushInternalBuffer();

            if (flushToDisk && _parent.CanWrite)
            {
                FlushOSBuffer();
            }
        }

        private void FlushInternalBuffer()
        {
            if (_writePos > 0)
            {
                FlushWrite(false);
            }
            else if (_readPos < _readLen && _parent.CanSeek)
            {
                FlushRead();
            }
        }

        [System.Security.SecuritySafeCritical]
        private void FlushOSBuffer()
        {
            if (!Interop.mincore.FlushFileBuffers(_handle))
            {
                throw Win32Marshal.GetExceptionForLastWin32Error();
            }
        }

        // Reading is done by blocks from the file, but someone could read
        // 1 byte from the buffer then write.  At that point, the OS's file
        // pointer is out of sync with the stream's position.  All write 
        // functions should call this function to preserve the position in the file.
        private void FlushRead()
        {
            Debug.Assert(_writePos == 0, "FileStream: Write buffer must be empty in FlushRead!");
            if (_readPos - _readLen != 0)
            {
                Debug.Assert(_parent.CanSeek, "FileStream will lose buffered read data now.");
                SeekCore(_readPos - _readLen, SeekOrigin.Current);
            }
            _readPos = 0;
            _readLen = 0;
        }

        // Writes are buffered.  Anytime the buffer fills up 
        // (_writePos + delta > _bufferSize) or the buffer switches to reading
        // and there is left over data (_writePos > 0), this function must be called.
        private void FlushWrite(bool calledFromFinalizer)
        {
            Debug.Assert(_readPos == 0 && _readLen == 0, "FileStream: Read buffer must be empty in FlushWrite!");

#if USE_OVERLAPPED
            if (_isAsync)
            {
                IAsyncResult asyncResult = BeginWriteCore(_buffer, 0, _writePos, null, null);
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
                    EndWrite(asyncResult);
            }
            else
#endif
                WriteCore(_buffer, 0, _writePos);

            _writePos = 0;
        }

        public override SafeFileHandle SafeFileHandle
        {
            [System.Security.SecurityCritical]  // auto-generated_required
            get
            {
                _parent.Flush();
                // Explicitly dump any buffered data, since the user could move our
                // position or write to the file.
                _readPos = 0;
                _readLen = 0;
                _writePos = 0;
                _exposedHandle = true;

                return _handle;
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override void SetLength(long value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_NeedNonNegNum);
            Contract.EndContractBlock();

            if (_handle.IsClosed) throw __Error.GetFileNotOpen();
            if (!_parent.CanSeek) throw __Error.GetSeekNotSupported();
            if (!_parent.CanWrite) throw __Error.GetWriteNotSupported();

            // Handle buffering updates.
            if (_writePos > 0)
            {
                FlushWrite(false);
            }
            else if (_readPos < _readLen)
            {
                FlushRead();
            }
            _readPos = 0;
            _readLen = 0;

            if (_appendStart != -1 && value < _appendStart)
                throw new IOException(SR.IO_SetLengthAppendTruncate);
            SetLengthCore(value);
        }

        // We absolutely need this method broken out so that BeginWriteCore can call
        // a method without having to go through buffering code that might call
        // FlushWrite.
        [System.Security.SecuritySafeCritical]  // auto-generated
        private void SetLengthCore(long value)
        {
            Debug.Assert(value >= 0, "value >= 0");
            long origPos = _pos;

            if (_exposedHandle)
                VerifyOSHandlePosition();
            if (_pos != value)
                SeekCore(value, SeekOrigin.Begin);
            if (!Interop.mincore.SetEndOfFile(_handle))
            {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode == Interop.ERROR_INVALID_PARAMETER)
                    throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_FileLengthTooBig);
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

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override int Read([In, Out] byte[] array, int offset, int count)
        {
            if (array == null)
                throw new ArgumentNullException("array", SR.ArgumentNull_Buffer);
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
            if (array.Length - offset < count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            Contract.EndContractBlock();

            if (_handle.IsClosed) throw __Error.GetFileNotOpen();

            Debug.Assert((_readPos == 0 && _readLen == 0 && _writePos >= 0) || (_writePos == 0 && _readPos <= _readLen), "We're either reading or writing, but not both.");

            bool isBlocked = false;
            int n = _readLen - _readPos;
            // if the read buffer is empty, read into either user's array or our
            // buffer, depending on number of bytes user asked for and buffer size.
            if (n == 0)
            {
                if (!_parent.CanRead) throw __Error.GetReadNotSupported();
                if (_writePos > 0) FlushWrite(false);
                if (!_parent.CanSeek || (count >= _bufferSize))
                {
                    n = ReadCore(array, offset, count);
                    // Throw away read buffer.
                    _readPos = 0;
                    _readLen = 0;
                    return n;
                }
                if (_buffer == null) _buffer = new byte[_bufferSize];
                n = ReadCore(_buffer, 0, _bufferSize);
                if (n == 0) return 0;
                isBlocked = n < _bufferSize;
                _readPos = 0;
                _readLen = n;
            }
            // Now copy min of count or numBytesAvailable (ie, near EOF) to array.
            if (n > count) n = count;
            Buffer.BlockCopy(_buffer, _readPos, array, offset, n);
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
                // fewer bytes from the underlying stream than we asked for (ie, we're 
                // probably blocked), don't ask for more bytes.
                if (n < count && !isBlocked)
                {
                    Debug.Assert(_readPos == _readLen, "Read buffer should be empty!");
                    int moreBytesRead = ReadCore(array, offset + n, count - n);
                    n += moreBytesRead;
                    // We've just made our buffer inconsistent with our position 
                    // pointer.  We must throw away the read buffer.
                    _readPos = 0;
                    _readLen = 0;
                }
            }

            return n;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        private unsafe int ReadCore(byte[] buffer, int offset, int count)
        {
            Debug.Assert(!_handle.IsClosed, "!_handle.IsClosed");
            Debug.Assert(_parent.CanRead, "_parent.CanRead");

            Debug.Assert(buffer != null, "buffer != null");
            Debug.Assert(_writePos == 0, "_writePos == 0");
            Debug.Assert(offset >= 0, "offset is negative");
            Debug.Assert(count >= 0, "count is negative");
#if USE_OVERLAPPED
            if (_isAsync)
            {
                IAsyncResult result = BeginReadCore(buffer, offset, count, null, null, 0);
                return EndRead(result);
            }
#endif

            // Make sure we are reading from the right spot
            if (_exposedHandle)
                VerifyOSHandlePosition();

            int errorCode = 0;
#if USE_OVERLAPPED
            int r = ReadFileNative(_handle, buffer, offset, count, null, out errorCode);
#else       
            int r = ReadFileNative(_handle, buffer, offset, count, out errorCode);
#endif
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
                        throw new ArgumentException(SR.Arg_HandleNotSync);

                    throw Win32Marshal.GetExceptionForWin32Error(errorCode);
                }
            }
            Debug.Assert(r >= 0, "FileStream's ReadCore is likely broken.");
            _pos += r;

            return r;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin < SeekOrigin.Begin || origin > SeekOrigin.End)
                throw new ArgumentException(SR.Argument_InvalidSeekOrigin, "origin");
            Contract.EndContractBlock();
            if (_handle.IsClosed) throw __Error.GetFileNotOpen();
            if (!_parent.CanSeek) throw __Error.GetSeekNotSupported();

            Debug.Assert((_readPos == 0 && _readLen == 0 && _writePos >= 0) || (_writePos == 0 && _readPos <= _readLen), "We're either reading or writing, but not both.");

            // If we've got bytes in our buffer to write, write them out.
            // If we've read in and consumed some bytes, we'll have to adjust
            // our seek positions ONLY IF we're seeking relative to the current
            // position in the stream.  This simulates doing a seek to the new
            // position, then a read for the number of bytes we have in our buffer.
            if (_writePos > 0)
            {
                FlushWrite(false);
            }
            else if (origin == SeekOrigin.Current)
            {
                // Don't call FlushRead here, which would have caused an infinite
                // loop.  Simply adjust the seek origin.  This isn't necessary
                // if we're seeking relative to the beginning or end of the stream.
                offset -= (_readLen - _readPos);
            }

            // Verify that internal position is in sync with the handle
            if (_exposedHandle)
                VerifyOSHandlePosition();

            long oldPos = _pos + (_readPos - _readLen);
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
            if (_readLen > 0)
            {
                // We can optimize the following condition:
                // oldPos - _readPos <= pos < oldPos + _readLen - _readPos
                if (oldPos == pos)
                {
                    if (_readPos > 0)
                    {
                        //Console.WriteLine("Seek: seeked for 0, adjusting buffer back by: "+_readPos+"  _readLen: "+_readLen);
                        Buffer.BlockCopy(_buffer, _readPos, _buffer, 0, _readLen - _readPos);
                        _readLen -= _readPos;
                        _readPos = 0;
                    }
                    // If we still have buffered data, we must update the stream's 
                    // position so our Position property is correct.
                    if (_readLen > 0)
                        SeekCore(_readLen, SeekOrigin.Current);
                }
                else if (oldPos - _readPos < pos && pos < oldPos + _readLen - _readPos)
                {
                    int diff = (int)(pos - oldPos);
                    //Console.WriteLine("Seek: diff was "+diff+", readpos was "+_readPos+"  adjusting buffer - shrinking by "+ (_readPos + diff));
                    Buffer.BlockCopy(_buffer, _readPos + diff, _buffer, 0, _readLen - (_readPos + diff));
                    _readLen -= (_readPos + diff);
                    _readPos = 0;
                    if (_readLen > 0)
                        SeekCore(_readLen, SeekOrigin.Current);
                }
                else
                {
                    // Lose the read buffer.
                    _readPos = 0;
                    _readLen = 0;
                }
                Debug.Assert(_readLen >= 0 && _readPos <= _readLen, "_readLen should be nonnegative, and _readPos should be less than or equal _readLen");
                Debug.Assert(pos == _parent.Position, "Seek optimization: pos != Position!  Buffer math was mangled.");
            }
            return pos;
        }

        // This doesn't do argument checking.  Necessary for SetLength, which must
        // set the file pointer beyond the end of the file. This will update the 
        // internal position
        // This is called during construction so it should avoid any virtual
        // calls
        [System.Security.SecuritySafeCritical]  // auto-generated
        private long SeekCore(long offset, SeekOrigin origin)
        {
            Debug.Assert(!_handle.IsClosed && _canSeek, "!_handle.IsClosed && _parent.CanSeek");
            Debug.Assert(origin >= SeekOrigin.Begin && origin <= SeekOrigin.End, "origin>=SeekOrigin.Begin && origin<=SeekOrigin.End");
            int errorCode = 0;
            long ret = 0;

            if (!Interop.mincore.SetFilePointerEx(_handle, offset, out ret, (uint)origin))
            {
                errorCode = Marshal.GetLastWin32Error();
                // #errorInvalidHandle
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
                if (errorCode == Interop.ERROR_INVALID_HANDLE)
                    _handle.Dispose();
                throw Win32Marshal.GetExceptionForWin32Error(errorCode);
            }

            _pos = ret;
            return ret;
        }

        // Checks the position of the OS's handle equals what we expect it to.
        // This will fail if someone else moved the Win32FileStream's handle or if
        // our position updating code is incorrect.
        private void VerifyOSHandlePosition()
        {
            if (!_parent.CanSeek)
                return;

            // SeekCore will override the current _pos, so save it now
            long oldPos = _pos;
            long curPos = SeekCore(0, SeekOrigin.Current);

            if (curPos != oldPos)
            {
                // For reads, this is non-fatal but we still could have returned corrupted 
                // data in some cases. So discard the internal buffer. Potential MDA 
                _readPos = 0;
                _readLen = 0;
                if (_writePos > 0)
                {
                    // Discard the buffer and let the user know!
                    _writePos = 0;
                    throw new IOException(SR.IO_FileStreamHandlePosition);
                }
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override void Write(byte[] array, int offset, int count)
        {
            if (array == null)
                throw new ArgumentNullException("array", SR.ArgumentNull_Buffer);
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
            if (array.Length - offset < count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            Contract.EndContractBlock();

            if (_handle.IsClosed) throw __Error.GetFileNotOpen();

            if (_writePos == 0)
            {
                // Ensure we can write to the stream, and ready buffer for writing.
                if (!_parent.CanWrite) throw __Error.GetWriteNotSupported();
                if (_readPos < _readLen) FlushRead();
                _readPos = 0;
                _readLen = 0;
            }

            // If our buffer has data in it, copy data from the user's array into
            // the buffer, and if we can fit it all there, return.  Otherwise, write
            // the buffer to disk and copy any remaining data into our buffer.
            // The assumption here is memcpy is cheaper than disk (or net) IO.
            // (10 milliseconds to disk vs. ~20-30 microseconds for a 4K memcpy)
            // So the extra copying will reduce the total number of writes, in 
            // non-pathological cases (ie, write 1 byte, then write for the buffer 
            // size repeatedly)
            if (_writePos > 0)
            {
                int numBytes = _bufferSize - _writePos;   // space left in buffer
                if (numBytes > 0)
                {
                    if (numBytes > count)
                        numBytes = count;
                    Buffer.BlockCopy(array, offset, _buffer, _writePos, numBytes);
                    _writePos += numBytes;
                    if (count == numBytes) return;
                    offset += numBytes;
                    count -= numBytes;
                }
                // Reset our buffer.  We essentially want to call FlushWrite
                // without calling Flush on the underlying Stream.

#if USE_OVERLAPPED
                if (_isAsync)
                {
                    IAsyncResult result = BeginWriteCore(_buffer, 0, _writePos, null, null);
                    EndWrite(result);
                }
                else
#endif
                {
                    WriteCore(_buffer, 0, _writePos);
                }
                _writePos = 0;
            }
            // If the buffer would slow writes down, avoid buffer completely.
            if (count >= _bufferSize)
            {
                Debug.Assert(_writePos == 0, "FileStream cannot have buffered data to write here!  Your stream will be corrupted.");
                WriteCore(array, offset, count);
                return;
            }
            else if (count == 0)
                return;  // Don't allocate a buffer then call memcpy for 0 bytes.
            if (_buffer == null) _buffer = new byte[_bufferSize];
            // Copy remaining bytes into buffer, to write at a later date.
            Buffer.BlockCopy(array, offset, _buffer, _writePos, count);
            _writePos = count;
            return;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        private unsafe void WriteCore(byte[] buffer, int offset, int count)
        {
            Debug.Assert(!_handle.IsClosed, "!_handle.IsClosed");
            Debug.Assert(_parent.CanWrite, "_parent.CanWrite");

            Debug.Assert(buffer != null, "buffer != null");
            Debug.Assert(_readPos == _readLen, "_readPos == _readLen");
            Debug.Assert(offset >= 0, "offset is negative");
            Debug.Assert(count >= 0, "count is negative");
#if USE_OVERLAPPED
            if (_isAsync)
            {
                IAsyncResult result = BeginWriteCore(buffer, offset, count, null, null);
                EndWrite(result);
                return;
            }
#endif

            // Make sure we are writing to the position that we think we are
            if (_exposedHandle)
                VerifyOSHandlePosition();

            int errorCode = 0;
#if USE_OVERLAPPED
            int r = WriteFileNative(_handle, buffer, offset, count, null, out errorCode);
#else
            int r = WriteFileNative(_handle, buffer, offset, count, out errorCode);
#endif

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
                    // where the position is too large (ie, writing at Int64.MaxValue 
                    // on Win9x) OR for synchronous writes to a handle opened 
                    // asynchronously.
                    if (errorCode == ERROR_INVALID_PARAMETER)
                        throw new IOException(SR.IO_FileTooLongOrHandleNotSync);
                    throw Win32Marshal.GetExceptionForWin32Error(errorCode);
                }
            }
            Debug.Assert(r >= 0, "FileStream's WriteCore is likely broken.");
            _pos += r;
            return;
        }

#if USE_OVERLAPPED
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal IAsyncResult BeginRead(byte[] array, int offset, int numBytes, AsyncCallback userCallback, Object stateObject)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", SR.ArgumentOutOfRange_NeedNonNegNum);
            if (numBytes < 0)
                throw new ArgumentOutOfRangeException("numBytes", SR.ArgumentOutOfRange_NeedNonNegNum);
            if (array.Length - offset < numBytes)
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            Contract.EndContractBlock();

            if (_handle.IsClosed) throw __Error.GetFileNotOpen();

            if (!_isAsync)
                return _streamAsyncHelper.BeginRead(array, offset, numBytes, userCallback, stateObject);
            else
                return BeginReadAsync(array, offset, numBytes, userCallback, stateObject);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        private FileStreamAsyncResult BeginReadAsync(byte[] array, int offset, int numBytes, AsyncCallback userCallback, Object stateObject)
        {
            Debug.Assert(_isAsync);

            if (!_parent.CanRead) throw __Error.GetReadNotSupported();

            Debug.Assert((_readPos == 0 && _readLen == 0 && _writePos >= 0) || (_writePos == 0 && _readPos <= _readLen), "We're either reading or writing, but not both.");

            if (_isPipe)
            {
                // Pipes are tricky, at least when you have 2 different pipes
                // that you want to use simultaneously.  When redirecting stdout
                // & stderr with the Process class, it's easy to deadlock your
                // parent & child processes when doing writes 4K at a time.  The
                // OS appears to use a 4K buffer internally.  If you write to a
                // pipe that is full, you will block until someone read from 
                // that pipe.  If you try reading from an empty pipe and 
                // Win32FileStream's BeginRead blocks waiting for data to fill it's 
                // internal buffer, you will be blocked.  In a case where a child
                // process writes to stdout & stderr while a parent process tries
                // reading from both, you can easily get into a deadlock here.
                // To avoid this deadlock, don't buffer when doing async IO on
                // pipes.  But don't completely ignore buffered data either.  
                if (_readPos < _readLen)
                {
                    int n = _readLen - _readPos;
                    if (n > numBytes) n = numBytes;
                    Buffer.BlockCopy(_buffer, _readPos, array, offset, n);
                    _readPos += n;

                    // Return a synchronous FileStreamAsyncResult
                    return FileStreamAsyncResult.CreateBufferedReadResult(n, userCallback, stateObject, false);
                }
                else
                {
                    Debug.Assert(_writePos == 0, "Win32FileStream must not have buffered write data here!  Pipes should be unidirectional.");
                    return BeginReadCore(array, offset, numBytes, userCallback, stateObject, 0);
                }
            }

            Debug.Assert(!_isPipe, "Should not be a pipe.");

            // Handle buffering.
            if (_writePos > 0) FlushWrite(false);
            if (_readPos == _readLen)
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

                if (numBytes < _bufferSize)
                {
                    if (_buffer == null) _buffer = new byte[_bufferSize];
                    IAsyncResult bufferRead = BeginReadCore(_buffer, 0, _bufferSize, null, null, 0);
                    _readLen = EndRead(bufferRead);
                    int n = _readLen;
                    if (n > numBytes) n = numBytes;
                    Buffer.BlockCopy(_buffer, 0, array, offset, n);
                    _readPos = n;

                    // Return a synchronous FileStreamAsyncResult
                    return FileStreamAsyncResult.CreateBufferedReadResult(n, userCallback, stateObject, false);
                }
                else
                {
                    // Here we're making our position pointer inconsistent
                    // with our read buffer.  Throw away the read buffer's contents.
                    _readPos = 0;
                    _readLen = 0;
                    return BeginReadCore(array, offset, numBytes, userCallback, stateObject, 0);
                }
            }
            else
            {
                int n = _readLen - _readPos;
                if (n > numBytes) n = numBytes;
                Buffer.BlockCopy(_buffer, _readPos, array, offset, n);
                _readPos += n;

                if (n >= numBytes)
                {
                    // Return a synchronous FileStreamAsyncResult
                    return FileStreamAsyncResult.CreateBufferedReadResult(n, userCallback, stateObject, false);
                }
                else
                {
                    // For streams with no clear EOF like serial ports or pipes
                    // we cannot read more data without causing an app to block
                    // incorrectly.  Pipes don't go down this path 
                    // though.  This code needs to be fixed.
                    // Throw away read buffer.
                    _readPos = 0;
                    _readLen = 0;
                    return BeginReadCore(array, offset + n, numBytes - n, userCallback, stateObject, n);
                }
                // WARNING: all state on asyncResult objects must be set before
                // we call ReadFile in BeginReadCore, since the OS can run our
                // callback & the user's callback before ReadFile returns.
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        unsafe private FileStreamAsyncResult BeginReadCore(byte[] bytes, int offset, int numBytes, AsyncCallback userCallback, Object stateObject, int numBufferedBytesRead)
        {
            Debug.Assert(!_handle.IsClosed, "!_handle.IsClosed");
            Debug.Assert(_parent.CanRead, "_parent.CanRead");
            Debug.Assert(bytes != null, "bytes != null");
            Debug.Assert(_writePos == 0, "_writePos == 0");
            Debug.Assert(_isAsync, "BeginReadCore doesn't work on synchronous file streams!");
            Debug.Assert(offset >= 0, "offset is negative");
            Debug.Assert(numBytes >= 0, "numBytes is negative");

            // Create and store async stream class library specific data in the async result

            // Must pass in _numBufferedBytes here to ensure all the state on the IAsyncResult 
            // object is set before we call ReadFile, which gives the OS an
            // opportunity to run our callback (including the user callback &
            // the call to EndRead) before ReadFile has returned.   
            FileStreamAsyncResult asyncResult = new FileStreamAsyncResult(numBufferedBytesRead, bytes, _handle, userCallback, stateObject, false);
            NativeOverlapped* intOverlapped = asyncResult.OverLapped;

            // Calculate position in the file we should be at after the read is done
            if (_parent.CanSeek)
            {
                long len = _parent.Length;

                // Make sure we are reading from the position that we think we are
                if (_exposedHandle)
                    VerifyOSHandlePosition();

                if (_pos + numBytes > len)
                {
                    if (_pos <= len)
                        numBytes = (int)(len - _pos);
                    else
                        numBytes = 0;
                }

                // Now set the position to read from in the NativeOverlapped struct
                // For pipes, we should leave the offset fields set to 0.
                intOverlapped->OffsetLow = unchecked((int)_pos);
                intOverlapped->OffsetHigh = (int)(_pos >> 32);

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
            int r = ReadFileNative(_handle, bytes, offset, numBytes, intOverlapped, out errorCode);
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
                    asyncResult.CallUserCallback();
                    // EndRead will free the Overlapped struct correctly.
                }
                else if (errorCode != ERROR_IO_PENDING)
                {
                    if (!_handle.IsClosed && _parent.CanSeek)  // Update Position - It could be anywhere.
                        SeekCore(0, SeekOrigin.Current);

                    if (errorCode == ERROR_HANDLE_EOF)
                        throw __Error.GetEndOfFile();
                    else
                        throw Win32Marshal.GetExceptionForWin32Error(errorCode);
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

            return asyncResult;
        }

        [System.Security.SecuritySafeCritical]
        internal unsafe int EndRead(IAsyncResult asyncResult)
        {
            // There are 3 significantly different IAsyncResults we'll accept
            // here.  One is from Stream::BeginRead.  The other two are variations
            // on our FileStreamAsyncResult.  One is from BeginReadCore,
            // while the other is from the BeginRead buffering wrapper.
            if (asyncResult == null)
                throw new ArgumentNullException("asyncResult");
            Contract.EndContractBlock();

            if (!_isAsync)
                return _streamAsyncHelper.EndRead(asyncResult);

            FileStreamAsyncResult afsar = asyncResult as FileStreamAsyncResult;
            if (afsar == null || afsar.IsWrite)
                throw __Error.GetWrongAsyncResult();

            // Ensure we can't get into any race conditions by doing an 
            // Interlocked.CompareExchange here.  Avoids corrupting memory via 
            // freeing the NativeOverlapped class or GCHandle twice.  -- 
            if (1 == Interlocked.CompareExchange(ref afsar._EndXxxCalled, 1, 0))
                throw __Error.GetEndReadCalledTwice();

            // Obtain the WaitHandle, but don't use public property in case we
            // delay initialize the manual reset event in the future.
            afsar.Wait();

            // Free memory & GC handles.
            afsar.ReleaseNativeResource();

            // Now check for any error during the read.
            if (afsar.ErrorCode != 0)
                throw Win32Marshal.GetExceptionForWin32Error(afsar.ErrorCode);

            return afsar.NumBytesRead;
        }
#endif

        // Reads a byte from the file stream.  Returns the byte cast to an int
        // or -1 if reading from the end of the stream.
        [System.Security.SecuritySafeCritical]  // auto-generated
        public override int ReadByte()
        {
            if (_handle.IsClosed) throw __Error.GetFileNotOpen();
            if (_readLen == 0 && !_parent.CanRead) throw __Error.GetReadNotSupported();
            Debug.Assert((_readPos == 0 && _readLen == 0 && _writePos >= 0) || (_writePos == 0 && _readPos <= _readLen), "We're either reading or writing, but not both.");
            if (_readPos == _readLen)
            {
                if (_writePos > 0) FlushWrite(false);
                Debug.Assert(_bufferSize > 0, "_bufferSize > 0");
                if (_buffer == null) _buffer = new byte[_bufferSize];
                _readLen = ReadCore(_buffer, 0, _bufferSize);
                _readPos = 0;
            }
            if (_readPos == _readLen)
                return -1;

            int result = _buffer[_readPos];
            _readPos++;
            return result;
        }

#if USE_OVERLAPPED
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal IAsyncResult BeginWrite(byte[] array, int offset, int numBytes, AsyncCallback userCallback, Object stateObject)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", SR.ArgumentOutOfRange_NeedNonNegNum);
            if (numBytes < 0)
                throw new ArgumentOutOfRangeException("numBytes", SR.ArgumentOutOfRange_NeedNonNegNum);
            if (array.Length - offset < numBytes)
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            Contract.EndContractBlock();

            if (_handle.IsClosed) throw __Error.GetFileNotOpen();

            if (!_isAsync)
                return _streamAsyncHelper.BeginWrite(array, offset, numBytes, userCallback, stateObject);
            else
                return BeginWriteAsync(array, offset, numBytes, userCallback, stateObject);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        private FileStreamAsyncResult BeginWriteAsync(byte[] array, int offset, int numBytes, AsyncCallback userCallback, Object stateObject)
        {
            Debug.Assert(_isAsync);

            if (!_parent.CanWrite) throw __Error.GetWriteNotSupported();

            Debug.Assert((_readPos == 0 && _readLen == 0 && _writePos >= 0) || (_writePos == 0 && _readPos <= _readLen), "We're either reading or writing, but not both.");

            if (_isPipe)
            {
                // Pipes are tricky, at least when you have 2 different pipes
                // that you want to use simultaneously.  When redirecting stdout
                // & stderr with the Process class, it's easy to deadlock your
                // parent & child processes when doing writes 4K at a time.  The
                // OS appears to use a 4K buffer internally.  If you write to a
                // pipe that is full, you will block until someone read from 
                // that pipe.  If you try reading from an empty pipe and 
                // Win32FileStream's BeginRead blocks waiting for data to fill it's 
                // internal buffer, you will be blocked.  In a case where a child
                // process writes to stdout & stderr while a parent process tries
                // reading from both, you can easily get into a deadlock here.
                // To avoid this deadlock, don't buffer when doing async IO on
                // pipes.   
                Debug.Assert(_readPos == 0 && _readLen == 0, "Win32FileStream must not have buffered data here!  Pipes should be unidirectional.");

                if (_writePos > 0)
                    FlushWrite(false);

                return BeginWriteCore(array, offset, numBytes, userCallback, stateObject);
            }

            // Handle buffering.
            if (_writePos == 0)
            {
                if (_readPos < _readLen) FlushRead();
                _readPos = 0;
                _readLen = 0;
            }

            int n = _bufferSize - _writePos;
            if (numBytes <= n)
            {
                if (_writePos == 0) _buffer = new byte[_bufferSize];
                Buffer.BlockCopy(array, offset, _buffer, _writePos, numBytes);
                _writePos += numBytes;

                // Return a synchronous FileStreamAsyncResult
                return FileStreamAsyncResult.CreateBufferedReadResult(numBytes, userCallback, stateObject, true);
            }

            if (_writePos > 0)
                FlushWrite(false);

            return BeginWriteCore(array, offset, numBytes, userCallback, stateObject);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        unsafe private FileStreamAsyncResult BeginWriteCore(byte[] bytes, int offset, int numBytes, AsyncCallback userCallback, Object stateObject)
        {
            Debug.Assert(!_handle.IsClosed, "!_handle.IsClosed");
            Debug.Assert(_parent.CanWrite, "_parent.CanWrite");
            Debug.Assert(bytes != null, "bytes != null");
            Debug.Assert(_readPos == _readLen, "_readPos == _readLen");
            Debug.Assert(_isAsync, "BeginWriteCore doesn't work on synchronous file streams!");
            Debug.Assert(offset >= 0, "offset is negative");
            Debug.Assert(numBytes >= 0, "numBytes is negative");

            // Create and store async stream class library specific data in the async result
            FileStreamAsyncResult asyncResult = new FileStreamAsyncResult(0, bytes, _handle, userCallback, stateObject, true);
            NativeOverlapped* intOverlapped = asyncResult.OverLapped;

            if (_parent.CanSeek)
            {
                // Make sure we set the length of the file appropriately.
                long len = _parent.Length;
                //Console.WriteLine("BeginWrite - Calculating end pos.  pos: "+pos+"  len: "+len+"  numBytes: "+numBytes);

                // Make sure we are writing to the position that we think we are
                if (_exposedHandle)
                    VerifyOSHandlePosition();

                if (_pos + numBytes > len)
                {
                    //Console.WriteLine("BeginWrite - Setting length to: "+(pos + numBytes));
                    SetLengthCore(_pos + numBytes);
                }

                // Now set the position to read from in the NativeOverlapped struct
                // For pipes, we should leave the offset fields set to 0.
                intOverlapped->OffsetLow = (int)_pos;
                intOverlapped->OffsetHigh = (int)(_pos >> 32);

                // When using overlapped IO, the OS is not supposed to 
                // touch the file pointer location at all.  We will adjust it 
                // ourselves.  This isn't threadsafe.
                SeekCore(numBytes, SeekOrigin.Current);
            }

            //Console.WriteLine("BeginWrite finishing.  pos: "+pos+"  numBytes: "+numBytes+"  _pos: "+_pos+"  Position: "+Position);

            int errorCode = 0;
            // queue an async WriteFile operation and pass in a packed overlapped
            int r = WriteFileNative(_handle, bytes, offset, numBytes, intOverlapped, out errorCode);

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
                    // Not an error, but EOF.  AsyncFSCallback will NOT be 
                    // called.  Call the user callback here.
                    asyncResult.CallUserCallback();
                    // EndWrite will free the Overlapped struct correctly.
                }
                else if (errorCode != ERROR_IO_PENDING)
                {
                    if (!_handle.IsClosed && _parent.CanSeek)  // Update Position - It could be anywhere.
                        SeekCore(0, SeekOrigin.Current);

                    if (errorCode == ERROR_HANDLE_EOF)
                        throw __Error.GetEndOfFile();
                    else
                        throw Win32Marshal.GetExceptionForWin32Error(errorCode);
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

            return asyncResult;
        }

        [System.Security.SecuritySafeCritical]
        internal unsafe void EndWrite(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException("asyncResult");
            Contract.EndContractBlock();

            if (!_isAsync)
            {
                _streamAsyncHelper.EndWrite(asyncResult);
                return;
            }

            FileStreamAsyncResult afsar = asyncResult as FileStreamAsyncResult;
            if (afsar == null || !afsar.IsWrite)
                throw __Error.GetWrongAsyncResult();

            // Ensure we can't get into any race conditions by doing an 
            // Interlocked.CompareExchange here.  Avoids corrupting memory via
            // freeing the NativeOverlapped class or GCHandle twice.  -- 
            if (1 == Interlocked.CompareExchange(ref afsar._EndXxxCalled, 1, 0))
                throw __Error.GetEndWriteCalledTwice();

            // Obtain the WaitHandle, but don't use public property in case we
            // delay initialize the manual reset event in the future.
            afsar.Wait();

            // Free memory & GC handles.
            afsar.ReleaseNativeResource();

            // Now check for any error during the write.
            if (afsar.ErrorCode != 0)
                throw Win32Marshal.GetExceptionForWin32Error(afsar.ErrorCode);

            // Number of bytes written is afsar._numBytes + afsar._numBufferedBytes.
            return;
        }
#endif

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override void WriteByte(byte value)
        {
            if (_handle.IsClosed) throw __Error.GetFileNotOpen();
            if (_writePos == 0)
            {
                if (!_parent.CanWrite) throw __Error.GetWriteNotSupported();
                if (_readPos < _readLen) FlushRead();
                _readPos = 0;
                _readLen = 0;
                Debug.Assert(_bufferSize > 0, "_bufferSize > 0");
                if (_buffer == null) _buffer = new byte[_bufferSize];
            }
            if (_writePos == _bufferSize)
                FlushWrite(false);

            _buffer[_writePos] = value;
            _writePos++;
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
        [System.Security.SecurityCritical]  // auto-generated
#if USE_OVERLAPPED
        private unsafe int ReadFileNative(SafeFileHandle handle, byte[] bytes, int offset, int count, NativeOverlapped* overlapped, out int errorCode)
#else
        private unsafe int ReadFileNative(SafeFileHandle handle, byte[] bytes, int offset, int count, out int errorCode)
#endif
        {
            Contract.Requires(handle != null, "handle != null");
            Contract.Requires(offset >= 0, "offset >= 0");
            Contract.Requires(count >= 0, "count >= 0");
            Contract.Requires(bytes != null, "bytes != null");
            // Don't corrupt memory when multiple threads are erroneously writing
            // to this stream simultaneously.
            if (bytes.Length - offset < count)
                throw new IndexOutOfRangeException(SR.IndexOutOfRange_IORaceCondition);
            Contract.EndContractBlock();

#if USE_OVERLAPPED
            Debug.Assert((_isAsync && overlapped != null) || (!_isAsync && overlapped == null), "Async IO and overlapped parameters inconsistent in call to ReadFileNative.");
#endif

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
#if USE_OVERLAPPED
                if (_isAsync)
                    r = Interop.mincore.ReadFile(handle, p + offset, count, IntPtr.Zero, overlapped);
                else
#endif
                r = Interop.mincore.ReadFile(handle, p + offset, count, out numBytesRead, IntPtr.Zero);
            }

            if (r == 0)
            {
                errorCode = Marshal.GetLastWin32Error();
                // We should never ignore an error here without some extra work.
                // We must make sure that BeginReadCore won't return an 
                // IAsyncResult that will cause EndRead to block, since the OS 
                // won't call AsyncFSCallback for us.  
                if (errorCode == ERROR_BROKEN_PIPE || errorCode == Interop.ERROR_PIPE_NOT_CONNECTED)
                {
                    // This handle was a pipe, and it's done. Not an error, but EOF.
                    // However, the OS will not call AsyncFSCallback!
                    // Let the caller handle this, since BeginReadCore & ReadCore 
                    // need to do different things.
                    return -1;
                }

                // See code:#errorInvalidHandle in "private long SeekCore(long offset, SeekOrigin origin)".
                if (errorCode == Interop.ERROR_INVALID_HANDLE)
                    _handle.Dispose();

                return -1;
            }
            else
                errorCode = 0;
            return numBytesRead;
        }

        [System.Security.SecurityCritical]  // auto-generated
#if USE_OVERLAPPED
        private unsafe int WriteFileNative(SafeFileHandle handle, byte[] bytes, int offset, int count, NativeOverlapped* overlapped, out int errorCode)
        {
#else
        private unsafe int WriteFileNative(SafeFileHandle handle, byte[] bytes, int offset, int count, out int errorCode)
        {
#endif
            Contract.Requires(handle != null, "handle != null");
            Contract.Requires(offset >= 0, "offset >= 0");
            Contract.Requires(count >= 0, "count >= 0");
            Contract.Requires(bytes != null, "bytes != null");
            // Don't corrupt memory when multiple threads are erroneously writing
            // to this stream simultaneously.  (the OS is reading from
            // the array we pass to WriteFile, but if we read beyond the end and
            // that memory isn't allocated, we could get an AV.)
            if (bytes.Length - offset < count)
                throw new IndexOutOfRangeException(SR.IndexOutOfRange_IORaceCondition);
            Contract.EndContractBlock();

#if USE_OVERLAPPED
            Debug.Assert((_isAsync && overlapped != null) || (!_isAsync && overlapped == null), "Async IO and overlapped parameters inconsistent in call to WriteFileNative.");
#endif

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
#if USE_OVERLAPPED
                if (_isAsync)
                    r = Interop.mincore.WriteFile(handle, p + offset, count, IntPtr.Zero, overlapped);
                else
#endif
                r = Interop.mincore.WriteFile(handle, p + offset, count, out numBytesWritten, IntPtr.Zero);
            }

            if (r == 0)
            {
                errorCode = Marshal.GetLastWin32Error();
                // We should never ignore an error here without some extra work.
                //  We must make sure that BeginWriteCore won't return an 
                // IAsyncResult that will cause EndWrite to block, since the OS 
                // won't call AsyncFSCallback for us.  

                if (errorCode == ERROR_NO_DATA)
                {
                    // This handle was a pipe, and the pipe is being closed on the 
                    // other side.  Let the caller handle this, since BeginWriteCore 
                    // & WriteCore need to do different things.
                    return -1;
                }

                // See code:#errorInvalidHandle in "private long SeekCore(long offset, SeekOrigin origin)".
                if (errorCode == Interop.ERROR_INVALID_HANDLE)
                    _handle.Dispose();

                return -1;
            }
            else
                errorCode = 0;
            return numBytesWritten;
        }

        [System.Security.SecuritySafeCritical]
        public override Task<int> ReadAsync(Byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<int>(cancellationToken);

            if (_handle.IsClosed)
                throw __Error.GetFileNotOpen();

#if USE_OVERLAPPED
            // If async IO is not supported on this platform or 
            // if this Win32FileStream was not opened with FileOptions.Asynchronous.
            if (!_isAsync)
#endif
            {
                return base.ReadAsync(buffer, offset, count, cancellationToken);
            }
#if USE_OVERLAPPED
            var asyncState = new FileStreamTaskAsyncState<int>();
            asyncState._cancellationToken = cancellationToken;
            asyncState._taskCompletionSource = new TaskCompletionSource<int>(asyncState);
            var endReadTask = s_endReadTask;
            if (endReadTask == null) s_endReadTask = endReadTask = EndReadTask; // benign initialization race condition
            asyncState._asyncResult = BeginReadAsync(buffer, offset, count, endReadTask, asyncState);

            if (asyncState._asyncResult.IsAsync && cancellationToken.CanBeCanceled)
            {
                var cancelReadHandler = s_cancelReadHandler;
                if (cancelReadHandler == null) s_cancelReadHandler = cancelReadHandler = CancelTask<int>; // benign initialization race condition
                asyncState._registration = cancellationToken.Register(cancelReadHandler, asyncState);

                // In case the task is completed right before we register the cancellation callback.
                if (asyncState._asyncResult.IsCompleted)
                    asyncState._registration.Dispose();
            }

            return asyncState._taskCompletionSource.Task;
#endif
        }

        [System.Security.SecuritySafeCritical]
        public override Task WriteAsync(Byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            if (_handle.IsClosed)
                throw __Error.GetFileNotOpen();

#if USE_OVERLAPPED
            // If async IO is not supported on this platform or 
            // if this Win32FileStream was not opened with FileOptions.Asynchronous.
            if (!_isAsync)
#endif
                return base.WriteAsync(buffer, offset, count, cancellationToken);
#if USE_OVERLAPPED
            var asyncState = new FileStreamTaskAsyncState<VoidTaskResult>();
            asyncState._cancellationToken = cancellationToken;
            asyncState._taskCompletionSource = new TaskCompletionSource<VoidTaskResult>(asyncState);
            var endWriteTask = s_endWriteTask;
            if (endWriteTask == null) s_endWriteTask = endWriteTask = EndWriteTask; // benign initialization race condition
            asyncState._asyncResult = BeginWriteAsync(buffer, offset, count, endWriteTask, asyncState);

            if (asyncState._asyncResult.IsAsync && cancellationToken.CanBeCanceled)
            {
                var cancelWriteHandler = s_cancelWriteHandler;
                if (cancelWriteHandler == null) s_cancelWriteHandler = cancelWriteHandler = CancelTask<VoidTaskResult>; // benign initialization race condition
                asyncState._registration = cancellationToken.Register(cancelWriteHandler, asyncState);

                // In case the task is completed right before we register the cancellation callback.
                if (asyncState._asyncResult.IsCompleted)
                    asyncState._registration.Dispose();
            }

            return asyncState._taskCompletionSource.Task;
#endif
        }

#if USE_OVERLAPPED
        // Holds state we need when our TPM methods delegate to our APM methods
        private sealed class FileStreamTaskAsyncState<T>
        {
            internal TaskCompletionSource<T> _taskCompletionSource;
            internal CancellationToken _cancellationToken;
            internal CancellationTokenRegistration _registration; // initialized after Begin call completes
            internal FileStreamAsyncResult _asyncResult; // initialized after Begin call completes
        }

        // Cancellation callback for both ReadAsync and WriteAsync.
        [System.Security.SecuritySafeCritical]
        private static void CancelTask<T>(object state)
        {
            var fileStreamTaskAsyncState = state as FileStreamTaskAsyncState<T>;
            Debug.Assert(fileStreamTaskAsyncState != null);
            FileStreamAsyncResult asyncResult = fileStreamTaskAsyncState._asyncResult;

            // This method is used as both the completion callback and the cancellation callback.
            // We should try to cancel the operation if this is running as the completion callback
            // or if cancellation is not applicable:
            // 1. asyncResult is not a FileStreamAsyncResult
            // 2. asyncResult.IsAsync is false: asyncResult is a "synchronous" FileStreamAsyncResult.
            // 3. The asyncResult is completed: this should never happen.
            Debug.Assert((!asyncResult.IsWrite && typeof(T) == typeof(int)) ||
                            (asyncResult.IsWrite && typeof(T) == typeof(VoidTaskResult)));
            Debug.Assert(asyncResult != null);
            Debug.Assert(asyncResult.IsAsync);

            try
            {
                // Cancel the overlapped read and set the task to cancelled state.
                if (!asyncResult.IsCompleted)
                    asyncResult.Cancel();
            }
            catch (Exception ex)
            {
                fileStreamTaskAsyncState._taskCompletionSource.TrySetException(ex);
            }
        }

        // Completion callback for ReadAsync
        [System.Security.SecuritySafeCritical]
        private static void EndReadTask(IAsyncResult iar)
        {
            FileStreamAsyncResult asyncResult = iar as FileStreamAsyncResult;
            Debug.Assert(asyncResult != null);
            Debug.Assert(asyncResult.IsCompleted, "How can we end up in the completion callback if the IAsyncResult is not completed?");

            var asyncState = asyncResult.AsyncState as FileStreamTaskAsyncState<int>;
            Debug.Assert(asyncState != null);

            try
            {
                if (asyncResult.IsAsync)
                {
                    asyncResult.ReleaseNativeResource();

                    // release the resource held by CancellationTokenRegistration
                    asyncState._registration.Dispose();
                }

                if (asyncResult.ErrorCode == Interop.ERROR_OPERATION_ABORTED)
                {
                    var cancellationToken = asyncState._cancellationToken;
                    Debug.Assert(cancellationToken.IsCancellationRequested, "How can the IO operation be aborted if cancellation was not requested?");
                    TrySetCanceled(asyncState._taskCompletionSource, cancellationToken);
                }
                else
                    asyncState._taskCompletionSource.TrySetResult(asyncResult.NumBytesRead);
            }
            catch (Exception ex)
            {
                asyncState._taskCompletionSource.TrySetException(ex);
            }
        }

        // Completion callback for WriteAsync
        [System.Security.SecuritySafeCritical]
        private static void EndWriteTask(IAsyncResult iar)
        {
            var asyncResult = iar as FileStreamAsyncResult;
            Debug.Assert(asyncResult != null);
            Debug.Assert(asyncResult.IsCompleted, "How can we end up in the completion callback if the IAsyncResult is not completed?");

            var asyncState = iar.AsyncState as FileStreamTaskAsyncState<VoidTaskResult>;
            Debug.Assert(asyncState != null);

            try
            {
                if (asyncResult.IsAsync)
                {
                    asyncResult.ReleaseNativeResource();

                    // release the resource held by CancellationTokenRegistration
                    asyncState._registration.Dispose();
                }

                if (asyncResult.ErrorCode == Interop.ERROR_OPERATION_ABORTED)
                {
                    var cancellationToken = asyncState._cancellationToken;
                    Debug.Assert(cancellationToken.IsCancellationRequested, "How can the IO operation be aborted if cancellation was not requested?");
                    TrySetCanceled(asyncState._taskCompletionSource, cancellationToken);
                }
                else
                    asyncState._taskCompletionSource.TrySetResult(default(VoidTaskResult));
            }
            catch (Exception ex)
            {
                asyncState._taskCompletionSource.TrySetException(ex);
            }
        }

        private static void TrySetCanceled<T>(TaskCompletionSource<T> taskCompletionSource, CancellationToken cancellationToken)
        {
            taskCompletionSource.TrySetCanceled(cancellationToken);
        }
#endif

        // Unlike Flush(), FlushAsync() always flushes to disk. This is intentional.
        // Legend is that we chose not to flush the OS file buffers in Flush() in fear of 
        // perf problems with frequent, long running FlushFileBuffers() calls. But we don't 
        // have that problem with FlushAsync() because we will call FlushFileBuffers() in the background.
        [System.Security.SecuritySafeCritical]
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            if (_handle.IsClosed)
                throw __Error.GetFileNotOpen();

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

            if (_parent.CanWrite)
                return Task.Factory.StartNew(
                    state => ((Win32FileStream)state).FlushOSBuffer(),
                    this,
                    cancellationToken,
                    TaskCreationOptions.DenyChildAttach,
                    TaskScheduler.Default);
            else
                return Task.CompletedTask;
        }

        private struct VoidTaskResult { }
    }
}