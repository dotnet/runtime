// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Pipes
{
    public abstract partial class PipeStream : Stream
    {
        internal const bool CheckOperationsRequiresSetHandle = true;
        internal ThreadPoolBoundHandle? _threadPoolBinding;
        private ReadWriteValueTaskSource? _reusableReadValueTaskSource; // reusable ReadWriteValueTaskSource for read operations, that is currently NOT being used
        private ReadWriteValueTaskSource? _reusableWriteValueTaskSource; // reusable ReadWriteValueTaskSource for write operations, that is currently NOT being used

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_isAsync)
            {
                return ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
            }

            ValidateBufferArguments(buffer, offset, count);
            if (!CanRead)
            {
                throw Error.GetReadNotSupported();
            }
            CheckReadOperations();

            return ReadCore(new Span<byte>(buffer, offset, count));
        }

        public override int Read(Span<byte> buffer)
        {
            if (_isAsync)
            {
                return base.Read(buffer);
            }

            if (!CanRead)
            {
                throw Error.GetReadNotSupported();
            }
            CheckReadOperations();

            return ReadCore(buffer);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            if (!CanRead)
            {
                throw Error.GetReadNotSupported();
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
            }

            CheckReadOperations();

            if (!_isAsync)
            {
                return base.ReadAsync(buffer, offset, count, cancellationToken);
            }

            if (count == 0)
            {
                UpdateMessageCompletion(false);
                return Task.FromResult(0);
            }

            return ReadAsyncCore(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!_isAsync)
            {
                return base.ReadAsync(buffer, cancellationToken);
            }

            if (!CanRead)
            {
                throw Error.GetReadNotSupported();
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<int>(cancellationToken);
            }

            CheckReadOperations();

            if (buffer.Length == 0)
            {
                UpdateMessageCompletion(false);
                return new ValueTask<int>(0);
            }

            return ReadAsyncCore(buffer, cancellationToken);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            if (_isAsync)
                return TaskToApm.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), callback, state);
            else
                return base.BeginRead(buffer, offset, count, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            if (_isAsync)
                return TaskToApm.End<int>(asyncResult);
            else
                return base.EndRead(asyncResult);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_isAsync)
            {
                WriteAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
                return;
            }

            ValidateBufferArguments(buffer, offset, count);
            if (!CanWrite)
            {
                throw Error.GetWriteNotSupported();
            }
            CheckWriteOperations();

            WriteCore(new ReadOnlySpan<byte>(buffer, offset, count));
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (_isAsync)
            {
                base.Write(buffer);
                return;
            }

            if (!CanWrite)
            {
                throw Error.GetWriteNotSupported();
            }
            CheckWriteOperations();

            WriteCore(buffer);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            if (!CanWrite)
            {
                throw Error.GetWriteNotSupported();
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
            }

            CheckWriteOperations();

            if (!_isAsync)
            {
                return base.WriteAsync(buffer, offset, count, cancellationToken);
            }

            if (count == 0)
            {
                return Task.CompletedTask;
            }

            return WriteAsyncCore(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!_isAsync)
            {
                return base.WriteAsync(buffer, cancellationToken);
            }

            if (!CanWrite)
            {
                throw Error.GetWriteNotSupported();
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled(cancellationToken);
            }

            CheckWriteOperations();

            if (buffer.Length == 0)
            {
                return default;
            }

            return WriteAsyncCore(buffer, cancellationToken);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            if (_isAsync)
                return TaskToApm.Begin(WriteAsync(buffer, offset, count, CancellationToken.None), callback, state);
            else
                return base.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            if (_isAsync)
                TaskToApm.End(asyncResult);
            else
                base.EndWrite(asyncResult);
        }

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

        internal virtual void TryToReuse(PipeValueTaskSource source)
        {
            source._source.Reset();

            if (source is ReadWriteValueTaskSource readWriteSource)
            {
                ref ReadWriteValueTaskSource? field = ref readWriteSource._isWrite ? ref _reusableWriteValueTaskSource : ref _reusableReadValueTaskSource;
                if (Interlocked.CompareExchange(ref field, readWriteSource, null) is not null)
                {
                    source._preallocatedOverlapped.Dispose();
                }
            }
        }

        private void DisposeCore(bool disposing)
        {
            if (disposing)
            {
                _threadPoolBinding?.Dispose();
                Interlocked.Exchange(ref _reusableReadValueTaskSource, null)?.Dispose();
                Interlocked.Exchange(ref _reusableWriteValueTaskSource, null)?.Dispose();
            }
        }

        private unsafe int ReadCore(Span<byte> buffer)
        {
            DebugAssertHandleValid(_handle!);
            Debug.Assert(!_isAsync);

            if (buffer.Length == 0)
            {
                return 0;
            }

            fixed (byte* p = &MemoryMarshal.GetReference(buffer))
            {
                int bytesRead = 0;
                if (Interop.Kernel32.ReadFile(_handle!, p, buffer.Length, out bytesRead, IntPtr.Zero) != 0)
                {
                    _isMessageComplete = true;
                    return bytesRead;
                }
                else
                {
                    int errorCode = Marshal.GetLastPInvokeError();
                    _isMessageComplete = errorCode != Interop.Errors.ERROR_MORE_DATA;
                    switch (errorCode)
                    {
                        case Interop.Errors.ERROR_MORE_DATA:
                            return bytesRead;

                        case Interop.Errors.ERROR_BROKEN_PIPE:
                        case Interop.Errors.ERROR_PIPE_NOT_CONNECTED:
                            State = PipeState.Broken;
                            return 0;

                        default:
                            throw Win32Marshal.GetExceptionForWin32Error(errorCode, string.Empty);
                    }
                }
            }
        }

        private unsafe ValueTask<int> ReadAsyncCore(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            Debug.Assert(_isAsync);

            ReadWriteValueTaskSource vts = Interlocked.Exchange(ref _reusableReadValueTaskSource, null) ?? new ReadWriteValueTaskSource(this, isWrite: false);
            try
            {
                vts.PrepareForOperation(buffer);
                Debug.Assert(vts._memoryHandle.Pointer != null);

                // Queue an async ReadFile operation.
                if (Interop.Kernel32.ReadFile(_handle!, (byte*)vts._memoryHandle.Pointer, buffer.Length, IntPtr.Zero, vts._overlapped) == 0)
                {
                    // The operation failed, or it's pending.
                    int errorCode = Marshal.GetLastPInvokeError();
                    switch (errorCode)
                    {
                        case Interop.Errors.ERROR_IO_PENDING:
                            // Common case: IO was initiated, completion will be handled by callback.
                            // Register for cancellation now that the operation has been initiated.
                            vts.RegisterForCancellation(cancellationToken);
                            break;

                        case Interop.Errors.ERROR_MORE_DATA:
                            // The operation is completing asynchronously but there's nothing to cancel.
                            break;

                        // One side has closed its handle or server disconnected.
                        // Set the state to Broken and do some cleanup work
                        case Interop.Errors.ERROR_BROKEN_PIPE:
                        case Interop.Errors.ERROR_PIPE_NOT_CONNECTED:
                            State = PipeState.Broken;
                            vts._overlapped->InternalLow = IntPtr.Zero;
                            vts.Dispose();
                            UpdateMessageCompletion(true);
                            return new ValueTask<int>(0);

                        default:
                            // Error. Callback will not be called.
                            vts.Dispose();
                            return ValueTask.FromException<int>(Win32Marshal.GetExceptionForWin32Error(errorCode));
                    }
                }
            }
            catch
            {
                vts.Dispose();
                throw;
            }

            vts.FinishedScheduling();
            return new ValueTask<int>(vts, vts.Version);
        }

        private unsafe void WriteCore(ReadOnlySpan<byte> buffer)
        {
            DebugAssertHandleValid(_handle!);
            Debug.Assert(!_isAsync);

            if (buffer.Length == 0)
            {
                return;
            }

            fixed (byte* p = &MemoryMarshal.GetReference(buffer))
            {
                int bytesWritten = 0;
                if (Interop.Kernel32.WriteFile(_handle!, p, buffer.Length, out bytesWritten, IntPtr.Zero) == 0)
                {
                    throw WinIOError(Marshal.GetLastPInvokeError());
                }
            }
        }

        private unsafe ValueTask WriteAsyncCore(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            Debug.Assert(_isAsync);

            ReadWriteValueTaskSource vts = Interlocked.Exchange(ref _reusableWriteValueTaskSource, null) ?? new ReadWriteValueTaskSource(this, isWrite: true);
            try
            {
                vts.PrepareForOperation(buffer);
                Debug.Assert(vts._memoryHandle.Pointer != null);

                // Queue an async WriteFile operation.
                if (Interop.Kernel32.WriteFile(_handle!, (byte*)vts._memoryHandle.Pointer, buffer.Length, IntPtr.Zero, vts._overlapped) == 0)
                {
                    // The operation failed, or it's pending.
                    int errorCode = Marshal.GetLastPInvokeError();
                    switch (errorCode)
                    {
                        case Interop.Errors.ERROR_IO_PENDING:
                            // Common case: IO was initiated, completion will be handled by callback.
                            // Register for cancellation now that the operation has been initiated.
                            vts.RegisterForCancellation(cancellationToken);
                            break;

                        default:
                            // Error. Callback will not be invoked.
                            vts.Dispose();
                            return ValueTask.FromException(ExceptionDispatchInfo.SetCurrentStackTrace(WinIOError(errorCode)));
                    }
                }
            }
            catch
            {
                vts.Dispose();
                throw;
            }

            // Completion handled by callback.
            vts.FinishedScheduling();
            return new ValueTask(vts, vts.Version);
        }

        // Blocks until the other end of the pipe has read in all written buffer.
        [SupportedOSPlatform("windows")]
        public void WaitForPipeDrain()
        {
            CheckWriteOperations();
            if (!CanWrite)
            {
                throw Error.GetWriteNotSupported();
            }

            // Block until other end of the pipe has read everything.
            if (!Interop.Kernel32.FlushFileBuffers(_handle!))
            {
                throw WinIOError(Marshal.GetLastPInvokeError());
            }
        }

        // Gets the transmission mode for the pipe.  This is virtual so that subclassing types can
        // override this in cases where only one mode is legal (such as anonymous pipes)
        public unsafe virtual PipeTransmissionMode TransmissionMode
        {
            get
            {
                CheckPipePropertyOperations();

                if (_isFromExistingHandle)
                {
                    uint pipeFlags;
                    if (!Interop.Kernel32.GetNamedPipeInfo(_handle!, &pipeFlags, null, null, null))
                    {
                        throw WinIOError(Marshal.GetLastPInvokeError());
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
            get
            {
                CheckPipePropertyOperations();
                if (!CanRead)
                {
                    throw new NotSupportedException(SR.NotSupported_UnreadableStream);
                }

                uint inBufferSize;
                if (!Interop.Kernel32.GetNamedPipeInfo(_handle!, null, null, &inBufferSize, null))
                {
                    throw WinIOError(Marshal.GetLastPInvokeError());
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
                else if (!Interop.Kernel32.GetNamedPipeInfo(_handle!, null, &outBufferSize, null, null))
                {
                    throw WinIOError(Marshal.GetLastPInvokeError());
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
                    if (!Interop.Kernel32.SetNamedPipeHandleState(_handle!, &pipeReadType, IntPtr.Zero, IntPtr.Zero))
                    {
                        throw WinIOError(Marshal.GetLastPInvokeError());
                    }
                    else
                    {
                        _readMode = value;
                    }
                }
            }
        }

        internal static unsafe Interop.Kernel32.SECURITY_ATTRIBUTES GetSecAttrs(HandleInheritability inheritability)
        {
            Interop.Kernel32.SECURITY_ATTRIBUTES secAttrs = new Interop.Kernel32.SECURITY_ATTRIBUTES
            {
                nLength = (uint)sizeof(Interop.Kernel32.SECURITY_ATTRIBUTES),
                bInheritHandle = ((inheritability & HandleInheritability.Inheritable) != 0) ? Interop.BOOL.TRUE : Interop.BOOL.FALSE
            };

            return secAttrs;
        }

        internal static unsafe Interop.Kernel32.SECURITY_ATTRIBUTES GetSecAttrs(HandleInheritability inheritability, PipeSecurity? pipeSecurity, ref GCHandle pinningHandle)
        {
            Interop.Kernel32.SECURITY_ATTRIBUTES secAttrs = GetSecAttrs(inheritability);

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
                throw WinIOError(Marshal.GetLastPInvokeError());
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
                    _handle!.SetHandleAsInvalid();
                    _state = PipeState.Broken;
                    break;
            }

            return Win32Marshal.GetExceptionForWin32Error(errorCode);
        }
    }
}
