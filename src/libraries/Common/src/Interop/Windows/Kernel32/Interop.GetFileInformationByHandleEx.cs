// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, SetLastError = true, ExactSpelling = true)]
        internal static extern unsafe bool GetFileInformationByHandle(SafeFileHandle hFile, BY_HANDLE_FILE_INFORMATION lpFileInformation);

        [GeneratedDllImport(Libraries.Kernel32, ExactSpelling = true, SetLastError = true)]
        internal static unsafe partial bool GetFileInformationByHandleEx(SafeFileHandle hFile, int FileInformationClass, void* lpFileInformation, uint dwBufferSize);

        internal struct BY_HANDLE_FILE_INFORMATION
        {
            internal uint dwFileAttributes;
            internal FILE_TIME ftCreationTime;
            internal FILE_TIME ftLastAccessTime;
            internal FILE_TIME ftLastWriteTime;
            internal uint dwVolumeSerialNumber;
            internal uint nFileSizeHigh;
            internal uint nFileSizeLow;
            internal uint nNumberOfLinks;
            internal uint nFileIndexHigh;
            internal uint nFileIndexLow;
        }
    }
}
