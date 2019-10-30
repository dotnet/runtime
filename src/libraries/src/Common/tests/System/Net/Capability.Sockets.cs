// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace System.Net.Test.Common
{
    public static partial class Capability
    {
        // TODO: Using RtlGetVersion is temporary until issue #4741 gets resolved.
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

        public static bool? SocketsReuseUnicastPortSupport()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                RTL_OSVERSIONINFOW v = default(RTL_OSVERSIONINFOW);
                v.dwOSVersionInfoSize = (uint)Marshal.SizeOf<RTL_OSVERSIONINFOW>();
                RtlGetVersion(ref v);

                if (v.dwMajorVersion == 10)
                {
                    return true;
                }
                else if (v.dwMajorVersion == 6 && (v.dwMinorVersion == 2 || v.dwMinorVersion == 3))
                {
                    // On Windows 8/Windows Server 2012 (major=6, minor=2) or Windows 8.1/Windows Server 2012 R2
                    // (major=6, minor=3), this feature is not present unless a servicing patch is installed.
                    // So, we return null to indicate that it is indeterminate whether the feature is active.
                    return null;
                }
                else
                {
                    return false;
                }
            }

            return false;
        }

        public static bool IPv6Support()
        {
            return Socket.OSSupportsIPv6;
        }

        public static bool IPv4Support()
        {
            return Socket.OSSupportsIPv4;
        }
    }
}
