// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Runtime.Augments;
using Microsoft.Win32;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace System
{
    public static partial class Environment
    {
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
                result.Capacity = currentSize;
                result.Length = 0;

                size = Interop.Kernel32.ExpandEnvironmentStringsW(name, result, currentSize);
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

            string path;
            int hr = Interop.Shell32.SHGetKnownFolderPath(folderId, (uint)option, IntPtr.Zero, out path);
            if (hr != 0) // Not S_OK
            {
                if (hr == Interop.Shell32.COR_E_PLATFORMNOTSUPPORTED)
                {
                    throw new PlatformNotSupportedException();
                }
                else
                {
                    return string.Empty;
                }
            }

            return path;
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
                StringBuilder sb = StringBuilderCache.Acquire(PathInternal.MaxShortPath);
                if (Interop.Kernel32.GetSystemDirectoryW(sb, PathInternal.MaxShortPath) == 0)
                {
                    StringBuilderCache.Release(sb);
                    throw Win32Marshal.GetExceptionForLastWin32Error();
                }
                return StringBuilderCache.GetStringAndRelease(sb);
            }
        }

        public static string UserName
        {
            get
            {
                // Use GetUserNameExW, as GetUserNameW isn't available on all platforms, e.g. Win7
                var domainName = new StringBuilder(1024);
                uint domainNameLen = (uint)domainName.Capacity;
                if (Interop.SspiCli.GetUserNameExW(Interop.SspiCli.NameSamCompatible, domainName, ref domainNameLen) == 1)
                {
                    string samName = domainName.ToString();
                    int index = samName.IndexOf('\\');
                    if (index != -1)
                    {
                        return samName.Substring(index + 1);
                    }
                }

                return string.Empty;
            }
        }

        public static string UserDomainName
        {
            get
            {
                var domainName = new StringBuilder(1024);
                uint domainNameLen = (uint)domainName.Capacity;
                if (Interop.SspiCli.GetUserNameExW(Interop.SspiCli.NameSamCompatible, domainName, ref domainNameLen) == 1)
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
                if (!Interop.Advapi32.LookupAccountNameW(null, UserName, sid, ref sidLen, domainName, ref domainNameLen, out peUse))
                {
                    throw new InvalidOperationException(Win32Marshal.GetExceptionForLastWin32Error().Message);
                }

                return domainName.ToString();
            }
        }

    }
}
