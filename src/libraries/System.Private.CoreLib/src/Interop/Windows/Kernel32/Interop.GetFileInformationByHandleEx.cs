// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, SetLastError = true)]
        internal static extern bool GetFileInformationByHandleEx(SafeFileHandle hFile, FILE_INFO_BY_HANDLE_CLASS FileInformationClass, out FILE_STANDARD_INFO lpFileInformation, uint dwBufferSize);

        internal struct FILE_STANDARD_INFO
        {
            internal long AllocationSize;
            internal long EndOfFile;
            internal uint NumberOfLinks;
            internal BOOL DeletePending;
            internal BOOL Directory;
        }
    }
}
