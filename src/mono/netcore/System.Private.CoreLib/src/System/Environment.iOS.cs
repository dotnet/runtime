// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NSSearchPathDirectory = Interop.Sys.NSSearchPathDirectory;

namespace System
{
    public static partial class Environment
    {
        private static Dictionary<SpecialFolder, string>? s_specialFolders;

        private static string GetFolderPathCore(SpecialFolder folder, SpecialFolderOption option)
        {
            if (s_specialFolders == null)
            {
                Interlocked.CompareExchange(ref s_specialFolders, new Dictionary<SpecialFolder, string>(), null);
            }

            string path;
            lock (s_specialFolders)
            {
                if (!s_specialFolders.TryGetValue(folder, out path))
                {
                    path = GetSpecialFolder(folder) ?? string.Empty;
                    s_specialFolders[folder] = path;
                }
            }
            return path;
        }

        private static string? GetSpecialFolder(SpecialFolder folder)
        {
            switch (folder)
            {
                // TODO: fix for tvOS (https://github.com/dotnet/runtime/issues/34007)
                // The "normal" NSDocumentDirectory is a read-only directory on tvOS
                // and that breaks a lot of assumptions in the runtime and the BCL

                case SpecialFolder.Personal:
                case SpecialFolder.LocalApplicationData:
                    return Interop.Sys.SearchPath(NSSearchPathDirectory.NSDocumentDirectory);

                case SpecialFolder.ApplicationData:
                    // note: at first glance that looked like a good place to return NSLibraryDirectory
                    // but it would break isolated storage for existing applications
                    return Path.Combine(Interop.Sys.SearchPath(NSSearchPathDirectory.NSDocumentDirectory), ".config");

                case SpecialFolder.Resources:
                    return Interop.Sys.SearchPath(NSSearchPathDirectory.NSLibraryDirectory); // older (8.2 and previous) would return String.Empty

                case SpecialFolder.Desktop:
                case SpecialFolder.DesktopDirectory:
                    return Interop.Sys.SearchPath(NSSearchPathDirectory.NSDesktopDirectory);

                case SpecialFolder.MyMusic:
                    return Interop.Sys.SearchPath(NSSearchPathDirectory.NSMusicDirectory);

                case SpecialFolder.MyPictures:
                    return Interop.Sys.SearchPath(NSSearchPathDirectory.NSPicturesDirectory);

                case SpecialFolder.Templates:
                    return Path.Combine(Interop.Sys.SearchPath(NSSearchPathDirectory.NSDocumentDirectory), "Templates");

                case SpecialFolder.MyVideos:
                    return Interop.Sys.SearchPath(NSSearchPathDirectory.NSMoviesDirectory);

                case SpecialFolder.CommonTemplates:
                    return "/usr/share/templates";

                case SpecialFolder.Fonts:
                    return Path.Combine(Interop.Sys.SearchPath(NSSearchPathDirectory.NSDocumentDirectory), ".fonts");

                case SpecialFolder.Favorites:
                    return Path.Combine(Interop.Sys.SearchPath(NSSearchPathDirectory.NSLibraryDirectory), "Favorites");

                case SpecialFolder.ProgramFiles:
                    return Interop.Sys.SearchPath(NSSearchPathDirectory.NSApplicationDirectory);

                case SpecialFolder.InternetCache:
                    return Interop.Sys.SearchPath(NSSearchPathDirectory.NSCachesDirectory);

                case SpecialFolder.UserProfile:
                    return InternalGetEnvironmentVariable("HOME");

                case SpecialFolder.CommonApplicationData:
                    return "/usr/share";

                default:
                    return string.Empty;
            }
        }
    }
}
