// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Formats.Tar
{
    /// <summary>
    /// Abstract class that represents a tar entry from an archive.
    /// </summary>
    /// <remarks>All the properties exposed by this class are supported by the <see cref="TarEntryFormat.V7"/>, <see cref="TarEntryFormat.Ustar"/>, <see cref="TarEntryFormat.Pax"/> and <see cref="TarEntryFormat.Gnu"/> formats.</remarks>
    public abstract partial class TarEntry
    {
        internal TarHeader _header;

        // Used to access the data section of this entry in an unseekable file
        private TarReader? _readerOfOrigin;

        // Constructor called when reading a TarEntry from a TarReader.
        internal TarEntry(TarHeader header, TarReader readerOfOrigin, TarEntryFormat format)
        {
            // This constructor is called after reading a header from the archive,
            // and we should've already detected the format of the header.
            Debug.Assert(header._format == format);
            _header = header;
            _readerOfOrigin = readerOfOrigin;
        }

        // Constructor called when the user creates a TarEntry instance from scratch.
        internal TarEntry(TarEntryType entryType, string entryName, TarEntryFormat format, bool isGea)
        {
            ArgumentException.ThrowIfNullOrEmpty(entryName);

            Debug.Assert(!isGea || entryType is TarEntryType.GlobalExtendedAttributes);

            if (!isGea)
            {
                TarHelpers.ThrowIfEntryTypeNotSupported(entryType, format);
            }

            // Default values for fields shared by all supported formats
            _header = new TarHeader(format, entryName, TarHelpers.GetDefaultMode(entryType), DateTimeOffset.UtcNow, entryType);
        }

        // Constructor called when converting an entry to the selected format.
        internal TarEntry(TarEntry other, TarEntryFormat format)
        {
            if (other is PaxGlobalExtendedAttributesTarEntry)
            {
                throw new InvalidOperationException(SR.TarCannotConvertPaxGlobalExtendedAttributesEntry);
            }

            TarEntryType compatibleEntryType = TarHelpers.GetCorrectTypeFlagForFormat(format, other.EntryType);

            TarHelpers.ThrowIfEntryTypeNotSupported(compatibleEntryType, format);

            _readerOfOrigin = other._readerOfOrigin;

            _header = new TarHeader(format, compatibleEntryType, other._header);
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
        /// The format of the entry.
        /// </summary>
        public TarEntryFormat Format => _header._format;

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
        /// <remarks>The entry type that commonly contains data is <see cref="TarEntryType.RegularFile"/> (or <see cref="TarEntryType.V7RegularFile"/> in the <see cref="TarEntryFormat.V7"/> format). Other uncommon entry types that can also contain data are: <see cref="TarEntryType.ContiguousFile"/>, <see cref="TarEntryType.DirectoryList"/>, <see cref="TarEntryType.MultiVolume"/> and <see cref="TarEntryType.SparseFile"/>.</remarks>
        public long Length => _header._dataStream != null ? _header._dataStream.Length : _header._size;

        /// <summary>
        /// When the <see cref="EntryType"/> indicates a <see cref="TarEntryType.SymbolicLink"/> or a <see cref="TarEntryType.HardLink"/>, this property returns the link target path of such link.
        /// </summary>
        /// <exception cref="InvalidOperationException">Cannot set the link name if the entry type is not <see cref="TarEntryType.HardLink"/> or <see cref="TarEntryType.SymbolicLink"/>.</exception>
        public string LinkName
        {
            get => _header._linkName ?? string.Empty;
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
        public UnixFileMode Mode
        {
            get => (UnixFileMode)_header._mode;
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
        /// Extracts the current file or directory to the filesystem. Symbolic links and hard links are not extracted.
        /// </summary>
        /// <param name="destinationFileName">The path to the destination file.</param>
        /// <param name="overwrite"><see langword="true"/> if this method should overwrite any existing filesystem object located in the <paramref name="destinationFileName"/> path; <see langword="false"/> to prevent overwriting.</param>
        /// <remarks><para>Files of type <see cref="TarEntryType.BlockDevice"/>, <see cref="TarEntryType.CharacterDevice"/> or <see cref="TarEntryType.Fifo"/> can only be extracted in Unix platforms.</para>
        /// <para>Elevation is required to extract a <see cref="TarEntryType.BlockDevice"/> or <see cref="TarEntryType.CharacterDevice"/> to disk.</para>
        /// <para>Symbolic links can be recreated using <see cref="File.CreateSymbolicLink(string, string)"/>, <see cref="Directory.CreateSymbolicLink(string, string)"/> or <see cref="FileSystemInfo.CreateAsSymbolicLink(string)"/>.</para>
        /// <para>Hard links can only be extracted when using <see cref="TarFile.ExtractToDirectory(Stream, string, bool)"/> or <see cref="TarFile.ExtractToDirectory(string, string, bool)"/>.</para></remarks>
        /// <exception cref="ArgumentException"><paramref name="destinationFileName"/> is <see langword="null"/> or empty.</exception>
        /// <exception cref="IOException"><para>The parent directory of <paramref name="destinationFileName"/> does not exist.</para>
        /// <para>-or-</para>
        /// <para><paramref name="overwrite"/> is <see langword="false"/> and a file already exists in <paramref name="destinationFileName"/>.</para>
        /// <para>-or-</para>
        /// <para>A directory exists with the same name as <paramref name="destinationFileName"/>.</para>
        /// <para>-or-</para>
        /// <para>An I/O problem occurred.</para></exception>
        /// <exception cref="InvalidOperationException">Attempted to extract a symbolic link, a hard link or an unsupported entry type.</exception>
        /// <exception cref="UnauthorizedAccessException">Operation not permitted due to insufficient permissions.</exception>
        public void ExtractToFile(string destinationFileName, bool overwrite)
        {
            ArgumentException.ThrowIfNullOrEmpty(destinationFileName);
            if (EntryType is TarEntryType.SymbolicLink or TarEntryType.HardLink or TarEntryType.GlobalExtendedAttributes)
            {
                throw new InvalidOperationException(string.Format(SR.TarEntryTypeNotSupportedForExtracting, EntryType));
            }
            ExtractToFileInternal(destinationFileName, linkTargetPath: null, overwrite);
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
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }
            ArgumentException.ThrowIfNullOrEmpty(destinationFileName);
            if (EntryType is TarEntryType.SymbolicLink or TarEntryType.HardLink or TarEntryType.GlobalExtendedAttributes)
            {
                return Task.FromException(new InvalidOperationException(string.Format(SR.TarEntryTypeNotSupportedForExtracting, EntryType)));
            }
            return ExtractToFileInternalAsync(destinationFileName, linkTargetPath: null, overwrite, cancellationToken);
        }

        /// <summary>
        /// The data section of this entry. If the <see cref="EntryType"/> does not support containing data, then returns <see langword="null"/>.
        /// </summary>
        /// <value><para>Gets a stream that represents the data section of this entry.</para>
        /// <para>Sets a new stream that represents the data section, if it makes sense for the <see cref="EntryType"/> to contain data; if a stream already existed, the old stream gets disposed before substituting it with the new stream. Setting a <see langword="null"/> stream is allowed.</para></value>
        /// <remarks>If you write data to this data stream, make sure to rewind it to the desired start position before writing this entry into an archive using <see cref="TarWriter.WriteEntry(TarEntry)"/>.</remarks>
        /// <exception cref="InvalidOperationException">Setting a data section is not supported because the <see cref="EntryType"/> is not <see cref="TarEntryType.RegularFile"/> (or <see cref="TarEntryType.V7RegularFile"/> for an archive of <see cref="TarEntryFormat.V7"/> format).</exception>
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

        // Extracts the current entry to a location relative to the specified directory.
        internal void ExtractRelativeToDirectory(string destinationDirectoryPath, bool overwrite)
        {
            (string fileDestinationPath, string? linkTargetPath) = GetDestinationAndLinkPaths(destinationDirectoryPath);

            if (EntryType == TarEntryType.Directory)
            {
                Directory.CreateDirectory(fileDestinationPath);
            }
            else
            {
                // If it is a file, create containing directory.
                Directory.CreateDirectory(Path.GetDirectoryName(fileDestinationPath)!);
                ExtractToFileInternal(fileDestinationPath, linkTargetPath, overwrite);
            }
        }

        // Asynchronously extracts the current entry to a location relative to the specified directory.
        internal Task ExtractRelativeToDirectoryAsync(string destinationDirectoryPath, bool overwrite, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            (string fileDestinationPath, string? linkTargetPath) = GetDestinationAndLinkPaths(destinationDirectoryPath);

            if (EntryType == TarEntryType.Directory)
            {
                Directory.CreateDirectory(fileDestinationPath);
                return Task.CompletedTask;
            }
            else
            {
                // If it is a file, create containing directory.
                Directory.CreateDirectory(Path.GetDirectoryName(fileDestinationPath)!);
                return ExtractToFileInternalAsync(fileDestinationPath, linkTargetPath, overwrite, cancellationToken);
            }
        }

        // Gets the sanitized paths for the file destination and link target paths to be used when extracting relative to a directory.
        private (string, string?) GetDestinationAndLinkPaths(string destinationDirectoryPath)
        {
            Debug.Assert(!string.IsNullOrEmpty(destinationDirectoryPath));
            Debug.Assert(Path.IsPathFullyQualified(destinationDirectoryPath));

            destinationDirectoryPath = Path.TrimEndingDirectorySeparator(destinationDirectoryPath);

            string? fileDestinationPath = GetSanitizedFullPath(destinationDirectoryPath, Name);
            if (fileDestinationPath == null)
            {
                throw new IOException(string.Format(SR.TarExtractingResultsFileOutside, Name, destinationDirectoryPath));
            }

            string? linkTargetPath = null;
            if (EntryType is TarEntryType.SymbolicLink or TarEntryType.HardLink)
            {
                if (string.IsNullOrEmpty(LinkName))
                {
                    throw new FormatException(SR.TarEntryHardLinkOrSymlinkLinkNameEmpty);
                }

                linkTargetPath = GetSanitizedFullPath(destinationDirectoryPath, LinkName);
                if (linkTargetPath == null)
                {
                    throw new IOException(string.Format(SR.TarExtractingResultsLinkOutside, LinkName, destinationDirectoryPath));
                }
            }

            return (fileDestinationPath, linkTargetPath);
        }

        // If the path can be extracted in the specified destination directory, returns the full path with sanitized file name. Otherwise, returns null.
        private static string? GetSanitizedFullPath(string destinationDirectoryFullPath, string path)
        {
            string fullyQualifiedPath = Path.IsPathFullyQualified(path) ? path : Path.Combine(destinationDirectoryFullPath, path);
            string normalizedPath = Path.GetFullPath(fullyQualifiedPath); // Removes relative segments
            string sanitizedPath = Path.Join(Path.GetDirectoryName(normalizedPath), ArchivingUtils.SanitizeEntryFilePath(Path.GetFileName(normalizedPath)));
            return sanitizedPath.StartsWith(destinationDirectoryFullPath, PathInternal.StringComparison) ? sanitizedPath : null;
        }

        // Extracts the current entry into the filesystem, regardless of the entry type.
        private void ExtractToFileInternal(string filePath, string? linkTargetPath, bool overwrite)
        {
            VerifyPathsForEntryType(filePath, linkTargetPath, overwrite);

            if (EntryType is TarEntryType.RegularFile or TarEntryType.V7RegularFile or TarEntryType.ContiguousFile)
            {
                ExtractAsRegularFile(filePath);
            }
            else
            {
                CreateNonRegularFile(filePath, linkTargetPath);
            }
        }

        // Asynchronously extracts the current entry into the filesystem, regardless of the entry type.
        private Task ExtractToFileInternalAsync(string filePath, string? linkTargetPath, bool overwrite, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }
            VerifyPathsForEntryType(filePath, linkTargetPath, overwrite);

            if (EntryType is TarEntryType.RegularFile or TarEntryType.V7RegularFile or TarEntryType.ContiguousFile)
            {
                return ExtractAsRegularFileAsync(filePath, cancellationToken);
            }
            else
            {
                CreateNonRegularFile(filePath, linkTargetPath);
                return Task.CompletedTask;
            }
        }

        private void CreateNonRegularFile(string filePath, string? linkTargetPath)
        {
            Debug.Assert(EntryType is not TarEntryType.RegularFile or TarEntryType.V7RegularFile or TarEntryType.ContiguousFile);

            switch (EntryType)
            {
                case TarEntryType.Directory:
                case TarEntryType.DirectoryList:
                    Directory.CreateDirectory(filePath);
                    break;

                case TarEntryType.SymbolicLink:
                    Debug.Assert(!string.IsNullOrEmpty(linkTargetPath));
                    FileInfo link = new(filePath);
                    link.CreateAsSymbolicLink(linkTargetPath);
                    break;

                case TarEntryType.HardLink:
                    Debug.Assert(!string.IsNullOrEmpty(linkTargetPath));
                    ExtractAsHardLink(linkTargetPath, filePath);
                    break;

                case TarEntryType.BlockDevice:
                    ExtractAsBlockDevice(filePath);
                    break;

                case TarEntryType.CharacterDevice:
                    ExtractAsCharacterDevice(filePath);
                    break;

                case TarEntryType.Fifo:
                    ExtractAsFifo(filePath);
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

        // Verifies if the specified paths make sense for the current type of entry.
        private void VerifyPathsForEntryType(string filePath, string? linkTargetPath, bool overwrite)
        {
            string? directoryPath = Path.GetDirectoryName(filePath);
            // If the destination contains a directory segment, need to check that it exists
            if (!string.IsNullOrEmpty(directoryPath) && !Path.Exists(directoryPath))
            {
                throw new IOException(string.Format(SR.IO_PathNotFound_NoPathName, filePath));
            }

            if (!Path.Exists(filePath))
            {
                return;
            }

            // We never want to overwrite a directory, so we always throw
            if (Directory.Exists(filePath))
            {
                throw new IOException(string.Format(SR.IO_AlreadyExists_Name, filePath));
            }

            // A file exists at this point
            if (!overwrite)
            {
                throw new IOException(string.Format(SR.IO_AlreadyExists_Name, filePath));
            }
            File.Delete(filePath);

            if (EntryType is TarEntryType.SymbolicLink or TarEntryType.HardLink)
            {
                if (!string.IsNullOrEmpty(linkTargetPath))
                {
                    string? targetDirectoryPath = Path.GetDirectoryName(linkTargetPath);
                    // If the destination target contains a directory segment, need to check that it exists
                    if (!string.IsNullOrEmpty(targetDirectoryPath) && !Path.Exists(targetDirectoryPath))
                    {
                        throw new IOException(string.Format(SR.TarSymbolicLinkTargetNotExists, filePath, linkTargetPath));
                    }

                    if (EntryType is TarEntryType.HardLink)
                    {
                        if (!Path.Exists(linkTargetPath))
                        {
                            throw new IOException(string.Format(SR.TarHardLinkTargetNotExists, filePath, linkTargetPath));
                        }
                        else if (Directory.Exists(linkTargetPath))
                        {
                            throw new IOException(string.Format(SR.TarHardLinkToDirectoryNotAllowed, filePath, linkTargetPath));
                        }
                    }
                }
                else
                {
                    throw new FormatException(SR.TarEntryHardLinkOrSymlinkLinkNameEmpty);
                }
            }
        }

        // Extracts the current entry as a regular file into the specified destination.
        // The assumption is that at this point there is no preexisting file or directory in that destination.
        private void ExtractAsRegularFile(string destinationFileName)
        {
            Debug.Assert(!Path.Exists(destinationFileName));

            // Rely on FileStream's ctor for further checking destinationFileName parameter
            using (FileStream fs = new FileStream(destinationFileName, CreateFileStreamOptions(isAsync: false)))
            {
                // Important: The DataStream will be written from its current position
                DataStream?.CopyTo(fs);
            }

            ArchivingUtils.AttemptSetLastWriteTime(destinationFileName, ModificationTime);
        }

        // Asynchronously extracts the current entry as a regular file into the specified destination.
        // The assumption is that at this point there is no preexisting file or directory in that destination.
        private async Task ExtractAsRegularFileAsync(string destinationFileName, CancellationToken cancellationToken)
        {
            Debug.Assert(!Path.Exists(destinationFileName));

            cancellationToken.ThrowIfCancellationRequested();

            // Rely on FileStream's ctor for further checking destinationFileName parameter
            FileStream fs = new FileStream(destinationFileName, CreateFileStreamOptions(isAsync: true));
            await using (fs.ConfigureAwait(false))
            {
                if (DataStream != null)
                {
                    // Important: The DataStream will be written from its current position
                    await DataStream.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
                }
            }

            ArchivingUtils.AttemptSetLastWriteTime(destinationFileName, ModificationTime);
        }

        private FileStreamOptions CreateFileStreamOptions(bool isAsync)
        {
            FileStreamOptions fileStreamOptions = new()
            {
                Access = FileAccess.Write,
                Mode = FileMode.CreateNew,
                Share = FileShare.None,
                PreallocationSize = Length,
                Options = isAsync ? FileOptions.Asynchronous : FileOptions.None
            };

            if (!OperatingSystem.IsWindows())
            {
                 const UnixFileMode OwnershipPermissions =
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead | UnixFileMode.OtherWrite |  UnixFileMode.OtherExecute;

                // Restore permissions.
                // For security, limit to ownership permissions, and respect umask (through UnixCreateMode).
                fileStreamOptions.UnixCreateMode = Mode & OwnershipPermissions;
            }

            return fileStreamOptions;
        }
    }
}
