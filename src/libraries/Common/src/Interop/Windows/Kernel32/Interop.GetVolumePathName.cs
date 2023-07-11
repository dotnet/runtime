// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [LibraryImport(Libraries.Kernel32, EntryPoint = "GetVolumePathNameW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static unsafe partial bool _GetVolumePathName(char* lpszFileName, char* lpszVolumePathName, int cchBufferLength);

        public static unsafe int GetVolumePathName(string fileName, out string? volumePath)
        {
            fileName = PathInternal.EnsureExtendedPrefixIfNeeded(fileName); //todo: unsure if this is needed for this API
            fixed (char* lpszFileName = fileName)
            {
                // Allocate output buffer on the stack initially.
                const int stackAllocation = 512;
                Span<char> volumePathBuffer = stackalloc char[stackAllocation + 1]; // +1 to ensure a \0 at the end, todo: is this necessary
                int bufferSize = stackAllocation;

                // Loop until the buffer's big enough.
                while (true)
                {
                    fixed (char* lpszVolumePathName = volumePathBuffer)
                    {
                        // Try to get the volume name, will succeed if the buffer's big enough.
                        if (_GetVolumePathName(lpszFileName, lpszVolumePathName, volumePathBuffer.Length - 1))
                        {
                            volumePath = new string(lpszVolumePathName);
                            return 0;
                        }

                        // Check if the error was that the buffer is not large enough.
                        int error = Marshal.GetLastWin32Error();
                        if (error == Interop.Errors.ERROR_INSUFFICIENT_BUFFER) //todo: check this is the correct error
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
}
