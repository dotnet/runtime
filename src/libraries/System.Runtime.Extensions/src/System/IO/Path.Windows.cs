// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;

namespace System.IO
{
    public static partial class Path
    {
        public static readonly char DirectorySeparatorChar = '\\';
        public static readonly char VolumeSeparatorChar = ':';
        public static readonly char PathSeparator = ';';

        private const string DirectorySeparatorCharAsString = "\\";

        private static readonly char[] InvalidFileNameChars = 
        { 
            '\"', '<', '>', '|', '\0', 
            (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10, 
            (char)11, (char)12, (char)13, (char)14, (char)15, (char)16, (char)17, (char)18, (char)19, (char)20, 
            (char)21, (char)22, (char)23, (char)24, (char)25, (char)26, (char)27, (char)28, (char)29, (char)30, 
            (char)31, ':', '*', '?', '\\', '/' 
        };

        // The max total path is 260, and the max individual component length is 255. 
        // For example, D:\<256 char file name> isn't legal, even though it's under 260 chars.
        internal static readonly int MaxPath = 260;
        internal static readonly int MaxLongPath = short.MaxValue;

        private static bool IsDirectoryOrVolumeSeparator(char c)
        {
            return PathInternal.IsDirectorySeparator(c) || VolumeSeparatorChar == c;
        }

        // Expands the given path to a fully qualified path. 
        [System.Security.SecuritySafeCritical]
        public static string GetFullPath(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            string fullPath = NormalizeAndValidatePath(path);

            // Emulate FileIOPermissions checks, retained for compatibility (normal invalid characters have already been checked)
            if (PathInternal.HasWildCardCharacters(fullPath))
                throw new ArgumentException(SR.Argument_InvalidPathChars, nameof(path));

            return fullPath;
        }

        /// <summary>
        /// Checks for known bad extended paths (paths that start with \\?\)
        /// </summary>
        /// <returns>'true' if the path passes validity checks.</returns>
        private static bool ValidateExtendedPath(string path)
        {
            if (path.Length == PathInternal.ExtendedPathPrefix.Length)
            {
                // Effectively empty and therefore invalid
                return false;
            }

            if (path.StartsWith(PathInternal.UncExtendedPathPrefix, StringComparison.Ordinal))
            {
                // UNC specific checks
                if (path.Length == PathInternal.UncExtendedPathPrefix.Length || path[PathInternal.UncExtendedPathPrefix.Length] == DirectorySeparatorChar)
                {
                    // Effectively empty and therefore invalid (\\?\UNC\ or \\?\UNC\\)
                    return false;
                }

                int serverShareSeparator = path.IndexOf(DirectorySeparatorChar, PathInternal.UncExtendedPathPrefix.Length);
                if (serverShareSeparator == -1 || serverShareSeparator == path.Length - 1)
                {
                    // Need at least a Server\Share
                    return false;
                }
            }

            // Segments can't be empty "\\" or contain *just* "." or ".."
            char twoBack = '?';
            char oneBack = DirectorySeparatorChar;
            char currentCharacter;
            bool periodSegment = false;
            for (int i = PathInternal.ExtendedPathPrefix.Length; i < path.Length; i++)
            {
                currentCharacter = path[i];
                switch (currentCharacter)
                {
                    case '\\':
                        if (oneBack == DirectorySeparatorChar || periodSegment)
                            throw new ArgumentException(SR.Arg_PathIllegal);
                        periodSegment = false;
                        break;
                    case '.':
                        periodSegment = (oneBack == DirectorySeparatorChar || (twoBack == DirectorySeparatorChar && oneBack == '.'));
                        break;
                    default:
                        periodSegment = false;
                        break;
                }

                twoBack = oneBack;
                oneBack = currentCharacter;
            }

            if (periodSegment)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Normalize the path and check for bad characters or other invalid syntax.
        /// </summary>
        /// <remarks>
        /// The legacy NormalizePath
        /// </remarks>
        private static string NormalizeAndValidatePath(string path)
        {
            Debug.Assert(path != null, "path can't be null");

            // Embedded null characters are the only invalid character case we want to check up front.
            // This is because the nulls will signal the end of the string to Win32 and therefore have
            // unpredictable results. Other invalid characters we give a chance to be normalized out.
            if (path.IndexOf('\0') != -1)
                throw new ArgumentException(SR.Argument_InvalidPathChars, nameof(path));

            // Toss out paths with colons that aren't a valid drive specifier.
            // Cannot start with a colon and can only be of the form "C:" or "\\?\C:".
            // (Note that we used to explicitly check "http:" and "file:"- these are caught by this check now.)
            int startIndex = PathInternal.PathStartSkip(path);
            bool isExtended = path.Length >= PathInternal.ExtendedPathPrefix.Length + startIndex
                && path.IndexOf(PathInternal.ExtendedPathPrefix, startIndex, PathInternal.ExtendedPathPrefix.Length, StringComparison.Ordinal) >= 0;

            if (isExtended)
            {
                startIndex += PathInternal.ExtendedPathPrefix.Length;
            }

            // Move past the colon
            startIndex += 2;

            if ((path.Length > 0 && path[0] == VolumeSeparatorChar)
                || (path.Length >= startIndex && path[startIndex - 1] == VolumeSeparatorChar && !PathInternal.IsValidDriveChar(path[startIndex - 2]))
                || (path.Length > startIndex && path.IndexOf(VolumeSeparatorChar, startIndex) != -1))
            {
                throw new NotSupportedException(SR.Argument_PathFormatNotSupported);
            }

            if (isExtended)
            {
                // If the path is in extended syntax, we don't need to normalize, but we still do some basic validity checks
                if (!ValidateExtendedPath(path))
                {
                    throw new ArgumentException(SR.Arg_PathIllegal);
                }

                // \\?\GLOBALROOT gives access to devices out of the scope of the current user, we
                // don't want to allow this for security reasons.
                // https://msdn.microsoft.com/en-us/library/windows/desktop/aa365247.aspx#nt_namespaces
                if (path.StartsWith(@"\\?\globalroot", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException(SR.Arg_PathGlobalRoot);


                // Look for illegal path characters.
                PathInternal.CheckInvalidPathChars(path);

                return path;
            }
            else
            {
                // Technically this doesn't matter but we used to throw for this case
                if (String.IsNullOrWhiteSpace(path))
                    throw new ArgumentException(SR.Arg_PathIllegal);

                return PathHelper.Normalize(path, checkInvalidCharacters: true, expandShortPaths: true);
            }
        }

        [System.Security.SecuritySafeCritical]
        public static string GetTempPath()
        {
            StringBuilder sb = StringBuilderCache.Acquire(MaxPath);
            uint r = Interop.mincore.GetTempPathW(MaxPath, sb);
            if (r == 0)
                throw Win32Marshal.GetExceptionForLastWin32Error();
            return GetFullPath(StringBuilderCache.GetStringAndRelease(sb));
        }

        [System.Security.SecurityCritical]
        private static string InternalGetTempFileName(bool checkHost)
        {
            // checkHost was originally intended for file security checks, but is ignored.

            string path = GetTempPath();

            StringBuilder sb = StringBuilderCache.Acquire(MaxPath);
            uint r = Interop.mincore.GetTempFileNameW(path, "tmp", 0, sb);
            if (r == 0)
                throw Win32Marshal.GetExceptionForLastWin32Error();
            return StringBuilderCache.GetStringAndRelease(sb);
        }

        // Tests if the given path contains a root. A path is considered rooted
        // if it starts with a backslash ("\") or a drive letter and a colon (":").
        public static bool IsPathRooted(string path)
        {
            if (path != null)
            {
                PathInternal.CheckInvalidPathChars(path);

                int length = path.Length;
                if ((length >= 1 && PathInternal.IsDirectorySeparator(path[0])) || 
                    (length >= 2 && path[1] == VolumeSeparatorChar))
                    return true;
            }
            return false;
        }

        // Returns the root portion of the given path. The resulting string
        // consists of those rightmost characters of the path that constitute the
        // root of the path. Possible patterns for the resulting string are: An
        // empty string (a relative path on the current drive), "\" (an absolute
        // path on the current drive), "X:" (a relative path on a given drive,
        // where X is the drive letter), "X:\" (an absolute path on a given drive),
        // and "\\server\share" (a UNC path for a given server and share name).
        // The resulting string is null if path is null.
        public static string GetPathRoot(string path)
        {
            if (path == null) return null;
            PathInternal.CheckInvalidPathChars(path);

            // Need to return the normalized directory separator
            path = PathInternal.NormalizeDirectorySeparators(path);

            int pathRoot = PathInternal.GetRootLength(path);
            return pathRoot <= 0 ? string.Empty : path.Substring(0, pathRoot);
        }
    }
}
