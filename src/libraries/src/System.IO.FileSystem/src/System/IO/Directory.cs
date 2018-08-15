// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

#if MS_IO_REDIST
using Microsoft.IO.Enumeration;

namespace Microsoft.IO
#else
using System.IO.Enumeration;

namespace System.IO
#endif
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

            return new DirectoryInfo(path, fullPath, isNormalized: true);
        }

        // Tests if the given path refers to an existing DirectoryInfo on disk.
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

        public static string[] GetFiles(string path) => GetFiles(path, "*", enumerationOptions: EnumerationOptions.Compatible);

        public static string[] GetFiles(string path, string searchPattern) => GetFiles(path, searchPattern, enumerationOptions: EnumerationOptions.Compatible);

        public static string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
            => GetFiles(path, searchPattern, EnumerationOptions.FromSearchOption(searchOption));

        public static string[] GetFiles(string path, string searchPattern, EnumerationOptions enumerationOptions)
            => InternalEnumeratePaths(path, searchPattern, SearchTarget.Files, enumerationOptions).ToArray();

        public static string[] GetDirectories(string path) => GetDirectories(path, "*", enumerationOptions: EnumerationOptions.Compatible);

        public static string[] GetDirectories(string path, string searchPattern) => GetDirectories(path, searchPattern, enumerationOptions: EnumerationOptions.Compatible);

        public static string[] GetDirectories(string path, string searchPattern, SearchOption searchOption)
            => GetDirectories(path, searchPattern, EnumerationOptions.FromSearchOption(searchOption));

        public static string[] GetDirectories(string path, string searchPattern, EnumerationOptions enumerationOptions)
            => InternalEnumeratePaths(path, searchPattern, SearchTarget.Directories, enumerationOptions).ToArray();

        public static string[] GetFileSystemEntries(string path) => GetFileSystemEntries(path, "*", enumerationOptions: EnumerationOptions.Compatible);

        public static string[] GetFileSystemEntries(string path, string searchPattern) => GetFileSystemEntries(path, searchPattern, enumerationOptions: EnumerationOptions.Compatible);

        public static string[] GetFileSystemEntries(string path, string searchPattern, SearchOption searchOption)
            => GetFileSystemEntries(path, searchPattern, EnumerationOptions.FromSearchOption(searchOption));

        public static string[] GetFileSystemEntries(string path, string searchPattern, EnumerationOptions enumerationOptions)
            => InternalEnumeratePaths(path, searchPattern, SearchTarget.Both, enumerationOptions).ToArray();

        internal static IEnumerable<string> InternalEnumeratePaths(
            string path,
            string searchPattern,
            SearchTarget searchTarget,
            EnumerationOptions options)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));

            FileSystemEnumerableFactory.NormalizeInputs(ref path, ref searchPattern, options);

            switch (searchTarget)
            {
                case SearchTarget.Files:
                    return FileSystemEnumerableFactory.UserFiles(path, searchPattern, options);
                case SearchTarget.Directories:
                    return FileSystemEnumerableFactory.UserDirectories(path, searchPattern, options);
                case SearchTarget.Both:
                    return FileSystemEnumerableFactory.UserEntries(path, searchPattern, options);
                default:
                    throw new ArgumentOutOfRangeException(nameof(searchTarget));
            }
        }

        public static IEnumerable<string> EnumerateDirectories(string path) => EnumerateDirectories(path, "*", enumerationOptions: EnumerationOptions.Compatible);

        public static IEnumerable<string> EnumerateDirectories(string path, string searchPattern) => EnumerateDirectories(path, searchPattern, enumerationOptions: EnumerationOptions.Compatible);

        public static IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption searchOption)
            => EnumerateDirectories(path, searchPattern, EnumerationOptions.FromSearchOption(searchOption));

        public static IEnumerable<string> EnumerateDirectories(string path, string searchPattern, EnumerationOptions enumerationOptions)
            => InternalEnumeratePaths(path, searchPattern, SearchTarget.Directories, enumerationOptions);

        public static IEnumerable<string> EnumerateFiles(string path) => EnumerateFiles(path, "*", enumerationOptions: EnumerationOptions.Compatible);

        public static IEnumerable<string> EnumerateFiles(string path, string searchPattern)
            => EnumerateFiles(path, searchPattern, enumerationOptions: EnumerationOptions.Compatible);

        public static IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
            => EnumerateFiles(path, searchPattern, EnumerationOptions.FromSearchOption(searchOption));

        public static IEnumerable<string> EnumerateFiles(string path, string searchPattern, EnumerationOptions enumerationOptions)
            => InternalEnumeratePaths(path, searchPattern, SearchTarget.Files, enumerationOptions);

        public static IEnumerable<string> EnumerateFileSystemEntries(string path)
            => EnumerateFileSystemEntries(path, "*", enumerationOptions: EnumerationOptions.Compatible);

        public static IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern)
            => EnumerateFileSystemEntries(path, searchPattern, enumerationOptions: EnumerationOptions.Compatible);

        public static IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, SearchOption searchOption)
            => EnumerateFileSystemEntries(path, searchPattern, EnumerationOptions.FromSearchOption(searchOption));

        public static IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, EnumerationOptions enumerationOptions)
            =>  InternalEnumeratePaths(path, searchPattern, SearchTarget.Both, enumerationOptions);

        public static string GetDirectoryRoot(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            string fullPath = Path.GetFullPath(path);
            string root = fullPath.Substring(0, PathInternal.GetRootLength(fullPath.AsSpan()));

            return root;
        }

        internal static string InternalGetDirectoryRoot(string path)
        {
            if (path == null) return null;
            return path.Substring(0, PathInternal.GetRootLength(path.AsSpan()));
        }

        public static string GetCurrentDirectory() => Environment.CurrentDirectory;

        public static void SetCurrentDirectory(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (path.Length == 0)
                throw new ArgumentException(SR.Argument_PathEmpty, nameof(path));

            Environment.CurrentDirectory = Path.GetFullPath(path);
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
            string sourcePath = PathInternal.EnsureTrailingSeparator(fullsourceDirName);

            string fulldestDirName = Path.GetFullPath(destDirName);
            string destPath = PathInternal.EnsureTrailingSeparator(fulldestDirName);

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

