// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.IO
{
    /// <summary>Provides an implementation of FileSystem for Unix systems.</summary>
    internal static partial class FileSystem
    {
        internal const int DefaultBufferSize = 4096;

        public static void CopyFile(string sourceFullPath, string destFullPath, bool overwrite)
        {
            // If the destination path points to a directory, we throw to match Windows behaviour
            if (DirectoryExists(destFullPath))
            {
                throw new IOException(SR.Format(SR.Arg_FileIsDirectory_Name, destFullPath));
            }

            // Copy the contents of the file from the source to the destination, creating the destination in the process
            using (var src = new FileStream(sourceFullPath, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, FileOptions.None))
            {
                int result = Interop.Sys.CopyFile(src.SafeFileHandle, sourceFullPath, destFullPath, overwrite ? 1 : 0);

                if (result < 0)
                {
                    Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();

                    // If we fail to open the file due to a path not existing, we need to know whether to blame
                    // the file itself or its directory.  If we're creating the file, then we blame the directory,
                    // otherwise we blame the file.
                    //
                    // When opening, we need to align with Windows, which considers a missing path to be
                    // FileNotFound only if the containing directory exists.

                    bool isDirectory = (error.Error == Interop.Error.ENOENT) &&
                        (overwrite || !DirectoryExists(Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(destFullPath.AsSpan()))!));

                    Interop.CheckIo(
                        error.Error,
                        destFullPath,
                        isDirectory,
                        errorRewriter: e => (e.Error == Interop.Error.EISDIR) ? Interop.Error.EACCES.Info() : e);
                }
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

        public static void ReplaceFile(string sourceFullPath, string destFullPath, string? destBackupFullPath, bool ignoreMetadataErrors)
        {
            if (destBackupFullPath != null)
            {
                // We're backing up the destination file to the backup file. If it exists, it will be removed if possible.
                if (Interop.Sys.Rename(destFullPath, destBackupFullPath, 1) < 0)
                {
                    Interop.ErrorInfo errno = Interop.Sys.GetLastErrorInfo();
                    if (errno.Error == Interop.Error.EXDEV ||      // rename fails across devices / mount points
                        errno.Error == Interop.Error.EACCES ||
                        errno.Error == Interop.Error.EPERM ||      // permissions might not allow renaming even if a copy would work
                        errno.Error == Interop.Error.EMLINK)       // too many hard links to the source file
                   {
                        // Destination file could not be renamed. Copy it.
                        CopyFile(destFullPath, destBackupFullPath, true);
                        if (Interop.Sys.Unlink(destBackupFullPath) != 0)
                        {
                            errno = Interop.Sys.GetLastErrorInfo();
                            if (errno.Error != Interop.Error.ENOENT)
                            {
                                throw Interop.GetExceptionForIoErrno(errno, destBackupFullPath);
                            }
                        }
                    }
                    else if (errno.Error != Interop.Error.ENOENT)
                    {
                        throw Interop.GetExceptionForIoErrno(errno, destBackupFullPath);
                    }
                }
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
            Interop.CheckIo(Interop.Sys.Rename(sourceFullPath, destFullPath, 1));
        }

        public static void MoveFile(string sourceFullPath, string destFullPath)
        {
            MoveFile(sourceFullPath, destFullPath, false);
        }

        public static void MoveFile(string sourceFullPath, string destFullPath, bool overwrite)
        {
            // The desired behavior for Move(source, dest) is to not overwrite the destination file
            // if it exists. Since rename(source, dest) will replace the file at 'dest' if it exists,
            // link/unlink are used instead. rename is used when the source path and dest path refer
            // to the same file (on the same device). This is important for case-insensitive
            // file systems (e.g. renaming a file in a way that just changes casing), so that we support
            // changing the casing in the naming of the file.

            // Note that the test is actually in native code, and the only thing managed code does
            // is check fo EXDEV and call CopyFile.
            if (Interop.Sys.Rename(sourceFullPath, destFullPath, overwrite ? 1 : 0) < 0)
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
                    // FileNotFoundException instead of DirectoryNotFoundexception.
                    throw Interop.GetExceptionForIoErrno(errorInfo, destFullPath,
                        isDirectory: errorInfo.Error == Interop.Error.ENOENT && !Directory.Exists(Path.GetDirectoryName(destFullPath))   // The parent directory of destFile can't be found
                        );
                }
            }
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
                        // github.com/dotnet/corefx/issues/21273
                        Interop.ErrorInfo fileExistsError;

                        // Input allows trailing separators in order to match Windows behavior
                        // Unix does not accept trailing separators, so must be trimmed
                        if (!FileExists(Path.TrimEndingDirectorySeparator(fullPath),
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
            Stack<string> stackDir = new Stack<string>();
            int lengthRoot = PathInternal.GetRootLength(fullPath);
            if (length > lengthRoot)
            {
                int i = length - 1;
                while (i >= lengthRoot && !somepathexists)
                {
                    if (!DirectoryExists(fullPath.AsSpan(0, i + 1))) // Create only the ones missing
                    {
                        stackDir.Push(fullPath.Substring(0, i + 1));
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

                if (Path.EndsInDirectorySeparator(sourceFullPath))
                    throw new IOException(SR.Format(SR.IO_PathNotFound_Path, sourceFullPath));

                // ... but it doesn't care if the destination has a trailing separator.
                destFullPath = Path.TrimEndingDirectorySeparator(destFullPath);
            }

            if (Interop.Sys.Rename(sourceFullPath, destFullPath, 2) < 0)
            {
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                switch (errorInfo.Error)
                {
                    case Interop.Error.EACCES: // match Win32 exception
                        throw new IOException(SR.Format(SR.UnauthorizedAccess_IODenied_Path, sourceFullPath), errorInfo.RawErrno);
                    case Interop.Error.EEXIST: // match Win32 exception
                        throw new IOException(SR.Format(SR.IO_AlreadyExists_Name, destFullPath));
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
    }
}
