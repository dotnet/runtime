// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    internal static class SafeFileHandleHelper
    {
        /// <summary>Opens the specified file with the requested flags and mode.</summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="flags">The flags with which to open the file.</param>
        /// <param name="mode">The mode for opening the file.</param>
        /// <returns>A SafeFileHandle for the opened file.</returns>
        internal static SafeFileHandle Open(string path, Interop.Sys.OpenFlags flags, int mode)
        {
            Debug.Assert(path != null);

            // If we fail to open the file due to a path not existing, we need to know whether to blame
            // the file itself or its directory.  If we're creating the file, then we blame the directory,
            // otherwise we blame the file.
            bool enoentDueToDirectory = (flags & Interop.Sys.OpenFlags.O_CREAT) != 0;

            // Open the file. 
            SafeFileHandle handle = Interop.CheckIo(
                Interop.Sys.Open(path, flags, mode),
                path, 
                isDirectory: enoentDueToDirectory,
                errorRewriter: e => (e.Error == Interop.Error.EISDIR) ? Interop.Error.EACCES.Info() : e);

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
    }
}
