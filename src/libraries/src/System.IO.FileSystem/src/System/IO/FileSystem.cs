// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.IO
{
    internal abstract partial class FileSystem
    {
        public static FileSystem Current { get { return s_current; } }

        // Directory
        public abstract void CreateDirectory(string fullPath);
        public abstract bool DirectoryExists(string fullPath);
        public abstract void MoveDirectory(string sourceFullPath, string destFullPath);
        public abstract void RemoveDirectory(string fullPath, bool recursive);

        // File
        public abstract void CopyFile(string sourceFullPath, string destFullPath, bool overwrite);
        public abstract void ReplaceFile(string sourceFullPath, string destFullPath, string destBackupFullPath, bool ignoreMetadataErrors);
        public abstract void DeleteFile(string fullPath);
        public abstract bool FileExists(string fullPath);
        public abstract FileStream Open(string fullPath, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, FileStream parent);
        public abstract void MoveFile(string sourceFullPath, string destFullPath);

        public abstract FileAttributes GetAttributes(string fullPath);
        public abstract void SetAttributes(string fullPath, FileAttributes attributes);

        public abstract DateTimeOffset GetCreationTime(string fullPath);
        public abstract void SetCreationTime(string fullPath, DateTimeOffset time, bool asDirectory);
        public abstract DateTimeOffset GetLastAccessTime(string fullPath);
        public abstract void SetLastAccessTime(string fullPath, DateTimeOffset time, bool asDirectory);
        public abstract DateTimeOffset GetLastWriteTime(string fullPath);
        public abstract void SetLastWriteTime(string fullPath, DateTimeOffset time, bool asDirectory);

        // Discovery
        public abstract IFileSystemObject GetFileSystemInfo(string fullPath, bool asDirectory);
        public abstract IEnumerable<string> EnumeratePaths(string fullPath, string searchPattern, SearchOption searchOption, SearchTarget searchTarget);
        public abstract IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(string fullPath, string searchPattern, SearchOption searchOption, SearchTarget searchTarget);

        // Path
        public abstract string GetCurrentDirectory();
        public abstract void SetCurrentDirectory(string fullPath);
        public abstract int MaxPath { get; }
        public abstract int MaxDirectoryPath { get; }

        // Volume
        public abstract string[] GetLogicalDrives();
    }
}
