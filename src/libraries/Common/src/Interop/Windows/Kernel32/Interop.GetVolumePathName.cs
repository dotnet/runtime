// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [LibraryImport(Libraries.Kernel32, EntryPoint = "GetVolumePathNameW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static unsafe partial bool GetVolumePathName(char* lpszFileName, char* lpszVolumePathName, int cchBufferLength);

        public static unsafe string GetVolumePathName(string fileName)
        {
            // Ensure we have the prefix
            fileName = PathInternal.EnsureExtendedPrefixIfNeeded(fileName);

            // Ensure our output buffer will be long enough (see https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getvolumepathnamew#remarks)
            ValueStringBuilder vsb = new(stackalloc char[MAX_PATH + 1]); //note: MAX_PATH is not a hard limit, but would only be exceeded by a long path
            PathHelper.GetFullPathName(fileName, ref vsb);

            // Call the actual API
            fixed (char* lpszFileName = fileName)
            {
                fixed (char* lpszVolumePathName = vsb.RawChars)
                {
                    // + 1 because \0 is not included in Length from GetFullPathName, but should exist
                    if (_GetVolumePathName(lpszFileName, lpszVolumePathName, vsb.RawChars.Length + 1))
                    {
                        return new string(vsb.RawChars[..vsb.RawChars.IndexOf('\0')]);
                    }
                }
            }

            // Deal with error
            int error = Marshal.GetLastWin32Error();
            Debug.Assert(error != Errors.ERROR_INSUFFICIENT_BUFFER);
            throw Win32Marshal.GetExceptionForWin32Error(error, fileName);
        }
    }
}
