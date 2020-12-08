// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;

#if MS_IO_REDIST
namespace Microsoft.IO
#else
namespace System.IO
#endif
{
    internal static partial class FileSystem
    {
        public static void CopyFile(string sourceFullPath, string destFullPath, bool overwrite)
        {
            int errorCode = Interop.Kernel32.CopyFile(sourceFullPath, destFullPath, !overwrite);

            if (errorCode != Interop.Errors.ERROR_SUCCESS)
            {
                string fileName = destFullPath;

                if (errorCode != Interop.Errors.ERROR_FILE_EXISTS)
                {
                    // For a number of error codes (sharing violation, path not found, etc) we don't know if the problem was with
                    // the source or dest file.  Try reading the source file.
                    using (SafeFileHandle handle = Interop.Kernel32.CreateFile(sourceFullPath, Interop.Kernel32.GenericOperations.GENERIC_READ, FileShare.Read, FileMode.Open, 0))
                    {
                        if (handle.IsInvalid)
                            fileName = sourceFullPath;
                    }

                    if (errorCode == Interop.Errors.ERROR_ACCESS_DENIED)
                    {
                        if (DirectoryExists(destFullPath))
                            throw new IOException(SR.Format(SR.Arg_FileIsDirectory_Name, destFullPath), Interop.Errors.ERROR_ACCESS_DENIED);
                    }
                }

                throw Win32Marshal.GetExceptionForWin32Error(errorCode, fileName);
            }
        }

        public static void ReplaceFile(string sourceFullPath, string destFullPath, string? destBackupFullPath, bool ignoreMetadataErrors)
        {
            int flags = ignoreMetadataErrors ? Interop.Kernel32.REPLACEFILE_IGNORE_MERGE_ERRORS : 0;

            if (!Interop.Kernel32.ReplaceFile(destFullPath, sourceFullPath, destBackupFullPath, flags, IntPtr.Zero, IntPtr.Zero))
            {
                throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
            }
        }

        public static void DeleteFile(string fullPath)
        {
            bool r = Interop.Kernel32.DeleteFile(fullPath);
            if (!r)
            {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode == Interop.Errors.ERROR_FILE_NOT_FOUND)
                    return;
                else
                    throw Win32Marshal.GetExceptionForWin32Error(errorCode, fullPath);
            }
        }

        public static FileAttributes GetAttributes(string fullPath)
        {
            Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data = default;
            int errorCode = FillAttributeInfo(fullPath, ref data, returnErrorOnNotFound: true);
            if (errorCode != 0)
                throw Win32Marshal.GetExceptionForWin32Error(errorCode, fullPath);

            return (FileAttributes)data.dwFileAttributes;
        }

        public static DateTimeOffset GetCreationTime(string fullPath)
        {
            Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data = default;
            int errorCode = FillAttributeInfo(fullPath, ref data, returnErrorOnNotFound: false);
            if (errorCode != 0)
                throw Win32Marshal.GetExceptionForWin32Error(errorCode, fullPath);

            return data.ftCreationTime.ToDateTimeOffset();
        }

        public static FileSystemInfo GetFileSystemInfo(string fullPath, bool asDirectory)
        {
            return asDirectory ?
                (FileSystemInfo)new DirectoryInfo(fullPath, null) :
                (FileSystemInfo)new FileInfo(fullPath, null);
        }

        public static DateTimeOffset GetLastAccessTime(string fullPath)
        {
            Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data = default;
            int errorCode = FillAttributeInfo(fullPath, ref data, returnErrorOnNotFound: false);
            if (errorCode != 0)
                throw Win32Marshal.GetExceptionForWin32Error(errorCode, fullPath);

            return data.ftLastAccessTime.ToDateTimeOffset();
        }

        public static DateTimeOffset GetLastWriteTime(string fullPath)
        {
            Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data = default;
            int errorCode = FillAttributeInfo(fullPath, ref data, returnErrorOnNotFound: false);
            if (errorCode != 0)
                throw Win32Marshal.GetExceptionForWin32Error(errorCode, fullPath);

            return data.ftLastWriteTime.ToDateTimeOffset();
        }

        public static void MoveDirectory(string sourceFullPath, string destFullPath)
        {
            if (!Interop.Kernel32.MoveFile(sourceFullPath, destFullPath, overwrite: false))
            {
                int errorCode = Marshal.GetLastWin32Error();

                if (errorCode == Interop.Errors.ERROR_FILE_NOT_FOUND)
                    throw Win32Marshal.GetExceptionForWin32Error(Interop.Errors.ERROR_PATH_NOT_FOUND, sourceFullPath);

                if (errorCode == Interop.Errors.ERROR_ALREADY_EXISTS)
                    throw Win32Marshal.GetExceptionForWin32Error(Interop.Errors.ERROR_ALREADY_EXISTS, destFullPath);

                // This check was originally put in for Win9x (unfortunately without special casing it to be for Win9x only). We can't change the NT codepath now for backcomp reasons.
                if (errorCode == Interop.Errors.ERROR_ACCESS_DENIED) // WinNT throws IOException. This check is for Win9x. We can't change it for backcomp.
                    throw new IOException(SR.Format(SR.UnauthorizedAccess_IODenied_Path, sourceFullPath), Win32Marshal.MakeHRFromErrorCode(errorCode));

                throw Win32Marshal.GetExceptionForWin32Error(errorCode);
            }
        }

        public static void MoveFile(string sourceFullPath, string destFullPath, bool overwrite)
        {
            if (!Interop.Kernel32.MoveFile(sourceFullPath, destFullPath, overwrite))
            {
                throw Win32Marshal.GetExceptionForLastWin32Error();
            }
        }

        private static SafeFileHandle OpenHandle(string fullPath, bool asDirectory)
        {
            string root = fullPath.Substring(0, PathInternal.GetRootLength(fullPath.AsSpan()));
            if (root == fullPath && root[1] == Path.VolumeSeparatorChar)
            {
                // intentionally not fullpath, most upstack public APIs expose this as path.
                throw new ArgumentException(SR.Arg_PathIsVolume, "path");
            }

            SafeFileHandle handle = Interop.Kernel32.CreateFile(
                fullPath,
                Interop.Kernel32.GenericOperations.GENERIC_WRITE,
                FileShare.ReadWrite | FileShare.Delete,
                FileMode.Open,
                asDirectory ? Interop.Kernel32.FileOperations.FILE_FLAG_BACKUP_SEMANTICS : 0);

            if (handle.IsInvalid)
            {
                int errorCode = Marshal.GetLastWin32Error();

                // NT5 oddity - when trying to open "C:\" as a File,
                // we usually get ERROR_PATH_NOT_FOUND from the OS.  We should
                // probably be consistent w/ every other directory.
                if (!asDirectory && errorCode == Interop.Errors.ERROR_PATH_NOT_FOUND && fullPath.Equals(Directory.GetDirectoryRoot(fullPath)))
                    errorCode = Interop.Errors.ERROR_ACCESS_DENIED;

                throw Win32Marshal.GetExceptionForWin32Error(errorCode, fullPath);
            }

            return handle;
        }

        public static void RemoveDirectory(string fullPath, bool recursive)
        {
            if (!recursive)
            {
                RemoveDirectoryInternal(fullPath, topLevel: true);
                return;
            }

            Interop.Kernel32.WIN32_FIND_DATA findData = default;
            GetFindData(fullPath, ref findData);
            if (IsNameSurrogateReparsePoint(ref findData))
            {
                // Don't recurse
                RemoveDirectoryInternal(fullPath, topLevel: true);
                return;
            }

            // We want extended syntax so we can delete "extended" subdirectories and files
            // (most notably ones with trailing whitespace or periods)
            fullPath = PathInternal.EnsureExtendedPrefix(fullPath);
            RemoveDirectoryRecursive(fullPath, ref findData, topLevel: true);
        }

        private static void GetFindData(string fullPath, ref Interop.Kernel32.WIN32_FIND_DATA findData)
        {
            using (SafeFindHandle handle = Interop.Kernel32.FindFirstFile(Path.TrimEndingDirectorySeparator(fullPath), ref findData))
            {
                if (handle.IsInvalid)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    // File not found doesn't make much sense coming from a directory delete.
                    if (errorCode == Interop.Errors.ERROR_FILE_NOT_FOUND)
                        errorCode = Interop.Errors.ERROR_PATH_NOT_FOUND;
                    throw Win32Marshal.GetExceptionForWin32Error(errorCode, fullPath);
                }
            }
        }

        private static bool IsNameSurrogateReparsePoint(ref Interop.Kernel32.WIN32_FIND_DATA data)
        {
            // Name surrogates are reparse points that point to other named entities local to the file system.
            // Reparse points can be used for other types of files, notably OneDrive placeholder files. We
            // should treat reparse points that are not name surrogates as any other directory, e.g. recurse
            // into them. Surrogates should just be detached.
            //
            // See
            // https://github.com/dotnet/runtime/issues/23646
            // https://msdn.microsoft.com/en-us/library/windows/desktop/aa365511.aspx
            // https://msdn.microsoft.com/en-us/library/windows/desktop/aa365197.aspx

            return ((FileAttributes)data.dwFileAttributes & FileAttributes.ReparsePoint) != 0
                && (data.dwReserved0 & 0x20000000) != 0; // IsReparseTagNameSurrogate
        }

        private static void RemoveDirectoryRecursive(string fullPath, ref Interop.Kernel32.WIN32_FIND_DATA findData, bool topLevel)
        {
            int errorCode;
            Exception? exception = null;

            using (SafeFindHandle handle = Interop.Kernel32.FindFirstFile(Path.Join(fullPath, "*"), ref findData))
            {
                if (handle.IsInvalid)
                    throw Win32Marshal.GetExceptionForLastWin32Error(fullPath);

                do
                {
                    if ((findData.dwFileAttributes & Interop.Kernel32.FileAttributes.FILE_ATTRIBUTE_DIRECTORY) == 0)
                    {
                        // File
                        string fileName = findData.cFileName.GetStringFromFixedBuffer();
                        if (!Interop.Kernel32.DeleteFile(Path.Combine(fullPath, fileName)) && exception == null)
                        {
                            errorCode = Marshal.GetLastWin32Error();

                            // We don't care if something else deleted the file first
                            if (errorCode != Interop.Errors.ERROR_FILE_NOT_FOUND)
                            {
                                exception = Win32Marshal.GetExceptionForWin32Error(errorCode, fileName);
                            }
                        }
                    }
                    else
                    {
                        // Directory, skip ".", "..".
                        if (findData.cFileName.FixedBufferEqualsString(".") || findData.cFileName.FixedBufferEqualsString(".."))
                            continue;

                        string fileName = findData.cFileName.GetStringFromFixedBuffer();

                        if (!IsNameSurrogateReparsePoint(ref findData))
                        {
                            // Not a reparse point, or the reparse point isn't a name surrogate, recurse.
                            try
                            {
                                RemoveDirectoryRecursive(
                                    Path.Combine(fullPath, fileName),
                                    findData: ref findData,
                                    topLevel: false);
                            }
                            catch (Exception e)
                            {
                                if (exception == null)
                                    exception = e;
                            }
                        }
                        else
                        {
                            // Name surrogate reparse point, don't recurse, simply remove the directory.
                            // If a mount point, we have to delete the mount point first.
                            if (findData.dwReserved0 == Interop.Kernel32.IOReparseOptions.IO_REPARSE_TAG_MOUNT_POINT)
                            {
                                // Mount point. Unmount using full path plus a trailing '\'.
                                // (Note: This doesn't remove the underlying directory)
                                string mountPoint = Path.Join(fullPath, fileName, PathInternal.DirectorySeparatorCharAsString);
                                if (!Interop.Kernel32.DeleteVolumeMountPoint(mountPoint) && exception == null)
                                {
                                    errorCode = Marshal.GetLastWin32Error();
                                    if (errorCode != Interop.Errors.ERROR_SUCCESS &&
                                        errorCode != Interop.Errors.ERROR_PATH_NOT_FOUND)
                                    {
                                        exception = Win32Marshal.GetExceptionForWin32Error(errorCode, fileName);
                                    }
                                }
                            }

                            // Note that RemoveDirectory on a symbolic link will remove the link itself.
                            if (!Interop.Kernel32.RemoveDirectory(Path.Combine(fullPath, fileName)) && exception == null)
                            {
                                errorCode = Marshal.GetLastWin32Error();
                                if (errorCode != Interop.Errors.ERROR_PATH_NOT_FOUND)
                                {
                                    exception = Win32Marshal.GetExceptionForWin32Error(errorCode, fileName);
                                }
                            }
                        }
                    }
                } while (Interop.Kernel32.FindNextFile(handle, ref findData));

                if (exception != null)
                    throw exception;

                errorCode = Marshal.GetLastWin32Error();
                if (errorCode != Interop.Errors.ERROR_SUCCESS && errorCode != Interop.Errors.ERROR_NO_MORE_FILES)
                    throw Win32Marshal.GetExceptionForWin32Error(errorCode, fullPath);
            }

            // As we successfully removed all of the files we shouldn't care about the directory itself
            // not being empty. As file deletion is just a marker to remove the file when all handles
            // are closed we could still have undeleted contents.
            RemoveDirectoryInternal(fullPath, topLevel: topLevel, allowDirectoryNotEmpty: true);
        }

        private static void RemoveDirectoryInternal(string fullPath, bool topLevel, bool allowDirectoryNotEmpty = false)
        {
            if (!Interop.Kernel32.RemoveDirectory(fullPath))
            {
                int errorCode = Marshal.GetLastWin32Error();
                switch (errorCode)
                {
                    case Interop.Errors.ERROR_FILE_NOT_FOUND:
                        // File not found doesn't make much sense coming from a directory delete.
                        errorCode = Interop.Errors.ERROR_PATH_NOT_FOUND;
                        goto case Interop.Errors.ERROR_PATH_NOT_FOUND;
                    case Interop.Errors.ERROR_PATH_NOT_FOUND:
                        // We only throw for the top level directory not found, not for any contents.
                        if (!topLevel)
                            return;
                        break;
                    case Interop.Errors.ERROR_DIR_NOT_EMPTY:
                        if (allowDirectoryNotEmpty)
                            return;
                        break;
                    case Interop.Errors.ERROR_ACCESS_DENIED:
                        // This conversion was originally put in for Win9x. Keeping for compatibility.
                        throw new IOException(SR.Format(SR.UnauthorizedAccess_IODenied_Path, fullPath));
                }

                throw Win32Marshal.GetExceptionForWin32Error(errorCode, fullPath);
            }
        }

        public static void SetAttributes(string fullPath, FileAttributes attributes)
        {
            if (!Interop.Kernel32.SetFileAttributes(fullPath, (int)attributes))
            {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode == Interop.Errors.ERROR_INVALID_PARAMETER)
                    throw new ArgumentException(SR.Arg_InvalidFileAttrs, nameof(attributes));
                throw Win32Marshal.GetExceptionForWin32Error(errorCode, fullPath);
            }
        }

        // Default values indicate "no change".  Use defaults so that we don't force callsites to be aware of the default values
        private static unsafe void SetFileTime(
            string fullPath,
            bool asDirectory,
            long creationTime = -1,
            long lastAccessTime = -1,
            long lastWriteTime = -1,
            long changeTime = -1,
            uint fileAttributes = 0)
        {
            using (SafeFileHandle handle = OpenHandle(fullPath, asDirectory))
            {
                var basicInfo = new Interop.Kernel32.FILE_BASIC_INFO()
                {
                    CreationTime = creationTime,
                    LastAccessTime = lastAccessTime,
                    LastWriteTime = lastWriteTime,
                    ChangeTime = changeTime,
                    FileAttributes = fileAttributes
                };

                if (!Interop.Kernel32.SetFileInformationByHandle(handle, Interop.Kernel32.FileBasicInfo, &basicInfo, (uint)sizeof(Interop.Kernel32.FILE_BASIC_INFO)))
                {
                    throw Win32Marshal.GetExceptionForLastWin32Error(fullPath);
                }
            }
        }

        public static void SetCreationTime(string fullPath, DateTimeOffset time, bool asDirectory)
           => SetFileTime(fullPath, asDirectory, creationTime: time.ToFileTime());

        public static void SetLastAccessTime(string fullPath, DateTimeOffset time, bool asDirectory)
           => SetFileTime(fullPath, asDirectory, lastAccessTime: time.ToFileTime());

        public static void SetLastWriteTime(string fullPath, DateTimeOffset time, bool asDirectory)
           => SetFileTime(fullPath, asDirectory, lastWriteTime: time.ToFileTime());

        public static string[] GetLogicalDrives()
            => DriveInfoInternal.GetLogicalDrives();
    }
}
