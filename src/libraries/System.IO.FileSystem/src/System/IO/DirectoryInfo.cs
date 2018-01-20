// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.IO
{
    public sealed partial class DirectoryInfo : FileSystemInfo
    {
        private string _name;

        public DirectoryInfo(string path)
        {
            Init(originalPath: PathHelpers.ShouldReviseDirectoryPathToCurrent(path) ? "." : path,
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
            Debug.Assert(!isNormalized || !PathInternal.IsPartiallyQualified(fullPath), "should be fully qualified if normalized");
            fullPath = isNormalized ? fullPath : Path.GetFullPath(fullPath);

            _name = fileName ?? (PathHelpers.IsRoot(fullPath) ?
                    fullPath :
                    Path.GetFileName(PathHelpers.TrimEndingDirectorySeparator(fullPath)));

            FullPath = fullPath;
            DisplayPath = PathHelpers.ShouldReviseDirectoryPathToCurrent(originalPath) ? "." : originalPath;
        }

        public override string Name => _name;

        public DirectoryInfo Parent
        {
            get
            {
                string s = FullPath;

                // FullPath might end in either "parent\child" or "parent\child", and in either case we want 
                // the parent of child, not the child. Trim off an ending directory separator if there is one,
                // but don't mangle the root.
                if (!PathHelpers.IsRoot(s))
                {
                    s = PathHelpers.TrimEndingDirectorySeparator(s);
                }

                string parentName = Path.GetDirectoryName(s);
                return parentName != null ? 
                    new DirectoryInfo(parentName, null) :
                    null;
            }
        }


        public DirectoryInfo CreateSubdirectory(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            return CreateSubdirectoryHelper(path);
        }

        private DirectoryInfo CreateSubdirectoryHelper(string path)
        {
            Debug.Assert(path != null);

            PathHelpers.ThrowIfEmptyOrRootedPath(path);

            string newDirs = Path.Combine(FullPath, path);
            string fullPath = Path.GetFullPath(newDirs);

            if (0 != string.Compare(FullPath, 0, fullPath, 0, FullPath.Length, PathInternal.StringComparison))
            {
                throw new ArgumentException(SR.Format(SR.Argument_InvalidSubPath, path, DisplayPath), nameof(path));
            }

            FileSystem.CreateDirectory(fullPath);

            // Check for read permission to directory we hand back by calling this constructor.
            return new DirectoryInfo(fullPath);
        }

        public void Create()
        {
            FileSystem.CreateDirectory(FullPath);
        }

        // Tests if the given path refers to an existing DirectoryInfo on disk.
        // 
        // Your application must have Read permission to the directory's
        // contents.
        //
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

        // Returns an array of Files in the current DirectoryInfo matching the 
        // given search criteria (i.e. "*.txt").
        public FileInfo[] GetFiles(string searchPattern)
        {
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));

            return InternalGetFiles(searchPattern, SearchOption.TopDirectoryOnly);
        }

        // Returns an array of Files in the current DirectoryInfo matching the 
        // given search criteria (i.e. "*.txt").
        public FileInfo[] GetFiles(string searchPattern, SearchOption searchOption)
        {
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException(nameof(searchOption), SR.ArgumentOutOfRange_Enum);

            return InternalGetFiles(searchPattern, searchOption);
        }

        // Returns an array of Files in the current DirectoryInfo matching the 
        // given search criteria (i.e. "*.txt").
        private FileInfo[] InternalGetFiles(string searchPattern, SearchOption searchOption)
        {
            Debug.Assert(searchPattern != null);
            Debug.Assert(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

            IEnumerable<FileInfo> enumerable = (IEnumerable<FileInfo>)FileSystem.EnumerateFileSystemInfos(FullPath, searchPattern, searchOption, SearchTarget.Files);
            return EnumerableHelpers.ToArray(enumerable);
        }

        // Returns an array of Files in the DirectoryInfo specified by path
        public FileInfo[] GetFiles()
        {
            return InternalGetFiles("*", SearchOption.TopDirectoryOnly);
        }

        // Returns an array of Directories in the current directory.
        public DirectoryInfo[] GetDirectories()
        {
            return InternalGetDirectories("*", SearchOption.TopDirectoryOnly);
        }

        // Returns an array of strongly typed FileSystemInfo entries in the path with the
        // given search criteria (i.e. "*.txt").
        public FileSystemInfo[] GetFileSystemInfos(string searchPattern)
        {
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));

            return InternalGetFileSystemInfos(searchPattern, SearchOption.TopDirectoryOnly);
        }

        // Returns an array of strongly typed FileSystemInfo entries in the path with the
        // given search criteria (i.e. "*.txt").
        public FileSystemInfo[] GetFileSystemInfos(string searchPattern, SearchOption searchOption)
        {
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException(nameof(searchOption), SR.ArgumentOutOfRange_Enum);

            return InternalGetFileSystemInfos(searchPattern, searchOption);
        }

        // Returns an array of strongly typed FileSystemInfo entries in the path with the
        // given search criteria (i.e. "*.txt").
        private FileSystemInfo[] InternalGetFileSystemInfos(string searchPattern, SearchOption searchOption)
        {
            Debug.Assert(searchPattern != null);
            Debug.Assert(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

            IEnumerable<FileSystemInfo> enumerable = FileSystem.EnumerateFileSystemInfos(FullPath, searchPattern, searchOption, SearchTarget.Both);
            return EnumerableHelpers.ToArray(enumerable);
        }

        // Returns an array of strongly typed FileSystemInfo entries which will contain a listing
        // of all the files and directories.
        public FileSystemInfo[] GetFileSystemInfos()
        {
            return InternalGetFileSystemInfos("*", SearchOption.TopDirectoryOnly);
        }

        // Returns an array of Directories in the current DirectoryInfo matching the 
        // given search criteria (i.e. "System*" could match the System & System32
        // directories).
        public DirectoryInfo[] GetDirectories(string searchPattern)
        {
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));

            return InternalGetDirectories(searchPattern, SearchOption.TopDirectoryOnly);
        }

        // Returns an array of Directories in the current DirectoryInfo matching the 
        // given search criteria (i.e. "System*" could match the System & System32
        // directories).
        public DirectoryInfo[] GetDirectories(string searchPattern, SearchOption searchOption)
        {
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException(nameof(searchOption), SR.ArgumentOutOfRange_Enum);

            return InternalGetDirectories(searchPattern, searchOption);
        }

        // Returns an array of Directories in the current DirectoryInfo matching the 
        // given search criteria (i.e. "System*" could match the System & System32
        // directories).
        private DirectoryInfo[] InternalGetDirectories(string searchPattern, SearchOption searchOption)
        {
            Debug.Assert(searchPattern != null);
            Debug.Assert(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

            IEnumerable<DirectoryInfo> enumerable = (IEnumerable<DirectoryInfo>)FileSystem.EnumerateFileSystemInfos(FullPath, searchPattern, searchOption, SearchTarget.Directories);
            return EnumerableHelpers.ToArray(enumerable);
        }

        public IEnumerable<DirectoryInfo> EnumerateDirectories()
        {
            return InternalEnumerateDirectories("*", SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<DirectoryInfo> EnumerateDirectories(string searchPattern)
        {
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));

            return InternalEnumerateDirectories(searchPattern, SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<DirectoryInfo> EnumerateDirectories(string searchPattern, SearchOption searchOption)
        {
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException(nameof(searchOption), SR.ArgumentOutOfRange_Enum);

            return InternalEnumerateDirectories(searchPattern, searchOption);
        }

        private IEnumerable<DirectoryInfo> InternalEnumerateDirectories(string searchPattern, SearchOption searchOption)
        {
            Debug.Assert(searchPattern != null);
            Debug.Assert(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

            return (IEnumerable<DirectoryInfo>)FileSystem.EnumerateFileSystemInfos(FullPath, searchPattern, searchOption, SearchTarget.Directories);
        }

        public IEnumerable<FileInfo> EnumerateFiles()
        {
            return InternalEnumerateFiles("*", SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<FileInfo> EnumerateFiles(string searchPattern)
        {
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));

            return InternalEnumerateFiles(searchPattern, SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<FileInfo> EnumerateFiles(string searchPattern, SearchOption searchOption)
        {
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException(nameof(searchOption), SR.ArgumentOutOfRange_Enum);

            return InternalEnumerateFiles(searchPattern, searchOption);
        }

        private IEnumerable<FileInfo> InternalEnumerateFiles(string searchPattern, SearchOption searchOption)
        {
            Debug.Assert(searchPattern != null);
            Debug.Assert(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

            return (IEnumerable<FileInfo>)FileSystem.EnumerateFileSystemInfos(FullPath, searchPattern, searchOption, SearchTarget.Files);
        }

        public IEnumerable<FileSystemInfo> EnumerateFileSystemInfos()
        {
            return InternalEnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(string searchPattern)
        {
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));

            return InternalEnumerateFileSystemInfos(searchPattern, SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(string searchPattern, SearchOption searchOption)
        {
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException(nameof(searchOption), SR.ArgumentOutOfRange_Enum);

            return InternalEnumerateFileSystemInfos(searchPattern, searchOption);
        }

        private IEnumerable<FileSystemInfo> InternalEnumerateFileSystemInfos(string searchPattern, SearchOption searchOption)
        {
            Debug.Assert(searchPattern != null);
            Debug.Assert(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

            return FileSystem.EnumerateFileSystemInfos(FullPath, searchPattern, searchOption, SearchTarget.Both);
        }

        // Returns the root portion of the given path. The resulting string
        // consists of those rightmost characters of the path that constitute the
        // root of the path. Possible patterns for the resulting string are: An
        // empty string (a relative path on the current drive), "\" (an absolute
        // path on the current drive), "X:" (a relative path on a given drive,
        // where X is the drive letter), "X:\" (an absolute path on a given drive),
        // and "\\server\share" (a UNC path for a given server and share name).
        // The resulting string is null if path is null.
        //

        public DirectoryInfo Root
        {
            get
            {
                string rootPath = Path.GetPathRoot(FullPath);

                return new DirectoryInfo(rootPath);
            }
        }

        public void MoveTo(string destDirName)
        {
            if (destDirName == null)
                throw new ArgumentNullException(nameof(destDirName));
            if (destDirName.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyFileName, nameof(destDirName));

            string destination = Path.GetFullPath(destDirName);
            string destinationWithSeparator = destination;
            if (destinationWithSeparator[destinationWithSeparator.Length - 1] != Path.DirectorySeparatorChar)
                destinationWithSeparator = destinationWithSeparator + PathHelpers.DirectorySeparatorCharAsString;

            string fullSourcePath;
            if (FullPath.Length > 0 && FullPath[FullPath.Length - 1] == Path.DirectorySeparatorChar)
                fullSourcePath = FullPath;
            else
                fullSourcePath = FullPath + PathHelpers.DirectorySeparatorCharAsString;

            StringComparison pathComparison = PathInternal.StringComparison;
            if (string.Equals(fullSourcePath, destinationWithSeparator, pathComparison))
                throw new IOException(SR.IO_SourceDestMustBeDifferent);

            string sourceRoot = Path.GetPathRoot(fullSourcePath);
            string destinationRoot = Path.GetPathRoot(destinationWithSeparator);

            if (!string.Equals(sourceRoot, destinationRoot, pathComparison))
                throw new IOException(SR.IO_SourceDestMustHaveSameRoot);

            // Windows will throw if the source file/directory doesn't exist, we preemptively check
            // to make sure our cross platform behavior matches NetFX behavior.
            if (!Exists && !FileSystem.FileExists(FullPath))
                throw new DirectoryNotFoundException(SR.Format(SR.IO_PathNotFound_Path, FullPath));

            if (FileSystem.DirectoryExists(destinationWithSeparator))
                throw new IOException(SR.Format(SR.IO_AlreadyExists_Name, destinationWithSeparator));

            FileSystem.MoveDirectory(FullPath, destination);

            Init(originalPath: destDirName,
                 fullPath: destinationWithSeparator,
                 fileName: _name,
                 isNormalized: true);

            // Flush any cached information about the directory.
            Invalidate();
        }

        public override void Delete()
        {
            FileSystem.RemoveDirectory(FullPath, false);
        }

        public void Delete(bool recursive)
        {
            FileSystem.RemoveDirectory(FullPath, recursive);
        }

        /// <summary>
        /// Returns the original path. Use FullPath or Name properties for the path / directory name.
        /// </summary>
        public override string ToString()
        {
            return DisplayPath;
        }
    }
}
