// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.IO
{
    /// <summary>Provides an implementation of FileSystem for Unix systems.</summary>
    internal static partial class FileSystem
    {
        public static bool DirectoryExists(ReadOnlySpan<char> fullPath)
        {
            Interop.ErrorInfo ignored;
            return DirectoryExists(fullPath, out ignored);
        }

        private static bool DirectoryExists(ReadOnlySpan<char> fullPath, out Interop.ErrorInfo errorInfo)
        {
            return FileExists(fullPath, Interop.Sys.FileTypes.S_IFDIR, out errorInfo);
        }

        public static bool FileExists(ReadOnlySpan<char> fullPath)
        {
            Interop.ErrorInfo ignored;    
            // File.Exists() explicitly checks for a trailing separator and returns false if found. FileInfo.Exists and all other
            // internal usages do not check for the trailing separator. Historically we've always removed the trailing separator
            // when getting attributes as trailing separators are generally not accepted by Windows APIs. Unix will take
            // trailing separators, but it infers that the path must be a directory (it effectively appends "."). To align with
            // our historical behavior (outside of File.Exists()), we need to trim.
            //
            // See http://pubs.opengroup.org/onlinepubs/009695399/basedefs/xbd_chap04.html#tag_04_11 for details.
            return FileExists(Path.TrimEndingDirectorySeparator(fullPath), Interop.Sys.FileTypes.S_IFREG, out ignored);
        }

        private static bool FileExists(ReadOnlySpan<char> fullPath, int fileType, out Interop.ErrorInfo errorInfo)
        {
            Debug.Assert(fileType == Interop.Sys.FileTypes.S_IFREG || fileType == Interop.Sys.FileTypes.S_IFDIR);

            Interop.Sys.FileStatus fileinfo;
            errorInfo = default(Interop.ErrorInfo);

            // First use stat, as we want to follow symlinks.  If that fails, it could be because the symlink
            // is broken, we don't have permissions, etc., in which case fall back to using LStat to evaluate
            // based on the symlink itself.
            if (Interop.Sys.Stat(fullPath, out fileinfo) < 0 &&
                Interop.Sys.LStat(fullPath, out fileinfo) < 0)
            {
                errorInfo = Interop.Sys.GetLastErrorInfo();
                return false;
            }

            // Something exists at this path.  If the caller is asking for a directory, return true if it's
            // a directory and false for everything else.  If the caller is asking for a file, return false for
            // a directory and true for everything else.
            return
                (fileType == Interop.Sys.FileTypes.S_IFDIR) ==
                ((fileinfo.Mode & Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFDIR);
        } 
    }
}
