// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [LibraryImport(Libraries.Kernel32, EntryPoint = "K32GetModuleInformation", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetModuleInformation(SafeProcessHandle processHandle, IntPtr moduleHandle, out NtModuleInfo ntModuleInfo, int size);

        internal static unsafe bool GetModuleInformation(SafeProcessHandle processHandle, IntPtr moduleHandle, out NtModuleInfo ntModuleInfo) =>
            GetModuleInformation(processHandle, moduleHandle, out ntModuleInfo, sizeof(NtModuleInfo));

        internal struct NtModuleInfo
        {
            internal IntPtr BaseOfDll;
            internal int SizeOfImage;
            internal IntPtr EntryPoint;
        }
    }
}
