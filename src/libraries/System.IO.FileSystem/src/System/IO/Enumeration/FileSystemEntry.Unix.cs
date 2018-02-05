// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Enumeration
{
    /// <summary>
    /// Lower level view of FileSystemInfo used for processing and filtering find results.
    /// </summary>
    public unsafe ref struct FileSystemEntry
    {
        // TODO: Unix implementation https://github.com/dotnet/corefx/issues/26715
        // Inital implementation is naive and not optimized.

        internal static void Initialize(
            ref FileSystemEntry entry,
            Interop.Sys.DirectoryEntry directoryEntry,
            bool isDirectory,
            ReadOnlySpan<char> directory,
            string rootDirectory,
            string originalRootDirectory)
        {
            entry._directoryEntry = directoryEntry;
            entry._isDirectory = isDirectory;
            entry.Directory = directory;
            entry.RootDirectory = rootDirectory;
            entry.OriginalRootDirectory = originalRootDirectory;
        }

        internal Interop.Sys.DirectoryEntry _directoryEntry;
        private FileSystemInfo _info;
        private bool _isDirectory;

        private FileSystemInfo Info
        {
            get
            {
                if (_info == null)
                {
                    string fullPath = PathHelpers.CombineNoChecks(Directory, _directoryEntry.InodeName);
                    _info = _isDirectory
                        ? (FileSystemInfo) new DirectoryInfo(fullPath, fullPath, _directoryEntry.InodeName, isNormalized: true)
                        : new FileInfo(fullPath, fullPath, _directoryEntry.InodeName, isNormalized: true);
                    _info.Refresh();
                }
                return _info;
            }
        }

        /// <summary>
        /// The full path of the directory this entry resides in.
        /// </summary>
        public ReadOnlySpan<char> Directory { get; private set; }

        /// <summary>
        /// The full path of the root directory used for the enumeration.
        /// </summary>
        public string RootDirectory { get; private set; }

        /// <summary>
        /// The root directory for the enumeration as specified in the constructor.
        /// </summary>
        public string OriginalRootDirectory { get; private set; }

        public ReadOnlySpan<char> FileName => _directoryEntry.InodeName;
        public FileAttributes Attributes => Info.Attributes;
        public long Length => Info.LengthCore;
        public DateTimeOffset CreationTimeUtc => Info.CreationTimeCore;
        public DateTimeOffset LastAccessTimeUtc => Info.LastAccessTimeCore;
        public DateTimeOffset LastWriteTimeUtc => Info.LastWriteTimeCore;
        public bool IsDirectory => _isDirectory;
        public FileSystemInfo ToFileSystemInfo() => Info;

        /// <summary>
        /// Returns the full path for find results, based on the initially provided path.
        /// </summary>
        public string ToSpecifiedFullPath() =>
            PathHelpers.CombineNoChecks(OriginalRootDirectory, Directory.Slice(RootDirectory.Length), FileName);

        /// <summary>
        /// Returns the full path of the find result.
        /// </summary>
        public string ToFullPath() =>
            PathHelpers.CombineNoChecks(Directory, FileName);
    }
}
