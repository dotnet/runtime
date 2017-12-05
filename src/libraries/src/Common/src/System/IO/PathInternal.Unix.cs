// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Text;

namespace System.IO
{
    /// <summary>Contains internal path helpers that are shared between many projects.</summary>
    internal static partial class PathInternal
    {
        // There is only one invalid path character in Unix
        private const char InvalidPathChar = '\0';
        internal static char[] GetInvalidPathChars() => new char[] { InvalidPathChar };

        internal const string ParentDirectoryPrefix = @"../";

        internal static int GetRootLength(ReadOnlySpan<char> path)
        {
            return path.Length > 0 && IsDirectorySeparator(path[0]) ? 1 : 0;
        }

        internal static bool IsDirectorySeparator(char c)
        {
            // The alternate directory separator char is the same as the directory separator,
            // so we only need to check one.
            Debug.Assert(Path.DirectorySeparatorChar == Path.AltDirectorySeparatorChar);
            return c == Path.DirectorySeparatorChar;
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

        /// <summary>
        /// Returns true if the character is a directory or volume separator.
        /// </summary>
        /// <param name="ch">The character to test.</param>
        internal static bool IsDirectoryOrVolumeSeparator(char ch)
        {
            // The directory separator, volume separator, and the alternate directory
            // separator should be the same on Unix, so we only need to check one.
            Debug.Assert(Path.DirectorySeparatorChar == Path.AltDirectorySeparatorChar);
            Debug.Assert(Path.DirectorySeparatorChar == Path.VolumeSeparatorChar);
            return ch == Path.DirectorySeparatorChar;
        }

        internal static bool IsPartiallyQualified(string path)
        {
            // This is much simpler than Windows where paths can be rooted, but not fully qualified (such as Drive Relative)
            // As long as the path is rooted in Unix it doesn't use the current directory and therefore is fully qualified.
            return string.IsNullOrEmpty(path) || path[0] != Path.DirectorySeparatorChar;
        }
    }
}
