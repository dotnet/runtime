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

        internal static void CheckSearchPattern(string searchPattern)
        {
            // ".." should not be used to move up directories. On Windows, this is more strict, and ".."
            // can only be used in particular places in a name, whereas on Unix it can be used anywhere.
            // So, throw if we find a ".." that's its own component in the path.
            for (int index = 0; (index = searchPattern.IndexOf("..", index, StringComparison.Ordinal)) >= 0; index += 2)
            {
                if ((index == 0 || PathInternal.IsDirectorySeparator(searchPattern[index - 1])) && // previous character is directory separator
                    (index + 2 == searchPattern.Length || PathInternal.IsDirectorySeparator(searchPattern[index + 2]))) // next character is directory separator
                {
                    throw new ArgumentException(SR.Arg_InvalidSearchPattern, nameof(searchPattern));
                }
            }
        }
    }
}
