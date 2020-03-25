// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.InteropServices;

namespace System
{
    public static partial class Environment
    {
        // NSSearchPathDirectory enum
        private const int NSApplicationDirectoryId = 1;
        private const int NSLibraryDirectoryId = 5;
        private const int NSUserDirectoryId = 7;
        private const int NSDocumentDirectoryId = 9;
        private const int NSDesktopDirectoryId = 12;
        private const int NSCachesDirectoryId = 13;
        private const int NSMoviesDirectoryId = 17;
        private const int NSMusicDirectoryId = 18;
        private const int NSPicturesDirectoryId = 19;

        // Cache frequently used folders into lazy properties

        // TODO: fix for tvOS (https://github.com/dotnet/runtime/issues/34007)
        // The "normal" NSDocumentDirectory is a read-only directory on tvOS
        // and that breaks a lot of assumptions in the runtime and the BCL
        private static string s_document;
        private static string NSDocumentDirectory => s_document ??= Interop.Sys.SearchPath(NSDocumentDirectoryId);

        private static string s_library;
        private static string NSLibraryDirectory => s_library ??= Interop.Sys.SearchPath(NSLibraryDirectoryId);

        private static string s_user;
        private static string NSUserDirectory => s_user ??= Interop.Sys.SearchPath(NSUserDirectoryId);

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
                    return Interop.Sys.SearchPath(NSDesktopDirectoryId);

                case SpecialFolder.MyMusic:
                    return Interop.Sys.SearchPath(NSMusicDirectoryId);

                case SpecialFolder.MyPictures:
                    return Interop.Sys.SearchPath(NSPicturesDirectoryId);

                case SpecialFolder.Templates:
                    return Path.Combine(NSDocumentDirectory, "Templates");

                case SpecialFolder.MyVideos:
                    return Interop.Sys.SearchPath(NSMoviesDirectoryId);

                case SpecialFolder.CommonTemplates:
                    return "/usr/share/templates";

                case SpecialFolder.Fonts:
                    return Path.Combine(NSDocumentDirectory, ".fonts");

                case SpecialFolder.Favorites:
                    return Path.Combine(NSLibraryDirectory, "Favorites");

                case SpecialFolder.ProgramFiles:
                    return Interop.Sys.SearchPath(NSApplicationDirectoryId);

                case SpecialFolder.InternetCache:
                    return Interop.Sys.SearchPath(NSCachesDirectoryId);

                case SpecialFolder.UserProfile:
                    return NSUserDirectory;

                case SpecialFolder.CommonApplicationData:
                    return "/usr/share";

                default:
                    return string.Empty;
            }
        }
    }
}
