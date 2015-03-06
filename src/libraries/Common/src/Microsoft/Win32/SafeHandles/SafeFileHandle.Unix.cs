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
            handle = s_invalidHandle; // TODO: remove this once base implementation correctly sets it
        }

        /// <summary>Opens the specified file with the requested flags and mode.</summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="flags">The flags with which to open the file.</param>
        /// <param name="mode">The mode for opening the file.</param>
        /// <returns>A SafeFileHandle for the opened file.</returns>
        internal static SafeFileHandle Open(string path, Interop.libc.OpenFlags flags, int mode)
        {
            // SafeFileHandle wraps a file descriptor rather than a pointer, and a file descriptor is always 4 bytes
            // rather than being pointer sized, which means we can't utilize the runtime's ability to marshal safe handles.
            // Ideally this would be a constrained execution region, but we don't have access to PrepareConstrainedRegions.
            // We still use a finally block to house the code that opens the file and stores the handle in hopes
            // of making it as non-interruptable as possible.  The SafeFileHandle is also allocated first to avoid
            // the allocation after getting the file descriptor but before storing it.
            SafeFileHandle handle = new SafeFileHandle(ownsHandle: true);
            try { } finally
            {
                int fd;
                while (Interop.CheckIo(fd = Interop.libc.open(path, flags, mode))) ;
                Debug.Assert(fd >= 0);
                handle.SetHandle((IntPtr)fd);
            }
            return handle;
        }

        [System.Security.SecurityCritical]
        protected override bool ReleaseHandle()
        {
            // Close the handle. We do not want to throw here nor retry
            // in the case of an EINTR error, so we simply check whether
            // the call was successful or not.
            int fd = (int)handle;
            Debug.Assert(fd >= 0);
            return Interop.libc.close(fd) == 0;
        }

        public override bool IsInvalid
        {
            [System.Security.SecurityCritical]
            get { return (int)handle < 0; }
        }
    }
}
