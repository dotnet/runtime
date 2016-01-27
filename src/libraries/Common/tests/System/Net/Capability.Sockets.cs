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

        public static bool SocketsReuseUnicastPortSupport()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                RTL_OSVERSIONINFOW v = default(RTL_OSVERSIONINFOW);
                v.dwOSVersionInfoSize = (uint)Marshal.SizeOf<RTL_OSVERSIONINFOW>();
                RtlGetVersion(ref v);

                return (v.dwMajorVersion == 10);
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
