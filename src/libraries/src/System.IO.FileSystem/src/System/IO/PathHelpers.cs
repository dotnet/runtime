// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.IO
{
    // Helper methods related to paths.  Some of these are copies of 
    // internal members of System.IO.Path from System.Runtime.Extensions.dll.
    internal static partial class PathHelpers
    {
        // Array of the separator chars
        internal static readonly char[] DirectorySeparatorChars = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        // String-representation of the directory-separator character, used when appending the character to another
        // string so as to avoid the boxing of the character when calling String.Concat(..., object).
        internal static readonly string DirectorySeparatorCharAsString = Path.DirectorySeparatorChar.ToString();

        internal static void CheckInvalidPathChars(String path, bool checkAdditional = false)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            if (PathInternal.HasIllegalCharacters(path, checkAdditional))
                throw new ArgumentException(SR.Argument_InvalidPathChars, "path");
        }

        // System.IO.Path has both public Combine and internal InternalCombine
        // members.  InternalCombine performs these extra validations on the second 
        // argument.  This provides a convenient helper to maintain this extra
        // validation when porting code from Path.InternalCombine to Path.Combine.
        internal static void ThrowIfEmptyOrRootedPath(string path2)
        {
            if (path2 == null)
                throw new ArgumentNullException("path2");
            if (path2.Length == 0)
                throw new ArgumentException(SR.Argument_PathEmpty, "path2");
            if (Path.IsPathRooted(path2))
                throw new ArgumentException(SR.Arg_Path2IsRooted, "path2");
        }

        internal static bool EndsInDirectorySeparator(String path)
        {
            return path.Length > 0 && IsDirectorySeparator(path[path.Length - 1]);
        }
    }
}
