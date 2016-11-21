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

        public static char[] GetInvalidFileNameChars() => new char[]
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

        // Expands the given path to a fully qualified path. 
        public static string GetFullPath(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            // Embedded null characters are the only invalid character case we want to check up front.
            // This is because the nulls will signal the end of the string to Win32 and therefore have
            // unpredictable results. Other invalid characters we give a chance to be normalized out.
            if (path.IndexOf('\0') != -1)
                throw new ArgumentException(SR.Argument_InvalidPathChars, nameof(path));

            if (PathInternal.IsExtended(path))
            {
                // We can't really know what is valid for all cases of extended paths.
                //
                //  - object names can include other characters as well (':', '/', etc.)
                //  - even file objects have different rules (pipe names can contain most characters)
                //
                // As such we will do no further analysis of extended paths to avoid blocking known and unknown
                // scenarios as well as minimizing compat breaks should we block now and need to unblock later.
                return path;
            }

            bool isDevice = PathInternal.IsDevice(path);
            if (!isDevice)
            {
                // Toss out paths with colons that aren't a valid drive specifier.
                // Cannot start with a colon and can only be of the form "C:".
                // (Note that we used to explicitly check "http:" and "file:"- these are caught by this check now.)
                int startIndex = PathInternal.PathStartSkip(path);

                // Move past the colon
                startIndex += 2;

                if ((path.Length > 0 && path[0] == VolumeSeparatorChar)
                    || (path.Length >= startIndex && path[startIndex - 1] == VolumeSeparatorChar && !PathInternal.IsValidDriveChar(path[startIndex - 2]))
                    || (path.Length > startIndex && path.IndexOf(VolumeSeparatorChar, startIndex) != -1))
                {
                    throw new NotSupportedException(SR.Argument_PathFormatNotSupported);
                }
            }

            // Technically this doesn't matter but we used to throw for this case
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException(SR.Arg_PathIllegal);

            // We don't want to check invalid characters for device format- see comments for extended above
            string fullPath = PathHelper.Normalize(path, checkInvalidCharacters: !isDevice, expandShortPaths: true);

            if (!isDevice)
            {
                // Emulate FileIOPermissions checks, retained for compatibility (normal invalid characters have already been checked)
                if (PathInternal.HasWildCardCharacters(fullPath))
                    throw new ArgumentException(SR.Argument_InvalidPathChars, nameof(path));
            }

            return fullPath;
        }

        public static string GetTempPath()
        {
            StringBuilder sb = StringBuilderCache.Acquire(MaxPath);
            uint r = Interop.Kernel32.GetTempPathW(MaxPath, sb);
            if (r == 0)
                throw Win32Marshal.GetExceptionForLastWin32Error();
            return GetFullPath(StringBuilderCache.GetStringAndRelease(sb));
        }

        // Returns a unique temporary file name, and creates a 0-byte file by that
        // name on disk.
        public static string GetTempFileName()
        {
            string path = GetTempPath();

            StringBuilder sb = StringBuilderCache.Acquire(MaxPath);
            uint r = Interop.Kernel32.GetTempFileNameW(path, "tmp", 0, sb);
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

        /// <summary>Gets whether the system is case-sensitive.</summary>
        internal static bool IsCaseSensitive { get { return false; } }
    }
}
