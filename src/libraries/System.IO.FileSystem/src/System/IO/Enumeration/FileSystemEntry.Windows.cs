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
        internal static void Initialize(
            ref FileSystemEntry entry,
            Interop.NtDll.FILE_FULL_DIR_INFORMATION* info,
            ReadOnlySpan<char> directory,
            string rootDirectory,
            string originalRootDirectory)
        {
            entry._info = info;
            entry.Directory = directory;
            entry.RootDirectory = rootDirectory;
            entry.OriginalRootDirectory = originalRootDirectory;
        }

        internal unsafe Interop.NtDll.FILE_FULL_DIR_INFORMATION* _info;

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

        /// <summary>
        /// The file name for this entry.
        /// </summary>
        public ReadOnlySpan<char> FileName => _info->FileName;

        /// <summary>
        /// The attributes for this entry.
        /// </summary>
        public FileAttributes Attributes => _info->FileAttributes;

        /// <summary>
        /// The length of file in bytes.
        /// </summary>
        public long Length => _info->EndOfFile;

        /// <summary>
        /// The creation time for the entry or the oldest available time stamp if the
        /// operating system does not support creation time stamps.
        /// </summary>
        public DateTimeOffset CreationTimeUtc => _info->CreationTime.ToDateTimeOffset();
        public DateTimeOffset LastAccessTimeUtc => _info->LastAccessTime.ToDateTimeOffset();
        public DateTimeOffset LastWriteTimeUtc => _info->LastWriteTime.ToDateTimeOffset();

        /// <summary>
        /// Returns true if this entry is a directory.
        /// </summary>
        public bool IsDirectory => (Attributes & FileAttributes.Directory) != 0;

        public FileSystemInfo ToFileSystemInfo()
            => FileSystemInfo.Create(PathHelpers.CombineNoChecks(Directory, FileName), ref this);

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
