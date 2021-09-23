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
    /// <summary>Provides a lower level view of <see cref="System.IO.FileSystemInfo" /> to help process and filter find results.</summary>
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

        /// <summary>Gets the full path of the directory this entry resides in.</summary>
        /// <value>The full path of this entry's directory.</value>
        public ReadOnlySpan<char> Directory { get; private set; }

        /// <summary>Gets the full path of the root directory used for the enumeration.</summary>
        /// <value>The root directory.</value>
        public ReadOnlySpan<char> RootDirectory { get; private set; }

        /// <summary>Gets the root directory for the enumeration as specified in the constructor.</summary>
        /// <value>The original root directory.</value>
        public ReadOnlySpan<char> OriginalRootDirectory { get; private set; }

        /// <summary>Gets the file name for this entry.</summary>
        /// <value>This entry's file name.</value>
        public ReadOnlySpan<char> FileName => _info->FileName;

        /// <summary>Gets the attributes for this entry.</summary>
        /// <value>The attributes for this entry.</value>
        public FileAttributes Attributes => _info->FileAttributes;

        /// <summary>Gets the length of the file, in bytes.</summary>
        /// <value>The file length in bytes.</value>
        public long Length => _info->EndOfFile;

        /// <summary>Gets the creation time for the entry or the oldest available time stamp if the operating system does not support creation time stamps.</summary>
        /// <value>The creation time for the entry.</value>
        public DateTimeOffset CreationTimeUtc => _info->CreationTime.ToDateTimeOffset();
        /// <summary>Gets a datetime offset that represents the last access time in UTC.</summary>
        /// <value>The last access time in UTC.</value>
        public DateTimeOffset LastAccessTimeUtc => _info->LastAccessTime.ToDateTimeOffset();
        /// <summary>Gets a datetime offset that represents the last write time in UTC.</summary>
        /// <value>The last write time in UTC.</value>
        public DateTimeOffset LastWriteTimeUtc => _info->LastWriteTime.ToDateTimeOffset();

        /// <summary>Gets a value that indicates whether this entry is a directory.</summary>
        /// <value><see langword="true" /> if the entry is a directory; otherwise, <see langword="false" />.</value>
        public bool IsDirectory => (Attributes & FileAttributes.Directory) != 0;

        /// <summary>Gets a value that indicates whether the file has the hidden attribute.</summary>
        /// <value><see langword="true" /> if the file has the hidden attribute; otherwise, <see langword="false" />.</value>
        public bool IsHidden => (Attributes & FileAttributes.Hidden) != 0;

        /// <summary>Converts the value of this instance to a <see cref="System.IO.FileSystemInfo" />.</summary>
        /// <returns>The value of this instance as a <see cref="System.IO.FileSystemInfo" />.</returns>
        public FileSystemInfo ToFileSystemInfo()
            => FileSystemInfo.Create(Path.Join(Directory, FileName), ref this);

        /// <summary>Returns the full path of the find result.</summary>
        /// <returns>A string representing the full path.</returns>
        public string ToFullPath() =>
            Path.Join(Directory, FileName);
    }
}
