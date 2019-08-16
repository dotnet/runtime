// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Windows.Foundation.Metadata;
using Windows.Storage;
using System.IO;
using static System.Environment;

namespace System
{
    internal static class WinRTFolderPaths
    {
        public static string GetFolderPath(SpecialFolder folder, SpecialFolderOption option)
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

        private static string GetFolderPathCoreCurrent(SpecialFolder folder) =>
            // While all of these give back real paths, most of them are not accessible
            // from an appcontainer currently (they will give access denied)
            folder switch
            {
                SpecialFolder.ApplicationData => UserDataPaths.GetDefault().RoamingAppData,
                SpecialFolder.CommonApplicationData => AppDataPaths.GetDefault().ProgramData,
                SpecialFolder.LocalApplicationData => AppDataPaths.GetDefault().LocalAppData,
                SpecialFolder.Cookies => AppDataPaths.GetDefault().Cookies,
                SpecialFolder.Desktop => AppDataPaths.GetDefault().Desktop,
                SpecialFolder.Favorites => AppDataPaths.GetDefault().Favorites,
                SpecialFolder.History => AppDataPaths.GetDefault().History,
                SpecialFolder.InternetCache => AppDataPaths.GetDefault().InternetCache,
                SpecialFolder.MyMusic => UserDataPaths.GetDefault().Music,
                SpecialFolder.MyPictures => UserDataPaths.GetDefault().Pictures,
                SpecialFolder.MyVideos => UserDataPaths.GetDefault().Videos,
                SpecialFolder.Recent => UserDataPaths.GetDefault().Recent,
                SpecialFolder.System => SystemDataPaths.GetDefault().System,
                SpecialFolder.Templates => UserDataPaths.GetDefault().Templates,
                SpecialFolder.DesktopDirectory => UserDataPaths.GetDefault().Desktop,
                SpecialFolder.Personal => UserDataPaths.GetDefault().Documents,
                SpecialFolder.CommonDocuments => SystemDataPaths.GetDefault().PublicDocuments,
                SpecialFolder.CommonMusic => SystemDataPaths.GetDefault().PublicMusic,
                SpecialFolder.CommonPictures => SystemDataPaths.GetDefault().PublicPictures,
                SpecialFolder.CommonDesktopDirectory => SystemDataPaths.GetDefault().PublicDesktop,
                SpecialFolder.CommonVideos => SystemDataPaths.GetDefault().PublicVideos,
                SpecialFolder.UserProfile => UserDataPaths.GetDefault().Profile,
                SpecialFolder.SystemX86 => SystemDataPaths.GetDefault().SystemX86,
                SpecialFolder.Windows => SystemDataPaths.GetDefault().Windows,

                // The following aren't available on WinRT. Our default behavior
                // is string.Empty for paths that aren't available:
                // SpecialFolder.Programs
                // SpecialFolder.MyComputer
                // SpecialFolder.SendTo
                // SpecialFolder.StartMenu
                // SpecialFolder.Startup
                // SpecialFolder.ProgramFiles
                // SpecialFolder.CommonProgramFiles
                // SpecialFolder.AdminTools
                // SpecialFolder.CDBurning
                // SpecialFolder.CommonAdminTools
                // SpecialFolder.CommonOemLinks
                // SpecialFolder.CommonStartMenu
                // SpecialFolder.CommonPrograms
                // SpecialFolder.CommonStartup
                // SpecialFolder.CommonTemplates
                // SpecialFolder.Fonts
                // SpecialFolder.NetworkShortcuts
                // SpecialFolder.PrinterShortcuts
                // SpecialFolder.CommonProgramFilesX86
                // SpecialFolder.ProgramFilesX86
                // SpecialFolder.Resources
                // SpecialFolder.LocalizedResources

                _ => string.Empty,
            };

        private static string GetFolderPathCoreFallBack(SpecialFolder folder) =>
            // For testing without the new WinRT APIs. We cannot use Win32 APIs for
            // special folders as they are not in the WACK.
            folder switch
            {
                SpecialFolder.ApplicationData => ApplicationData.Current.RoamingFolder?.Path ?? string.Empty,
                SpecialFolder.LocalApplicationData => ApplicationData.Current.LocalFolder?.Path ?? string.Empty,
                SpecialFolder.System => SystemDirectory,
                SpecialFolder.Windows => Path.GetDirectoryName(SystemDirectory)!,
                _ => string.Empty,
            };
    }
}
