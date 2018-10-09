// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class NtDll
    {
        [DllImport(Libraries.NtDll, ExactSpelling=true)]
        private static extern int RtlGetVersion(ref RTL_OSVERSIONINFOEX lpVersionInformation);

        internal static unsafe string RtlGetVersion()
        {
            var osvi = new RTL_OSVERSIONINFOEX();
            osvi.dwOSVersionInfoSize = (uint)sizeof(RTL_OSVERSIONINFOEX);
            const string version = "Microsoft Windows";
            if (RtlGetVersion(ref osvi) == 0)
            {
                return osvi.szCSDVersion[0] != '\0' ?
                    string.Format("{0} {1}.{2}.{3} {4}", version, osvi.dwMajorVersion, osvi.dwMinorVersion, osvi.dwBuildNumber, new string(&(osvi.szCSDVersion[0]))) :
                    string.Format("{0} {1}.{2}.{3}", version, osvi.dwMajorVersion, osvi.dwMinorVersion, osvi.dwBuildNumber);
            }
            else
            {
                return version;
            }
        }
    }
}
