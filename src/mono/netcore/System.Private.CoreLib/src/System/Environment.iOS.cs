// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.InteropServices;

namespace System
{
    public static partial class Environment
    {
        private const int NSDocumentDirectoryId = 9;
        private const int NSLibraryDirectoryId = 5;

        private static string s_document;
        private static string s_library;

        // TODO: fix for tvOS
        // The "normal" NSDocumentDirectory is a read-only directory on tvOS
        // and that breaks a lot of assumptions in the runtime and the BCL
        private static string NSDocumentDirectory => s_document ??= Interop.Sys.SearchPath(NSDocumentDirectoryId);

        // Various user-visible documentation, support, and configuration files
        private static string NSLibraryDirectory => s_library ??= Interop.Sys.SearchPath(NSLibraryDirectoryId);

        private static string GetFolderPathCore(SpecialFolder folder, SpecialFolderOption option)
        {
            switch (folder)
            {
                case SpecialFolder.Personal:
                case SpecialFolder.LocalApplicationData:
                    return NSDocumentDirectory;

                case SpecialFolder.ApplicationData:
                    // note: at first glance that looked like a good place to return NSLibraryDirectory 
                    // but it would break isolated storage for existing applications
                    return Path.Combine(NSDocumentDirectory, ".config");

                case SpecialFolder.Resources:
                    return NSLibraryDirectory; // older (8.2 and previous) would return String.Empty

                case SpecialFolder.Desktop:
                case SpecialFolder.DesktopDirectory:
                    return Path.Combine(NSDocumentDirectory, "Desktop");

                case SpecialFolder.MyMusic:
                    return Path.Combine(NSDocumentDirectory, "Music");

                case SpecialFolder.MyPictures:
                    return Path.Combine(NSDocumentDirectory, "Pictures");

                case SpecialFolder.Templates:
                    return Path.Combine(NSDocumentDirectory, "Templates");

                case SpecialFolder.MyVideos:
                    return Path.Combine(NSDocumentDirectory, "Videos");

                case SpecialFolder.CommonTemplates:
                    return "/usr/share/templates";

                case SpecialFolder.Fonts:
                    return Path.Combine(NSDocumentDirectory, ".fonts");

                case SpecialFolder.Favorites:
                    return Path.Combine(NSLibraryDirectory, "Favorites");

                case SpecialFolder.ProgramFiles:
                    return "/Applications";

                case SpecialFolder.InternetCache:
                    return Path.Combine(NSLibraryDirectory, "Caches");

                case SpecialFolder.UserProfile:
                    return Environment.GetEnvironmentVariable("HOME");

                case SpecialFolder.CommonApplicationData:
                    return "/usr/share";

                default:
                    return string.Empty;
            }
        }
    }
}
