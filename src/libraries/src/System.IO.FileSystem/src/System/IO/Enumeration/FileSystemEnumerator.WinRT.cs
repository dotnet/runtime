// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.IO.Enumeration
{
    public partial class FileSystemEnumerator<TResult>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe bool GetData()
        {
            if (!Interop.Kernel32.GetFileInformationByHandleEx(
                _directoryHandle,
                Interop.Kernel32.FILE_INFO_BY_HANDLE_CLASS.FileFullDirectoryInfo,
                _buffer,
                (uint)_bufferLength))
            {
                int error = Marshal.GetLastWin32Error();
                switch (error)
                {
                    case Interop.Errors.ERROR_NO_MORE_FILES:
                        DirectoryFinished();
                        return false;
                    case Interop.Errors.ERROR_ACCESS_DENIED:
                        if (_options.IgnoreInaccessible)
                        {
                            return false;
                        }
                        break;
                }

                if (!ContinueOnError(error))
                    throw Win32Marshal.GetExceptionForWin32Error(error, _currentPath);
            }

            return true;
        }

        private IntPtr CreateRelativeDirectoryHandle(ReadOnlySpan<char> relativePath, string fullPath)
        {
            // We don't have access to any APIs that allow us to pass in a base handle in UAP,
            // just call our "normal" handle open.
            return CreateDirectoryHandle(fullPath, ignoreNotFound: true);
        }
    }
}
