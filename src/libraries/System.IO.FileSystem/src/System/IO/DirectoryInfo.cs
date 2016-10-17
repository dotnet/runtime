// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Security;
using System.Runtime.Serialization;

namespace System.IO
{
    [Serializable]
    public sealed partial class DirectoryInfo : FileSystemInfo
    {
        [System.Security.SecuritySafeCritical]
        public DirectoryInfo(String path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            Contract.EndContractBlock();

            OriginalPath = PathHelpers.ShouldReviseDirectoryPathToCurrent(path) ? "." : path;
            FullPath = Path.GetFullPath(path);
            DisplayPath = GetDisplayName(OriginalPath);
        }

        [System.Security.SecuritySafeCritical]
        internal DirectoryInfo(String fullPath, String originalPath)
        {
            Debug.Assert(Path.IsPathRooted(fullPath), "fullPath must be fully qualified!");

            // Fast path when we know a DirectoryInfo exists.
            OriginalPath = originalPath ?? Path.GetFileName(fullPath);
            FullPath = fullPath;
            DisplayPath = GetDisplayName(OriginalPath);
        }

        private DirectoryInfo(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            DisplayPath = GetDisplayName(OriginalPath);
        }

        public override String Name
        {
            get
            {
                return GetDirName(FullPath);
            }
        }

        public DirectoryInfo Parent
        {
            [System.Security.SecuritySafeCritical]
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


        [System.Security.SecuritySafeCritical]
        public DirectoryInfo CreateSubdirectory(String path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            Contract.EndContractBlock();

            return CreateSubdirectoryHelper(path);
        }

        [System.Security.SecurityCritical]  // auto-generated
        private DirectoryInfo CreateSubdirectoryHelper(String path)
        {
            Debug.Assert(path != null);

            PathHelpers.ThrowIfEmptyOrRootedPath(path);

            String newDirs = Path.Combine(FullPath, path);
            String fullPath = Path.GetFullPath(newDirs);

            if (0 != String.Compare(FullPath, 0, fullPath, 0, FullPath.Length, PathInternal.StringComparison))
            {
                throw new ArgumentException(SR.Format(SR.Argument_InvalidSubPath, path, DisplayPath), nameof(path));
            }

            FileSystem.Current.CreateDirectory(fullPath);

            // Check for read permission to directory we hand back by calling this constructor.
            return new DirectoryInfo(fullPath);
        }

        [System.Security.SecurityCritical]
        public void Create()
        {
            FileSystem.Current.CreateDirectory(FullPath);
        }

        // Tests if the given path refers to an existing DirectoryInfo on disk.
        // 
        // Your application must have Read permission to the directory's
        // contents.
        //
        public override bool Exists
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                try
                {
                    return FileSystemObject.Exists;
                }
                catch
                {
                    return false;
                }
            }
        }

        // Returns an array of Files in the current DirectoryInfo matching the 
        // given search criteria (i.e. "*.txt").
        [SecurityCritical]
        public FileInfo[] GetFiles(String searchPattern)
        {
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            Contract.EndContractBlock();

            return InternalGetFiles(searchPattern, SearchOption.TopDirectoryOnly);
        }

        // Returns an array of Files in the current DirectoryInfo matching the 
        // given search criteria (i.e. "*.txt").
        public FileInfo[] GetFiles(String searchPattern, SearchOption searchOption)
        {
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException(nameof(searchOption), SR.ArgumentOutOfRange_Enum);
            Contract.EndContractBlock();

            return InternalGetFiles(searchPattern, searchOption);
        }

        // Returns an array of Files in the current DirectoryInfo matching the 
        // given search criteria (i.e. "*.txt").
        private FileInfo[] InternalGetFiles(String searchPattern, SearchOption searchOption)
        {
            Debug.Assert(searchPattern != null);
            Debug.Assert(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

            IEnumerable<FileInfo> enumerable = (IEnumerable<FileInfo>)FileSystem.Current.EnumerateFileSystemInfos(FullPath, searchPattern, searchOption, SearchTarget.Files);
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
        public FileSystemInfo[] GetFileSystemInfos(String searchPattern)
        {
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            Contract.EndContractBlock();

            return InternalGetFileSystemInfos(searchPattern, SearchOption.TopDirectoryOnly);
        }

        // Returns an array of strongly typed FileSystemInfo entries in the path with the
        // given search criteria (i.e. "*.txt").
        public FileSystemInfo[] GetFileSystemInfos(String searchPattern, SearchOption searchOption)
        {
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException(nameof(searchOption), SR.ArgumentOutOfRange_Enum);
            Contract.EndContractBlock();

            return InternalGetFileSystemInfos(searchPattern, searchOption);
        }

        // Returns an array of strongly typed FileSystemInfo entries in the path with the
        // given search criteria (i.e. "*.txt").
        private FileSystemInfo[] InternalGetFileSystemInfos(String searchPattern, SearchOption searchOption)
        {
            Debug.Assert(searchPattern != null);
            Debug.Assert(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

            IEnumerable<FileSystemInfo> enumerable = FileSystem.Current.EnumerateFileSystemInfos(FullPath, searchPattern, searchOption, SearchTarget.Both);
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
        public DirectoryInfo[] GetDirectories(String searchPattern)
        {
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            Contract.EndContractBlock();

            return InternalGetDirectories(searchPattern, SearchOption.TopDirectoryOnly);
        }

        // Returns an array of Directories in the current DirectoryInfo matching the 
        // given search criteria (i.e. "System*" could match the System & System32
        // directories).
        public DirectoryInfo[] GetDirectories(String searchPattern, SearchOption searchOption)
        {
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException(nameof(searchOption), SR.ArgumentOutOfRange_Enum);
            Contract.EndContractBlock();

            return InternalGetDirectories(searchPattern, searchOption);
        }

        // Returns an array of Directories in the current DirectoryInfo matching the 
        // given search criteria (i.e. "System*" could match the System & System32
        // directories).
        private DirectoryInfo[] InternalGetDirectories(String searchPattern, SearchOption searchOption)
        {
            Debug.Assert(searchPattern != null);
            Debug.Assert(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

            IEnumerable<DirectoryInfo> enumerable = (IEnumerable<DirectoryInfo>)FileSystem.Current.EnumerateFileSystemInfos(FullPath, searchPattern, searchOption, SearchTarget.Directories);
            return EnumerableHelpers.ToArray(enumerable);
        }

        public IEnumerable<DirectoryInfo> EnumerateDirectories()
        {
            return InternalEnumerateDirectories("*", SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<DirectoryInfo> EnumerateDirectories(String searchPattern)
        {
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            Contract.EndContractBlock();

            return InternalEnumerateDirectories(searchPattern, SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<DirectoryInfo> EnumerateDirectories(String searchPattern, SearchOption searchOption)
        {
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException(nameof(searchOption), SR.ArgumentOutOfRange_Enum);
            Contract.EndContractBlock();

            return InternalEnumerateDirectories(searchPattern, searchOption);
        }

        private IEnumerable<DirectoryInfo> InternalEnumerateDirectories(String searchPattern, SearchOption searchOption)
        {
            Debug.Assert(searchPattern != null);
            Debug.Assert(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

            return (IEnumerable<DirectoryInfo>)FileSystem.Current.EnumerateFileSystemInfos(FullPath, searchPattern, searchOption, SearchTarget.Directories);
        }

        public IEnumerable<FileInfo> EnumerateFiles()
        {
            return InternalEnumerateFiles("*", SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<FileInfo> EnumerateFiles(String searchPattern)
        {
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            Contract.EndContractBlock();

            return InternalEnumerateFiles(searchPattern, SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<FileInfo> EnumerateFiles(String searchPattern, SearchOption searchOption)
        {
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException(nameof(searchOption), SR.ArgumentOutOfRange_Enum);
            Contract.EndContractBlock();

            return InternalEnumerateFiles(searchPattern, searchOption);
        }

        private IEnumerable<FileInfo> InternalEnumerateFiles(String searchPattern, SearchOption searchOption)
        {
            Debug.Assert(searchPattern != null);
            Debug.Assert(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

            return (IEnumerable<FileInfo>)FileSystem.Current.EnumerateFileSystemInfos(FullPath, searchPattern, searchOption, SearchTarget.Files);
        }

        public IEnumerable<FileSystemInfo> EnumerateFileSystemInfos()
        {
            return InternalEnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(String searchPattern)
        {
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            Contract.EndContractBlock();

            return InternalEnumerateFileSystemInfos(searchPattern, SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(String searchPattern, SearchOption searchOption)
        {
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException(nameof(searchOption), SR.ArgumentOutOfRange_Enum);
            Contract.EndContractBlock();

            return InternalEnumerateFileSystemInfos(searchPattern, searchOption);
        }

        private IEnumerable<FileSystemInfo> InternalEnumerateFileSystemInfos(String searchPattern, SearchOption searchOption)
        {
            Debug.Assert(searchPattern != null);
            Debug.Assert(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

            return FileSystem.Current.EnumerateFileSystemInfos(FullPath, searchPattern, searchOption, SearchTarget.Both);
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
            [System.Security.SecuritySafeCritical]
            get
            {
                String rootPath = Path.GetPathRoot(FullPath);

                return new DirectoryInfo(rootPath);
            }
        }

        [System.Security.SecuritySafeCritical]
        public void MoveTo(String destDirName)
        {
            if (destDirName == null)
                throw new ArgumentNullException(nameof(destDirName));
            if (destDirName.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyFileName, nameof(destDirName));
            Contract.EndContractBlock();

            String fullDestDirName = Path.GetFullPath(destDirName);
            if (fullDestDirName[fullDestDirName.Length - 1] != Path.DirectorySeparatorChar)
                fullDestDirName = fullDestDirName + PathHelpers.DirectorySeparatorCharAsString;

            String fullSourcePath;
            if (FullPath.Length > 0 && FullPath[FullPath.Length - 1] == Path.DirectorySeparatorChar)
                fullSourcePath = FullPath;
            else
                fullSourcePath = FullPath + PathHelpers.DirectorySeparatorCharAsString;

            if (PathInternal.IsDirectoryTooLong(fullSourcePath))
                throw new PathTooLongException(SR.IO_PathTooLong);

            if (PathInternal.IsDirectoryTooLong(fullDestDirName))
                throw new PathTooLongException(SR.IO_PathTooLong);

            StringComparison pathComparison = PathInternal.StringComparison;
            if (String.Equals(fullSourcePath, fullDestDirName, pathComparison))
                throw new IOException(SR.IO_SourceDestMustBeDifferent);

            String sourceRoot = Path.GetPathRoot(fullSourcePath);
            String destinationRoot = Path.GetPathRoot(fullDestDirName);

            if (!String.Equals(sourceRoot, destinationRoot, pathComparison))
                throw new IOException(SR.IO_SourceDestMustHaveSameRoot);

            FileSystem.Current.MoveDirectory(FullPath, fullDestDirName);

            FullPath = fullDestDirName;
            OriginalPath = destDirName;
            DisplayPath = GetDisplayName(OriginalPath);

            // Flush any cached information about the directory.
            Invalidate();
        }

        [System.Security.SecuritySafeCritical]
        public override void Delete()
        {
            FileSystem.Current.RemoveDirectory(FullPath, false);
        }

        [System.Security.SecuritySafeCritical]
        public void Delete(bool recursive)
        {
            FileSystem.Current.RemoveDirectory(FullPath, recursive);
        }

        /// <summary>
        /// Returns the original path. Use FullPath or Name properties for the path / directory name.
        /// </summary>
        public override String ToString()
        {
            return DisplayPath;
        }

        private static String GetDisplayName(String originalPath)
        {
            Debug.Assert(originalPath != null);

            // Desktop documents that the path returned by ToString() should be the original path.
            // For SL/Phone we only gave the directory name regardless of what was passed in.
            return PathHelpers.ShouldReviseDirectoryPathToCurrent(originalPath) ?
                "." :
                originalPath;
        }

        private static String GetDirName(String fullPath)
        {
            Debug.Assert(fullPath != null);

            return PathHelpers.IsRoot(fullPath) ?
                fullPath :
                Path.GetFileName(PathHelpers.TrimEndingDirectorySeparator(fullPath));
        }
    }
}
