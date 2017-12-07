// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    internal static partial class PathHelpers
    {
        internal static bool ShouldReviseDirectoryPathToCurrent(string path)
        {
            // Unlike on Windows, there are no special cases on Unix where we'd want to ignore
            // user-provided path and instead automatically use the current directory.
            return false;
        }

        internal static string TrimEndingDirectorySeparator(string path) =>
            path.Length > 1 && PathInternal.IsDirectorySeparator(path[path.Length - 1]) ? // exclude root "/"
                path.Substring(0, path.Length - 1) :
                path;
    }
}
