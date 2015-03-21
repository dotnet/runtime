// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.CoreFile_L2, SetLastError = true)]
        internal static extern bool GetFileInformationByHandleEx(SafeFileHandle hFile, FILE_INFO_BY_HANDLE_CLASS FileInformationClass, out FILE_STANDARD_INFO lpFileInformation, uint dwBufferSize);

        internal partial struct FILE_STANDARD_INFO
        {
            internal long AllocationSize;
            internal long EndOfFile;
            internal uint NumberOfLinks;
            [MarshalAs(UnmanagedType.U1)]
            internal bool DeletePending;
            [MarshalAs(UnmanagedType.U1)]
            internal bool Directory;
        }

    }
}
