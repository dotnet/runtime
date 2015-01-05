// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipes
{
    public abstract partial class PipeStream : Stream
    {
        private class ReadWriteAsyncParams
        {
            public ReadWriteAsyncParams() { }
            public ReadWriteAsyncParams(Byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                this.Buffer = buffer;
                this.Offset = offset;
                this.Count = count;
                this.CancellationHelper = cancellationToken.CanBeCanceled ? new IOCancellationHelper(cancellationToken) : null;
            }
            public Byte[] Buffer { get; set; }
            public int Offset { get; set; }
            public int Count { get; set; }
            public IOCancellationHelper CancellationHelper { get; private set; }
        }

        [SecurityCritical]
        private unsafe static readonly IOCompletionCallback s_IOCallback = new IOCompletionCallback(PipeStream.AsyncPSCallback);

        /// <summary>Throws an exception if the supplied handle does not represent a valid pipe.</summary>
        /// <param name="safePipeHandle">The handle to validate.</param>
        internal static void ValidateHandleIsPipe(SafePipeHandle safePipeHandle)
        {
            // Check that this handle is infact a handle to a pipe.
            if (Interop.mincore.GetFileType(safePipeHandle) != Interop.FILE_TYPE_PIPE)
            {
                throw new IOException(SR.IO_InvalidPipeHandle);
            }
        }

        /// <summary>Initializes the handle to be used asynchronously.</summary>
        /// <param name="handle">The handle.</param>
        private void InitializeAsyncHandle(SafePipeHandle handle)
        {
            // If the handle is of async type, bind the handle to the ThreadPool so that we can use 
            // the async operations (it's needed so that our native callbacks get called).
            if (!ThreadPool.BindHandle(handle))
            {
                throw new IOException(SR.IO_BindHandleFailed);
            }
        }

        [SecurityCritical]
        private unsafe int ReadCore(byte[] buffer, int offset, int count)
        {
            Debug.Assert(_handle != null, "_handle is null");
            Debug.Assert(!_handle.IsClosed, "_handle is closed");
            Debug.Assert(CanRead, "can't read");
            Debug.Assert(buffer != null, "buffer is null");
            Debug.Assert(offset >= 0, "offset is negative");
            Debug.Assert(count >= 0, "count is negative");

            if (_isAsync)
            {
                IAsyncResult result = BeginReadCore(buffer, offset, count, null, null);
                return EndRead(result);
            }

            int errorCode = 0;
            int r = ReadFileNative(_handle, buffer, offset, count, null, out errorCode);

            if (r == -1)
            {
                // If the other side has broken the connection, set state to Broken and return 0
                if (errorCode == Interop.ERROR_BROKEN_PIPE ||
                    errorCode == Interop.ERROR_PIPE_NOT_CONNECTED)
                {
                    State = PipeState.Broken;
                    r = 0;
                }
                else
                {
                    throw Win32Marshal.GetExceptionForWin32Error(errorCode, String.Empty);
                }
            }
            _isMessageComplete = (errorCode != Interop.ERROR_MORE_DATA);

            Debug.Assert(r >= 0, "PipeStream's ReadCore is likely broken.");

            return r;
        }

        [SecurityCritical]
        private IAsyncResult BeginRead(AsyncCallback callback, Object state)
        {
            ReadWriteAsyncParams readWriteParams = state as ReadWriteAsyncParams;
            Debug.Assert(readWriteParams != null);
            byte[] buffer = readWriteParams.Buffer;
            int offset = readWriteParams.Offset;
            int count = readWriteParams.Count;

            if (buffer == null)
            {
                throw new ArgumentNullException("buffer", SR.ArgumentNull_Buffer);
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset", SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (buffer.Length - offset < count)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }
            if (!CanRead)
            {
                throw __Error.GetReadNotSupported();
            }
            CheckReadOperations();

            if (!_isAsync)
            {
                // special case when this is called for sync broken pipes because otherwise Stream's
                // Begin/EndRead hang. Reads return 0 bytes in this case so we can call the user's
                // callback immediately
                if (_state == PipeState.Broken)
                {
                    PipeStreamAsyncResult asyncResult = new PipeStreamAsyncResult();
                    asyncResult._handle = _handle;
                    asyncResult._userCallback = callback;
                    asyncResult._userStateObject = state;
                    asyncResult._isWrite = false;
                    asyncResult.CallUserCallback();
                    return asyncResult;
                }
                else
                {
                    return _streamAsyncHelper.BeginRead(buffer, offset, count, callback, state);
                }
            }
            else
            {
                return BeginReadCore(buffer, offset, count, callback, state);
            }
        }

        [SecurityCritical]
        unsafe private PipeStreamAsyncResult BeginReadCore(byte[] buffer, int offset, int count,
                AsyncCallback callback, Object state)
        {
            Debug.Assert(_handle != null, "_handle is null");
            Debug.Assert(!_handle.IsClosed, "_handle is closed");
            Debug.Assert(CanRead, "can't read");
            Debug.Assert(buffer != null, "buffer == null");
            Debug.Assert(_isAsync, "BeginReadCore doesn't work on synchronous file streams!");
            Debug.Assert(offset >= 0, "offset is negative");
            Debug.Assert(count >= 0, "count is negative");

            // Create and store async stream class library specific data in the async result
            PipeStreamAsyncResult asyncResult = new PipeStreamAsyncResult();
            asyncResult._handle = _handle;
            asyncResult._userCallback = callback;
            asyncResult._userStateObject = state;
            asyncResult._isWrite = false;

            // handle zero-length buffers separately; fixed keyword ReadFileNative doesn't like
            // 0-length buffers. Call user callback and we're done
            if (buffer.Length == 0)
            {
                asyncResult.CallUserCallback();
            }
            else
            {
                // For Synchronous IO, I could go with either a userCallback and using
                // the managed Monitor class, or I could create a handle and wait on it.
                ManualResetEvent waitHandle = new ManualResetEvent(false);
                asyncResult._waitHandle = waitHandle;

                // Create a managed overlapped class; set the file offsets later
                Overlapped overlapped = new Overlapped();
                overlapped.OffsetLow = 0;
                overlapped.OffsetHigh = 0;
                overlapped.AsyncResult = asyncResult;

                // Pack the Overlapped class, and store it in the async result
                NativeOverlapped* intOverlapped;
                intOverlapped = overlapped.Pack(s_IOCallback, buffer);


                asyncResult._overlapped = intOverlapped;

                // Queue an async ReadFile operation and pass in a packed overlapped
                int errorCode = 0;
                int r = ReadFileNative(_handle, buffer, offset, count, intOverlapped, out errorCode);

                // ReadFile, the OS version, will return 0 on failure, but this ReadFileNative wrapper
                // returns -1. This will return the following:
                // - On error, r==-1.
                // - On async requests that are still pending, r==-1 w/ hr==ERROR_IO_PENDING
                // - On async requests that completed sequentially, r==0
                // 
                // You will NEVER RELIABLY be able to get the number of buffer read back from this call 
                // when using overlapped structures!  You must not pass in a non-null lpNumBytesRead to
                // ReadFile when using overlapped structures!  This is by design NT behavior.
                if (r == -1)
                {
                    // One side has closed its handle or server disconnected. Set the state to Broken 
                    // and do some cleanup work
                    if (errorCode == Interop.ERROR_BROKEN_PIPE ||
                        errorCode == Interop.ERROR_PIPE_NOT_CONNECTED)
                    {
                        State = PipeState.Broken;

                        // Clear the overlapped status bit for this special case. Failure to do so looks 
                        // like we are freeing a pending overlapped.
                        intOverlapped->InternalLow = IntPtr.Zero;

                        // EndRead will free the Overlapped struct
                        asyncResult.CallUserCallback();
                    }
                    else if (errorCode != Interop.ERROR_IO_PENDING)
                    {
                        throw Win32Marshal.GetExceptionForWin32Error(errorCode);
                    }
                }
                ReadWriteAsyncParams readWriteParams = state as ReadWriteAsyncParams;
                if (readWriteParams != null)
                {
                    if (readWriteParams.CancellationHelper != null)
                    {
                        readWriteParams.CancellationHelper.AllowCancellation(_handle, intOverlapped);
                    }
                }
            }
            return asyncResult;
        }

        [SecurityCritical]
        private unsafe int EndRead(IAsyncResult asyncResult)
        {
            // There are 3 significantly different IAsyncResults we'll accept
            // here.  One is from Stream::BeginRead.  The other two are variations
            // on our PipeStreamAsyncResult.  One is from BeginReadCore,
            // while the other is from the BeginRead buffering wrapper.
            if (asyncResult == null)
            {
                throw new ArgumentNullException("asyncResult");
            }
            if (!_isAsync)
            {
                return _streamAsyncHelper.EndRead(asyncResult);
            }

            PipeStreamAsyncResult afsar = asyncResult as PipeStreamAsyncResult;
            if (afsar == null || afsar._isWrite)
            {
                throw __Error.GetWrongAsyncResult();
            }

            // Ensure we can't get into any races by doing an interlocked
            // CompareExchange here.  Avoids corrupting memory via freeing the
            // NativeOverlapped class or GCHandle twice. 
            if (1 == Interlocked.CompareExchange(ref afsar._EndXxxCalled, 1, 0))
            {
                throw __Error.GetEndReadCalledTwice();
            }

            ReadWriteAsyncParams readWriteParams = asyncResult.AsyncState as ReadWriteAsyncParams;
            IOCancellationHelper cancellationHelper = null;
            if (readWriteParams != null)
            {
                cancellationHelper = readWriteParams.CancellationHelper;
                if (cancellationHelper != null)
                {
                    readWriteParams.CancellationHelper.SetOperationCompleted();
                }
            }

            // Obtain the WaitHandle, but don't use public property in case we
            // delay initialize the manual reset event in the future.
            WaitHandle wh = afsar._waitHandle;
            if (wh != null)
            {
                // We must block to ensure that AsyncPSCallback has completed,
                // and we should close the WaitHandle in here.  AsyncPSCallback
                // and the hand-ported imitation version in COMThreadPool.cpp 
                // are the only places that set this event.
                using (wh)
                {
                    wh.WaitOne();
                    Debug.Assert(afsar._isComplete == true,
                        "FileStream::EndRead - AsyncPSCallback didn't set _isComplete to true!");
                }
            }

            // Free memory & GC handles.
            NativeOverlapped* overlappedPtr = afsar._overlapped;
            if (overlappedPtr != null)
            {
                Overlapped.Free(overlappedPtr);
            }

            // Now check for any error during the read.
            if (afsar._errorCode != 0)
            {
                if (afsar._errorCode == Interop.ERROR_OPERATION_ABORTED)
                {
                    if (cancellationHelper != null)
                    {
                        cancellationHelper.ThrowIOOperationAborted();
                    }
                }
                WinIOError(afsar._errorCode);
            }

            // set message complete to true if the pipe is broken as well; need this to signal to readers
            // to stop reading
            _isMessageComplete = _state == PipeState.Broken ||
                                  afsar._isMessageComplete;

            return afsar._numBytes;
        }

        [SecurityCritical]
        private unsafe void WriteCore(byte[] buffer, int offset, int count)
        {
            Debug.Assert(_handle != null, "_handle is null");
            Debug.Assert(!_handle.IsClosed, "_handle is closed");
            Debug.Assert(CanWrite, "can't write");
            Debug.Assert(buffer != null, "buffer is null");
            Debug.Assert(offset >= 0, "offset is negative");
            Debug.Assert(count >= 0, "count is negative");

            if (_isAsync)
            {
                IAsyncResult result = BeginWriteCore(buffer, offset, count, null, null);
                EndWrite(result);
                return;
            }

            int errorCode = 0;
            int r = WriteFileNative(_handle, buffer, offset, count, null, out errorCode);

            if (r == -1)
            {
                WinIOError(errorCode);
            }
            Debug.Assert(r >= 0, "PipeStream's WriteCore is likely broken.");
            return;
        }

        [SecurityCritical]
        private IAsyncResult BeginWrite(AsyncCallback callback, Object state)
        {
            ReadWriteAsyncParams readWriteParams = state as ReadWriteAsyncParams;
            Debug.Assert(readWriteParams != null);
            byte[] buffer = readWriteParams.Buffer;
            int offset = readWriteParams.Offset;
            int count = readWriteParams.Count;

            if (buffer == null)
            {
                throw new ArgumentNullException("buffer", SR.ArgumentNull_Buffer);
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset", SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (buffer.Length - offset < count)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }
            if (!CanWrite)
            {
                throw __Error.GetWriteNotSupported();
            }
            CheckWriteOperations();

            if (!_isAsync)
            {
                return _streamAsyncHelper.BeginWrite(buffer, offset, count, callback, state);
            }
            else
            {
                return BeginWriteCore(buffer, offset, count, callback, state);
            }
        }

        [SecurityCritical]
        unsafe private PipeStreamAsyncResult BeginWriteCore(byte[] buffer, int offset, int count,
                AsyncCallback callback, Object state)
        {
            Debug.Assert(_handle != null, "_handle is null");
            Debug.Assert(!_handle.IsClosed, "_handle is closed");
            Debug.Assert(CanWrite, "can't write");
            Debug.Assert(buffer != null, "buffer == null");
            Debug.Assert(_isAsync, "BeginWriteCore doesn't work on synchronous file streams!");
            Debug.Assert(offset >= 0, "offset is negative");
            Debug.Assert(count >= 0, "count is negative");

            // Create and store async stream class library specific data in the async result
            PipeStreamAsyncResult asyncResult = new PipeStreamAsyncResult();
            asyncResult._userCallback = callback;
            asyncResult._userStateObject = state;
            asyncResult._isWrite = true;
            asyncResult._handle = _handle;

            // fixed doesn't work well with zero length arrays. Set the zero-byte flag in case
            // caller needs to do any cleanup
            if (buffer.Length == 0)
            {
                //intOverlapped->InternalLow = IntPtr.Zero;

                // EndRead will free the Overlapped struct
                asyncResult.CallUserCallback();
            }
            else
            {
                // For Synchronous IO, I could go with either a userCallback and using the managed 
                // Monitor class, or I could create a handle and wait on it.
                ManualResetEvent waitHandle = new ManualResetEvent(false);
                asyncResult._waitHandle = waitHandle;

                // Create a managed overlapped class; set the file offsets later
                Overlapped overlapped = new Overlapped();
                overlapped.OffsetLow = 0;
                overlapped.OffsetHigh = 0;
                overlapped.AsyncResult = asyncResult;

                // Pack the Overlapped class, and store it in the async result
                NativeOverlapped* intOverlapped = overlapped.Pack(s_IOCallback, buffer);
                asyncResult._overlapped = intOverlapped;

                int errorCode = 0;

                // Queue an async WriteFile operation and pass in a packed overlapped
                int r = WriteFileNative(_handle, buffer, offset, count, intOverlapped, out errorCode);

                // WriteFile, the OS version, will return 0 on failure, but this WriteFileNative 
                // wrapper returns -1. This will return the following:
                // - On error, r==-1.
                // - On async requests that are still pending, r==-1 w/ hr==ERROR_IO_PENDING
                // - On async requests that completed sequentially, r==0
                // 
                // You will NEVER RELIABLY be able to get the number of buffer written back from this 
                // call when using overlapped structures!  You must not pass in a non-null 
                // lpNumBytesWritten to WriteFile when using overlapped structures!  This is by design 
                // NT behavior.
                if (r == -1 && errorCode != Interop.ERROR_IO_PENDING)
                {
                    // Clean up
                    if (intOverlapped != null) Overlapped.Free(intOverlapped);
                    WinIOError(errorCode);
                }

                ReadWriteAsyncParams readWriteParams = state as ReadWriteAsyncParams;
                if (readWriteParams != null)
                {
                    if (readWriteParams.CancellationHelper != null)
                    {
                        readWriteParams.CancellationHelper.AllowCancellation(_handle, intOverlapped);
                    }
                }
            }

            return asyncResult;
        }

        [SecurityCritical]
        private unsafe void EndWrite(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw new ArgumentNullException("asyncResult");
            }

            if (!_isAsync)
            {
                _streamAsyncHelper.EndWrite(asyncResult);
                return;
            }

            PipeStreamAsyncResult afsar = asyncResult as PipeStreamAsyncResult;
            if (afsar == null || !afsar._isWrite)
            {
                throw __Error.GetWrongAsyncResult();
            }

            // Ensure we can't get into any races by doing an interlocked
            // CompareExchange here.  Avoids corrupting memory via freeing the
            // NativeOverlapped class or GCHandle twice.  -- 
            if (1 == Interlocked.CompareExchange(ref afsar._EndXxxCalled, 1, 0))
            {
                throw __Error.GetEndWriteCalledTwice();
            }

            ReadWriteAsyncParams readWriteParams = afsar.AsyncState as ReadWriteAsyncParams;
            IOCancellationHelper cancellationHelper = null;
            if (readWriteParams != null)
            {
                cancellationHelper = readWriteParams.CancellationHelper;
                if (cancellationHelper != null)
                {
                    cancellationHelper.SetOperationCompleted();
                }
            }

            // Obtain the WaitHandle, but don't use public property in case we
            // delay initialize the manual reset event in the future.
            WaitHandle wh = afsar._waitHandle;
            if (wh != null)
            {
                // We must block to ensure that AsyncPSCallback has completed,
                // and we should close the WaitHandle in here.  AsyncPSCallback
                // and the hand-ported imitation version in COMThreadPool.cpp 
                // are the only places that set this event.
                using (wh)
                {
                    wh.WaitOne();
                    Debug.Assert(afsar._isComplete == true, "PipeStream::EndWrite - AsyncPSCallback didn't set _isComplete to true!");
                }
            }

            // Free memory & GC handles.
            NativeOverlapped* overlappedPtr = afsar._overlapped;
            if (overlappedPtr != null)
            {
                Overlapped.Free(overlappedPtr);
            }

            // Now check for any error during the write.
            if (afsar._errorCode != 0)
            {
                if (afsar._errorCode == Interop.ERROR_OPERATION_ABORTED)
                {
                    if (cancellationHelper != null)
                    {
                        cancellationHelper.ThrowIOOperationAborted();
                    }
                }
                WinIOError(afsar._errorCode);
            }

            // Number of buffer written is afsar._numBytes.
            return;
        }

        [SecuritySafeCritical]
        public override Task<int> ReadAsync(Byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
            if (buffer.Length - offset < count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            Contract.EndContractBlock();

            if (cancellationToken.IsCancellationRequested)
            {
                return TaskHelpers.FromCancellation<int>(cancellationToken);
            }

            CheckReadOperations();

            if (!_isAsync)
            {
                return base.ReadAsync(buffer, offset, count, cancellationToken);
            }

            ReadWriteAsyncParams state = new ReadWriteAsyncParams(buffer, offset, count, cancellationToken);

            return Task.Factory.FromAsync<int>(BeginRead, EndRead, state);
        }

        [SecuritySafeCritical]
        public override Task WriteAsync(Byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
            if (buffer.Length - offset < count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            Contract.EndContractBlock();

            if (cancellationToken.IsCancellationRequested)
            {
                return TaskHelpers.FromCancellation<int>(cancellationToken);
            }

            CheckWriteOperations();

            if (!_isAsync)
            {
                return base.WriteAsync(buffer, offset, count, cancellationToken);
            }

            ReadWriteAsyncParams state = new ReadWriteAsyncParams(buffer, offset, count, cancellationToken);

            return Task.Factory.FromAsync(BeginWrite, EndWrite, state);
        }

        [SecurityCritical]
        private unsafe int ReadFileNative(SafePipeHandle handle, byte[] buffer, int offset, int count,
                NativeOverlapped* overlapped, out int errorCode)
        {
            Debug.Assert(handle != null, "handle is null");
            Debug.Assert(offset >= 0, "offset is negative");
            Debug.Assert(count >= 0, "count is negative");
            Debug.Assert(buffer != null, "buffer == null");
            Debug.Assert((_isAsync && overlapped != null) || (!_isAsync && overlapped == null), "Async IO parameter screwup in call to ReadFileNative.");
            Debug.Assert(buffer.Length - offset >= count, "offset + count >= buffer length");

            // You can't use the fixed statement on an array of length 0. Note that async callers
            // check to avoid calling this first, so they can call user's callback
            if (buffer.Length == 0)
            {
                errorCode = 0;
                return 0;
            }

            int r = 0;
            int numBytesRead = 0;

            fixed (byte* p = buffer)
            {
                if (_isAsync)
                {
                    r = Interop.mincore.ReadFile(handle, p + offset, count, IntPtr.Zero, overlapped);
                }
                else
                {
                    r = Interop.mincore.ReadFile(handle, p + offset, count, out numBytesRead, IntPtr.Zero);
                }
            }

            if (r == 0)
            {
                // We should never silently swallow an error here without some
                // extra work.  We must make sure that BeginReadCore won't return an 
                // IAsyncResult that will cause EndRead to block, since the OS won't
                // call AsyncPSCallback for us.  
                errorCode = Marshal.GetLastWin32Error();

                // In message mode, the ReadFile can inform us that there is more data to come.
                if (errorCode == Interop.ERROR_MORE_DATA)
                {
                    return numBytesRead;
                }

                return -1;
            }
            else
            {
                errorCode = 0;
            }

            return numBytesRead;
        }

        [SecurityCritical]
        private unsafe int WriteFileNative(SafePipeHandle handle, byte[] buffer, int offset, int count,
                NativeOverlapped* overlapped, out int errorCode)
        {
            Debug.Assert(handle != null, "handle is null");
            Debug.Assert(offset >= 0, "offset is negative");
            Debug.Assert(count >= 0, "count is negative");
            Debug.Assert(buffer != null, "buffer == null");
            Debug.Assert((_isAsync && overlapped != null) || (!_isAsync && overlapped == null), "Async IO parameter screwup in call to WriteFileNative.");
            Debug.Assert(buffer.Length - offset >= count, "offset + count >= buffer length");

            // You can't use the fixed statement on an array of length 0. Note that async callers
            // check to avoid calling this first, so they can call user's callback
            if (buffer.Length == 0)
            {
                errorCode = 0;
                return 0;
            }

            int numBytesWritten = 0;
            int r = 0;

            fixed (byte* p = buffer)
            {
                if (_isAsync)
                {
                    r = Interop.mincore.WriteFile(handle, p + offset, count, IntPtr.Zero, overlapped);
                }
                else
                {
                    r = Interop.mincore.WriteFile(handle, p + offset, count, out numBytesWritten, IntPtr.Zero);
                }
            }

            if (r == 0)
            {
                // We should never silently swallow an error here without some
                // extra work.  We must make sure that BeginWriteCore won't return an 
                // IAsyncResult that will cause EndWrite to block, since the OS won't
                // call AsyncPSCallback for us.  
                errorCode = Marshal.GetLastWin32Error();
                return -1;
            }
            else
            {
                errorCode = 0;
            }

            return numBytesWritten;
        }

        // Blocks until the other end of the pipe has read in all written buffer.
        [SecurityCritical]
        public void WaitForPipeDrain()
        {
            CheckWriteOperations();
            if (!CanWrite)
            {
                throw __Error.GetWriteNotSupported();
            }

            // Block until other end of the pipe has read everything.
            if (!Interop.mincore.FlushFileBuffers(_handle))
            {
                WinIOError(Marshal.GetLastWin32Error());
            }
        }

        // ********************** Public Properties *********************** //

        // Gets the transmission mode for the pipe.  This is virtual so that subclassing types can 
        // override this in cases where only one mode is legal (such as anonymous pipes)
        public virtual PipeTransmissionMode TransmissionMode
        {
            [SecurityCritical]
            [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Security model of pipes: demand at creation but no subsequent demands")]
            get
            {
                CheckPipePropertyOperations();

                if (_isFromExistingHandle)
                {
                    int pipeFlags;
                    if (!Interop.mincore.GetNamedPipeInfo(_handle, out pipeFlags, Interop.NULL, Interop.NULL,
                            Interop.NULL))
                    {
                        WinIOError(Marshal.GetLastWin32Error());
                    }
                    if ((pipeFlags & Interop.PIPE_TYPE_MESSAGE) != 0)
                    {
                        return PipeTransmissionMode.Message;
                    }
                    else
                    {
                        return PipeTransmissionMode.Byte;
                    }
                }
                else
                {
                    return _transmissionMode;
                }
            }
        }

        // Gets the buffer size in the inbound direction for the pipe. This checks if pipe has read
        // access. If that passes, call to GetNamedPipeInfo will succeed.
        public virtual int InBufferSize
        {
            [SecurityCritical]
            [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
            get
            {
                CheckPipePropertyOperations();
                if (!CanRead)
                {
                    throw new NotSupportedException(SR.NotSupported_UnreadableStream);
                }

                int inBufferSize;
                if (!Interop.mincore.GetNamedPipeInfo(_handle, Interop.NULL, Interop.NULL, out inBufferSize, Interop.NULL))
                {
                    WinIOError(Marshal.GetLastWin32Error());
                }

                return inBufferSize;
            }
        }

        // Gets the buffer size in the outbound direction for the pipe. This uses cached version 
        // if it's an outbound only pipe because GetNamedPipeInfo requires read access to the pipe.
        // However, returning cached is good fallback, especially if user specified a value in 
        // the ctor.
        public virtual int OutBufferSize
        {
            [SecurityCritical]
            [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Security model of pipes: demand at creation but no subsequent demands")]
            get
            {
                CheckPipePropertyOperations();
                if (!CanWrite)
                {
                    throw new NotSupportedException(SR.NotSupported_UnwritableStream);
                }

                int outBufferSize;

                // Use cached value if direction is out; otherwise get fresh version
                if (_pipeDirection == PipeDirection.Out)
                {
                    outBufferSize = _outBufferSize;
                }
                else if (!Interop.mincore.GetNamedPipeInfo(_handle, Interop.NULL, out outBufferSize,
                    Interop.NULL, Interop.NULL))
                {
                    WinIOError(Marshal.GetLastWin32Error());
                }

                return outBufferSize;
            }
        }

        public virtual PipeTransmissionMode ReadMode
        {
            [SecurityCritical]
            get
            {
                CheckPipePropertyOperations();

                // get fresh value if it could be stale
                if (_isFromExistingHandle || IsHandleExposed)
                {
                    UpdateReadMode();
                }
                return _readMode;
            }
            [SecurityCritical]
            [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Security model of pipes: demand at creation but no subsequent demands")]
            set
            {
                // Nothing fancy here.  This is just a wrapper around the Win32 API.  Note, that NamedPipeServerStream
                // and the AnonymousPipeStreams override this.

                CheckPipePropertyOperations();
                if (value < PipeTransmissionMode.Byte || value > PipeTransmissionMode.Message)
                {
                    throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_TransmissionModeByteOrMsg);
                }

                unsafe
                {
                    int pipeReadType = (int)value << 1;
                    if (!Interop.mincore.SetNamedPipeHandleState(_handle, &pipeReadType, Interop.NULL, Interop.NULL))
                    {
                        WinIOError(Marshal.GetLastWin32Error());
                    }
                    else
                    {
                        _readMode = value;
                    }
                }
            }
        }

        /// <summary>
        /// Determine pipe read mode from Win32 
        /// </summary>
        [SecurityCritical]
        private void UpdateReadMode()
        {
            int flags;
            if (!Interop.mincore.GetNamedPipeHandleState(SafePipeHandle, out flags, Interop.NULL, Interop.NULL,
                    Interop.NULL, Interop.NULL, 0))
            {
                WinIOError(Marshal.GetLastWin32Error());
            }

            if ((flags & Interop.PIPE_READMODE_MESSAGE) != 0)
            {
                _readMode = PipeTransmissionMode.Message;
            }
            else
            {
                _readMode = PipeTransmissionMode.Byte;
            }
        }

        /// <summary>
        /// Filter out all pipe related errors and do some cleanup before calling __Error.WinIOError.
        /// </summary>
        /// <param name="errorCode"></param>
        [SecurityCritical]
        internal void WinIOError(int errorCode)
        {
            if (errorCode == Interop.ERROR_BROKEN_PIPE ||
                errorCode == Interop.ERROR_PIPE_NOT_CONNECTED ||
                errorCode == Interop.ERROR_NO_DATA
                )
            {
                // Other side has broken the connection
                _state = PipeState.Broken;
                throw new IOException(SR.IO_PipeBroken, Win32Marshal.MakeHRFromErrorCode(errorCode));
            }
            else if (errorCode == Interop.ERROR_HANDLE_EOF)
            {
                throw __Error.GetEndOfFile();
            }
            else
            {
                // For invalid handles, detect the error and mark our handle
                // as invalid to give slightly better error messages.  Also
                // help ensure we avoid handle recycling bugs.
                if (errorCode == Interop.ERROR_INVALID_HANDLE)
                {
                    _handle.SetHandleAsInvalid();
                    _state = PipeState.Broken;
                }

                throw Win32Marshal.GetExceptionForWin32Error(errorCode);
            }
        }

        // ************************ Static Methods ************************ //

        [SecurityCritical]
        internal static Interop.SECURITY_ATTRIBUTES GetSecAttrs(HandleInheritability inheritability)
        {
            Interop.SECURITY_ATTRIBUTES secAttrs = default(Interop.SECURITY_ATTRIBUTES);
            if ((inheritability & HandleInheritability.Inheritable) != 0)
            {
                secAttrs = new Interop.SECURITY_ATTRIBUTES();
                secAttrs.nLength = (uint)Marshal.SizeOf(secAttrs);
                secAttrs.bInheritHandle = true;
            }
            return secAttrs;
        }

        // When doing IO asynchronously (i.e., m_isAsync==true), this callback is 
        // called by a free thread in the threadpool when the IO operation 
        // completes.  
        [SecurityCritical]
        unsafe private static void AsyncPSCallback(uint errorCode, uint numBytes, NativeOverlapped* pOverlapped)
        {
            // Unpack overlapped
            Overlapped overlapped = Overlapped.Unpack(pOverlapped);
            // Free the overlapped struct in EndRead/EndWrite.

            // Extract async result from overlapped 
            PipeStreamAsyncResult asyncResult = (PipeStreamAsyncResult)overlapped.AsyncResult;
            asyncResult._numBytes = (int)numBytes;

            // Allow async read to finish
            if (!asyncResult._isWrite)
            {
                if (errorCode == Interop.ERROR_BROKEN_PIPE ||
                    errorCode == Interop.ERROR_PIPE_NOT_CONNECTED ||
                    errorCode == Interop.ERROR_NO_DATA)
                {
                    errorCode = 0;
                    numBytes = 0;
                }
            }

            // For message type buffer.
            if (errorCode == Interop.ERROR_MORE_DATA)
            {
                errorCode = 0;
                asyncResult._isMessageComplete = false;
            }
            else
            {
                asyncResult._isMessageComplete = true;
            }

            asyncResult._errorCode = (int)errorCode;

            // Call the user-provided callback.  It can and often should
            // call EndRead or EndWrite.  There's no reason to use an async 
            // delegate here - we're already on a threadpool thread.  
            // IAsyncResult's completedSynchronously property must return
            // false here, saying the user callback was called on another thread.
            asyncResult._completedSynchronously = false;
            asyncResult._isComplete = true;

            // The OS does not signal this event.  We must do it ourselves.
            ManualResetEvent wh = asyncResult._waitHandle;
            if (wh != null)
            {
                Debug.Assert(!wh.GetSafeWaitHandle().IsClosed, "ManualResetEvent already closed!");
                bool r = wh.Set();
                Debug.Assert(r, "ManualResetEvent::Set failed!");
                if (!r)
                {
                    throw Win32Marshal.GetExceptionForLastWin32Error();
                }
            }

            AsyncCallback callback = asyncResult._userCallback;
            if (callback != null)
            {
                callback(asyncResult);
            }
        }
    }
}


