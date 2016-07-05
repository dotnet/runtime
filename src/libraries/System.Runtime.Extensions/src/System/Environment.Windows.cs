// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace System
{
    public static partial class Environment
    {
        private static readonly Lazy<bool> s_isAppX = new Lazy<bool>(() =>
        {
            // TODO: Determine the right way to get at this information from outside of System.Private.Corelib
            try
            {
                Type appDomain = typeof(object).Assembly.GetType("System.AppDomain", throwOnError: true);
                bool isAppXModel = (bool)appDomain.GetMethod("IsAppXModel", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
                bool isAppXDesignMode = (bool)appDomain.GetMethod("IsAppXDesignMode", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
                return isAppXModel && !isAppXDesignMode;
            }
            catch { return true; }
        });

        private static string CurrentDirectoryCore
        {
            get
            {
                // TODO: Combine into Common with System.IO.FileSystem's Directory's implementation (or have it delegate to this one).

                StringBuilder sb = StringBuilderCache.Acquire(Interop.mincore.MAX_PATH + 1);
                if (Interop.mincore.GetCurrentDirectory(sb.Capacity, sb) == 0)
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
                    int r = Interop.mincore.GetLongPathName(currentDirectory, sb, sb.Capacity);
                    if (r == 0 || r >= Interop.mincore.MAX_PATH)
                    {
                        int errorCode = r >= Interop.mincore.MAX_PATH ?
                            Interop.mincore.Errors.ERROR_FILENAME_EXCED_RANGE :
                            Marshal.GetLastWin32Error();

                        if (errorCode != Interop.mincore.Errors.ERROR_FILE_NOT_FOUND &&
                            errorCode != Interop.mincore.Errors.ERROR_PATH_NOT_FOUND &&
                            errorCode != Interop.mincore.Errors.ERROR_INVALID_FUNCTION &&
                            errorCode != Interop.mincore.Errors.ERROR_ACCESS_DENIED)
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
                if (!Interop.mincore.SetCurrentDirectory(value))
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw Win32Marshal.GetExceptionForWin32Error(
                        errorCode == Interop.mincore.Errors.ERROR_FILE_NOT_FOUND ? Interop.mincore.Errors.ERROR_PATH_NOT_FOUND : errorCode, 
                        value);
                }
            }
        }

        private static string ExpandEnvironmentVariablesCore(string name)
        {
            if (s_isAppX.Value)
            {
                // If environment variables are not available, behave as if not defined.
                return name;
            }

            int currentSize = 100;
            StringBuilder result = StringBuilderCache.Acquire(currentSize); // A somewhat reasonable default size

            result.Length = 0;
            int size = Interop.mincore.ExpandEnvironmentStringsW(name, result, currentSize);
            if (size == 0)
            {
                StringBuilderCache.Release(result);
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            while (size > currentSize)
            {
                currentSize = size;
                result.Capacity = currentSize;
                result.Length = 0;

                size = Interop.mincore.ExpandEnvironmentStringsW(name, result, currentSize);
                if (size == 0)
                {
                    StringBuilderCache.Release(result);
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
            }

            return StringBuilderCache.GetStringAndRelease(result);
        }

        private static string GetFolderPathCore(SpecialFolder folder, SpecialFolderOption option)
        {
            // TODO: SHGetFolderPath is not available in the approved API list
            throw new PlatformNotSupportedException();
        }

        public static string[] GetLogicalDrives() => DriveInfoInternal.GetLogicalDrives();

        private static bool Is64BitOperatingSystemWhen32BitProcess
        {
            get
            {
                bool isWow64;
                return
                    !s_isAppX.Value &&
                    Interop.mincore.IsWow64Process(Interop.mincore.GetCurrentProcess(), out isWow64) && 
                    isWow64;
            }
        }

        public static string MachineName
        {
            get
            {
                if (s_isAppX.Value)
                {
                    throw new PlatformNotSupportedException();
                }

                string name = Interop.mincore.GetComputerName();
                if (name == null)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_ComputerName);
                }
                return name;
            }
        }

        public static string NewLine => "\r\n";

        private static Lazy<OperatingSystem> s_osVersion = new Lazy<OperatingSystem>(() =>
        {
            if (s_isAppX.Value)
            {
                // GetVersionExW isn't available.  We could throw a PlatformNotSupportedException, but we can
                // at least hand back Win32NT to highlight that we're on Windows rather than Unix.
                return new OperatingSystem(PlatformID.Win32NT, new Version(0, 0));
            }

            var version = new Interop.mincore.OSVERSIONINFOEX { dwOSVersionInfoSize = Marshal.SizeOf<Interop.mincore.OSVERSIONINFOEX>() };
            if (!Interop.mincore.GetVersionExW(ref version))
            {
                throw new InvalidOperationException(SR.InvalidOperation_GetVersion);
            }

            return new OperatingSystem(
                PlatformID.Win32NT,
                new Version(version.dwMajorVersion, version.dwMinorVersion, version.dwBuildNumber, (version.wServicePackMajor << 16) | version.wServicePackMinor),
                version.szCSDVersion);
        });


        public static int ProcessorCount
        {
            get
            {
                if (!s_isAppX.Value)
                {
                    // TODO: Need to use GetLogicalProcessorInformationEx to get the number of
                    // cores for when there's more than one CPU group, e.g. > 64 cores
                }

                var info = default(Interop.mincore.SYSTEM_INFO);
                Interop.mincore.GetSystemInfo(out info);
                return info.dwNumberOfProcessors;
            }
        }

        public static string SystemDirectory
        {
            get
            {
                if (s_isAppX.Value)
                {
                    throw new PlatformNotSupportedException();
                }

                StringBuilder sb = StringBuilderCache.Acquire(Path.MaxPath);
                if (Interop.mincore.GetSystemDirectoryW(sb, Path.MaxPath) == 0)
                {
                    StringBuilderCache.Release(sb);
                    throw Win32Marshal.GetExceptionForLastWin32Error();
                }
                return StringBuilderCache.GetStringAndRelease(sb);
            }
        }

        public static int SystemPageSize
        {
            get
            {
                var info = default(Interop.mincore.SYSTEM_INFO);
                Interop.mincore.GetSystemInfo(out info);
                return info.dwPageSize;
            }
        }

        public static int TickCount => (int)Interop.mincore.GetTickCount64();

        public static string UserName
        {
            get
            {
                if (s_isAppX.Value)
                {
                    throw new PlatformNotSupportedException();
                }

                const int UNLEN = 254;
                StringBuilder sb = StringBuilderCache.Acquire(UNLEN + 1);
                int size = sb.Capacity;

                try
                {
                    if (Interop.mincore.GetUserNameW(sb, ref size))
                    {
                        return StringBuilderCache.GetStringAndRelease(sb);
                    }
                }
                catch (EntryPointNotFoundException)
                {
                    // not available on Windows 7
                }

                StringBuilderCache.Release(sb);
                return string.Empty;
            }
        }

        public static string UserDomainName
        {
            get
            {
                if (s_isAppX.Value)
                {
                    throw new PlatformNotSupportedException();
                }

                var domainName = new StringBuilder(1024);
                uint domainNameLen = (uint)domainName.Capacity;
                if (Interop.mincore.GetUserNameExW(Interop.mincore.NameSamCompatible, domainName, ref domainNameLen) == 1)
                {
                    string samName = domainName.ToString();
                    int index = samName.IndexOf('\\');
                    if (index != -1)
                    {
                        return samName.Substring(0, index);
                    }
                }
                domainNameLen = (uint)domainName.Capacity;

                byte[] sid = new byte[1024];
                int sidLen = sid.Length;
                int peUse;
                if (!Interop.mincore.LookupAccountNameW(null, UserName, sid, ref sidLen, domainName, ref domainNameLen, out peUse))
                {
                    throw new InvalidOperationException(Win32Marshal.GetExceptionForLastWin32Error().Message);
                }

                return domainName.ToString();
            }
        }
    }
}
