// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

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
                    int errorCode = Marshal.GetLastPInvokeError();
                    throw Win32Marshal.GetExceptionForWin32Error(
                        errorCode == Interop.Errors.ERROR_FILE_NOT_FOUND ? Interop.Errors.ERROR_PATH_NOT_FOUND : errorCode,
                        value);
                }
            }
        }

        public static string[] GetLogicalDrives() => DriveInfoInternal.GetLogicalDrives();

        internal const string NewLineConst = "\r\n";

        private static int GetSystemPageSize()
        {
            Interop.Kernel32.SYSTEM_INFO info;
            unsafe
            {
                Interop.Kernel32.GetSystemInfo(&info);
            }

            return info.dwPageSize;
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
                throw Win32Marshal.GetExceptionForLastWin32Error();

            // length includes the null terminator
            builder.Length = (int)length - 1;
            return builder.ToString();
        }

        private static unsafe bool IsPrivilegedProcessCore()
        {
            SafeTokenHandle? token = null;
            try
            {
                if (Interop.Advapi32.OpenProcessToken(Interop.Kernel32.GetCurrentProcess(), (int)Interop.Advapi32.TOKEN_ACCESS_LEVELS.Read, out token))
                {
                    Interop.Advapi32.TOKEN_ELEVATION elevation = default;

                    if (Interop.Advapi32.GetTokenInformation(
                            token,
                            Interop.Advapi32.TOKEN_INFORMATION_CLASS.TokenElevation,
                            &elevation,
                            (uint)sizeof(Interop.Advapi32.TOKEN_ELEVATION),
                            out _))
                    {
                        return elevation.TokenIsElevated != Interop.BOOL.FALSE;
                    }
                }

                throw Win32Marshal.GetExceptionForLastWin32Error();
            }
            finally
            {
                token?.Dispose();
            }
        }

        private static bool Is64BitOperatingSystemWhen32BitProcess =>
            Interop.Kernel32.IsWow64Process(Interop.Kernel32.GetCurrentProcess(), out bool isWow64) && isWow64;

        public static string MachineName =>
            Interop.Kernel32.GetComputerName() ??
            throw new InvalidOperationException(SR.InvalidOperation_ComputerName);

        [MethodImpl(MethodImplOptions.NoInlining)] // Avoid inlining PInvoke frame into the hot path
        private static int GetProcessId() => unchecked((int)Interop.Kernel32.GetCurrentProcessId());

        private static string? GetProcessPath()
        {
            var builder = new ValueStringBuilder(stackalloc char[Interop.Kernel32.MAX_PATH]);

            uint length;
            while ((length = Interop.Kernel32.GetModuleFileName(IntPtr.Zero, ref builder.GetPinnableReference(), (uint)builder.Capacity)) >= builder.Capacity)
            {
                builder.EnsureCapacity((int)length);
            }

            if (length == 0)
                throw Win32Marshal.GetExceptionForLastWin32Error();

            builder.Length = (int)length;
            return builder.ToString();
        }

        private static unsafe OperatingSystem GetOSVersion()
        {
            if (Interop.NtDll.RtlGetVersionEx(out Interop.NtDll.RTL_OSVERSIONINFOEX osvi) != 0)
            {
                throw new InvalidOperationException(SR.InvalidOperation_GetVersion);
            }

            var version = new Version((int)osvi.dwMajorVersion, (int)osvi.dwMinorVersion, (int)osvi.dwBuildNumber, 0);

            return osvi.szCSDVersion[0] != '\0' ?
                new OperatingSystem(PlatformID.Win32NT, version, new string(&osvi.szCSDVersion[0])) :
                new OperatingSystem(PlatformID.Win32NT, version);
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

        public static unsafe bool UserInteractive
        {
            get
            {
                // Per documentation of GetProcessWindowStation, this handle should not be closed
                IntPtr handle = Interop.User32.GetProcessWindowStation();
                if (handle != IntPtr.Zero)
                {
                    Interop.User32.USEROBJECTFLAGS flags = default;
                    uint dummy = 0;
                    if (Interop.User32.GetUserObjectInformationW(handle, Interop.User32.UOI_FLAGS, &flags, (uint)sizeof(Interop.User32.USEROBJECTFLAGS), ref dummy))
                    {
                        return ((flags.dwFlags & Interop.User32.WSF_VISIBLE) != 0);
                    }
                }

                // If we can't determine, return true optimistically
                // This will include cases like Windows Nano which do not expose WindowStations
                return true;
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

        private static unsafe string[] GetCommandLineArgsNative()
        {
            char* lpCmdLine = Interop.Kernel32.GetCommandLine();
            Debug.Assert(lpCmdLine != null);

            return SegmentCommandLine(lpCmdLine);
        }

        private static unsafe string[] SegmentCommandLine(char* cmdLine)
        {
            // Parse command line arguments using the rules documented at
            // https://learn.microsoft.com/cpp/cpp/main-function-command-line-args#parsing-c-command-line-arguments

            // CommandLineToArgvW API cannot be used here since
            // it has slightly different behavior.

            ArrayBuilder<string> arrayBuilder = default;

            Span<char> stringBuffer = stackalloc char[260]; // Use MAX_PATH for a typical maximum
            scoped ValueStringBuilder stringBuilder;

            char c;

            // First scan the program name, copy it, and count the bytes

            char* p = cmdLine;

            // A quoted program name is handled here. The handling is much
            // simpler than for other arguments. Basically, whatever lies
            // between the leading double-quote and next one, or a terminal null
            // character is simply accepted. Fancier handling is not required
            // because the program name must be a legal NTFS/HPFS file name.
            // Note that the double-quote characters are not copied, nor do they
            // contribyte to character_count.

            bool inQuotes = false;
            stringBuilder = new ValueStringBuilder(stringBuffer);

            do
            {
                if (*p == '"')
                {
                    inQuotes = !inQuotes;
                    c = *p++;
                    continue;
                }

                c = *p++;
                stringBuilder.Append(c);
            }
            while (c != '\0' && (inQuotes || (c is not (' ' or '\t'))));

            if (c == '\0')
            {
                p--;
            }

            stringBuilder.Length--;
            arrayBuilder.Add(stringBuilder.ToString());
            inQuotes = false;

            // loop on each argument
            while (true)
            {
                if (*p != '\0')
                {
                    while (*p is ' ' or '\t')
                    {
                        ++p;
                    }
                }

                if (*p == '\0')
                {
                    // end of args
                    break;
                }

                // scan an argument
                stringBuilder = new ValueStringBuilder(stringBuffer);

                // loop through scanning one argument
                while (true)
                {
                    bool copyChar = true;

                    // Rules:
                    // 2N   backslashes + " ==> N backslashes and begin/end quote
                    // 2N+1 backslashes + " ==> N backslashes + literal "
                    // N    backslashes     ==> N backslashes
                    int numSlash = 0;

                    while (*p == '\\')
                    {
                        // Count number of backslashes for use below
                        ++p;
                        ++numSlash;
                    }

                    if (*p == '"')
                    {
                        // if 2N backslashes before, start / end quote, otherwise
                        // copy literally:
                        if (numSlash % 2 == 0)
                        {
                            if (inQuotes && p[1] == '"')
                            {
                                p++; // Double quote inside quoted string
                            }
                            else
                            {
                                // Skip first quote char and copy second:
                                copyChar = false;       // Don't copy quote
                                inQuotes = !inQuotes;
                            }
                        }

                        numSlash /= 2;
                    }

                    // Copy slashes:
                    while (numSlash-- > 0)
                    {
                        stringBuilder.Append('\\');
                    }

                    // If at end of arg, break loop:
                    if (*p == '\0' || (!inQuotes && *p is ' ' or '\t'))
                    {
                        break;
                    }

                    // Copy character into argument:
                    if (copyChar)
                    {
                        stringBuilder.Append(*p);
                    }

                    ++p;
                }

                arrayBuilder.Add(stringBuilder.ToString());
            }

            return arrayBuilder.ToArray();
        }
    }
}
