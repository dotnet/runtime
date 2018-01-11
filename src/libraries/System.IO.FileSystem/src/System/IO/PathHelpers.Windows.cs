// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.IO
{
    internal static partial class PathInternal
    {
        internal static unsafe int GetRootLength(ReadOnlySpan<char> path)
        {
            fixed (char* p = &MemoryMarshal.GetReference(path))
            {
                return (int)GetRootLength(p, (uint)path.Length);
            }
        }
    }

    internal static partial class PathHelpers
    {
        internal static bool ShouldReviseDirectoryPathToCurrent(string path)
        {
            // In situations where this method is invoked, "<DriveLetter>:" should be special-cased
            // to instead go to the current directory.
            return path != null && path.Length == 2 && path[1] == ':';
        }

        internal static string TrimEndingDirectorySeparator(string path) =>
            EndsInDirectorySeparator(path) ?
                path.Substring(0, path.Length - 1) :
                path;

        public static bool IsPathRooted(string path)
        {
            // Want to avoid PathInternal.CheckInvalidPathChars on Path.IsPathRooted

            if (path != null)
            {
                int length = path.Length;
                if ((length >= 1 && PathInternal.IsDirectorySeparator(path[0])) ||
                    (length >= 2 && PathInternal.IsValidDriveChar(path[0]) && path[1] == Path.VolumeSeparatorChar))
                    return true;
            }
            return false;
        }
    }
}
