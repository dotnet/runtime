// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Libraries.Kernel32, EntryPoint = "MapViewOfFile", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static partial SafeMemoryMappedViewHandle MapViewOfFile(
#else
        [DllImport(Libraries.Kernel32, EntryPoint = "MapViewOfFile", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern SafeMemoryMappedViewHandle MapViewOfFile(
#endif
            SafeMemoryMappedFileHandle hFileMappingObject,
            int dwDesiredAccess,
            int dwFileOffsetHigh,
            int dwFileOffsetLow,
            UIntPtr dwNumberOfBytesToMap);
    }
}
