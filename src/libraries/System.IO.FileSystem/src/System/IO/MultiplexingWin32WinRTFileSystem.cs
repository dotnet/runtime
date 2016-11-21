// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.IO
{
    internal class MultiplexingWin32WinRTFileSystem : FileSystem
    {
        private readonly FileSystem _win32FileSystem = new Win32FileSystem();
        private readonly FileSystem _winRTFileSystem = new WinRTFileSystem();

        internal static IFileSystemObject GetFileSystemObject(FileSystemInfo caller, string fullPath)
        {
            return ShouldUseWinRT(fullPath, isCreate: false) ?
                (IFileSystemObject)new WinRTFileSystem.WinRTFileSystemObject(fullPath, asDirectory: caller is DirectoryInfo) :
                (IFileSystemObject)caller;
        }

        public override int MaxPath { get { return Interop.Kernel32.MAX_PATH; } }
        public override int MaxDirectoryPath { get { return Interop.Kernel32.MAX_DIRECTORY_PATH; } }

        public override void CopyFile(string sourceFullPath, string destFullPath, bool overwrite)
        {
            Select(sourceFullPath, destFullPath).CopyFile(sourceFullPath, destFullPath, overwrite);
        }

        public override void ReplaceFile(string sourceFullPath, string destFullPath, string destBackupFullPath, bool ignoreMetadataErrors)
        {
            Select(sourceFullPath, destFullPath, destBackupFullPath).ReplaceFile(sourceFullPath, destFullPath, destBackupFullPath, ignoreMetadataErrors);
        }

        public override void CreateDirectory(string fullPath)
        {
            Select(fullPath, isCreate: true).CreateDirectory(fullPath);
        }

        public override void DeleteFile(string fullPath)
        {
            Select(fullPath).DeleteFile(fullPath);
        }

        public override bool DirectoryExists(string fullPath)
        {
            return Select(fullPath).DirectoryExists(fullPath);
        }

        public override Collections.Generic.IEnumerable<string> EnumeratePaths(string fullPath, string searchPattern, SearchOption searchOption, SearchTarget searchTarget)
        {
            return Select(fullPath).EnumeratePaths(fullPath, searchPattern, searchOption, searchTarget);
        }

        public override Collections.Generic.IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(string fullPath, string searchPattern, SearchOption searchOption, SearchTarget searchTarget)
        {
            return Select(fullPath).EnumerateFileSystemInfos(fullPath, searchPattern, searchOption, searchTarget);
        }

        public override bool FileExists(string fullPath)
        {
            return Select(fullPath).FileExists(fullPath);
        }

        public override FileAttributes GetAttributes(string fullPath)
        {
            return Select(fullPath).GetAttributes(fullPath);
        }

        public override DateTimeOffset GetCreationTime(string fullPath)
        {
            return Select(fullPath).GetCreationTime(fullPath);
        }

        public override string GetCurrentDirectory()
        {
            // WinRT honors the Win32 current directory, but does not expose it,
            // so we use the Win32 implementation always.
            return _win32FileSystem.GetCurrentDirectory();
        }

        public override IFileSystemObject GetFileSystemInfo(string fullPath, bool asDirectory)
        {
            return Select(fullPath).GetFileSystemInfo(fullPath, asDirectory);
        }

        public override DateTimeOffset GetLastAccessTime(string fullPath)
        {
            return Select(fullPath).GetLastAccessTime(fullPath);
        }

        public override DateTimeOffset GetLastWriteTime(string fullPath)
        {
            return Select(fullPath).GetLastWriteTime(fullPath);
        }

        public override void MoveDirectory(string sourceFullPath, string destFullPath)
        {
            Select(sourceFullPath, destFullPath).MoveDirectory(sourceFullPath, destFullPath);
        }

        public override void MoveFile(string sourceFullPath, string destFullPath)
        {
            Select(sourceFullPath, destFullPath).MoveFile(sourceFullPath, destFullPath);
        }

        public override FileStreamBase Open(string fullPath, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, FileStream parent)
        {
            bool isCreate = mode != FileMode.Open && mode != FileMode.Truncate;
            return Select(fullPath, isCreate).Open(fullPath, mode, access, share, bufferSize, options, parent);
        }

        public override void RemoveDirectory(string fullPath, bool recursive)
        {
            Select(fullPath).RemoveDirectory(fullPath, recursive);
        }

        public override void SetAttributes(string fullPath, FileAttributes attributes)
        {
            Select(fullPath).SetAttributes(fullPath, attributes);
        }

        public override void SetCreationTime(string fullPath, DateTimeOffset time, bool asDirectory)
        {
            Select(fullPath).SetCreationTime(fullPath, time, asDirectory);
        }

        public override void SetCurrentDirectory(string fullPath)
        {
            // WinRT honors the Win32 current directory, but does not expose it,
            // so we use the Win32 implementation always.
            // This will throw UnauthorizedAccess on brokered paths.
            _win32FileSystem.SetCurrentDirectory(fullPath);
        }

        public override void SetLastAccessTime(string fullPath, DateTimeOffset time, bool asDirectory)
        {
            Select(fullPath).SetLastAccessTime(fullPath, time, asDirectory);
        }

        public override void SetLastWriteTime(string fullPath, DateTimeOffset time, bool asDirectory)
        {
            Select(fullPath).SetLastWriteTime(fullPath, time, asDirectory);
        }

        private FileSystem Select(string fullPath, bool isCreate = false)
        {
            return ShouldUseWinRT(fullPath, isCreate) ? _winRTFileSystem : _win32FileSystem;
        }

        private FileSystem Select(string sourceFullPath, string destFullPath)
        {
            return (ShouldUseWinRT(sourceFullPath, isCreate: false) || ShouldUseWinRT(destFullPath, isCreate: true)) ? _winRTFileSystem : _win32FileSystem;
        }

        private FileSystem Select(string sourceFullPath, string destFullPath, string destFullBackupPath)
        {
            return 
                (ShouldUseWinRT(sourceFullPath, isCreate: false) || ShouldUseWinRT(destFullPath, isCreate: true) || ShouldUseWinRT(destFullBackupPath, isCreate: true)) ?
                _winRTFileSystem :
                _win32FileSystem;
        }

        public override string[] GetLogicalDrives()
        {
            // This API is always blocked on WinRT, don't use Win32
            return _winRTFileSystem.GetLogicalDrives();
        }

        private static bool ShouldUseWinRT(string fullPath, bool isCreate)
        {
            // The purpose of this method is to determine if we can access a path
            // via Win32 or if we need to fallback to WinRT.
            // We prefer Win32 since it is faster, WinRT's APIs eventually just
            // call into Win32 after all, but it doesn't provide access to,
            // brokered paths (like Pictures or Documents) nor does it handle 
            // placeholder files.  So we'd like to fall back to WinRT whenever
            // we can't access a path, or if it known to be a placeholder file.

            bool useWinRt = false;

            do
            {
                // first use GetFileAttributesEx as it is faster than FindFirstFile and requires minimum permissions
                Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data = new Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA();
                if (Interop.Kernel32.GetFileAttributesEx(fullPath, Interop.Kernel32.GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, ref data))
                {
                    // got the attributes
                    if ((data.fileAttributes & Interop.Kernel32.FileAttributes.FILE_ATTRIBUTE_DIRECTORY) != 0 ||
                        (data.fileAttributes & Interop.Kernel32.FileAttributes.FILE_ATTRIBUTE_REPARSE_POINT) == 0)
                    {
                        // we have a directory or a file that is not a reparse point
                        // useWinRt = false;
                        break;
                    }
                    else
                    {
                        // we need to get the find data to determine if it is a placeholder file
                        Interop.Kernel32.WIN32_FIND_DATA findData = new Interop.Kernel32.WIN32_FIND_DATA();
                        using (SafeFindHandle handle = Interop.Kernel32.FindFirstFile(fullPath, ref findData))
                        {
                            if (!handle.IsInvalid)
                            {
                                // got the find data, use WinRT for placeholder files

                                Debug.Assert((findData.dwFileAttributes & Interop.Kernel32.FileAttributes.FILE_ATTRIBUTE_DIRECTORY) == 0);
                                Debug.Assert((findData.dwFileAttributes & Interop.Kernel32.FileAttributes.FILE_ATTRIBUTE_REPARSE_POINT) != 0);

                                useWinRt = findData.dwReserved0 == Interop.Kernel32.IOReparseOptions.IO_REPARSE_TAG_FILE_PLACEHOLDER;
                                break;
                            }
                        }
                    }
                }

                int error = Marshal.GetLastWin32Error();
                Debug.Assert(error != Interop.Errors.ERROR_SUCCESS);

                if (error == Interop.Errors.ERROR_ACCESS_DENIED)
                {
                    // The path was not accessible with Win32, so try WinRT
                    useWinRt = true;
                    break;
                }
                else if (error != Interop.Errors.ERROR_PATH_NOT_FOUND && error != Interop.Errors.ERROR_FILE_NOT_FOUND)
                {
                    // We hit some error other than ACCESS_DENIED or NOT_FOUND,
                    // Default to Win32 to provide most accurate error behavior
                    break;
                }

                // error was ERROR_PATH_NOT_FOUND or ERROR_FILE_NOT_FOUND
                // if we are creating a file/directory we cannot assume that Win32 will have access to 
                // the parent directory, so we walk up the path.
                fullPath = PathHelpers.GetDirectoryNameInternal(fullPath);
                // only walk up the path if we are creating a file/directory and not at the root
            } while (isCreate && !String.IsNullOrEmpty(fullPath));

            return useWinRt;
        }
    }
}
