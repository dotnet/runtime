// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Internal.IO
{
    internal static partial class File
    {
        internal static bool InternalExists(string fullPath)
        {
            Interop.Sys.FileStatus fileinfo;

            // First use stat, as we want to follow symlinks.  If that fails, it could be because the symlink
            // is broken, we don't have permissions, etc., in which case fall back to using LStat to evaluate
            // based on the symlink itself.
            if (Interop.Sys.Stat(fullPath, out fileinfo) < 0 &&
                Interop.Sys.LStat(fullPath, out fileinfo) < 0)
            {
                return false;
            }

            return ((fileinfo.Mode & Interop.Sys.FileTypes.S_IFMT) != Interop.Sys.FileTypes.S_IFDIR);
        }
    }
}
