// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    public partial class FileStream : Stream
    {
        private SafeFileHandle OpenHandle(FileMode mode, FileShare share, FileOptions options)
        {
            return CreateFileOpenHandle(mode, share, options);
        }

        private unsafe SafeFileHandle CreateFileOpenHandle(FileMode mode, FileShare share, FileOptions options)
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
            // (note that this is the effective default on CreateFile2)
            flagsAndAttributes |= (Interop.Kernel32.SecurityOptions.SECURITY_SQOS_PRESENT | Interop.Kernel32.SecurityOptions.SECURITY_ANONYMOUS);

            using (DisableMediaInsertionPrompt.Create())
            {
                Debug.Assert(_path != null);
                return ValidateFileHandle(
                    Interop.Kernel32.CreateFile(_path, fAccess, share, ref secAttrs, mode, flagsAndAttributes, IntPtr.Zero));
            }
        }

        private static bool GetDefaultIsAsync(SafeFileHandle handle)
        {
            return handle.IsAsync ?? !IsHandleSynchronous(handle, ignoreInvalid: true) ?? DefaultIsAsync;
        }

        private static unsafe bool? IsHandleSynchronous(SafeFileHandle fileHandle, bool ignoreInvalid)
        {
            if (fileHandle.IsInvalid)
                return null;

            uint fileMode;

            int status = Interop.NtDll.NtQueryInformationFile(
                FileHandle: fileHandle,
                IoStatusBlock: out Interop.NtDll.IO_STATUS_BLOCK ioStatus,
                FileInformation: &fileMode,
                Length: sizeof(uint),
                FileInformationClass: Interop.NtDll.FileModeInformation);

            switch (status)
            {
                case 0:
                    // We were successful
                    break;
                case Interop.NtDll.STATUS_INVALID_HANDLE:
                    if (!ignoreInvalid)
                    {
                        throw Win32Marshal.GetExceptionForWin32Error(Interop.Errors.ERROR_INVALID_HANDLE);
                    }
                    else
                    {
                        return null;
                    }
                default:
                    // Something else is preventing access
                    Debug.Fail("Unable to get the file mode information, status was" + status.ToString());
                    return null;
            }

            // If either of these two flags are set, the file handle is synchronous (not overlapped)
            return (fileMode & (Interop.NtDll.FILE_SYNCHRONOUS_IO_ALERT | Interop.NtDll.FILE_SYNCHRONOUS_IO_NONALERT)) > 0;
        }

        private static void VerifyHandleIsSync(SafeFileHandle handle, int fileType, FileAccess access)
        {
            // As we can accurately check the handle type when we have access to NtQueryInformationFile we don't need to skip for
            // any particular file handle type.

            // If the handle was passed in without an explicit async setting, we already looked it up in GetDefaultIsAsync
            if (!handle.IsAsync.HasValue)
                return;

            // If we can't check the handle, just assume it is ok.
            if (!(IsHandleSynchronous(handle, ignoreInvalid: false) ?? true))
                throw new ArgumentException(SR.Arg_HandleNotSync, nameof(handle));
        }
    }
}
