// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace System.IO
{
    /// <summary>Provides an implementation of FileSystem for Unix systems.</summary>
    internal sealed partial class UnixFileSystem : FileSystem
    {
        /// <summary>The maximum path length for the system.  -1 if it hasn't yet been initialized.</summary>
        private static int _maxPath = -1;
        /// <summary>The maximum name length for the system.  -1 if it hasn't yet been initialized.</summary>
        private static int _maxName = -1;

        public override int MaxPath 
        {
            get { return GetPathConfValue(ref _maxPath, Interop.libc.PathConfNames._PC_PATH_MAX, Interop.libc.DEFAULT_PC_PATH_MAX); } 
        }

        public override int MaxDirectoryPath 
        {
            get { return GetPathConfValue(ref _maxName, Interop.libc.PathConfNames._PC_NAME_MAX, Interop.libc.DEFAULT_PC_NAME_MAX); } 
        }

        /// <summary>
        /// Gets a pathconf value by name.  If the cached value is less than zero (meaning not yet initialized),
        /// pathconf is used to retrieve the value, which is then stored into the field.
        /// If the field is greater than or equal to zero, it's value is returned.
        /// </summary>
        /// <param name="cachedValue">The field used to cache the pathconf value.</param>
        /// <param name="pathConfName">The name of the pathconf value.</param>
        /// <param name="defaultValue">The default value to use in case pathconf fails.</param>
        /// <returns>The pathconf value, or the default if pathconf failed to return a value.</returns>
        private static int GetPathConfValue(ref int cachedValue, int pathConfName, int defaultValue)
        {
            if (cachedValue < 0) // benign race condition on cached value
            {
                int result = Interop.libc.pathconf("/", pathConfName);
                cachedValue = result >= 0 ? result : defaultValue;
            }
            return cachedValue;
        }

        public override FileStreamBase Open(string fullPath, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, FileStream parent)
        {
            return new UnixFileStream(fullPath, mode, access, share, bufferSize, options, parent);
        }

        public override void CopyFile(string sourceFullPath, string destFullPath, bool overwrite)
        {
            // Note: we could consider using sendfile here, but it isn't part of the POSIX spec, and
            // has varying degrees of support on different systems.

            // Copy the contents of the file from the source to the destination, creating the destination in the process
            const int bufferSize = 0x1000;
            using (UnixFileStream src = new UnixFileStream(sourceFullPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.None))
            using (UnixFileStream dst = new UnixFileStream(destFullPath, overwrite ? FileMode.CreateNew : FileMode.Create, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.None))
            {
                src.CopyTo(dst);
            }

            // Now copy over relevant read/write/execute permissions from the source to the destination
            Interop.libc.structStat stat;
            while (Interop.CheckIo(Interop.libc.stat(sourceFullPath, out stat), sourceFullPath)) ;
            int newMode = stat.st_mode & (int)Interop.libc.Permissions.Mask;
            while (Interop.CheckIo(Interop.libc.chmod(destFullPath, newMode), destFullPath)) ;
        }

        public override void MoveFile(string sourceFullPath, string destFullPath)
        {
            while (Interop.libc.rename(sourceFullPath, destFullPath) < 0)
            {
                int errno = Marshal.GetLastWin32Error();
                if (errno == Interop.Errors.EINTR) // interrupted; try again
                {
                    continue;
                }
                else if (errno == Interop.Errors.EXDEV) // rename fails across devices / mount points
                {
                    CopyFile(sourceFullPath, destFullPath, overwrite: false);
                    DeleteFile(sourceFullPath);
                    break;
                }
                else
                {
                    throw Interop.GetExceptionForIoErrno(errno);
                }
            }
        }

        public override void DeleteFile(string fullPath)
        {
            while (Interop.libc.remove(fullPath) < 0)
            {
                int errno = Marshal.GetLastWin32Error();
                if (errno == Interop.Errors.EINTR) // interrupted; try again
                {
                    continue;
                }
                else if (errno == Interop.Errors.ENOENT) // already doesn't exist; nop
                {
                    break;
                }
                else
                {
                    throw Interop.GetExceptionForIoErrno(errno, fullPath);
                }
            }
        }

        public override bool FileExists(string fullPath)
        {
            while (Interop.libc.access(fullPath, Interop.libc.AccessModes.F_OK) < 0)
            {
                if (Marshal.GetLastWin32Error() == Interop.Errors.EINTR)
                {
                    continue;
                }
                return false;
            }
            return true;
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
            if (length == 2 && PathHelpers.IsDirectorySeparator(fullPath[1]))
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
            int lengthRoot = PathHelpers.GetRootLength(fullPath);
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

                    while (i > lengthRoot && !PathHelpers.IsDirectorySeparator(fullPath[i]))
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
                    throw Interop.GetExceptionForIoErrno(Interop.Errors.ENOENT, fullPath, isDirectory: true);
                }
                return;
            }

            // Create all the directories
            int result = 0;
            int firstError = 0;
            string errorString = fullPath;
            while (stackDir.Count > 0)
            {
                string name = stackDir.Pop();
                if (name.Length >= MaxDirectoryPath)
                {
                    throw new PathTooLongException(SR.IO_PathTooLong);
                }

                int errno = 0;
                while ((result = Interop.libc.mkdir(name, (int)Interop.libc.Permissions.S_IRWXU)) < 0 && (errno = Marshal.GetLastWin32Error()) == Interop.Errors.EINTR) ;
                if (result < 0 && firstError == 0)
                {
                    // While we tried to avoid creating directories that don't
                    // exist above, there are a few cases that can fail, e.g.
                    // a race condition where another process or thread creates
                    // the directory first, or there's a file at the location.
                    if (errno != Interop.Errors.EEXIST)
                    {
                        firstError = errno;
                    }
                    else if (FileExists(name) || (!DirectoryExists(name, out errno) && errno == Interop.Errors.EACCES))
                    {
                        // If there's a file in this directory's place, or if we have ERROR_ACCESS_DENIED when checking if the directory already exists throw.
                        firstError = errno;
                        errorString = name;
                    }
                }
            }

            // Only throw an exception if creating the exact directory we wanted failed to work correctly.
            if (result < 0 && firstError != 0)
            {
                throw Interop.GetExceptionForIoErrno(firstError, errorString, isDirectory: true);
            }
        }

        public override void MoveDirectory(string sourceFullPath, string destFullPath)
        {
            while (Interop.libc.rename(sourceFullPath, destFullPath) < 0)
            {
                int errno = Marshal.GetLastWin32Error();
                switch (errno)
                {
                    case Interop.Errors.EINTR: // interrupted; try again
                        continue;
                    case Interop.Errors.EACCES: // match Win32 exception
                        throw new IOException(SR.Format(SR.UnauthorizedAccess_IODenied_Path, sourceFullPath), errno);
                    default:
                        throw Interop.GetExceptionForIoErrno(errno, sourceFullPath, isDirectory: true);
                }
            }
        }

        public override void RemoveDirectory(string fullPath, bool recursive)
        {
            if (!DirectoryExists(fullPath))
            {
                throw Interop.GetExceptionForIoErrno(Interop.Errors.ENOENT, fullPath, isDirectory: true);
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

            while (Interop.libc.remove(fullPath) < 0)
            {
                int errno = Marshal.GetLastWin32Error();
                switch (errno)
                {
                    case Interop.Errors.EINTR: // interrupted; try again
                        continue;
                    case Interop.Errors.EACCES:
                        throw new IOException(SR.Format(SR.UnauthorizedAccess_IODenied_Path, fullPath)); // match Win32 exception
                    case Interop.Errors.ENOENT:
                        if (!throwOnTopLevelDirectoryNotFound)
                        {
                            return;
                        }
                        goto default;
                    default:
                        throw Interop.GetExceptionForIoErrno(errno, fullPath, isDirectory: true);
                }
            }
        }

        public override bool DirectoryExists(string fullPath)
        {
            int errno;
            return DirectoryExists(fullPath, out errno);
        }

        private bool DirectoryExists(string fullPath, out int errno)
        {
            Interop.libc.structStat stat;
            while (true)
            {
                errno = 0;
                int result = Interop.libc.stat(fullPath, out stat);
                if (result < 0)
                {
                    errno = Marshal.GetLastWin32Error();
                    if (errno == Interop.Errors.EINTR)
                    {
                        continue;
                    }
                    return false;
                }
                return (stat.st_mode & (int)Interop.libc.FileTypes.S_IFMT) == (int)Interop.libc.FileTypes.S_IFDIR;
            }
        }

        public override IEnumerable<string> EnumeratePaths(string fullPath, string searchPattern, SearchOption searchOption, SearchTarget searchTarget)
        {
            return EnumerateResults<string>(fullPath, searchPattern, searchOption, searchTarget, (path, _) => path);
        }

        public override IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(string fullPath, string searchPattern, SearchOption searchOption, SearchTarget searchTarget)
        {
            switch (searchTarget)
            {
                case SearchTarget.Files:
                    return EnumerateResults<FileInfo>(fullPath, searchPattern, searchOption, searchTarget, (path, isDir) =>
                        new FileInfo(path, new UnixFileSystemObject(path, isDir)));
                case SearchTarget.Directories:
                    return EnumerateResults<DirectoryInfo>(fullPath, searchPattern, searchOption, searchTarget, (path, isDir) =>
                        new DirectoryInfo(path, new UnixFileSystemObject(path, isDir)));
                default:
                    return EnumerateResults<FileSystemInfo>(fullPath, searchPattern, searchOption, searchTarget, (path, isDir) => isDir ?
                        (FileSystemInfo)new DirectoryInfo(path, new UnixFileSystemObject(path, isDir)) :
                        (FileSystemInfo)new FileInfo(path, new UnixFileSystemObject(path, isDir)));
            }
        }

        private static IEnumerable<T> EnumerateResults<T>(string fullPath, string searchPattern, SearchOption searchOption, SearchTarget searchTarget, Func<string, bool, T> translateResult)
        {
            // Maintain a stack of the directories to explore, in the case of SearchOption.AllDirectories
            var toExplore = new Stack<string>();
            toExplore.Push(fullPath);

            // Check whether we care about files, directories, or both
            bool includeFiles = (searchTarget & SearchTarget.Files) != 0;
            bool includeDirectories = (searchTarget & SearchTarget.Directories) != 0;

            // Process directories until we're out
            while (toExplore.Count > 0)
            {
                // Get the next directory to process
                string dirPath = toExplore.Pop();

                // Open an enumerator of its contents.
                IntPtr pdir;
                while (Interop.CheckIoPtr(pdir = Interop.libc.opendir(dirPath), dirPath, isDirectory: true)) ;
                try
                {
                    // Read each entry from the enumerator
                    IntPtr curEntry;
                    while ((curEntry = Interop.libc.readdir(pdir)) != IntPtr.Zero) // no validation needed for readdir
                    {
                        // Get the name and full name of the entry
                        string name = GetDirEntName(curEntry);
                        string fullNewName = dirPath + "/" + name;

                        // Determine whether the entry is a file or a directory and whether it matches the supplied pattern
                        Interop.libc.structStat stat;
                        while (Interop.CheckIo(Interop.libc.stat(fullNewName, out stat), fullNewName)) ;
                        bool isDir = (stat.st_mode & (int)Interop.libc.FileTypes.S_IFMT) == (int)Interop.libc.FileTypes.S_IFDIR && !ShouldIgnoreDirectory(name);
                        bool isFile = (stat.st_mode & (int)Interop.libc.FileTypes.S_IFMT) == (int)Interop.libc.FileTypes.S_IFREG;
                        bool matchesSearchPattern = Interop.libc.fnmatch(searchPattern, name, Interop.libc.FnmatchFlags.None) == 0;

                        // Yield the result if the user has asked for it.  In the case of directories,
                        // always explore it by pushing it onto the stack, regardless of whether
                        // we're returning directories.
                        if (isDir)
                        {
                            if (includeDirectories && matchesSearchPattern)
                            {
                                yield return translateResult(fullNewName, /*isDirectory*/true);
                            }
                            if (searchOption == SearchOption.AllDirectories)
                            {
                                toExplore.Push(fullNewName);
                            }
                        }
                        else if (isFile && includeFiles && matchesSearchPattern)
                        {
                            yield return translateResult(fullNewName, /*isDirectory*/false);
                        }
                    }
                }
                finally
                {
                    // Close the directory enumerator
                    while (Interop.CheckIo(Interop.libc.closedir(pdir), dirPath)) ;
                }
            }
        }

        /// <summary>Gets the name of a directory from a dirent*.</summary>
        /// <param name="dirEnt">
        /// The pointer to the dirent.  It's represented as an IntPtr, as unsafe code
        /// can't be used in iterators.
        /// </param>
        /// <returns>
        /// The name extracted from the directory entry.
        /// </returns>
        private static unsafe string GetDirEntName(IntPtr dirEnt)
        {
            Interop.libc.dirent* curEntryPtr = (Interop.libc.dirent*)dirEnt;
            return PtrToString(curEntryPtr->d_name);
        }

        /// <summary>Creates a string from a pointer to a sequence of null-terminated bytes.</summary>
        /// <param name="buffer">A pointer to the first character in the string.  It must be null terminated.</param>
        /// <returns>The string.</returns>
        private static unsafe string PtrToString(byte* buffer)
        {
            int length = 0;
            for (byte* ptr = buffer; *ptr != 0; ptr++, length++) ;
            return Encoding.UTF8.GetString(buffer, length);
        }

        /// <summary>Determines whether the specified directory name should be ignored.</summary>
        /// <param name="name">The name to evaluate.</param>
        /// <returns>true if the name is "." or ".."; otherwise, false.</returns>
        private static bool ShouldIgnoreDirectory(string name)
        {
            return name == "." || name == "..";
        }

        public override unsafe string GetCurrentDirectory()
        {
            byte[] pathBuffer = new byte[MaxPath];
            fixed (byte* ptr = pathBuffer)
            {
                while (Interop.CheckIoPtr((IntPtr)Interop.libc.getcwd(ptr, (IntPtr)pathBuffer.Length))) ;
                return PtrToString(ptr);
            }
        }

        public override void SetCurrentDirectory(string fullPath)
        {
            while (Interop.CheckIo(Interop.libc.chdir(fullPath), fullPath)) ;
        }

        public override FileAttributes GetAttributes(string fullPath)
        {
            return new UnixFileSystemObject(fullPath, false).Attributes;
        }

        public override void SetAttributes(string fullPath, FileAttributes attributes)
        {
            new UnixFileSystemObject(fullPath, false).Attributes = attributes;
        }

        public override DateTimeOffset GetCreationTime(string fullPath)
        {
            return new UnixFileSystemObject(fullPath, false).CreationTime;
        }

        public override void SetCreationTime(string fullPath, DateTimeOffset time, bool asDirectory)
        {
            new UnixFileSystemObject(fullPath, asDirectory).CreationTime = time;
        }

        public override DateTimeOffset GetLastAccessTime(string fullPath)
        {
            return new UnixFileSystemObject(fullPath, false).LastAccessTime;
        }

        public override void SetLastAccessTime(string fullPath, DateTimeOffset time, bool asDirectory)
        {
            new UnixFileSystemObject(fullPath, asDirectory).LastAccessTime = time;
        }

        public override DateTimeOffset GetLastWriteTime(string fullPath)
        {
            return new UnixFileSystemObject(fullPath, false).LastAccessTime;
        }

        public override void SetLastWriteTime(string fullPath, DateTimeOffset time, bool asDirectory)
        {
            new UnixFileSystemObject(fullPath, asDirectory).LastWriteTime = time;
        }

        public override IFileSystemObject GetFileSystemInfo(string fullPath, bool asDirectory)
        {
            return new UnixFileSystemObject(fullPath, asDirectory);
        }
    }
}
