// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace System.IO
{
    /// <summary>Contains internal path helpers that are shared between many projects.</summary>
    internal static partial class PathInternal
    {
        internal const char DirectorySeparatorChar = '/';
        internal const char AltDirectorySeparatorChar = '/';
        internal const char VolumeSeparatorChar = '/';
        internal const char PathSeparator = ':';
        internal const string DirectorySeparatorCharAsString = "/";
        internal const string ParentDirectoryPrefix = @"../";

        internal static int GetRootLength(ReadOnlySpan<char> path)
        {
            return path.Length > 0 && IsDirectorySeparator(path[0]) ? 1 : 0;
        }

        internal static bool IsDirectorySeparator(char c)
        {
            // The alternate directory separator char is the same as the directory separator,
            // so we only need to check one.
            Debug.Assert(DirectorySeparatorChar == AltDirectorySeparatorChar);
            return c == DirectorySeparatorChar;
        }

        /// <summary>
        /// Normalize separators in the given path. Compresses forward slash runs.
        /// </summary>
        [return: NotNullIfNotNull(nameof(path))]
        internal static string? NormalizeDirectorySeparators(string? path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

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

            if (normalized)
                return path;

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

        internal static bool IsPartiallyQualified(ReadOnlySpan<char> path)
        {
            // This is much simpler than Windows where paths can be rooted, but not fully qualified (such as Drive Relative)
            // As long as the path is rooted in Unix it doesn't use the current directory and therefore is fully qualified.
            return !Path.IsPathRooted(path);
        }

        /// <summary>
        /// Returns true if the path is effectively empty for the current OS.
        /// For unix, this is empty or null. For Windows, this is empty, null, or
        /// just spaces ((char)32).
        /// </summary>
        internal static bool IsEffectivelyEmpty(string? path)
        {
            return string.IsNullOrEmpty(path);
        }

        internal static bool IsEffectivelyEmpty(ReadOnlySpan<char> path)
        {
            return path.IsEmpty;
        }
    }
}
