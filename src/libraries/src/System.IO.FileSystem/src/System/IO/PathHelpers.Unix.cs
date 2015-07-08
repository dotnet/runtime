// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.IO
{
    internal static partial class PathHelpers
    {
        internal static int GetRootLength(string path)
        {
            CheckInvalidPathChars(path);
            return path.Length > 0 && IsDirectorySeparator(path[0]) ? 1 : 0;
        }

        internal static void CheckSearchPattern(string searchPattern)
        {
            // ".." should not be used to move up directories. On Windows, this is more strict, and ".."
            // can only be used in particular places in a name, whereas on Unix it can be used anywhere.
            // So, throw if we find a ".." that's its own component in the path.
            for (int index = 0; (index = searchPattern.IndexOf("..", index, StringComparison.Ordinal)) >= 0; index += 2)
            {
                if ((index == 0 || IsDirectorySeparator(searchPattern[index - 1])) && // previous character is directory separator
                    (index + 2 == searchPattern.Length || IsDirectorySeparator(searchPattern[index + 2]))) // next character is directory separator
                {
                    throw new ArgumentException(SR.Arg_InvalidSearchPattern, "searchPattern");
                }
            }
        }

        internal static bool IsDirectorySeparator(char c)
        {
            return c == Path.DirectorySeparatorChar;
        }

        internal static string GetFullPathInternal(string path)
        {
            // The Windows implementation trims off whitespace from the start and end of the path,
            // and then based on whether that trimmed path is rooted decides to use the trimmed
            // or original version.  On Unix, a filename can be composed of entirely whitespace
            // characters, so we can't legimately do such trimming.  As such, we just delegate
            // to the real GetFullPath.
            return Path.GetFullPath(path);
        }
    }
}
