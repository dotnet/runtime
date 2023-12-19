// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace System
{
    public static partial class Environment
    {
        private static Dictionary<SpecialFolder, string>? s_specialFolders;

        private static string GetFolderPathCore(SpecialFolder folder, SpecialFolderOption _ /*option*/)
        {
            if (s_specialFolders == null)
            {
                Interlocked.CompareExchange(ref s_specialFolders, new Dictionary<SpecialFolder, string>(), null);
            }

            string? path;
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
            string? home = null;
            try
            {
                home = PersistedFiles.GetHomeDirectory();
            }
            catch (Exception exc)
            {
                Debug.Fail($"Unable to get home directory: {exc}");
            }

            // Fall back to '/' when we can't determine the home directory.
            // This location isn't writable by non-root users which provides some safeguard
            // that the application doesn't write data which is meant to be private.
            if (string.IsNullOrEmpty(home))
            {
                home = "/";
            }

            switch (folder)
            {
                case SpecialFolder.LocalApplicationData:
                    return home;

                case SpecialFolder.ApplicationData:
                    return Path.Combine(home, ".config");

                case SpecialFolder.Desktop:
                case SpecialFolder.DesktopDirectory:
                    return Path.Combine(home, "Desktop");

                case SpecialFolder.MyDocuments:     // Same value as Personal
                    return Path.Combine(home, "Documents");

                case SpecialFolder.MyMusic:
                    return Path.Combine(home, "Music");

                case SpecialFolder.MyPictures:
                    return Path.Combine(home, "Pictures");

                case SpecialFolder.Templates:
                    return Path.Combine(home, "Templates");

                case SpecialFolder.MyVideos:
                    return Path.Combine(home, "Videos");

                case SpecialFolder.CommonTemplates:
                    return "/usr/share/templates";

                case SpecialFolder.Fonts:
                    return Path.Combine(home, ".fonts");

                case SpecialFolder.UserProfile:
                    return home;

                case SpecialFolder.CommonApplicationData:
                    return "/usr/share";

                default:
                    return string.Empty;
            }
        }
    }
}
