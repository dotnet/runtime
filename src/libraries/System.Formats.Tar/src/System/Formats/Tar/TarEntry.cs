// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.Formats.Tar
{
    /// <summary>
    /// Abstract class that represents a tar entry from an archive.
    /// </summary>
    /// <remarks>All the properties exposed by this class are supported by the <see cref="TarFormat.V7"/>, <see cref="TarFormat.Ustar"/>, <see cref="TarFormat.Pax"/> and <see cref="TarFormat.Gnu"/> formats.</remarks>
    public abstract partial class TarEntry
    {
        internal TarHeader _header;
        // Used to access the data section of this entry in an unseekable file
        private TarReader? _readerOfOrigin;

        // Constructor used when reading an existing archive.
        internal TarEntry(TarHeader header, TarReader readerOfOrigin)
        {
            _header = header;
            _readerOfOrigin = readerOfOrigin;
        }

        // Constructor called when creating a new 'TarEntry*' instance that can be passed to a TarWriter.
        internal TarEntry(TarEntryType entryType, string entryName, TarFormat format)
        {
            ArgumentException.ThrowIfNullOrEmpty(entryName);

            // Throws if format is unknown or out of range
            TarHelpers.VerifyEntryTypeIsSupported(entryType, format, forWriting: false);

            _readerOfOrigin = null;

            _header = default;

            _header._extendedAttributes = new Dictionary<string, string>();

            _header._name = entryName;
            _header._linkName = string.Empty;
            _header._typeFlag = entryType;
            _header._mode = (int)TarHelpers.DefaultMode;

            _header._gName = string.Empty;
            _header._uName = string.Empty;

            DateTimeOffset now = DateTimeOffset.Now;
            _header._mTime = now;
            _header._aTime = now;
            _header._cTime = now;
        }

        /// <summary>
        /// The checksum of all the fields in this entry. The value is non-zero either when the entry is read from an existing archive, or after the entry is written to a new archive.
        /// </summary>
        public int Checksum => _header._checksum;

        /// <summary>
        /// The type of filesystem object represented by this entry.
        /// </summary>
        public TarEntryType EntryType => _header._typeFlag;

        /// <summary>
        /// The ID of the group that owns the file represented by this entry.
        /// </summary>
        /// <remarks>This field is only supported in Unix platforms.</remarks>
        public int Gid
        {
            get => _header._gid;
            set => _header._gid = value;
        }

        /// <summary>
        /// A timestamps that represents the last time the contents of the file represented by this entry were modified.
        /// </summary>
        /// <remarks>In Unix platforms, this timestamp is commonly known as <c>mtime</c>.</remarks>
        public DateTimeOffset ModificationTime
        {
            get => _header._mTime;
            set
            {
                if (value < DateTimeOffset.UnixEpoch)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _header._mTime = value;
            }
        }

        /// <summary>
        /// When the <see cref="EntryType"/> indicates an entry that can contain data, this property returns the length in bytes of such data.
        /// </summary>
        /// <remarks>The entry type that commonly contains data is <see cref="TarEntryType.RegularFile"/> (or <see cref="TarEntryType.V7RegularFile"/> in the <see cref="TarFormat.V7"/> format). Other uncommon entry types that can also contain data are: <see cref="TarEntryType.ContiguousFile"/>, <see cref="TarEntryType.DirectoryList"/>, <see cref="TarEntryType.MultiVolume"/> and <see cref="TarEntryType.SparseFile"/>.</remarks>
        public long Length => _header._dataStream != null ? _header._dataStream.Length : _header._size;

        /// <summary>
        /// When the <see cref="EntryType"/> indicates a <see cref="TarEntryType.SymbolicLink"/> or a <see cref="TarEntryType.HardLink"/>, this property returns the link target path of such link.
        /// </summary>
        /// <exception cref="InvalidOperationException">Cannot set the link name if the entry type is not <see cref="TarEntryType.HardLink"/> or <see cref="TarEntryType.SymbolicLink"/>.</exception>
        public string LinkName
        {
            get => _header._linkName;
            set
            {
                if (_header._typeFlag is not TarEntryType.HardLink and not TarEntryType.SymbolicLink)
                {
                    throw new InvalidOperationException(SR.TarEntryHardLinkOrSymLinkExpected);
                }
                _header._linkName = value;
            }
        }

        /// <summary>
        /// Represents the Unix file permissions of the file represented by this entry.
        /// </summary>
        /// <remarks>The value in this field has no effect on Windows platforms.</remarks>
        public TarFileMode Mode
        {
            get => (TarFileMode)_header._mode;
            set
            {
                if ((int)value is < 0 or > 4095) // 4095 in decimal is 7777 in octal
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _header._mode = (int)value;
            }
        }

        /// <summary>
        /// Represents the name of the entry, which includes the relative path and the filename.
        /// </summary>
        public string Name
        {
            get => _header._name;
            set
            {
                ArgumentException.ThrowIfNullOrEmpty(value);
                _header._name = value;
            }
        }

        /// <summary>
        /// The ID of the user that owns the file represented by this entry.
        /// </summary>
        /// <remarks>This field is only supported in Unix platforms.</remarks>
        public int Uid
        {
            get => _header._uid;
            set => _header._uid = value;
        }

        /// <summary>
        /// Extracts the current entry to the filesystem.
        /// </summary>
        /// <param name="destinationFileName">The path to the destination file.</param>
        /// <param name="overwrite"><see langword="true"/> if this method should overwrite any existing filesystem object located in the <paramref name="destinationFileName"/> path; <see langword="false"/> to prevent overwriting.</param>
        /// <remarks><para>Files of type <see cref="TarEntryType.BlockDevice"/>, <see cref="TarEntryType.CharacterDevice"/> or <see cref="TarEntryType.Fifo"/> can only be extracted in Unix platforms.</para>
        /// <para>Elevation is required to extract a <see cref="TarEntryType.BlockDevice"/> or <see cref="TarEntryType.CharacterDevice"/> to disk.</para></remarks>
        /// <exception cref="ArgumentException"><paramref name="destinationFileName"/> is <see langword="null"/> or empty.</exception>
        /// <exception cref="IOException"><para>The parent directory of <paramref name="destinationFileName"/> does not exist.</para>
        /// <para>-or-</para>
        /// <para><paramref name="overwrite"/> is <see langword="false"/> and a file already exists in <paramref name="destinationFileName"/>.</para>
        /// <para>-or-</para>
        /// <para>A directory exists with the same name as <paramref name="destinationFileName"/>.</para>
        /// <para>-or-</para>
        /// <para>An I/O problem occurred.</para></exception>
        /// <exception cref="InvalidOperationException">Attempted to extract an unsupported entry type.</exception>
        /// <exception cref="UnauthorizedAccessException">Operation not permitted due to insufficient permissions.</exception>
        public void ExtractToFile(string destinationFileName, bool overwrite)
        {
            ArgumentException.ThrowIfNullOrEmpty(destinationFileName);

            string? directoryPath = Path.GetDirectoryName(destinationFileName);
            // If the destination contains a directory segment, need to check that it exists
            if (!string.IsNullOrEmpty(directoryPath) && !Path.Exists(directoryPath))
            {
                throw new IOException(string.Format(SR.IO_PathNotFound_NoPathName, destinationFileName));
            }

            VerifyOverwriteFileIsPossible(destinationFileName, overwrite);

            switch (EntryType)
            {
                case TarEntryType.Directory:
                case TarEntryType.DirectoryList:
                    Directory.CreateDirectory(destinationFileName);
                    break;

                case TarEntryType.RegularFile:
                case TarEntryType.V7RegularFile:
                case TarEntryType.ContiguousFile:
                    ExtractAsRegularFile(destinationFileName);

                    break;

                case TarEntryType.SymbolicLink:
                    FileInfo link = new(destinationFileName);
                    link.CreateAsSymbolicLink(LinkName);
                    break;

                case TarEntryType.HardLink:
                    ExtractAsHardLink(destinationFileName);
                    break;

                case TarEntryType.BlockDevice:
                    ExtractAsBlockDevice(destinationFileName);
                    break;

                case TarEntryType.CharacterDevice:
                    ExtractAsCharacterDevice(destinationFileName);
                    break;

                case TarEntryType.Fifo:
                    ExtractAsFifo(destinationFileName);
                    break;

                case TarEntryType.ExtendedAttributes:
                case TarEntryType.GlobalExtendedAttributes:
                case TarEntryType.LongPath:
                case TarEntryType.LongLink:
                    Debug.Assert(false, $"Metadata entry type should not be visible: '{EntryType}'");
                    break;

                case TarEntryType.MultiVolume:
                case TarEntryType.RenamedOrSymlinked:
                case TarEntryType.SparseFile:
                case TarEntryType.TapeVolume:
                default:
                    throw new InvalidOperationException(string.Format(SR.TarEntryTypeNotSupportedForExtracting, EntryType));
            }
        }

        /// <summary>
        /// Asynchronously extracts the current entry to the filesystem.
        /// </summary>
        /// <param name="destinationFileName">The path to the destination file.</param>
        /// <param name="overwrite"><see langword="true"/> if this method should overwrite any existing filesystem object located in the <paramref name="destinationFileName"/> path; <see langword="false"/> to prevent overwriting.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous extraction operation.</returns>
        /// <remarks><para>Files of type <see cref="TarEntryType.BlockDevice"/>, <see cref="TarEntryType.CharacterDevice"/> or <see cref="TarEntryType.Fifo"/> can only be extracted in Unix platforms.</para>
        /// <para>Elevation is required to extract a <see cref="TarEntryType.BlockDevice"/> or <see cref="TarEntryType.CharacterDevice"/> to disk.</para></remarks>
        /// <exception cref="ArgumentException"><paramref name="destinationFileName"/> is <see langword="null"/> or empty.</exception>
        /// <exception cref="IOException"><para>The parent directory of <paramref name="destinationFileName"/> does not exist.</para>
        /// <para>-or-</para>
        /// <para><paramref name="overwrite"/> is <see langword="false"/> and a file already exists in <paramref name="destinationFileName"/>.</para>
        /// <para>-or-</para>
        /// <para>A directory exists with the same name as <paramref name="destinationFileName"/>.</para>
        /// <para>-or-</para>
        /// <para>An I/O problem occurred.</para></exception>
        /// <exception cref="InvalidOperationException">Attempted to extract an unsupported entry type.</exception>
        /// <exception cref="UnauthorizedAccessException">Operation not permitted due to insufficient permissions.</exception>
        public Task ExtractToFileAsync(string destinationFileName, bool overwrite, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The data section of this entry. If the <see cref="EntryType"/> does not support containing data, then returns <see langword="null"/>.
        /// </summary>
        /// <value><para>Gets a stream that represents the data section of this entry.</para>
        /// <para>Sets a new stream that represents the data section, if it makes sense for the <see cref="EntryType"/> to contain data; if a stream already existed, the old stream gets disposed before substituting it with the new stream. Setting a <see langword="null"/> stream is allowed.</para></value>
        /// <remarks>If you write data to this data stream, make sure to rewind it to the desired start position before writing this entry into an archive using <see cref="TarWriter.WriteEntry(TarEntry)"/> or <see cref="TarWriter.WriteEntryAsync(TarEntry, CancellationToken)"/>.</remarks>
        /// <exception cref="InvalidOperationException">Setting a data section is not supported because the <see cref="EntryType"/> is not <see cref="TarEntryType.RegularFile"/> (or <see cref="TarEntryType.V7RegularFile"/> for an archive of <see cref="TarFormat.V7"/> format).</exception>
        /// <exception cref="IOException"><para>Cannot set an unreadable stream.</para>
        /// <para>-or-</para>
        /// <para>An I/O problem occurred.</para></exception>
        public Stream? DataStream
        {
            get => _header._dataStream;
            set
            {
                if (!IsDataStreamSetterSupported())
                {
                    throw new InvalidOperationException(string.Format(SR.TarEntryDoesNotSupportDataStream, Name, EntryType));
                }

                if (value != null && !value.CanRead)
                {
                    throw new IOException(SR.IO_NotSupported_UnreadableStream);
                }

                if (_readerOfOrigin != null)
                {
                    // This entry came from a reader, so if the underlying stream is unseekable, we need to
                    // manually advance the stream pointer to the next header before doing the substitution
                    // The original stream will get disposed when the reader gets disposed.
                    _readerOfOrigin.AdvanceDataStreamIfNeeded();
                    // We only do this once
                    _readerOfOrigin = null;
                }

                _header._dataStream?.Dispose();

                _header._dataStream = value;
            }
        }

        /// <summary>
        /// A string that represents the current entry.
        /// </summary>
        /// <returns>The <see cref="Name"/> of the current entry.</returns>
        public override string ToString() => Name;

        // Abstract method that determines if setting the data stream for this entry is allowed.
        internal abstract bool IsDataStreamSetterSupported();

        // Throws an exception if a file exists in the location of 'destinationFileName' and 'overwrite' is 'false',
        // or if a directory exists in the location of 'destinationFileName'.
        private static void VerifyOverwriteFileIsPossible(string destinationFileName, bool overwrite)
        {
            // In most cases, nothing exists in the destination, so we perform one check
            if (Path.Exists(destinationFileName))
            {
                if (File.Exists(destinationFileName))
                {
                    if (!overwrite)
                    {
                        throw new IOException(string.Format(SR.IO_AlreadyExists_Name, destinationFileName));
                    }
                    File.Delete(destinationFileName);
                }
                // We never want to overwrite a directory, so we always throw
                else if (Directory.Exists(destinationFileName))
                {
                    throw new IOException(string.Format(SR.IO_AlreadyExists_Name, destinationFileName));
                }
            }
        }

        // Extracts the current entry to a location relative to the specified directory.
        internal void ExtractRelativeToDirectory(string destinationDirectoryName, bool overwrite)
        {
            Debug.Assert(!string.IsNullOrEmpty(destinationDirectoryName));

            // This returns a good DirectoryInfo even if destinationDirectoryName exists
            DirectoryInfo di = Directory.CreateDirectory(destinationDirectoryName);

            string destinationDirectoryFullPath = di.FullName.EndsWith(Path.DirectorySeparatorChar) ? di.FullName : di.FullName + Path.DirectorySeparatorChar;

            // Resolves unexpected relative segments
            string fileDestinationPath = Path.GetFullPath(Path.Combine(destinationDirectoryFullPath, ArchivingUtils.SanitizeEntryFilePath(Name)));

            if (!fileDestinationPath.StartsWith(destinationDirectoryFullPath, PathInternal.StringComparison))
            {
                throw new IOException(string.Format(SR.TarExtractingResultsInOutside, fileDestinationPath, destinationDirectoryFullPath));
            }

            if (EntryType == TarEntryType.Directory)
            {
                Directory.CreateDirectory(fileDestinationPath);
            }
            else
            {
                // If it is a file, create containing directory.
                Directory.CreateDirectory(Path.GetDirectoryName(fileDestinationPath)!);
                ExtractToFile(fileDestinationPath, overwrite);
            }
        }

        // Extracts the current entry as a regular file into the specified destination.
        // The assumption is that at this point there is no preexisting file or directory in that destination.
        private void ExtractAsRegularFile(string destinationFileName)
        {
            Debug.Assert(!Path.Exists(destinationFileName));

            FileStreamOptions fileStreamOptions = new FileStreamOptions()
            {
                Access = FileAccess.Write,
                Mode = FileMode.CreateNew,
                Share = FileShare.None,
                PreallocationSize = Length,
            };
            // Rely on FileStream's ctor for further checking destinationFileName parameter
            using (FileStream fs = new FileStream(destinationFileName, fileStreamOptions))
            {
                if (DataStream != null)
                {
                    // Important: The DataStream will be written from its current position
                    DataStream.CopyTo(fs);
                    SetModeOnFile(fs.SafeFileHandle, destinationFileName);
                }
            }

            ArchivingUtils.AttemptSetLastWriteTime(destinationFileName, ModificationTime);
        }

        // Abstract method that extracts the current entry when it is a block device.
        partial void ExtractAsBlockDevice(string destinationFileName);

        // Abstract method that extracts the current entry when it is a character device.
        partial void ExtractAsCharacterDevice(string destinationFileName);

        // Abstract method that extracts the current entry when it is a fifo file.
        partial void ExtractAsFifo(string destinationFileName);

        // Abstract method that extracts the current entry when it is a hard link.
        partial void ExtractAsHardLink(string destinationFileName);

        // Abstract method that sets the file permissions of the file.
        partial void SetModeOnFile(SafeFileHandle handle, string destinationFileName);
    }
}
