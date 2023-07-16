// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [LibraryImport(Libraries.Kernel32, EntryPoint = "GetVolumePathNameW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static unsafe partial bool GetVolumePathName(char* lpszFileName, char* lpszVolumePathName, int cchBufferLength);

        internal static unsafe string GetVolumePathName(string fileName)
        {
            // Ensure we have the prefix
            fileName = PathInternal.EnsureExtendedPrefixIfNeeded(fileName);

            // Ensure our output buffer will be long enough (see https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getvolumepathnamew#remarks)
            int requiredBufferLength = (int)GetFullPathNameW(ref MemoryMarshal.GetReference<char>(fileName), 0, ref Unsafe.NullRef<char>(), 0);
            if (requiredBufferLength == 0) throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error(), fileName);

            // Allocate a value string builder
            // note: MAX_PATH is not a hard limit, but would only be exceeded by a long path
            ValueStringBuilder vsb = new(requiredBufferLength <= Interop.Kernel32.MAX_PATH ? stackalloc char[Interop.Kernel32.MAX_PATH] : default);
            vsb.EnsureCapacity(requiredBufferLength);

            // Call the actual API
            fixed (char* lpszFileName = fileName)
            {
                fixed (char* lpszVolumePathName = vsb.RawChars)
                {
                    if (GetVolumePathName(lpszFileName, lpszVolumePathName, requiredBufferLength))
                    {
                        vsb.Length = vsb.RawChars.IndexOf('\0');
                        return vsb.ToString();
                    }
                }
            }

            // Deal with error
            int error = Marshal.GetLastWin32Error();
            Debug.Assert(error != Interop.Errors.ERROR_INSUFFICIENT_BUFFER);
            throw Win32Marshal.GetExceptionForWin32Error(error, fileName);
        }
    }
}
