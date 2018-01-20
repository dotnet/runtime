// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.IO
{
    /// <summary>Provides an implementation of FileSystem for Unix systems.</summary>
    internal static partial class FileSystem
    {
        internal const int DefaultBufferSize = 4096;

        public static void CopyFile(string sourceFullPath, string destFullPath, bool overwrite)
        {
            // The destination path may just be a directory into which the file should be copied.
            // If it is, append the filename from the source onto the destination directory
            if (DirectoryExists(destFullPath))
            {
                destFullPath = Path.Combine(destFullPath, Path.GetFileName(sourceFullPath));
            }

            // Copy the contents of the file from the source to the destination, creating the destination in the process
            using (var src = new FileStream(sourceFullPath, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, FileOptions.None))
            using (var dst = new FileStream(destFullPath, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, DefaultBufferSize, FileOptions.None))
            {
                Interop.CheckIo(Interop.Sys.CopyFile(src.SafeFileHandle, dst.SafeFileHandle));
            }
        }

        public static void ReplaceFile(string sourceFullPath, string destFullPath, string destBackupFullPath, bool ignoreMetadataErrors)
        {
            if (destBackupFullPath != null)
            {
                // We're backing up the destination file to the backup file, so we need to first delete the backup
                // file, if it exists.  If deletion fails for a reason other than the file not existing, fail.
                if (Interop.Sys.Unlink(destBackupFullPath) != 0)
                {
                    Interop.ErrorInfo errno = Interop.Sys.GetLastErrorInfo();
                    if (errno.Error != Interop.Error.ENOENT)
                    {
                        throw Interop.GetExceptionForIoErrno(errno, destBackupFullPath);
                    }
                }

                // Now that the backup is gone, link the backup to point to the same file as destination.
                // This way, we don't lose any data in the destination file, no copy is necessary, etc.
                Interop.CheckIo(Interop.Sys.Link(destFullPath, destBackupFullPath), destFullPath);
            }
            else
            {
                // There is no backup file.  Just make sure the destination file exists, throwing if it doesn't.
                Interop.Sys.FileStatus ignored;
                if (Interop.Sys.Stat(destFullPath, out ignored) != 0)
                {
                    Interop.ErrorInfo errno = Interop.Sys.GetLastErrorInfo();
                    if (errno.Error == Interop.Error.ENOENT)
                    {
                        throw Interop.GetExceptionForIoErrno(errno, destBackupFullPath);
                    }
                }
            }

            // Finally, rename the source to the destination, overwriting the destination.
            Interop.CheckIo(Interop.Sys.Rename(sourceFullPath, destFullPath));
        }

        public static void MoveFile(string sourceFullPath, string destFullPath)
        {
            // The desired behavior for Move(source, dest) is to not overwrite the destination file
            // if it exists. Since rename(source, dest) will replace the file at 'dest' if it exists,
            // link/unlink are used instead. However, if the source path and the dest path refer to
            // the same file, then do a rename rather than a link and an unlink.  This is important
            // for case-insensitive file systems (e.g. renaming a file in a way that just changes casing),
            // so that we support changing the casing in the naming of the file. If this fails in any
            // way (e.g. source file doesn't exist, dest file doesn't exist, rename fails, etc.), we
            // just fall back to trying the link/unlink approach and generating any exceptional messages
            // from there as necessary.
            Interop.Sys.FileStatus sourceStat, destStat;
            if (Interop.Sys.LStat(sourceFullPath, out sourceStat) == 0 && // source file exists
                Interop.Sys.LStat(destFullPath, out destStat) == 0 && // dest file exists
                sourceStat.Dev == destStat.Dev && // source and dest are on the same device
                sourceStat.Ino == destStat.Ino && // and source and dest are the same file on that device
                Interop.Sys.Rename(sourceFullPath, destFullPath) == 0) // try the rename
            {
                // Renamed successfully.
                return;
            }

            if (Interop.Sys.Link(sourceFullPath, destFullPath) < 0)
            {
                // If link fails, we can fall back to doing a full copy, but we'll only do so for
                // cases where we expect link could fail but such a copy could succeed.  We don't
                // want to do so for all errors, because the copy could incur a lot of cost
                // even if we know it'll eventually fail, e.g. EROFS means that the source file
                // system is read-only and couldn't support the link being added, but if it's
                // read-only, then the move should fail any way due to an inability to delete
                // the source file.
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                if (errorInfo.Error == Interop.Error.EXDEV ||      // rename fails across devices / mount points
                    errorInfo.Error == Interop.Error.EPERM ||      // permissions might not allow creating hard links even if a copy would work
                    errorInfo.Error == Interop.Error.EOPNOTSUPP || // links aren't supported by the source file system
                    errorInfo.Error == Interop.Error.EMLINK)       // too many hard links to the source file
                {
                    CopyFile(sourceFullPath, destFullPath, overwrite: false);
                }
                else
                {
                    // The operation failed.  Within reason, try to determine which path caused the problem 
                    // so we can throw a detailed exception.
                    string path = null;
                    bool isDirectory = false;
                    if (errorInfo.Error == Interop.Error.ENOENT)
                    {
                        if (!Directory.Exists(Path.GetDirectoryName(destFullPath)))
                        {
                            // The parent directory of destFile can't be found.
                            // Windows distinguishes between whether the directory or the file isn't found,
                            // and throws a different exception in these cases.  We attempt to approximate that
                            // here; there is a race condition here, where something could change between
                            // when the error occurs and our checks, but it's the best we can do, and the
                            // worst case in such a race condition (which could occur if the file system is
                            // being manipulated concurrently with these checks) is that we throw a
                            // FileNotFoundException instead of DirectoryNotFoundexception.
                            path = destFullPath;
                            isDirectory = true;
                        }
                        else
                        {
                            path = sourceFullPath;
                        }
                    }
                    else if (errorInfo.Error == Interop.Error.EEXIST)
                    {
                        path = destFullPath;
                    }

                    throw Interop.GetExceptionForIoErrno(errorInfo, path, isDirectory);
                }
            }
            DeleteFile(sourceFullPath);
        }

        public static void DeleteFile(string fullPath)
        {
            if (Interop.Sys.Unlink(fullPath) < 0)
            {
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                switch (errorInfo.Error)
                {
                    case Interop.Error.ENOENT:
                        // ENOENT means it already doesn't exist; nop
                        return;
                    case Interop.Error.EROFS:
                        // EROFS means the file system is read-only
                        // Need to manually check file existence
                        // github.com/dotnet/corefx/issues/21273
                        Interop.ErrorInfo fileExistsError;

                        // Input allows trailing separators in order to match Windows behavior
                        // Unix does not accept trailing separators, so must be trimmed
                        if (!FileExists(PathHelpers.TrimEndingDirectorySeparator(fullPath),
                            Interop.Sys.FileTypes.S_IFREG, out fileExistsError) &&
                            fileExistsError.Error == Interop.Error.ENOENT)
                        {
                            return;
                        }
                        goto default;
                    case Interop.Error.EISDIR:
                        errorInfo = Interop.Error.EACCES.Info();
                        goto default;
                    default: 
                        throw Interop.GetExceptionForIoErrno(errorInfo, fullPath);
                }
            }
        }

        public static void CreateDirectory(string fullPath)
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

                // The mkdir command uses 0777 by default (it'll be AND'd with the process umask internally).
                // We do the same.
                result = Interop.Sys.MkDir(name, (int)Interop.Sys.Permissions.Mask);
                if (result < 0 && firstError.Error == 0)
                {
                    Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();

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

        public static void MoveDirectory(string sourceFullPath, string destFullPath)
        {
            // Windows doesn't care if you try and copy a file via "MoveDirectory"...
            if (FileExists(sourceFullPath))
            {
                // ... but it doesn't like the source to have a trailing slash ...

                // On Windows we end up with ERROR_INVALID_NAME, which is
                // "The filename, directory name, or volume label syntax is incorrect."
                //
                // This surfaces as a IOException, if we let it go beyond here it would
                // give DirectoryNotFound.

                if (PathHelpers.EndsInDirectorySeparator(sourceFullPath))
                    throw new IOException(SR.Format(SR.IO_PathNotFound_Path, sourceFullPath));

                // ... but it doesn't care if the destination has a trailing separator.
                destFullPath = PathHelpers.TrimEndingDirectorySeparator(destFullPath);
            }

            if (Interop.Sys.Rename(sourceFullPath, destFullPath) < 0)
            {
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                switch (errorInfo.Error)
                {
                    case Interop.Error.EACCES: // match Win32 exception
                        throw new IOException(SR.Format(SR.UnauthorizedAccess_IODenied_Path, sourceFullPath), errorInfo.RawErrno);
                    default:
                        throw Interop.GetExceptionForIoErrno(errorInfo, sourceFullPath, isDirectory: true);
                }
            }
        }

        public static void RemoveDirectory(string fullPath, bool recursive)
        {
            var di = new DirectoryInfo(fullPath);
            if (!di.Exists)
            {
                throw Interop.GetExceptionForIoErrno(Interop.Error.ENOENT.Info(), fullPath, isDirectory: true);
            }
            RemoveDirectoryInternal(di, recursive, throwOnTopLevelDirectoryNotFound: true);
        }

        private static void RemoveDirectoryInternal(DirectoryInfo directory, bool recursive, bool throwOnTopLevelDirectoryNotFound)
        {
            Exception firstException = null;

            if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                DeleteFile(directory.FullName);
                return;
            }

            if (recursive)
            {
                try
                {
                    foreach (string item in EnumeratePaths(directory.FullName, "*", SearchOption.TopDirectoryOnly, SearchTarget.Both))
                    {
                        if (!ShouldIgnoreDirectory(Path.GetFileName(item)))
                        {
                            try
                            {
                                var childDirectory = new DirectoryInfo(item);
                                if (childDirectory.Exists)
                                {
                                    RemoveDirectoryInternal(childDirectory, recursive, throwOnTopLevelDirectoryNotFound: false);
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

            if (Interop.Sys.RmDir(directory.FullName) < 0)
            {
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                switch (errorInfo.Error)
                {
                    case Interop.Error.EACCES:
                    case Interop.Error.EPERM:
                    case Interop.Error.EROFS:
                    case Interop.Error.EISDIR:
                        throw new IOException(SR.Format(SR.UnauthorizedAccess_IODenied_Path, directory.FullName)); // match Win32 exception
                    case Interop.Error.ENOENT:
                        if (!throwOnTopLevelDirectoryNotFound)
                        {
                            return;
                        }
                        goto default;
                    default:
                        throw Interop.GetExceptionForIoErrno(errorInfo, directory.FullName, isDirectory: true);
                }
            }
        }

        public static bool DirectoryExists(string fullPath)
        {
            Interop.ErrorInfo ignored;
            return DirectoryExists(fullPath, out ignored);
        }

        private static bool DirectoryExists(string fullPath, out Interop.ErrorInfo errorInfo)
        {
            return FileExists(fullPath, Interop.Sys.FileTypes.S_IFDIR, out errorInfo);
        }

        public static bool FileExists(string fullPath)
        {
            Interop.ErrorInfo ignored;

            // Input allows trailing separators in order to match Windows behavior
            // Unix does not accept trailing separators, so must be trimmed
            return FileExists(PathHelpers.TrimEndingDirectorySeparator(fullPath), Interop.Sys.FileTypes.S_IFREG, out ignored);
        }

        private static bool FileExists(string fullPath, int fileType, out Interop.ErrorInfo errorInfo)
        {
            Debug.Assert(fileType == Interop.Sys.FileTypes.S_IFREG || fileType == Interop.Sys.FileTypes.S_IFDIR);

            Interop.Sys.FileStatus fileinfo;
            errorInfo = default(Interop.ErrorInfo);

            // First use stat, as we want to follow symlinks.  If that fails, it could be because the symlink
            // is broken, we don't have permissions, etc., in which case fall back to using LStat to evaluate
            // based on the symlink itself.
            if (Interop.Sys.Stat(fullPath, out fileinfo) < 0 &&
                Interop.Sys.LStat(fullPath, out fileinfo) < 0)
            {
                errorInfo = Interop.Sys.GetLastErrorInfo();
                return false;
            }

            // Something exists at this path.  If the caller is asking for a directory, return true if it's
            // a directory and false for everything else.  If the caller is asking for a file, return false for
            // a directory and true for everything else.
            return
                (fileType == Interop.Sys.FileTypes.S_IFDIR) ==
                ((fileinfo.Mode & Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFDIR);
        }

        public static IEnumerable<string> EnumeratePaths(string path, string searchPattern, SearchOption searchOption, SearchTarget searchTarget)
        {
            return new FileSystemEnumerable<string>(path, searchPattern, searchOption, searchTarget, (p, _) => p);
        }

        public static IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(string fullPath, string searchPattern, SearchOption searchOption, SearchTarget searchTarget)
        {
            switch (searchTarget)
            {
                case SearchTarget.Files:
                    return new FileSystemEnumerable<FileInfo>(fullPath, searchPattern, searchOption, searchTarget, (path, isDir) =>
                        {
                            var info = new FileInfo(path, null);
                            info.Refresh();
                            return info;
                        });
                case SearchTarget.Directories:
                    return new FileSystemEnumerable<DirectoryInfo>(fullPath, searchPattern, searchOption, searchTarget, (path, isDir) =>
                        {
                            var info = new DirectoryInfo(path, null);
                            info.Refresh();
                            return info;
                        });
                default:
                    return new FileSystemEnumerable<FileSystemInfo>(fullPath, searchPattern, searchOption, searchTarget, (path, isDir) =>
                        {
                            var info = isDir ?
                                (FileSystemInfo)new DirectoryInfo(path, null) :
                                (FileSystemInfo)new FileInfo(path, null);
                            info.Refresh();
                            return info;
                        });
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
                if (string.IsNullOrEmpty(userPath))
                {
                    throw new ArgumentException(SR.Argument_EmptyPath, "path");
                }

                // Validate and normalize the search pattern.  If after doing so it's empty,
                // matching Win32 behavior we can skip all additional validation and effectively
                // return an empty enumerable.
                searchPattern = NormalizeSearchPattern(searchPattern);
                if (searchPattern.Length > 0)
                {
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

                    // Typically we shouldn't see either of these cases, an upfront check is much faster
                    foreach (char c in searchPattern)
                    {
                        if (c == '\\' || c == '[')
                        {
                            // We need to escape any escape characters in the search pattern
                            searchPattern = searchPattern.Replace(@"\", @"\\");

                            // And then escape '[' to prevent it being picked up as a wildcard
                            searchPattern = searchPattern.Replace(@"[", @"\[");
                            break;
                        }
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
                                // If we can't (e.g. symlink to a file, broken symlink, etc.), we'll just treat it as a file.
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

        public static string GetCurrentDirectory()
        {
            return Interop.Sys.GetCwd();
        }

        public static void SetCurrentDirectory(string fullPath)
        {
            Interop.CheckIo(Interop.Sys.ChDir(fullPath), fullPath, isDirectory:true);
        }

        public static FileAttributes GetAttributes(string fullPath)
        {
            FileAttributes attributes = new FileInfo(fullPath, null).Attributes;

            if (attributes == (FileAttributes)(-1))
                FileSystemInfo.ThrowNotFound(fullPath);

            return attributes;
        }

        public static void SetAttributes(string fullPath, FileAttributes attributes)
        {
            new FileInfo(fullPath, null).Attributes = attributes;
        }

        public static DateTimeOffset GetCreationTime(string fullPath)
        {
            return new FileInfo(fullPath, null).CreationTime;
        }

        public static void SetCreationTime(string fullPath, DateTimeOffset time, bool asDirectory)
        {
            FileSystemInfo info = asDirectory ?
                (FileSystemInfo)new DirectoryInfo(fullPath, null) :
                (FileSystemInfo)new FileInfo(fullPath, null);

            info.CreationTimeCore = time;
        }

        public static DateTimeOffset GetLastAccessTime(string fullPath)
        {
            return new FileInfo(fullPath, null).LastAccessTime;
        }

        public static void SetLastAccessTime(string fullPath, DateTimeOffset time, bool asDirectory)
        {
            FileSystemInfo info = asDirectory ?
                (FileSystemInfo)new DirectoryInfo(fullPath, null) :
                (FileSystemInfo)new FileInfo(fullPath, null);

            info.LastAccessTimeCore = time;
        }

        public static DateTimeOffset GetLastWriteTime(string fullPath)
        {
            return new FileInfo(fullPath, null).LastWriteTime;
        }

        public static void SetLastWriteTime(string fullPath, DateTimeOffset time, bool asDirectory)
        {
            FileSystemInfo info = asDirectory ?
                (FileSystemInfo)new DirectoryInfo(fullPath, null) :
                (FileSystemInfo)new FileInfo(fullPath, null);

            info.LastWriteTimeCore = time;
        }

        public static FileSystemInfo GetFileSystemInfo(string fullPath, bool asDirectory)
        {
            return asDirectory ?
                (FileSystemInfo)new DirectoryInfo(fullPath, null) :
                (FileSystemInfo)new FileInfo(fullPath, null);
        }

        public static string[] GetLogicalDrives()
        {
            return DriveInfoInternal.GetLogicalDrives();
        }
    }
}
