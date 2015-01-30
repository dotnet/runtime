// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace System.IO
{
    internal class MultiplexingWin32WinRTFileSystem : FileSystem
    {
        private readonly FileSystem _win32FileSystem = new Win32FileSystem();
        private readonly FileSystem _winRTFileSystem = new WinRTFileSystem();

        public override int MaxPath { get { return Interop.MAX_PATH; } }
        public override int MaxDirectoryPath { get { return Interop.MAX_DIRECTORY_PATH; } }

        public override void CopyFile(string sourceFullPath, string destFullPath, bool overwrite)
        {
            Select(sourceFullPath, destFullPath).CopyFile(sourceFullPath, destFullPath, overwrite);
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
            return Select(fullPath, isCreate: true).Open(fullPath, mode, access, share, bufferSize, options, parent);
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

        private bool ShouldUseWinRT(string fullPath, bool isCreate)
        {
            bool useWinRt = false;

            do
            {
                Interop.WIN32_FIND_DATA findData = new Interop.WIN32_FIND_DATA();
                using (SafeFindHandle handle = Interop.mincore.FindFirstFile(fullPath, ref findData))
                {
                    int error = Marshal.GetLastWin32Error();

                    if (handle.IsInvalid)
                    {
                        if (error == Interop.ERROR_ACCESS_DENIED)
                        {
                            // The path was not accessible with Win32, so try WinRT
                            useWinRt = true;
                            break;
                        }
                        else if (error != Interop.ERROR_PATH_NOT_FOUND && error != Interop.ERROR_FILE_NOT_FOUND)
                        {
                            // We hit some error other than ACCESS_DENIED or NOT_FOUND,
                            // Default to Win32 to provide most accurate error behavior
                            break;
                        }
                    }
                    else
                    {
                        // Use WinRT for placeholder files
                        useWinRt = IsPlaceholderFile(findData);
                        break;
                    }
                }

                // error was ERROR_PATH_NOT_FOUND or ERROR_FILE_NOT_FOUND
                // if we are creating a file/directory we cannot assume that Win32 will have access to 
                // the parent directory, so we walk up the path.
                fullPath = PathHelpers.GetDirectoryNameInternal(fullPath);
                // only walk up the path if we are creating a file/directory and not at the root
            } while (isCreate && !String.IsNullOrEmpty(fullPath));

            return useWinRt;
        }

        private static bool IsPlaceholderFile(Interop.WIN32_FIND_DATA findData)
        {
            return (findData.dwFileAttributes & Interop.FILE_ATTRIBUTE_DIRECTORY) == 0 &&
                   (findData.dwFileAttributes & Interop.FILE_ATTRIBUTE_REPARSE_POINT) != 0 &&
                   (findData.dwReserved0 == Interop.IO_REPARSE_TAG_FILE_PLACEHOLDER);
        }
    }
}
