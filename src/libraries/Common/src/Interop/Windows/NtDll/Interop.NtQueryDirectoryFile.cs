// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class NtDll
    {
        // https://msdn.microsoft.com/en-us/library/windows/hardware/ff556633.aspx
        // https://msdn.microsoft.com/en-us/library/windows/hardware/ff567047.aspx
        [DllImport(Libraries.NtDll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public unsafe static extern int NtQueryDirectoryFile(
            IntPtr FileHandle,
            IntPtr Event,
            IntPtr ApcRoutine,
            IntPtr ApcContext,
            out IO_STATUS_BLOCK IoStatusBlock,
            IntPtr FileInformation,
            uint Length,
            FILE_INFORMATION_CLASS FileInformationClass,
            BOOLEAN ReturnSingleEntry,
            UNICODE_STRING* FileName,
            BOOLEAN RestartScan);
    }
}
