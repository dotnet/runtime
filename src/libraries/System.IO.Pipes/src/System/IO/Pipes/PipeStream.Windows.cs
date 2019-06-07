// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Pipes
{
    public abstract partial class PipeStream : Stream
    {
        internal const bool CheckOperationsRequiresSetHandle = true;
        internal ThreadPoolBoundHandle _threadPoolBinding;

        internal static string GetPipePath(string serverName, string pipeName)
        {
            string normalizedPipePath = Path.GetFullPath(@"\\" + serverName + @"\pipe\" + pipeName);
            if (string.Equals(normalizedPipePath, @"\\.\pipe\" + AnonymousPipeName, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentOutOfRangeException(nameof(pipeName), SR.ArgumentOutOfRange_AnonymousReserved);
            }
            return normalizedPipePath;
        }

        /// <summary>Throws an exception if the supplied handle does not represent a valid pipe.</summary>
        /// <param name="safePipeHandle">The handle to validate.</param>
        internal void ValidateHandleIsPipe(SafePipeHandle safePipeHandle)
        {
            // Check that this handle is infact a handle to a pipe.
            if (Interop.Kernel32.GetFileType(safePipeHandle) != Interop.Kernel32.FileTypes.FILE_TYPE_PIPE)
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
            _threadPoolBinding = ThreadPoolBoundHandle.BindHandle(handle);
        }

        private void DisposeCore(bool disposing)
        {
            if (disposing)
            {
                _threadPoolBinding?.Dispose();
            }
        }

        private unsafe int ReadCore(Span<byte> buffer)
        {
            int errorCode = 0;
            int r = ReadFileNative(_handle, buffer, null, out errorCode);

            if (r == -1)
            {
                // If the other side has broken the connection, set state to Broken and return 0
                if (errorCode == Interop.Errors.ERROR_BROKEN_PIPE ||
                    errorCode == Interop.Errors.ERROR_PIPE_NOT_CONNECTED)
                {
                    State = PipeState.Broken;
                    r = 0;
                }
                else
                {
                    throw Win32Marshal.GetExceptionForWin32Error(errorCode, string.Empty);
                }
            }
            _isMessageComplete = (errorCode != Interop.Errors.ERROR_MORE_DATA);

            Debug.Assert(r >= 0, "PipeStream's ReadCore is likely broken.");

            return r;
        }

        private ValueTask<int> ReadAsyncCore(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            var completionSource = new ReadWriteCompletionSource(this, buffer, isWrite: false);

            // Queue an async ReadFile operation and pass in a packed overlapped
            int errorCode = 0;
            int r;
            unsafe
            {
                r = ReadFileNative(_handle, buffer.Span, completionSource.Overlapped, out errorCode);
            }

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
                switch (errorCode)
                {
                    // One side has closed its handle or server disconnected.
                    // Set the state to Broken and do some cleanup work
                    case Interop.Errors.ERROR_BROKEN_PIPE:
                    case Interop.Errors.ERROR_PIPE_NOT_CONNECTED:
                        State = PipeState.Broken;

                        unsafe
                        {
                            // Clear the overlapped status bit for this special case. Failure to do so looks 
                            // like we are freeing a pending overlapped.
                            completionSource.Overlapped->InternalLow = IntPtr.Zero;
                        }

                        completionSource.ReleaseResources();
                        UpdateMessageCompletion(true);
                        return new ValueTask<int>(0);

                    case Interop.Errors.ERROR_IO_PENDING:
                        break;

                    default:
                        throw Win32Marshal.GetExceptionForWin32Error(errorCode);
                }
            }

            completionSource.RegisterForCancellation(cancellationToken);
            return new ValueTask<int>(completionSource.Task);
        }

        private unsafe void WriteCore(ReadOnlySpan<byte> buffer)
        {
            int errorCode = 0;
            int r = WriteFileNative(_handle, buffer, null, out errorCode);

            if (r == -1)
            {
                throw WinIOError(errorCode);
            }
            Debug.Assert(r >= 0, "PipeStream's WriteCore is likely broken.");
        }

        private Task WriteAsyncCore(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            var completionSource = new ReadWriteCompletionSource(this, buffer, isWrite: true);
            int errorCode = 0;

            // Queue an async WriteFile operation and pass in a packed overlapped
            int r;
            unsafe
            {
                r = WriteFileNative(_handle, buffer.Span, completionSource.Overlapped, out errorCode);
            }

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
            if (r == -1 && errorCode != Interop.Errors.ERROR_IO_PENDING)
            {
                completionSource.ReleaseResources();
                throw WinIOError(errorCode);
            }

            completionSource.RegisterForCancellation(cancellationToken);
            return completionSource.Task;
        }

        // Blocks until the other end of the pipe has read in all written buffer.
        public void WaitForPipeDrain()
        {
            CheckWriteOperations();
            if (!CanWrite)
            {
                throw Error.GetWriteNotSupported();
            }

            // Block until other end of the pipe has read everything.
            if (!Interop.Kernel32.FlushFileBuffers(_handle))
            {
                throw WinIOError(Marshal.GetLastWin32Error());
            }
        }

        // Gets the transmission mode for the pipe.  This is virtual so that subclassing types can 
        // override this in cases where only one mode is legal (such as anonymous pipes)
        public unsafe virtual PipeTransmissionMode TransmissionMode
        {
            [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Security model of pipes: demand at creation but no subsequent demands")]
            get
            {
                CheckPipePropertyOperations();

                if (_isFromExistingHandle)
                {
                    uint pipeFlags;
                    if (!Interop.Kernel32.GetNamedPipeInfo(_handle, &pipeFlags, null, null, null))
                    {
                        throw WinIOError(Marshal.GetLastWin32Error());
                    }
                    if ((pipeFlags & Interop.Kernel32.PipeOptions.PIPE_TYPE_MESSAGE) != 0)
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
        public unsafe virtual int InBufferSize
        {
            [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
            get
            {
                CheckPipePropertyOperations();
                if (!CanRead)
                {
                    throw new NotSupportedException(SR.NotSupported_UnreadableStream);
                }

                uint inBufferSize;
                if (!Interop.Kernel32.GetNamedPipeInfo(_handle, null, null, &inBufferSize, null))
                {
                    throw WinIOError(Marshal.GetLastWin32Error());
                }

                return (int)inBufferSize;
            }
        }

        // Gets the buffer size in the outbound direction for the pipe. This uses cached version 
        // if it's an outbound only pipe because GetNamedPipeInfo requires read access to the pipe.
        // However, returning cached is good fallback, especially if user specified a value in 
        // the ctor.
        public unsafe virtual int OutBufferSize
        {
            [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Security model of pipes: demand at creation but no subsequent demands")]
            get
            {
                CheckPipePropertyOperations();
                if (!CanWrite)
                {
                    throw new NotSupportedException(SR.NotSupported_UnwritableStream);
                }

                uint outBufferSize;

                // Use cached value if direction is out; otherwise get fresh version
                if (_pipeDirection == PipeDirection.Out)
                {
                    outBufferSize = _outBufferSize;
                }
                else if (!Interop.Kernel32.GetNamedPipeInfo(_handle, null, &outBufferSize, null, null))
                {
                    throw WinIOError(Marshal.GetLastWin32Error());
                }

                return (int)outBufferSize;
            }
        }

        public virtual PipeTransmissionMode ReadMode
        {
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
            [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Security model of pipes: demand at creation but no subsequent demands")]
            set
            {
                // Nothing fancy here.  This is just a wrapper around the Win32 API.  Note, that NamedPipeServerStream
                // and the AnonymousPipeStreams override this.

                CheckPipePropertyOperations();
                if (value < PipeTransmissionMode.Byte || value > PipeTransmissionMode.Message)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_TransmissionModeByteOrMsg);
                }

                unsafe
                {
                    int pipeReadType = (int)value << 1;
                    if (!Interop.Kernel32.SetNamedPipeHandleState(_handle, &pipeReadType, IntPtr.Zero, IntPtr.Zero))
                    {
                        throw WinIOError(Marshal.GetLastWin32Error());
                    }
                    else
                    {
                        _readMode = value;
                    }
                }
            }
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        private unsafe int ReadFileNative(SafePipeHandle handle, Span<byte> buffer, NativeOverlapped* overlapped, out int errorCode)
        {
            DebugAssertHandleValid(handle);
            Debug.Assert((_isAsync && overlapped != null) || (!_isAsync && overlapped == null), "Async IO parameter screwup in call to ReadFileNative.");

            // You can't use the fixed statement on an array of length 0. Note that async callers
            // check to avoid calling this first, so they can call user's callback
            if (buffer.Length == 0)
            {
                errorCode = 0;
                return 0;
            }

            int r = 0;
            int numBytesRead = 0;

            fixed (byte* p = &MemoryMarshal.GetReference(buffer))
            {
                r = _isAsync ?
                    Interop.Kernel32.ReadFile(handle, p, buffer.Length, IntPtr.Zero, overlapped) :
                    Interop.Kernel32.ReadFile(handle, p, buffer.Length, out numBytesRead, IntPtr.Zero);
            }

            if (r == 0)
            {
                // In message mode, the ReadFile can inform us that there is more data to come.
                errorCode = Marshal.GetLastWin32Error();
                return errorCode == Interop.Errors.ERROR_MORE_DATA ?
                    numBytesRead :
                    -1;
            }
            else
            {
                errorCode = 0;
                return numBytesRead;
            }
        }

        private unsafe int WriteFileNative(SafePipeHandle handle, ReadOnlySpan<byte> buffer, NativeOverlapped* overlapped, out int errorCode)
        {
            DebugAssertHandleValid(handle);
            Debug.Assert((_isAsync && overlapped != null) || (!_isAsync && overlapped == null), "Async IO parameter screwup in call to WriteFileNative.");

            // You can't use the fixed statement on an array of length 0. Note that async callers
            // check to avoid calling this first, so they can call user's callback
            if (buffer.Length == 0)
            {
                errorCode = 0;
                return 0;
            }

            int r = 0;
            int numBytesWritten = 0;

            fixed (byte* p = &MemoryMarshal.GetReference(buffer))
            {
                r = _isAsync ?
                    Interop.Kernel32.WriteFile(handle, p, buffer.Length, IntPtr.Zero, overlapped) :
                    Interop.Kernel32.WriteFile(handle, p, buffer.Length, out numBytesWritten, IntPtr.Zero);
            }

            if (r == 0)
            {
                errorCode = Marshal.GetLastWin32Error();
                return -1;
            }
            else
            {
                errorCode = 0;
                return numBytesWritten;
            }
        }

        internal static unsafe Interop.Kernel32.SECURITY_ATTRIBUTES GetSecAttrs(HandleInheritability inheritability)
        {
            Interop.Kernel32.SECURITY_ATTRIBUTES secAttrs = default(Interop.Kernel32.SECURITY_ATTRIBUTES);
            if ((inheritability & HandleInheritability.Inheritable) != 0)
            {
                secAttrs = new Interop.Kernel32.SECURITY_ATTRIBUTES();
                secAttrs.nLength = (uint)sizeof(Interop.Kernel32.SECURITY_ATTRIBUTES);
                secAttrs.bInheritHandle = Interop.BOOL.TRUE;
            }
            return secAttrs;
        }

        internal static unsafe Interop.Kernel32.SECURITY_ATTRIBUTES GetSecAttrs(HandleInheritability inheritability, PipeSecurity pipeSecurity, ref GCHandle pinningHandle)
        {
            Interop.Kernel32.SECURITY_ATTRIBUTES secAttrs = default(Interop.Kernel32.SECURITY_ATTRIBUTES);
            secAttrs.nLength = (uint)sizeof(Interop.Kernel32.SECURITY_ATTRIBUTES);

            if ((inheritability & HandleInheritability.Inheritable) != 0)
            {
                secAttrs.bInheritHandle = Interop.BOOL.TRUE;
            }

            if (pipeSecurity != null)
            {
                byte[] securityDescriptor = pipeSecurity.GetSecurityDescriptorBinaryForm();
                pinningHandle = GCHandle.Alloc(securityDescriptor, GCHandleType.Pinned);
                fixed (byte* pSecurityDescriptor = securityDescriptor)
                {
                    secAttrs.lpSecurityDescriptor = (IntPtr)pSecurityDescriptor;
                }
            }

            return secAttrs;
        }



        /// <summary>
        /// Determine pipe read mode from Win32 
        /// </summary>
        private unsafe void UpdateReadMode()
        {
            uint flags;
            if (!Interop.Kernel32.GetNamedPipeHandleStateW(SafePipeHandle, &flags, null, null, null, null, 0))
            {
                throw WinIOError(Marshal.GetLastWin32Error());
            }

            if ((flags & Interop.Kernel32.PipeOptions.PIPE_READMODE_MESSAGE) != 0)
            {
                _readMode = PipeTransmissionMode.Message;
            }
            else
            {
                _readMode = PipeTransmissionMode.Byte;
            }
        }

        /// <summary>
        /// Filter out all pipe related errors and do some cleanup before calling Error.WinIOError.
        /// </summary>
        /// <param name="errorCode"></param>
        internal Exception WinIOError(int errorCode)
        {
            switch (errorCode)
            {
                case Interop.Errors.ERROR_BROKEN_PIPE:
                case Interop.Errors.ERROR_PIPE_NOT_CONNECTED:
                case Interop.Errors.ERROR_NO_DATA:
                    // Other side has broken the connection
                    _state = PipeState.Broken;
                    return new IOException(SR.IO_PipeBroken, Win32Marshal.MakeHRFromErrorCode(errorCode));

                case Interop.Errors.ERROR_HANDLE_EOF:
                    return Error.GetEndOfFile();

                case Interop.Errors.ERROR_INVALID_HANDLE:
                    // For invalid handles, detect the error and mark our handle
                    // as invalid to give slightly better error messages.  Also
                    // help ensure we avoid handle recycling bugs.
                    _handle.SetHandleAsInvalid();
                    _state = PipeState.Broken;
                    break;
            }

            return Win32Marshal.GetExceptionForWin32Error(errorCode);
        }
    }
}
