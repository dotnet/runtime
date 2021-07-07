// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.IO
{
    public partial class FileSystemInfo
    {
        private FileStatus _fileStatus;

        protected FileSystemInfo()
        {
            _fileStatus.InitiallyDirectory = this is DirectoryInfo;
            _fileStatus.InvalidateCaches();
        }

        internal static FileSystemInfo Create(string fullPath, string fileName, ref FileStatus fileStatus)
        {
            FileSystemInfo info = fileStatus.InitiallyDirectory
                ? (FileSystemInfo)new DirectoryInfo(fullPath, fileName: fileName, isNormalized: true)
                : new FileInfo(fullPath, fileName: fileName, isNormalized: true);

            Debug.Assert(!PathInternal.IsPartiallyQualified(fullPath), $"'{fullPath}' should be fully qualified when constructed from directory enumeration");

            info.Init(ref fileStatus);
            return info;
        }

        internal void Invalidate() => _fileStatus.InvalidateCaches();

        internal unsafe void Init(ref FileStatus fileStatus)
        {
            _fileStatus = fileStatus;
            _fileStatus.EnsureCachesInitialized(FullPath);
        }

        public FileAttributes Attributes
        {
            get => _fileStatus.GetAttributes(FullPath, Name);
            set => _fileStatus.SetAttributes(FullPath, value);
        }

        internal bool ExistsCore => _fileStatus.GetExists(FullPath);

        internal DateTimeOffset CreationTimeCore
        {
            get => _fileStatus.GetCreationTime(FullPath);
            set => _fileStatus.SetCreationTime(FullPath, value);
        }

        internal DateTimeOffset LastAccessTimeCore
        {
            get => _fileStatus.GetLastAccessTime(FullPath);
            set => _fileStatus.SetLastAccessTime(FullPath, value);
        }

        internal DateTimeOffset LastWriteTimeCore
        {
            get => _fileStatus.GetLastWriteTime(FullPath);
            set => _fileStatus.SetLastWriteTime(FullPath, value);
        }

        internal long LengthCore => _fileStatus.GetLength(FullPath);

        public void Refresh() => _fileStatus.RefreshCaches(FullPath);

        internal static void ThrowNotFound(string path)
        {
            // Windows distinguishes between whether the directory or the file isn't found,
            // and throws a different exception in these cases.  We attempt to approximate that
            // here; there is a race condition here, where something could change between
            // when the error occurs and our checks, but it's the best we can do, and the
            // worst case in such a race condition (which could occur if the file system is
            // being manipulated concurrently with these checks) is that we throw a
            // FileNotFoundException instead of DirectoryNotFoundException.

            bool directoryError = !Directory.Exists(Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(path)));
            throw Interop.GetExceptionForIoErrno(new Interop.ErrorInfo(Interop.Error.ENOENT), path, directoryError);
        }

        // There is no special handling for Unix- see Windows code for the reason we do this
        internal string NormalizedPath => FullPath;
    }
}
