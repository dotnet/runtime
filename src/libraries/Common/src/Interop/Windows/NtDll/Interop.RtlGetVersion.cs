// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security;

internal partial class Interop
{
    internal partial class NtDll
    {
        [DllImport(Libraries.NtDll, ExactSpelling=true)]
        private static extern int RtlGetVersion(out RTL_OSVERSIONINFOEX lpVersionInformation);

        internal static string RtlGetVersion()
        {
            RTL_OSVERSIONINFOEX osvi = new RTL_OSVERSIONINFOEX();
            osvi.dwOSVersionInfoSize = (uint)Marshal.SizeOf(osvi);
            const string version = "Microsoft Windows";
            if (RtlGetVersion(out osvi) == 0)
            {
                return string.Format("{0} {1}.{2}.{3} {4}",
                    version, osvi.dwMajorVersion, osvi.dwMinorVersion, osvi.dwBuildNumber, osvi.szCSDVersion);
            }
            else
            {
                return version;
            }
        }
    }
}
