// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

#if MS_IO_REDIST
using Microsoft.IO.Enumeration;

namespace Microsoft.IO
#else
using System.IO.Enumeration;

namespace System.IO
#endif
{
    public sealed partial class DirectoryInfo : FileSystemInfo
    {
        public DirectoryInfo(string path)
        {
            Init(originalPath: path,
                  fullPath: Path.GetFullPath(path),
                  isNormalized: true);
        }

        internal DirectoryInfo(string originalPath, string fullPath = null, string fileName = null, bool isNormalized = false)
        {
            Init(originalPath, fullPath, fileName, isNormalized);
        }

        private void Init(string originalPath, string fullPath = null, string fileName = null, bool isNormalized = false)
        {
            // Want to throw the original argument name
            OriginalPath = originalPath ?? throw new ArgumentNullException("path");

            fullPath = fullPath ?? originalPath;
            fullPath = isNormalized ? fullPath : Path.GetFullPath(fullPath);

            _name = fileName ?? (PathInternal.IsRoot(fullPath.AsSpan()) ?
                    fullPath.AsSpan() :
                    Path.GetFileName(PathInternal.TrimEndingDirectorySeparator(fullPath.AsSpan()))).ToString();

            FullPath = fullPath;
        }

        public DirectoryInfo Parent
        {
            get
            {
                // FullPath might end in either "parent\child" or "parent\child\", and in either case we want 
                // the parent of child, not the child. Trim off an ending directory separator if there is one,
                // but don't mangle the root.
                string parentName = Path.GetDirectoryName(PathInternal.IsRoot(FullPath.AsSpan()) ? FullPath : PathInternal.TrimEndingDirectorySeparator(FullPath));
                return parentName != null ? 
                    new DirectoryInfo(parentName, isNormalized: true) :
                    null;
            }
        }

        public DirectoryInfo CreateSubdirectory(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (PathInternal.IsEffectivelyEmpty(path.AsSpan()))
                throw new ArgumentException(SR.Argument_PathEmpty, nameof(path));
            if (Path.IsPathRooted(path))
                throw new ArgumentException(SR.Arg_Path2IsRooted, nameof(path));

            string newPath = Path.GetFullPath(Path.Combine(FullPath, path));

            ReadOnlySpan<char> trimmedNewPath = PathInternal.TrimEndingDirectorySeparator(newPath.AsSpan());
            ReadOnlySpan<char> trimmedCurrentPath = PathInternal.TrimEndingDirectorySeparator(FullPath.AsSpan());

            // We want to make sure the requested directory is actually under the subdirectory.
            if (trimmedNewPath.StartsWith(trimmedCurrentPath, PathInternal.StringComparison)
                // Allow the exact same path, but prevent allowing "..\FooBar" through when the directory is "Foo"
                && ((trimmedNewPath.Length == trimmedCurrentPath.Length) || PathInternal.IsDirectorySeparator(newPath[trimmedCurrentPath.Length])))
            {
                FileSystem.CreateDirectory(newPath);
                return new DirectoryInfo(newPath);
            }

            // We weren't nested
            throw new ArgumentException(SR.Format(SR.Argument_InvalidSubPath, path, FullPath), nameof(path));
        }

        public void Create() => FileSystem.CreateDirectory(FullPath);

        // Returns an array of Files in the DirectoryInfo specified by path
        public FileInfo[] GetFiles() => GetFiles("*", enumerationOptions: EnumerationOptions.Compatible);

        // Returns an array of Files in the current DirectoryInfo matching the 
        // given search criteria (i.e. "*.txt").
        public FileInfo[] GetFiles(string searchPattern) => GetFiles(searchPattern, enumerationOptions: EnumerationOptions.Compatible);

        public FileInfo[] GetFiles(string searchPattern, SearchOption searchOption)
            => GetFiles(searchPattern, EnumerationOptions.FromSearchOption(searchOption));

        public FileInfo[] GetFiles(string searchPattern, EnumerationOptions enumerationOptions)
            => ((IEnumerable<FileInfo>)InternalEnumerateInfos(FullPath, searchPattern, SearchTarget.Files, enumerationOptions)).ToArray();

        // Returns an array of strongly typed FileSystemInfo entries which will contain a listing
        // of all the files and directories.
        public FileSystemInfo[] GetFileSystemInfos() => GetFileSystemInfos("*", enumerationOptions: EnumerationOptions.Compatible);

        // Returns an array of strongly typed FileSystemInfo entries in the path with the
        // given search criteria (i.e. "*.txt").
        public FileSystemInfo[] GetFileSystemInfos(string searchPattern)
            => GetFileSystemInfos(searchPattern, enumerationOptions: EnumerationOptions.Compatible);

        public FileSystemInfo[] GetFileSystemInfos(string searchPattern, SearchOption searchOption)
            => GetFileSystemInfos(searchPattern, EnumerationOptions.FromSearchOption(searchOption));

        public FileSystemInfo[] GetFileSystemInfos(string searchPattern, EnumerationOptions enumerationOptions)
            => InternalEnumerateInfos(FullPath, searchPattern, SearchTarget.Both, enumerationOptions).ToArray();

        // Returns an array of Directories in the current directory.
        public DirectoryInfo[] GetDirectories() => GetDirectories("*", enumerationOptions: EnumerationOptions.Compatible);

        // Returns an array of Directories in the current DirectoryInfo matching the 
        // given search criteria (i.e. "System*" could match the System & System32 directories).
        public DirectoryInfo[] GetDirectories(string searchPattern) => GetDirectories(searchPattern, enumerationOptions: EnumerationOptions.Compatible);

        public DirectoryInfo[] GetDirectories(string searchPattern, SearchOption searchOption)
            => GetDirectories(searchPattern, EnumerationOptions.FromSearchOption(searchOption));

        public DirectoryInfo[] GetDirectories(string searchPattern, EnumerationOptions enumerationOptions)
            => ((IEnumerable<DirectoryInfo>)InternalEnumerateInfos(FullPath, searchPattern, SearchTarget.Directories, enumerationOptions)).ToArray();

        public IEnumerable<DirectoryInfo> EnumerateDirectories()
            => EnumerateDirectories("*", enumerationOptions: EnumerationOptions.Compatible);

        public IEnumerable<DirectoryInfo> EnumerateDirectories(string searchPattern)
            => EnumerateDirectories(searchPattern, enumerationOptions: EnumerationOptions.Compatible);

        public IEnumerable<DirectoryInfo> EnumerateDirectories(string searchPattern, SearchOption searchOption)
            => EnumerateDirectories(searchPattern, EnumerationOptions.FromSearchOption(searchOption));

        public IEnumerable<DirectoryInfo> EnumerateDirectories(string searchPattern, EnumerationOptions enumerationOptions)
            => (IEnumerable<DirectoryInfo>)InternalEnumerateInfos(FullPath, searchPattern, SearchTarget.Directories, enumerationOptions);

        public IEnumerable<FileInfo> EnumerateFiles()
            => EnumerateFiles("*", enumerationOptions: EnumerationOptions.Compatible);

        public IEnumerable<FileInfo> EnumerateFiles(string searchPattern) => EnumerateFiles(searchPattern, enumerationOptions: EnumerationOptions.Compatible);

        public IEnumerable<FileInfo> EnumerateFiles(string searchPattern, SearchOption searchOption)
            => EnumerateFiles(searchPattern, EnumerationOptions.FromSearchOption(searchOption));

        public IEnumerable<FileInfo> EnumerateFiles(string searchPattern, EnumerationOptions enumerationOptions)
            => (IEnumerable<FileInfo>)InternalEnumerateInfos(FullPath, searchPattern, SearchTarget.Files, enumerationOptions);

        public IEnumerable<FileSystemInfo> EnumerateFileSystemInfos() => EnumerateFileSystemInfos("*", enumerationOptions: EnumerationOptions.Compatible);

        public IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(string searchPattern)
            => EnumerateFileSystemInfos(searchPattern, enumerationOptions: EnumerationOptions.Compatible);

        public IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(string searchPattern, SearchOption searchOption)
            => EnumerateFileSystemInfos(searchPattern, EnumerationOptions.FromSearchOption(searchOption));

        public IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(string searchPattern, EnumerationOptions enumerationOptions)
            => InternalEnumerateInfos(FullPath, searchPattern, SearchTarget.Both, enumerationOptions);

        internal static IEnumerable<FileSystemInfo> InternalEnumerateInfos(
            string path,
            string searchPattern,
            SearchTarget searchTarget,
            EnumerationOptions options)
        {
            Debug.Assert(path != null);
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));

            FileSystemEnumerableFactory.NormalizeInputs(ref path, ref searchPattern, options);

            switch (searchTarget)
            {
                case SearchTarget.Directories:
                    return FileSystemEnumerableFactory.DirectoryInfos(path, searchPattern, options);
                case SearchTarget.Files:
                    return FileSystemEnumerableFactory.FileInfos(path, searchPattern, options);
                case SearchTarget.Both:
                    return FileSystemEnumerableFactory.FileSystemInfos(path, searchPattern, options);
                default:
                    throw new ArgumentException(SR.ArgumentOutOfRange_Enum, nameof(searchTarget));
            }
        }

        public DirectoryInfo Root => new DirectoryInfo(Path.GetPathRoot(FullPath));

        public void MoveTo(string destDirName)
        {
            if (destDirName == null)
                throw new ArgumentNullException(nameof(destDirName));
            if (destDirName.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyFileName, nameof(destDirName));

            string destination = Path.GetFullPath(destDirName);

            string destinationWithSeparator = PathInternal.EnsureTrailingSeparator(destination);
            string sourceWithSeparator = PathInternal.EnsureTrailingSeparator(FullPath);

            if (string.Equals(sourceWithSeparator, destinationWithSeparator, PathInternal.StringComparison))
                throw new IOException(SR.IO_SourceDestMustBeDifferent);

            string sourceRoot = Path.GetPathRoot(sourceWithSeparator);
            string destinationRoot = Path.GetPathRoot(destinationWithSeparator);

            if (!string.Equals(sourceRoot, destinationRoot, PathInternal.StringComparison))
                throw new IOException(SR.IO_SourceDestMustHaveSameRoot);

            // Windows will throw if the source file/directory doesn't exist, we preemptively check
            // to make sure our cross platform behavior matches NetFX behavior.
            if (!Exists && !FileSystem.FileExists(FullPath))
                throw new DirectoryNotFoundException(SR.Format(SR.IO_PathNotFound_Path, FullPath));

            if (FileSystem.DirectoryExists(destination))
                throw new IOException(SR.Format(SR.IO_AlreadyExists_Name, destinationWithSeparator));

            FileSystem.MoveDirectory(FullPath, destination);

            Init(originalPath: destDirName,
                 fullPath: destinationWithSeparator,
                 fileName: null,
                 isNormalized: true);

            // Flush any cached information about the directory.
            Invalidate();
        }

        public override void Delete() => FileSystem.RemoveDirectory(FullPath, recursive: false);

        public void Delete(bool recursive) => FileSystem.RemoveDirectory(FullPath, recursive);
    }
}
