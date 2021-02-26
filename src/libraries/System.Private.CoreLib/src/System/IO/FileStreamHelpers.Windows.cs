// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    // this type defines a set of stateless FileStream/FileStreamStrategy helper methods
    internal static class FileStreamHelpers
    {
        internal static FileStreamStrategy ChooseStrategy(FileStream fileStream, SafeFileHandle handle, FileAccess access, int bufferSize, bool isAsync)
            => new LegacyFileStreamStrategy(fileStream, handle, access, bufferSize, isAsync);

        internal static FileStreamStrategy ChooseStrategy(FileStream fileStream, string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options)
            => new LegacyFileStreamStrategy(fileStream, path, mode, access, share, bufferSize, options);

        internal static SafeFileHandle OpenHandle(string path, FileMode mode, FileAccess access, FileShare share, FileOptions options)
            => CreateFileOpenHandle(path, mode, access, share, options);

        private static unsafe SafeFileHandle CreateFileOpenHandle(string path, FileMode mode, FileAccess access, FileShare share, FileOptions options)
        {
            Interop.Kernel32.SECURITY_ATTRIBUTES secAttrs = GetSecAttrs(share);

            int fAccess =
                ((access & FileAccess.Read) == FileAccess.Read ? Interop.Kernel32.GenericOperations.GENERIC_READ : 0) |
                ((access & FileAccess.Write) == FileAccess.Write ? Interop.Kernel32.GenericOperations.GENERIC_WRITE : 0);

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
                Debug.Assert(path != null);
                return ValidateFileHandle(
                    Interop.Kernel32.CreateFile(path, fAccess, share, &secAttrs, mode, flagsAndAttributes, IntPtr.Zero),
                    path,
                    (options & FileOptions.Asynchronous) != 0);
            }
        }

        internal static bool GetDefaultIsAsync(SafeFileHandle handle, bool defaultIsAsync)
        {
            return handle.IsAsync ?? !IsHandleSynchronous(handle, ignoreInvalid: true) ?? defaultIsAsync;
        }

        private static unsafe bool? IsHandleSynchronous(SafeFileHandle fileHandle, bool ignoreInvalid)
        {
            if (fileHandle.IsInvalid)
                return null;

            uint fileMode;

            int status = Interop.NtDll.NtQueryInformationFile(
                FileHandle: fileHandle,
                IoStatusBlock: out _,
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

        internal static void VerifyHandleIsSync(SafeFileHandle handle)
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

        private static unsafe Interop.Kernel32.SECURITY_ATTRIBUTES GetSecAttrs(FileShare share)
        {
            Interop.Kernel32.SECURITY_ATTRIBUTES secAttrs = default;
            if ((share & FileShare.Inheritable) != 0)
            {
                secAttrs = new Interop.Kernel32.SECURITY_ATTRIBUTES
                {
                    nLength = (uint)sizeof(Interop.Kernel32.SECURITY_ATTRIBUTES),
                    bInheritHandle = Interop.BOOL.TRUE
                };
            }
            return secAttrs;
        }

        private static SafeFileHandle ValidateFileHandle(SafeFileHandle fileHandle, string path, bool useAsyncIO)
        {
            if (fileHandle.IsInvalid)
            {
                // Return a meaningful exception with the full path.

                // NT5 oddity - when trying to open "C:\" as a Win32FileStream,
                // we usually get ERROR_PATH_NOT_FOUND from the OS.  We should
                // probably be consistent w/ every other directory.
                int errorCode = Marshal.GetLastWin32Error();

                if (errorCode == Interop.Errors.ERROR_PATH_NOT_FOUND && path!.Length == PathInternal.GetRootLength(path))
                    errorCode = Interop.Errors.ERROR_ACCESS_DENIED;

                throw Win32Marshal.GetExceptionForWin32Error(errorCode, path);
            }

            fileHandle.IsAsync = useAsyncIO;
            return fileHandle;
        }
    }
}
