// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

public static class PlatformDetection
{
    public static bool IsWindows { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public static bool IsWindows7 { get; } =  IsWindows && GetWindowsVersion() == 6 && GetWindowsMinorVersion() == 1;
    public static int WindowsVersion { get; } = GetWindowsVersion();

    private static int GetWindowsVersion()
    {
        if (IsWindows)
        {
            RTL_OSVERSIONINFOEX osvi = new RTL_OSVERSIONINFOEX();
            osvi.dwOSVersionInfoSize = (uint)Marshal.SizeOf(osvi);
            Assert.Equal(0, RtlGetVersion(out osvi));
            return (int)osvi.dwMajorVersion;
        }

        return -1;
    }
    
    private static int GetWindowsMinorVersion()
    {
        if (IsWindows)
        {
            RTL_OSVERSIONINFOEX osvi = new RTL_OSVERSIONINFOEX();
            osvi.dwOSVersionInfoSize = (uint)Marshal.SizeOf(osvi);
            Assert.Equal(0, RtlGetVersion(out osvi));
            return (int)osvi.dwMinorVersion;
        }

        return -1;
    }


    [DllImport("ntdll.dll")]
    private static extern int RtlGetVersion(out RTL_OSVERSIONINFOEX lpVersionInformation);

    [StructLayout(LayoutKind.Sequential)]
    private struct RTL_OSVERSIONINFOEX
    {
        internal uint dwOSVersionInfoSize;
        internal uint dwMajorVersion;
        internal uint dwMinorVersion;
        internal uint dwBuildNumber;
        internal uint dwPlatformId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        internal string szCSDVersion;
    }
}
