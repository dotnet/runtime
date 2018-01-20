// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Security;

namespace System.IO
{
    public static partial class Directory
    {
        public static DirectoryInfo GetParent(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (path.Length == 0)
                throw new ArgumentException(SR.Argument_PathEmpty, nameof(path));

            string fullPath = Path.GetFullPath(path);

            string s = Path.GetDirectoryName(fullPath);
            if (s == null)
                return null;
            return new DirectoryInfo(s);
        }

        public static DirectoryInfo CreateDirectory(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (path.Length == 0)
                throw new ArgumentException(SR.Argument_PathEmpty, nameof(path));

            string fullPath = Path.GetFullPath(path);

            FileSystem.CreateDirectory(fullPath);

            return new DirectoryInfo(fullPath, null);
        }

        // Input to this method should already be fullpath. This method will ensure that we append 
        // the trailing slash only when appropriate.
        internal static string EnsureTrailingDirectorySeparator(string fullPath)
        {
            string fullPathWithTrailingDirectorySeparator;

            if (!PathHelpers.EndsInDirectorySeparator(fullPath))
                fullPathWithTrailingDirectorySeparator = fullPath + PathHelpers.DirectorySeparatorCharAsString;
            else
                fullPathWithTrailingDirectorySeparator = fullPath;

            return fullPathWithTrailingDirectorySeparator;
        }


        // Tests if the given path refers to an existing DirectoryInfo on disk.
        // 
        // Your application must have Read permission to the directory's
        // contents.
        //
        public static bool Exists(string path)
        {
            try
            {
                if (path == null)
                    return false;
                if (path.Length == 0)
                    return false;

                string fullPath = Path.GetFullPath(path);

                return FileSystem.DirectoryExists(fullPath);
            }
            catch (ArgumentException) { }
            catch (NotSupportedException) { }  // Security can throw this on ":"
            catch (SecurityException) { }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            return false;
        }

        public static void SetCreationTime(string path, DateTime creationTime)
        {
            string fullPath = Path.GetFullPath(path);
            FileSystem.SetCreationTime(fullPath, creationTime, asDirectory: true);
        }

        public static void SetCreationTimeUtc(string path, DateTime creationTimeUtc)
        {
            string fullPath = Path.GetFullPath(path);
            FileSystem.SetCreationTime(fullPath, File.GetUtcDateTimeOffset(creationTimeUtc), asDirectory: true);
        }

        public static DateTime GetCreationTime(string path)
        {
            return File.GetCreationTime(path);
        }

        public static DateTime GetCreationTimeUtc(string path)
        {
            return File.GetCreationTimeUtc(path);
        }
 
        public static void SetLastWriteTime(string path, DateTime lastWriteTime)
        {
            string fullPath = Path.GetFullPath(path);
            FileSystem.SetLastWriteTime(fullPath, lastWriteTime, asDirectory: true);
        }

        public static void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc)
        {
            string fullPath = Path.GetFullPath(path);
            FileSystem.SetLastWriteTime(fullPath, File.GetUtcDateTimeOffset(lastWriteTimeUtc), asDirectory: true);
        }

        public static DateTime GetLastWriteTime(string path)
        {
            return File.GetLastWriteTime(path);
        }

        public static DateTime GetLastWriteTimeUtc(string path)
        {
            return File.GetLastWriteTimeUtc(path);
        }

        public static void SetLastAccessTime(string path, DateTime lastAccessTime)
        {
            string fullPath = Path.GetFullPath(path);
            FileSystem.SetLastAccessTime(fullPath, lastAccessTime, asDirectory: true);
        }

        public static void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc)
        {
            string fullPath = Path.GetFullPath(path);
            FileSystem.SetLastAccessTime(fullPath, File.GetUtcDateTimeOffset(lastAccessTimeUtc), asDirectory: true);
        }

        public static DateTime GetLastAccessTime(string path)
        {
            return File.GetLastAccessTime(path);
        }

        public static DateTime GetLastAccessTimeUtc(string path)
        {
            return File.GetLastAccessTimeUtc(path);
        }

        // Returns an array of filenames in the DirectoryInfo specified by path
        public static string[] GetFiles(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            return InternalGetFiles(path, "*", SearchOption.TopDirectoryOnly);
        }

        // Returns an array of Files in the current DirectoryInfo matching the 
        // given search pattern (i.e. "*.txt").
        public static string[] GetFiles(string path, string searchPattern)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));

            return InternalGetFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        // Returns an array of Files in the current DirectoryInfo matching the 
        // given search pattern (i.e. "*.txt") and search option
        public static string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException(nameof(searchOption), SR.ArgumentOutOfRange_Enum);

            return InternalGetFiles(path, searchPattern, searchOption);
        }

        // Returns an array of Files in the current DirectoryInfo matching the 
        // given search pattern (i.e. "*.txt") and search option
        private static string[] InternalGetFiles(string path, string searchPattern, SearchOption searchOption)
        {
            Debug.Assert(path != null);
            Debug.Assert(searchPattern != null);
            Debug.Assert(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

            return InternalGetFileDirectoryNames(path, path, searchPattern, true, false, searchOption);
        }

        // Returns an array of Directories in the current directory.
        public static string[] GetDirectories(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            return InternalGetDirectories(path, "*", SearchOption.TopDirectoryOnly);
        }

        // Returns an array of Directories in the current DirectoryInfo matching the 
        // given search criteria (i.e. "*.txt").
        public static string[] GetDirectories(string path, string searchPattern)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));

            return InternalGetDirectories(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        // Returns an array of Directories in the current DirectoryInfo matching the 
        // given search criteria (i.e. "*.txt").
        public static string[] GetDirectories(string path, string searchPattern, SearchOption searchOption)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException(nameof(searchOption), SR.ArgumentOutOfRange_Enum);

            return InternalGetDirectories(path, searchPattern, searchOption);
        }

        // Returns an array of Directories in the current DirectoryInfo matching the 
        // given search criteria (i.e. "*.txt").
        private static string[] InternalGetDirectories(string path, string searchPattern, SearchOption searchOption)
        {
            Debug.Assert(path != null);
            Debug.Assert(searchPattern != null);
            Debug.Assert(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

            return InternalGetFileDirectoryNames(path, path, searchPattern, false, true, searchOption);
        }

        // Returns an array of strongly typed FileSystemInfo entries in the path
        public static string[] GetFileSystemEntries(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            return InternalGetFileSystemEntries(path, "*", SearchOption.TopDirectoryOnly);
        }

        // Returns an array of strongly typed FileSystemInfo entries in the path with the
        // given search criteria (i.e. "*.txt"). We disallow .. as a part of the search criteria
        public static string[] GetFileSystemEntries(string path, string searchPattern)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));

            return InternalGetFileSystemEntries(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        // Returns an array of strongly typed FileSystemInfo entries in the path with the
        // given search criteria (i.e. "*.txt"). We disallow .. as a part of the search criteria
        public static string[] GetFileSystemEntries(string path, string searchPattern, SearchOption searchOption)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException(nameof(searchOption), SR.ArgumentOutOfRange_Enum);

            return InternalGetFileSystemEntries(path, searchPattern, searchOption);
        }

        private static string[] InternalGetFileSystemEntries(string path, string searchPattern, SearchOption searchOption)
        {
            Debug.Assert(path != null);
            Debug.Assert(searchPattern != null);
            Debug.Assert(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

            return InternalGetFileDirectoryNames(path, path, searchPattern, true, true, searchOption);
        }

        // Returns fully qualified user path of dirs/files that matches the search parameters. 
        // For recursive search this method will search through all the sub dirs  and execute 
        // the given search criteria against every dir.
        // For all the dirs/files returned, it will then demand path discovery permission for 
        // their parent folders (it will avoid duplicate permission checks)
        internal static string[] InternalGetFileDirectoryNames(string path, string userPathOriginal, string searchPattern, bool includeFiles, bool includeDirs, SearchOption searchOption)
        {
            Debug.Assert(path != null);
            Debug.Assert(userPathOriginal != null);
            Debug.Assert(searchPattern != null);
            Debug.Assert(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

            IEnumerable<string> enumerable = FileSystem.EnumeratePaths(path, searchPattern, searchOption,
                (includeFiles ? SearchTarget.Files : 0) | (includeDirs ? SearchTarget.Directories : 0));
            return EnumerableHelpers.ToArray(enumerable);
        }

        public static IEnumerable<string> EnumerateDirectories(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            return InternalEnumerateDirectories(path, "*", SearchOption.TopDirectoryOnly);
        }

        public static IEnumerable<string> EnumerateDirectories(string path, string searchPattern)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));

            return InternalEnumerateDirectories(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        public static IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption searchOption)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException(nameof(searchOption), SR.ArgumentOutOfRange_Enum);

            return InternalEnumerateDirectories(path, searchPattern, searchOption);
        }

        private static IEnumerable<string> InternalEnumerateDirectories(string path, string searchPattern, SearchOption searchOption)
        {
            Debug.Assert(path != null);
            Debug.Assert(searchPattern != null);
            Debug.Assert(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

            return EnumerateFileSystemNames(path, searchPattern, searchOption, false, true);
        }

        public static IEnumerable<string> EnumerateFiles(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            return InternalEnumerateFiles(path, "*", SearchOption.TopDirectoryOnly);
        }

        public static IEnumerable<string> EnumerateFiles(string path, string searchPattern)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));

            return InternalEnumerateFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        public static IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException(nameof(searchOption), SR.ArgumentOutOfRange_Enum);

            return InternalEnumerateFiles(path, searchPattern, searchOption);
        }

        private static IEnumerable<string> InternalEnumerateFiles(string path, string searchPattern, SearchOption searchOption)
        {
            Debug.Assert(path != null);
            Debug.Assert(searchPattern != null);
            Debug.Assert(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

            return EnumerateFileSystemNames(path, searchPattern, searchOption, true, false);
        }

        public static IEnumerable<string> EnumerateFileSystemEntries(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            return InternalEnumerateFileSystemEntries(path, "*", SearchOption.TopDirectoryOnly);
        }

        public static IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));

            return InternalEnumerateFileSystemEntries(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        public static IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, SearchOption searchOption)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException(nameof(searchOption), SR.ArgumentOutOfRange_Enum);

            return InternalEnumerateFileSystemEntries(path, searchPattern, searchOption);
        }

        private static IEnumerable<string> InternalEnumerateFileSystemEntries(string path, string searchPattern, SearchOption searchOption)
        {
            Debug.Assert(path != null);
            Debug.Assert(searchPattern != null);
            Debug.Assert(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

            return EnumerateFileSystemNames(path, searchPattern, searchOption, true, true);
        }

        private static IEnumerable<string> EnumerateFileSystemNames(string path, string searchPattern, SearchOption searchOption,
                                                            bool includeFiles, bool includeDirs)
        {
            Debug.Assert(path != null);
            Debug.Assert(searchPattern != null);
            Debug.Assert(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

            return FileSystem.EnumeratePaths(path, searchPattern, searchOption,
                (includeFiles ? SearchTarget.Files : 0) | (includeDirs ? SearchTarget.Directories : 0));
        }

        public static string GetDirectoryRoot(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            string fullPath = Path.GetFullPath(path);
            string root = fullPath.Substring(0, PathInternal.GetRootLength(fullPath));

            return root;
        }

        internal static string InternalGetDirectoryRoot(string path)
        {
            if (path == null) return null;
            return path.Substring(0, PathInternal.GetRootLength(path));
        }

        /*===============================CurrentDirectory===============================
       **Action:  Provides a getter and setter for the current directory.  The original
       **         current DirectoryInfo is the one from which the process was started.  
       **Returns: The current DirectoryInfo (from the getter).  Void from the setter.
       **Arguments: The current DirectoryInfo to which to switch to the setter.
       **Exceptions: 
       ==============================================================================*/
        public static string GetCurrentDirectory()
        {
            return FileSystem.GetCurrentDirectory();
        }

        public static void SetCurrentDirectory(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (path.Length == 0)
                throw new ArgumentException(SR.Argument_PathEmpty, nameof(path));

            string fulldestDirName = Path.GetFullPath(path);

            FileSystem.SetCurrentDirectory(fulldestDirName);
        }

        public static void Move(string sourceDirName, string destDirName)
        {
            if (sourceDirName == null)
                throw new ArgumentNullException(nameof(sourceDirName));
            if (sourceDirName.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyFileName, nameof(sourceDirName));

            if (destDirName == null)
                throw new ArgumentNullException(nameof(destDirName));
            if (destDirName.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyFileName, nameof(destDirName));

            string fullsourceDirName = Path.GetFullPath(sourceDirName);
            string sourcePath = EnsureTrailingDirectorySeparator(fullsourceDirName);

            string fulldestDirName = Path.GetFullPath(destDirName);
            string destPath = EnsureTrailingDirectorySeparator(fulldestDirName);

            StringComparison pathComparison = PathInternal.StringComparison;

            if (string.Equals(sourcePath, destPath, pathComparison))
                throw new IOException(SR.IO_SourceDestMustBeDifferent);

            string sourceRoot = Path.GetPathRoot(sourcePath);
            string destinationRoot = Path.GetPathRoot(destPath);
            if (!string.Equals(sourceRoot, destinationRoot, pathComparison))
                throw new IOException(SR.IO_SourceDestMustHaveSameRoot);

            // Windows will throw if the source file/directory doesn't exist, we preemptively check
            // to make sure our cross platform behavior matches NetFX behavior.
            if (!FileSystem.DirectoryExists(fullsourceDirName) && !FileSystem.FileExists(fullsourceDirName))
                throw new DirectoryNotFoundException(SR.Format(SR.IO_PathNotFound_Path, fullsourceDirName));
            
            if (FileSystem.DirectoryExists(fulldestDirName))
                throw new IOException(SR.Format(SR.IO_AlreadyExists_Name, fulldestDirName));

            FileSystem.MoveDirectory(fullsourceDirName, fulldestDirName);
        }

        public static void Delete(string path)
        {
            string fullPath = Path.GetFullPath(path);
            FileSystem.RemoveDirectory(fullPath, false);
        }

        public static void Delete(string path, bool recursive)
        {
            string fullPath = Path.GetFullPath(path);
            FileSystem.RemoveDirectory(fullPath, recursive);
        }

        public static string[] GetLogicalDrives()
        {
            return FileSystem.GetLogicalDrives();
        }
    }
}

