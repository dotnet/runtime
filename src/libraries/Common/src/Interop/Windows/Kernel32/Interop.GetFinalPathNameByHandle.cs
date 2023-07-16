// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal const uint FILE_NAME_NORMALIZED = 0x0;
        internal const uint VOLUME_NAME_DOS = 0x0;

        // https://docs.microsoft.com/windows/desktop/api/fileapi/nf-fileapi-getfinalpathnamebyhandlew (kernel32)
        [LibraryImport(Libraries.Kernel32, EntryPoint = "GetFinalPathNameByHandleW", SetLastError = true)]
        internal static unsafe partial uint GetFinalPathNameByHandle(
            SafeFileHandle hFile,
            char* lpszFilePath,
            uint cchFilePath,
            uint dwFlags);

        internal static unsafe bool GetFinalPathNameByHandle(SafeFileHandle hFile, uint dwFlags, [MaybeNullWhen(false)] out string result)
        {
            // Default value
            result = null;

            // Determine the required buffer size
            uint bufferSize = GetFinalPathNameByHandle(hFile, null, 0, dwFlags);
            if (bufferSize == 0) return false;

            // Allocate the buffer
            ValueStringBuilder vsb = new(bufferSize <= Interop.Kernel32.MAX_PATH ? stackalloc char[Interop.Kernel32.MAX_PATH] : default);
            vsb.EnsureCapacity((int)bufferSize);

            // Call the API
            fixed (char* lpszFilePath = vsb.RawChars)
            {
                int length = (int)GetFinalPathNameByHandle(hFile, lpszFilePath, bufferSize, dwFlags);
                if (length == 0) return false;

                // Return our string
                vsb.Length = length;
                result = vsb.ToString();
                return true;
            }
        }
    }
}
