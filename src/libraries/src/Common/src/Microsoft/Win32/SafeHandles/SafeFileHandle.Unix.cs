// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    [System.Security.SecurityCritical]
    sealed partial class SafeFileHandle : SafeHandle
    {
        /// <summary>A handle value of -1.</summary>
        private static readonly IntPtr s_invalidHandle = new IntPtr(-1);

        private SafeFileHandle(bool ownsHandle)
            : base(s_invalidHandle, ownsHandle)
        {
        }

        /// <summary>Opens the specified file with the requested flags and mode.</summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="flags">The flags with which to open the file.</param>
        /// <param name="mode">The mode for opening the file.</param>
        /// <returns>A SafeFileHandle for the opened file.</returns>
        internal static SafeFileHandle Open(string path, Interop.Sys.OpenFlags flags, int mode)
        {
            Debug.Assert(path != null);

            // SafeFileHandle wraps a file descriptor rather than a pointer, and a file descriptor is always 4 bytes
            // rather than being pointer sized, which means we can't utilize the runtime's ability to marshal safe handles.
            // Ideally this would be a constrained execution region, but we don't have access to PrepareConstrainedRegions.
            // We still use a finally block to house the code that opens the file and stores the handle in hopes
            // of making it as non-interruptable as possible.  The SafeFileHandle is also allocated first to avoid
            // the allocation after getting the file descriptor but before storing it.
            SafeFileHandle handle = new SafeFileHandle(ownsHandle: true);
            try { } finally
            {
                // If we fail to open the file due to a path not existing, we need to know whether to blame
                // the file itself or its directory.  If we're creating the file, then we blame the directory,
                // otherwise we blame the file.
                bool enoentDueToDirectory = (flags & Interop.Sys.OpenFlags.O_CREAT) != 0;

                // Open the file.
                int fd;
                while (Interop.CheckIo(fd = Interop.Sys.Open(path, flags, mode), path, isDirectory: enoentDueToDirectory,
                    errorRewriter: e => (e.Error == Interop.Error.EISDIR) ? Interop.Error.EACCES.Info() : e)) ;
                Debug.Assert(fd >= 0);
                handle.SetHandle((IntPtr)fd);
                Debug.Assert(!handle.IsInvalid);

                // Make sure it's not a directory; we do this after opening it once we have a file descriptor 
                // to avoid race conditions.
                Interop.Sys.FileStatus status;
                if (Interop.Sys.FStat(fd, out status) != 0)
                {
                    handle.Dispose();
                    throw Interop.GetExceptionForIoErrno(Interop.Sys.GetLastErrorInfo(), path);
                }
                if ((status.Mode & Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFDIR)
                {
                    handle.Dispose();
                    throw Interop.GetExceptionForIoErrno(Interop.Error.EACCES.Info(), path, isDirectory: true);
                }
            }
            return handle;
        }

        [System.Security.SecurityCritical]
        protected override bool ReleaseHandle()
        {
            // Close the handle. Although close is documented to potentially fail with EINTR, we never want
            // to retry, as the descriptor could actually have been closed, been subsequently reassigned, and
            // be in use elsewhere in the process.  Instead, we simply check whether the call was successful.
            int fd = (int)handle;
            Debug.Assert(fd >= 0);
            return Interop.Sys.Close(fd) == 0;
        }

        public override bool IsInvalid
        {
            [System.Security.SecurityCritical]
            get
            {
                long h = (long)handle;
                return h < 0 || h > int.MaxValue;
            }
        }
    }
}
