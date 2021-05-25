// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#if MS_IO_REDIST
namespace Microsoft.IO.Enumeration
#else
namespace System.IO.Enumeration
#endif
{
    public partial class FileSystemEnumerator<TResult>
    {
        /// <returns>'true' if new data was found</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe bool GetData()
        {
            Debug.Assert(_directoryHandle != (IntPtr)(-1) && _directoryHandle != IntPtr.Zero && !_lastEntryFound);

            int status = Interop.NtDll.NtQueryDirectoryFile(
                FileHandle: _directoryHandle,
                Event: IntPtr.Zero,
                ApcRoutine: IntPtr.Zero,
                ApcContext: IntPtr.Zero,
                IoStatusBlock: out Interop.NtDll.IO_STATUS_BLOCK statusBlock,
                FileInformation: _buffer,
                Length: (uint)_bufferLength,
                FileInformationClass: Interop.NtDll.FILE_INFORMATION_CLASS.FileFullDirectoryInformation,
                ReturnSingleEntry: Interop.BOOLEAN.FALSE,
                FileName: null,
                RestartScan: Interop.BOOLEAN.FALSE);

            switch ((uint)status)
            {
                case Interop.StatusOptions.STATUS_NO_MORE_FILES:
                    DirectoryFinished();
                    return false;
                case Interop.StatusOptions.STATUS_SUCCESS:
                    Debug.Assert(statusBlock.Information.ToInt64() != 0);
                    return true;
                // FILE_NOT_FOUND can occur when there are NO files in a volume root (usually there are hidden system files).
                case Interop.StatusOptions.STATUS_FILE_NOT_FOUND:
                    DirectoryFinished();
                    return false;
                default:
                    int error = (int)Interop.NtDll.RtlNtStatusToDosError(status);

                    // Note that there are many NT status codes that convert to ERROR_ACCESS_DENIED.
                    if ((error == Interop.Errors.ERROR_ACCESS_DENIED && _options.IgnoreInaccessible) || ContinueOnError(error))
                    {
                        DirectoryFinished();
                        return false;
                    }
                    throw Win32Marshal.GetExceptionForWin32Error(error, _currentPath);
            }
        }

        private unsafe IntPtr CreateRelativeDirectoryHandle(ReadOnlySpan<char> relativePath, string fullPath)
        {
            (uint status, IntPtr handle) = Interop.NtDll.CreateFile(
                relativePath,
                _directoryHandle,
                Interop.NtDll.CreateDisposition.FILE_OPEN,
                Interop.NtDll.DesiredAccess.FILE_LIST_DIRECTORY | Interop.NtDll.DesiredAccess.SYNCHRONIZE,
                createOptions: Interop.NtDll.CreateOptions.FILE_SYNCHRONOUS_IO_NONALERT | Interop.NtDll.CreateOptions.FILE_DIRECTORY_FILE
                    | Interop.NtDll.CreateOptions.FILE_OPEN_FOR_BACKUP_INTENT);

            switch (status)
            {
                case Interop.StatusOptions.STATUS_SUCCESS:
                    return handle;
                default:
                    // Note that there are numerous cases where multiple NT status codes convert to a single Win32 System Error codes,
                    // such as ERROR_ACCESS_DENIED. As we want to replicate Win32 handling/reporting and the mapping isn't documented,
                    // we should always do our logic on the converted code, not the NTSTATUS.

                    int error = (int)Interop.NtDll.RtlNtStatusToDosError((int)status);

                    if (ContinueOnDirectoryError(error, ignoreNotFound: true))
                    {
                        return IntPtr.Zero;
                    }

                    throw Win32Marshal.GetExceptionForWin32Error(error, fullPath);
            }
        }
    }
}
