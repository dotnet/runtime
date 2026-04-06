// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class NtDll
    {
        [LibraryImport(Libraries.NtDll)]
        private static unsafe partial int RtlGetVersion(RTL_OSVERSIONINFOEX* lpVersionInformation);

        internal static unsafe int RtlGetVersionEx(out RTL_OSVERSIONINFOEX osvi)
        {
            osvi = default;
            osvi.dwOSVersionInfoSize = (uint)sizeof(RTL_OSVERSIONINFOEX);
            fixed (RTL_OSVERSIONINFOEX* p = &osvi)
                return RtlGetVersion(p);
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct RTL_OSVERSIONINFOEX
        {
            internal uint dwOSVersionInfoSize;
            internal uint dwMajorVersion;
            internal uint dwMinorVersion;
            internal uint dwBuildNumber;
            internal uint dwPlatformId;
#if NET
            internal CSDVersionBuffer szCSDVersion;

            [InlineArray(128)]
            internal struct CSDVersionBuffer
            {
                private char _element0;
            }
#else
            internal unsafe fixed char szCSDVersion[128];
#endif
        }
    }
}
