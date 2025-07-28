// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.IO;
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

            Guid folderId;
            string? fallbackEnv = null;
            switch (folder)
            {
                // Special-cased values to not use SHGetFolderPath when we have a more direct option available.
                case SpecialFolder.System:
                    // This assumes the system directory always exists and thus we don't need to do anything special for any SpecialFolderOption.
                    return SystemDirectory;
                default:
                    return string.Empty;

                // Map the SpecialFolder to the appropriate Guid
                case SpecialFolder.ApplicationData:
                    folderId = Interop.Shell32.KnownFolders.RoamingAppData;
                    fallbackEnv = "APPDATA";
                    break;
                case SpecialFolder.CommonApplicationData:
                    folderId = Interop.Shell32.KnownFolders.ProgramData;
                    fallbackEnv = "ProgramData";
                    break;
                case SpecialFolder.LocalApplicationData:
                    folderId = Interop.Shell32.KnownFolders.LocalAppData;
                    fallbackEnv = "LOCALAPPDATA";
                    break;
                case SpecialFolder.Cookies:
                    folderId = Interop.Shell32.KnownFolders.Cookies;
                    break;
                case SpecialFolder.Desktop:
                    folderId = Interop.Shell32.KnownFolders.Desktop;
                    break;
                case SpecialFolder.Favorites:
                    folderId = Interop.Shell32.KnownFolders.Favorites;
                    break;
                case SpecialFolder.History:
                    folderId = Interop.Shell32.KnownFolders.History;
                    break;
                case SpecialFolder.InternetCache:
                    folderId = Interop.Shell32.KnownFolders.InternetCache;
                    break;
                case SpecialFolder.Programs:
                    folderId = Interop.Shell32.KnownFolders.Programs;
                    break;
                case SpecialFolder.MyComputer:
                    folderId = Interop.Shell32.KnownFolders.ComputerFolder;
                    break;
                case SpecialFolder.MyMusic:
                    folderId = Interop.Shell32.KnownFolders.Music;
                    break;
                case SpecialFolder.MyPictures:
                    folderId = Interop.Shell32.KnownFolders.Pictures;
                    break;
                case SpecialFolder.MyVideos:
                    folderId = Interop.Shell32.KnownFolders.Videos;
                    break;
                case SpecialFolder.Recent:
                    folderId = Interop.Shell32.KnownFolders.Recent;
                    break;
                case SpecialFolder.SendTo:
                    folderId = Interop.Shell32.KnownFolders.SendTo;
                    break;
                case SpecialFolder.StartMenu:
                    folderId = Interop.Shell32.KnownFolders.StartMenu;
                    break;
                case SpecialFolder.Startup:
                    folderId = Interop.Shell32.KnownFolders.Startup;
                    break;
                case SpecialFolder.Templates:
                    folderId = Interop.Shell32.KnownFolders.Templates;
                    break;
                case SpecialFolder.DesktopDirectory:
                    folderId = Interop.Shell32.KnownFolders.Desktop;
                    break;
                case SpecialFolder.Personal:
                    // Same as Personal
                    // case SpecialFolder.MyDocuments:
                    folderId = Interop.Shell32.KnownFolders.Documents;
                    break;
                case SpecialFolder.ProgramFiles:
                    folderId = Interop.Shell32.KnownFolders.ProgramFiles;
                    fallbackEnv = "ProgramFiles";
                    break;
                case SpecialFolder.CommonProgramFiles:
                    folderId = Interop.Shell32.KnownFolders.ProgramFilesCommon;
                    fallbackEnv = "CommonProgramFiles";
                    break;
                case SpecialFolder.AdminTools:
                    folderId = Interop.Shell32.KnownFolders.AdminTools;
                    break;
                case SpecialFolder.CDBurning:
                    folderId = Interop.Shell32.KnownFolders.CDBurning;
                    break;
                case SpecialFolder.CommonAdminTools:
                    folderId = Interop.Shell32.KnownFolders.CommonAdminTools;
                    break;
                case SpecialFolder.CommonDocuments:
                    folderId = Interop.Shell32.KnownFolders.PublicDocuments;
                    break;
                case SpecialFolder.CommonMusic:
                    folderId = Interop.Shell32.KnownFolders.PublicMusic;
                    break;
                case SpecialFolder.CommonOemLinks:
                    folderId = Interop.Shell32.KnownFolders.CommonOEMLinks;
                    break;
                case SpecialFolder.CommonPictures:
                    folderId = Interop.Shell32.KnownFolders.PublicPictures;
                    break;
                case SpecialFolder.CommonStartMenu:
                    folderId = Interop.Shell32.KnownFolders.CommonStartMenu;
                    break;
                case SpecialFolder.CommonPrograms:
                    folderId = Interop.Shell32.KnownFolders.CommonPrograms;
                    break;
                case SpecialFolder.CommonStartup:
                    folderId = Interop.Shell32.KnownFolders.CommonStartup;
                    break;
                case SpecialFolder.CommonDesktopDirectory:
                    folderId = Interop.Shell32.KnownFolders.PublicDesktop;
                    break;
                case SpecialFolder.CommonTemplates:
                    folderId = Interop.Shell32.KnownFolders.CommonTemplates;
                    break;
                case SpecialFolder.CommonVideos:
                    folderId = Interop.Shell32.KnownFolders.PublicVideos;
                    break;
                case SpecialFolder.Fonts:
                    folderId = Interop.Shell32.KnownFolders.Fonts;
                    break;
                case SpecialFolder.NetworkShortcuts:
                    folderId = Interop.Shell32.KnownFolders.NetHood;
                    break;
                case SpecialFolder.PrinterShortcuts:
                    folderId = Interop.Shell32.KnownFolders.PrintersFolder;
                    break;
                case SpecialFolder.UserProfile:
                    folderId = Interop.Shell32.KnownFolders.Profile;
                    fallbackEnv = "USERPROFILE";
                    break;
                case SpecialFolder.CommonProgramFilesX86:
                    folderId = Interop.Shell32.KnownFolders.ProgramFilesCommonX86;
                    fallbackEnv = "CommonProgramFiles(x86)";
                    break;
                case SpecialFolder.ProgramFilesX86:
                    folderId = Interop.Shell32.KnownFolders.ProgramFilesX86;
                    fallbackEnv = "ProgramFiles(x86)";
                    break;
                case SpecialFolder.Resources:
                    folderId = Interop.Shell32.KnownFolders.ResourceDir;
                    break;
                case SpecialFolder.LocalizedResources:
                    folderId = Interop.Shell32.KnownFolders.LocalizedResourcesDir;
                    break;
                case SpecialFolder.SystemX86:
                    folderId = Interop.Shell32.KnownFolders.SystemX86;
                    break;
                case SpecialFolder.Windows:
                    folderId = Interop.Shell32.KnownFolders.Windows;
                    fallbackEnv = "windir";
                    break;
            }

            int hr = Interop.Shell32.SHGetKnownFolderPath(folderId, (uint)option, IntPtr.Zero, out string path);
            if (hr == 0)
                return path;

            // Fallback logic if SHGetKnownFolderPath failed (nanoserver)
            return fallbackEnv != null ? Environment.GetEnvironmentVariable(fallbackEnv) ?? string.Empty : string.Empty;
        }

        // Separate type so a .cctor is not created for Environment which then would be triggered during startup
        private static class WindowsVersion
        {
            // Cache the value in static readonly that can be optimized out by the JIT
            // Windows 8 version is 6.2
            internal static readonly bool IsWindows8OrAbove = OperatingSystem.IsWindowsVersionAtLeast(6, 2);
        }
    }
}
