// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;

namespace System.IO
{
    /// <summary>Contains internal path helpers that are shared between many projects.</summary>
    internal static partial class PathInternal
    {
        internal const string ExtendedPathPrefix = @"\\?\";
        internal const string UncPathPrefix = @"\\";
        internal const string UncExtendedPrefixToInsert = @"?\UNC\";
        internal const string UncExtendedPathPrefix = @"\\?\UNC\";
        internal const string DevicePathPrefix = @"\\.\";
        internal const int MaxShortPath = 260;
        internal const int MaxShortDirectoryPath = 248;
        internal const int MaxLongPath = short.MaxValue;

        internal static readonly char[] InvalidPathChars =
        {
            '\"', '<', '>', '|', '\0',
            (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10,
            (char)11, (char)12, (char)13, (char)14, (char)15, (char)16, (char)17, (char)18, (char)19, (char)20,
            (char)21, (char)22, (char)23, (char)24, (char)25, (char)26, (char)27, (char)28, (char)29, (char)30,
            (char)31
        };

        internal static readonly char[] InvalidPathCharsWithAdditionalChecks = // This is used by HasIllegalCharacters
        {
            '\"', '<', '>', '|', '\0',
            (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10,
            (char)11, (char)12, (char)13, (char)14, (char)15, (char)16, (char)17, (char)18, (char)19, (char)20,
            (char)21, (char)22, (char)23, (char)24, (char)25, (char)26, (char)27, (char)28, (char)29, (char)30,
            (char)31, '*', '?'
        };

        [Flags]
        internal enum ExtendedType
        {
            NotExtended = 0x0,
            Extended = 0x1,
            ExtendedUnc = 0x2
        }

        /// <summary>
        /// Returns true if the path is too long
        /// </summary>
        internal static bool IsPathTooLong(string fullPath)
        {
            // We'll never know precisely what will fail as paths get changed internally in Windows and
            // may grow to exceed MaxExtendedPath. We'll only try to catch ones we know will absolutely
            // fail.

            if (fullPath.Length < MaxLongPath - UncExtendedPathPrefix.Length)
            {
                // We won't push it over MaxLongPath
                return false;
            }

            // We need to check if we have a prefix to account for one being implicitly added.
            if (IsExtended(fullPath))
            {
                // We won't prepend, just check
                return fullPath.Length >= MaxLongPath;
            }

            if (fullPath.StartsWith(UncPathPrefix, StringComparison.Ordinal))
            {
                return fullPath.Length + UncExtendedPrefixToInsert.Length >= MaxLongPath;
            }

            return fullPath.Length + ExtendedPathPrefix.Length >= MaxLongPath;
        }

        /// <summary>
        /// Returns true if the directory is too long
        /// </summary>
        internal static bool IsDirectoryTooLong(string fullPath)
        {
            return IsPathTooLong(fullPath);
        }

        /// <summary>
        /// Adds the extended path prefix (\\?\) if not already present, IF the path is not relative,
        /// AND the path is more than 259 characters. (> MAX_PATH + null)
        /// </summary>
        internal static string EnsureExtendedPrefixOverMaxPath(string path)
        {
            if (path != null && path.Length >= MaxShortPath)
            {
                return EnsureExtendedPrefix(path);
            }
            else
            {
                return path;
            }
        }

        /// <summary>
        /// Adds the extended path prefix (\\?\) if not already present and if the path is not relative or a device (\\.\).
        /// </summary>
        internal static string EnsureExtendedPrefix(string path)
        {
            if (IsExtended(path) || IsRelative(path) || IsDevice(path))
                return path;

            // Given \\server\share in longpath becomes \\?\UNC\server\share
            if (path.StartsWith(UncPathPrefix, StringComparison.OrdinalIgnoreCase))
                return path.Insert(2, PathInternal.UncExtendedPrefixToInsert);

            return PathInternal.ExtendedPathPrefix + path;
        }

        /// <summary>
        /// Adds the extended path prefix (\\?\) if not already present and if the path is not relative or a device (\\.\).
        /// </summary>
        internal static void EnsureExtendedPrefix(StringBuilder path)
        {
            if (IsExtended(path) || IsRelative(path) || IsDevice(path))
                return;

            // Given \\server\share in longpath becomes \\?\UNC\server\share
            if (path.StartsWithOrdinal(UncPathPrefix))
            {
                path.Insert(2, PathInternal.UncExtendedPrefixToInsert);
                return;
            }

            path.Insert(0, PathInternal.ExtendedPathPrefix);
        }

        /// <summary>
        /// Removes the extended path prefix (\\?\) if present.
        /// </summary>
        internal static string RemoveExtendedPrefix(string path)
        {
            RemoveExtendedPrefix(ref path);
            return path;
        }

        /// <summary>
        /// Removes the extended path prefix (\\?\) if present.
        /// </summary>
        internal static StringBuilder RemoveExtendedPrefix(StringBuilder path)
        {
            if (!IsExtended(path))
                return path;

            // Given \\?\UNC\server\share we return \\server\share
            if (IsExtendedUnc(path))
                return path.Remove(2, 6);

            return path.Remove(0, 4);
        }

        /// <summary>
        /// Remove the extended prefix from the given path. Return the type of path found.
        /// </summary>
        internal static ExtendedType RemoveExtendedPrefix(ref string path)
        {
            if (!IsExtended(path))
                return ExtendedType.NotExtended;

            // Given \\?\UNC\server\share we return \\server\share
            if (IsExtendedUnc(path))
            {
                path = path.Remove(2, 6);
                return ExtendedType.Extended | ExtendedType.Extended;
            }

            path = path.Substring(4);
            return ExtendedType.Extended;
        }

        /// <summary>
        /// Returns true if the path uses the device syntax (\\.\)
        /// </summary>
        internal static bool IsDevice(string path)
        {
            return path != null && path.StartsWith(DevicePathPrefix, StringComparison.Ordinal);
        }

        /// <summary>
        /// Returns true if the path uses the device syntax (\\.\)
        /// </summary>
        internal static bool IsDevice(StringBuilder path)
        {
            return path != null && path.StartsWithOrdinal(DevicePathPrefix);
        }

        /// <summary>
        /// Returns true if the path uses the extended syntax (\\?\)
        /// </summary>
        internal static bool IsExtended(string path)
        {
            return path != null && path.StartsWith(ExtendedPathPrefix, StringComparison.Ordinal);
        }

        /// <summary>
        /// Returns true if the path uses the extended syntax (\\?\)
        /// </summary>
        internal static bool IsExtended(StringBuilder path)
        {
            return path != null && path.StartsWithOrdinal(ExtendedPathPrefix);
        }

        /// <summary>
        /// Returns true if the path uses the extended UNC syntax (\\?\UNC\)
        /// </summary>
        internal static bool IsExtendedUnc(string path)
        {
            return path != null && path.StartsWith(UncExtendedPathPrefix, StringComparison.Ordinal);
        }

        /// <summary>
        /// Returns true if the path uses the extended UNC syntax (\\?\UNC\)
        /// </summary>
        internal static bool IsExtendedUnc(StringBuilder path)
        {
            return path != null && path.StartsWithOrdinal(UncExtendedPathPrefix);
        }

        private static bool StartsWithOrdinal(this StringBuilder builder, string value)
        {
            if (value == null || builder.Length < value.Length)
                return false;

            for (int i = 0; i < value.Length; i++)
            {
                if (builder[i] != value[i]) return false;
            }
            return true;
        }

        /// <summary>
        /// Returns a value indicating if the given path contains invalid characters (", &lt;, &gt;, | 
        /// NUL, or any ASCII char whose integer representation is in the range of 1 through 31), 
        /// optionally checking for ? and *.
        /// </summary>
        internal static bool HasIllegalCharacters(string path, bool checkAdditional = false)
        {
            Debug.Assert(path != null);

            // See: http://msdn.microsoft.com/en-us/library/windows/desktop/aa365247(v=vs.85).aspx
            // Question mark is a normal part of extended path syntax (\\?\)
            int startIndex = PathInternal.IsExtended(path) ? ExtendedPathPrefix.Length : 0;
            return path.IndexOfAny(checkAdditional ? InvalidPathCharsWithAdditionalChecks : InvalidPathChars, startIndex) >= 0;
        }

        /// <summary>
        /// Gets the length of the root of the path (drive, share, etc.).
        /// </summary>
        internal static int GetRootLength(string path)
        {
            CheckInvalidPathChars(path);

            int i = 0;
            int length = path.Length;
            int volumeSeparatorLength = 2;  // Length to the colon "C:"
            int uncRootLength = 2;          // Length to the start of the server name "\\"

            bool extendedSyntax = IsExtended(path);
            bool extendedUncSyntax = IsExtendedUnc(path);
            if (extendedSyntax)
            {
                // Shift the position we look for the root from to account for the extended prefix
                if (extendedUncSyntax)
                {
                    // "\\" -> "\\?\UNC\"
                    uncRootLength = UncExtendedPathPrefix.Length;
                }
                else
                {
                    // "C:" -> "\\?\C:"
                    volumeSeparatorLength += ExtendedPathPrefix.Length;
                }
            }

            if ((!extendedSyntax || extendedUncSyntax) && length > 0 && IsDirectorySeparator(path[0]))
            {
                // UNC or simple rooted path (e.g. "\foo", NOT "\\?\C:\foo")

                i = 1; //  Drive rooted (\foo) is one character
                if (extendedUncSyntax || (length > 1 && IsDirectorySeparator(path[1])))
                {
                    // UNC (\\?\UNC\ or \\), scan past the next two directory separators at most
                    // (e.g. to \\?\UNC\Server\Share or \\Server\Share\)
                    i = uncRootLength;
                    int n = 2; // Maximum separators to skip
                    while (i < length && (!IsDirectorySeparator(path[i]) || --n > 0)) i++;
                }
            }
            else if (length >= volumeSeparatorLength && path[volumeSeparatorLength - 1] == Path.VolumeSeparatorChar)
            {
                // Path is at least longer than where we expect a colon, and has a colon (\\?\A:, A:)
                // If the colon is followed by a directory separator, move past it
                i = volumeSeparatorLength;
                if (length >= volumeSeparatorLength + 1 && IsDirectorySeparator(path[volumeSeparatorLength])) i++;
            }
            return i;
        }

        /// <summary>
        /// Returns true if the path specified is relative to the current drive or working directory.
        /// Returns false if the path is fixed to a specific drive or UNC path.  This method does no
        /// validation of the path (URIs will be returned as relative as a result).
        /// </summary>
        /// <remarks>
        /// Handles paths that use the alternate directory separator.  It is a frequent mistake to
        /// assume that rooted paths (Path.IsPathRooted) are not relative.  This isn't the case.
        /// "C:a" is drive relative- meaning that it will be resolved against the current directory
        /// for C: (rooted, but relative). "C:\a" is rooted and not relative (the current directory
        /// will not be used to modify the path).
        /// </remarks>
        internal static bool IsRelative(string path)
        {
            if (path.Length < 2)
            {
                // It isn't fixed, it must be relative.  There is no way to specify a fixed
                // path with one character (or less).
                return true;
            }

            if (IsDirectorySeparator(path[0]))
            {
                // There is no valid way to specify a relative path with two initial slashes
                return !IsDirectorySeparator(path[1]);
            }

            // The only way to specify a fixed path that doesn't begin with two slashes
            // is the drive, colon, slash format- i.e. C:\
            return !((path.Length >= 3)
                && (path[1] == Path.VolumeSeparatorChar)
                && IsDirectorySeparator(path[2]));
        }

        /// <summary>
        /// Returns true if the path specified is relative to the current drive or working directory.
        /// Returns false if the path is fixed to a specific drive or UNC path.  This method does no
        /// validation of the path (URIs will be returned as relative as a result).
        /// </summary>
        /// <remarks>
        /// Handles paths that use the alternate directory separator.  It is a frequent mistake to
        /// assume that rooted paths (Path.IsPathRooted) are not relative.  This isn't the case.
        /// "C:a" is drive relative- meaning that it will be resolved against the current directory
        /// for C: (rooted, but relative). "C:\a" is rooted and not relative (the current directory
        /// will not be used to modify the path).
        /// </remarks>
        internal static bool IsRelative(StringBuilder path)
        {
            if (path.Length < 2)
            {
                // It isn't fixed, it must be relative.  There is no way to specify a fixed
                // path with one character (or less).
                return true;
            }

            if (IsDirectorySeparator(path[0]))
            {
                // There is no valid way to specify a relative path with two initial slashes
                return !IsDirectorySeparator(path[1]);
            }

            // The only way to specify a fixed path that doesn't begin with two slashes
            // is the drive, colon, slash format- i.e. C:\
            return !((path.Length >= 3)
                && (path[1] == Path.VolumeSeparatorChar)
                && IsDirectorySeparator(path[2]));
        }

        internal static bool IsDirectorySeparator(char c)
        {
            return c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar;
        }
    }
}
