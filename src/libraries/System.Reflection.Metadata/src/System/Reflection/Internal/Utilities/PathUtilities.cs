// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;

namespace System.Reflection.Metadata
{
    internal static class PathUtilities
    {
        private const char DirectorySeparatorChar = '\\';
        private const char AltDirectorySeparatorChar = '/';
        private const char VolumeSeparatorChar = ':';

        private static string? s_platformSpecificDirectorySeparator;

        private static string PlatformSpecificDirectorySeparator =>
            // '*' is a valid char on Unix-based FS
            s_platformSpecificDirectorySeparator ??=
                (Array.IndexOf(Path.GetInvalidFileNameChars(), '*') >= 0 ? DirectorySeparatorChar : AltDirectorySeparatorChar).ToString();

        /// <summary>
        /// Returns the position in given path where the file name starts.
        /// </summary>
        /// <returns>-1 if path is null.</returns>
        internal static int IndexOfFileName(string path)
        {
            if (path == null)
            {
                return -1;
            }

            for (int i = path.Length - 1; i >= 0; i--)
            {
                char ch = path[i];
                if (ch == DirectorySeparatorChar || ch == AltDirectorySeparatorChar || ch == VolumeSeparatorChar)
                {
                    return i + 1;
                }
            }

            return 0;
        }

        /// <summary>
        /// Get file name from path.
        /// </summary>
        /// <remarks>Unlike <see cref="System.IO.Path.GetFileName(string)"/> this method doesn't check for invalid path characters.</remarks>
        internal static string GetFileName(string path, bool includeExtension = true)
        {
            int fileNameStart = IndexOfFileName(path);
            return (fileNameStart <= 0) ? path : path.Substring(fileNameStart);
        }

        internal static string CombinePathWithRelativePath(string root, string relativePath)
        {
            if (root.Length == 0)
            {
                return relativePath;
            }

            char c = root[root.Length - 1];
            if (c == DirectorySeparatorChar || c == AltDirectorySeparatorChar || c == VolumeSeparatorChar)
            {
                return root + relativePath;
            }

            return root + PlatformSpecificDirectorySeparator + relativePath;
        }
    }
}
