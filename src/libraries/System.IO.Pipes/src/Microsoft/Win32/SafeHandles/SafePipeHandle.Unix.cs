// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafePipeHandle : SafeHandle
    {
        private const int DefaultInvalidHandle = -1;

        /// <summary>Opens the specified file with the requested flags and mode.</summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="flags">The flags with which to open the file.</param>
        /// <param name="mode">The mode for opening the file.</param>
        /// <returns>A SafeFileHandle for the opened file.</returns>
        internal static SafePipeHandle Open(string path, Interop.Sys.OpenFlags flags, int mode)
        {
            // SafePipeHandle wraps a file descriptor rather than a pointer, and a file descriptor is always 4 bytes
            // rather than being pointer sized, which means we can't utilize the runtime's ability to marshal safe handles.
            // Ideally this would be a constrained execution region, but we don't have access to PrepareConstrainedRegions.
            // We still use a finally block to house the code that opens the file and stores the handle in hopes
            // of making it as non-interruptable as possible.  The SafePipeHandle is also allocated first to avoid
            // the allocation after getting the file descriptor but before storing it.
            SafePipeHandle handle = new SafePipeHandle();
            try { }
            finally
            {
                int fd;
                while (Interop.CheckIo(fd = Interop.Sys.Open(path, flags, mode))) ;
                Debug.Assert(fd >= 0);
                handle.SetHandle((IntPtr)fd);
            }
            return handle;
        }

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
            [SecurityCritical]
            get { return (long)handle < 0; }
        }
    }
}
