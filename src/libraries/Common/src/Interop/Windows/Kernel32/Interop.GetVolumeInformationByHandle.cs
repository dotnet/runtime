// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [LibraryImport(Libraries.Kernel32, EntryPoint = "GetVolumeInformationByHandleW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static unsafe partial bool GetVolumeInformationByHandleW(
            SafeFileHandle hFile,
            char* volumeName,
            int volumeNameBufLen,
            int* volSerialNumber,
            int* maxFileNameLen,
            int* fileSystemFlags,
            char* fileSystemName,
            int fileSystemNameBufLen);

        public static unsafe int GetVolumeInformationByHandle(
            SafeFileHandle hFile,
            out string? volumePath,
            bool wantsVolumePath,
            int* volSerialNumber,
            int* maxFileNameLen,
            out int fileSystemFlags,
            char* fileSystemName,
            int fileSystemNameBufLen)
        {
            // Allocate output buffer on the stack initially.
            const int stackAllocation = 512;
            Span<char> volumePathBuffer = wantsVolumePath ? stackalloc char[stackAllocation + 1] : stackalloc char[0]; // +1 to ensure a \0 at the end, todo: is this necessary
            int bufferSize = stackAllocation;

            // Loop until the buffer's big enough.
            while (true)
            {
                fixed (char* lpszVolumePathName = volumePathBuffer)
                {
                    // Try to get the volume name, will succeed if the buffer's big enough.
                    fixed (int* pFileSystemFlags = &fileSystemFlags)
                    {
                        if (GetVolumeInformationByHandleW(hFile, lpszVolumePathName, Math.Max(volumePathBuffer.Length - 1, 0), volSerialNumber, maxFileNameLen, pFileSystemFlags, fileSystemName, fileSystemNameBufLen))
                        {
                            if (wantsVolumePath) volumePath = new string(lpszVolumePathName);
                            else volumePath = null;
                            return 0;
                        }
                    }

                    // Check if the error was that the buffer is not large enough.
                    int error = Marshal.GetLastWin32Error();
                    if (wantsVolumePath && error == Interop.Errors.ERROR_INSUFFICIENT_BUFFER) //todo: check this is the correct error
                    {
                        // Create a new buffer and try again.
                        // todo: use array pool and check for overflow
                        volumePathBuffer = new char[bufferSize *= 2];
                        continue;
                    }
                    else
                    {
                        // Return our error.
                        volumePath = null;
                        return error;
                    }
                }
            }
        }
    }
}
