// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace System
{
    public static partial class Environment
    {
        private static string CurrentDirectoryCore
        {
            get
            {
                var builder = new ValueStringBuilder(stackalloc char[Interop.Kernel32.MAX_PATH]);

                uint length;
                while ((length = Interop.Kernel32.GetCurrentDirectory((uint)builder.Capacity, ref builder.GetPinnableReference())) > builder.Capacity)
                {
                    builder.EnsureCapacity((int)length);
                }

                if (length == 0)
                    throw Win32Marshal.GetExceptionForLastWin32Error();

                builder.Length = (int)length;

                // If we have a tilde in the path, make an attempt to expand 8.3 filenames
                if (builder.AsSpan().Contains('~'))
                {
                    string result = PathHelper.TryExpandShortFileName(ref builder, null);
                    builder.Dispose();
                    return result;
                }

                return builder.ToString();
            }
            set
            {
                if (!Interop.Kernel32.SetCurrentDirectory(value))
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw Win32Marshal.GetExceptionForWin32Error(
                        errorCode == Interop.Errors.ERROR_FILE_NOT_FOUND ? Interop.Errors.ERROR_PATH_NOT_FOUND : errorCode,
                        value);
                }
            }
        }

        public static string[] GetLogicalDrives() => DriveInfoInternal.GetLogicalDrives();

        internal const string NewLineConst = "\r\n";

        public static int SystemPageSize
        {
            get
            {
                Interop.Kernel32.GetSystemInfo(out Interop.Kernel32.SYSTEM_INFO info);
                return info.dwPageSize;
            }
        }

        private static string ExpandEnvironmentVariablesCore(string name)
        {
            var builder = new ValueStringBuilder(stackalloc char[128]);

            uint length;
            while ((length = Interop.Kernel32.ExpandEnvironmentStrings(name, ref builder.GetPinnableReference(), (uint)builder.Capacity)) > builder.Capacity)
            {
                builder.EnsureCapacity((int)length);
            }

            if (length == 0)
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

            // length includes the null terminator
            builder.Length = (int)length - 1;
            return builder.ToString();
        }

        private static bool Is64BitOperatingSystemWhen32BitProcess =>
            Interop.Kernel32.IsWow64Process(Interop.Kernel32.GetCurrentProcess(), out bool isWow64) && isWow64;

        public static string MachineName =>
            Interop.Kernel32.GetComputerName() ??
            throw new InvalidOperationException(SR.InvalidOperation_ComputerName);

        private static unsafe OperatingSystem GetOSVersion()
        {
            var version = new Interop.Kernel32.OSVERSIONINFOEX { dwOSVersionInfoSize = sizeof(Interop.Kernel32.OSVERSIONINFOEX) };
            if (!Interop.Kernel32.GetVersionExW(ref version))
            {
                throw new InvalidOperationException(SR.InvalidOperation_GetVersion);
            }

            return new OperatingSystem(
                PlatformID.Win32NT,
                new Version(version.dwMajorVersion, version.dwMinorVersion, version.dwBuildNumber, (version.wServicePackMajor << 16) | version.wServicePackMinor),
                Marshal.PtrToStringUni((IntPtr)version.szCSDVersion));
        }

        public static string SystemDirectory
        {
            get
            {
                // Normally this will be C:\Windows\System32
                var builder = new ValueStringBuilder(stackalloc char[32]);

                uint length;
                while ((length = Interop.Kernel32.GetSystemDirectoryW(ref builder.GetPinnableReference(), (uint)builder.Capacity)) > builder.Capacity)
                {
                    builder.EnsureCapacity((int)length);
                }

                if (length == 0)
                    throw Win32Marshal.GetExceptionForLastWin32Error();

                builder.Length = (int)length;
                return builder.ToString();
            }
        }

        public static unsafe long WorkingSet
        {
            get
            {
                Interop.Kernel32.PROCESS_MEMORY_COUNTERS memoryCounters = default;
                memoryCounters.cb = (uint)(sizeof(Interop.Kernel32.PROCESS_MEMORY_COUNTERS));

                if (!Interop.Kernel32.GetProcessMemoryInfo(Interop.Kernel32.GetCurrentProcess(), ref memoryCounters, memoryCounters.cb))
                {
                    return 0;
                }
                return (long)memoryCounters.WorkingSetSize;
            }
        }
    }
}
