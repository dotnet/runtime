// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Enumeration;

namespace System.IO
{
    public sealed class DirectoryInfo : FileSystemInfo
    {
        private bool _isNormalized;

        public DirectoryInfo(string path)
        {
            ArgumentNullException.ThrowIfNull(path);
            Init(originalPath: path,
                  fullPath: Path.GetFullPath(path),
                  isNormalized: true);
        }

        internal DirectoryInfo(string originalPath, string? fullPath = null, string? fileName = null, bool isNormalized = false)
        {
            Init(originalPath, fullPath, fileName, isNormalized);
        }

        private void Init(string originalPath, string? fullPath = null, string? fileName = null, bool isNormalized = false)
        {
            OriginalPath = originalPath;

            fullPath ??= originalPath;
            fullPath = isNormalized ? fullPath : Path.GetFullPath(fullPath);

            _name = fileName;

            FullPath = fullPath;

            _isNormalized = isNormalized;
        }

        public override string Name
        {
            get
            {
                string? name = _name;
                if (name is null)
                {
                    ReadOnlySpan<char> fullPath = FullPath.AsSpan();
                    _name = name = (PathInternal.IsRoot(fullPath) ?
                        fullPath :
                        Path.GetFileName(Path.TrimEndingDirectorySeparator(fullPath))).ToString();
                }

                return name;
            }
        }

        public DirectoryInfo? Parent
        {
            get
            {
                // FullPath might end in either "parent\child" or "parent\child\", and in either case we want
                // the parent of child, not the child. Trim off an ending directory separator if there is one,
                // but don't mangle the root.
                string? parentName = Path.GetDirectoryName(PathInternal.IsRoot(FullPath.AsSpan()) ? FullPath : Path.TrimEndingDirectorySeparator(FullPath));
                return parentName != null ?
                    new DirectoryInfo(parentName, isNormalized: true) :
                    null;
            }
        }

        public DirectoryInfo CreateSubdirectory(string path)
        {
            ArgumentNullException.ThrowIfNull(path);

            if (PathInternal.IsEffectivelyEmpty(path.AsSpan()))
                throw new ArgumentException(SR.Argument_PathEmpty, nameof(path));
            if (Path.IsPathRooted(path))
                throw new ArgumentException(SR.Arg_Path2IsRooted, nameof(path));

            string newPath = Path.GetFullPath(Path.Combine(FullPath, path));

            ReadOnlySpan<char> trimmedNewPath = Path.TrimEndingDirectorySeparator(newPath.AsSpan());
            ReadOnlySpan<char> trimmedCurrentPath = Path.TrimEndingDirectorySeparator(FullPath.AsSpan());

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

        public void Create()
        {
            FileSystem.CreateDirectory(FullPath);
            Invalidate();
        }

        // Returns an array of Files in the DirectoryInfo specified by path
        public FileInfo[] GetFiles() => GetFiles("*", enumerationOptions: EnumerationOptions.Compatible);

        // Returns an array of Files in the current DirectoryInfo matching the
        // given search criteria (i.e. "*.txt").
        public FileInfo[] GetFiles(string searchPattern) => GetFiles(searchPattern, enumerationOptions: EnumerationOptions.Compatible);

        public FileInfo[] GetFiles(string searchPattern, SearchOption searchOption)
            => GetFiles(searchPattern, EnumerationOptions.FromSearchOption(searchOption));

        public FileInfo[] GetFiles(string searchPattern, EnumerationOptions enumerationOptions)
            => new List<FileInfo>((IEnumerable<FileInfo>)InternalEnumerateInfos(FullPath, searchPattern, SearchTarget.Files, enumerationOptions)).ToArray();

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
            => new List<FileSystemInfo>(InternalEnumerateInfos(FullPath, searchPattern, SearchTarget.Both, enumerationOptions)).ToArray();

        // Returns an array of Directories in the current directory.
        public DirectoryInfo[] GetDirectories() => GetDirectories("*", enumerationOptions: EnumerationOptions.Compatible);

        // Returns an array of Directories in the current DirectoryInfo matching the
        // given search criteria (i.e. "System*" could match the System & System32 directories).
        public DirectoryInfo[] GetDirectories(string searchPattern) => GetDirectories(searchPattern, enumerationOptions: EnumerationOptions.Compatible);

        public DirectoryInfo[] GetDirectories(string searchPattern, SearchOption searchOption)
            => GetDirectories(searchPattern, EnumerationOptions.FromSearchOption(searchOption));

        public DirectoryInfo[] GetDirectories(string searchPattern, EnumerationOptions enumerationOptions)
            => new List<DirectoryInfo>((IEnumerable<DirectoryInfo>)InternalEnumerateInfos(FullPath, searchPattern, SearchTarget.Directories, enumerationOptions)).ToArray();

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

        private IEnumerable<FileSystemInfo> InternalEnumerateInfos(
            string path,
            string searchPattern,
            SearchTarget searchTarget,
            EnumerationOptions options)
        {
            ArgumentNullException.ThrowIfNull(searchPattern);

            Debug.Assert(path != null);

            _isNormalized &= FileSystemEnumerableFactory.NormalizeInputs(ref path, ref searchPattern, options.MatchType);

            return searchTarget switch
            {
                SearchTarget.Directories => FileSystemEnumerableFactory.DirectoryInfos(path, searchPattern, options, _isNormalized),
                SearchTarget.Files => FileSystemEnumerableFactory.FileInfos(path, searchPattern, options, _isNormalized),
                SearchTarget.Both => FileSystemEnumerableFactory.FileSystemInfos(path, searchPattern, options, _isNormalized),
                _ => throw new ArgumentException(SR.ArgumentOutOfRange_Enum, nameof(searchTarget)),
            };
        }

        public DirectoryInfo Root => new DirectoryInfo(Path.GetPathRoot(FullPath)!);

        public void MoveTo(string destDirName)
        {
            ArgumentException.ThrowIfNullOrEmpty(destDirName);

            string destination = Path.GetFullPath(destDirName);

            FileSystem.MoveDirectory(FullPath, destination);

            Init(originalPath: destDirName,
                 fullPath: PathInternal.EnsureTrailingSeparator(destination),
                 fileName: null,
                 isNormalized: true);

            // Flush any cached information about the directory.
            Invalidate();
        }

        public override void Delete() => Delete(recursive: false);

        public void Delete(bool recursive)
        {
            FileSystem.RemoveDirectory(FullPath, recursive);
            Invalidate();
        }

        public override bool Exists
        {
            get
            {
                try
                {
                    return ExistsCore;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}
