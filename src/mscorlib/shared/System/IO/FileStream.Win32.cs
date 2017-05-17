// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace System.IO
{
    public partial class FileStream : Stream
    {
        private SafeFileHandle OpenHandle(FileMode mode, FileShare share, FileOptions options)
        {
            Interop.Kernel32.SECURITY_ATTRIBUTES secAttrs = GetSecAttrs(share);

            int fAccess =
                ((_access & FileAccess.Read) == FileAccess.Read ? GENERIC_READ : 0) |
                ((_access & FileAccess.Write) == FileAccess.Write ? GENERIC_WRITE : 0);

            // Our Inheritable bit was stolen from Windows, but should be set in
            // the security attributes class.  Don't leave this bit set.
            share &= ~FileShare.Inheritable;

            // Must use a valid Win32 constant here...
            if (mode == FileMode.Append)
                mode = FileMode.OpenOrCreate;

            int flagsAndAttributes = (int)options;

            // For mitigating local elevation of privilege attack through named pipes
            // make sure we always call CreateFile with SECURITY_ANONYMOUS so that the
            // named pipe server can't impersonate a high privileged client security context
            flagsAndAttributes |= (Interop.Kernel32.SecurityOptions.SECURITY_SQOS_PRESENT | Interop.Kernel32.SecurityOptions.SECURITY_ANONYMOUS);

            // Don't pop up a dialog for reading from an empty floppy drive
            uint oldMode;
            bool success = Interop.Kernel32.SetThreadErrorMode(Interop.Kernel32.SEM_FAILCRITICALERRORS, out oldMode);
            try
            {
                SafeFileHandle fileHandle = Interop.Kernel32.CreateFile(_path, fAccess, share, ref secAttrs, mode, flagsAndAttributes, IntPtr.Zero);
                fileHandle.IsAsync = _useAsyncIO;

                if (fileHandle.IsInvalid)
                {
                    // Return a meaningful exception with the full path.

                    // NT5 oddity - when trying to open "C:\" as a Win32FileStream,
                    // we usually get ERROR_PATH_NOT_FOUND from the OS.  We should
                    // probably be consistent w/ every other directory.
                    int errorCode = Marshal.GetLastWin32Error();

                    if (errorCode == Interop.Errors.ERROR_PATH_NOT_FOUND && _path.Length == PathInternal.GetRootLength(_path))
                        errorCode = Interop.Errors.ERROR_ACCESS_DENIED;

                    throw Win32Marshal.GetExceptionForWin32Error(errorCode, _path);
                }
                
                int fileType = Interop.Kernel32.GetFileType(fileHandle);
                if (fileType != Interop.Kernel32.FileTypes.FILE_TYPE_DISK)
                {
                    fileHandle.Dispose();
                    throw new NotSupportedException(SR.NotSupported_FileStreamOnNonFiles);
                }

                return fileHandle;
            }
            finally
            {
                if (success)
                    Interop.Kernel32.SetThreadErrorMode(oldMode, out oldMode);
            }
        }
    }
}
