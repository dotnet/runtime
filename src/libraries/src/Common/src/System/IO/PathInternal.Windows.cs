// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
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
        internal static readonly int MaxComponentLength = 255;

        internal static readonly char[] InvalidPathChars =
        {
            '\"', '<', '>', '|', '\0',
            (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10,
            (char)11, (char)12, (char)13, (char)14, (char)15, (char)16, (char)17, (char)18, (char)19, (char)20,
            (char)21, (char)22, (char)23, (char)24, (char)25, (char)26, (char)27, (char)28, (char)29, (char)30,
            (char)31
        };

        /// <summary>
        /// Returns true if the given character is a valid drive letter
        /// </summary>
        internal static bool IsValidDriveChar(char value)
        {
            return ((value >= 'A' && value <= 'Z') || (value >= 'a' && value <= 'z'));
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
            if (!IsExtended(path))
                return path;

            // Given \\?\UNC\server\share we return \\server\share
            if (IsExtendedUnc(path))
                return path.Remove(2, 6);

            return path.Substring(4);
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

        /// <summary>
        /// Returns a value indicating if the given path contains invalid characters (", &lt;, &gt;, | 
        /// NUL, or any ASCII char whose integer representation is in the range of 1 through 31).
        /// Does not check for wild card characters ? and *.
        /// </summary>
        internal static bool HasIllegalCharacters(string path, bool checkAdditional = false)
        {
            Debug.Assert(path != null);
            return path.IndexOfAny(InvalidPathChars) >= 0;
        }

        /// <summary>
        /// Check for ? and *.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe static bool HasWildCardCharacters(string path)
        {
            // Question mark is part of extended syntax so we have to skip if we're extended
            int startIndex = PathInternal.IsExtended(path) ? ExtendedPathPrefix.Length : 0;

            char currentChar;
            for (int i = startIndex; i < path.Length; i++)
            {
                currentChar = path[i];
                if (currentChar == '*' || currentChar == '?') return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the length of the root of the path (drive, share, etc.).
        /// </summary>
        internal unsafe static int GetRootLength(string path)
        {
            fixed(char* value = path)
            {
                return (int)GetRootLength(value, (uint)path.Length);
            }
        }

        private unsafe static uint GetRootLength(char* path, uint pathLength)
        {
            uint i = 0;
            uint volumeSeparatorLength = 2;  // Length to the colon "C:"
            uint uncRootLength = 2;          // Length to the start of the server name "\\"

            bool extendedSyntax = StartsWithOrdinal(path, pathLength, ExtendedPathPrefix);
            bool extendedUncSyntax = StartsWithOrdinal(path, pathLength, UncExtendedPathPrefix);
            if (extendedSyntax)
            {
                // Shift the position we look for the root from to account for the extended prefix
                if (extendedUncSyntax)
                {
                    // "\\" -> "\\?\UNC\"
                    uncRootLength = (uint)UncExtendedPathPrefix.Length;
                }
                else
                {
                    // "C:" -> "\\?\C:"
                    volumeSeparatorLength += (uint)ExtendedPathPrefix.Length;
                }
            }

            if ((!extendedSyntax || extendedUncSyntax) && pathLength > 0 && IsDirectorySeparator(path[0]))
            {
                // UNC or simple rooted path (e.g. "\foo", NOT "\\?\C:\foo")

                i = 1; //  Drive rooted (\foo) is one character
                if (extendedUncSyntax || (pathLength > 1 && IsDirectorySeparator(path[1])))
                {
                    // UNC (\\?\UNC\ or \\), scan past the next two directory separators at most
                    // (e.g. to \\?\UNC\Server\Share or \\Server\Share\)
                    i = uncRootLength;
                    int n = 2; // Maximum separators to skip
                    while (i < pathLength && (!IsDirectorySeparator(path[i]) || --n > 0)) i++;
                }
            }
            else if (pathLength >= volumeSeparatorLength && path[volumeSeparatorLength - 1] == Path.VolumeSeparatorChar)
            {
                // Path is at least longer than where we expect a colon, and has a colon (\\?\A:, A:)
                // If the colon is followed by a directory separator, move past it
                i = volumeSeparatorLength;
                if (pathLength >= volumeSeparatorLength + 1 && IsDirectorySeparator(path[volumeSeparatorLength])) i++;
            }
            return i;
        }

        private unsafe static bool StartsWithOrdinal(char* source, uint sourceLength, string value)
        {
            if (sourceLength < (uint)value.Length) return false;
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] != source[i]) return false;
            }
            return true;
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

        /// <summary>
        /// Returns the characters to skip at the start of the path if it starts with space(s) and a drive or directory separator.
        /// (examples are " C:", " \")
        /// This is a legacy behavior of Path.GetFullPath().
        /// </summary>
        /// <remarks>
        /// Note that this conflicts with IsPathRooted() which doesn't (and never did) such a skip.
        /// </remarks>
        internal static int PathStartSkip(string path)
        {
            int startIndex = 0;
            while (startIndex < path.Length && path[startIndex] == ' ') startIndex++;

            if (startIndex > 0 && (startIndex < path.Length && PathInternal.IsDirectorySeparator(path[startIndex]))
                || (startIndex + 1 < path.Length && path[startIndex + 1] == ':' && PathInternal.IsValidDriveChar(path[startIndex])))
            {
                // Go ahead and skip spaces as we're either " C:" or " \"
                return startIndex;
            }

            return 0;
        }

        /// <summary>
        /// True if the given character is a directory separator.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsDirectorySeparator(char c)
        {
            return c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar;
        }

        /// <summary>
        /// Normalize separators in the given path. Converts forward slashes into back slashes and compresses slash runs, keeping initial 2 if present.
        /// Also trims initial whitespace in front of "rooted" paths (see PathStartSkip).
        /// 
        /// This effectively replicates the behavior of the legacy NormalizePath when it was called with fullCheck=false and expandShortpaths=false.
        /// The current NormalizePath gets directory separator normalization from Win32's GetFullPathName(), which will resolve relative paths and as
        /// such can't be used here (and is overkill for our uses).
        /// 
        /// Like the current NormalizePath this will not try and analyze periods/spaces within directory segments.
        /// </summary>
        /// <remarks>
        /// The only callers that used to use Path.Normalize(fullCheck=false) were Path.GetDirectoryName() and Path.GetPathRoot(). Both usages do
        /// not need trimming of trailing whitespace here.
        /// 
        /// GetPathRoot() could technically skip normalizing separators after the second segment- consider as a future optimization.
        /// 
        /// For legacy desktop behavior with ExpandShortPaths:
        ///  - It has no impact on GetPathRoot() so doesn't need consideration.
        ///  - It could impact GetDirectoryName(), but only if the path isn't relative (C:\ or \\Server\Share).
        /// 
        /// In the case of GetDirectoryName() the ExpandShortPaths behavior was undocumented and provided inconsistent results if the path was
        /// fixed/relative. For example: "C:\PROGRA~1\A.TXT" would return "C:\Program Files" while ".\PROGRA~1\A.TXT" would return ".\PROGRA~1". If you
        /// ultimately call GetFullPath() this doesn't matter, but if you don't or have any intermediate string handling could easily be tripped up by
        /// this undocumented behavior.
        /// 
        /// We won't match this old behavior because:
        /// 
        ///   1. It was undocumented
        ///   2. It was costly (extremely so if it actually contained '~')
        ///   3. Doesn't play nice with string logic
        ///   4. Isn't a cross-plat friendly concept/behavior
        /// </remarks>
        internal static string NormalizeDirectorySeparators(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;

            char current;
            int start = PathStartSkip(path);

            if (start == 0)
            {
                // Make a pass to see if we need to normalize so we can potentially skip allocating
                bool normalized = true;

                for (int i = 0; i < path.Length; i++)
                {
                    current = path[i];
                    if (IsDirectorySeparator(current)
                        && (current != Path.DirectorySeparatorChar
                            // Check for sequential separators past the first position (we need to keep initial two for UNC/extended)
                            || (i > 0 && i + 1 < path.Length && IsDirectorySeparator(path[i + 1]))))
                    {
                        normalized = false;
                        break;
                    }
                }

                if (normalized) return path;
            }

            StringBuilder builder = new StringBuilder(path.Length);

            if (IsDirectorySeparator(path[start]))
            {
                start++;
                builder.Append(Path.DirectorySeparatorChar);
            }

            for (int i = start; i < path.Length; i++)
            {
                current = path[i];

                // If we have a separator
                if (IsDirectorySeparator(current))
                {
                    // If the next is a separator, skip adding this
                    if (i + 1 < path.Length && IsDirectorySeparator(path[i + 1]))
                    {
                        continue;
                    }

                    // Ensure it is the primary separator
                    current = Path.DirectorySeparatorChar;
                }

                builder.Append(current);
            }

            return builder.ToString();
        }
    }
}
