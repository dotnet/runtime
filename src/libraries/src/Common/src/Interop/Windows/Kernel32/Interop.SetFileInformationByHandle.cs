// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa365539.aspx
        [DllImport(Libraries.Kernel32, SetLastError = true, ExactSpelling = true)]
        internal static extern bool SetFileInformationByHandle(SafeFileHandle hFile, FILE_INFO_BY_HANDLE_CLASS FileInformationClass, ref FILE_BASIC_INFO lpFileInformation, uint dwBufferSize);

        // Default values indicate "no change".  Use defaults so that we don't force callsites to be aware of the default values
        internal static unsafe bool SetFileTime(
            SafeFileHandle hFile,
            long creationTime = -1,
            long lastAccessTime = -1,
            long lastWriteTime = -1,
            long changeTime = -1,
            uint fileAttributes = 0)
        {
            FILE_BASIC_INFO basicInfo = new FILE_BASIC_INFO()
            {
                CreationTime = creationTime,
                LastAccessTime = lastAccessTime,
                LastWriteTime = lastWriteTime,
                ChangeTime = changeTime,
                FileAttributes = fileAttributes
            };

            return SetFileInformationByHandle(hFile, FILE_INFO_BY_HANDLE_CLASS.FileBasicInfo, ref basicInfo, (uint)sizeof(FILE_BASIC_INFO));
        }

        internal struct FILE_BASIC_INFO
        {
            internal long CreationTime;
            internal long LastAccessTime;
            internal long LastWriteTime;
            internal long ChangeTime;
            internal uint FileAttributes;
        }
    }
}
