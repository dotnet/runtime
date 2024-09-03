// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace WindowsSpecific
{
    public static partial class Program
    {
        private static void CompatShims()
        {
            Version osVersion = RtlGetVersion();
            if (osVersion == null)
            {
                Console.WriteLine("Failed to get OS version through RtlGetVersion.");
            }
            else
            {
                Console.WriteLine($"Detected true OS version: {osVersion.Major}.{osVersion.Minor}");
                if (OsVersionIsNewerThan(osVersion))
                {
                    Console.WriteLine($"Reported OS version is newer or equal to the true OS version - no shims.");
                }
                else
                {
                    Console.WriteLine($"Reported OS version is lower than the true OS version - shims in use.");
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct OSVERSIONINFOEX
        {

            internal uint dwOSVersionInfoSize;
            internal uint dwMajorVersion;
            internal uint dwMinorVersion;
            internal uint dwBuildNumber;
            internal uint dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] //
            internal string szCSDVersion;
            internal ushort wServicePackMajor;
            internal ushort wServicePackMinor;
            internal ushort wSuiteMask;
            internal byte wProductType;
            internal byte wReserved;
        }

        [Flags]
        enum ConditionMask : byte
        {
            VER_EQUAL = 1,
            VER_GREATER = 2,
            VER_GREATER_EQUAL = 3,
            VER_LESS = 4,
            VER_LESS_EQUAL = 5,
            VER_AND = 6,
            VER_OR = 7
        }

        [Flags]
        enum TypeMask : uint
        {
            VER_MINORVERSION = 0x0000001,
            VER_MAJORVERSION = 0x0000002,
            VER_BUILDNUMBER = 0x0000004,
            VER_PLATFORMID = 0x0000008,
            VER_SERVICEPACKMINOR = 0x0000010,
            VER_SERVICEPACKMAJOR = 0x0000020,
            VER_SUITENAME = 0x0000040,
            VER_PRODUCT_TYPE = 0x0000080
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        internal unsafe struct RTL_OSVERSIONINFOEX
        {
            internal uint dwOSVersionInfoSize;
            internal uint dwMajorVersion;
            internal uint dwMinorVersion;
            internal uint dwBuildNumber;
            internal uint dwPlatformId;
            internal fixed char szCSDVersion[128];
        }

        [DllImport("ntdll.dll", ExactSpelling=true)]
        private static extern int RtlGetVersion(ref RTL_OSVERSIONINFOEX lpVersionInformation);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool VerifyVersionInfo(ref OSVERSIONINFOEX lpVersionInfo, TypeMask dwTypeMask, ulong dwlConditionMask);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern ulong VerSetConditionMask(ulong dwlConditionMask, TypeMask dwTypeBitMask, ConditionMask dwConditionMask);

        internal unsafe static int RtlGetVersionEx(out RTL_OSVERSIONINFOEX osvi)
        {
            osvi = new RTL_OSVERSIONINFOEX();
            osvi.dwOSVersionInfoSize = (uint)sizeof(RTL_OSVERSIONINFOEX);
            return RtlGetVersion(ref osvi);
        }

        internal static Version RtlGetVersion()
        {
            if (RtlGetVersionEx(out RTL_OSVERSIONINFOEX osvi) == 0)
            {
                return new Version((int)osvi.dwMajorVersion, (int)osvi.dwMinorVersion);
            }
            else
            {
                return null;
            }
        }

        internal static bool OsVersionIsNewerThan(Version osVersion)
        {
            // check if newer than
            OSVERSIONINFOEX osv = new OSVERSIONINFOEX()
            {
                dwOSVersionInfoSize = (uint)Marshal.SizeOf<OSVERSIONINFOEX>(),
                dwMajorVersion = (uint)osVersion.Major,
                dwMinorVersion = (uint)osVersion.Minor
            };

            var conditionMask = 0uL;
            conditionMask = VerSetConditionMask(conditionMask, TypeMask.VER_MAJORVERSION, ConditionMask.VER_GREATER_EQUAL);
            conditionMask = VerSetConditionMask(conditionMask, TypeMask.VER_MINORVERSION, ConditionMask.VER_GREATER_EQUAL);

            if (VerifyVersionInfo(ref osv, TypeMask.VER_MAJORVERSION | TypeMask.VER_MINORVERSION, conditionMask))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
