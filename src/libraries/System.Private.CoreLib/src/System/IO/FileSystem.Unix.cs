// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    /// <summary>Provides an implementation of FileSystem for Unix systems.</summary>
    internal static partial class FileSystem
    {
        internal const int DefaultBufferSize = 4096;

        // On Linux, the maximum number of symbolic links that are followed while resolving a pathname is 40.
        // See: https://man7.org/linux/man-pages/man7/path_resolution.7.html
        private const int MaxFollowedLinks = 40;

        public static void CopyFile(string sourceFullPath, string destFullPath, bool overwrite)
        {
            // If the destination path points to a directory, we throw to match Windows behaviour
            if (DirectoryExists(destFullPath))
            {
                throw new IOException(SR.Format(SR.Arg_FileIsDirectory_Name, destFullPath));
            }

            // Copy the contents of the file from the source to the destination, creating the destination in the process
            using (SafeFileHandle src = File.OpenHandle(sourceFullPath, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.None))
            using (SafeFileHandle dst = File.OpenHandle(destFullPath, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, FileOptions.None))
            {
                Interop.CheckIo(Interop.Sys.CopyFile(src, dst));
            }
        }

        public static void Encrypt(string path)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_FileEncryption);
        }

        public static void Decrypt(string path)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_FileEncryption);
        }

        private static void LinkOrCopyFile (string sourceFullPath, string destFullPath)
        {
            if (Interop.Sys.Link(sourceFullPath, destFullPath) >= 0)
                return;

            // If link fails, we can fall back to doing a full copy, but we'll only do so for
            // cases where we expect link could fail but such a copy could succeed.  We don't
            // want to do so for all errors, because the copy could incur a lot of cost
            // even if we know it'll eventually fail, e.g. EROFS means that the source file
            // system is read-only and couldn't support the link being added, but if it's
            // read-only, then the move should fail any way due to an inability to delete
            // the source file.
            Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
            if (errorInfo.Error == Interop.Error.EXDEV ||      // rename fails across devices / mount points
                errorInfo.Error == Interop.Error.EACCES ||
                errorInfo.Error == Interop.Error.EPERM ||      // permissions might not allow creating hard links even if a copy would work
                errorInfo.Error == Interop.Error.EOPNOTSUPP || // links aren't supported by the source file system
                errorInfo.Error == Interop.Error.EMLINK ||     // too many hard links to the source file
                errorInfo.Error == Interop.Error.ENOSYS)       // the file system doesn't support link
            {
                CopyFile(sourceFullPath, destFullPath, overwrite: false);
            }
            else
            {
                // The operation failed.  Within reason, try to determine which path caused the problem
                // so we can throw a detailed exception.
                string? path = null;
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


        public static void ReplaceFile(string sourceFullPath, string destFullPath, string? destBackupFullPath, bool ignoreMetadataErrors)
        {
            // Unix rename works in more cases, we limit to what is allowed by Windows File.Replace.
            // These checks are not atomic, the file could change after a check was performed and before it is renamed.
            Interop.Sys.FileStatus sourceStat;
            if (Interop.Sys.LStat(sourceFullPath, out sourceStat) != 0)
            {
                Interop.ErrorInfo errno = Interop.Sys.GetLastErrorInfo();
                throw Interop.GetExceptionForIoErrno(errno, sourceFullPath);
            }
            // Check source is not a directory.
            if ((sourceStat.Mode & Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFDIR)
            {
                throw new UnauthorizedAccessException(SR.Format(SR.IO_NotAFile, sourceFullPath));
            }

            Interop.Sys.FileStatus destStat;
            if (Interop.Sys.LStat(destFullPath, out destStat) == 0)
            {
                // Check destination is not a directory.
                if ((destStat.Mode & Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFDIR)
                {
                    throw new UnauthorizedAccessException(SR.Format(SR.IO_NotAFile, destFullPath));
                }
                // Check source and destination are not the same.
                if (sourceStat.Dev == destStat.Dev &&
                    sourceStat.Ino == destStat.Ino)
                  {
                      throw new IOException(SR.Format(SR.IO_CannotReplaceSameFile, sourceFullPath, destFullPath));
                  }
            }

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
                LinkOrCopyFile(destFullPath, destBackupFullPath);
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
            MoveFile(sourceFullPath, destFullPath, false);
        }

        public static void MoveFile(string sourceFullPath, string destFullPath, bool overwrite)
        {
            // If overwrite is allowed then just call rename
            if (overwrite)
            {
                if (Interop.Sys.Rename(sourceFullPath, destFullPath) < 0)
                {
                    Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                    if (errorInfo.Error == Interop.Error.EXDEV) // rename fails across devices / mount points
                    {
                        CopyFile(sourceFullPath, destFullPath, overwrite);
                        DeleteFile(sourceFullPath);
                    }
                    else
                    {
                        // Windows distinguishes between whether the directory or the file isn't found,
                        // and throws a different exception in these cases.  We attempt to approximate that
                        // here; there is a race condition here, where something could change between
                        // when the error occurs and our checks, but it's the best we can do, and the
                        // worst case in such a race condition (which could occur if the file system is
                        // being manipulated concurrently with these checks) is that we throw a
                        // FileNotFoundException instead of DirectoryNotFoundException.
                        throw Interop.GetExceptionForIoErrno(errorInfo, destFullPath,
                            isDirectory: errorInfo.Error == Interop.Error.ENOENT && !Directory.Exists(Path.GetDirectoryName(destFullPath))   // The parent directory of destFile can't be found
                            );
                    }
                }

                // Rename or CopyFile complete
                return;
            }

            // The desired behavior for Move(source, dest) is to not overwrite the destination file
            // if it exists. Since rename(source, dest) will replace the file at 'dest' if it exists,
            // link/unlink are used instead. Rename is more efficient than link/unlink on file systems
            // where hard links are not supported (such as FAT). Therefore, given that source file exists,
            // rename is used in 2 cases: when dest file does not exist or when source path and dest
            // path refer to the same file (on the same device). This is important for case-insensitive
            // file systems (e.g. renaming a file in a way that just changes casing), so that we support
            // changing the casing in the naming of the file. If this fails in any way (e.g. source file
            // doesn't exist, dest file doesn't exist, rename fails, etc.), we just fall back to trying the
            // link/unlink approach and generating any exceptional messages from there as necessary.

            Interop.Sys.FileStatus sourceStat, destStat;
            if (Interop.Sys.LStat(sourceFullPath, out sourceStat) == 0 && // source file exists
                (Interop.Sys.LStat(destFullPath, out destStat) != 0 || // dest file does not exist
                 (sourceStat.Dev == destStat.Dev && // source and dest are on the same device
                  sourceStat.Ino == destStat.Ino)) && // source and dest are the same file on that device
                Interop.Sys.Rename(sourceFullPath, destFullPath) == 0) // try the rename
            {
                // Renamed successfully.
                return;
            }

            LinkOrCopyFile(sourceFullPath, destFullPath);
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
                        // In order to match Windows behavior
                        string? directoryName = Path.GetDirectoryName(fullPath);
                        Debug.Assert(directoryName != null);
                        if (directoryName.Length > 0 && !Directory.Exists(directoryName))
                        {
                            throw Interop.GetExceptionForIoErrno(errorInfo, fullPath, true);
                        }
                        return;
                    case Interop.Error.EROFS:
                        // EROFS means the file system is read-only
                        // Need to manually check file existence
                        // https://github.com/dotnet/runtime/issues/22382
                        Interop.ErrorInfo fileExistsError;

                        // Input allows trailing separators in order to match Windows behavior
                        // Unix does not accept trailing separators, so must be trimmed
                        if (!FileExists(fullPath, out fileExistsError) &&
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
            if (length >= 2 && Path.EndsInDirectorySeparator(fullPath))
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
            List<string> stackDir = new List<string>();
            int lengthRoot = PathInternal.GetRootLength(fullPath);
            if (length > lengthRoot)
            {
                int i = length - 1;
                while (i >= lengthRoot && !somepathexists)
                {
                    if (!DirectoryExists(fullPath.AsSpan(0, i + 1))) // Create only the ones missing
                    {
                        stackDir.Add(fullPath.Substring(0, i + 1));
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
                ReadOnlySpan<char> root = Path.GetPathRoot(fullPath.AsSpan());
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
            for (int i = stackDir.Count - 1; i >= 0; i--)
            {
                string name = stackDir[i];

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
                // This surfaces as an IOException, if we let it go beyond here it would
                // give DirectoryNotFound.

                if (Path.EndsInDirectorySeparator(sourceFullPath))
                    throw new IOException(SR.Format(SR.IO_PathNotFound_Path, sourceFullPath));

                // ... but it doesn't care if the destination has a trailing separator.
                destFullPath = Path.TrimEndingDirectorySeparator(destFullPath);
            }

            if (FileExists(destFullPath))
            {
                // Some Unix distros will overwrite the destination file if it already exists.
                // Throwing IOException to match Windows behavior.
                throw new IOException(SR.Format(SR.IO_AlreadyExists_Name, destFullPath));
            }

            if (Interop.Sys.Rename(sourceFullPath, destFullPath) < 0)
            {
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                switch (errorInfo.Error)
                {
                    case Interop.Error.EACCES: // match Win32 exception
                        throw new IOException(SR.Format(SR.UnauthorizedAccess_IODenied_Path, sourceFullPath), errorInfo.RawErrno);
                    default:
                        throw Interop.GetExceptionForIoErrno(errorInfo, isDirectory: true);
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
            Exception? firstException = null;

            if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                DeleteFile(directory.FullName);
                return;
            }

            if (recursive)
            {
                try
                {
                    foreach (string item in Directory.EnumerateFileSystemEntries(directory.FullName))
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

        /// <summary>Determines whether the specified directory name should be ignored.</summary>
        /// <param name="name">The name to evaluate.</param>
        /// <returns>true if the name is "." or ".."; otherwise, false.</returns>
        private static bool ShouldIgnoreDirectory(string name)
        {
            return name == "." || name == "..";
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

        public static string[] GetLogicalDrives()
        {
            return DriveInfoInternal.GetLogicalDrives();
        }

        internal static string? GetLinkTarget(ReadOnlySpan<char> linkPath, bool isDirectory) => Interop.Sys.ReadLink(linkPath);

        internal static void CreateSymbolicLink(string path, string pathToTarget, bool isDirectory)
        {
            string pathToTargetFullPath = PathInternal.GetLinkTargetFullPath(path, pathToTarget);

            // Fail if the target exists but is not consistent with the expected filesystem entry type
            if (Interop.Sys.Stat(pathToTargetFullPath, out Interop.Sys.FileStatus targetInfo) == 0)
            {
                if (isDirectory != ((targetInfo.Mode & Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFDIR))
                {
                    throw new IOException(SR.Format(SR.IO_InconsistentLinkType, path));
                }
            }

            Interop.CheckIo(Interop.Sys.SymLink(pathToTarget, path), path, isDirectory);
        }

        internal static FileSystemInfo? ResolveLinkTarget(string linkPath, bool returnFinalTarget, bool isDirectory)
        {
            ValueStringBuilder sb = new(Interop.DefaultPathBufferSize);
            sb.Append(linkPath);

            string? linkTarget = GetLinkTarget(linkPath, isDirectory: false /* Irrelevant in Unix */);
            if (linkTarget == null)
            {
                sb.Dispose();
                Interop.Error error = Interop.Sys.GetLastError();
                // Not a link, return null
                if (error == Interop.Error.EINVAL)
                {
                    return null;
                }

                throw Interop.GetExceptionForIoErrno(new Interop.ErrorInfo(error), linkPath, isDirectory);
            }

            if (!returnFinalTarget)
            {
                GetLinkTargetFullPath(ref sb, linkTarget);
            }
            else
            {
                string? current = linkTarget;
                int visitCount = 1;

                while (current != null)
                {
                    if (visitCount > MaxFollowedLinks)
                    {
                        sb.Dispose();
                        // We went over the limit and couldn't reach the final target
                        throw new IOException(SR.Format(SR.IO_TooManySymbolicLinkLevels, linkPath));
                    }

                    GetLinkTargetFullPath(ref sb, current);
                    current = GetLinkTarget(sb.AsSpan(), isDirectory: false);
                    visitCount++;
                }
            }

            Debug.Assert(sb.Length > 0);
            linkTarget = sb.ToString(); // ToString disposes

            return isDirectory ?
                    new DirectoryInfo(linkTarget) :
                    new FileInfo(linkTarget);

            // In case of link target being relative:
            // Preserve the full path of the directory of the previous path
            // so the final target is returned with a valid full path
            static void GetLinkTargetFullPath(ref ValueStringBuilder sb, ReadOnlySpan<char> linkTarget)
            {
                if (PathInternal.IsPartiallyQualified(linkTarget))
                {
                    sb.Length = Path.GetDirectoryNameOffset(sb.AsSpan());
                    sb.Append(PathInternal.DirectorySeparatorChar);
                }
                else
                {
                    sb.Length = 0;
                }
                sb.Append(linkTarget);
            }
        }
    }
}
