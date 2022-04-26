// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Enumeration;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    /// <summary>Provides an implementation of FileSystem for Unix systems.</summary>
    internal static partial class FileSystem
    {
        // On Linux, the maximum number of symbolic links that are followed while resolving a pathname is 40.
        // See: https://man7.org/linux/man-pages/man7/path_resolution.7.html
        private const int MaxFollowedLinks = 40;

        public static void CopyFile(string sourceFullPath, string destFullPath, bool overwrite)
        {
            long fileLength;
            Interop.Sys.Permissions filePermissions;
            using SafeFileHandle src = SafeFileHandle.OpenReadOnly(sourceFullPath, FileOptions.None, out fileLength, out filePermissions);
            using SafeFileHandle dst = SafeFileHandle.Open(destFullPath, overwrite ? FileMode.Create : FileMode.CreateNew,
                                            FileAccess.ReadWrite, FileShare.None, FileOptions.None, preallocationSize: 0, openPermissions: filePermissions,
                                            (Interop.ErrorInfo error, Interop.Sys.OpenFlags flags, string path) => CreateOpenException(error, flags, path));

            Interop.CheckIo(Interop.Sys.CopyFile(src, dst, fileLength));

            static Exception? CreateOpenException(Interop.ErrorInfo error, Interop.Sys.OpenFlags flags, string path)
            {
                // If the destination path points to a directory, we throw to match Windows behaviour.
                if (error.Error == Interop.Error.EEXIST && DirectoryExists(path))
                {
                    return new IOException(SR.Format(SR.Arg_FileIsDirectory_Name, path));
                }

                return null; // Let SafeFileHandle create the exception for this error.
            }
        }

#pragma warning disable IDE0060
        public static void Encrypt(string path)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_FileEncryption);
        }

        public static void Decrypt(string path)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_FileEncryption);
        }
#pragma warning restore IDE0060

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

#pragma warning disable IDE0060
        public static void ReplaceFile(string sourceFullPath, string destFullPath, string? destBackupFullPath, bool ignoreMetadataErrors /* unused */)
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
                if (Interop.Sys.Stat(destFullPath, out _) != 0)
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
#pragma warning restore IDE0060

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
            // The argument is a full path, which means it is an absolute path that
            // doesn't contain "//", "/./", and "/../".
            Debug.Assert(fullPath.Length > 0);
            Debug.Assert(PathInternal.IsDirectorySeparator(fullPath[0]));

            if (fullPath.Length == 1)
            {
                return; // fullPath is '/'.
            }

            int result = Interop.Sys.MkDir(fullPath, (int)Interop.Sys.Permissions.Mask);
            if (result == 0)
            {
                return; // Created directory.
            }

            Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
            if (errorInfo.Error == Interop.Error.EEXIST && DirectoryExists(fullPath))
            {
                return; // Path already exists and it's a directory.
            }
            else if (errorInfo.Error == Interop.Error.ENOENT) // Some parts of the path don't exist yet.
            {
                CreateParentsAndDirectory(fullPath);
            }
            else
            {
                throw Interop.GetExceptionForIoErrno(errorInfo, fullPath, isDirectory: true);
            }
        }

        private static void CreateParentsAndDirectory(string fullPath)
        {
            // Try create parents bottom to top and track those that could not
            // be created due to missing parents. Then create them top to bottom.
            using ValueListBuilder<int> stackDir = new(stackalloc int[32]); // 32 arbitrarily chosen
            stackDir.Append(fullPath.Length);

            int i = fullPath.Length - 1;
            if (PathInternal.IsDirectorySeparator(fullPath[i]))
            {
                i--; // Trim trailing separator.
            }

            do
            {
                // Find the end of the parent directory.
                Debug.Assert(!PathInternal.IsDirectorySeparator(fullPath[i]));
                while (!PathInternal.IsDirectorySeparator(fullPath[i]))
                {
                    i--;
                }

                ReadOnlySpan<char> mkdirPath = fullPath.AsSpan(0, i);
                int result = Interop.Sys.MkDir(mkdirPath, (int)Interop.Sys.Permissions.Mask);
                if (result == 0)
                {
                    break; // Created parent.
                }

                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                if (errorInfo.Error == Interop.Error.ENOENT)
                {
                    // Some parts of the path don't exist yet.
                    // We'll try to create its parent on the next iteration.

                    // Track this path for later creation.
                    stackDir.Append(mkdirPath.Length);
                }
                else if (errorInfo.Error == Interop.Error.EEXIST)
                {
                    // Parent exists.
                    // If it is not a directory, MkDir will fail when we create a child directory.
                    break;
                }
                else
                {
                    throw Interop.GetExceptionForIoErrno(errorInfo, mkdirPath.ToString(), isDirectory: true);
                }
                i--;
            } while (i > 0);

            // Create directories that had missing parents.
            for (i = stackDir.Length - 1; i >= 0; i--)
            {
                ReadOnlySpan<char> mkdirPath = fullPath.AsSpan(0, stackDir[i]);
                int result = Interop.Sys.MkDir(mkdirPath, (int)Interop.Sys.Permissions.Mask);
                if (result < 0)
                {
                    Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                    if (errorInfo.Error == Interop.Error.EEXIST)
                    {
                        // Path was created since we last checked.
                        // Continue, and for the last item, which is fullPath,
                        // verify it is actually a directory.
                        if (i != 0)
                        {
                            continue;
                        }
                        if (DirectoryExists(mkdirPath))
                        {
                            return;
                        }
                    }

                    throw Interop.GetExceptionForIoErrno(errorInfo, mkdirPath.ToString(), isDirectory: true);
                }
            }
        }

        private static void MoveDirectory(string sourceFullPath, string destFullPath, bool isCaseSensitiveRename)
        {
            // isCaseSensitiveRename is only set for case-insensitive systems (like macOS).
            Debug.Assert(!isCaseSensitiveRename || !PathInternal.IsCaseSensitive);

            ReadOnlySpan<char> srcNoDirectorySeparator = Path.TrimEndingDirectorySeparator(sourceFullPath.AsSpan());
            ReadOnlySpan<char> destNoDirectorySeparator = Path.TrimEndingDirectorySeparator(destFullPath.AsSpan());

            // When the path ends with a directory separator, it must not be a file.
            // On Unix 'rename' fails with ENOTDIR, on wasm we need to manually check.
            if (OperatingSystem.IsBrowser() && Path.EndsInDirectorySeparator(sourceFullPath) && FileExists(sourceFullPath))
            {
                throw new IOException(SR.Format(SR.IO_PathNotFound_Path, sourceFullPath));
            }

            // The destination must not exist (unless it is a case-sensitive rename).
            // On Unix 'rename' will overwrite the destination file if it already exists, we need to manually check.
            if (!isCaseSensitiveRename && Interop.Sys.LStat(destNoDirectorySeparator, out Interop.Sys.FileStatus destFileStatus) >= 0)
            {
                // Maintain order of exceptions as on Windows.

                // Throw if the source doesn't exist.
                if (Interop.Sys.LStat(srcNoDirectorySeparator, out Interop.Sys.FileStatus sourceFileStatus) < 0)
                {
                    throw new DirectoryNotFoundException(SR.Format(SR.IO_PathNotFound_Path, sourceFullPath));
                }
                // Source and destination must not be the same file unless it is a case-sensitive rename.
                else if (sourceFileStatus.Dev == destFileStatus.Dev &&
                         sourceFileStatus.Ino == destFileStatus.Ino)
                {
                    // isCaseSensitiveRename is only true when the system is case-insensitive (like macOS).
                    // On a case-sensitive system (like Linux), there can stil be case-insensitive filesystems mounted.
                    // When both paths refer to the same file and they differ only in casing, we fall through to Rename.
                    if (!PathInternal.IsCaseSensitive && // handled by isCaseSensitiveRename.
                        !srcNoDirectorySeparator.Equals(destNoDirectorySeparator, StringComparison.OrdinalIgnoreCase) ||     // different paths.
                        Path.GetFileName(srcNoDirectorySeparator).SequenceEqual(Path.GetFileName(destNoDirectorySeparator))) // same names.
                    {
                        throw new IOException(SR.IO_SourceDestMustBeDifferent);
                    }
                }
                // When the path ends with a directory separator, it must be a directory.
                else if ((sourceFileStatus.Mode & Interop.Sys.FileTypes.S_IFMT) != Interop.Sys.FileTypes.S_IFDIR
                    && Path.EndsInDirectorySeparator(sourceFullPath))
                {
                    throw new IOException(SR.Format(SR.IO_PathNotFound_Path, sourceFullPath));
                }
                else
                {
                    throw new IOException(SR.Format(SR.IO_AlreadyExists_Name, destFullPath));
                }
            }

            if (Interop.Sys.Rename(sourceFullPath, destNoDirectorySeparator) < 0)
            {
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                switch (errorInfo.Error)
                {
                    case Interop.Error.EACCES: // match Win32 exception
                        throw new IOException(SR.Format(SR.UnauthorizedAccess_IODenied_Path, sourceFullPath), errorInfo.RawErrno);
                    case Interop.Error.ENOENT:
                        throw new DirectoryNotFoundException(SR.Format(SR.IO_PathNotFound_Path, sourceFullPath));
                    case Interop.Error.ENOTDIR: // sourceFullPath exists and it's not a directory
                        throw new IOException(SR.Format(SR.IO_PathNotFound_Path, sourceFullPath));
                    default:
                        throw Interop.GetExceptionForIoErrno(errorInfo, isDirectory: true);
                }
            }
        }

        public static void RemoveDirectory(string fullPath, bool recursive)
        {
            // Delete the directory.
            // If we're recursing, don't throw when it is not empty, and perform a recursive remove.
            if (!RemoveEmptyDirectory(fullPath, topLevel: true, throwWhenNotEmpty: !recursive))
            {
                Debug.Assert(recursive);

                RemoveDirectoryRecursive(fullPath);
            }
        }

        private static void RemoveDirectoryRecursive(string fullPath)
        {
            Exception? firstException = null;

            try
            {
                var fse = new FileSystemEnumerable<(string, bool)>(fullPath,
                            static (ref FileSystemEntry entry) =>
                            {
                                // Don't report symlinks to directories as directories.
                                bool isRealDirectory = !entry.IsSymbolicLink && entry.IsDirectory;
                                return (entry.ToFullPath(), isRealDirectory);
                            },
                            EnumerationOptions.Compatible);

                foreach ((string childPath, bool isDirectory) in fse)
                {
                    try
                    {
                        if (isDirectory)
                        {
                            RemoveDirectoryRecursive(childPath);
                        }
                        else
                        {
                            DeleteFile(childPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        firstException ??= ex;
                    }
                }
            }
            catch (Exception exc)
            {
                firstException ??= exc;
            }

            if (firstException != null)
            {
                throw firstException;
            }

            RemoveEmptyDirectory(fullPath);
        }

        private static bool RemoveEmptyDirectory(string fullPath, bool topLevel = false, bool throwWhenNotEmpty = true)
        {
            if (Interop.Sys.RmDir(fullPath) < 0)
            {
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();

                if (errorInfo.Error == Interop.Error.ENOTEMPTY)
                {
                    if (!throwWhenNotEmpty)
                    {
                        return false;
                    }
                }
                else if (errorInfo.Error == Interop.Error.ENOENT)
                {
                    // When we're recursing, don't throw for items that go missing.
                    if (!topLevel)
                    {
                        return true;
                    }
                }
                else if (DirectoryExists(fullPath, out Interop.ErrorInfo existErr))
                {
                    // Top-level path is a symlink to a directory, delete the link.
                    if (topLevel && errorInfo.Error == Interop.Error.ENOTDIR)
                    {
                        DeleteFile(fullPath);
                        return true;
                    }
                }
                else if (existErr.Error == Interop.Error.ENOENT)
                {
                    // Prefer throwing DirectoryNotFoundException over other exceptions.
                    errorInfo = existErr;
                }

                if (errorInfo.Error == Interop.Error.EACCES ||
                    errorInfo.Error == Interop.Error.EPERM ||
                    errorInfo.Error == Interop.Error.EROFS)
                {
                    throw new IOException(SR.Format(SR.UnauthorizedAccess_IODenied_Path, fullPath));
                }

                throw Interop.GetExceptionForIoErrno(errorInfo, fullPath, isDirectory: true);
            }

            return true;
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
            => default(FileStatus).SetAttributes(fullPath, attributes, asDirectory: false);

        public static DateTimeOffset GetCreationTime(string fullPath)
            => default(FileStatus).GetCreationTime(fullPath).UtcDateTime;

        public static void SetCreationTime(string fullPath, DateTimeOffset time, bool asDirectory)
            => default(FileStatus).SetCreationTime(fullPath, time, asDirectory);

        public static DateTimeOffset GetLastAccessTime(string fullPath)
            => default(FileStatus).GetLastAccessTime(fullPath).UtcDateTime;

        public static void SetLastAccessTime(string fullPath, DateTimeOffset time, bool asDirectory)
            => default(FileStatus).SetLastAccessTime(fullPath, time, asDirectory);

        public static DateTimeOffset GetLastWriteTime(string fullPath)
            => default(FileStatus).GetLastWriteTime(fullPath).UtcDateTime;

        public static void SetLastWriteTime(string fullPath, DateTimeOffset time, bool asDirectory)
            => default(FileStatus).SetLastWriteTime(fullPath, time, asDirectory);

        public static string[] GetLogicalDrives()
        {
            return DriveInfoInternal.GetLogicalDrives();
        }

#pragma warning disable IDE0060
        internal static string? GetLinkTarget(ReadOnlySpan<char> linkPath, bool isDirectory) => Interop.Sys.ReadLink(linkPath);
#pragma warning restore IDE0060

        internal static void CreateSymbolicLink(string path, string pathToTarget, bool isDirectory)
        {
            string pathToTargetFullPath = PathInternal.GetLinkTargetFullPath(path, pathToTarget);
            Interop.CheckIo(Interop.Sys.SymLink(pathToTarget, path), path, isDirectory);
        }

        internal static FileSystemInfo? ResolveLinkTarget(string linkPath, bool returnFinalTarget, bool isDirectory)
        {
            ValueStringBuilder sb = new(Interop.DefaultPathBufferSize);
            sb.Append(linkPath);

            string? linkTarget = Interop.Sys.ReadLink(linkPath);
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
                    current = Interop.Sys.ReadLink(sb.AsSpan());
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
