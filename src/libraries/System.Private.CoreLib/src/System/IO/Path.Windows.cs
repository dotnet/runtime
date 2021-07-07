// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

#if MS_IO_REDIST
using System;
using System.IO;

namespace Microsoft.IO
#else
namespace System.IO
#endif
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
            if (PathInternal.IsEffectivelyEmpty(path.AsSpan()))
                throw new ArgumentException(SR.Arg_PathEmpty, nameof(path));

            // Embedded null characters are the only invalid character case we trully care about.
            // This is because the nulls will signal the end of the string to Win32 and therefore have
            // unpredictable results.
            if (path.Contains('\0'))
                throw new ArgumentException(SR.Argument_InvalidPathChars, nameof(path));

            if (PathInternal.IsExtended(path.AsSpan()))
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

            if (PathInternal.IsEffectivelyEmpty(path.AsSpan()))
                return basePath;

            int length = path.Length;
            string combinedPath;
            if (length >= 1 && PathInternal.IsDirectorySeparator(path[0]))
            {
                // Path is current drive rooted i.e. starts with \:
                // "\Foo" and "C:\Bar" => "C:\Foo"
                // "\Foo" and "\\?\C:\Bar" => "\\?\C:\Foo"
                combinedPath = Join(GetPathRoot(basePath.AsSpan()), path.AsSpan(1)); // Cut the separator to ensure we don't end up with two separators when joining with the root.
            }
            else if (length >= 2 && PathInternal.IsValidDriveChar(path[0]) && path[1] == PathInternal.VolumeSeparatorChar)
            {
                // Drive relative paths
                Debug.Assert(length == 2 || !PathInternal.IsDirectorySeparator(path[2]));

                if (GetVolumeName(path.AsSpan()).EqualsOrdinal(GetVolumeName(basePath.AsSpan())))
                {
                    // Matching root
                    // "C:Foo" and "C:\Bar" => "C:\Bar\Foo"
                    // "C:Foo" and "\\?\C:\Bar" => "\\?\C:\Bar\Foo"
                    combinedPath = Join(basePath.AsSpan(), path.AsSpan(2));
                }
                else
                {
                    // No matching root, root to specified drive
                    // "D:Foo" and "C:\Bar" => "D:Foo"
                    // "D:Foo" and "\\?\C:\Bar" => "\\?\D:\Foo"
                    combinedPath = !PathInternal.IsDevice(basePath.AsSpan())
                        ? path.Insert(2, @"\")
                        : length == 2
                            ? JoinInternal(basePath.AsSpan(0, 4), path.AsSpan(), @"\".AsSpan())
                            : JoinInternal(basePath.AsSpan(0, 4), path.AsSpan(0, 2), @"\".AsSpan(), path.AsSpan(2));
                }
            }
            else
            {
                // "Simple" relative path
                // "Foo" and "C:\Bar" => "C:\Bar\Foo"
                // "Foo" and "\\?\C:\Bar" => "\\?\C:\Bar\Foo"
                combinedPath = JoinInternal(basePath.AsSpan(), path.AsSpan());
            }

            // Device paths are normalized by definition, so passing something of this format (i.e. \\?\C:\.\tmp, \\.\C:\foo)
            // to Windows APIs won't do anything by design. Additionally, GetFullPathName() in Windows doesn't root
            // them properly. As such we need to manually remove segments and not use GetFullPath().

            return PathInternal.IsDevice(combinedPath.AsSpan())
                ? PathInternal.RemoveRelativeSegments(combinedPath, PathInternal.GetRootLength(combinedPath.AsSpan()))
                : GetFullPath(combinedPath);
        }

        public static string GetTempPath()
        {
            var builder = new ValueStringBuilder(stackalloc char[PathInternal.MaxShortPath]);

            GetTempPath(ref builder);

            string path = PathHelper.Normalize(ref builder);
            builder.Dispose();
            return path;
        }

        private static void GetTempPath(ref ValueStringBuilder builder)
        {
            uint result;
            while ((result = Interop.Kernel32.GetTempPathW(builder.Capacity, ref builder.GetPinnableReference())) > builder.Capacity)
            {
                // Reported size is greater than the buffer size. Increase the capacity.
                builder.EnsureCapacity(checked((int)result));
            }

            if (result == 0)
                throw Win32Marshal.GetExceptionForLastWin32Error();

            builder.Length = (int)result;
        }

        // Returns a unique temporary file name, and creates a 0-byte file by that
        // name on disk.
        public static string GetTempFileName()
        {
            var tempPathBuilder = new ValueStringBuilder(stackalloc char[PathInternal.MaxShortPath]);

            GetTempPath(ref tempPathBuilder);

            var builder = new ValueStringBuilder(stackalloc char[PathInternal.MaxShortPath]);

            uint result = Interop.Kernel32.GetTempFileNameW(
                ref tempPathBuilder.GetPinnableReference(), "tmp", 0, ref builder.GetPinnableReference());

            tempPathBuilder.Dispose();

            if (result == 0)
                throw Win32Marshal.GetExceptionForLastWin32Error();

            builder.Length = builder.RawChars.IndexOf('\0');

            string path = PathHelper.Normalize(ref builder);
            builder.Dispose();
            return path;
        }

        // Tests if the given path contains a root. A path is considered rooted
        // if it starts with a backslash ("\") or a valid drive letter and a colon (":").
        public static bool IsPathRooted([NotNullWhen(true)] string? path)
        {
            return path != null && IsPathRooted(path.AsSpan());
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
        public static string? GetPathRoot(string? path)
        {
            if (PathInternal.IsEffectivelyEmpty(path.AsSpan()))
                return null;

            ReadOnlySpan<char> result = GetPathRoot(path.AsSpan());
            if (path!.Length == result.Length)
                return PathInternal.NormalizeDirectorySeparators(path);

            return PathInternal.NormalizeDirectorySeparators(result.ToString());
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

        /// <summary>
        /// Returns the volume name for dos, UNC and device paths.
        /// </summary>
        internal static ReadOnlySpan<char> GetVolumeName(ReadOnlySpan<char> path)
        {
            // 3 cases: UNC ("\\server\share"), Device ("\\?\C:\"), or Dos ("C:\")
            ReadOnlySpan<char> root = GetPathRoot(path);
            if (root.Length == 0)
                return root;

            // Cut from "\\?\UNC\Server\Share" to "Server\Share"
            // Cut from  "\\Server\Share" to "Server\Share"
            int startOffset = GetUncRootLength(path);
            if (startOffset == -1)
            {
                if (PathInternal.IsDevice(path))
                {
                    startOffset = 4; // Cut from "\\?\C:\" to "C:"
                }
                else
                {
                    startOffset = 0; // e.g. "C:"
                }
            }

            ReadOnlySpan<char> pathToTrim = root.Slice(startOffset);
            return Path.EndsInDirectorySeparator(pathToTrim) ? pathToTrim.Slice(0, pathToTrim.Length - 1) : pathToTrim;
        }

        /// <summary>
        /// Returns offset as -1 if the path is not in Unc format, otherwise returns the root length.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static int GetUncRootLength(ReadOnlySpan<char> path)
        {
            bool isDevice = PathInternal.IsDevice(path);

            if (!isDevice && path.Slice(0, 2).EqualsOrdinal(@"\\".AsSpan()))
                return 2;
            else if (isDevice && path.Length >= 8
                && (path.Slice(0, 8).EqualsOrdinal(PathInternal.UncExtendedPathPrefix.AsSpan())
                || path.Slice(5, 4).EqualsOrdinal(@"UNC\".AsSpan())))
                return 8;

            return -1;
        }
    }
}
