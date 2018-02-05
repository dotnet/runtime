// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.IO.Enumeration
{
    public partial class FileSystemEnumerator<TResult>
    {
        /// <returns>'true' if new data was found</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool GetData()
        {
            Debug.Assert(_directoryHandle != (IntPtr)(-1) && _directoryHandle != IntPtr.Zero && !_lastEntryFound);

            int status = Interop.NtDll.NtQueryDirectoryFile(
                FileHandle: _directoryHandle,
                Event: IntPtr.Zero,
                ApcRoutine: IntPtr.Zero,
                ApcContext: IntPtr.Zero,
                IoStatusBlock: out Interop.NtDll.IO_STATUS_BLOCK statusBlock,
                FileInformation: _buffer,
                Length: (uint)_buffer.Length,
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
    }
}
