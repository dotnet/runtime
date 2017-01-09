// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipes
{
    public abstract partial class PipeStream : Stream
    {
        // The Windows implementation of PipeStream sets the stream's handle during 
        // creation, and as such should always have a handle, but the Unix implementation 
        // sometimes sets the handle not during creation but later during connection.  
        // As such, validation during member access needs to verify a valid handle on 
        // Windows, but can't assume a valid handle on Unix.
        internal const bool CheckOperationsRequiresSetHandle = false;

        /// <summary>Characters that can't be used in a pipe's name.</summary>
        private static readonly char[] s_invalidFileNameChars = Path.GetInvalidFileNameChars();

        /// <summary>Prefix to prepend to all pipe names.</summary>
        private static readonly string s_pipePrefix = Path.Combine(Path.GetTempPath(), "CoreFxPipe_");

        internal static string GetPipePath(string serverName, string pipeName)
        {
            if (serverName != "." && serverName != Interop.Sys.GetHostName())
            {
                // Cross-machine pipes are not supported.
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_RemotePipes);
            }

            if (string.Equals(pipeName, AnonymousPipeName, StringComparison.OrdinalIgnoreCase))
            {
                // Match Windows constraint
                throw new ArgumentOutOfRangeException(nameof(pipeName), SR.ArgumentOutOfRange_AnonymousReserved);
            }

            if (pipeName.IndexOfAny(s_invalidFileNameChars) >= 0)
            {
                // Since pipes are stored as files in the file system, we don't support
                // pipe names that are actually paths or that otherwise have invalid
                // filename characters in them.
                throw new PlatformNotSupportedException(SR.PlatformNotSupproted_InvalidNameChars);
            }

            // Return the pipe path.  The pipe is created directly under %TMPDIR%.  We don't bother
            // putting it into subdirectories, as the pipe will only exist on disk for the
            // duration between when the server starts listening and the client connects, after
            // which the pipe will be deleted.
            return s_pipePrefix + pipeName;
        }

        /// <summary>Throws an exception if the supplied handle does not represent a valid pipe.</summary>
        /// <param name="safePipeHandle">The handle to validate.</param>
        internal void ValidateHandleIsPipe(SafePipeHandle safePipeHandle)
        {
            if (safePipeHandle.NamedPipeSocket == null)
            {
                Interop.Sys.FileStatus status;
                int result = CheckPipeCall(Interop.Sys.FStat(safePipeHandle, out status));
                if (result == 0)
                {
                    if ((status.Mode & Interop.Sys.FileTypes.S_IFMT) != Interop.Sys.FileTypes.S_IFIFO &&
                        (status.Mode & Interop.Sys.FileTypes.S_IFMT) != Interop.Sys.FileTypes.S_IFSOCK)
                    {
                        throw new IOException(SR.IO_InvalidPipeHandle);
                    }
                }
            }
        }

        /// <summary>Initializes the handle to be used asynchronously.</summary>
        /// <param name="handle">The handle.</param>
        [SecurityCritical]
        private void InitializeAsyncHandle(SafePipeHandle handle)
        {
            // nop
        }

        private void UninitializeAsyncHandle()
        {
            // nop
        }

        private unsafe int ReadCore(byte[] buffer, int offset, int count)
        {
            DebugAssertReadWriteArgs(buffer, offset, count, _handle);

            // For named pipes, receive on the socket.
            Socket socket = _handle.NamedPipeSocket;
            if (socket != null)
            {
                // For a blocking socket, we could simply use the same Read syscall as is done
                // for reading an anonymous pipe.  However, for a non-blocking socket, Read could
                // end up returning EWOULDBLOCK rather than blocking waiting for data.  Such a case
                // is already handled by Socket.Receive, so we use it here.
                try
                {
                    return socket.Receive(buffer, offset, count, SocketFlags.None);
                }
                catch (SocketException e)
                {
                    throw GetIOExceptionForSocketException(e);
                }
            }

            // For anonymous pipes, read from the file descriptor.
            fixed (byte* bufPtr = buffer)
            {
                int result = CheckPipeCall(Interop.Sys.Read(_handle, bufPtr + offset, count));
                Debug.Assert(result <= count);

                return result;
            }
        }

        private unsafe void WriteCore(byte[] buffer, int offset, int count)
        {
            DebugAssertReadWriteArgs(buffer, offset, count, _handle);

            // For named pipes, send to the socket.
            Socket socket = _handle.NamedPipeSocket;
            if (socket != null)
            {
                // For a blocking socket, we could simply use the same Write syscall as is done
                // for writing to anonymous pipe.  However, for a non-blocking socket, Write could
                // end up returning EWOULDBLOCK rather than blocking waiting for space available.  
                // Such a case is already handled by Socket.Send, so we use it here.
                try
                {
                    while (count > 0)
                    {
                        int bytesWritten = socket.Send(buffer, offset, count, SocketFlags.None);
                        Debug.Assert(bytesWritten <= count);

                        count -= bytesWritten;
                        offset += bytesWritten;
                    }
                }
                catch (SocketException e)
                {
                    throw GetIOExceptionForSocketException(e);
                }
            }

            // For anonymous pipes, write the file descriptor.
            fixed (byte* bufPtr = buffer)
            {
                while (count > 0)
                {
                    int bytesWritten = CheckPipeCall(Interop.Sys.Write(_handle, bufPtr + offset, count));
                    Debug.Assert(bytesWritten <= count);

                    count -= bytesWritten;
                    offset += bytesWritten;
                }
            }
        }

        private async Task<int> ReadAsyncCore(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Debug.Assert(this is NamedPipeClientStream || this is NamedPipeServerStream, $"Expected a named pipe, got a {GetType()}");

            Socket socket = InternalHandle.NamedPipeSocket;

            // If a cancelable token is used, we have a choice: we can either ignore it and use a true async operation
            // with Socket.ReceiveAsync, or we can use a polling loop on a worker thread to block for short intervals
            // and check for cancellation in between.  We do the latter.
            if (cancellationToken.CanBeCanceled)
            {
                await Task.CompletedTask.ForceAsync(); // queue the remainder of the work to avoid blocking the caller
                int timeout = 10000;
                const int MaxTimeoutMicroseconds = 500000;
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (socket.Poll(timeout, SelectMode.SelectRead))
                    {
                        return ReadCore(buffer, offset, count);
                    }
                    timeout = Math.Min(timeout * 2, MaxTimeoutMicroseconds);
                }
            }

            // The token wasn't cancelable, so we can simply use an async receive on the socket.
            try
            {
                return await socket.ReceiveAsync(new ArraySegment<byte>(buffer, offset, count), SocketFlags.None).ConfigureAwait(false);
            }
            catch (SocketException e)
            {
                throw GetIOExceptionForSocketException(e);
            }
        }

        private async Task WriteAsyncCore(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Debug.Assert(this is NamedPipeClientStream || this is NamedPipeServerStream, $"Expected a named pipe, got a {GetType()}");
            try
            {
                while (count > 0)
                {
                    // cancellationToken is (mostly) ignored.  We could institute a polling loop like we do for reads if 
                    // cancellationToken.CanBeCanceled, but that adds costs regardless of whether the operation will be canceled, and 
                    // most writes will complete immediately as they simply store data into the socket's buffer.  The only time we end 
                    // up using it in a meaningful way is if a write completes partially, we'll check it on each individual write operation.
                    cancellationToken.ThrowIfCancellationRequested();

                    int bytesWritten = await _handle.NamedPipeSocket.SendAsync(new ArraySegment<byte>(buffer, offset, count), SocketFlags.None).ConfigureAwait(false);
                    Debug.Assert(bytesWritten <= count);

                    count -= bytesWritten;
                    offset += bytesWritten;
                }
            }
            catch (SocketException e)
            {
                throw GetIOExceptionForSocketException(e);
            }
        }

        private IOException GetIOExceptionForSocketException(SocketException e)
        {
            if (e.SocketErrorCode == SocketError.Shutdown) // EPIPE
            {
                State = PipeState.Broken;
            }
            return new IOException(e.Message, e);
        }

        // Blocks until the other end of the pipe has read in all written buffer.
        [SecurityCritical]
        public void WaitForPipeDrain()
        {
            CheckWriteOperations();
            if (!CanWrite)
            {
                throw Error.GetWriteNotSupported();
            }

            // For named pipes on sockets, we could potentially partially implement this
            // via ioctl and TIOCOUTQ, which provides the number of unsent bytes.  However, 
            // that would require polling, and it wouldn't actually mean that the other
            // end has read all of the data, just that the data has left this end's buffer.
            throw new PlatformNotSupportedException(); 
        }

        // Gets the transmission mode for the pipe.  This is virtual so that subclassing types can 
        // override this in cases where only one mode is legal (such as anonymous pipes)
        public virtual PipeTransmissionMode TransmissionMode
        {
            [SecurityCritical]
            [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Security model of pipes: demand at creation but no subsequent demands")]
            get
            {
                CheckPipePropertyOperations();
                return PipeTransmissionMode.Byte; // Unix pipes are only byte-based, not message-based
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
                return GetPipeBufferSize();
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
                return GetPipeBufferSize();
            }
        }

        public virtual PipeTransmissionMode ReadMode
        {
            [SecurityCritical]
            get
            {
                CheckPipePropertyOperations();
                return PipeTransmissionMode.Byte; // Unix pipes are only byte-based, not message-based
            }
            [SecurityCritical]
            [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Security model of pipes: demand at creation but no subsequent demands")]
            set
            {
                CheckPipePropertyOperations();
                if (value < PipeTransmissionMode.Byte || value > PipeTransmissionMode.Message)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_TransmissionModeByteOrMsg);
                }

                if (value != PipeTransmissionMode.Byte) // Unix pipes are only byte-based, not message-based
                {
                    throw new PlatformNotSupportedException(SR.PlatformNotSupported_MessageTransmissionMode);
                }

                // nop, since it's already the only valid value
            }
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        /// <summary>
        /// We want to ensure that only one asynchronous operation is actually in flight
        /// at a time. The base Stream class ensures this by serializing execution via a 
        /// semaphore.  Since we don't delegate to the base stream for Read/WriteAsync due 
        /// to having specialized support for cancellation, we do the same serialization here.
        /// </summary>
        private SemaphoreSlim _asyncActiveSemaphore;

        private SemaphoreSlim EnsureAsyncActiveSemaphoreInitialized()
        {
            return LazyInitializer.EnsureInitialized(ref _asyncActiveSemaphore, () => new SemaphoreSlim(1, 1));
        }

        private static void CreateDirectory(string directoryPath)
        {
            int result = Interop.Sys.MkDir(directoryPath, (int)Interop.Sys.Permissions.Mask);

            // If successful created, we're done.
            if (result >= 0)
                return;

            // If the directory already exists, consider it a success.
            Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
            if (errorInfo.Error == Interop.Error.EEXIST)
                return;

            // Otherwise, fail.
            throw Interop.GetExceptionForIoErrno(errorInfo, directoryPath, isDirectory: true);
        }

        /// <summary>Creates an anonymous pipe.</summary>
        /// <param name="inheritability">The inheritability to try to use.  This may not always be honored, depending on platform.</param>
        /// <param name="reader">The resulting reader end of the pipe.</param>
        /// <param name="writer">The resulting writer end of the pipe.</param>
        internal static unsafe void CreateAnonymousPipe(
            HandleInheritability inheritability, out SafePipeHandle reader, out SafePipeHandle writer)
        {
            // Allocate the safe handle objects prior to calling pipe/pipe2, in order to help slightly in low-mem situations
            reader = new SafePipeHandle();
            writer = new SafePipeHandle();

            // Create the OS pipe
            int* fds = stackalloc int[2];
            CreateAnonymousPipe(inheritability, fds);

            // Store the file descriptors into our safe handles
            reader.SetHandle(fds[Interop.Sys.ReadEndOfPipe]);
            writer.SetHandle(fds[Interop.Sys.WriteEndOfPipe]);
        }

        internal int CheckPipeCall(int result)
        {
            if (result == -1)
            {
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();

                if (errorInfo.Error == Interop.Error.EPIPE)
                    State = PipeState.Broken;

                throw Interop.GetExceptionForIoErrno(errorInfo);
            }

            return result;
        }

        private int GetPipeBufferSize()
        {
            if (!Interop.Sys.Fcntl.CanGetSetPipeSz)
            {
                throw new PlatformNotSupportedException();
            }

            // If we have a handle, get the capacity of the pipe (there's no distinction between in/out direction).
            // If we don't, just return the buffer size that was passed to the constructor.
            return _handle != null ?
                CheckPipeCall(Interop.Sys.Fcntl.GetPipeSz(_handle)) :
                _outBufferSize;
        }

        internal static unsafe void CreateAnonymousPipe(HandleInheritability inheritability, int* fdsptr)
        {
            var flags = (inheritability & HandleInheritability.Inheritable) == 0 ?
                Interop.Sys.PipeFlags.O_CLOEXEC : 0;
            Interop.CheckIo(Interop.Sys.Pipe(fdsptr, flags));
        }

        internal static void ConfigureSocket(
            Socket s, SafePipeHandle pipeHandle, 
            PipeDirection direction, int inBufferSize, int outBufferSize, HandleInheritability inheritability)
        {
            if (inBufferSize > 0)
            {
                s.ReceiveBufferSize = inBufferSize;
            }

            if (outBufferSize > 0)
            {
                s.SendBufferSize = outBufferSize;
            }

            if (inheritability != HandleInheritability.Inheritable)
            {
                Interop.Sys.Fcntl.SetCloseOnExec(pipeHandle); // ignore failures, best-effort attempt
            }

            switch (direction)
            {
                case PipeDirection.In:
                    s.Shutdown(SocketShutdown.Send);
                    break;
                case PipeDirection.Out:
                    s.Shutdown(SocketShutdown.Receive);
                    break;
            }
        }
    }
}
