// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Security;

namespace System.IO.Pipes
{
    public abstract partial class PipeStream : Stream
    {
        private SafePipeHandle _handle;
        private bool _canRead;
        private bool _canWrite;
        private bool _isAsync;
        private bool _isMessageComplete;
        private bool _isFromExistingHandle;
        private bool _isHandleExposed;
        private PipeTransmissionMode _readMode;
        private PipeTransmissionMode _transmissionMode;
        private PipeDirection _pipeDirection;
        private int _outBufferSize;
        private PipeState _state;
        private StreamAsyncHelper _streamAsyncHelper;

        protected PipeStream(PipeDirection direction, int bufferSize)
        {
            if (direction < PipeDirection.In || direction > PipeDirection.InOut)
            {
                throw new ArgumentOutOfRangeException("direction", SR.ArgumentOutOfRange_DirectionModeInOutOrInOut);
            }
            if (bufferSize < 0)
            {
                throw new ArgumentOutOfRangeException("bufferSize", SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            Init(direction, PipeTransmissionMode.Byte, bufferSize);
        }

        protected PipeStream(PipeDirection direction, PipeTransmissionMode transmissionMode, int outBufferSize)
        {
            if (direction < PipeDirection.In || direction > PipeDirection.InOut)
            {
                throw new ArgumentOutOfRangeException("direction", SR.ArgumentOutOfRange_DirectionModeInOutOrInOut);
            }
            if (transmissionMode < PipeTransmissionMode.Byte || transmissionMode > PipeTransmissionMode.Message)
            {
                throw new ArgumentOutOfRangeException("transmissionMode", SR.ArgumentOutOfRange_TransmissionModeByteOrMsg);
            }
            if (outBufferSize < 0)
            {
                throw new ArgumentOutOfRangeException("outBufferSize", SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            Init(direction, transmissionMode, outBufferSize);
        }

        private void Init(PipeDirection direction, PipeTransmissionMode transmissionMode, int outBufferSize)
        {
            Debug.Assert(direction >= PipeDirection.In && direction <= PipeDirection.InOut, "invalid pipe direction");
            Debug.Assert(transmissionMode >= PipeTransmissionMode.Byte && transmissionMode <= PipeTransmissionMode.Message, "transmissionMode is out of range");
            Debug.Assert(outBufferSize >= 0, "outBufferSize is negative");

            // always defaults to this until overriden
            _readMode = transmissionMode;
            _transmissionMode = transmissionMode;

            _pipeDirection = direction;

            if ((_pipeDirection & PipeDirection.In) != 0)
            {
                _canRead = true;
            }
            if ((_pipeDirection & PipeDirection.Out) != 0)
            {
                _canWrite = true;
            }

            _outBufferSize = outBufferSize;

            // This should always default to true
            _isMessageComplete = true;

            _state = PipeState.WaitingToConnect;
            _streamAsyncHelper = new StreamAsyncHelper(this);
        }

        // Once a PipeStream has a handle ready, it should call this method to set up the PipeStream.  If
        // the pipe is in a connected state already, it should also set the IsConnected (protected) property.
        [SecuritySafeCritical]
        internal void InitializeHandle(SafePipeHandle handle, bool isExposed, bool isAsync)
        {
            Debug.Assert(handle != null, "handle is null");

            if (isAsync)
            {
                InitializeAsyncHandle(handle);
            }

            _handle = handle;
            _isAsync = isAsync;

            // track these separately; m_isHandleExposed will get updated if accessed though the property
            _isHandleExposed = isExposed;
            _isFromExistingHandle = isExposed;
        }

        [SecurityCritical]
        public override int Read([In, Out] byte[] buffer, int offset, int count)
        {
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

            return ReadCore(buffer, offset, count);
        }

        [SecurityCritical]
        public override void Write(byte[] buffer, int offset, int count)
        {
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

            WriteCore(buffer, offset, count);

            return;
        }

        [ThreadStatic]
        private static byte[] t_singleByteArray;

        private static byte[] SingleByteArray
        {
<<<<<<< HEAD
            get { return t_singleByteArray ?? (t_singleByteArray = new byte[1]); }
=======
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

        [System.Security.SecuritySafeCritical]
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
                return Task.FromCanceled<int>(cancellationToken);
            }

            CheckReadOperations();

            if (!_isAsync)
            {
                return base.ReadAsync(buffer, offset, count, cancellationToken);
            }

            ReadWriteAsyncParams state = new ReadWriteAsyncParams(buffer, offset, count, cancellationToken);

            return Task.Factory.FromAsync<int>(BeginRead, EndRead, state);
        }

        [System.Security.SecuritySafeCritical]
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
                return Task.FromCanceled<int>(cancellationToken);
            }

            CheckWriteOperations();

            if (!_isAsync)
            {
                return base.WriteAsync(buffer, offset, count, cancellationToken);
            }

            ReadWriteAsyncParams state = new ReadWriteAsyncParams(buffer, offset, count, cancellationToken);

            return Task.Factory.FromAsync(BeginWrite, EndWrite, state);
        }

        [System.Security.SecurityCritical]
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

        [System.Security.SecurityCritical]
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
>>>>>>> master
        }

        // Reads a byte from the pipe stream.  Returns the byte cast to an int
        // or -1 if the connection has been broken.
        [SecurityCritical]
        public override int ReadByte()
        {
            CheckReadOperations();
            if (!CanRead)
            {
                throw __Error.GetReadNotSupported();
            }

            byte[] buffer = SingleByteArray;
            int n = ReadCore(buffer, 0, 1);

            if (n == 0) { return -1; }
            else return (int)buffer[0];
        }

        [SecurityCritical]
        public override void WriteByte(byte value)
        {
            CheckWriteOperations();
            if (!CanWrite)
            {
                throw __Error.GetWriteNotSupported();
            }

            byte[] buffer = SingleByteArray;
            buffer[0] = value;
            WriteCore(buffer, 0, 1);
        }

        // Does nothing on PipeStreams.  We cannot call Interop.FlushFileBuffers here because we can deadlock
        // if the other end of the pipe is no longer interested in reading from the pipe. 
        [SecurityCritical]
        public override void Flush()
        {
            CheckWriteOperations();
            if (!CanWrite)
            {
                throw __Error.GetWriteNotSupported();
            }
        }

        [SecurityCritical]
        protected override void Dispose(bool disposing)
        {
            try
            {
                // Nothing will be done differently based on whether we are 
                // disposing vs. finalizing.  
                if (_handle != null && !_handle.IsClosed)
                {
                    _handle.Dispose();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }

            _state = PipeState.Closed;
        }

        // ********************** Public Properties *********************** //

        // Apis use coarser definition of connected, but these map to internal 
        // Connected/Disconnected states. Note that setter is protected; only
        // intended to be called by custom PipeStream concrete children
        public bool IsConnected
        {
            get
            {
                return State == PipeState.Connected;
            }
            protected set
            {
                _state = (value) ? PipeState.Connected : PipeState.Disconnected;
            }
        }

        public bool IsAsync
        {
            get { return _isAsync; }
        }

        // Set by the most recent call to Read or EndRead.  Will be false if there are more buffer in the
        // message, otherwise it is set to true. 
        public bool IsMessageComplete
        {
            [SecurityCritical]
            [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Security model of pipes: demand at creation but no subsequent demands")]
            get
            {
                // omitting pipe broken exception to allow reader to finish getting message
                if (_state == PipeState.WaitingToConnect)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_PipeNotYetConnected);
                }
                if (_state == PipeState.Disconnected)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_PipeDisconnected);
                }
                if (_handle == null)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_PipeHandleNotSet);
                }

                if (_state == PipeState.Closed)
                {
                    throw __Error.GetPipeNotOpen();
                }
                if (_handle.IsClosed)
                {
                    throw __Error.GetPipeNotOpen();
                }
                // don't need to check transmission mode; just care about read mode. Always use
                // cached mode; otherwise could throw for valid message when other side is shutting down
                if (_readMode != PipeTransmissionMode.Message)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_PipeReadModeNotMessage);
                }

                return _isMessageComplete;
            }
        }

        public SafePipeHandle SafePipeHandle
        {
            [SecurityCritical]
            [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Security model of pipes: demand at creation but no subsequent demands")]
            get
            {
                if (_handle == null)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_PipeHandleNotSet);
                }
                if (_handle.IsClosed)
                {
                    throw __Error.GetPipeNotOpen();
                }

                _isHandleExposed = true;
                return _handle;
            }
        }

        internal SafePipeHandle InternalHandle
        {
            [SecurityCritical]
            get
            {
                return _handle;
            }
        }

        internal bool IsHandleExposed
        {
            get
            {
                return _isHandleExposed;
            }
        }

        public override bool CanRead
        {
            [Pure]
            get
            {
                return _canRead;
            }
        }

        public override bool CanWrite
        {
            [Pure]
            get
            {
                return _canWrite;
            }
        }

        public override bool CanSeek
        {
            [Pure]
            get
            {
                return false;
            }
        }

        public override long Length
        {
            get
            {
                throw __Error.GetSeekNotSupported();
            }
        }

        public override long Position
        {
            get
            {
                throw __Error.GetSeekNotSupported();
            }
            set
            {
                throw __Error.GetSeekNotSupported();
            }
        }

        public override void SetLength(long value)
        {
            throw __Error.GetSeekNotSupported();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw __Error.GetSeekNotSupported();
        }

        // anonymous pipe ends and named pipe server can get/set properties when broken 
        // or connected. Named client overrides
        [SecurityCritical]
        internal virtual void CheckPipePropertyOperations()
        {
            if (_handle == null)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeHandleNotSet);
            }

            // these throw object disposed
            if (_state == PipeState.Closed)
            {
                throw __Error.GetPipeNotOpen();
            }
            if (_handle.IsClosed)
            {
                throw __Error.GetPipeNotOpen();
            }
        }

        // Reads can be done in Connected and Broken. In the latter,
        // read returns 0 bytes
        [SecurityCritical]
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Consistent with security model")]
        internal void CheckReadOperations()
        {
            // Invalid operation
            if (_state == PipeState.WaitingToConnect)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeNotYetConnected);
            }
            if (_state == PipeState.Disconnected)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeDisconnected);
            }
            if (_handle == null)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeHandleNotSet);
            }

            // these throw object disposed
            if (_state == PipeState.Closed)
            {
                throw __Error.GetPipeNotOpen();
            }
            if (_handle.IsClosed)
            {
                throw __Error.GetPipeNotOpen();
            }
        }

        // Writes can only be done in connected state
        [SecurityCritical]
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Consistent with security model")]
        internal void CheckWriteOperations()
        {
            // Invalid operation
            if (_state == PipeState.WaitingToConnect)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeNotYetConnected);
            }
            if (_state == PipeState.Disconnected)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeDisconnected);
            }
            if (_handle == null)
            {
                throw new InvalidOperationException(SR.InvalidOperation_PipeHandleNotSet);
            }

            // IOException
            if (_state == PipeState.Broken)
            {
                throw new IOException(SR.IO_PipeBroken);
            }

            // these throw object disposed
            if (_state == PipeState.Closed)
            {
                throw __Error.GetPipeNotOpen();
            }
            if (_handle.IsClosed)
            {
                throw __Error.GetPipeNotOpen();
            }
        }

        internal PipeState State
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
            }
        }
    }
}


