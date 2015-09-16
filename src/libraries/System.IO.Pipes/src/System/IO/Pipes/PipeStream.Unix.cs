// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
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

        internal static string GetPipePath(string serverName, string pipeName)
        {
            if (serverName != "." && serverName != Interop.libc.gethostname())
            {
                // Cross-machine pipes are not supported.
                throw new PlatformNotSupportedException();
            }

            if (pipeName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                // Since pipes are stored as files in the file system, we don't support
                // pipe names that are actually paths or that otherwise have invalid
                // filename characters in them.
                throw new PlatformNotSupportedException();
            }

            // Return the pipe path
            return Path.Combine(EnsurePipeDirectoryPath(), pipeName);
        }

        /// <summary>Throws an exception if the supplied handle does not represent a valid pipe.</summary>
        /// <param name="safePipeHandle">The handle to validate.</param>
        internal void ValidateHandleIsPipe(SafePipeHandle safePipeHandle)
        {
            SysCall(safePipeHandle, (fd, _, __, ___) =>
            {
                Interop.Sys.FileStatus status;
                int result = Interop.Sys.FStat(fd, out status);
                if (result == 0)
                {
                    if ((status.Mode & Interop.Sys.FileTypes.S_IFMT) != Interop.Sys.FileTypes.S_IFIFO)
                    {
                        throw new IOException(SR.IO_InvalidPipeHandle);
                    }
                }
                return result;
            });
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

        private int ReadCore(byte[] buffer, int offset, int count)
        {
            return ReadCoreNoCancellation(buffer, offset, count);
        }

        private void WriteCore(byte[] buffer, int offset, int count)
        {
            WriteCoreNoCancellation(buffer, offset, count);
        }

        private async Task<int> ReadAsyncCore(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            SemaphoreSlim activeAsync = EnsureAsyncActiveSemaphoreInitialized();
            await activeAsync.WaitAsync(cancellationToken).ForceAsync();
            try
            {
                return cancellationToken.CanBeCanceled ?
                    ReadCoreWithCancellation(buffer, offset, count, cancellationToken) :
                    ReadCoreNoCancellation(buffer, offset, count);
            }
            finally
            {
                activeAsync.Release();
            }
        }

        private async Task WriteAsyncCore(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            SemaphoreSlim activeAsync = EnsureAsyncActiveSemaphoreInitialized();
            await activeAsync.WaitAsync(cancellationToken).ForceAsync();
            try
            {
                if (cancellationToken.CanBeCanceled)
                    WriteCoreWithCancellation(buffer, offset, count, cancellationToken);
                else
                    WriteCoreNoCancellation(buffer, offset, count);
            }
            finally
            {
                activeAsync.Release();
            }
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

            throw new PlatformNotSupportedException(); // no mechanism for this on Unix
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
                    throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_TransmissionModeByteOrMsg);
                }

                if (value != PipeTransmissionMode.Byte) // Unix pipes are only byte-based, not message-based
                {
                    throw new PlatformNotSupportedException();
                }

                // nop, since it's already the only valid value
            }
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        private static string s_pipeDirectoryPath;

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

        private static string EnsurePipeDirectoryPath()
        {
            const string PipesFeatureName = "pipes";

            // Ideally this would simply use PersistedFiles.GetTempFeatureDirectory(PipesFeatureName) and then
            // Directory.CreateDirectory to ensure it exists.  But this assembly doesn't reference System.IO.FileSystem.
            // As such, we'd be calling GetTempFeatureDirectory, only to then need to parse it in order
            // to create each of the individual directories as part of the path.  We instead access the named portions 
            // of the path directly and do the building of the path and directory structure manually.

            // First ensure we know what the full path should be, e.g. /tmp/.dotnet/corefx/pipes/
            string fullPath = s_pipeDirectoryPath;
            string tempPath = null;
            if (fullPath == null)
            {
                tempPath = Path.GetTempPath();
                fullPath = Path.Combine(tempPath, PersistedFiles.TopLevelHiddenDirectory, PersistedFiles.SecondLevelDirectory, PipesFeatureName);
                s_pipeDirectoryPath = fullPath;
            }

            // Then create the directory if it doesn't already exist.  If we get any error back from stat,
            // just proceed to build up the directory, failing in the CreateDirectory calls if there's some
            // problem.  Similarly, it's possible stat succeeds but the path is a file rather than directory; we'll
            // call that success for now and let this fail later when the code tries to create a file in this "directory"
            // (we don't want to overwrite/delete whatever that unknown file may be, and this is similar to other cases
            // we can't control where the file system is manipulated concurrently with and separately from this code).
            Interop.Sys.FileStatus ignored;
            bool pathExists = Interop.Sys.Stat(fullPath, out ignored) == 0;
            if (!pathExists)
            {
                // We need to build up the directory manually.  Ensure we have the temp directory in which
                // we'll create the structure, e.g. /tmp/
                if (tempPath == null)
                {
                    tempPath = Path.GetTempPath();
                }
                Debug.Assert(Interop.Sys.Stat(tempPath, out ignored) == 0, "Path.GetTempPath() directory could not be accessed");

                // Create /tmp/.dotnet/ if it doesn't exist.
                string partialPath = Path.Combine(tempPath, PersistedFiles.TopLevelHiddenDirectory);
                CreateDirectory(partialPath);

                // Create /tmp/.dotnet/corefx/ if it doesn't exist
                partialPath = Path.Combine(partialPath, PersistedFiles.SecondLevelDirectory);
                CreateDirectory(partialPath);

                // Create /tmp/.dotnet/corefx/pipes/ if it doesn't exist
                CreateDirectory(fullPath);
            }

            return fullPath;
        }

        private static void CreateDirectory(string directoryPath)
        {
            while (true)
            {
                int result = Interop.Sys.MkDir(directoryPath, (int)Interop.Sys.Permissions.S_IRWXU);

                // If successful created, we're done.
                if (result >= 0)
                    return;

                // If the directory already exists, consider it a success.
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                if (errorInfo.Error == Interop.Error.EEXIST)
                    return;

                // If the I/O was interrupted, try again.
                if (errorInfo.Error == Interop.Error.EINTR)
                    continue;

                // Otherwise, fail.
                throw Interop.GetExceptionForIoErrno(errorInfo, directoryPath, isDirectory: true);
            }
        }

        internal static Interop.Sys.OpenFlags TranslateFlags(PipeDirection direction, PipeOptions options, HandleInheritability inheritability)
        {
            // Translate direction
            Interop.Sys.OpenFlags flags =
                direction == PipeDirection.InOut ? Interop.Sys.OpenFlags.O_RDWR :
                direction == PipeDirection.Out ? Interop.Sys.OpenFlags.O_WRONLY :
                Interop.Sys.OpenFlags.O_RDONLY;

            // Translate options
            if ((options & PipeOptions.WriteThrough) != 0)
            {
                flags |= Interop.Sys.OpenFlags.O_SYNC;
            }

            // Translate inheritability.
            if ((inheritability & HandleInheritability.Inheritable) == 0)
            {
                flags |= Interop.Sys.OpenFlags.O_CLOEXEC;
            }
            
            // PipeOptions.Asynchronous is ignored, at least for now.  Asynchronous processing
            // is handling just by queueing a work item to do the work synchronously on a pool thread.

            return flags;
        }

        private unsafe int ReadCoreNoCancellation(byte[] buffer, int offset, int count)
        {
            DebugAssertReadWriteArgs(buffer, offset, count, _handle);
            fixed (byte* bufPtr = buffer)
            {
                return (int)SysCall(_handle, (fd, ptr, len, _) =>
                {
                    int result = Interop.Sys.Read(fd, (byte*)ptr, len);
                    Debug.Assert(result <= len);
                    return result;
                }, (IntPtr)(bufPtr + offset), count);
            }
        }

        private unsafe int ReadCoreWithCancellation(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            DebugAssertReadWriteArgs(buffer, offset, count, _handle);
            Debug.Assert(cancellationToken.CanBeCanceled, "ReadCoreNoCancellation should be used if cancellation can't happen");

            // Register for a cancellation request.  This will throw if cancellation has already been requested,
            // and otherwise will write to the cancellation pipe if/when cancellation has been requested.
            using (DescriptorCancellationRegistration cancellation = RegisterForCancellation(cancellationToken))
            {
                bool gotRef = false;
                try
                {
                    cancellation.Poll.DangerousAddRef(ref gotRef);
                    fixed (byte* bufPtr = buffer)
                    {
                        const int CancellationSentinel = -42;
                        int rv = (int)SysCall(_handle, (fd, ptr, len, cancellationFd) =>
                        {
                            // We want to wait for data to be available on either the pipe we want to read from
                            // or on the cancellation pipe, which would signal a cancellation request.
                            Interop.Sys.PollFD* fds = stackalloc Interop.Sys.PollFD[2];
                            fds[0] = new Interop.Sys.PollFD { FD = fd, Events = Interop.Sys.PollFlags.POLLIN, REvents = 0 };
                            fds[1] = new Interop.Sys.PollFD { FD = (int)cancellationFd, Events = Interop.Sys.PollFlags.POLLIN, REvents = 0 };

                            // Some systems (at least OS X) appear to have a race condition in poll with FIFOs where the poll can 
                            // end up not noticing writes of greater than the internal buffer size.  Restarting the poll causes it 
                            // to notice. To deal with that, we loop around poll, first starting with a small timeout and backing off
                            // to a larger one.  This ensures we'll at least eventually notice such changes in these corner
                            // cases, while not adding too much overhead on systems that don't suffer from the problem.
                            const int InitialMsTimeout = 30, MaxMsTimeout = 2000;
                            for (int timeout = InitialMsTimeout; ; timeout = Math.Min(timeout * 2, MaxMsTimeout))
                            {
                                // Do the poll.
                                int signaledFdCount;
                                while (Interop.CheckIo(signaledFdCount = Interop.Sys.Poll(fds, 2, timeout))) ;
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    // Cancellation occurred.  Bail by returning the cancellation sentinel.
                                    return CancellationSentinel;
                                }
                                else if (signaledFdCount == 0)
                                {
                                    // Timeout occurred.  Loop around to poll again.
                                    continue;
                                }
                                else
                                {
                                    // Our pipe is ready.  Break out of the loop to read from it.
                                    Debug.Assert((fds[0].REvents & Interop.Sys.PollFlags.POLLIN) != 0, "Expected revents on read fd to have POLLIN set");
                                    break;
                                }
                            }

                            // Read it.
                            Debug.Assert((fds[0].REvents & Interop.Sys.PollFlags.POLLIN) != 0);
                            int result = Interop.Sys.Read(fd, (byte*)ptr, len);
                            Debug.Assert(result <= len);
                            return result;
                        }, (IntPtr)(bufPtr + offset), count, cancellation.Poll.DangerousGetHandle());
                        Debug.Assert(rv >= 0 || rv == CancellationSentinel);

                        // If cancellation was requested, waking up the read, throw.
                        if (rv == CancellationSentinel)
                        {
                            Debug.Assert(cancellationToken.IsCancellationRequested);
                            throw new OperationCanceledException(cancellationToken);
                        }

                        // Otherwise return what we read.
                        return rv;
                    }
                }
                finally
                {
                    if (gotRef)
                        cancellation.Poll.DangerousRelease();
                }
            }
        }

        private unsafe void WriteCoreNoCancellation(byte[] buffer, int offset, int count)
        {
            DebugAssertReadWriteArgs(buffer, offset, count, _handle);

            fixed (byte* bufPtr = buffer)
            {
                while (count > 0)
                {
                    int bytesWritten = (int)SysCall(_handle, (fd, ptr, len, _) =>
                    {
                        int result = Interop.Sys.Write(fd, (byte*)ptr, len);
                        Debug.Assert(result <= len);
                        return result;
                    }, (IntPtr)(bufPtr + offset), count);
                    count -= bytesWritten;
                    offset += bytesWritten;
                }
            }
        }

        private void WriteCoreWithCancellation(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            DebugAssertReadWriteArgs(buffer, offset, count, _handle);

            // NOTE:
            // We currently ignore cancellationToken.  Unlike on Windows, writes to pipes on Unix are likely to succeed
            // immediately, even if no reader is currently listening, as long as there's room in the kernel buffer.
            // However it's still possible for write to block if the buffer is full.  We could try using a poll mechanism
            // like we do for read, but checking for POLLOUT is only going to tell us that there's room to write at least
            // one byte to the pipe, not enough room to write enough that we won't block.  The only way to guarantee
            // that would seem to be writing one byte at a time, which has huge overheads when writing large chunks of data.
            // Given all this, at least for now we just do an initial check for cancellation and then call to the 
            // non -cancelable version.

            cancellationToken.ThrowIfCancellationRequested();
            WriteCoreNoCancellation(buffer, offset, count);
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

        /// <summary>
        /// Creates a cancellation registration.  This creates a pipe that'll have one end written to
        /// when cancellation is requested.  The other end can be poll'd to see when cancellation has occurred.
        /// </summary>
        private static unsafe DescriptorCancellationRegistration RegisterForCancellation(CancellationToken cancellationToken)
        {
            Debug.Assert(cancellationToken.CanBeCanceled);

            // Fast path: before doing any real work, throw if cancellation has already been requested
            cancellationToken.ThrowIfCancellationRequested();

            // Create a pipe we can use to send a signal to the reader/writer
            // to wake it up if blocked.
            SafePipeHandle poll, send;
            CreateAnonymousPipe(HandleInheritability.None, out poll, out send);

            // Register a cancellation callback to send a byte to the cancellation pipe
            CancellationTokenRegistration reg = cancellationToken.Register(s =>
            {
                SafePipeHandle sendRef = (SafePipeHandle)s;
                bool gotSendRef = false;
                try
                {
                    sendRef.DangerousAddRef(ref gotSendRef);
                    int fd = (int)sendRef.DangerousGetHandle();
                    byte b = 1;
                    while (Interop.CheckIo((int)Interop.Sys.Write(fd, &b, 1))) ;
                }
                finally
                {
                    if (gotSendRef)
                        sendRef.DangerousRelease();
                }
            }, send);

            // Return a disposable struct that will unregister the cancellation registration
            // and dispose of the pipe.
            return new DescriptorCancellationRegistration(reg, poll, send);
        }

        /// <summary>Disposable struct that'll clean up the results of a RegisterForCancellation operation.</summary>
        private struct DescriptorCancellationRegistration : IDisposable
        {
            private CancellationTokenRegistration _registration;
            private readonly SafePipeHandle _poll;
            private readonly SafePipeHandle _send;

            internal DescriptorCancellationRegistration(
                CancellationTokenRegistration registration,
                SafePipeHandle poll, SafePipeHandle send)
            {
                Debug.Assert(poll != null);
                Debug.Assert(send != null);

                _registration = registration;
                _poll = poll;
                _send = send;
            }

            internal SafePipeHandle Poll { get { return _poll; } }

            public void Dispose()
            {
                // Dispose the registration prior to disposing of the pipe handles.
                // Otherwise a concurrent cancellation request could try to use
                // the already disposed pipe.
                _registration.Dispose();

                if (_send != null)
                    _send.Dispose();
                if (_poll != null)
                    _poll.Dispose();
            }
        }

        /// <summary>
        /// Helper for making system calls that involve the stream's file descriptor.
        /// System calls are expected to return greather than or equal to zero on success,
        /// and less than zero on failure.  In the case of failure, errno is expected to
        /// be set to the relevant error code.
        /// </summary>
        /// <param name="sysCall">A delegate that invokes the system call.</param>
        /// <param name="arg1">The first argument to be passed to the system call, after the file descriptor.</param>
        /// <param name="arg2">The second argument to be passed to the system call.</param>
        /// <param name="arg3">The third argument to be passed to the system call.</param>
        /// <returns>The return value of the system call.</returns>
        /// <remarks>
        /// Arguments are expected to be passed via <paramref name="arg1"/> and <paramref name="arg2"/>
        /// so as to avoid delegate and closure allocations at the call sites.
        /// </remarks>
        private long SysCall(
            SafePipeHandle handle,
            Func<int, IntPtr, int, IntPtr, long> sysCall,
            IntPtr arg1 = default(IntPtr), int arg2 = default(int), IntPtr arg3 = default(IntPtr))
        {
            bool gotRefOnHandle = false;
            try
            {
                // Get the file descriptor from the handle.  We increment the ref count to help
                // ensure it's not closed out from under us.
                handle.DangerousAddRef(ref gotRefOnHandle);
                Debug.Assert(gotRefOnHandle);
                int fd = (int)handle.DangerousGetHandle();
                Debug.Assert(fd >= 0);

                while (true)
                {
                    long result = sysCall(fd, arg1, arg2, arg3);
                    if (result == -1)
                    {
                        Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();

                        if (errorInfo.Error == Interop.Error.EINTR)
                            continue;

                        if (errorInfo.Error == Interop.Error.EPIPE)
                            State = PipeState.Broken;

                        throw Interop.GetExceptionForIoErrno(errorInfo);
                    }
                    return result;
                }
            }
            finally
            {
                if (gotRefOnHandle)
                {
                    handle.DangerousRelease();
                }
            }
        }

        internal void InitializeBufferSize(SafePipeHandle handle, int bufferSize)
        {
            // bufferSize is just advisory and ignored if platform does not support setting pipe capacity via fcntl.
            if (bufferSize > 0 && Interop.Sys.Fcntl.CanGetSetPipeSz)
            {
                SysCall(handle, (fd, _, size, __) => Interop.Sys.Fcntl.SetPipeSz(fd, size), 
                    IntPtr.Zero, bufferSize);
            }
        }

        private int GetPipeBufferSize()
        {
            if (!Interop.Sys.Fcntl.CanGetSetPipeSz)
            {
                throw new PlatformNotSupportedException();
            }

            // If we have a handle, get the capacity of the pipe (there's no distinction between in/out direction).
            // If we don't, the pipe has been created but not yet connected (in the case of named pipes),
            // so just return the buffer size that was passed to the constructor.
            return _handle != null ?
                (int)SysCall(_handle, (fd, _, __, ___) => Interop.Sys.Fcntl.GetPipeSz(fd)) :
                _outBufferSize;
        }

        internal static unsafe void CreateAnonymousPipe(HandleInheritability inheritability, int* fdsptr)
        {
            var flags = (inheritability & HandleInheritability.Inheritable) == 0 ?
                Interop.Sys.PipeFlags.O_CLOEXEC : 0;
            while (Interop.CheckIo(Interop.Sys.Pipe(fdsptr, flags))) ;
        }
    }
}
