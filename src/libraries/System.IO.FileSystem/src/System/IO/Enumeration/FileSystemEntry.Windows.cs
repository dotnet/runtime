// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

#if MS_IO_REDIST
namespace Microsoft.IO.Enumeration
#else
namespace System.IO.Enumeration
#endif
{
    /// <summary>
    /// Lower level view of FileSystemInfo used for processing and filtering find results.
    /// </summary>
    public unsafe ref partial struct FileSystemEntry
    {
        internal static void Initialize(
            ref FileSystemEntry entry,
            Interop.NtDll.FILE_FULL_DIR_INFORMATION* info,
            ReadOnlySpan<char> directory,
            ReadOnlySpan<char> rootDirectory,
            ReadOnlySpan<char> originalRootDirectory)
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
        public ReadOnlySpan<char> RootDirectory { get; private set; }

        /// <summary>
        /// The root directory for the enumeration as specified in the constructor.
        /// </summary>
        public ReadOnlySpan<char> OriginalRootDirectory { get; private set; }

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

        /// <summary>
        /// Returns true if the file has the hidden attribute.
        /// </summary>
        public bool IsHidden => (Attributes & FileAttributes.Hidden) != 0;

        public FileSystemInfo ToFileSystemInfo()
            => FileSystemInfo.Create(Path.Join(Directory, FileName), ref this);

        /// <summary>
        /// Returns the full path of the find result.
        /// </summary>
        public string ToFullPath() =>
            Path.Join(Directory, FileName);
    }
}
