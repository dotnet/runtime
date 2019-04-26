// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.Configuration
{
    internal static class UrlPath
    {
        private const string FileUrlLocal = "file:///";
        private const string FileUrlUnc = "file:";

        internal static string GetDirectoryOrRootName(string path)
        {
            return Path.GetDirectoryName(path) ?? Path.GetPathRoot(path);
        }

        // Determine if subdir is equal to or a subdirectory of dir.
        // For example, c:\mydir\subdir is a subdirectory of c:\mydir
        // Account for optional trailing backslashes.
        internal static bool IsEqualOrSubdirectory(string dir, string subdir)
        {
            if (string.IsNullOrEmpty(dir))
                return true;

            if (string.IsNullOrEmpty(subdir))
                return false;

            // Compare up to but not including trailing backslash
            int lDir = dir.Length;
            if (dir[lDir - 1] == '\\' || dir[lDir - 1] == '/') lDir -= 1;

            int lSubdir = subdir.Length;
            if (subdir[lSubdir - 1] == '\\' || dir[lDir - 1] == '/') lSubdir -= 1;

            if (lSubdir < lDir)
                return false;

            if (string.Compare(dir, 0, subdir, 0, lDir, StringComparison.OrdinalIgnoreCase) != 0)
                return false;

            // Check subdir that character following length of dir is a backslash
            return (lSubdir <= lDir) || (subdir[lDir] == '\\') || (subdir[lDir] == '/');
        }

        // NOTE: This function is also present in fx\src\xsp\system\web\util\urlpath.cs
        // Please propagate any changes to that file.
        //
        // Determine if subpath is equal to or a subpath of path.
        // For example, /myapp/foo.aspx is a subpath of /myapp
        // Account for optional trailing slashes.
        internal static bool IsEqualOrSubpath(string path, string subpath)
        {
            return IsEqualOrSubpathImpl(path, subpath, false);
        }

        // Determine if subpath is a subpath of path, but return
        // false if subpath & path are the same.
        // For example, /myapp/foo.aspx is a subpath of /myapp
        // Account for optional trailing slashes.
        internal static bool IsSubpath(string path, string subpath)
        {
            return IsEqualOrSubpathImpl(path, subpath, true);
        }

        private static bool IsEqualOrSubpathImpl(string path, string subpath, bool excludeEqual)
        {
            if (string.IsNullOrEmpty(path))
                return true;

            if (string.IsNullOrEmpty(subpath))
                return false;

            // Compare up to but not including trailing slash
            int lPath = path.Length;
            if (path[lPath - 1] == '/') lPath -= 1;

            int lSubpath = subpath.Length;
            if (subpath[lSubpath - 1] == '/') lSubpath -= 1;

            if (lSubpath < lPath)
                return false;

            if (excludeEqual && (lSubpath == lPath))
                return false;

            if (string.Compare(path, 0, subpath, 0, lPath, StringComparison.OrdinalIgnoreCase) != 0)
                return false;

            // Check subpath that character following length of path is a slash
            return (lSubpath <= lPath) || (subpath[lPath] == '/');
        }

        private static bool IsDirectorySeparatorChar(char ch)
        {
            return (ch == '\\') || (ch == '/');
        }

        private static bool IsAbsoluteLocalPhysicalPath(string path)
        {
            if ((path == null) || (path.Length < 3))
                return false;

            // e.g c:\foo
            return (path[1] == ':') && IsDirectorySeparatorChar(path[2]);
        }

        private static bool IsAbsoluteUncPhysicalPath(string path)
        {
            if ((path == null) || (path.Length < 3))
                return false;

            // e.g \\server\share\foo or //server/share/foo
            return IsDirectorySeparatorChar(path[0]) && IsDirectorySeparatorChar(path[1]);
        }

        internal static string ConvertFileNameToUrl(string fileName)
        {
            string prefix;

            if (IsAbsoluteLocalPhysicalPath(fileName)) prefix = FileUrlLocal;
            else
            {
                if (IsAbsoluteUncPhysicalPath(fileName)) prefix = FileUrlUnc;
                else
                {
                    // We should never get here, but if we do we are likely to have
                    // serious security problems, so throw an exception rather than simply 
                    // asserting.
                    throw ExceptionUtil.ParameterInvalid(nameof(fileName));
                }
            }

            string newFileName = prefix + fileName.Replace('\\', '/');
            return newFileName;
        }
    }
}
