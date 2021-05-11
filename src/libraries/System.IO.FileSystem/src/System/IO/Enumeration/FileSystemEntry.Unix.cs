// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.IO.Enumeration
{
    /// <summary>
    /// Lower level view of FileSystemInfo used for processing and filtering find results.
    /// </summary>
    public unsafe ref partial struct FileSystemEntry
   {
        internal Interop.Sys.DirectoryEntry _directoryEntry;
        private FileStatus _status;
        private Span<char> _pathBuffer;
        private ReadOnlySpan<char> _fullPath;
        private ReadOnlySpan<char> _fileName;
        private fixed char _fileNameBuffer[Interop.Sys.DirectoryEntry.NameBufferSize];

        internal static FileAttributes Initialize(
            ref FileSystemEntry entry,
            Interop.Sys.DirectoryEntry directoryEntry,
            ReadOnlySpan<char> directory,
            ReadOnlySpan<char> rootDirectory,
            ReadOnlySpan<char> originalRootDirectory,
            Span<char> pathBuffer)
        {
            entry._directoryEntry = directoryEntry;
            entry.Directory = directory;
            entry.RootDirectory = rootDirectory;
            entry.OriginalRootDirectory = originalRootDirectory;
            entry._pathBuffer = pathBuffer;
            entry._fullPath = ReadOnlySpan<char>.Empty;
            entry._fileName = ReadOnlySpan<char>.Empty;

            entry._status.InvalidateCaches();

            bool isDirectory = directoryEntry.InodeType == Interop.Sys.NodeType.DT_DIR;
            bool isSymlink   = directoryEntry.InodeType == Interop.Sys.NodeType.DT_LNK;
            bool isUnknown   = directoryEntry.InodeType == Interop.Sys.NodeType.DT_UNKNOWN;

            // Some operating systems don't have the inode type in the dirent structure,
            // so we use DT_UNKNOWN as a sentinel value. As such, check if the dirent is a
            // symlink or a directory.
            if (isUnknown)
            {
                isSymlink = entry.IsSymbolicLink;
                isDirectory = entry._status.IsDirectory(entry.FullPath);
            }
            // Same idea as the directory check, just repeated for (and tweaked due to the
            // nature of) symlinks.
            // Whether we had the dirent structure or not, we treat a symlink to a directory as a directory,
            // so we need to reflect that in our isDirectory variable.
            else if (isSymlink)
            {
                isDirectory = entry._status.IsDirectory(entry.FullPath);
            }

            entry._status.InitiallyDirectory = isDirectory;

            FileAttributes attributes = default;
            if (isSymlink)
                attributes |= FileAttributes.ReparsePoint;
            if (isDirectory)
                attributes |= FileAttributes.Directory;

            return attributes;
        }

        private ReadOnlySpan<char> FullPath
        {
            get
            {
                if (_fullPath.Length == 0)
                {
                    Debug.Assert(Directory.Length + FileName.Length < _pathBuffer.Length,
                        $"directory ({Directory.Length} chars) & name ({Directory.Length} chars) too long for buffer ({_pathBuffer.Length} chars)");
                    Path.TryJoin(Directory, FileName, _pathBuffer, out int charsWritten);
                    Debug.Assert(charsWritten > 0, "didn't write any chars to buffer");
                    _fullPath = _pathBuffer.Slice(0, charsWritten);
                }
                return _fullPath;
            }
        }

        public ReadOnlySpan<char> FileName
        {
            get
            {
                if (_directoryEntry.NameLength != 0 && _fileName.Length == 0)
                {
                    fixed (char* c = _fileNameBuffer)
                    {
                        Span<char> buffer = new Span<char>(c, Interop.Sys.DirectoryEntry.NameBufferSize);
                        _fileName = _directoryEntry.GetName(buffer);
                    }
                }

                return _fileName;
            }
        }

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

        // Windows never fails getting attributes, length, or time as that information comes back
        // with the native enumeration struct. As such we must not throw here.
        public FileAttributes Attributes => _status.GetAttributes(FullPath, FileName);
        public long Length => _status.GetLength(FullPath, continueOnError: true);
        public DateTimeOffset CreationTimeUtc => _status.GetCreationTime(FullPath, continueOnError: true);
        public DateTimeOffset LastAccessTimeUtc => _status.GetLastAccessTime(FullPath, continueOnError: true);
        public DateTimeOffset LastWriteTimeUtc => _status.GetLastWriteTime(FullPath, continueOnError: true);
        public bool IsDirectory => _status.InitiallyDirectory;
        public bool IsHidden => _status.IsHidden(FullPath, FileName);
        internal bool IsReadOnly => _status.IsReadOnly(FullPath);
        internal bool IsSymbolicLink => _status.IsSymbolicLink(FullPath);

        public FileSystemInfo ToFileSystemInfo()
        {
            string fullPath = ToFullPath();
            return FileSystemInfo.Create(fullPath, new string(FileName), ref _status);
        }

        /// <summary>
        /// Returns the full path of the find result.
        /// </summary>
        public string ToFullPath() =>
            new string(FullPath);
    }
}
