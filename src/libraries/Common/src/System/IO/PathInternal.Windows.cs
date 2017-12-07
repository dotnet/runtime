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
        // All paths in Win32 ultimately end up becoming a path to a File object in the Windows object manager. Passed in paths get mapped through
        // DosDevice symbolic links in the object tree to actual File objects under \Devices. To illustrate, this is what happens with a typical
        // path "Foo" passed as a filename to any Win32 API:
        //
        //  1. "Foo" is recognized as a relative path and is appended to the current directory (say, "C:\" in our example)
        //  2. "C:\Foo" is prepended with the DosDevice namespace "\??\"
        //  3. CreateFile tries to create an object handle to the requested file "\??\C:\Foo"
        //  4. The Object Manager recognizes the DosDevices prefix and looks
        //      a. First in the current session DosDevices ("\Sessions\1\DosDevices\" for example, mapped network drives go here)
        //      b. If not found in the session, it looks in the Global DosDevices ("\GLOBAL??\")
        //  5. "C:" is found in DosDevices (in our case "\GLOBAL??\C:", which is a symbolic link to "\Device\HarddiskVolume6")
        //  6. The full path is now "\Device\HarddiskVolume6\Foo", "\Device\HarddiskVolume6" is a File object and parsing is handed off
        //      to the registered parsing method for Files
        //  7. The registered open method for File objects is invoked to create the file handle which is then returned
        //
        // There are multiple ways to directly specify a DosDevices path. The final format of "\??\" is one way. It can also be specified
        // as "\\.\" (the most commonly documented way) and "\\?\". If the question mark syntax is used the path will skip normalization
        // (essentially GetFullPathName()) and path length checks.

        // Windows Kernel-Mode Object Manager
        // https://msdn.microsoft.com/en-us/library/windows/hardware/ff565763.aspx
        // https://channel9.msdn.com/Shows/Going+Deep/Windows-NT-Object-Manager
        //
        // Introduction to MS-DOS Device Names
        // https://msdn.microsoft.com/en-us/library/windows/hardware/ff548088.aspx
        //
        // Local and Global MS-DOS Device Names
        // https://msdn.microsoft.com/en-us/library/windows/hardware/ff554302.aspx

        internal const string ExtendedPathPrefix = @"\\?\";
        internal const string UncPathPrefix = @"\\";
        internal const string UncExtendedPrefixToInsert = @"?\UNC\";
        internal const string UncExtendedPathPrefix = @"\\?\UNC\";
        internal const string DevicePathPrefix = @"\\.\";
        internal const string ParentDirectoryPrefix = @"..\";

        internal const int MaxShortPath = 260;
        internal const int MaxShortDirectoryPath = 248;
        internal const int MaxLongPath = short.MaxValue;
        // \\?\, \\.\, \??\
        internal const int DevicePrefixLength = 4;
        // \\
        internal const int UncPrefixLength = 2;
        // \\?\UNC\, \\.\UNC\
        internal const int UncExtendedPrefixLength = 8;

        internal static char[] GetInvalidPathChars() => new char[]
        {
            '|', '\0',
            (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10,
            (char)11, (char)12, (char)13, (char)14, (char)15, (char)16, (char)17, (char)18, (char)19, (char)20,
            (char)21, (char)22, (char)23, (char)24, (char)25, (char)26, (char)27, (char)28, (char)29, (char)30,
            (char)31
        };

        // [MS - FSA] 2.1.4.4 Algorithm for Determining if a FileName Is in an Expression
        // https://msdn.microsoft.com/en-us/library/ff469270.aspx
        private static readonly char[] s_wildcardChars =
        {
            '\"', '<', '>', '*', '?'
        };

        /// <summary>
        /// Returns true if the given character is a valid drive letter
        /// </summary>
        internal static bool IsValidDriveChar(char value)
        {
            return ((value >= 'A' && value <= 'Z') || (value >= 'a' && value <= 'z'));
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
                return path.Insert(2, PathInternal.UncExtendedPrefixToInsert);

            return PathInternal.ExtendedPathPrefix + path;
        }

        /// <summary>
        /// Returns true if the path uses any of the DOS device path syntaxes. ("\\.\", "\\?\", or "\??\")
        /// </summary>
        internal static bool IsDevice(string path)
        {
            // If the path begins with any two separators is will be recognized and normalized and prepped with
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
        /// Check for known wildcard characters. '*' and '?' are the most common ones.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe bool HasWildCardCharacters(string path)
        {
            // Question mark is part of dos device syntax so we have to skip if we are
            int startIndex = PathInternal.IsDevice(path) ? ExtendedPathPrefix.Length : 0;

            return path.IndexOfAny(s_wildcardChars, startIndex) >= 0;
        }

        /// <summary>
        /// Gets the length of the root of the path (drive, share, etc.).
        /// </summary>
        internal static unsafe int GetRootLength(string path)
        {
            fixed(char* value = path)
            {
                return (int)GetRootLength(value, (uint)path.Length);
            }
        }

        private static unsafe uint GetRootLength(char* path, uint pathLength)
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

        private static unsafe bool StartsWithOrdinal(char* source, uint sourceLength, string value)
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
        internal static bool IsPartiallyQualified(string path)
        {
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
        
        /// <summary>
        /// Returns true if the character is a directory or volume separator.
        /// </summary>
        /// <param name="ch">The character to test.</param>
        internal static bool IsDirectoryOrVolumeSeparator(char ch)
        {
            return PathInternal.IsDirectorySeparator(ch) || Path.VolumeSeparatorChar == ch;
        }
    }
}
