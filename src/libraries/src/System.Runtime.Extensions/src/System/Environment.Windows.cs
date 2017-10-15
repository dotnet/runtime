// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Internal.Runtime.Augments;

namespace System
{
    public static partial class Environment
    {
        private static string CurrentDirectoryCore
        {
            get
            {
                StringBuilder sb = StringBuilderCache.Acquire(Interop.Kernel32.MAX_PATH + 1);
                if (Interop.Kernel32.GetCurrentDirectory(sb.Capacity, sb) == 0)
                {
                    StringBuilderCache.Release(sb);
                    throw Win32Marshal.GetExceptionForLastWin32Error();
                }
                string currentDirectory = sb.ToString();

                // Note that if we have somehow put our command prompt into short
                // file name mode (i.e. by running edlin or a DOS grep, etc), then
                // this will return a short file name.
                if (currentDirectory.IndexOf('~') >= 0)
                {
                    int r = Interop.Kernel32.GetLongPathName(currentDirectory, sb, sb.Capacity);
                    if (r == 0 || r >= Interop.Kernel32.MAX_PATH)
                    {
                        int errorCode = r >= Interop.Kernel32.MAX_PATH ?
                            Interop.Errors.ERROR_FILENAME_EXCED_RANGE :
                            Marshal.GetLastWin32Error();

                        if (errorCode != Interop.Errors.ERROR_FILE_NOT_FOUND &&
                            errorCode != Interop.Errors.ERROR_PATH_NOT_FOUND &&
                            errorCode != Interop.Errors.ERROR_INVALID_FUNCTION &&
                            errorCode != Interop.Errors.ERROR_ACCESS_DENIED)
                        {
                            StringBuilderCache.Release(sb);
                            throw Win32Marshal.GetExceptionForWin32Error(errorCode);
                        }
                    }

                    currentDirectory = sb.ToString();
                }

                StringBuilderCache.Release(sb);
                return currentDirectory;
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

        public static string NewLine => "\r\n";

        private static int ProcessorCountFromSystemInfo
        {
            get
            {
                var info = default(Interop.Kernel32.SYSTEM_INFO);
                Interop.Kernel32.GetSystemInfo(out info);
                return info.dwNumberOfProcessors;
            }
        }

        public static int SystemPageSize
        {
            get
            {
                var info = default(Interop.Kernel32.SYSTEM_INFO);
                Interop.Kernel32.GetSystemInfo(out info);
                return info.dwPageSize;
            }
        }

        public static int ExitCode { get { return EnvironmentAugments.ExitCode; } set { EnvironmentAugments.ExitCode = value; } }

        private static string ExpandEnvironmentVariablesCore(string name)
        {
            int currentSize = 100;
            StringBuilder result = StringBuilderCache.Acquire(currentSize); // A somewhat reasonable default size

            result.Length = 0;
            int size = Interop.Kernel32.ExpandEnvironmentStringsW(name, result, currentSize);
            if (size == 0)
            {
                StringBuilderCache.Release(result);
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            while (size > currentSize)
            {
                currentSize = size;
                result.Length = 0;
                result.Capacity = currentSize;

                size = Interop.Kernel32.ExpandEnvironmentStringsW(name, result, currentSize);
                if (size == 0)
                {
                    StringBuilderCache.Release(result);
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
            }

            return StringBuilderCache.GetStringAndRelease(result);
        }

        private static bool Is64BitOperatingSystemWhen32BitProcess
        {
            get
            {
                bool isWow64;
                return Interop.Kernel32.IsWow64Process(Interop.Kernel32.GetCurrentProcess(), out isWow64) && isWow64;
            }
        }

        public static string MachineName
        {
            get
            {
                string name = Interop.Kernel32.GetComputerName();
                if (name == null)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_ComputerName);
                }
                return name;
            }
        }

        private static unsafe Lazy<OperatingSystem> s_osVersion = new Lazy<OperatingSystem>(() =>
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
        });

        public static int ProcessorCount
        {
            get
            {
                // First try GetLogicalProcessorInformationEx, caching the result as desktop/coreclr does.
                // If that fails for some reason, fall back to a non-cached result from GetSystemInfo.
                // (See SystemNative::GetProcessorCount in coreclr for a comparison.)
                int pc = s_processorCountFromGetLogicalProcessorInformationEx.Value;
                return pc != 0 ? pc : ProcessorCountFromSystemInfo;
            }
        }

        private static readonly unsafe Lazy<int> s_processorCountFromGetLogicalProcessorInformationEx = new Lazy<int>(() =>
        {
            // Determine how much size we need for a call to GetLogicalProcessorInformationEx
            uint len = 0;
            if (!Interop.Kernel32.GetLogicalProcessorInformationEx(Interop.Kernel32.LOGICAL_PROCESSOR_RELATIONSHIP.RelationGroup, IntPtr.Zero, ref len) &&
                Marshal.GetLastWin32Error() == Interop.Errors.ERROR_INSUFFICIENT_BUFFER)
            {
                // Allocate that much space
                Debug.Assert(len > 0);
                var buffer = new byte[len];
                fixed (byte* bufferPtr = buffer)
                {
                    // Call GetLogicalProcessorInformationEx with the allocated buffer
                    if (Interop.Kernel32.GetLogicalProcessorInformationEx(Interop.Kernel32.LOGICAL_PROCESSOR_RELATIONSHIP.RelationGroup, (IntPtr)bufferPtr, ref len))
                    {
                        // Walk each SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX in the buffer, where the Size of each dictates how
                        // much space it's consuming.  For each group relation, count the number of active processors in each of its group infos.
                        int processorCount = 0;
                        byte* ptr = bufferPtr, endPtr = bufferPtr + len;
                        while (ptr < endPtr)
                        {
                            var current = (Interop.Kernel32.SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX*)ptr;
                            if (current->Relationship == Interop.Kernel32.LOGICAL_PROCESSOR_RELATIONSHIP.RelationGroup)
                            {
                                Interop.Kernel32.PROCESSOR_GROUP_INFO* groupInfo = &current->Group.GroupInfo;
                                int groupCount = current->Group.ActiveGroupCount;
                                for (int i = 0; i < groupCount; i++)
                                {
                                    processorCount += (groupInfo + i)->ActiveProcessorCount;
                                }
                            }
                            ptr += current->Size;
                        }
                        return processorCount;
                    }
                }
            }

            return 0;
        });

        public static string SystemDirectory
        {
            get
            {
                // The path will likely be under 32 characters, e.g. C:\Windows\system32
                Span<char> buffer = stackalloc char[32];
                int requiredSize = Interop.Kernel32.GetSystemDirectoryW(buffer);

                if (requiredSize > buffer.Length)
                {
                    buffer = new char[requiredSize];
                    requiredSize = Interop.Kernel32.GetSystemDirectoryW(buffer);
                }

                if (requiredSize == 0)
                {
                    throw Win32Marshal.GetExceptionForLastWin32Error();
                }

                return new string(buffer.Slice(0, requiredSize));
            }
        }

        public static string UserName
        {
            get
            {
                string username = "Windows User";
                GetUserName(ref username);
                return username;
            }
        }

        static partial void GetUserName(ref string username);

        public static string UserDomainName
        {
            get
            {
                string userDomainName = "Windows Domain";
                GetDomainName(ref userDomainName);
                return userDomainName;
            }
        }

        static partial void GetDomainName(ref string userDomainName);
    }
}
