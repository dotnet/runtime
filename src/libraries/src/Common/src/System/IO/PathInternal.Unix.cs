// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.IO
{
    /// <summary>Contains internal path helpers that are shared between many projects.</summary>
    internal static partial class PathInternal
    {
        // Therre is only one invalid path character in Unix
        private const char InvalidPathChar = '\0';
        internal static readonly char[] InvalidPathChars = { InvalidPathChar };

        /// <summary>Returns a value indicating if the given path contains invalid characters.</summary>
        internal static bool HasIllegalCharacters(string path, bool checkAdditional = false)
        {
            Debug.Assert(path != null);

            foreach (char c in path)
            {
                // Same as InvalidPathChars, unrolled here for performance
                if (c == InvalidPathChar)
                    return true;
            }

            return false;
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
    }
}
