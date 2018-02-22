// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;

namespace System.IO
{
    public static partial class Path
    {
        public static char[] GetInvalidFileNameChars() => new char[]
        {
            '\"', '<', '>', '|', '\0',
            (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10,
            (char)11, (char)12, (char)13, (char)14, (char)15, (char)16, (char)17, (char)18, (char)19, (char)20,
            (char)21, (char)22, (char)23, (char)24, (char)25, (char)26, (char)27, (char)28, (char)29, (char)30,
            (char)31, ':', '*', '?', '\\', '/'
        };

        public static char[] GetInvalidPathChars() => new char[]
        {
            '|', '\0',
            (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10,
            (char)11, (char)12, (char)13, (char)14, (char)15, (char)16, (char)17, (char)18, (char)19, (char)20,
            (char)21, (char)22, (char)23, (char)24, (char)25, (char)26, (char)27, (char)28, (char)29, (char)30,
            (char)31
        };

        // Expands the given path to a fully qualified path.
        public static string GetFullPath(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            // If the path would normalize to string empty, we'll consider it empty
            if (PathInternal.IsEffectivelyEmpty(path))
                throw new ArgumentException(SR.Arg_PathEmpty, nameof(path));

            // Embedded null characters are the only invalid character case we trully care about.
            // This is because the nulls will signal the end of the string to Win32 and therefore have
            // unpredictable results.
            if (path.IndexOf('\0') != -1)
                throw new ArgumentException(SR.Argument_InvalidPathChars, nameof(path));

            if (PathInternal.IsExtended(path))
            {
                // \\?\ paths are considered normalized by definition. Windows doesn't normalize \\?\
                // paths and neither should we. Even if we wanted to GetFullPathName does not work
                // properly with device paths. If one wants to pass a \\?\ path through normalization
                // one can chop off the prefix, pass it to GetFullPath and add it again.
                return path;
            }

            return PathHelper.Normalize(path);
        }

        public static string GetFullPath(string path, string basePath)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (basePath == null)
                throw new ArgumentNullException(nameof(basePath));

            if (!IsPathFullyQualified(basePath))
                throw new ArgumentException(SR.Arg_BasePathNotFullyQualified, nameof(basePath));

            if (basePath.Contains('\0') || path.Contains('\0'))
                throw new ArgumentException(SR.Argument_InvalidPathChars);

            if (IsPathFullyQualified(path))
                return GetFullPath(path);

            int length = path.Length;
            string combinedPath = null;

            if ((length >= 1 && PathInternal.IsDirectorySeparator(path[0])))
            {
                // Path is current drive rooted i.e. starts with \:
                // "\Foo" and "C:\Bar" => "C:\Foo"
                // "\Foo" and "\\?\C:\Bar" => "\\?\C:\Foo"
                combinedPath = CombineNoChecks(GetPathRoot(basePath), path.AsReadOnlySpan().Slice(1));
            }
            else if (length >= 2 && PathInternal.IsValidDriveChar(path[0]) && path[1] == PathInternal.VolumeSeparatorChar)
            {
                // Drive relative paths
                Debug.Assert(length == 2 || !PathInternal.IsDirectorySeparator(path[2]));

                if (StringSpanHelpers.Equals(GetVolumeName(path.AsReadOnlySpan()), GetVolumeName(basePath.AsReadOnlySpan())))
                {
                    // Matching root
                    // "C:Foo" and "C:\Bar" => "C:\Bar\Foo"
                    // "C:Foo" and "\\?\C:\Bar" => "\\?\C:\Bar\Foo"
                    combinedPath = CombineNoChecks(basePath, path.AsReadOnlySpan().Slice(2));
                }
                else
                {
                    // No matching root, root to specified drive
                    // "D:Foo" and "C:\Bar" => "D:Foo"
                    // "D:\Foo" and "\\?\C:\Bar" => "\\?\D:\Foo"
                    combinedPath = path.Insert(2, "\\");
                }
            }
            else
            {
                // "Simple" relative path
                // "Foo" and "C:\Bar" => "C:\Bar\Foo"
                // "Foo" and "\\?\C:\Bar" => "\\?\C:\Bar\Foo"
                combinedPath = CombineNoChecks(basePath, path);
            }

            // Device paths are normalized by definition, so passing something of this format
            // to GetFullPath() won't do anything by design. Additionally, GetFullPathName() in
            // Windows doesn't root them properly. As such we need to manually remove segments.
            return PathInternal.IsDevice(combinedPath)
                ? RemoveRelativeSegments(combinedPath, PathInternal.GetRootLength(combinedPath))
                : GetFullPath(combinedPath);
        }

        public static string GetTempPath()
        {
            StringBuilder sb = StringBuilderCache.Acquire(Interop.Kernel32.MAX_PATH);
            uint r = Interop.Kernel32.GetTempPathW(Interop.Kernel32.MAX_PATH, sb);
            if (r == 0)
                throw Win32Marshal.GetExceptionForLastWin32Error();
            return GetFullPath(StringBuilderCache.GetStringAndRelease(sb));
        }

        // Returns a unique temporary file name, and creates a 0-byte file by that
        // name on disk.
        public static string GetTempFileName()
        {
            string path = GetTempPath();

            StringBuilder sb = StringBuilderCache.Acquire(Interop.Kernel32.MAX_PATH);
            uint r = Interop.Kernel32.GetTempFileNameW(path, "tmp", 0, sb);
            if (r == 0)
                throw Win32Marshal.GetExceptionForLastWin32Error();
            return StringBuilderCache.GetStringAndRelease(sb);
        }

        // Tests if the given path contains a root. A path is considered rooted
        // if it starts with a backslash ("\") or a valid drive letter and a colon (":").
        public static bool IsPathRooted(string path)
        {
            return path != null && IsPathRooted(path.AsReadOnlySpan());
        }

        public static bool IsPathRooted(ReadOnlySpan<char> path)
        {
            int length = path.Length;
            return (length >= 1 && PathInternal.IsDirectorySeparator(path[0]))
                || (length >= 2 && PathInternal.IsValidDriveChar(path[0]) && path[1] == PathInternal.VolumeSeparatorChar);
        }

        // Returns the root portion of the given path. The resulting string
        // consists of those rightmost characters of the path that constitute the
        // root of the path. Possible patterns for the resulting string are: An
        // empty string (a relative path on the current drive), "\" (an absolute
        // path on the current drive), "X:" (a relative path on a given drive,
        // where X is the drive letter), "X:\" (an absolute path on a given drive),
        // and "\\server\share" (a UNC path for a given server and share name).
        // The resulting string is null if path is null. If the path is empty or
        // only contains whitespace characters an ArgumentException gets thrown.
        public static string GetPathRoot(string path)
        {
            if (PathInternal.IsEffectivelyEmpty(path))
                return null;

            ReadOnlySpan<char> result = GetPathRoot(path.AsReadOnlySpan());
            if (path.Length == result.Length)
                return PathInternal.NormalizeDirectorySeparators(path);

            return PathInternal.NormalizeDirectorySeparators(new string(result));
        }

        /// <remarks>
        /// Unlike the string overload, this method will not normalize directory separators.
        /// </remarks>
        public static ReadOnlySpan<char> GetPathRoot(ReadOnlySpan<char> path)
        {
            if (PathInternal.IsEffectivelyEmpty(path))
                return ReadOnlySpan<char>.Empty;

            int pathRoot = PathInternal.GetRootLength(path);
            return pathRoot <= 0 ? ReadOnlySpan<char>.Empty : path.Slice(0, pathRoot);
        }

        /// <summary>Gets whether the system is case-sensitive.</summary>
        internal static bool IsCaseSensitive { get { return false; } }

        /// <summary>
        /// Returns the volume name for dos, UNC and device paths.
        /// </summary>
        internal static ReadOnlySpan<char> GetVolumeName(ReadOnlySpan<char> path)
        {
            // 3 cases: UNC ("\\server\share"), Device ("\\?\C:\"), or Dos ("C:\")
            ReadOnlySpan<char> root = GetPathRoot(path);
            if (root.Length == 0)
                return root;

            int offset = GetUncRootLength(path);
            if (offset >= 0)
            {
                // Cut from "\\?\UNC\Server\Share" to "Server\Share"
                // Cut from  "\\Server\Share" to "Server\Share"
                return TrimEndingDirectorySeparator(root.Slice(offset));
            }
            else if (PathInternal.IsDevice(path))
            {
                return TrimEndingDirectorySeparator(root.Slice(4)); // Cut from "\\?\C:\" to "C:"
            }

            return TrimEndingDirectorySeparator(root); // e.g. "C:"
        }

        /// <summary>
        /// Returns true if the path ends in a directory separator.
        /// </summary>
        internal static bool EndsInDirectorySeparator(ReadOnlySpan<char> path)
        {
            return path.Length > 0 && PathInternal.IsDirectorySeparator(path[path.Length - 1]);
        }

        /// <summary>
        /// Trims the ending directory separator if present.
        /// </summary>
        /// <param name="path"></param>
        internal static ReadOnlySpan<char> TrimEndingDirectorySeparator(ReadOnlySpan<char> path) =>
            EndsInDirectorySeparator(path) ?
                path.Slice(0, path.Length - 1) :
                path;

        /// <summary>
        /// Returns offset as -1 if the path is not in Unc format, otherwise returns the root length.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static int GetUncRootLength(ReadOnlySpan<char> path)
        {
            bool isDevice = PathInternal.IsDevice(path);

            if (!isDevice && StringSpanHelpers.Equals(path.Slice(0, 2), @"\\") )
                return 2;
            else if (isDevice && path.Length >= 8
                && (StringSpanHelpers.Equals(path.Slice(0, 8), PathInternal.UncExtendedPathPrefix)
                || StringSpanHelpers.Equals(path.Slice(5, 4), @"UNC\")))
                return 8;

            return -1;
        }
    }
}
