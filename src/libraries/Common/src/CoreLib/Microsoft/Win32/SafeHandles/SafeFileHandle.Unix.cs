// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    public sealed class SafeFileHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>A handle value of -1.</summary>
        private static readonly IntPtr s_invalidHandle = new IntPtr(-1);

        private SafeFileHandle() : this(ownsHandle: true)
        {
        }

        private SafeFileHandle(bool ownsHandle)
            : base(ownsHandle)
        {
            SetHandle(s_invalidHandle);
        }

        public SafeFileHandle(IntPtr preexistingHandle, bool ownsHandle) : this(ownsHandle)
        {
            SetHandle(preexistingHandle);
        }

        internal bool? IsAsync { get; set; }

        /// <summary>Opens the specified file with the requested flags and mode.</summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="flags">The flags with which to open the file.</param>
        /// <param name="mode">The mode for opening the file.</param>
        /// <returns>A SafeFileHandle for the opened file.</returns>
        internal static SafeFileHandle Open(string path, Interop.Sys.OpenFlags flags, int mode)
        {
            Debug.Assert(path != null);
            SafeFileHandle handle = Interop.Sys.Open(path, flags, mode);

            if (handle.IsInvalid)
            {
                handle.Dispose();
                Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();

                // If we fail to open the file due to a path not existing, we need to know whether to blame
                // the file itself or its directory.  If we're creating the file, then we blame the directory,
                // otherwise we blame the file.
                //
                // When opening, we need to align with Windows, which considers a missing path to be
                // FileNotFound only if the containing directory exists.

                bool isDirectory = (error.Error == Interop.Error.ENOENT) &&
                    ((flags & Interop.Sys.OpenFlags.O_CREAT) != 0
                    || !DirectoryExists(Path.GetDirectoryName(PathInternal.TrimEndingDirectorySeparator(path))));

                Interop.CheckIo(
                    error.Error,
                    path,
                    isDirectory,
                    errorRewriter: e => (e.Error == Interop.Error.EISDIR) ? Interop.Error.EACCES.Info() : e);
            }

            // Make sure it's not a directory; we do this after opening it once we have a file descriptor 
            // to avoid race conditions.
            Interop.Sys.FileStatus status;
            if (Interop.Sys.FStat(handle, out status) != 0)
            {
                handle.Dispose();
                throw Interop.GetExceptionForIoErrno(Interop.Sys.GetLastErrorInfo(), path);
            }
            if ((status.Mode & Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFDIR)
            {
                handle.Dispose();
                throw Interop.GetExceptionForIoErrno(Interop.Error.EACCES.Info(), path, isDirectory: true);
            }

            return handle;
        }

        private static bool DirectoryExists(string fullPath)
        {
            int fileType = Interop.Sys.FileTypes.S_IFDIR;

            Interop.Sys.FileStatus fileinfo;
            Interop.ErrorInfo errorInfo = default(Interop.ErrorInfo);

            // First use stat, as we want to follow symlinks.  If that fails, it could be because the symlink
            // is broken, we don't have permissions, etc., in which case fall back to using LStat to evaluate
            // based on the symlink itself.
            if (Interop.Sys.Stat(fullPath, out fileinfo) < 0 &&
                Interop.Sys.LStat(fullPath, out fileinfo) < 0)
            {
                errorInfo = Interop.Sys.GetLastErrorInfo();
                return false;
            }

            // Something exists at this path.  If the caller is asking for a directory, return true if it's
            // a directory and false for everything else.  If the caller is asking for a file, return false for
            // a directory and true for everything else.
            return
                (fileType == Interop.Sys.FileTypes.S_IFDIR) ==
                ((fileinfo.Mode & Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFDIR);
        }

        /// <summary>Opens a SafeFileHandle for a file descriptor created by a provided delegate.</summary>
        /// <param name="fdFunc">
        /// The function that creates the file descriptor. Returns the file descriptor on success, or an invalid
        /// file descriptor on error with Marshal.GetLastWin32Error() set to the error code.
        /// </param>
        /// <returns>The created SafeFileHandle.</returns>
        internal static SafeFileHandle Open(Func<SafeFileHandle> fdFunc)
        {
            SafeFileHandle handle = Interop.CheckIo(fdFunc());

            Debug.Assert(!handle.IsInvalid, "File descriptor is invalid");
            return handle;
        }

        protected override bool ReleaseHandle()
        {
            // When the SafeFileHandle was opened, we likely issued an flock on the created descriptor in order to add 
            // an advisory lock.  This lock should be removed via closing the file descriptor, but close can be
            // interrupted, and we don't retry closes.  As such, we could end up leaving the file locked,
            // which could prevent subsequent usage of the file until this process dies.  To avoid that, we proactively
            // try to release the lock before we close the handle. (If it's not locked, there's no behavioral
            // problem trying to unlock it.)
            Interop.Sys.FLock(handle, Interop.Sys.LockOperations.LOCK_UN); // ignore any errors

            // Close the descriptor. Although close is documented to potentially fail with EINTR, we never want
            // to retry, as the descriptor could actually have been closed, been subsequently reassigned, and
            // be in use elsewhere in the process.  Instead, we simply check whether the call was successful.
            int result = Interop.Sys.Close(handle);
#if DEBUG
            if (result != 0)
            {
                Debug.Fail(string.Format(
                    "Close failed with result {0} and error {1}", 
                    result, Interop.Sys.GetLastErrorInfo()));
            }
#endif
            return result == 0;
        }

        public override bool IsInvalid
        {
            get
            {
                long h = (long)handle;
                return h < 0 || h > int.MaxValue;
            }
        }
    }
}
