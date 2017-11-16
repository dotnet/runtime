// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Windows.Foundation.Metadata;
using Windows.Storage;
using System.IO;

namespace System
{
    public static partial class Environment
    {
        private static string GetFolderPathCore(SpecialFolder folder, SpecialFolderOption option)
        {
            // For testing we'll fall back if the needed APIs aren't present.
            //
            // We're not honoring the special folder options (noverify/create) for a few reasons. One, most of the
            // folders always exist (e.g. it is moot). Two, most locations are inaccessible from an appcontainer
            // currently - making it impossible to answer the question of existence or create if necessary. Thirdly,
            // the Win32 API would create these folders with very specific ACLs, which even in the cases we can create
            // are a significant compat risk (trying to replicate internal Windows behavior- it is documented that they
            // set specific ACLs, but not which ones).
            if (ApiInformation.IsTypePresent("Windows.Storage.UserDataPaths"))
            {
                return GetFolderPathCoreCurrent(folder);
            }
            else
            {
                return GetFolderPathCoreFallBack(folder);
            }
        }

        private static string GetFolderPathCoreCurrent(SpecialFolder folder)
        {
            // While all of these give back real paths, most of them are not accessible
            // from an appcontainer currently (they will give access denied)
            switch (folder)
            {
                case SpecialFolder.ApplicationData:
                    return UserDataPaths.GetDefault().RoamingAppData;
                case SpecialFolder.CommonApplicationData:
                    return AppDataPaths.GetDefault().ProgramData;
                case SpecialFolder.LocalApplicationData:
                    return AppDataPaths.GetDefault().LocalAppData;
                case SpecialFolder.Cookies:
                    return AppDataPaths.GetDefault().Cookies;
                case SpecialFolder.Desktop:
                    return AppDataPaths.GetDefault().Desktop;
                case SpecialFolder.Favorites:
                    return AppDataPaths.GetDefault().Favorites;
                case SpecialFolder.History:
                    return AppDataPaths.GetDefault().History;
                case SpecialFolder.InternetCache:
                    return AppDataPaths.GetDefault().InternetCache;
                case SpecialFolder.MyMusic:
                    return UserDataPaths.GetDefault().Music;
                case SpecialFolder.MyPictures:
                    return UserDataPaths.GetDefault().Pictures;
                case SpecialFolder.MyVideos:
                    return UserDataPaths.GetDefault().Videos;
                case SpecialFolder.Recent:
                    return UserDataPaths.GetDefault().Recent;
                case SpecialFolder.System:
                    return SystemDataPaths.GetDefault().System;
                case SpecialFolder.Templates:
                    return UserDataPaths.GetDefault().Templates;
                case SpecialFolder.DesktopDirectory:
                    return UserDataPaths.GetDefault().Desktop;
                case SpecialFolder.Personal:
                    return UserDataPaths.GetDefault().Documents;
                case SpecialFolder.CommonDocuments:
                    return SystemDataPaths.GetDefault().PublicDocuments;
                case SpecialFolder.CommonMusic:
                    return SystemDataPaths.GetDefault().PublicMusic;
                case SpecialFolder.CommonPictures:
                    return SystemDataPaths.GetDefault().PublicPictures;
                case SpecialFolder.CommonDesktopDirectory:
                    return SystemDataPaths.GetDefault().PublicDesktop;
                case SpecialFolder.CommonVideos:
                    return SystemDataPaths.GetDefault().PublicVideos;
                case SpecialFolder.UserProfile:
                    return UserDataPaths.GetDefault().Profile;
                case SpecialFolder.SystemX86:
                    return SystemDataPaths.GetDefault().SystemX86;
                case SpecialFolder.Windows:
                    return SystemDataPaths.GetDefault().Windows;

                // The following aren't available on WinRT. Our default behavior
                // is string.Empty for paths that aren't available.
                //
                // case SpecialFolder.Programs:
                // case SpecialFolder.MyComputer:
                // case SpecialFolder.SendTo:
                // case SpecialFolder.StartMenu:
                // case SpecialFolder.Startup:
                // case SpecialFolder.ProgramFiles:
                // case SpecialFolder.CommonProgramFiles:
                // case SpecialFolder.AdminTools:
                // case SpecialFolder.CDBurning:
                // case SpecialFolder.CommonAdminTools:
                // case SpecialFolder.CommonOemLinks:
                // case SpecialFolder.CommonStartMenu:
                // case SpecialFolder.CommonPrograms:
                // case SpecialFolder.CommonStartup:
                // case SpecialFolder.CommonTemplates:
                // case SpecialFolder.Fonts:
                // case SpecialFolder.NetworkShortcuts:
                // case SpecialFolder.PrinterShortcuts:
                // case SpecialFolder.CommonProgramFilesX86:
                // case SpecialFolder.ProgramFilesX86:
                // case SpecialFolder.Resources:
                // case SpecialFolder.LocalizedResources:

                default:
                    return string.Empty;
            }
        }

        private static string GetFolderPathCoreFallBack(SpecialFolder folder)
        {
            // For testing without the new WinRT APIs. We cannot use Win32 APIs for
            // special folders as they are not in the WACK.
            switch (folder)
            {
                case SpecialFolder.ApplicationData:
                    return ApplicationData.Current.RoamingFolder?.Path;
                case SpecialFolder.LocalApplicationData:
                    return ApplicationData.Current.LocalFolder?.Path;
                case SpecialFolder.System:
                    return SystemDirectory;
                case SpecialFolder.Windows:
                    return Path.GetDirectoryName(SystemDirectory);
                default:
                    return string.Empty;
            }
        }
    }
}
