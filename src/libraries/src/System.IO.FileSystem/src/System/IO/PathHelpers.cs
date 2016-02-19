// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        // System.IO.Path has both public Combine and internal InternalCombine
        // members.  InternalCombine performs these extra validations on the second 
        // argument.  This provides a convenient helper to maintain this extra
        // validation when porting code from Path.InternalCombine to Path.Combine.
        internal static void ThrowIfEmptyOrRootedPath(string path2)
        {
            if (path2 == null)
                throw new ArgumentNullException(nameof(path2));
            if (path2.Length == 0)
                throw new ArgumentException(SR.Argument_PathEmpty, nameof(path2));
            if (Path.IsPathRooted(path2))
                throw new ArgumentException(SR.Arg_Path2IsRooted, nameof(path2));
        }

        internal static bool IsRoot(string path)
        {
            return path.Length == PathInternal.GetRootLength(path);
        }

        internal static bool EndsInDirectorySeparator(String path)
        {
            return path.Length > 0 && PathInternal.IsDirectorySeparator(path[path.Length - 1]);
        }

        internal static string TrimEndingDirectorySeparator(string path)
        {
            return EndsInDirectorySeparator(path) ?
                path.Substring(0, path.Length - 1) :
                path;
        }
    }
}
