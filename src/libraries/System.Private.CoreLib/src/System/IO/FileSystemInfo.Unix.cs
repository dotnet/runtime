// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.IO
{
    public partial class FileSystemInfo
    {
        private readonly bool _asDirectory;
        private FileStatus _fileStatus;

        protected FileSystemInfo()
        {
            _asDirectory = this is DirectoryInfo;
        }

        internal static FileSystemInfo Create(string fullPath, string fileName, bool asDirectory, ref FileStatus fileStatus)
        {
            FileSystemInfo info = asDirectory
                ? new DirectoryInfo(fullPath, fileName: fileName, isNormalized: true)
                : new FileInfo(fullPath, fileName: fileName, isNormalized: true);

            Debug.Assert(!PathInternal.IsPartiallyQualified(fullPath), $"'{fullPath}' should be fully qualified when constructed from directory enumeration");

            info.Init(ref fileStatus);
            return info;
        }

        internal void InvalidateCore() => _fileStatus.InvalidateCaches();

        internal unsafe void Init(ref FileStatus fileStatus)
        {
            _fileStatus = fileStatus;
            _fileStatus.EnsureCachesInitialized(FullPath);
        }

        public FileAttributes Attributes
        {
            get => _fileStatus.GetAttributes(FullPath, Name);
            set => _fileStatus.SetAttributes(FullPath, value, _asDirectory);
        }

        internal bool ExistsCore => _fileStatus.GetExists(FullPath, _asDirectory);

        internal DateTimeOffset CreationTimeCore
        {
            get => _fileStatus.GetCreationTime(FullPath);
            set => _fileStatus.SetCreationTime(FullPath, value, _asDirectory);
        }

        internal DateTimeOffset LastAccessTimeCore
        {
            get => _fileStatus.GetLastAccessTime(FullPath);
            set => _fileStatus.SetLastAccessTime(FullPath, value, _asDirectory);
        }

        internal DateTimeOffset LastWriteTimeCore
        {
            get => _fileStatus.GetLastWriteTime(FullPath);
            set => _fileStatus.SetLastWriteTime(FullPath, value, _asDirectory);
        }

        internal long LengthCore => _fileStatus.GetLength(FullPath);

        internal UnixFileMode UnixFileModeCore
        {
            get => _fileStatus.GetUnixFileMode(FullPath);
            set => _fileStatus.SetUnixFileMode(FullPath, value);
        }

        public void Refresh()
        {
            _linkTargetIsValid = false;
            _fileStatus.RefreshCaches(FullPath);
        }

        // There is no special handling for Unix- see Windows code for the reason we do this
        internal string NormalizedPath => FullPath;
    }
}
