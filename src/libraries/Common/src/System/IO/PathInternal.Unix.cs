// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;

namespace System.IO
{
    /// <summary>Contains internal path helpers that are shared between many projects.</summary>
    internal static partial class PathInternal
    {
        // Therre is only one invalid path character in Unix
        private const char InvalidPathChar = '\0';
        internal static readonly char[] InvalidPathChars = { InvalidPathChar };

        internal static readonly int MaxComponentLength = Interop.Sys.MaxName;

        /// <summary>Returns a value indicating if the given path contains invalid characters.</summary>
        internal static bool HasIllegalCharacters(string path)
        {
            Debug.Assert(path != null);
            return path.IndexOf(InvalidPathChar) >= 0;
        }

        internal static int GetRootLength(string path)
        {
            PathInternal.CheckInvalidPathChars(path);
            return path.Length > 0 && IsDirectorySeparator(path[0]) ? 1 : 0;
        }

        internal static bool IsDirectorySeparator(char c)
        {
            // The alternatie directory separator char is the same as the directory separator,
            // so we only need to check one.
            Debug.Assert(Path.DirectorySeparatorChar == Path.AltDirectorySeparatorChar);
            return c == Path.DirectorySeparatorChar;
        }

        /// <summary>
        /// Returns true if the path is too long
        /// </summary>
        internal static bool IsPathTooLong(string fullPath)
        {
            return fullPath.Length >= Interop.Sys.MaxPath;
        }

        /// <summary>
        /// Returns true if the directory is too long
        /// </summary>
        internal static bool IsDirectoryTooLong(string fullPath)
        {
            return fullPath.Length >= Interop.Sys.MaxPath;
        }

        /// <summary>
        /// Normalize separators in the given path. Compresses forward slash runs.
        /// </summary>
        internal static string NormalizeDirectorySeparators(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;

            // Make a pass to see if we need to normalize so we can potentially skip allocating
            bool normalized = true;

            for (int i = 0; i < path.Length; i++)
            {
                if (IsDirectorySeparator(path[i])
                    && (i + 1 < path.Length && IsDirectorySeparator(path[i + 1])))
                {
                    normalized = false;
                    break;
                }
            }

            if (normalized) return path;

            StringBuilder builder = new StringBuilder(path.Length);

            for (int i = 0; i < path.Length; i++)
            {
                char current = path[i];

                // Skip if we have another separator following
                if (IsDirectorySeparator(current)
                    && (i + 1 < path.Length && IsDirectorySeparator(path[i + 1])))
                    continue;

                builder.Append(current);
            }

            return builder.ToString();
        }
    }
}
