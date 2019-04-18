// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Internal.Win32;

namespace System
{
    public static partial class Environment
    {
        internal static bool IsWindows8OrAbove => WindowsVersion.IsWindows8OrAbove;

        private static string? GetEnvironmentVariableFromRegistry(string variable, bool fromMachine)
        {
            Debug.Assert(variable != null);

#if FEATURE_APPX
            if (ApplicationModel.IsUap)
                return null; // Systems without the Windows registry pretend that it's always empty.
#endif

            using (RegistryKey? environmentKey = OpenEnvironmentKeyIfExists(fromMachine, writable: false))
            {
                return environmentKey?.GetValue(variable) as string;
            }
        }

        private static void SetEnvironmentVariableFromRegistry(string variable, string? value, bool fromMachine)
        {
            Debug.Assert(variable != null);

#if FEATURE_APPX
            if (ApplicationModel.IsUap)
                return; // Systems without the Windows registry pretend that it's always empty.
#endif

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

            // send a WM_SETTINGCHANGE message to all windows
            IntPtr r = Interop.User32.SendMessageTimeout(new IntPtr(Interop.User32.HWND_BROADCAST), Interop.User32.WM_SETTINGCHANGE, IntPtr.Zero, "Environment", 0, 1000, IntPtr.Zero);
            Debug.Assert(r != IntPtr.Zero, "SetEnvironmentVariable failed: " + Marshal.GetLastWin32Error());
        }

        private static IDictionary GetEnvironmentVariablesFromRegistry(bool fromMachine)
        {
            var results = new Hashtable();
#if FEATURE_APPX
            if (ApplicationModel.IsUap) // Systems without the Windows registry pretend that it's always empty.
                return results;
#endif

            using (RegistryKey? environmentKey = OpenEnvironmentKeyIfExists(fromMachine, writable: false))
            {
                if (environmentKey != null)
                {
                    foreach (string name in environmentKey.GetValueNames())
                    {
                        string? value = environmentKey.GetValue(name, "")!.ToString(); // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761
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
#if FEATURE_APPX
                if (ApplicationModel.IsUap)
                    return "Windows User";
#endif

                // 40 should be enough as we're asking for the SAM compatible name (DOMAIN\User).
                // The max length should be 15 (domain) + 1 (separator) + 20 (name) + null. If for
                // some reason it isn't, we'll grow the buffer.

                // https://support.microsoft.com/en-us/help/909264/naming-conventions-in-active-directory-for-computers-domains-sites-and
                // https://msdn.microsoft.com/en-us/library/ms679635.aspx

                Span<char> initialBuffer = stackalloc char[40];
                var builder = new ValueStringBuilder(initialBuffer);
                GetUserName(ref builder);

                ReadOnlySpan<char> name = builder.AsSpan();
                int index = name.IndexOf('\\');
                if (index != -1)
                {
                    // In the form of DOMAIN\User, cut off DOMAIN\
                    name = name.Slice(index + 1);
                }

                return name.ToString();
            }
        }

        private static void GetUserName(ref ValueStringBuilder builder)
        {
            uint size = 0;
            while (Interop.Secur32.GetUserNameExW(Interop.Secur32.NameSamCompatible, ref builder.GetPinnableReference(), ref size) == Interop.BOOLEAN.FALSE)
            {
                if (Marshal.GetLastWin32Error() == Interop.Errors.ERROR_MORE_DATA)
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
#if FEATURE_APPX
                if (ApplicationModel.IsUap)
                    return "Windows Domain";
#endif

                // See the comment in UserName
                Span<char> initialBuffer = stackalloc char[40];
                var builder = new ValueStringBuilder(initialBuffer);
                GetUserName(ref builder);

                ReadOnlySpan<char> name = builder.AsSpan();
                int index = name.IndexOf('\\');
                if (index != -1)
                {
                    // In the form of DOMAIN\User, cut off \User and return
                    return name.Slice(0, index).ToString();
                }

                // In theory we should never get use out of LookupAccountNameW as the above API should
                // always return what we need. Can't find any clues in the historical sources, however.

                // Domain names aren't typically long.
                // https://support.microsoft.com/en-us/help/909264/naming-conventions-in-active-directory-for-computers-domains-sites-and
                Span<char> initialDomainNameBuffer = stackalloc char[64];
                var domainBuilder = new ValueStringBuilder(initialBuffer);
                uint length = (uint)domainBuilder.Capacity;

                // This API will fail to return the domain name without a buffer for the SID.
                // SIDs are never over 68 bytes long.
                Span<byte> sid = stackalloc byte[68];
                uint sidLength = 68;

                while (!Interop.Advapi32.LookupAccountNameW(null, ref builder.GetPinnableReference(), ref MemoryMarshal.GetReference(sid),
                    ref sidLength, ref domainBuilder.GetPinnableReference(), ref length, out _))
                {
                    int error = Marshal.GetLastWin32Error();

                    // The docs don't call this out clearly, but experimenting shows that the error returned is the following.
                    if (error != Interop.Errors.ERROR_INSUFFICIENT_BUFFER)
                    {
                        throw new InvalidOperationException(Win32Marshal.GetMessage(error));
                    }

                    domainBuilder.EnsureCapacity((int)length);
                }

                domainBuilder.Length = (int)length;
                return domainBuilder.ToString();
            }
        }

        private static string GetFolderPathCore(SpecialFolder folder, SpecialFolderOption option)
        {
#if FEATURE_APPX
            if (ApplicationModel.IsUap)
                return WinRTFolderPaths.GetFolderPath(folder, option);
#endif

            // We're using SHGetKnownFolderPath instead of SHGetFolderPath as SHGetFolderPath is
            // capped at MAX_PATH.
            //
            // Because we validate both of the input enums we shouldn't have to care about CSIDL and flag
            // definitions we haven't mapped. If we remove or loosen the checks we'd have to account
            // for mapping here (this includes tweaking as SHGetFolderPath would do).
            //
            // The only SpecialFolderOption defines we have are equivalent to KnownFolderFlags.

            string folderGuid;

            switch (folder)
            {
                case SpecialFolder.ApplicationData:
                    folderGuid = Interop.Shell32.KnownFolders.RoamingAppData;
                    break;
                case SpecialFolder.CommonApplicationData:
                    folderGuid = Interop.Shell32.KnownFolders.ProgramData;
                    break;
                case SpecialFolder.LocalApplicationData:
                    folderGuid = Interop.Shell32.KnownFolders.LocalAppData;
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
                case SpecialFolder.System:
                    folderGuid = Interop.Shell32.KnownFolders.System;
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
                    break;
                case SpecialFolder.CommonProgramFiles:
                    folderGuid = Interop.Shell32.KnownFolders.ProgramFilesCommon;
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
                    break;
                case SpecialFolder.CommonProgramFilesX86:
                    folderGuid = Interop.Shell32.KnownFolders.ProgramFilesCommonX86;
                    break;
                case SpecialFolder.ProgramFilesX86:
                    folderGuid = Interop.Shell32.KnownFolders.ProgramFilesX86;
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
                    break;
                default:
                    return string.Empty;
            }

            return GetKnownFolderPath(folderGuid, option);
        }

        private static string GetKnownFolderPath(string folderGuid, SpecialFolderOption option)
        {
            Guid folderId = new Guid(folderGuid);

            int hr = Interop.Shell32.SHGetKnownFolderPath(folderId, (uint)option, IntPtr.Zero, out string path);
            if (hr != 0) // Not S_OK
            {
                return string.Empty;
            }

            return path;
        }

#if FEATURE_APPX
        private static class WinRTFolderPaths
        {
            private static Func<SpecialFolder, SpecialFolderOption, string> s_winRTFolderPathsGetFolderPath;

            public static string GetFolderPath(SpecialFolder folder, SpecialFolderOption option)
            {
                if (s_winRTFolderPathsGetFolderPath == null)
                {
                    Type winRtFolderPathsType = Type.GetType("System.WinRTFolderPaths, System.Runtime.WindowsRuntime, Version=4.0.14.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", throwOnError: false);
                    MethodInfo? getFolderPathsMethod = winRtFolderPathsType?.GetMethod("GetFolderPath", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(SpecialFolder), typeof(SpecialFolderOption) }, null);
                    var d = (Func<SpecialFolder, SpecialFolderOption, string>?)getFolderPathsMethod?.CreateDelegate(typeof(Func<SpecialFolder, SpecialFolderOption, string>));
                    s_winRTFolderPathsGetFolderPath = d ?? delegate { return string.Empty; };
                }

                return s_winRTFolderPathsGetFolderPath(folder, option);
            }
        }
#endif

        // Seperate type so a .cctor is not created for Enviroment which then would be triggered during startup
        private static class WindowsVersion
        {
            // Cache the value in readonly static that can be optimized out by the JIT
            internal readonly static bool IsWindows8OrAbove = GetIsWindows8OrAbove();

            private static bool GetIsWindows8OrAbove()
            {
                ulong conditionMask = Interop.Kernel32.VerSetConditionMask(0, Interop.Kernel32.VER_MAJORVERSION, Interop.Kernel32.VER_GREATER_EQUAL);
                conditionMask = Interop.Kernel32.VerSetConditionMask(conditionMask, Interop.Kernel32.VER_MINORVERSION, Interop.Kernel32.VER_GREATER_EQUAL);
                conditionMask = Interop.Kernel32.VerSetConditionMask(conditionMask, Interop.Kernel32.VER_SERVICEPACKMAJOR, Interop.Kernel32.VER_GREATER_EQUAL);
                conditionMask = Interop.Kernel32.VerSetConditionMask(conditionMask, Interop.Kernel32.VER_SERVICEPACKMINOR, Interop.Kernel32.VER_GREATER_EQUAL);

                // Windows 8 version is 6.2
                Interop.Kernel32.OSVERSIONINFOEX version = default;
                unsafe
                {
                    version.dwOSVersionInfoSize = sizeof(Interop.Kernel32.OSVERSIONINFOEX);
                }
                version.dwMajorVersion = 6;
                version.dwMinorVersion = 2;
                version.wServicePackMajor = 0;
                version.wServicePackMinor = 0;

                return Interop.Kernel32.VerifyVersionInfoW(ref version,
                    Interop.Kernel32.VER_MAJORVERSION | Interop.Kernel32.VER_MINORVERSION | Interop.Kernel32.VER_SERVICEPACKMAJOR | Interop.Kernel32.VER_SERVICEPACKMINOR,
                    conditionMask);
            }
        }
    }
}
