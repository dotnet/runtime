// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

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

        /// <summary>Characters that can't be used in an absolute path pipe's name.</summary>
        private static readonly char[] s_invalidPathNameChars = Path.GetInvalidPathChars();

        /// <summary>Prefix to prepend to all pipe names.</summary>
        private static readonly string s_pipePrefix = Path.Combine(Path.GetTempPath(), "CoreFxPipe_");

        public override int Read(byte[] buffer, int offset, int count)
        {
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

            if (count == 0)
            {
                UpdateMessageCompletion(false);
                return Task.FromResult(0);
            }

            return ReadAsyncCore(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
        {
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
            => TaskToAsyncResult.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), callback, state);

        public override int EndRead(IAsyncResult asyncResult)
            => TaskToAsyncResult.End<int>(asyncResult);

        public override void Write(byte[] buffer, int offset, int count)
        {
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

            if (count == 0)
            {
                return Task.CompletedTask;
            }

            return WriteAsyncCore(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
        {
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

            return new ValueTask(WriteAsyncCore(buffer, cancellationToken));
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
            => TaskToAsyncResult.Begin(WriteAsync(buffer, offset, count, CancellationToken.None), callback, state);

        public override void EndWrite(IAsyncResult asyncResult)
            => TaskToAsyncResult.End(asyncResult);

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

            // Since pipes are stored as files in the system we support either an absolute path to a file name
            // or a file name. The support of absolute path was added to allow working around the limited
            // length available for the pipe name when concatenated with the temp path, while being
            // cross-platform with Windows (which has only '\' as an invalid char).
            if (Path.IsPathRooted(pipeName))
            {
                if (pipeName.AsSpan().ContainsAny(s_invalidPathNameChars) || pipeName.EndsWith(Path.DirectorySeparatorChar))
                    throw new PlatformNotSupportedException(SR.PlatformNotSupported_InvalidPipeNameChars);

                // Caller is in full control of file location.
                return pipeName;
            }

            if (pipeName.AsSpan().ContainsAny(s_invalidFileNameChars))
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_InvalidPipeNameChars);
            }

            // The pipe is created directly under Path.GetTempPath() with "CoreFXPipe_" prefix.
            //
            // We previously didn't put it into a subdirectory because it only existed on disk for the duration
            // between when the server started listening in WaitForConnection and when the client
            // connected, after which the pipe was deleted.  We now create the pipe when the
            // server stream is created, which leaves it on disk longer, but we can't change the
            // naming scheme used as that breaks the ability for code running on an older
            // runtime to connect to code running on the newer runtime.  That means we're stuck
            // with a tmp file for the lifetime of the server stream.
            return s_pipePrefix + pipeName;
        }

#pragma warning disable CA1822
        /// <summary>Throws an exception if the supplied handle does not represent a valid pipe.</summary>
        /// <param name="safePipeHandle">The handle to validate.</param>
        internal void ValidateHandleIsPipe(SafePipeHandle safePipeHandle)
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
#pragma warning restore CA1822

        /// <summary>Initializes the handle to be used asynchronously.</summary>
        /// <param name="handle">The handle.</param>
        partial void InitializeAsyncHandle(SafePipeHandle handle);

        internal virtual void DisposeCore(bool disposing)
        {
            // nop
        }

        private unsafe int ReadCore(Span<byte> buffer)
        {
            Debug.Assert(_handle != null);
            DebugAssertHandleValid(_handle);

            if (buffer.Length == 0)
            {
                return 0;
            }

            // For a blocking socket, we could simply use the same Read syscall as is done
            // for reading an anonymous pipe.  However, for a non-blocking socket, Read could
            // end up returning EWOULDBLOCK rather than blocking waiting for data.  Such a case
            // is already handled by Socket.Receive, so we use it here.
            try
            {
                return _handle!.PipeSocket.Receive(buffer, SocketFlags.None);
            }
            catch (SocketException e)
            {
                throw GetIOExceptionForSocketException(e);
            }
        }

        private unsafe void WriteCore(ReadOnlySpan<byte> buffer)
        {
            Debug.Assert(_handle != null);
            DebugAssertHandleValid(_handle);

            // For a blocking socket, we could simply use the same Write syscall as is done
            // for writing to anonymous pipe.  However, for a non-blocking socket, Write could
            // end up returning EWOULDBLOCK rather than blocking waiting for space available.
            // Such a case is already handled by Socket.Send, so we use it here.
            try
            {
                while (buffer.Length > 0)
                {
                    int bytesWritten = _handle!.PipeSocket.Send(buffer, SocketFlags.None);
                    buffer = buffer.Slice(bytesWritten);
                }
            }
            catch (SocketException e)
            {
                throw GetIOExceptionForSocketException(e);
            }
        }

        private async ValueTask<int> ReadAsyncCore(Memory<byte> destination, CancellationToken cancellationToken)
        {
            try
            {
                return await InternalHandle!.PipeSocket.ReceiveAsync(destination, SocketFlags.None, cancellationToken).ConfigureAwait(false);
            }
            catch (SocketException e)
            {
                throw GetIOExceptionForSocketException(e);
            }
        }

        private async Task WriteAsyncCore(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
        {
            try
            {
                while (source.Length > 0)
                {
                    int bytesWritten = await _handle!.PipeSocket.SendAsync(source, SocketFlags.None, cancellationToken).ConfigureAwait(false);
                    Debug.Assert(bytesWritten > 0 && bytesWritten <= source.Length);
                    source = source.Slice(bytesWritten);
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
        [SupportedOSPlatform("windows")]
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
            throw new PlatformNotSupportedException(); // not fully implementable on unix
        }

        // Gets the transmission mode for the pipe.  This is virtual so that subclassing types can
        // override this in cases where only one mode is legal (such as anonymous pipes)
        public virtual PipeTransmissionMode TransmissionMode
        {
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
            get
            {
                CheckPipePropertyOperations();
                return PipeTransmissionMode.Byte; // Unix pipes are only byte-based, not message-based
            }
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

        /// <summary>
        /// We want to ensure that only one asynchronous operation is actually in flight
        /// at a time. The base Stream class ensures this by serializing execution via a
        /// semaphore.  Since we don't delegate to the base stream for Read/WriteAsync due
        /// to having specialized support for cancellation, we do the same serialization here.
        /// </summary>
        private SemaphoreSlim? _asyncActiveSemaphore;

        private SemaphoreSlim EnsureAsyncActiveSemaphoreInitialized()
        {
            return LazyInitializer.EnsureInitialized(ref _asyncActiveSemaphore, () => new SemaphoreSlim(1, 1));
        }

        /// <summary>Creates an anonymous pipe.</summary>
        /// <param name="reader">The resulting reader end of the pipe.</param>
        /// <param name="writer">The resulting writer end of the pipe.</param>
        internal static unsafe void CreateAnonymousPipe(out SafePipeHandle reader, out SafePipeHandle writer)
        {
            // Allocate the safe handle objects prior to calling pipe/pipe2, in order to help slightly in low-mem situations
            reader = new SafePipeHandle();
            writer = new SafePipeHandle();

            // Create the OS pipe.  We always create it as O_CLOEXEC (trying to do so atomically) so that the
            // file descriptors aren't inherited.  Then if inheritability was requested, we opt-in the child file
            // descriptor later; if the server file descriptor was also inherited, closing the server file
            // descriptor would fail to signal EOF for the child side.
            int* fds = stackalloc int[2];
            Interop.CheckIo(Interop.Sys.Pipe(fds, Interop.Sys.PipeFlags.O_CLOEXEC));

            // Store the file descriptors into our safe handles
            reader.SetHandle(new IntPtr(fds[Interop.Sys.ReadEndOfPipe]));
            writer.SetHandle(new IntPtr(fds[Interop.Sys.WriteEndOfPipe]));
        }

        private int CheckPipeCall(int result)
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
                throw new PlatformNotSupportedException(); // OS does not support getting pipe size
            }

            // If we have a handle, get the capacity of the pipe (there's no distinction between in/out direction).
            // If we don't, just return the buffer size that was passed to the constructor.
            return _handle != null ?
                CheckPipeCall(Interop.Sys.Fcntl.GetPipeSz(_handle)) :
                (int)_outBufferSize;
        }

        internal static void ConfigureSocket(
            Socket s, SafePipeHandle _,
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

            // Sockets are created with O_CLOEXEC.  If inheritability has been requested, we need to unset the flag.
            if (inheritability == HandleInheritability.Inheritable &&
                Interop.Sys.Fcntl.SetFD(s.SafeHandle, 0) == -1)
            {
                throw Interop.GetExceptionForIoErrno(Interop.Sys.GetLastErrorInfo());
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

        internal static Exception CreateExceptionForLastError(string? pipeName = null)
        {
            Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
            return error.Error == Interop.Error.ENOTSUP ?
                new PlatformNotSupportedException(SR.Format(SR.PlatformNotSupported_OperatingSystemError, nameof(Interop.Error.ENOTSUP))) :
                Interop.GetExceptionForIoErrno(error, pipeName);
        }
    }
}
