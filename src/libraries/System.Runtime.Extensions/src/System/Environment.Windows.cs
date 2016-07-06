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

        public static string[] GetLogicalDrives() => DriveInfoInternal.GetLogicalDrives();

        public static string NewLine => "\r\n";

        private static int ProcessorCountFromSystemInfo
        {
            get
            {
                var info = default(Interop.mincore.SYSTEM_INFO);
                Interop.mincore.GetSystemInfo(out info);
                return info.dwNumberOfProcessors;
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
    }
}
