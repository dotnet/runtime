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
            SysCall(safePipeHandle, (fd, _, __) =>
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

        [SecurityCritical]
        private unsafe int ReadCore(byte[] buffer, int offset, int count)
        {
            Debug.Assert(_handle != null, "_handle is null");
            Debug.Assert(!_handle.IsClosed, "_handle is closed");
            Debug.Assert(CanRead, "can't read");
            Debug.Assert(buffer != null, "buffer is null");
            Debug.Assert(offset >= 0, "offset is negative");
            Debug.Assert(count >= 0, "count is negative");

            fixed (byte* bufPtr = buffer)
            {
                return (int)SysCall(_handle, (fd, ptr, len) =>
                {
                    long result = (long)Interop.libc.read(fd, (byte*)ptr, (IntPtr)len);
                    Debug.Assert(result <= len);
                    return result;
                }, (IntPtr)(bufPtr + offset), count);
            }
        }

        [SecuritySafeCritical]
        private Task<int> ReadAsyncCore(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // Delegate to the base Stream's ReadAsync, which will just invoke Read asynchronously.
            return base.ReadAsync(buffer, offset, count, cancellationToken);
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

            fixed (byte* bufPtr = buffer)
            {
                while (count > 0)
                {
                    int bytesWritten = (int)SysCall(_handle, (fd, ptr, len) =>
                    {
                        long result = (long)Interop.libc.write(fd, (byte*)ptr, (IntPtr)len);
                        Debug.Assert(result <= len);
                        return result;
                    }, (IntPtr)(bufPtr + offset), count);
                    count -= bytesWritten;
                    offset += bytesWritten;
                }
            }
        }

        [SecuritySafeCritical]
        private Task WriteAsyncCore(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // Delegate to the base Stream's WriteAsync, which will just invoke Write asynchronously.
            return base.WriteAsync(buffer, offset, count, cancellationToken);
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
                return InBufferSizeCore;
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
                return OutBufferSizeCore;
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
                int result = Interop.libc.mkdir(directoryPath, (int)Interop.libc.Permissions.S_IRWXU);

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

        /// <summary>
        /// Helper for making system calls that involve the stream's file descriptor.
        /// System calls are expected to return greather than or equal to zero on success,
        /// and less than zero on failure.  In the case of failure, errno is expected to
        /// be set to the relevant error code.
        /// </summary>
        /// <param name="sysCall">A delegate that invokes the system call.</param>
        /// <param name="arg1">The first argument to be passed to the system call, after the file descriptor.</param>
        /// <param name="arg2">The second argument to be passed to the system call.</param>
        /// <returns>The return value of the system call.</returns>
        /// <remarks>
        /// Arguments are expected to be passed via <paramref name="arg1"/> and <paramref name="arg2"/>
        /// so as to avoid delegate and closure allocations at the call sites.
        /// </remarks>
        private long SysCall(
            SafePipeHandle handle,
            Func<int, IntPtr, int, long> sysCall,
            IntPtr arg1 = default(IntPtr), int arg2 = default(int))
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
                    long result = sysCall(fd, arg1, arg2);
                    if (result < 0)
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

    }
}
