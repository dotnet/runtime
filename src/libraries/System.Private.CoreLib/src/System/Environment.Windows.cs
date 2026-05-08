// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32.SafeHandles;
using Internal.Win32;

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
                builder.EnsureCapacity(builder.Capacity * 2);
            }

            if (length == 0)
                throw Win32Marshal.GetExceptionForLastWin32Error();

            builder.Length = (int)length;
            return builder.ToString();
        }

        private static OperatingSystem GetOSVersion()
        {
            if (Interop.NtDll.RtlGetVersionEx(out Interop.NtDll.RTL_OSVERSIONINFOEX osvi) != 0)
            {
                throw new InvalidOperationException(SR.InvalidOperation_GetVersion);
            }

            var version = new Version((int)osvi.dwMajorVersion, (int)osvi.dwMinorVersion, (int)osvi.dwBuildNumber, 0);

            if (osvi.szCSDVersion[0] != '\0')
            {
                ReadOnlySpan<char> csd = osvi.szCSDVersion;
                int idx = csd.IndexOf('\0');
                return new OperatingSystem(PlatformID.Win32NT, version, new string(idx >= 0 ? csd[..idx] : csd));
            }

            return new OperatingSystem(PlatformID.Win32NT, version);
        }

        private static string? s_systemDirectory;

        public static string SystemDirectory => s_systemDirectory ??= GetSystemDirectory();

        private static string GetSystemDirectory()
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
            // contribute to character_count.

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

        /// <summary>
        /// Get the CPU usage, including the process time spent running the application code, the process time spent running the operating system code,
        /// and the total time spent running both the application and operating system code.
        /// </summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("browser")]
        [SupportedOSPlatform("maccatalyst")]
        public static ProcessCpuUsage CpuUsage
        {
            get => Interop.Kernel32.GetProcessTimes(Interop.Kernel32.GetCurrentProcess(), out _, out _, out long procKernelTime, out long procUserTime) ?
                    new ProcessCpuUsage { UserTime = new TimeSpan(procUserTime), PrivilegedTime = new TimeSpan(procKernelTime) } :
                    new ProcessCpuUsage { UserTime = TimeSpan.Zero, PrivilegedTime = TimeSpan.Zero };
        }

        /// <summary>Gets the number of milliseconds elapsed since the system started.</summary>
        /// <value>A 64-bit signed integer containing the amount of time in milliseconds that has passed since the last time the computer was started.</value>
        public static long TickCount64
        {
            get
            {
                unsafe
                {
                    // GetTickCount64 uses fixed resolution of 10-16ms for backward compatibility. Use
                    // QueryUnbiasedInterruptTime instead which becomes more accurate if the underlying system
                    // resolution is improved. This helps responsiveness in the case an app is trying to opt
                    // into things like multimedia scenarios and additionally does not include "bias" from time
                    // the system is spent asleep or in hibernation.

                    ulong unbiasedTime;

                    Interop.BOOL result = Interop.Kernel32.QueryUnbiasedInterruptTime(&unbiasedTime);
                    // The P/Invoke is documented to only fail if a null-ptr is passed in
                    Debug.Assert(result != Interop.BOOL.FALSE);

                    return (long)(unbiasedTime / TimeSpan.TicksPerMillisecond);
                }
            }
        }

        private static string? GetEnvironmentVariableFromRegistry(string variable, bool fromMachine)
        {
            Debug.Assert(variable != null);

            using (RegistryKey? environmentKey = OpenEnvironmentKeyIfExists(fromMachine, writable: false))
            {
                return environmentKey?.GetValue(variable) as string;
            }
        }

        private static void SetEnvironmentVariableFromRegistry(string variable, string? value, bool fromMachine)
        {
            Debug.Assert(variable != null);

            const int MaxUserEnvVariableLength = 255; // User-wide env vars stored in the registry have names limited to 255 chars
            if (!fromMachine && variable.Length >= MaxUserEnvVariableLength)
            {
                throw new ArgumentException(SR.Argument_LongEnvVarValue, nameof(variable));
            }

            using (RegistryKey? environmentKey = OpenEnvironmentKeyIfExists(fromMachine, writable: true))
            {
                if (environmentKey != null)
                {
                    if (value == null)
                    {
                        environmentKey.DeleteValue(variable, throwOnMissingValue: false);
                    }
                    else
                    {
                        environmentKey.SetValue(variable, value);
                    }
                }
            }

            unsafe
            {
                // send a WM_SETTINGCHANGE message to all windows
                fixed (char* lParam = "Environment")
                {
                    IntPtr unused;
                    IntPtr r = Interop.User32.SendMessageTimeout(new IntPtr(Interop.User32.HWND_BROADCAST), Interop.User32.WM_SETTINGCHANGE, IntPtr.Zero, (IntPtr)lParam, 0, 1000, &unused);

                    // SendMessageTimeout message is a empty stub on Windows Nano Server that fails with both result and last error 0.
                    Debug.Assert(r != IntPtr.Zero || Marshal.GetLastPInvokeError() == 0, $"SetEnvironmentVariable failed: {Marshal.GetLastPInvokeError()}");
                }
            }
        }

        private static Hashtable GetEnvironmentVariablesFromRegistry(bool fromMachine)
        {
            var results = new Hashtable();

            using (RegistryKey? environmentKey = OpenEnvironmentKeyIfExists(fromMachine, writable: false))
            {
                if (environmentKey != null)
                {
                    foreach (string name in environmentKey.GetValueNames())
                    {
                        string? value = environmentKey.GetValue(name, "").ToString();
                        try
                        {
                            results.Add(name, value);
                        }
                        catch (ArgumentException)
                        {
                            // Throw and catch intentionally to provide non-fatal notification about corrupted environment block
                        }
                    }
                }
            }

            return results;
        }

        private static RegistryKey? OpenEnvironmentKeyIfExists(bool fromMachine, bool writable)
        {
            RegistryKey baseKey;
            string keyName;

            if (fromMachine)
            {
                baseKey = Registry.LocalMachine;
                keyName = @"System\CurrentControlSet\Control\Session Manager\Environment";
            }
            else
            {
                baseKey = Registry.CurrentUser;
                keyName = "Environment";
            }

            return baseKey.OpenSubKey(keyName, writable: writable);
        }

        public static string UserName
        {
            get
            {
                // 40 should be enough as we're asking for the SAM compatible name (DOMAIN\User).
                // The max length should be 15 (domain) + 1 (separator) + 20 (name) + null. If for
                // some reason it isn't, we'll grow the buffer.

                // https://support.microsoft.com/en-us/help/909264/naming-conventions-in-active-directory-for-computers-domains-sites-and
                // https://msdn.microsoft.com/en-us/library/ms679635.aspx

                var builder = new ValueStringBuilder(stackalloc char[40]);
                GetUserName(ref builder);

                ReadOnlySpan<char> name = builder.AsSpan();
                int index = name.IndexOf('\\');
                if (index >= 0)
                {
                    // In the form of DOMAIN\User, cut off DOMAIN\
                    name = name.Slice(index + 1);
                }

                string result = name.ToString();
                builder.Dispose();
                return result;
            }
        }

        private static void GetUserName(ref ValueStringBuilder builder)
        {
            uint size = 0;
            while (Interop.Secur32.GetUserNameExW(Interop.Secur32.NameSamCompatible, ref builder.GetPinnableReference(), ref size) == Interop.BOOLEAN.FALSE)
            {
                if (Marshal.GetLastPInvokeError() == Interop.Errors.ERROR_MORE_DATA)
                {
                    builder.EnsureCapacity(checked((int)size));
                }
                else
                {
                    builder.Length = 0;
                    return;
                }
            }

            builder.Length = (int)size;
        }

        public static string UserDomainName
        {
            get
            {
                // See the comment in UserName
                var builder = new ValueStringBuilder(stackalloc char[40]);
                GetUserName(ref builder);

                ReadOnlySpan<char> name = builder.AsSpan();
                int index = name.IndexOf('\\');
                if (index >= 0)
                {
                    // In the form of DOMAIN\User, cut off \User and return
                    builder.Length = index;
                    return builder.ToString();
                }

                // In theory we should never get use out of LookupAccountNameW as the above API should
                // always return what we need. Can't find any clues in the historical sources, however.

                // Domain names aren't typically long.
                // https://support.microsoft.com/en-us/help/909264/naming-conventions-in-active-directory-for-computers-domains-sites-and
                var domainBuilder = new ValueStringBuilder(stackalloc char[64]);
                uint length = (uint)domainBuilder.Capacity;

                // This API will fail to return the domain name without a buffer for the SID.
                // SIDs are never over 68 bytes long.
                Span<byte> sid = stackalloc byte[68];
                uint sidLength = 68;

                while (!Interop.Advapi32.LookupAccountNameW(null, ref builder.GetPinnableReference(), ref MemoryMarshal.GetReference(sid),
                    ref sidLength, ref domainBuilder.GetPinnableReference(), ref length, out _))
                {
                    int error = Marshal.GetLastPInvokeError();

                    // The docs don't call this out clearly, but experimenting shows that the error returned is the following.
                    if (error != Interop.Errors.ERROR_INSUFFICIENT_BUFFER)
                    {
                        throw new InvalidOperationException(Marshal.GetPInvokeErrorMessage(error));
                    }

                    domainBuilder.EnsureCapacity((int)length);
                }

                builder.Dispose();
                domainBuilder.Length = (int)length;
                return domainBuilder.ToString();
            }
        }

        private static string GetFolderPathCore(SpecialFolder folder, SpecialFolderOption option)
        {
            // We're using SHGetKnownFolderPath instead of SHGetFolderPath as SHGetFolderPath is
            // capped at MAX_PATH.
            //
            // Because we validate both of the input enums we shouldn't have to care about CSIDL and flag
            // definitions we haven't mapped. If we remove or loosen the checks we'd have to account
            // for mapping here (this includes tweaking as SHGetFolderPath would do).
            //
            // The only SpecialFolderOption defines we have are equivalent to KnownFolderFlags.

            ReadOnlySpan<byte> folderGuid;
            string? fallbackEnv = null;
            switch (folder)
            {
                // Special-cased values to not use SHGetFolderPath when we have a more direct option available.
                case SpecialFolder.System:
                    // This assumes the system directory always exists and thus we don't need to do anything special for any SpecialFolderOption.
                    return SystemDirectory;

                // Map the SpecialFolder to the appropriate Guid
                case SpecialFolder.ApplicationData:
                    folderGuid = Interop.Shell32.KnownFolders.RoamingAppData;
                    fallbackEnv = "APPDATA";
                    break;
                case SpecialFolder.CommonApplicationData:
                    folderGuid = Interop.Shell32.KnownFolders.ProgramData;
                    fallbackEnv = "ProgramData";
                    break;
                case SpecialFolder.LocalApplicationData:
                    folderGuid = Interop.Shell32.KnownFolders.LocalAppData;
                    fallbackEnv = "LOCALAPPDATA";
                    break;
                case SpecialFolder.Cookies:
                    folderGuid = Interop.Shell32.KnownFolders.Cookies;
                    break;
                case SpecialFolder.Desktop:
                    folderGuid = Interop.Shell32.KnownFolders.Desktop;
                    break;
                case SpecialFolder.Favorites:
                    folderGuid = Interop.Shell32.KnownFolders.Favorites;
                    break;
                case SpecialFolder.History:
                    folderGuid = Interop.Shell32.KnownFolders.History;
                    break;
                case SpecialFolder.InternetCache:
                    folderGuid = Interop.Shell32.KnownFolders.InternetCache;
                    break;
                case SpecialFolder.Programs:
                    folderGuid = Interop.Shell32.KnownFolders.Programs;
                    break;
                case SpecialFolder.MyComputer:
                    folderGuid = Interop.Shell32.KnownFolders.ComputerFolder;
                    break;
                case SpecialFolder.MyMusic:
                    folderGuid = Interop.Shell32.KnownFolders.Music;
                    break;
                case SpecialFolder.MyPictures:
                    folderGuid = Interop.Shell32.KnownFolders.Pictures;
                    break;
                case SpecialFolder.MyVideos:
                    folderGuid = Interop.Shell32.KnownFolders.Videos;
                    break;
                case SpecialFolder.Recent:
                    folderGuid = Interop.Shell32.KnownFolders.Recent;
                    break;
                case SpecialFolder.SendTo:
                    folderGuid = Interop.Shell32.KnownFolders.SendTo;
                    break;
                case SpecialFolder.StartMenu:
                    folderGuid = Interop.Shell32.KnownFolders.StartMenu;
                    break;
                case SpecialFolder.Startup:
                    folderGuid = Interop.Shell32.KnownFolders.Startup;
                    break;
                case SpecialFolder.Templates:
                    folderGuid = Interop.Shell32.KnownFolders.Templates;
                    break;
                case SpecialFolder.DesktopDirectory:
                    folderGuid = Interop.Shell32.KnownFolders.Desktop;
                    break;
                case SpecialFolder.Personal:
                    // Same as Personal
                    // case SpecialFolder.MyDocuments:
                    folderGuid = Interop.Shell32.KnownFolders.Documents;
                    break;
                case SpecialFolder.ProgramFiles:
                    folderGuid = Interop.Shell32.KnownFolders.ProgramFiles;
                    fallbackEnv = "ProgramFiles";
                    break;
                case SpecialFolder.CommonProgramFiles:
                    folderGuid = Interop.Shell32.KnownFolders.ProgramFilesCommon;
                    fallbackEnv = "CommonProgramFiles";
                    break;
                case SpecialFolder.AdminTools:
                    folderGuid = Interop.Shell32.KnownFolders.AdminTools;
                    break;
                case SpecialFolder.CDBurning:
                    folderGuid = Interop.Shell32.KnownFolders.CDBurning;
                    break;
                case SpecialFolder.CommonAdminTools:
                    folderGuid = Interop.Shell32.KnownFolders.CommonAdminTools;
                    break;
                case SpecialFolder.CommonDocuments:
                    folderGuid = Interop.Shell32.KnownFolders.PublicDocuments;
                    break;
                case SpecialFolder.CommonMusic:
                    folderGuid = Interop.Shell32.KnownFolders.PublicMusic;
                    break;
                case SpecialFolder.CommonOemLinks:
                    folderGuid = Interop.Shell32.KnownFolders.CommonOEMLinks;
                    break;
                case SpecialFolder.CommonPictures:
                    folderGuid = Interop.Shell32.KnownFolders.PublicPictures;
                    break;
                case SpecialFolder.CommonStartMenu:
                    folderGuid = Interop.Shell32.KnownFolders.CommonStartMenu;
                    break;
                case SpecialFolder.CommonPrograms:
                    folderGuid = Interop.Shell32.KnownFolders.CommonPrograms;
                    break;
                case SpecialFolder.CommonStartup:
                    folderGuid = Interop.Shell32.KnownFolders.CommonStartup;
                    break;
                case SpecialFolder.CommonDesktopDirectory:
                    folderGuid = Interop.Shell32.KnownFolders.PublicDesktop;
                    break;
                case SpecialFolder.CommonTemplates:
                    folderGuid = Interop.Shell32.KnownFolders.CommonTemplates;
                    break;
                case SpecialFolder.CommonVideos:
                    folderGuid = Interop.Shell32.KnownFolders.PublicVideos;
                    break;
                case SpecialFolder.Fonts:
                    folderGuid = Interop.Shell32.KnownFolders.Fonts;
                    break;
                case SpecialFolder.NetworkShortcuts:
                    folderGuid = Interop.Shell32.KnownFolders.NetHood;
                    break;
                case SpecialFolder.PrinterShortcuts:
                    folderGuid = Interop.Shell32.KnownFolders.PrintersFolder;
                    break;
                case SpecialFolder.UserProfile:
                    folderGuid = Interop.Shell32.KnownFolders.Profile;
                    fallbackEnv = "USERPROFILE";
                    break;
                case SpecialFolder.CommonProgramFilesX86:
                    folderGuid = Interop.Shell32.KnownFolders.ProgramFilesCommonX86;
                    fallbackEnv = "CommonProgramFiles(x86)";
                    break;
                case SpecialFolder.ProgramFilesX86:
                    folderGuid = Interop.Shell32.KnownFolders.ProgramFilesX86;
                    fallbackEnv = "ProgramFiles(x86)";
                    break;
                case SpecialFolder.Resources:
                    folderGuid = Interop.Shell32.KnownFolders.ResourceDir;
                    break;
                case SpecialFolder.LocalizedResources:
                    folderGuid = Interop.Shell32.KnownFolders.LocalizedResourcesDir;
                    break;
                case SpecialFolder.SystemX86:
                    folderGuid = Interop.Shell32.KnownFolders.SystemX86;
                    break;
                case SpecialFolder.Windows:
                    folderGuid = Interop.Shell32.KnownFolders.Windows;
                    fallbackEnv = "windir";
                    break;
                default:
                    Debug.Assert(!Enum.IsDefined(folder), $"Unexpected SpecialFolder value: {folder}. Please ensure all SpecialFolder enum values are handled in the switch statement.");
                    throw new ArgumentOutOfRangeException(nameof(folder), folder, SR.Format(SR.Arg_EnumIllegalVal, folder));
            }

            Guid folderId = new Guid(folderGuid);
            int hr = Interop.Shell32.SHGetKnownFolderPath(folderId, (uint)option, IntPtr.Zero, out string path);
            if (hr == 0)
                return path;

            // Fallback logic if SHGetKnownFolderPath failed (nanoserver)
            return fallbackEnv != null ? Environment.GetEnvironmentVariable(fallbackEnv) ?? string.Empty : string.Empty;
        }
    }
}
