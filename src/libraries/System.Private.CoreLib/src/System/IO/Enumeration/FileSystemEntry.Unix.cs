// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.IO.Enumeration
{
    /// <summary>
    /// Lower level view of FileSystemInfo used for processing and filtering find results.
    /// </summary>
    public unsafe ref partial struct FileSystemEntry
    {
        private Interop.Sys.DirectoryEntry _directoryEntry;
        private bool _isDirectory;
        private FileStatus _status;
        private Span<char> _pathBuffer;
        private ReadOnlySpan<char> _fullPath;
        private ReadOnlySpan<char> _fileName;
        private FileNameBuffer _fileNameBuffer;

        // Wrap the fixed buffer to workaround visibility issues in api compat verification
        private struct FileNameBuffer
        {
            internal fixed char _buffer[Interop.Sys.DirectoryEntry.NameBufferSize];
        }

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
            entry._isDirectory = false;
            entry._status.InvalidateCaches();

            bool isDirectory = directoryEntry.InodeType == Interop.Sys.NodeType.DT_DIR;
            bool isSymlink   = directoryEntry.InodeType == Interop.Sys.NodeType.DT_LNK;
            bool isUnknown   = directoryEntry.InodeType == Interop.Sys.NodeType.DT_UNKNOWN;

            if (isDirectory)
            {
                entry._isDirectory = true;
            }
            else if (isSymlink)
            {
                entry._isDirectory = entry._status.IsDirectory(entry.FullPath, continueOnError: true);
            }
            else if (isUnknown)
            {
                entry._isDirectory = entry._status.IsDirectory(entry.FullPath, continueOnError: true);
                if (entry._status.IsSymbolicLink(entry.FullPath, continueOnError: true))
                {
                    entry._directoryEntry.InodeType = Interop.Sys.NodeType.DT_LNK;
                }
            }

            FileAttributes attributes = default;
            if (entry.IsSymbolicLink)
                attributes |= FileAttributes.ReparsePoint;
            if (entry.IsDirectory)
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
                    Span<char> buffer = MemoryMarshal.CreateSpan(ref _fileNameBuffer._buffer[0], Interop.Sys.DirectoryEntry.NameBufferSize);
                    _fileName = _directoryEntry.GetName(buffer);
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
        public FileAttributes Attributes
        {
            get
            {
                FileAttributes attributes = _status.GetAttributes(FullPath, FileName, continueOnError: true);
                if (attributes != (FileAttributes)(-1))
                {
                    return attributes;
                }

                // File was removed before we retrieved attributes.
                // Return what we know.
                attributes = default;

                if (IsSymbolicLink)
                    attributes |= FileAttributes.ReparsePoint;

                if (IsDirectory)
                    attributes |= FileAttributes.Directory;

                if (FileStatus.IsNameHidden(FileName))
                    attributes |= FileAttributes.Hidden;

                return attributes != default ? attributes : FileAttributes.Normal;
            }
        }
        public long Length => _status.GetLength(FullPath, continueOnError: true);
        public DateTimeOffset CreationTimeUtc => _status.GetCreationTime(FullPath, continueOnError: true);
        public DateTimeOffset LastAccessTimeUtc => _status.GetLastAccessTime(FullPath, continueOnError: true);
        public DateTimeOffset LastWriteTimeUtc => _status.GetLastWriteTime(FullPath, continueOnError: true);

        public bool IsHidden => _status.IsFileSystemEntryHidden(FullPath, FileName);
        internal bool IsReadOnly => _status.IsReadOnly(FullPath, continueOnError: true);

        public bool IsDirectory => _isDirectory;
        internal bool IsSymbolicLink => _directoryEntry.InodeType == Interop.Sys.NodeType.DT_LNK;

        public FileSystemInfo ToFileSystemInfo()
        {
            string fullPath = ToFullPath();
            return FileSystemInfo.Create(fullPath, new string(FileName), _isDirectory, ref _status);
        }

        /// <summary>
        /// Returns the full path of the find result.
        /// </summary>
        public string ToFullPath() =>
            new string(FullPath);
    }
}
