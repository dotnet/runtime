// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.IO
{
    public partial class FileStream : Stream
    {
        private unsafe SafeFileHandle OpenHandle(FileMode mode, FileShare share, FileOptions options)
        {
            Interop.Kernel32.SECURITY_ATTRIBUTES secAttrs = GetSecAttrs(share);

            int access =
                ((_access & FileAccess.Read) == FileAccess.Read ? GENERIC_READ : 0) |
                ((_access & FileAccess.Write) == FileAccess.Write ? GENERIC_WRITE : 0);

            // Our Inheritable bit was stolen from Windows, but should be set in
            // the security attributes class.  Don't leave this bit set.
            share &= ~FileShare.Inheritable;

            // Must use a valid Win32 constant here...
            if (mode == FileMode.Append)
                mode = FileMode.OpenOrCreate;

            Interop.Kernel32.CREATEFILE2_EXTENDED_PARAMETERS parameters = new Interop.Kernel32.CREATEFILE2_EXTENDED_PARAMETERS();
            parameters.dwSize = (uint)sizeof(Interop.Kernel32.CREATEFILE2_EXTENDED_PARAMETERS);
            parameters.dwFileFlags = (uint)options;
            parameters.lpSecurityAttributes = &secAttrs;

            using (DisableMediaInsertionPrompt.Create())
            {
                Debug.Assert(_path != null);
                return ValidateFileHandle(Interop.Kernel32.CreateFile2(
                    lpFileName: _path,
                    dwDesiredAccess: access,
                    dwShareMode: share,
                    dwCreationDisposition: mode,
                    pCreateExParams: ref parameters));
            }
        }

        private static bool GetDefaultIsAsync(SafeFileHandle handle) => handle.IsAsync ?? DefaultIsAsync;

        private static unsafe bool? IsHandleSynchronous(SafeFileHandle handle, FileAccess access)
        {
            // Do NOT use this method on any type other than DISK. Reading or writing to a pipe may
            // cause an app to block incorrectly, introducing a deadlock (depending on whether a write
            // will wake up an already-blocked thread or this Win32FileStream's thread).

            byte* bytes = stackalloc byte[1];
            int numBytesReadWritten;
            int r = -1;

            // If the handle is a pipe, ReadFile will block until there
            // has been a write on the other end.  We'll just have to deal with it,
            // For the read end of a pipe, you can mess up and 
            // accidentally read synchronously from an async pipe.
            if ((access & FileAccess.Read) != 0)
            {
                r = Interop.Kernel32.ReadFile(handle, bytes, 0, out numBytesReadWritten, IntPtr.Zero);
            }
            else if ((access & FileAccess.Write) != 0)
            {
                r = Interop.Kernel32.WriteFile(handle, bytes, 0, out numBytesReadWritten, IntPtr.Zero);
            }

            if (r == 0)
            {
                int errorCode = Marshal.GetLastWin32Error();
                switch (errorCode)
                {
                    case Interop.Errors.ERROR_INVALID_PARAMETER:
                        return false;
                    case Interop.Errors.ERROR_INVALID_HANDLE:
                        throw Win32Marshal.GetExceptionForWin32Error(errorCode);
                }
            }

            return true;
        }

        private static void VerifyHandleIsSync(SafeFileHandle handle, int fileType, FileAccess access)
        {
            // The technique here only really works for FILE_TYPE_DISK. FileMode is the right thing to check, but it currently
            // isn't available in WinRT.

            if (fileType == Interop.Kernel32.FileTypes.FILE_TYPE_DISK)
            {
                // If we can't check the handle, just assume it is ok.
                if (!(IsHandleSynchronous(handle, access) ?? true))
                    throw new ArgumentException(SR.Arg_HandleNotSync, nameof(handle));
            }
        }
    }
}
