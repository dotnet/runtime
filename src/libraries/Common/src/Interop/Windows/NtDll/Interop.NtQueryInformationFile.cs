// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class NtDll
    {
        [LibraryImport(Libraries.NtDll)]
        internal static unsafe partial int NtQueryInformationFile(
            SafeFileHandle FileHandle,
            out IO_STATUS_BLOCK IoStatusBlock,
            void* FileInformation,
            uint Length,
            uint FileInformationClass);

        internal const uint FileModeInformation = 16;

        internal const int STATUS_INVALID_HANDLE = unchecked((int)0xC0000008);
    }
}
