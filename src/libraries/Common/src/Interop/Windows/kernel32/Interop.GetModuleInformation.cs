// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal partial class Interop
{
    internal partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "K32GetModuleInformation")]
        private static extern bool GetModuleInformation(SafeProcessHandle processHandle, IntPtr moduleHandle, out NtModuleInfo ntModuleInfo, int size);

        internal static bool GetModuleInformation(SafeProcessHandle processHandle, IntPtr moduleHandle, out NtModuleInfo ntModuleInfo) =>
            GetModuleInformation(processHandle, moduleHandle, out ntModuleInfo, NtModuleInfo.s_sizeOf);

        internal struct NtModuleInfo
        {
            internal static readonly int s_sizeOf = Marshal.SizeOf<NtModuleInfo>();

            internal IntPtr BaseOfDll;
            internal int SizeOfImage;
            internal IntPtr EntryPoint;
        }
    }
}
