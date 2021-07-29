// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            Interop.Sys.FileStatus fileinfo;
            errorInfo = default(Interop.ErrorInfo);

            if (Interop.Sys.Stat(fullPath, out fileinfo) < 0)
            {
                errorInfo = Interop.Sys.GetLastErrorInfo();
                return false;
            }

            return (fileinfo.Mode & Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFDIR;
        }

        public static bool FileExists(ReadOnlySpan<char> fullPath)
        {
            Interop.ErrorInfo ignored;
            return FileExists(fullPath, out ignored);
        }

        private static bool FileExists(ReadOnlySpan<char> fullPath, out Interop.ErrorInfo errorInfo)
        {
            Interop.Sys.FileStatus fileinfo;
            errorInfo = default(Interop.ErrorInfo);

            // File.Exists() explicitly checks for a trailing separator and returns false if found. FileInfo.Exists and all other
            // internal usages do not check for the trailing separator. Historically we've always removed the trailing separator
            // when getting attributes as trailing separators are generally not accepted by Windows APIs. Unix will take
            // trailing separators, but it infers that the path must be a directory (it effectively appends "."). To align with
            // our historical behavior (outside of File.Exists()), we need to trim.
            //
            // See http://pubs.opengroup.org/onlinepubs/009695399/basedefs/xbd_chap04.html#tag_04_11 for details.
            fullPath = Path.TrimEndingDirectorySeparator(fullPath);

            if (Interop.Sys.LStat(fullPath, out fileinfo) < 0)
            {
                errorInfo = Interop.Sys.GetLastErrorInfo();
                return false;
            }

            // Something exists at this path. Return false for a directory and true for everything else.
            // When the path is a link, get its target info.
            if ((fileinfo.Mode & Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFLNK)
            {
                if (Interop.Sys.Stat(fullPath, out fileinfo) < 0)
                {
                    return true;
                }
            }

            return (fileinfo.Mode & Interop.Sys.FileTypes.S_IFMT) != Interop.Sys.FileTypes.S_IFDIR;
        }
    }
}
