// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [GeneratedDllImport(Libraries.Kernel32, ExactSpelling = true, SetLastError = true)]
        internal static unsafe partial bool GetFileInformationByHandleEx(SafeFileHandle hFile, int FileInformationClass, void* lpFileInformation, uint dwBufferSize);
    }
}
