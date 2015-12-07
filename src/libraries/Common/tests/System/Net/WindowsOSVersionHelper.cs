// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace System.Net.Test.Common
{
    // TODO: This class is temporary until issue #4741 gets resolved.
    public static class WindowsOSVersionHelper
    {
        [DllImport("ntdll", CharSet = CharSet.Unicode)]
        private static extern int RtlGetVersion(ref RTL_OSVERSIONINFOW lpVersionInformation);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct RTL_OSVERSIONINFOW
        {
            internal uint dwOSVersionInfoSize;
            internal uint dwMajorVersion;
            internal uint dwMinorVersion;
            internal uint dwBuildNumber;
            internal uint dwPlatformId;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
            internal char[] szCSDVersion;
        }

        public static Version GetVersion()
        {
            RTL_OSVERSIONINFOW v = default(RTL_OSVERSIONINFOW);
            v.dwOSVersionInfoSize = (uint)Marshal.SizeOf<RTL_OSVERSIONINFOW>();
            RtlGetVersion(ref v);

            return new Version((int)v.dwMajorVersion, (int)v.dwMinorVersion, (int)v.dwBuildNumber);
        }
    }
}
