// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32;
using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace System.IO
{
    /// <summary>Contains internal path helpers that are shared between many projects.</summary>
    internal static class PathInternal
    {
        internal const string ExtendedPathPrefix = @"\\?\";
        internal const string UncPathPrefix = @"\\";
        internal const string UncExtendedPrefixToInsert = @"?\UNC\";
        internal const string UncExtendedPathPrefix = @"\\?\UNC\";
        internal const string DevicePathPrefix = @"\\.\";
        // \\?\, \\.\, \??\
        internal const int DevicePrefixLength = 4;
        // \\
        internal const int UncPrefixLength = 2;
        // \\?\UNC\, \\.\UNC\
        internal const int UncExtendedPrefixLength = 8;
#if !PLATFORM_UNIX
        internal const int MaxShortPath = 260;
        internal const int MaxShortDirectoryPath = 248;
#else
        internal const int MaxShortPath = 1024;
        internal const int MaxShortDirectoryPath = MaxShortPath;
#endif

        // Windows is limited in long paths by the max size of its internal representation of a unicode string.
        // UNICODE_STRING has a max length of USHORT in _bytes_ without a trailing null.
        // https://msdn.microsoft.com/en-us/library/windows/hardware/ff564879.aspx
        internal const int MaxLongPath = short.MaxValue;
        internal static readonly int MaxComponentLength = 255;

#if !PLATFORM_UNIX
        internal static readonly char[] InvalidPathChars =
        {
            '\"', '<', '>', '|', '\0',
            (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10,
            (char)11, (char)12, (char)13, (char)14, (char)15, (char)16, (char)17, (char)18, (char)19, (char)20,
            (char)21, (char)22, (char)23, (char)24, (char)25, (char)26, (char)27, (char)28, (char)29, (char)30,
            (char)31
        };
#else
        internal static readonly char[] InvalidPathChars = { '\0' };
#endif


        /// <summary>
        /// Validates volume separator only occurs as C: or \\?\C:. This logic is meant to filter out Alternate Data Streams.
        /// </summary>
        /// <returns>True if the path has an invalid volume separator.</returns>
        internal static bool HasInvalidVolumeSeparator(string path)
        {
            // Toss out paths with colons that aren't a valid drive specifier.
            // Cannot start with a colon and can only be of the form "C:" or "\\?\C:".
            // (Note that we used to explicitly check "http:" and "file:"- these are caught by this check now.)

            // We don't care about skipping starting space for extended paths. Assume no knowledge of extended paths if we're forcing old path behavior.
            bool isExtended = 
#if FEATURE_PATHCOMPAT
                !AppContextSwitches.UseLegacyPathHandling &&
#endif
                IsExtended(path);
            int startIndex = isExtended ? ExtendedPathPrefix.Length : PathStartSkip(path);

            // If we start with a colon
            if ((path.Length > startIndex && path[startIndex] == Path.VolumeSeparatorChar)
                // Or have an invalid drive letter and colon
                || (path.Length >= startIndex + 2 && path[startIndex + 1] == Path.VolumeSeparatorChar && !IsValidDriveChar(path[startIndex]))
                // Or have any colons beyond the drive colon
                || (path.Length > startIndex + 2 && path.IndexOf(Path.VolumeSeparatorChar, startIndex + 2) != -1))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if the given StringBuilder starts with the given value.
        /// </summary>
        /// <param name="value">The string to compare against the start of the StringBuilder.</param>
        internal static bool StartsWithOrdinal(StringBuilder builder, string value, bool ignoreCase = false)
        {
            if (value == null || builder.Length < value.Length)
                return false;

            if (ignoreCase)
            {
                for (int i = 0; i < value.Length; i++)
                    if (char.ToUpperInvariant(builder[i]) != char.ToUpperInvariant(value[i])) return false;
            }
            else
            {
                for (int i = 0; i < value.Length; i++)
                    if (builder[i] != value[i]) return false;
            }

            return true;
        }

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
            // may grow beyond / shrink below exceed MaxLongPath.
#if FEATURE_PATHCOMPAT
            if (AppContextSwitches.BlockLongPaths)
            {
                // We allow paths of any length if extended (and not in compat mode)
                if (AppContextSwitches.UseLegacyPathHandling || !IsExtended(fullPath))
                    return fullPath.Length >= MaxShortPath;
            }
#endif

            return fullPath.Length >= MaxLongPath;
        }

        /// <summary>
        /// Return true if any path segments are too long
        /// </summary>
        internal static bool AreSegmentsTooLong(string fullPath)
        {
            int length = fullPath.Length;
            int lastSeparator = 0;

            for (int i = 0; i < length; i++)
            {
                if (IsDirectorySeparator(fullPath[i]))
                {
                    if (i - lastSeparator > MaxComponentLength)
                        return true;
                    lastSeparator = i;
                }
            }

            if (length - 1 - lastSeparator > MaxComponentLength)
                return true;

            return false;
        }

        /// <summary>
        /// Returns true if the directory is too long
        /// </summary>
        internal static bool IsDirectoryTooLong(string fullPath)
        {
#if FEATURE_PATHCOMPAT
            if (AppContextSwitches.BlockLongPaths)
            {
                // We allow paths of any length if extended (and not in compat mode)
                if (AppContextSwitches.UseLegacyPathHandling || !IsExtended(fullPath))
                    return (fullPath.Length >= MaxShortDirectoryPath);
            }
#endif

            return IsPathTooLong(fullPath);
        }

        /// <summary>
        /// Adds the extended path prefix (\\?\) if not relative or already a device path.
        /// </summary>
        internal static string EnsureExtendedPrefix(string path)
        {
            // Putting the extended prefix on the path changes the processing of the path. It won't get normalized, which
            // means adding to relative paths will prevent them from getting the appropriate current directory inserted.

            // If it already has some variant of a device path (\??\, \\?\, \\.\, //./, etc.) we don't need to change it
            // as it is either correct or we will be changing the behavior. When/if Windows supports long paths implicitly
            // in the future we wouldn't want normalization to come back and break existing code.

            // In any case, all internal usages should be hitting normalize path (Path.GetFullPath) before they hit this
            // shimming method. (Or making a change that doesn't impact normalization, such as adding a filename to a
            // normalized base path.)
            if (IsPartiallyQualified(path) || IsDevice(path))
                return path;

            // Given \\server\share in longpath becomes \\?\UNC\server\share
            if (path.StartsWith(UncPathPrefix, StringComparison.OrdinalIgnoreCase))
                return path.Insert(2, UncExtendedPrefixToInsert);

            return ExtendedPathPrefix + path;
        }

        /// <summary>
        /// Adds the extended path prefix (\\?\) if not already a device path, IF the path is not relative,
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
        /// Removes the extended path prefix (\\?\) if present.
        /// </summary>
        internal static string RemoveExtendedPrefix(string path)
        {
            if (!IsExtended(path))
                return path;

            // Given \\?\UNC\server\share we return \\server\share
            if (IsExtendedUnc(path))
                return path.Remove(2, 6);

            return path.Substring(DevicePrefixLength);
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

            return path.Remove(0, DevicePrefixLength);
        }

        /// <summary>
        /// Returns true if the path uses any of the DOS device path syntaxes. ("\\.\", "\\?\", or "\??\")
        /// </summary>
        internal static bool IsDevice(string path)
        {
            // If the path begins with any two separators it will be recognized and normalized and prepped with
            // "\??\" for internal usage correctly. "\??\" is recognized and handled, "/??/" is not.
            return IsExtended(path)
                ||
                (
                    path.Length >= DevicePrefixLength
                    && IsDirectorySeparator(path[0])
                    && IsDirectorySeparator(path[1])
                    && (path[2] == '.' || path[2] == '?')
                    && IsDirectorySeparator(path[3])
                );
        }

        /// <summary>
        /// Returns true if the path uses any of the DOS device path syntaxes. ("\\.\", "\\?\", or "\??\")
        /// </summary>
        internal static bool IsDevice(StringBuffer path)
        {
            // If the path begins with any two separators it will be recognized and normalized and prepped with
            // "\??\" for internal usage correctly. "\??\" is recognized and handled, "/??/" is not.
            return IsExtended(path)
                ||
                (
                    path.Length >= DevicePrefixLength
                    && IsDirectorySeparator(path[0])
                    && IsDirectorySeparator(path[1])
                    && (path[2] == '.' || path[2] == '?')
                    && IsDirectorySeparator(path[3])
                );
        }

        /// <summary>
        /// Returns true if the path uses the canonical form of extended syntax ("\\?\" or "\??\"). If the
        /// path matches exactly (cannot use alternate directory separators) Windows will skip normalization
        /// and path length checks.
        /// </summary>
        internal static bool IsExtended(string path)
        {
            // While paths like "//?/C:/" will work, they're treated the same as "\\.\" paths.
            // Skipping of normalization will *only* occur if back slashes ('\') are used.
            return path.Length >= DevicePrefixLength
                && path[0] == '\\'
                && (path[1] == '\\' || path[1] == '?')
                && path[2] == '?'
                && path[3] == '\\';
        }

        /// <summary>
        /// Returns true if the path uses the canonical form of extended syntax ("\\?\" or "\??\"). If the
        /// path matches exactly (cannot use alternate directory separators) Windows will skip normalization
        /// and path length checks.
        /// </summary>
        internal static bool IsExtended(StringBuilder path)
        {
            // While paths like "//?/C:/" will work, they're treated the same as "\\.\" paths.
            // Skipping of normalization will *only* occur if back slashes ('\') are used.
            return path.Length >= DevicePrefixLength
                && path[0] == '\\'
                && (path[1] == '\\' || path[1] == '?')
                && path[2] == '?'
                && path[3] == '\\';
        }

        /// <summary>
        /// Returns true if the path uses the canonical form of extended syntax ("\\?\" or "\??\"). If the
        /// path matches exactly (cannot use alternate directory separators) Windows will skip normalization
        /// and path length checks.
        /// </summary>
        internal static bool IsExtended(StringBuffer path)
        {
            // While paths like "//?/C:/" will work, they're treated the same as "\\.\" paths.
            // Skipping of normalization will *only* occur if back slashes ('\') are used.
            return path.Length >= DevicePrefixLength
                && path[0] == '\\'
                && (path[1] == '\\' || path[1] == '?')
                && path[2] == '?'
                && path[3] == '\\';
        }

        /// <summary>
        /// Returns true if the path uses the extended UNC syntax (\\?\UNC\ or \??\UNC\)
        /// </summary>
        internal static bool IsExtendedUnc(string path)
        {
            return path.Length >= UncExtendedPathPrefix.Length
                && IsExtended(path)
                && char.ToUpper(path[4]) == 'U'
                && char.ToUpper(path[5]) == 'N'
                && char.ToUpper(path[6]) == 'C'
                && path[7] == '\\';
        }

        /// <summary>
        /// Returns true if the path uses the extended UNC syntax (\\?\UNC\ or \??\UNC\)
        /// </summary>
        internal static bool IsExtendedUnc(StringBuilder path)
        {
            return path.Length >= UncExtendedPathPrefix.Length
                && IsExtended(path)
                && char.ToUpper(path[4]) == 'U'
                && char.ToUpper(path[5]) == 'N'
                && char.ToUpper(path[6]) == 'C'
                && path[7] == '\\';
        }

        /// <summary>
        /// Returns a value indicating if the given path contains invalid characters (", &lt;, &gt;, | 
        /// NUL, or any ASCII char whose integer representation is in the range of 1 through 31).
        /// Does not check for wild card characters ? and *.
        ///
        /// Will not check if the path is a device path and not in Legacy mode as many of these
        /// characters are valid for devices (pipes for example).
        /// </summary>
        internal static bool HasIllegalCharacters(string path, bool checkAdditional = false)
        {
            if (
#if FEATURE_PATHCOMPAT
            !AppContextSwitches.UseLegacyPathHandling &&
#endif
                IsDevice(path))
            {
                return false;
            }

            return AnyPathHasIllegalCharacters(path, checkAdditional: checkAdditional);
        }

        /// <summary>
        /// Version of HasIllegalCharacters that checks no AppContextSwitches. Only use if you know you need to skip switches and don't care
        /// about proper device path handling.
        /// </summary>
        internal static bool AnyPathHasIllegalCharacters(string path, bool checkAdditional = false)
        {
            return path.IndexOfAny(InvalidPathChars) >= 0
#if !PLATFORM_UNIX
             || (checkAdditional && AnyPathHasWildCardCharacters(path))
#endif
             ;
        }

        /// <summary>
        /// Check for ? and *.
        /// </summary>
        internal static bool HasWildCardCharacters(string path)
        {
            // Question mark is part of some device paths
            int startIndex =
#if FEATURE_PATHCOMPAT
            AppContextSwitches.UseLegacyPathHandling ? 0 : 
#endif
            IsDevice(path) ? ExtendedPathPrefix.Length : 0;
            return AnyPathHasWildCardCharacters(path, startIndex: startIndex);
        }

        /// <summary>
        /// Version of HasWildCardCharacters that checks no AppContextSwitches. Only use if you know you need to skip switches and don't care
        /// about proper device path handling.
        /// </summary>
        internal static bool AnyPathHasWildCardCharacters(string path, int startIndex = 0)
        {
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
        [System.Security.SecuritySafeCritical]
        internal unsafe static int GetRootLength(string path)
        {
            fixed (char* value = path)
            {
                return (int)GetRootLength(value, (ulong)path.Length);
            }
        }

        /// <summary>
        /// Gets the length of the root of the path (drive, share, etc.).
        /// </summary>
        [System.Security.SecuritySafeCritical]
        internal unsafe static uint GetRootLength(StringBuffer path)
        {
            if (path.Length == 0) return 0;
            return GetRootLength(path.CharPointer, path.Length);
        }

        [System.Security.SecurityCritical]
        private unsafe static uint GetRootLength(char* path, ulong pathLength)
        {
            uint i = 0;

#if PLATFORM_UNIX
            if (pathLength >= 1 && (IsDirectorySeparator(path[0])))
                i = 1;
#else
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
#endif // !PLATFORM_UNIX
            return i;
        }

        [System.Security.SecurityCritical]
        private unsafe static bool StartsWithOrdinal(char* source, ulong sourceLength, string value)
        {
            if (sourceLength < (ulong)value.Length) return false;
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
        internal static bool IsPartiallyQualified(string path)
        {
#if PLATFORM_UNIX
            return !(path.Length >= 1 && path[0] == Path.DirectorySeparatorChar);
#else
            if (path.Length < 2)
            {
                // It isn't fixed, it must be relative.  There is no way to specify a fixed
                // path with one character (or less).
                return true;
            }

            if (IsDirectorySeparator(path[0]))
            {
                // There is no valid way to specify a relative path with two initial slashes or
                // \? as ? isn't valid for drive relative paths and \??\ is equivalent to \\?\
                return !(path[1] == '?' || IsDirectorySeparator(path[1]));
            }

            // The only way to specify a fixed path that doesn't begin with two slashes
            // is the drive, colon, slash format- i.e. C:\
            return !((path.Length >= 3)
                && (path[1] == Path.VolumeSeparatorChar)
                && IsDirectorySeparator(path[2])
                // To match old behavior we'll check the drive character for validity as the path is technically
                // not qualified if you don't have a valid drive. "=:\" is the "=" file's default data stream.
                && IsValidDriveChar(path[0]));
#endif // !PLATFORM_UNIX
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
        internal static bool IsPartiallyQualified(StringBuffer path)
        {
#if PLATFORM_UNIX
            return !(path.Length >= 1 && path[0] == Path.DirectorySeparatorChar);
#else
            if (path.Length < 2)
            {
                // It isn't fixed, it must be relative.  There is no way to specify a fixed
                // path with one character (or less).
                return true;
            }

            if (IsDirectorySeparator(path[0]))
            {
                // There is no valid way to specify a relative path with two initial slashes or
                // \? as ? isn't valid for drive relative paths and \??\ is equivalent to \\?\
                return !(path[1] == '?' || IsDirectorySeparator(path[1]));
            }

            // The only way to specify a fixed path that doesn't begin with two slashes
            // is the drive, colon, slash format- i.e. C:\
            return !((path.Length >= 3)
                && (path[1] == Path.VolumeSeparatorChar)
                && IsDirectorySeparator(path[2])
                // To match old behavior we'll check the drive character for validity as the path is technically
                // not qualified if you don't have a valid drive. "=:\" is the "=" file's default data stream.
                && IsValidDriveChar(path[0]));
#endif // !PLATFORM_UNIX
        }

        /// <summary>
        /// On Windows, returns the characters to skip at the start of the path if it starts with space(s) and a drive or directory separator.
        /// (examples are " C:", " \")
        /// This is a legacy behavior of Path.GetFullPath().
        /// </summary>
        /// <remarks>
        /// Note that this conflicts with IsPathRooted() which doesn't (and never did) such a skip.
        /// </remarks>
        internal static int PathStartSkip(string path)
        {
#if !PLATFORM_UNIX
            int startIndex = 0;
            while (startIndex < path.Length && path[startIndex] == ' ') startIndex++;

            if (startIndex > 0 && (startIndex < path.Length && IsDirectorySeparator(path[startIndex]))
                || (startIndex + 1 < path.Length && path[startIndex + 1] == Path.VolumeSeparatorChar && IsValidDriveChar(path[startIndex])))
            {
                // Go ahead and skip spaces as we're either " C:" or " \"
                return startIndex;
            }
#endif

            return 0;
        }

        /// <summary>
        /// True if the given character is a directory separator.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsDirectorySeparator(char c)
        {
            return c == Path.DirectorySeparatorChar
#if !PLATFORM_UNIX
                 || c == Path.AltDirectorySeparatorChar
#endif
                 ;
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
#if !PLATFORM_UNIX
                            // Check for sequential separators past the first position (we need to keep initial two for UNC/extended)
                            || (i > 0 && i + 1 < path.Length && IsDirectorySeparator(path[i + 1]))
#endif
                        ))
                    {
                    normalized = false;
                        break;
                    }
                }

                if (normalized) return path;
            }

            StringBuilder builder = StringBuilderCache.Acquire(path.Length);

#if !PLATFORM_UNIX
            // On Windows we always keep the first separator, even if the next is a separator (we need to keep initial two for UNC/extended)
            if (IsDirectorySeparator(path[start]))
            {
                start++;
                builder.Append(Path.DirectorySeparatorChar);
            }
#endif

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

            return StringBuilderCache.GetStringAndRelease(builder);
        }

#if PLATFORM_UNIX
        // We rely on Windows to remove relative segments on Windows. This would need to be updated to
        // handle the proper rooting on Windows if we for some reason need it.

        /// <summary>
        /// Try to remove relative segments from the given path (without combining with a root).
        /// </summary>
        /// <param name="skip">Skip the specified number of characters before evaluating.</param>
        internal static string RemoveRelativeSegments(string path, int skip = 0)
        {
            bool flippedSeparator = false;

            // Remove "//", "/./", and "/../" from the path by copying each character to the output, 
            // except the ones we're removing, such that the builder contains the normalized path 
            // at the end.
            var sb = StringBuilderCache.Acquire(path.Length);
            if (skip > 0)
            {
                sb.Append(path, 0, skip);
            }

            int componentCharCount = 0;
            for (int i = skip; i < path.Length; i++)
            {
                char c = path[i];

                if (PathInternal.IsDirectorySeparator(c) && i + 1 < path.Length)
                {
                    componentCharCount = 0;

                    // Skip this character if it's a directory separator and if the next character is, too,
                    // e.g. "parent//child" => "parent/child"
                    if (PathInternal.IsDirectorySeparator(path[i + 1]))
                    {
                        continue;
                    }

                    // Skip this character and the next if it's referring to the current directory,
                    // e.g. "parent/./child" =? "parent/child"
                    if ((i + 2 == path.Length || PathInternal.IsDirectorySeparator(path[i + 2])) &&
                        path[i + 1] == '.')
                    {
                        i++;
                        continue;
                    }

                    // Skip this character and the next two if it's referring to the parent directory,
                    // e.g. "parent/child/../grandchild" => "parent/grandchild"
                    if (i + 2 < path.Length &&
                        (i + 3 == path.Length || PathInternal.IsDirectorySeparator(path[i + 3])) &&
                        path[i + 1] == '.' && path[i + 2] == '.')
                    {
                        // Unwind back to the last slash (and if there isn't one, clear out everything).
                        int s;
                        for (s = sb.Length - 1; s >= 0; s--)
                        {
                            if (PathInternal.IsDirectorySeparator(sb[s]))
                            {
                                sb.Length = s;
                                break;
                            }
                        }
                        if (s < 0)
                        {
                            sb.Length = 0;
                        }

                        i += 2;
                        continue;
                    }
                }

                if (++componentCharCount > PathInternal.MaxComponentLength)
                {
                    throw new PathTooLongException();
                }

                // Normalize the directory separator if needed
                if (c != Path.DirectorySeparatorChar && c == Path.AltDirectorySeparatorChar)
                {
                    c = Path.DirectorySeparatorChar;
                    flippedSeparator = true;
                }

                sb.Append(c);
            }

            if (flippedSeparator || sb.Length != path.Length)
            {
                return StringBuilderCache.GetStringAndRelease(sb);
            }
            else
            {
                // We haven't changed the source path, return the original
                StringBuilderCache.Release(sb);
                return path;
            }
        }
#endif // PLATFORM_UNIX
    }
}