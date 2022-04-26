// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Enumeration;

namespace System.IO
{
    public static partial class Directory
    {
        public static DirectoryInfo? GetParent(string path)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);

            string fullPath = Path.GetFullPath(path);

            string? s = Path.GetDirectoryName(fullPath);
            if (s == null)
                return null;
            return new DirectoryInfo(s);
        }

        public static DirectoryInfo CreateDirectory(string path)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);

            string fullPath = Path.GetFullPath(path);

            FileSystem.CreateDirectory(fullPath);

            return new DirectoryInfo(path, fullPath, isNormalized: true);
        }

        // Tests if the given path refers to an existing DirectoryInfo on disk.
        public static bool Exists([NotNullWhen(true)] string? path)
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
            => new List<string>(InternalEnumeratePaths(path, searchPattern, SearchTarget.Files, enumerationOptions)).ToArray();

        public static string[] GetDirectories(string path) => GetDirectories(path, "*", enumerationOptions: EnumerationOptions.Compatible);

        public static string[] GetDirectories(string path, string searchPattern) => GetDirectories(path, searchPattern, enumerationOptions: EnumerationOptions.Compatible);

        public static string[] GetDirectories(string path, string searchPattern, SearchOption searchOption)
            => GetDirectories(path, searchPattern, EnumerationOptions.FromSearchOption(searchOption));

        public static string[] GetDirectories(string path, string searchPattern, EnumerationOptions enumerationOptions)
            => new List<string>(InternalEnumeratePaths(path, searchPattern, SearchTarget.Directories, enumerationOptions)).ToArray();

        public static string[] GetFileSystemEntries(string path) => GetFileSystemEntries(path, "*", enumerationOptions: EnumerationOptions.Compatible);

        public static string[] GetFileSystemEntries(string path, string searchPattern) => GetFileSystemEntries(path, searchPattern, enumerationOptions: EnumerationOptions.Compatible);

        public static string[] GetFileSystemEntries(string path, string searchPattern, SearchOption searchOption)
            => GetFileSystemEntries(path, searchPattern, EnumerationOptions.FromSearchOption(searchOption));

        public static string[] GetFileSystemEntries(string path, string searchPattern, EnumerationOptions enumerationOptions)
            => new List<string>(InternalEnumeratePaths(path, searchPattern, SearchTarget.Both, enumerationOptions)).ToArray();

        internal static IEnumerable<string> InternalEnumeratePaths(
            string path,
            string searchPattern,
            SearchTarget searchTarget,
            EnumerationOptions options)
        {
            ArgumentNullException.ThrowIfNull(path);
            ArgumentNullException.ThrowIfNull(searchPattern);

            FileSystemEnumerableFactory.NormalizeInputs(ref path, ref searchPattern, options.MatchType);

            return searchTarget switch
            {
                SearchTarget.Files => FileSystemEnumerableFactory.UserFiles(path, searchPattern, options),
                SearchTarget.Directories => FileSystemEnumerableFactory.UserDirectories(path, searchPattern, options),
                SearchTarget.Both => FileSystemEnumerableFactory.UserEntries(path, searchPattern, options),
                _ => throw new ArgumentOutOfRangeException(nameof(searchTarget)),
            };
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
            => InternalEnumeratePaths(path, searchPattern, SearchTarget.Both, enumerationOptions);

        public static string GetDirectoryRoot(string path)
        {
            ArgumentNullException.ThrowIfNull(path);

            string fullPath = Path.GetFullPath(path);
            string root = Path.GetPathRoot(fullPath)!;

            return root;
        }

        public static string GetCurrentDirectory() => Environment.CurrentDirectory;

        public static void SetCurrentDirectory(string path)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);

            Environment.CurrentDirectory = Path.GetFullPath(path);
        }

        public static void Move(string sourceDirName, string destDirName)
        {
            ArgumentException.ThrowIfNullOrEmpty(sourceDirName);
            ArgumentException.ThrowIfNullOrEmpty(destDirName);

            FileSystem.MoveDirectory(Path.GetFullPath(sourceDirName), Path.GetFullPath(destDirName));
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

        /// <summary>
        /// Creates a directory symbolic link identified by <paramref name="path"/> that points to <paramref name="pathToTarget"/>.
        /// </summary>
        /// <param name="path">The absolute path where the symbolic link should be created.</param>
        /// <param name="pathToTarget">The target directory of the symbolic link.</param>
        /// <returns>A <see cref="DirectoryInfo"/> instance that wraps the newly created directory symbolic link.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> or <paramref name="pathToTarget"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> or <paramref name="pathToTarget"/> is empty.
        /// -or-
        /// <paramref name="path"/> is not an absolute path.
        /// -or-
        /// <paramref name="path"/> or <paramref name="pathToTarget"/> contains invalid path characters.</exception>
        /// <exception cref="IOException">A file or directory already exists in the location of <paramref name="path"/>.
        /// -or-
        /// An I/O error occurred.</exception>
        public static FileSystemInfo CreateSymbolicLink(string path, string pathToTarget)
        {
            string fullPath = Path.GetFullPath(path);
            FileSystem.VerifyValidPath(pathToTarget, nameof(pathToTarget));

            FileSystem.CreateSymbolicLink(path, pathToTarget, isDirectory: true);
            return new DirectoryInfo(originalPath: path, fullPath: fullPath, isNormalized: true);
        }

        /// <summary>
        /// Gets the target of the specified directory link.
        /// </summary>
        /// <param name="linkPath">The path of the directory link.</param>
        /// <param name="returnFinalTarget"><see langword="true"/> to follow links to the final target; <see langword="false"/> to return the immediate next link.</param>
        /// <returns>A <see cref="DirectoryInfo"/> instance if <paramref name="linkPath"/> exists, independently if the target exists or not. <see langword="null"/> if <paramref name="linkPath"/> is not a link.</returns>
        /// <exception cref="IOException">The directory on <paramref name="linkPath"/> does not exist.
        /// -or-
        /// The link's file system entry type is inconsistent with that of its target.
        /// -or-
        /// Too many levels of symbolic links.</exception>
        /// <remarks>When <paramref name="returnFinalTarget"/> is <see langword="true"/>, the maximum number of symbolic links that are followed are 40 on Unix and 63 on Windows.</remarks>
        public static FileSystemInfo? ResolveLinkTarget(string linkPath, bool returnFinalTarget)
        {
            FileSystem.VerifyValidPath(linkPath, nameof(linkPath));
            return FileSystem.ResolveLinkTarget(linkPath, returnFinalTarget, isDirectory: true);
        }
    }
}
