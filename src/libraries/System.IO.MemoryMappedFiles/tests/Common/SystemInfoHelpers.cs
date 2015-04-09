// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

/// <summary>
/// Shared helper class that exposes the operating system's memory page size.
/// </summary>
internal class SystemInfoHelpers
{
    internal static uint GetPageSize()
    {
        if (Interop.PlatformDetection.OperatingSystem == Interop.OperatingSystem.Windows)
        {
            SYSTEM_INFO info;
            GetSystemInfo(out info);
            return info.dwPageSize;
        }
        else
        {
            int result = sysconf(_SC_PAGESIZE);
            return result > 0 ?
                (uint)result :
                8192; // reasonable default
        }
    }

    [DllImport("api-ms-win-core-sysinfo-l1-1-0.dll")]
    private static extern void GetSystemInfo(out SYSTEM_INFO input);

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_INFO
    {
        internal uint dwOemId;
        internal uint dwPageSize;
        internal IntPtr lpMinimumApplicationAddress;
        internal IntPtr lpMaximumApplicationAddress;
        internal IntPtr dwActiveProcessorMask;
        internal uint dwNumberOfProcessors;
        internal uint dwProcessorType;
        internal uint dwAllocationGranularity;
        internal short wProcessorLevel;
        internal short wProcessorRevision;
    }

    [DllImport("libc", SetLastError = true)]
    private static extern int sysconf(int name);

    private const int _SC_PAGESIZE = 30;
}