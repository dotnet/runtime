// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.IO
{
    /// <summary>Provides an implementation of FileSystem for Unix systems.</summary>
    internal sealed partial class UnixFileSystem : FileSystem
    {
        public override int MaxPath { get { return Interop.Sys.MaxPath; } }

        public override int MaxDirectoryPath { get { return Interop.Sys.MaxPath; } }

        public override FileStreamBase Open(string fullPath, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, FileStream parent)
        {
            return new UnixFileStream(fullPath, mode, access, share, bufferSize, options, parent);
        }

        public override void CopyFile(string sourceFullPath, string destFullPath, bool overwrite)
        {
            // Note: we could consider using sendfile here, but it isn't part of the POSIX spec, and
            // has varying degrees of support on different systems.

            // The destination path may just be a directory into which the file should be copied.
            // If it is, append the filename from the source onto the destination directory
            if (DirectoryExists(destFullPath))
            {
                destFullPath = Path.Combine(destFullPath, Path.GetFileName(sourceFullPath));
            }

            // Copy the contents of the file from the source to the destination, creating the destination in the process
            const int bufferSize = FileStream.DefaultBufferSize;
            const bool useAsync = false;
            using (Stream src = new FileStream(sourceFullPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync))
            using (Stream dst = new FileStream(destFullPath, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, bufferSize, useAsync))
            {
                src.CopyTo(dst);
            }

            // Now copy over relevant read/write/execute permissions from the source to the destination
            Interop.Sys.FileStatus status;
            while (Interop.CheckIo(Interop.Sys.Stat(sourceFullPath, out status), sourceFullPath)) ;
            int newMode = status.Mode & (int)Interop.Sys.Permissions.Mask;
            while (Interop.CheckIo(Interop.Sys.ChMod(destFullPath, newMode), destFullPath)) ;
        }

        public override void MoveFile(string sourceFullPath, string destFullPath)
        {
            // The desired behavior for Move(source, dest) is to not overwrite the destination file
            // if it exists. Since rename(source, dest) will replace the file at 'dest' if it exists,
            // link/unlink are used instead. Note that the Unix FileSystemWatcher will treat a Move 
            // as a Creation and Deletion instead of a Rename and thus differ from Windows.
            while (Interop.Sys.Link(sourceFullPath, destFullPath) < 0)
            {
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                if (errorInfo.Error == Interop.Error.EINTR) // interrupted; try again
                {
                    continue;
                }
                else if (errorInfo.Error == Interop.Error.EXDEV) // rename fails across devices / mount points
                {
                    CopyFile(sourceFullPath, destFullPath, overwrite: false);
                    break;
                }
                else if (errorInfo.Error == Interop.Error.ENOENT && !Directory.Exists(Path.GetDirectoryName(destFullPath))) // The parent directory of destFile can't be found
                {
                    // Windows distinguishes between whether the directory or the file isn't found,
                    // and throws a different exception in these cases.  We attempt to approximate that
                    // here; there is a race condition here, where something could change between
                    // when the error occurs and our checks, but it's the best we can do, and the
                    // worst case in such a race condition (which could occur if the file system is
                    // being manipulated concurrently with these checks) is that we throw a
                    // FileNotFoundException instead of DirectoryNotFoundexception.
                    throw Interop.GetExceptionForIoErrno(errorInfo, destFullPath, isDirectory: true);
                }
                else
                {
                    throw Interop.GetExceptionForIoErrno(errorInfo);
                }
            }
            DeleteFile(sourceFullPath);
        }

        public override void DeleteFile(string fullPath)
        {
            while (Interop.Sys.Unlink(fullPath) < 0)
            {
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                if (errorInfo.Error == Interop.Error.EINTR) // interrupted; try again
                {
                    continue;
                }
                else if (errorInfo.Error == Interop.Error.ENOENT) // already doesn't exist; nop
                {
                    break;
                }
                else
                {
                    if (errorInfo.Error == Interop.Error.EISDIR)
                        errorInfo = Interop.Error.EACCES.Info();
                    throw Interop.GetExceptionForIoErrno(errorInfo, fullPath);
                }
            }
        }

        public override void CreateDirectory(string fullPath)
        {
            // NOTE: This logic is primarily just carried forward from Win32FileSystem.CreateDirectory.

            int length = fullPath.Length;

            // We need to trim the trailing slash or the code will try to create 2 directories of the same name.
            if (length >= 2 && PathHelpers.EndsInDirectorySeparator(fullPath))
            {
                length--;
            }

            // For paths that are only // or /// 
            if (length == 2 && PathInternal.IsDirectorySeparator(fullPath[1]))
            {
                throw new IOException(SR.Format(SR.IO_CannotCreateDirectory, fullPath));
            }

            // We can save a bunch of work if the directory we want to create already exists.
            if (DirectoryExists(fullPath))
            {
                return;
            }

            // Attempt to figure out which directories don't exist, and only create the ones we need.
            bool somepathexists = false;
            Stack<string> stackDir = new Stack<string>();
            int lengthRoot = PathInternal.GetRootLength(fullPath);
            if (length > lengthRoot)
            {
                int i = length - 1;
                while (i >= lengthRoot && !somepathexists)
                {
                    string dir = fullPath.Substring(0, i + 1);
                    if (!DirectoryExists(dir)) // Create only the ones missing
                    {
                        stackDir.Push(dir);
                    }
                    else
                    {
                        somepathexists = true;
                    }

                    while (i > lengthRoot && !PathInternal.IsDirectorySeparator(fullPath[i]))
                    {
                        i--;
                    }
                    i--;
                }
            }

            int count = stackDir.Count;
            if (count == 0 && !somepathexists)
            {
                string root = Directory.InternalGetDirectoryRoot(fullPath);
                if (!DirectoryExists(root))
                {
                    throw Interop.GetExceptionForIoErrno(Interop.Error.ENOENT.Info(), fullPath, isDirectory: true);
                }
                return;
            }

            // Create all the directories
            int result = 0;
            Interop.ErrorInfo firstError = default(Interop.ErrorInfo);
            string errorString = fullPath;
            while (stackDir.Count > 0)
            {
                string name = stackDir.Pop();
                if (name.Length >= MaxDirectoryPath)
                {
                    throw new PathTooLongException(SR.IO_PathTooLong);
                }

                Interop.ErrorInfo errorInfo = default(Interop.ErrorInfo);
                while ((result = Interop.Sys.MkDir(name, (int)Interop.Sys.Permissions.S_IRWXU)) < 0 && (errorInfo = Interop.Sys.GetLastErrorInfo()).Error == Interop.Error.EINTR) ;
                if (result < 0 && firstError.Error == 0)
                {
                    // While we tried to avoid creating directories that don't
                    // exist above, there are a few cases that can fail, e.g.
                    // a race condition where another process or thread creates
                    // the directory first, or there's a file at the location.
                    if (errorInfo.Error != Interop.Error.EEXIST)
                    {
                        firstError = errorInfo;
                    }
                    else if (FileExists(name) || (!DirectoryExists(name, out errorInfo) && errorInfo.Error == Interop.Error.EACCES))
                    {
                        // If there's a file in this directory's place, or if we have ERROR_ACCESS_DENIED when checking if the directory already exists throw.
                        firstError = errorInfo;
                        errorString = name;
                    }
                }
            }

            // Only throw an exception if creating the exact directory we wanted failed to work correctly.
            if (result < 0 && firstError.Error != 0)
            {
                throw Interop.GetExceptionForIoErrno(firstError, errorString, isDirectory: true);
            }
        }

        public override void MoveDirectory(string sourceFullPath, string destFullPath)
        {
            while (Interop.Sys.Rename(sourceFullPath, destFullPath) < 0)
            {
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                switch (errorInfo.Error)
                {
                    case Interop.Error.EINTR: // interrupted; try again
                        continue;
                    case Interop.Error.EACCES: // match Win32 exception
                        throw new IOException(SR.Format(SR.UnauthorizedAccess_IODenied_Path, sourceFullPath), errorInfo.RawErrno);
                    default:
                        throw Interop.GetExceptionForIoErrno(errorInfo, sourceFullPath, isDirectory: true);
                }
            }
        }

        public override void RemoveDirectory(string fullPath, bool recursive)
        {
            if (!DirectoryExists(fullPath))
            {
                throw Interop.GetExceptionForIoErrno(Interop.Error.ENOENT.Info(), fullPath, isDirectory: true);
            }
            RemoveDirectoryInternal(fullPath, recursive, throwOnTopLevelDirectoryNotFound: true);
        }

        private void RemoveDirectoryInternal(string fullPath, bool recursive, bool throwOnTopLevelDirectoryNotFound)
        {
            Exception firstException = null;

            if (recursive)
            {
                try
                {
                    foreach (string item in EnumeratePaths(fullPath, "*", SearchOption.TopDirectoryOnly, SearchTarget.Both))
                    {
                        if (!ShouldIgnoreDirectory(Path.GetFileName(item)))
                        {
                            try
                            {
                                if (DirectoryExists(item))
                                {
                                    RemoveDirectoryInternal(item, recursive, throwOnTopLevelDirectoryNotFound: false);
                                }
                                else
                                {
                                    DeleteFile(item);
                                }
                            }
                            catch (Exception exc)
                            {
                                if (firstException != null)
                                {
                                    firstException = exc;
                                }
                            }
                        }
                    }
                }
                catch (Exception exc)
                {
                    if (firstException != null)
                    {
                        firstException = exc;
                    }
                }

                if (firstException != null)
                {
                    throw firstException;
                }
            }

            while (Interop.Sys.RmDir(fullPath) < 0)
            {
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                switch (errorInfo.Error)
                {
                    case Interop.Error.EINTR: // interrupted; try again
                        continue;
                    case Interop.Error.EACCES:
                    case Interop.Error.EPERM:
                    case Interop.Error.EROFS:
                    case Interop.Error.EISDIR:
                        throw new IOException(SR.Format(SR.UnauthorizedAccess_IODenied_Path, fullPath)); // match Win32 exception
                    case Interop.Error.ENOENT:
                        if (!throwOnTopLevelDirectoryNotFound)
                        {
                            return;
                        }
                        goto default;
                    default:
                        throw Interop.GetExceptionForIoErrno(errorInfo, fullPath, isDirectory: true);
                }
            }
        }

        public override bool DirectoryExists(string fullPath)
        {
            Interop.ErrorInfo ignored;
            return DirectoryExists(fullPath, out ignored);
        }

        private static bool DirectoryExists(string fullPath, out Interop.ErrorInfo errorInfo)
        {
            return FileExists(fullPath, Interop.Sys.FileTypes.S_IFDIR, out errorInfo);
        }

        public override bool FileExists(string fullPath)
        {
            Interop.ErrorInfo ignored;
            return FileExists(fullPath, Interop.Sys.FileTypes.S_IFREG, out ignored);
        }

        private static bool FileExists(string fullPath, int fileType, out Interop.ErrorInfo errorInfo)
        {
            Interop.Sys.FileStatus fileinfo;
            while (true)
            {
                errorInfo = default(Interop.ErrorInfo);
                int result = Interop.Sys.Stat(fullPath, out fileinfo);
                if (result < 0)
                {
                    errorInfo = Interop.Sys.GetLastErrorInfo();
                    if (errorInfo.Error == Interop.Error.EINTR)
                    {
                        continue;
                    }
                    return false;
                }
                return (fileinfo.Mode & Interop.Sys.FileTypes.S_IFMT) == fileType;
            }
        }

        public override IEnumerable<string> EnumeratePaths(string path, string searchPattern, SearchOption searchOption, SearchTarget searchTarget)
        {
            return new FileSystemEnumerable<string>(path, searchPattern, searchOption, searchTarget, (p, _) => p);
        }

        public override IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(string fullPath, string searchPattern, SearchOption searchOption, SearchTarget searchTarget)
        {
            switch (searchTarget)
            {
                case SearchTarget.Files:
                    return new FileSystemEnumerable<FileInfo>(fullPath, searchPattern, searchOption, searchTarget, (path, isDir) =>
                        new FileInfo(path, null));
                case SearchTarget.Directories:
                    return new FileSystemEnumerable<DirectoryInfo>(fullPath, searchPattern, searchOption, searchTarget, (path, isDir) =>
                        new DirectoryInfo(path, null));
                default:
                    return new FileSystemEnumerable<FileSystemInfo>(fullPath, searchPattern, searchOption, searchTarget, (path, isDir) => isDir ?
                        (FileSystemInfo)new DirectoryInfo(path, null) :
                        (FileSystemInfo)new FileInfo(path, null));
            }
        }

        private sealed class FileSystemEnumerable<T> : IEnumerable<T>
        {
            private readonly PathPair _initialDirectory;
            private readonly string _searchPattern;
            private readonly SearchOption _searchOption;
            private readonly bool _includeFiles;
            private readonly bool _includeDirectories;
            private readonly Func<string, bool, T> _translateResult;
            private IEnumerator<T> _firstEnumerator;

            internal FileSystemEnumerable(
                string userPath, string searchPattern,
                SearchOption searchOption, SearchTarget searchTarget,
                Func<string, bool, T> translateResult)
            {
                // Basic validation of the input path
                if (userPath == null)
                {
                    throw new ArgumentNullException("path");
                }
                if (string.IsNullOrWhiteSpace(userPath))
                {
                    throw new ArgumentException(SR.Argument_EmptyPath, "path");
                }

                // Validate and normalize the search pattern.  If after doing so it's empty,
                // matching Win32 behavior we can skip all additional validation and effectively
                // return an empty enumerable.
                searchPattern = NormalizeSearchPattern(searchPattern);
                if (searchPattern.Length > 0)
                {
                    PathHelpers.CheckSearchPattern(searchPattern);
                    PathHelpers.ThrowIfEmptyOrRootedPath(searchPattern);

                    // If the search pattern contains any paths, make sure we factor those into 
                    // the user path, and then trim them off.
                    int lastSlash = searchPattern.LastIndexOf(Path.DirectorySeparatorChar);
                    if (lastSlash >= 0)
                    {
                        if (lastSlash >= 1)
                        {
                            userPath = Path.Combine(userPath, searchPattern.Substring(0, lastSlash));
                        }
                        searchPattern = searchPattern.Substring(lastSlash + 1);
                    }
                    string fullPath = Path.GetFullPath(userPath);

                    // Store everything for the enumerator
                    _initialDirectory = new PathPair(userPath, fullPath);
                    _searchPattern = searchPattern;
                    _searchOption = searchOption;
                    _includeFiles = (searchTarget & SearchTarget.Files) != 0;
                    _includeDirectories = (searchTarget & SearchTarget.Directories) != 0;
                    _translateResult = translateResult;
                }

                // Open the first enumerator so that any errors are propagated synchronously.
                _firstEnumerator = Enumerate();
            }

            public IEnumerator<T> GetEnumerator()
            {
                return Interlocked.Exchange(ref _firstEnumerator, null) ?? Enumerate();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private IEnumerator<T> Enumerate()
            {
                return Enumerate(
                    _initialDirectory.FullPath != null ? 
                        OpenDirectory(_initialDirectory.FullPath) : 
                        null);
            }

            private IEnumerator<T> Enumerate(Microsoft.Win32.SafeHandles.SafeDirectoryHandle dirHandle)
            {
                if (dirHandle == null)
                {
                    // Empty search
                    yield break;
                }

                Debug.Assert(!dirHandle.IsInvalid);
                Debug.Assert(!dirHandle.IsClosed);

                // Maintain a stack of the directories to explore, in the case of SearchOption.AllDirectories
                // Lazily-initialized only if we find subdirectories that will be explored.
                Stack<PathPair> toExplore = null;
                PathPair dirPath = _initialDirectory;
                while (dirHandle != null)
                {
                    try
                    {
                        // Read each entry from the enumerator
                        Interop.Sys.DirectoryEntry dirent;
                        while (Interop.Sys.ReadDir(dirHandle, out dirent) == 0)
                        {
                            // Get from the dir entry whether the entry is a file or directory.
                            // We classify everything as a file unless we know it to be a directory.
                            bool isDir;
                            if (dirent.InodeType == Interop.Sys.NodeType.DT_DIR)
                            {
                                // We know it's a directory.
                                isDir = true;
                            }
                            else if (dirent.InodeType == Interop.Sys.NodeType.DT_LNK || dirent.InodeType == Interop.Sys.NodeType.DT_UNKNOWN)
                            {
                                // It's a symlink or unknown: stat to it to see if we can resolve it to a directory.
                                // If we can't (e.g.symlink to a file, broken symlink, etc.), we'll just treat it as a file.
                                Interop.ErrorInfo errnoIgnored;
                                isDir = DirectoryExists(Path.Combine(dirPath.FullPath, dirent.InodeName), out errnoIgnored);
                            }
                            else
                            {
                                // Otherwise, treat it as a file.  This includes regular files, FIFOs, etc.
                                isDir = false;
                            }

                            // Yield the result if the user has asked for it.  In the case of directories,
                            // always explore it by pushing it onto the stack, regardless of whether
                            // we're returning directories.
                            if (isDir)
                            {
                                if (!ShouldIgnoreDirectory(dirent.InodeName))
                                {
                                    string userPath = null;
                                    if (_searchOption == SearchOption.AllDirectories)
                                    {
                                        if (toExplore == null)
                                        {
                                            toExplore = new Stack<PathPair>();
                                        }
                                        userPath = Path.Combine(dirPath.UserPath, dirent.InodeName);
                                        toExplore.Push(new PathPair(userPath, Path.Combine(dirPath.FullPath, dirent.InodeName)));
                                    }
                                    if (_includeDirectories &&
                                        Interop.Sys.FnMatch(_searchPattern, dirent.InodeName, Interop.Sys.FnMatchFlags.FNM_NONE) == 0)
                                    {
                                        yield return _translateResult(userPath ?? Path.Combine(dirPath.UserPath, dirent.InodeName), /*isDirectory*/true);
                                    }
                                }
                            }
                            else if (_includeFiles &&
                                     Interop.Sys.FnMatch(_searchPattern, dirent.InodeName, Interop.Sys.FnMatchFlags.FNM_NONE) == 0)
                            {
                                yield return _translateResult(Path.Combine(dirPath.UserPath, dirent.InodeName), /*isDirectory*/false);
                            }
                        }
                    }
                    finally
                    {
                        // Close the directory enumerator
                        dirHandle.Dispose();
                        dirHandle = null;
                    }

                    if (toExplore != null && toExplore.Count > 0)
                    {
                        // Open the next directory.
                        dirPath = toExplore.Pop();
                        dirHandle = OpenDirectory(dirPath.FullPath);
                    }
                }
            }

            private static string NormalizeSearchPattern(string searchPattern)
            {
                if (searchPattern == "." || searchPattern == "*.*")
                {
                    searchPattern = "*";
                }
                else if (PathHelpers.EndsInDirectorySeparator(searchPattern))
                {
                    searchPattern += "*";
                }
                return searchPattern;
            }

            private static Microsoft.Win32.SafeHandles.SafeDirectoryHandle OpenDirectory(string fullPath)
            {
                Microsoft.Win32.SafeHandles.SafeDirectoryHandle handle = Interop.Sys.OpenDir(fullPath);
                if (handle.IsInvalid)
                {
                    throw Interop.GetExceptionForIoErrno(Interop.Sys.GetLastErrorInfo(), fullPath, isDirectory: true);
                }
                return handle;
            }
        }

        /// <summary>Determines whether the specified directory name should be ignored.</summary>
        /// <param name="name">The name to evaluate.</param>
        /// <returns>true if the name is "." or ".."; otherwise, false.</returns>
        private static bool ShouldIgnoreDirectory(string name)
        {
            return name == "." || name == "..";
        }

        public override string GetCurrentDirectory()
        {
            return Interop.Sys.GetCwd();
        }

        public override void SetCurrentDirectory(string fullPath)
        {
            while (Interop.CheckIo(Interop.Sys.ChDir(fullPath), fullPath)) ;
        }

        public override FileAttributes GetAttributes(string fullPath)
        {
            return new FileInfo(fullPath, null).Attributes;
        }

        public override void SetAttributes(string fullPath, FileAttributes attributes)
        {
            new FileInfo(fullPath, null).Attributes = attributes;
        }

        public override DateTimeOffset GetCreationTime(string fullPath)
        {
            return new FileInfo(fullPath, null).CreationTime;
        }

        public override void SetCreationTime(string fullPath, DateTimeOffset time, bool asDirectory)
        {
            IFileSystemObject info = asDirectory ?
                (IFileSystemObject)new DirectoryInfo(fullPath, null) :
                (IFileSystemObject)new FileInfo(fullPath, null);

            info.CreationTime = time;
        }

        public override DateTimeOffset GetLastAccessTime(string fullPath)
        {
            return new FileInfo(fullPath, null).LastAccessTime;
        }

        public override void SetLastAccessTime(string fullPath, DateTimeOffset time, bool asDirectory)
        {
            IFileSystemObject info = asDirectory ?
                (IFileSystemObject)new DirectoryInfo(fullPath, null) :
                (IFileSystemObject)new FileInfo(fullPath, null);

            info.LastAccessTime = time;
        }

        public override DateTimeOffset GetLastWriteTime(string fullPath)
        {
            return new FileInfo(fullPath, null).LastWriteTime;
        }

        public override void SetLastWriteTime(string fullPath, DateTimeOffset time, bool asDirectory)
        {
            IFileSystemObject info = asDirectory ?
                (IFileSystemObject)new DirectoryInfo(fullPath, null) :
                (IFileSystemObject)new FileInfo(fullPath, null);

            info.LastWriteTime = time;
        }

        public override IFileSystemObject GetFileSystemInfo(string fullPath, bool asDirectory)
        {
            return asDirectory ?
                (IFileSystemObject)new DirectoryInfo(fullPath, null) :
                (IFileSystemObject)new FileInfo(fullPath, null);
        }
    }
}
