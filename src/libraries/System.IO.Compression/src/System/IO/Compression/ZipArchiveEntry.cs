// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using static System.IO.Compression.ZipArchiveEntryConstants;

namespace System.IO.Compression
{
    // The disposable fields that this class owns get disposed when the ZipArchive it belongs to gets disposed
    public partial class ZipArchiveEntry
    {
        private ZipArchive _archive;
        private readonly bool _originallyInArchive;
        private readonly uint _diskNumberStart;
        private readonly ZipVersionMadeByPlatform _versionMadeByPlatform;
        private ZipVersionNeededValues _versionMadeBySpecification;
        private ZipVersionNeededValues _versionToExtract;
        private BitFlagValues _generalPurposeBitFlag;
        private readonly bool _isEncrypted;
        private CompressionMethodValues _storedCompressionMethod;
        private DateTimeOffset _lastModified;
        private long _compressedSize;
        private long _uncompressedSize;
        private long _offsetOfLocalHeader;
        private long? _storedOffsetOfCompressedData;
        private uint _crc32;
        // An array of buffers, each a maximum of MaxSingleBufferSize in size
        private byte[][]? _compressedBytes;
        private MemoryStream? _storedUncompressedData;
        private bool _currentlyOpenForWrite;
        private bool _everOpenedForWrite;
        private Stream? _outstandingWriteStream;
        private uint _externalFileAttr;
        private string _storedEntryName;
        private byte[] _storedEntryNameBytes;
        // only apply to update mode
        private List<ZipGenericExtraField>? _cdUnknownExtraFields;
        private byte[]? _cdTrailingExtraFieldData;
        private List<ZipGenericExtraField>? _lhUnknownExtraFields;
        private byte[]? _lhTrailingExtraFieldData;
        private byte[] _fileComment;
        private readonly CompressionLevel _compressionLevel;

        // Initializes a ZipArchiveEntry instance for an existing archive entry.
        internal ZipArchiveEntry(ZipArchive archive, ZipCentralDirectoryFileHeader cd)
        {
            _archive = archive;

            _originallyInArchive = true;
            // It's possible for the CompressionMethod setter and DetectEntryNameVersion to update this, even without any explicit
            // changes. This can occur if a ZipArchive instance runs in Update mode and opens a stream with invalid data. In such
            // a situation, both the local file header and the central directory header will be rewritten (to prevent the headers
            // from falling out of sync when the central directory header is rewritten.)
            Changes = ZipArchive.ChangeState.Unchanged;

            _diskNumberStart = cd.DiskNumberStart;
            _versionMadeByPlatform = (ZipVersionMadeByPlatform)cd.VersionMadeByCompatibility;
            _versionMadeBySpecification = (ZipVersionNeededValues)cd.VersionMadeBySpecification;
            _versionToExtract = (ZipVersionNeededValues)cd.VersionNeededToExtract;
            _generalPurposeBitFlag = (BitFlagValues)cd.GeneralPurposeBitFlag;
            _isEncrypted = (_generalPurposeBitFlag & BitFlagValues.IsEncrypted) != 0;
            CompressionMethod = (CompressionMethodValues)cd.CompressionMethod;
            _lastModified = new DateTimeOffset(ZipHelper.DosTimeToDateTime(cd.LastModified));
            _compressedSize = cd.CompressedSize;
            _uncompressedSize = cd.UncompressedSize;
            _externalFileAttr = cd.ExternalFileAttributes;
            _offsetOfLocalHeader = cd.RelativeOffsetOfLocalHeader;
            // we don't know this yet: should be _offsetOfLocalHeader + 30 + _storedEntryNameBytes.Length + extrafieldlength
            // but entryname/extra length could be different in LH
            _storedOffsetOfCompressedData = null;
            _crc32 = cd.Crc32;

            _compressedBytes = null;
            _storedUncompressedData = null;
            _currentlyOpenForWrite = false;
            _everOpenedForWrite = false;
            _outstandingWriteStream = null;

            _storedEntryNameBytes = cd.Filename;
            _storedEntryName = DecodeEntryString(_storedEntryNameBytes);
            DetectEntryNameVersion();

            _lhUnknownExtraFields = null;
            // the cd should have this as null if we aren't in Update mode
            _cdUnknownExtraFields = cd.ExtraFields;
            _cdTrailingExtraFieldData = cd.TrailingExtraFieldData;

            _fileComment = cd.FileComment;

            _compressionLevel = MapCompressionLevel(_generalPurposeBitFlag, CompressionMethod);
        }

        // Initializes a ZipArchiveEntry instance for a new archive entry with a specified compression level.
        internal ZipArchiveEntry(ZipArchive archive, string entryName, CompressionLevel compressionLevel)
            : this(archive, entryName)
        {
            _compressionLevel = compressionLevel;
            if (_compressionLevel == CompressionLevel.NoCompression)
            {
                CompressionMethod = CompressionMethodValues.Stored;
            }
            _generalPurposeBitFlag = MapDeflateCompressionOption(_generalPurposeBitFlag, _compressionLevel, CompressionMethod);
        }

        // Initializes a ZipArchiveEntry instance for a new archive entry.
        internal ZipArchiveEntry(ZipArchive archive, string entryName)
        {
            _archive = archive;

            _originallyInArchive = false;

            _diskNumberStart = 0;
            _versionMadeByPlatform = CurrentZipPlatform;
            _versionMadeBySpecification = ZipVersionNeededValues.Default;
            _versionToExtract = ZipVersionNeededValues.Default; // this must happen before following two assignment
            _compressionLevel = CompressionLevel.Optimal;
            CompressionMethod = CompressionMethodValues.Deflate;
            _generalPurposeBitFlag = MapDeflateCompressionOption(0, _compressionLevel, CompressionMethod);
            _lastModified = DateTimeOffset.Now;

            _compressedSize = 0; // we don't know these yet
            _uncompressedSize = 0;
            _externalFileAttr = entryName.EndsWith(Path.DirectorySeparatorChar) || entryName.EndsWith(Path.AltDirectorySeparatorChar)
                                        ? DefaultDirectoryExternalAttributes
                                        : DefaultFileExternalAttributes;

            _offsetOfLocalHeader = 0;
            _storedOffsetOfCompressedData = null;
            _crc32 = 0;

            _compressedBytes = null;
            _storedUncompressedData = null;
            _currentlyOpenForWrite = false;
            _everOpenedForWrite = false;
            _outstandingWriteStream = null;

            FullName = entryName;

            _cdUnknownExtraFields = null;
            _lhUnknownExtraFields = null;

            _fileComment = Array.Empty<byte>();

            if (_storedEntryNameBytes.Length > ushort.MaxValue)
                throw new ArgumentException(SR.EntryNamesTooLong);

            // grab the stream if we're in create mode
            if (_archive.Mode == ZipArchiveMode.Create)
            {
                _archive.AcquireArchiveStream(this);
            }

            Changes = ZipArchive.ChangeState.Unchanged;
        }

        /// <summary>
        /// The ZipArchive that this entry belongs to. If this entry has been deleted, this will return null.
        /// </summary>
        public ZipArchive Archive => _archive;

        [CLSCompliant(false)]
        public uint Crc32 => _crc32;

        /// <summary>
        /// Gets a value that indicates whether the entry is encrypted.
        /// </summary>
        public bool IsEncrypted => _isEncrypted;

        /// <summary>
        /// The compressed size of the entry. If the archive that the entry belongs to is in Create mode, attempts to get this property will always throw an exception. If the archive that the entry belongs to is in update mode, this property will only be valid if the entry has not been opened.
        /// </summary>
        /// <exception cref="InvalidOperationException">This property is not available because the entry has been written to or modified.</exception>
        public long CompressedLength
        {
            get
            {
                if (_everOpenedForWrite)
                    throw new InvalidOperationException(SR.LengthAfterWrite);
                return _compressedSize;
            }
        }

        public int ExternalAttributes
        {
            get
            {
                return (int)_externalFileAttr;
            }
            set
            {
                ThrowIfInvalidArchive();
                _externalFileAttr = (uint)value;
                Changes |= ZipArchive.ChangeState.FixedLengthMetadata;
            }
        }

        /// <summary>
        /// Gets or sets the optional entry comment.
        /// </summary>
        /// <remarks>
        ///The comment encoding is determined by the <c>entryNameEncoding</c> parameter of the <see cref="ZipArchive(Stream,ZipArchiveMode,bool,Encoding?)"/> constructor.
        /// If the comment byte length is larger than <see cref="ushort.MaxValue"/>, it will be truncated when disposing the archive.
        /// </remarks>
        [AllowNull]
        public string Comment
        {
            get => DecodeEntryString(_fileComment);
            set
            {
                _fileComment = ZipHelper.GetEncodedTruncatedBytesFromString(value, _archive.EntryNameAndCommentEncoding, ushort.MaxValue, out bool isUTF8);

                if (isUTF8)
                {
                    _generalPurposeBitFlag |= BitFlagValues.UnicodeFileNameAndComment;
                }
                Changes |= ZipArchive.ChangeState.DynamicLengthMetadata;
            }
        }

        /// <summary>
        /// The relative path of the entry as stored in the Zip archive. Note that Zip archives allow any string to be the path of the entry, including invalid and absolute paths.
        /// </summary>
        public string FullName
        {
            get
            {
                return _storedEntryName;
            }

            [MemberNotNull(nameof(_storedEntryNameBytes))]
            [MemberNotNull(nameof(_storedEntryName))]
            private set
            {
                ArgumentNullException.ThrowIfNull(value, nameof(FullName));

                _storedEntryNameBytes = ZipHelper.GetEncodedTruncatedBytesFromString(
                    value, _archive.EntryNameAndCommentEncoding, 0 /* No truncation */, out bool isUTF8);

                _storedEntryName = value;

                if (isUTF8)
                {
                    _generalPurposeBitFlag |= BitFlagValues.UnicodeFileNameAndComment;
                }
                else
                {
                    _generalPurposeBitFlag &= ~BitFlagValues.UnicodeFileNameAndComment;
                }

                DetectEntryNameVersion();
            }
        }

        /// <summary>
        /// The last write time of the entry as stored in the Zip archive. When setting this property, the DateTime will be converted to the
        /// Zip timestamp format, which supports a resolution of two seconds. If the data in the last write time field is not a valid Zip timestamp,
        /// an indicator value of 1980 January 1 at midnight will be returned.
        /// </summary>
        /// <exception cref="NotSupportedException">An attempt to set this property was made, but the ZipArchive that this entry belongs to was
        /// opened in read-only mode.</exception>
        /// <exception cref="ArgumentOutOfRangeException">An attempt was made to set this property to a value that cannot be represented in the
        /// Zip timestamp format. The earliest date/time that can be represented is 1980 January 1 0:00:00 (midnight), and the last date/time
        /// that can be represented is 2107 December 31 23:59:58 (one second before midnight).</exception>
        public DateTimeOffset LastWriteTime
        {
            get
            {
                return _lastModified;
            }
            set
            {
                ThrowIfInvalidArchive();
                if (_archive.Mode == ZipArchiveMode.Read)
                    throw new NotSupportedException(SR.ReadOnlyArchive);
                if (_archive.Mode == ZipArchiveMode.Create && _everOpenedForWrite)
                    throw new IOException(SR.FrozenAfterWrite);
                if (value.DateTime.Year < ZipHelper.ValidZipDate_YearMin || value.DateTime.Year > ZipHelper.ValidZipDate_YearMax)
                    throw new ArgumentOutOfRangeException(nameof(value), SR.DateTimeOutOfRange);

                _lastModified = value;
                Changes |= ZipArchive.ChangeState.FixedLengthMetadata;
            }
        }

        /// <summary>
        /// The uncompressed size of the entry. This property is not valid in Create mode, and it is only valid in Update mode if the entry has not been opened.
        /// </summary>
        /// <exception cref="InvalidOperationException">This property is not available because the entry has been written to or modified.</exception>
        public long Length
        {
            get
            {
                if (_everOpenedForWrite)
                    throw new InvalidOperationException(SR.LengthAfterWrite);
                return _uncompressedSize;
            }
        }

        /// <summary>
        /// The filename of the entry. This is equivalent to the substring of Fullname that follows the final directory separator character.
        /// </summary>
        public string Name => ParseFileName(FullName, _versionMadeByPlatform);

        /// <summary>
        /// Return the byte that specifies the platform on which the zip was created.
        /// </summary>
        public ZipVersionMadeByPlatform VersionMadeByPlatform => _versionMadeByPlatform;

        internal ZipArchive.ChangeState Changes { get; private set; }

        internal bool OriginallyInArchive => _originallyInArchive;

        internal long OffsetOfLocalHeader => _offsetOfLocalHeader;

        /// <summary>
        /// Deletes the entry from the archive.
        /// </summary>
        /// <exception cref="IOException">The entry is already open for reading or writing.</exception>
        /// <exception cref="NotSupportedException">The ZipArchive that this entry belongs to was opened in a mode other than ZipArchiveMode.Update. </exception>
        /// <exception cref="ObjectDisposedException">The ZipArchive that this entry belongs to has been disposed.</exception>
        public void Delete()
        {
            if (_archive == null)
                return;

            if (_currentlyOpenForWrite)
                throw new IOException(SR.DeleteOpenEntry);

            if (_archive.Mode != ZipArchiveMode.Update)
                throw new NotSupportedException(SR.DeleteOnlyInUpdate);

            _archive.ThrowIfDisposed();

            _archive.RemoveEntry(this);
            _archive = null!;
            UnloadStreams();
        }

        /// <summary>
        /// Opens the entry. If the archive that the entry belongs to was opened in Read mode, the returned stream will be readable, and it may or may not be seekable. If Create mode, the returned stream will be writable and not seekable. If Update mode, the returned stream will be readable, writable, seekable, and support SetLength.
        /// </summary>
        /// <returns>A Stream that represents the contents of the entry.</returns>
        /// <exception cref="IOException">The entry is already currently open for writing. -or- The entry has been deleted from the archive. -or- The archive that this entry belongs to was opened in ZipArchiveMode.Create, and this entry has already been written to once.</exception>
        /// <exception cref="InvalidDataException">The entry is missing from the archive or is corrupt and cannot be read. -or- The entry has been compressed using a compression method that is not supported.</exception>
        /// <exception cref="ObjectDisposedException">The ZipArchive that this entry belongs to has been disposed.</exception>
        public Stream Open()
        {
            ThrowIfInvalidArchive();

            switch (_archive.Mode)
            {
                case ZipArchiveMode.Read:
                    return OpenInReadMode(checkOpenable: true);
                case ZipArchiveMode.Create:
                    return OpenInWriteMode();
                case ZipArchiveMode.Update:
                default:
                    Debug.Assert(_archive.Mode == ZipArchiveMode.Update);
                    return OpenInUpdateMode();
            }
        }

        /// <summary>
        /// Returns the FullName of the entry.
        /// </summary>
        /// <returns>FullName of the entry</returns>
        public override string ToString()
        {
            return FullName;
        }

        private string DecodeEntryString(byte[] entryStringBytes)
        {
            Debug.Assert(entryStringBytes != null);

            Encoding readEntryStringEncoding =
                (_generalPurposeBitFlag & BitFlagValues.UnicodeFileNameAndComment) == BitFlagValues.UnicodeFileNameAndComment
                ? Encoding.UTF8
                : _archive?.EntryNameAndCommentEncoding ?? Encoding.UTF8;

            return readEntryStringEncoding.GetString(entryStringBytes);
        }

        // Only allow opening ZipArchives with large ZipArchiveEntries in update mode when running in a 64-bit process.
        // This is for compatibility with old behavior that threw an exception for all process bitnesses, because this
        // will not work in a 32-bit process.
        private static readonly bool s_allowLargeZipArchiveEntriesInUpdateMode = IntPtr.Size > 4;

        internal bool EverOpenedForWrite => _everOpenedForWrite;

        internal long GetOffsetOfCompressedData()
        {
            if (_storedOffsetOfCompressedData == null)
            {
                _archive.ArchiveStream.Seek(_offsetOfLocalHeader, SeekOrigin.Begin);
                // by calling this, we are using local header _storedEntryNameBytes.Length and extraFieldLength
                // to find start of data, but still using central directory size information
                if (!ZipLocalFileHeader.TrySkipBlock(_archive.ArchiveStream))
                    throw new InvalidDataException(SR.LocalFileHeaderCorrupt);
                _storedOffsetOfCompressedData = _archive.ArchiveStream.Position;
            }
            return _storedOffsetOfCompressedData.Value;
        }

        private MemoryStream GetUncompressedData()
        {
            if (_storedUncompressedData == null)
            {
                // this means we have never opened it before

                // if _uncompressedSize > int.MaxValue, it's still okay, because MemoryStream will just
                // grow as data is copied into it
                _storedUncompressedData = new MemoryStream((int)_uncompressedSize);

                if (_originallyInArchive)
                {
                    using (Stream decompressor = OpenInReadMode(false))
                    {
                        try
                        {
                            decompressor.CopyTo(_storedUncompressedData);
                        }
                        catch (InvalidDataException)
                        {
                            // this is the case where the archive say the entry is deflate, but deflateStream
                            // throws an InvalidDataException. This property should only be getting accessed in
                            // Update mode, so we want to make sure _storedUncompressedData stays null so
                            // that later when we dispose the archive, this entry loads the compressedBytes, and
                            // copies them straight over
                            _storedUncompressedData.Dispose();
                            _storedUncompressedData = null;
                            _currentlyOpenForWrite = false;
                            _everOpenedForWrite = false;
                            throw;
                        }
                    }
                }

                // if they start modifying it and the compression method is not "store", we should make sure it will get deflated
                if (CompressionMethod != CompressionMethodValues.Stored)
                {
                    CompressionMethod = CompressionMethodValues.Deflate;
                }
            }

            return _storedUncompressedData;
        }

        private CompressionMethodValues CompressionMethod
        {
            get { return _storedCompressionMethod; }
            set
            {
                if (value == CompressionMethodValues.Deflate)
                    VersionToExtractAtLeast(ZipVersionNeededValues.Deflate);
                else if (value == CompressionMethodValues.Deflate64)
                    VersionToExtractAtLeast(ZipVersionNeededValues.Deflate64);
                _storedCompressionMethod = value;
            }
        }
        // does almost everything you need to do to forget about this entry
        // writes the local header/data, gets rid of all the data,
        // closes all of the streams except for the very outermost one that
        // the user holds on to and is responsible for closing
        //
        // after calling this, and only after calling this can we be guaranteed
        // that we are reading to write the central directory
        //
        // should only throw an exception in extremely exceptional cases because it is called from dispose
        internal void WriteAndFinishLocalEntry(bool forceWrite)
        {
            CloseStreams();
            WriteLocalFileHeaderAndDataIfNeeded(forceWrite);
            UnloadStreams();
        }

        private bool WriteCentralDirectoryFileHeaderInitialize(bool forceWrite, out Zip64ExtraField? zip64ExtraField, out uint compressedSizeTruncated, out uint uncompressedSizeTruncated, out ushort extraFieldLength, out uint offsetOfLocalHeaderTruncated)
        {
            // This part is simple, because we should definitely know the sizes by this time

            // _storedEntryNameBytes only gets set when we read in or call moveTo. MoveTo does a check, and
            // reading in should not be able to produce an entryname longer than ushort.MaxValue
            // _fileComment only gets set when we read in or set the FileComment property. This performs its own
            // length check.
            Debug.Assert(_storedEntryNameBytes.Length <= ushort.MaxValue);
            Debug.Assert(_fileComment.Length <= ushort.MaxValue);

            // decide if we need the Zip64 extra field:
            zip64ExtraField = null;

            if (AreSizesTooLarge
#if DEBUG_FORCE_ZIP64
                || _archive._forceZip64
#endif
                )
            {
                compressedSizeTruncated = ZipHelper.Mask32Bit;
                uncompressedSizeTruncated = ZipHelper.Mask32Bit;

                // If we have one of the sizes, the other must go in there as speced for LH, but not necessarily for CH, but we do it anyways
                zip64ExtraField = new()
                {
                    CompressedSize = _compressedSize,
                    UncompressedSize = _uncompressedSize
                };
            }
            else
            {
                compressedSizeTruncated = (uint)_compressedSize;
                uncompressedSizeTruncated = (uint)_uncompressedSize;
            }


            if (IsOffsetTooLarge
#if DEBUG_FORCE_ZIP64
                || _archive._forceZip64
#endif
                )
            {
                offsetOfLocalHeaderTruncated = ZipHelper.Mask32Bit;

                // If we have one of the sizes, the other must go in there as speced for LH, but not necessarily for CH, but we do it anyways
                zip64ExtraField = new()
                {
                    LocalHeaderOffset = _offsetOfLocalHeader
                };
            }
            else
            {
                offsetOfLocalHeaderTruncated = (uint)_offsetOfLocalHeader;
            }

            if (zip64ExtraField != null)
            {
                VersionToExtractAtLeast(ZipVersionNeededValues.Zip64);
            }


            // determine if we can fit zip64 extra field and original extra fields all in
            int currExtraFieldDataLength = ZipGenericExtraField.TotalSize(_cdUnknownExtraFields, _cdTrailingExtraFieldData?.Length ?? 0);
            int bigExtraFieldLength = (zip64ExtraField != null ? zip64ExtraField.TotalSize : 0)
                                      + currExtraFieldDataLength;

            if (bigExtraFieldLength > ushort.MaxValue)
            {
                extraFieldLength = (ushort)(zip64ExtraField != null ? zip64ExtraField.TotalSize : 0);
                _cdUnknownExtraFields = null;
            }
            else
            {
                extraFieldLength = (ushort)bigExtraFieldLength;
            }

            if (_originallyInArchive && Changes == ZipArchive.ChangeState.Unchanged && !forceWrite)
            {
                long centralDirectoryHeaderLength = ZipCentralDirectoryFileHeader.FieldLocations.DynamicData
                    + _storedEntryNameBytes.Length
                    + (zip64ExtraField != null ? zip64ExtraField.TotalSize : 0)
                    + currExtraFieldDataLength
                    + _fileComment.Length;

                _archive.ArchiveStream.Seek(centralDirectoryHeaderLength, SeekOrigin.Current);

                return false;
            }

            return true;
        }

        private void WriteCentralDirectoryFileHeaderPrepare(Span<byte> cdStaticHeader, uint compressedSizeTruncated, uint uncompressedSizeTruncated, ushort extraFieldLength, uint offsetOfLocalHeaderTruncated)
        {
            // The central directory file header begins with the below constant-length structure:
            // Central directory file header signature  (4 bytes)
            // Version made by Specification (version)  (1 byte)
            // Version made by Compatibility (type)     (1 byte)
            // Minimum version needed to extract        (2 bytes)
            // General Purpose bit flag                 (2 bytes)
            // The Compression method                   (2 bytes)
            // File last modification time and date     (4 bytes)
            // CRC-32                                   (4 bytes)
            // Compressed Size                          (4 bytes)
            // Uncompressed Size                        (4 bytes)
            // File Name Length                         (2 bytes)
            // Extra Field Length                       (2 bytes)
            // File Comment Length                      (2 bytes)
            // Start Disk Number                        (2 bytes)
            // Internal File Attributes                 (2 bytes)
            // External File Attributes                 (4 bytes)
            // Offset Of Local Header                   (4 bytes)

            ZipCentralDirectoryFileHeader.SignatureConstantBytes.CopyTo(cdStaticHeader[ZipCentralDirectoryFileHeader.FieldLocations.Signature..]);
            cdStaticHeader[ZipCentralDirectoryFileHeader.FieldLocations.VersionMadeBySpecification] = (byte)_versionMadeBySpecification;
            cdStaticHeader[ZipCentralDirectoryFileHeader.FieldLocations.VersionMadeByCompatibility] = (byte)CurrentZipPlatform;
            BinaryPrimitives.WriteUInt16LittleEndian(cdStaticHeader[ZipCentralDirectoryFileHeader.FieldLocations.VersionNeededToExtract..], (ushort)_versionToExtract);
            BinaryPrimitives.WriteUInt16LittleEndian(cdStaticHeader[ZipCentralDirectoryFileHeader.FieldLocations.GeneralPurposeBitFlags..], (ushort)_generalPurposeBitFlag);
            BinaryPrimitives.WriteUInt16LittleEndian(cdStaticHeader[ZipCentralDirectoryFileHeader.FieldLocations.CompressionMethod..], (ushort)CompressionMethod);
            BinaryPrimitives.WriteUInt32LittleEndian(cdStaticHeader[ZipCentralDirectoryFileHeader.FieldLocations.LastModified..], ZipHelper.DateTimeToDosTime(_lastModified.DateTime));
            BinaryPrimitives.WriteUInt32LittleEndian(cdStaticHeader[ZipCentralDirectoryFileHeader.FieldLocations.Crc32..], _crc32);
            BinaryPrimitives.WriteUInt32LittleEndian(cdStaticHeader[ZipCentralDirectoryFileHeader.FieldLocations.CompressedSize..], compressedSizeTruncated);
            BinaryPrimitives.WriteUInt32LittleEndian(cdStaticHeader[ZipCentralDirectoryFileHeader.FieldLocations.UncompressedSize..], uncompressedSizeTruncated);
            BinaryPrimitives.WriteUInt16LittleEndian(cdStaticHeader[ZipCentralDirectoryFileHeader.FieldLocations.FilenameLength..], (ushort)_storedEntryNameBytes.Length);
            BinaryPrimitives.WriteUInt16LittleEndian(cdStaticHeader[ZipCentralDirectoryFileHeader.FieldLocations.ExtraFieldLength..], extraFieldLength);
            BinaryPrimitives.WriteUInt16LittleEndian(cdStaticHeader[ZipCentralDirectoryFileHeader.FieldLocations.FileCommentLength..], (ushort)_fileComment.Length);
            BinaryPrimitives.WriteUInt16LittleEndian(cdStaticHeader[ZipCentralDirectoryFileHeader.FieldLocations.DiskNumberStart..], 0);
            BinaryPrimitives.WriteUInt16LittleEndian(cdStaticHeader[ZipCentralDirectoryFileHeader.FieldLocations.InternalFileAttributes..], 0);
            BinaryPrimitives.WriteUInt32LittleEndian(cdStaticHeader[ZipCentralDirectoryFileHeader.FieldLocations.ExternalFileAttributes..], _externalFileAttr);
            BinaryPrimitives.WriteUInt32LittleEndian(cdStaticHeader[ZipCentralDirectoryFileHeader.FieldLocations.RelativeOffsetOfLocalHeader..], offsetOfLocalHeaderTruncated);
        }

        // should only throw an exception in extremely exceptional cases because it is called from dispose
        internal void WriteCentralDirectoryFileHeader(bool forceWrite)
        {
            if (WriteCentralDirectoryFileHeaderInitialize(forceWrite, out Zip64ExtraField? zip64ExtraField, out uint compressedSizeTruncated, out uint uncompressedSizeTruncated, out ushort extraFieldLength, out uint offsetOfLocalHeaderTruncated))
            {
                Span<byte> cdStaticHeader = stackalloc byte[ZipCentralDirectoryFileHeader.BlockConstantSectionSize];
                WriteCentralDirectoryFileHeaderPrepare(cdStaticHeader, compressedSizeTruncated, uncompressedSizeTruncated, extraFieldLength, offsetOfLocalHeaderTruncated);

                _archive.ArchiveStream.Write(cdStaticHeader);
                _archive.ArchiveStream.Write(_storedEntryNameBytes);

                // only write zip64ExtraField if we decided we need it (it's not null)
                zip64ExtraField?.WriteBlock(_archive.ArchiveStream);

                // write extra fields (and any malformed trailing data).
                ZipGenericExtraField.WriteAllBlocks(_cdUnknownExtraFields, _cdTrailingExtraFieldData ?? Array.Empty<byte>(), _archive.ArchiveStream);

                if (_fileComment.Length > 0)
                {
                    _archive.ArchiveStream.Write(_fileComment);
                }
            }
        }

        // throws exception if fails, will get called on every relevant entry before closing in update mode
        // can throw InvalidDataException
        internal void LoadLocalHeaderExtraFieldIfNeeded()
        {
            // we should have made this exact call in _archive.Init through ThrowIfOpenable
            Debug.Assert(IsOpenable(false, true, out _));

            // load local header's extra fields. it will be null if we couldn't read for some reason
            if (_originallyInArchive)
            {
                _archive.ArchiveStream.Seek(_offsetOfLocalHeader, SeekOrigin.Begin);
                _lhUnknownExtraFields = ZipLocalFileHeader.GetExtraFields(_archive.ArchiveStream, out _lhTrailingExtraFieldData);
            }
        }

        private byte[][] LoadCompressedBytesIfNeededInitialize(out int maxSingleBufferSize)
        {
            // we know that it is openable at this point
            maxSingleBufferSize = Array.MaxLength;

            byte[][] compressedBytes = new byte[(_compressedSize / maxSingleBufferSize) + 1][];
            for (int i = 0; i < compressedBytes.Length - 1; i++)
            {
                compressedBytes[i] = new byte[maxSingleBufferSize];
            }
            compressedBytes[compressedBytes.Length - 1] = new byte[_compressedSize % maxSingleBufferSize];

            return compressedBytes;
        }

        // throws exception if fails, will get called on every relevant entry before closing in update mode
        // can throw InvalidDataException
        internal void LoadCompressedBytesIfNeeded()
        {
            // we should have made this exact call in _archive.Init through ThrowIfOpenable
            Debug.Assert(IsOpenable(false, true, out _));

            if (!_everOpenedForWrite && _originallyInArchive)
            {
                _compressedBytes = LoadCompressedBytesIfNeededInitialize(out int maxSingleBufferSize);

                _archive.ArchiveStream.Seek(GetOffsetOfCompressedData(), SeekOrigin.Begin);

                for (int i = 0; i < _compressedBytes.Length - 1; i++)
                {
                    _archive.ArchiveStream.ReadAtLeast(_compressedBytes[i], maxSingleBufferSize, throwOnEndOfStream: true);
                }
                _archive.ArchiveStream.ReadAtLeast(_compressedBytes[_compressedBytes.Length - 1], (int)(_compressedSize % maxSingleBufferSize), throwOnEndOfStream: true);
            }
        }

        internal void ThrowIfNotOpenable(bool needToUncompress, bool needToLoadIntoMemory)
        {
            if (!IsOpenable(needToUncompress, needToLoadIntoMemory, out string? message))
                throw new InvalidDataException(message);
        }

        private void DetectEntryNameVersion()
        {
            if (ParseFileName(_storedEntryName, _versionMadeByPlatform) == "")
            {
                VersionToExtractAtLeast(ZipVersionNeededValues.ExplicitDirectory);
            }
        }

        private CheckSumAndSizeWriteStream GetDataCompressor(Stream backingStream, bool leaveBackingStreamOpen, EventHandler? onClose)
        {
            // stream stack: backingStream -> DeflateStream -> CheckSumWriteStream

            // By default we compress with deflate, except if compression level is set to NoCompression then stored is used.
            // Stored is also used for empty files, but we don't actually call through this function for that - we just write the stored value in the header
            // Deflate64 is not supported on all platforms
            Debug.Assert(CompressionMethod == CompressionMethodValues.Deflate
                || CompressionMethod == CompressionMethodValues.Stored);

            bool isIntermediateStream = true;
            Stream compressorStream;
            switch (CompressionMethod)
            {
                case CompressionMethodValues.Stored:
                    compressorStream = backingStream;
                    isIntermediateStream = false;
                    break;
                case CompressionMethodValues.Deflate:
                case CompressionMethodValues.Deflate64:
                default:
                    compressorStream = new DeflateStream(backingStream, _compressionLevel, leaveBackingStreamOpen);
                    break;

            }
            bool leaveCompressorStreamOpenOnClose = leaveBackingStreamOpen && !isIntermediateStream;
            var checkSumStream = new CheckSumAndSizeWriteStream(
                compressorStream,
                backingStream,
                leaveCompressorStreamOpenOnClose,
                this,
                onClose,
                (long initialPosition, long currentPosition, uint checkSum, Stream backing, ZipArchiveEntry thisRef, EventHandler? closeHandler) =>
                {
                    thisRef._crc32 = checkSum;
                    thisRef._uncompressedSize = currentPosition;
                    thisRef._compressedSize = backing.Position - initialPosition;
                    closeHandler?.Invoke(thisRef, EventArgs.Empty);
                });

            return checkSumStream;
        }

        private Stream GetDataDecompressor(Stream compressedStreamToRead)
        {
            Stream? uncompressedStream;
            switch (CompressionMethod)
            {
                case CompressionMethodValues.Deflate:
                    uncompressedStream = new DeflateStream(compressedStreamToRead, CompressionMode.Decompress, _uncompressedSize);
                    break;
                case CompressionMethodValues.Deflate64:
                    uncompressedStream = new DeflateManagedStream(compressedStreamToRead, CompressionMethodValues.Deflate64, _uncompressedSize);
                    break;
                case CompressionMethodValues.Stored:
                default:
                    // we can assume that only deflate/deflate64/stored are allowed because we assume that
                    // IsOpenable is checked before this function is called
                    Debug.Assert(CompressionMethod == CompressionMethodValues.Stored);

                    uncompressedStream = compressedStreamToRead;
                    break;
            }

            return uncompressedStream;
        }

        private Stream OpenInReadMode(bool checkOpenable)
        {
            if (checkOpenable)
                ThrowIfNotOpenable(needToUncompress: true, needToLoadIntoMemory: false);
            return OpenInReadModeGetDataCompressor(GetOffsetOfCompressedData());
        }

        private Stream OpenInReadModeGetDataCompressor(long offsetOfCompressedData)
        {
            Stream compressedStream = new SubReadStream(_archive.ArchiveStream, offsetOfCompressedData, _compressedSize);
            return GetDataDecompressor(compressedStream);
        }

        private WrappedStream OpenInWriteMode()
        {
            if (_everOpenedForWrite)
                throw new IOException(SR.CreateModeWriteOnceAndOneEntryAtATime);

            // we assume that if another entry grabbed the archive stream, that it set this entry's _everOpenedForWrite property to true by calling WriteLocalFileHeaderAndDataIfNeeded
            _archive.DebugAssertIsStillArchiveStreamOwner(this);

            _everOpenedForWrite = true;
            Changes |= ZipArchive.ChangeState.StoredData;
            CheckSumAndSizeWriteStream crcSizeStream = GetDataCompressor(_archive.ArchiveStream, true, (object? o, EventArgs e) =>
            {
                // release the archive stream
                var entry = (ZipArchiveEntry)o!;
                entry._archive.ReleaseArchiveStream(entry);
                entry._outstandingWriteStream = null;
            });
            _outstandingWriteStream = new DirectToArchiveWriterStream(crcSizeStream, this);

            return new WrappedStream(baseStream: _outstandingWriteStream, closeBaseStream: true);
        }

        private WrappedStream OpenInUpdateMode()
        {
            if (_currentlyOpenForWrite)
                throw new IOException(SR.UpdateModeOneStream);

            ThrowIfNotOpenable(needToUncompress: true, needToLoadIntoMemory: true);

            _everOpenedForWrite = true;
            Changes |= ZipArchive.ChangeState.StoredData;
            _currentlyOpenForWrite = true;
            // always put it at the beginning for them
            Stream uncompressedData = GetUncompressedData();
            uncompressedData.Seek(0, SeekOrigin.Begin);
            return new WrappedStream(uncompressedData, this, thisRef =>
            {
                // once they close, we know uncompressed length, but still not compressed length
                // so we don't fill in any size information
                // those fields get figured out when we call GetCompressor as we write it to
                // the actual archive
                thisRef!._currentlyOpenForWrite = false;
            });
        }

        private bool IsOpenable(bool needToUncompress, bool needToLoadIntoMemory, out string? message)
        {
            message = null;

            if (_originallyInArchive)
            {
                if (!IsOpenableInitialVerifications(needToUncompress, out message))
                {
                    return false;
                }
                if (!ZipLocalFileHeader.TrySkipBlock(_archive.ArchiveStream))
                {
                    message = SR.LocalFileHeaderCorrupt;
                    return false;
                }

                // when this property gets called, some duplicated work
                long offsetOfCompressedData = GetOffsetOfCompressedData();
                if (!IsOpenableFinalVerifications(needToLoadIntoMemory, offsetOfCompressedData, out message))
                {
                    return false;
                }

                return true;
            }

            return true;
        }

        private bool IsOpenableInitialVerifications(bool needToUncompress, out string? message)
        {
            message = null;
            if (needToUncompress)
            {
                if (CompressionMethod != CompressionMethodValues.Stored &&
                    CompressionMethod != CompressionMethodValues.Deflate &&
                    CompressionMethod != CompressionMethodValues.Deflate64)
                {
                    message = CompressionMethod switch
                    {
                        CompressionMethodValues.BZip2 or CompressionMethodValues.LZMA => SR.Format(SR.UnsupportedCompressionMethod, CompressionMethod.ToString()),
                        _ => SR.UnsupportedCompression,
                    };
                    return false;
                }
            }
            if (_diskNumberStart != _archive.NumberOfThisDisk)
            {
                message = SR.SplitSpanned;
                return false;
            }
            if (_offsetOfLocalHeader > _archive.ArchiveStream.Length)
            {
                message = SR.LocalFileHeaderCorrupt;
                return false;
            }

            _archive.ArchiveStream.Seek(_offsetOfLocalHeader, SeekOrigin.Begin);
            return true;
        }

        private bool IsOpenableFinalVerifications(bool needToLoadIntoMemory, long offsetOfCompressedData, out string? message)
        {
            message = null;
            if (offsetOfCompressedData + _compressedSize > _archive.ArchiveStream.Length)
            {
                message = SR.LocalFileHeaderCorrupt;
                return false;
            }
            // This limitation originally existed because a) it is unreasonable to load > 4GB into memory
            // but also because the stream reading functions make it hard.  This has been updated to handle
            // this scenario in a 64-bit process using multiple buffers, delivered first as an OOB for
            // compatibility.
            if (needToLoadIntoMemory)
            {
                if (_compressedSize > int.MaxValue)
                {
                    if (!s_allowLargeZipArchiveEntriesInUpdateMode)
                    {
                        message = SR.EntryTooLarge;
                        return false;
                    }
                }
            }

            return true;
        }

        private bool AreSizesTooLarge => _compressedSize > uint.MaxValue || _uncompressedSize > uint.MaxValue;

        private static CompressionLevel MapCompressionLevel(BitFlagValues generalPurposeBitFlag, CompressionMethodValues compressionMethod)
        {
            // Information about the Deflate compression option is stored in bits 1 and 2 of the general purpose bit flags.
            // If the compression method is not Deflate, the Deflate compression option is invalid - default to NoCompression.
            if (compressionMethod == CompressionMethodValues.Deflate || compressionMethod == CompressionMethodValues.Deflate64)
            {
                return ((int)generalPurposeBitFlag & 0x6) switch
                {
                    0 => CompressionLevel.Optimal,
                    2 => CompressionLevel.SmallestSize,
                    4 => CompressionLevel.Fastest,
                    6 => CompressionLevel.Fastest,
                    _ => CompressionLevel.Optimal
                };
            }
            else
            {
                return CompressionLevel.NoCompression;
            }
        }

        private static BitFlagValues MapDeflateCompressionOption(BitFlagValues generalPurposeBitFlag, CompressionLevel compressionLevel, CompressionMethodValues compressionMethod)
        {
            ushort deflateCompressionOptions = (ushort)(
                // The Deflate compression level is only valid if the compression method is actually Deflate (or Deflate64). If it's not, the
                // value of the two bits is undefined and they should be zeroed out.
                compressionMethod == CompressionMethodValues.Deflate || compressionMethod == CompressionMethodValues.Deflate64
                    ? compressionLevel switch
                    {
                        CompressionLevel.Optimal => 0,
                        CompressionLevel.SmallestSize => 2,
                        CompressionLevel.Fastest => 6,
                        CompressionLevel.NoCompression => 6,
                        _ => 0
                    }
                    : 0);

            return (BitFlagValues)(((int)generalPurposeBitFlag & ~0x6) | deflateCompressionOptions);
        }

        private bool IsOffsetTooLarge => _offsetOfLocalHeader > uint.MaxValue;

        private bool ShouldUseZIP64 => AreSizesTooLarge || IsOffsetTooLarge;

        private bool WriteLocalFileHeaderInitialize(bool isEmptyFile, bool forceWrite, out Zip64ExtraField? zip64ExtraField, out uint compressedSizeTruncated, out uint uncompressedSizeTruncated, out ushort extraFieldLength)
        {
            // _entryname only gets set when we read in or call moveTo. MoveTo does a check, and
            // reading in should not be able to produce an entryname longer than ushort.MaxValue
            Debug.Assert(_storedEntryNameBytes.Length <= ushort.MaxValue);

            // decide if we need the Zip64 extra field:
            zip64ExtraField = null;

            // save offset
            _offsetOfLocalHeader = _archive.ArchiveStream.Position;

            // if we already know that we have an empty file don't worry about anything, just do a straight shot of the header
            if (isEmptyFile)
            {
                CompressionMethod = CompressionMethodValues.Stored;
                compressedSizeTruncated = 0;
                uncompressedSizeTruncated = 0;
                Debug.Assert(_compressedSize == 0);
                Debug.Assert(_uncompressedSize == 0);
                Debug.Assert(_crc32 == 0);
            }
            else
            {
                // if we have a non-seekable stream, don't worry about sizes at all, and just set the right bit
                // if we are using the data descriptor, then sizes and crc should be set to 0 in the header
                if (_archive.Mode == ZipArchiveMode.Create && !_archive.ArchiveStream.CanSeek)
                {
                    _generalPurposeBitFlag |= BitFlagValues.DataDescriptor;
                    compressedSizeTruncated = 0;
                    uncompressedSizeTruncated = 0;
                    // the crc should not have been set if we are in create mode, but clear it just to be sure
                    Debug.Assert(_crc32 == 0);
                }
                else // if we are not in streaming mode, we have to decide if we want to write zip64 headers
                {
                    // We are in seekable mode so we will not need to write a data descriptor
                    _generalPurposeBitFlag &= ~BitFlagValues.DataDescriptor;
                    if (ShouldUseZIP64
#if DEBUG_FORCE_ZIP64
                        || (_archive._forceZip64 && _archive.Mode == ZipArchiveMode.Update)
#endif
                        )
                    {
                        compressedSizeTruncated = ZipHelper.Mask32Bit;
                        uncompressedSizeTruncated = ZipHelper.Mask32Bit;

                        // prepare Zip64 extra field object. If we have one of the sizes, the other must go in there
                        zip64ExtraField = new()
                        {
                            CompressedSize = _compressedSize,
                            UncompressedSize = _uncompressedSize,
                        };

                        VersionToExtractAtLeast(ZipVersionNeededValues.Zip64);
                    }
                    else
                    {
                        compressedSizeTruncated = (uint)_compressedSize;
                        uncompressedSizeTruncated = (uint)_uncompressedSize;
                    }
                }
            }

            // save offset
            _offsetOfLocalHeader = _archive.ArchiveStream.Position;

            // calculate extra field. if zip64 stuff + original extraField aren't going to fit, dump the original extraField, because this is more important
            int currExtraFieldDataLength = ZipGenericExtraField.TotalSize(_lhUnknownExtraFields, _lhTrailingExtraFieldData?.Length ?? 0);
            int bigExtraFieldLength = (zip64ExtraField != null ? zip64ExtraField.TotalSize : 0)
                                      + currExtraFieldDataLength;

            if (bigExtraFieldLength > ushort.MaxValue)
            {
                extraFieldLength = (ushort)(zip64ExtraField != null ? zip64ExtraField.TotalSize : 0);
                _lhUnknownExtraFields = null;
            }
            else
            {
                extraFieldLength = (ushort)bigExtraFieldLength;
            }

            // If this is an existing, unchanged entry then silently skip forwards.
            // If it's new or changed, write the header.
            if (_originallyInArchive && Changes == ZipArchive.ChangeState.Unchanged && !forceWrite)
            {
                _archive.ArchiveStream.Seek(ZipLocalFileHeader.SizeOfLocalHeader + _storedEntryNameBytes.Length, SeekOrigin.Current);

                if (zip64ExtraField != null)
                {
                    _archive.ArchiveStream.Seek(zip64ExtraField.TotalSize, SeekOrigin.Current);
                }

                _archive.ArchiveStream.Seek(currExtraFieldDataLength, SeekOrigin.Current);

                return false;
            }

            return true;
        }

        private void WriteLocalFileHeaderPrepare(Span<byte> lfStaticHeader, uint compressedSizeTruncated, uint uncompressedSizeTruncated, ushort extraFieldLength)
        {
            ZipLocalFileHeader.SignatureConstantBytes.CopyTo(lfStaticHeader[ZipLocalFileHeader.FieldLocations.Signature..]);
            BinaryPrimitives.WriteUInt16LittleEndian(lfStaticHeader[ZipLocalFileHeader.FieldLocations.VersionNeededToExtract..], (ushort)_versionToExtract);
            BinaryPrimitives.WriteUInt16LittleEndian(lfStaticHeader[ZipLocalFileHeader.FieldLocations.GeneralPurposeBitFlags..], (ushort)_generalPurposeBitFlag);
            BinaryPrimitives.WriteUInt16LittleEndian(lfStaticHeader[ZipLocalFileHeader.FieldLocations.CompressionMethod..], (ushort)CompressionMethod);
            BinaryPrimitives.WriteUInt32LittleEndian(lfStaticHeader[ZipLocalFileHeader.FieldLocations.LastModified..], ZipHelper.DateTimeToDosTime(_lastModified.DateTime));
            BinaryPrimitives.WriteUInt32LittleEndian(lfStaticHeader[ZipLocalFileHeader.FieldLocations.Crc32..], _crc32);
            BinaryPrimitives.WriteUInt32LittleEndian(lfStaticHeader[ZipLocalFileHeader.FieldLocations.CompressedSize..], compressedSizeTruncated);
            BinaryPrimitives.WriteUInt32LittleEndian(lfStaticHeader[ZipLocalFileHeader.FieldLocations.UncompressedSize..], uncompressedSizeTruncated);
            BinaryPrimitives.WriteUInt16LittleEndian(lfStaticHeader[ZipLocalFileHeader.FieldLocations.FilenameLength..], (ushort)_storedEntryNameBytes.Length);
            BinaryPrimitives.WriteUInt16LittleEndian(lfStaticHeader[ZipLocalFileHeader.FieldLocations.ExtraFieldLength..], extraFieldLength);
        }

        // return value is true if we allocated an extra field for 64 bit headers, un/compressed size
        private bool WriteLocalFileHeader(bool isEmptyFile, bool forceWrite)
        {
            if (WriteLocalFileHeaderInitialize(isEmptyFile, forceWrite, out Zip64ExtraField? zip64ExtraField, out uint compressedSizeTruncated, out uint uncompressedSizeTruncated, out ushort extraFieldLength))
            {
                Span<byte> lfStaticHeader = stackalloc byte[ZipLocalFileHeader.SizeOfLocalHeader];
                WriteLocalFileHeaderPrepare(lfStaticHeader, compressedSizeTruncated, uncompressedSizeTruncated, extraFieldLength);

                // write header
                _archive.ArchiveStream.Write(lfStaticHeader);
                _archive.ArchiveStream.Write(_storedEntryNameBytes);

                // Only when handling zip64
                zip64ExtraField?.WriteBlock(_archive.ArchiveStream);

                ZipGenericExtraField.WriteAllBlocks(_lhUnknownExtraFields, _lhTrailingExtraFieldData ?? Array.Empty<byte>(), _archive.ArchiveStream);
            }

            return zip64ExtraField != null;
        }

        private void WriteLocalFileHeaderAndDataIfNeeded(bool forceWrite)
        {
            // _storedUncompressedData gets frozen here, and is what gets written to the file
            if (_storedUncompressedData != null || _compressedBytes != null)
            {
                if (_storedUncompressedData != null)
                {
                    _uncompressedSize = _storedUncompressedData.Length;

                    //The compressor fills in CRC and sizes
                    //The DirectToArchiveWriterStream writes headers and such
                    using (DirectToArchiveWriterStream entryWriter = new(
                                                    GetDataCompressor(_archive.ArchiveStream, true, null),
                                                    this))
                    {
                        _storedUncompressedData.Seek(0, SeekOrigin.Begin);
                        _storedUncompressedData.CopyTo(entryWriter);
                        _storedUncompressedData.Dispose();
                        _storedUncompressedData = null;
                    }
                }
                else
                {
                    if (_uncompressedSize == 0)
                    {
                        // reset size to ensure proper central directory size header
                        _compressedSize = 0;
                    }

                    WriteLocalFileHeader(isEmptyFile: _uncompressedSize == 0, forceWrite: true);

                    // according to ZIP specs, zero-byte files MUST NOT include file data
                    if (_uncompressedSize != 0)
                    {
                        Debug.Assert(_compressedBytes != null);
                        foreach (byte[] compressedBytes in _compressedBytes)
                        {
                            _archive.ArchiveStream.Write(compressedBytes, 0, compressedBytes.Length);
                        }
                    }
                }
            }
            else // there is no data in the file (or the data in the file has not been loaded), but if we are in update mode, we may still need to write a header
            {
                if (_archive.Mode == ZipArchiveMode.Update || !_everOpenedForWrite)
                {
                    _everOpenedForWrite = true;
                    WriteLocalFileHeader(isEmptyFile: _uncompressedSize == 0, forceWrite: forceWrite);

                    // If we know that we need to update the file header (but don't need to load and update the data itself)
                    // then advance the position past it.
                    if (_compressedSize != 0)
                    {
                        _archive.ArchiveStream.Seek(_compressedSize, SeekOrigin.Current);
                    }
                }
            }
        }

        private const int MetadataBufferLength = ZipLocalFileHeader.FieldLengths.VersionNeededToExtract + ZipLocalFileHeader.FieldLengths.GeneralPurposeBitFlags;
        private const int CrcAndSizesBufferLength = ZipLocalFileHeader.FieldLengths.Crc32 + ZipLocalFileHeader.FieldLengths.CompressedSize + ZipLocalFileHeader.FieldLengths.UncompressedSize;
        private const int Zip64SizesBufferLength = Zip64ExtraField.FieldLengths.UncompressedSize + Zip64ExtraField.FieldLengths.CompressedSize;
        private const int Zip64DataDescriptorCrcAndSizesBufferLength = ZipLocalFileHeader.Zip64DataDescriptor.FieldLengths.Crc32
            + ZipLocalFileHeader.Zip64DataDescriptor.FieldLengths.CompressedSize + ZipLocalFileHeader.Zip64DataDescriptor.FieldLengths.UncompressedSize;

        // Using _offsetOfLocalHeader, seeks back to where CRC and sizes should be in the header,
        // writes them, then seeks back to where you started
        // Assumes that the stream is currently at the end of the data
        private void WriteCrcAndSizesInLocalHeader(bool zip64HeaderUsed)
        {
            // Buffer has been sized to the largest data payload required: the 64-bit data descriptor.
            Span<byte> writeBuffer = stackalloc byte[Zip64DataDescriptorCrcAndSizesBufferLength];

            WriteCrcAndSizesInLocalHeaderInitialize(zip64HeaderUsed, out long finalPosition, out bool pretendStreaming, out uint compressedSizeTruncated, out uint uncompressedSizeTruncated);

            // first step is, if we need zip64, but didn't allocate it, pretend we did a stream write, because
            // we can't go back and give ourselves the space that the extra field needs.
            // we do this by setting the correct property in the bit flag to indicate we have a data descriptor
            // and setting the version to Zip64 to indicate that descriptor contains 64-bit values
            if (pretendStreaming)
            {
                WriteCrcAndSizesInLocalHeaderPrepareForZip64PretendStreaming(writeBuffer);
                _archive.ArchiveStream.Write(writeBuffer[..MetadataBufferLength]);
            }

            // next step is fill out the 32-bit size values in the normal header. we can't assume that
            // they are correct. we also write the CRC
            WriteCrcAndSizesInLocalHeaderPrepareFor32bitValuesWriting(pretendStreaming, writeBuffer, compressedSizeTruncated, uncompressedSizeTruncated);
            _archive.ArchiveStream.Write(writeBuffer[..CrcAndSizesBufferLength]);

            // next step: if we wrote the 64 bit header initially, a different implementation might
            // try to read it, even if the 32-bit size values aren't masked. thus, we should always put the
            // correct size information in there. note that order of uncomp/comp is switched, and these are
            // 64-bit values
            // also, note that in order for this to be correct, we have to ensure that the zip64 extra field
            // is always the first extra field that is written
            if (zip64HeaderUsed)
            {
                WriteCrcAndSizesInLocalHeaderPrepareForWritingWhenZip64HeaderUsed(writeBuffer);
                _archive.ArchiveStream.Write(writeBuffer[..Zip64SizesBufferLength]);
            }

            // now go to the where we were. assume that this is the end of the data
            _archive.ArchiveStream.Seek(finalPosition, SeekOrigin.Begin);

            // if we are pretending we did a stream write, we want to write the data descriptor out
            // the data descriptor can have 32-bit sizes or 64-bit sizes. In this case, we always use
            // 64-bit sizes
            if (pretendStreaming)
            {
                WriteCrcAndSizesInLocalHeaderPrepareForWritingDataDescriptor(writeBuffer);
                _archive.ArchiveStream.Write(writeBuffer[..Zip64DataDescriptorCrcAndSizesBufferLength]);
            }
        }

        private void WriteCrcAndSizesInLocalHeaderInitialize(bool zip64HeaderUsed, out long finalPosition, out bool pretendStreaming, out uint compressedSizeTruncated, out uint uncompressedSizeTruncated)
        {
            finalPosition = _archive.ArchiveStream.Position;

            bool zip64Needed = ShouldUseZIP64
#if DEBUG_FORCE_ZIP64
                || _archive._forceZip64
#endif
            ;

            pretendStreaming = zip64Needed && !zip64HeaderUsed;
            compressedSizeTruncated = zip64Needed ? ZipHelper.Mask32Bit : (uint)_compressedSize;
            uncompressedSizeTruncated = zip64Needed ? ZipHelper.Mask32Bit : (uint)_uncompressedSize;
        }

        private void WriteCrcAndSizesInLocalHeaderPrepareForZip64PretendStreaming(Span<byte> writeBuffer)
        {
            int relativeVersionToExtractLocation = ZipLocalFileHeader.FieldLocations.VersionNeededToExtract - ZipLocalFileHeader.FieldLocations.VersionNeededToExtract;
            int relativeGeneralPurposeBitFlagsLocation = ZipLocalFileHeader.FieldLocations.GeneralPurposeBitFlags - ZipLocalFileHeader.FieldLocations.VersionNeededToExtract;

            VersionToExtractAtLeast(ZipVersionNeededValues.Zip64);
            _generalPurposeBitFlag |= BitFlagValues.DataDescriptor;

            _archive.ArchiveStream.Seek(_offsetOfLocalHeader + ZipLocalFileHeader.FieldLocations.VersionNeededToExtract,
                                        SeekOrigin.Begin);
            BinaryPrimitives.WriteUInt16LittleEndian(writeBuffer[relativeVersionToExtractLocation..], (ushort)_versionToExtract);
            BinaryPrimitives.WriteUInt16LittleEndian(writeBuffer[relativeGeneralPurposeBitFlagsLocation..], (ushort)_generalPurposeBitFlag);
        }

        private void WriteCrcAndSizesInLocalHeaderPrepareFor32bitValuesWriting(bool pretendStreaming, Span<byte> writeBuffer, uint compressedSizeTruncated, uint uncompressedSizeTruncated)
        {
            _archive.ArchiveStream.Seek(_offsetOfLocalHeader + ZipLocalFileHeader.FieldLocations.Crc32,
                                            SeekOrigin.Begin);
            if (!pretendStreaming)
            {
                int relativeCrc32Location = ZipLocalFileHeader.FieldLocations.Crc32 - ZipLocalFileHeader.FieldLocations.Crc32;
                int relativeCompressedSizeLocation = ZipLocalFileHeader.FieldLocations.CompressedSize - ZipLocalFileHeader.FieldLocations.Crc32;
                int relativeUncompressedSizeLocation = ZipLocalFileHeader.FieldLocations.UncompressedSize - ZipLocalFileHeader.FieldLocations.Crc32;

                BinaryPrimitives.WriteUInt32LittleEndian(writeBuffer[relativeCrc32Location..], _crc32);
                BinaryPrimitives.WriteUInt32LittleEndian(writeBuffer[relativeCompressedSizeLocation..], compressedSizeTruncated);
                BinaryPrimitives.WriteUInt32LittleEndian(writeBuffer[relativeUncompressedSizeLocation..], uncompressedSizeTruncated);
            }
            else // but if we are pretending to stream, we want to fill in with zeroes
            {
                writeBuffer[..CrcAndSizesBufferLength].Clear();
            }
        }

        private void WriteCrcAndSizesInLocalHeaderPrepareForWritingWhenZip64HeaderUsed(Span<byte> writeBuffer)
        {
            int relativeUncompressedSizeLocation = Zip64ExtraField.FieldLocations.UncompressedSize - Zip64ExtraField.FieldLocations.UncompressedSize;
            int relativeCompressedSizeLocation = Zip64ExtraField.FieldLocations.CompressedSize - Zip64ExtraField.FieldLocations.UncompressedSize;

            _archive.ArchiveStream.Seek(_offsetOfLocalHeader + ZipLocalFileHeader.SizeOfLocalHeader
                                        + _storedEntryNameBytes.Length + Zip64ExtraField.OffsetToFirstField,
                                        SeekOrigin.Begin);
            BinaryPrimitives.WriteInt64LittleEndian(writeBuffer[relativeUncompressedSizeLocation..], _uncompressedSize);
            BinaryPrimitives.WriteInt64LittleEndian(writeBuffer[relativeCompressedSizeLocation..], _compressedSize);
        }

        private void WriteCrcAndSizesInLocalHeaderPrepareForWritingDataDescriptor(Span<byte> writeBuffer)
        {
            int relativeCrc32Location = ZipLocalFileHeader.Zip64DataDescriptor.FieldLocations.Crc32 - ZipLocalFileHeader.Zip64DataDescriptor.FieldLocations.Crc32;
            int relativeCompressedSizeLocation = ZipLocalFileHeader.Zip64DataDescriptor.FieldLocations.CompressedSize - ZipLocalFileHeader.Zip64DataDescriptor.FieldLocations.Crc32;
            int relativeUncompressedSizeLocation = ZipLocalFileHeader.Zip64DataDescriptor.FieldLocations.UncompressedSize - ZipLocalFileHeader.Zip64DataDescriptor.FieldLocations.Crc32;

            BinaryPrimitives.WriteUInt32LittleEndian(writeBuffer.Slice(relativeCrc32Location), _crc32);
            BinaryPrimitives.WriteInt64LittleEndian(writeBuffer.Slice(relativeCompressedSizeLocation), _compressedSize);
            BinaryPrimitives.WriteInt64LittleEndian(writeBuffer.Slice(relativeUncompressedSizeLocation), _uncompressedSize);

        }

        // data descriptor can be 32-bit or 64-bit sizes. 32-bit is more compatible, so use that if possible
        // signature is optional but recommended by the spec
        private const int MaxSizeOfDataDescriptor = 24;

        private void WriteDataDescriptor()
        {
            Span<byte> dataDescriptor = stackalloc byte[MaxSizeOfDataDescriptor];
            int bytesToWrite = PrepareToWriteDataDescriptor(dataDescriptor);
            _archive.ArchiveStream.Write(dataDescriptor[..bytesToWrite]);
        }

        private int PrepareToWriteDataDescriptor(Span<byte> dataDescriptor)
        {
            // We enter here because we cannot seek, so the data descriptor bit should be on
            Debug.Assert((_generalPurposeBitFlag & BitFlagValues.DataDescriptor) != 0);

            int bytesToWrite;

            ZipLocalFileHeader.DataDescriptorSignatureConstantBytes.CopyTo(dataDescriptor[ZipLocalFileHeader.ZipDataDescriptor.FieldLocations.Signature..]);
            BinaryPrimitives.WriteUInt32LittleEndian(dataDescriptor[ZipLocalFileHeader.ZipDataDescriptor.FieldLocations.Crc32..], _crc32);

            if (AreSizesTooLarge)
            {
                BinaryPrimitives.WriteInt64LittleEndian(dataDescriptor[ZipLocalFileHeader.Zip64DataDescriptor.FieldLocations.CompressedSize..], _compressedSize);
                BinaryPrimitives.WriteInt64LittleEndian(dataDescriptor[ZipLocalFileHeader.Zip64DataDescriptor.FieldLocations.UncompressedSize..], _uncompressedSize);

                bytesToWrite = ZipLocalFileHeader.Zip64DataDescriptor.FieldLocations.UncompressedSize + ZipLocalFileHeader.Zip64DataDescriptor.FieldLengths.UncompressedSize;
            }
            else
            {
                BinaryPrimitives.WriteUInt32LittleEndian(dataDescriptor[ZipLocalFileHeader.ZipDataDescriptor.FieldLocations.CompressedSize..], (uint)_compressedSize);
                BinaryPrimitives.WriteUInt32LittleEndian(dataDescriptor[ZipLocalFileHeader.ZipDataDescriptor.FieldLocations.UncompressedSize..], (uint)_uncompressedSize);

                bytesToWrite = ZipLocalFileHeader.ZipDataDescriptor.FieldLocations.UncompressedSize + ZipLocalFileHeader.ZipDataDescriptor.FieldLengths.UncompressedSize;
            }

            return bytesToWrite;
        }

        private void UnloadStreams()
        {
            _storedUncompressedData?.Dispose();
            _compressedBytes = null;
            _outstandingWriteStream = null;
        }

        private void CloseStreams()
        {
            // if the user left the stream open, close the underlying stream for them
            _outstandingWriteStream?.Dispose();
        }

        private void VersionToExtractAtLeast(ZipVersionNeededValues value)
        {
            if (_versionToExtract < value)
            {
                _versionToExtract = value;
                Changes |= ZipArchive.ChangeState.FixedLengthMetadata;
            }
            if (_versionMadeBySpecification < value)
            {
                _versionMadeBySpecification = value;
                Changes |= ZipArchive.ChangeState.FixedLengthMetadata;
            }
        }

        private void ThrowIfInvalidArchive()
        {
            if (_archive == null)
                throw new InvalidOperationException(SR.DeletedEntry);
            _archive.ThrowIfDisposed();
        }

        /// <summary>
        /// Gets the file name of the path based on Windows path separator characters
        /// </summary>
        private static string GetFileName_Windows(string path)
        {
            int i = path.AsSpan().LastIndexOfAny('\\', '/', ':');
            return i >= 0 ?
                path.Substring(i + 1) :
                path;
        }

        /// <summary>
        /// Gets the file name of the path based on Unix path separator characters
        /// </summary>
        private static string GetFileName_Unix(string path)
        {
            int i = path.LastIndexOf('/');
            return i >= 0 ?
                path.Substring(i + 1) :
                path;
        }

        private sealed class DirectToArchiveWriterStream : Stream
        {
            private long _position;
            private readonly CheckSumAndSizeWriteStream _crcSizeStream;
            private bool _everWritten;
            private bool _isDisposed;
            private readonly ZipArchiveEntry _entry;
            private bool _usedZip64inLH;
            private bool _canWrite;

            // makes the assumption that somewhere down the line, crcSizeStream is eventually writing directly to the archive
            // this class calls other functions on ZipArchiveEntry that write directly to the archive
            public DirectToArchiveWriterStream(CheckSumAndSizeWriteStream crcSizeStream, ZipArchiveEntry entry)
            {
                _position = 0;
                _crcSizeStream = crcSizeStream;
                _everWritten = false;
                _isDisposed = false;
                _entry = entry;
                _usedZip64inLH = false;
                _canWrite = true;
            }

            public override long Length
            {
                get
                {
                    ThrowIfDisposed();
                    throw new NotSupportedException(SR.SeekingNotSupported);
                }
            }
            public override long Position
            {
                get
                {
                    ThrowIfDisposed();
                    return _position;
                }
                set
                {
                    ThrowIfDisposed();
                    throw new NotSupportedException(SR.SeekingNotSupported);
                }
            }

            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => _canWrite;

            private void ThrowIfDisposed()
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().ToString(), SR.HiddenStreamName);
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                ThrowIfDisposed();
                throw new NotSupportedException(SR.ReadingNotSupported);
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                ThrowIfDisposed();
                throw new NotSupportedException(SR.ReadingNotSupported);
            }

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                ThrowIfDisposed();
                throw new NotSupportedException(SR.ReadingNotSupported);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                ThrowIfDisposed();
                throw new NotSupportedException(SR.SeekingNotSupported);
            }

            public override void SetLength(long value)
            {
                ThrowIfDisposed();
                throw new NotSupportedException(SR.SetLengthRequiresSeekingAndWriting);
            }

            // careful: assumes that write is the only way to write to the stream, if writebyte/beginwrite are implemented
            // they must set _everWritten, etc.
            public override void Write(byte[] buffer, int offset, int count)
            {
                ValidateBufferArguments(buffer, offset, count);

                ThrowIfDisposed();
                Debug.Assert(CanWrite);

                // if we're not actually writing anything, we don't want to trigger the header
                if (count == 0)
                    return;

                if (!_everWritten)
                {
                    _everWritten = true;
                    // write local header, we are good to go
                    _usedZip64inLH = _entry.WriteLocalFileHeader(isEmptyFile: false, forceWrite: true);
                }

                _crcSizeStream.Write(buffer, offset, count);
                _position += count;
            }

            public override void Write(ReadOnlySpan<byte> source)
            {
                ThrowIfDisposed();
                Debug.Assert(CanWrite);

                // if we're not actually writing anything, we don't want to trigger the header
                if (source.Length == 0)
                    return;

                if (!_everWritten)
                {
                    _everWritten = true;
                    // write local header, we are good to go
                    _usedZip64inLH = _entry.WriteLocalFileHeader(isEmptyFile: false, forceWrite: true);
                }

                _crcSizeStream.Write(source);
                _position += source.Length;
            }

            public override void WriteByte(byte value) =>
                Write(new ReadOnlySpan<byte>(in value));

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                ValidateBufferArguments(buffer, offset, count);
                return WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
            }

            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                ThrowIfDisposed();
                Debug.Assert(CanWrite);

                return !buffer.IsEmpty ?
                    Core(buffer, cancellationToken) :
                    default;

                async ValueTask Core(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
                {
                    if (!_everWritten)
                    {
                        _everWritten = true;
                        // write local header, we are good to go
                        _usedZip64inLH = await _entry.WriteLocalFileHeaderAsync(isEmptyFile: false, forceWrite: true, cancellationToken).ConfigureAwait(false);
                    }

                    await _crcSizeStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
                    _position += buffer.Length;
                }
            }

            public override void Flush()
            {
                ThrowIfDisposed();
                Debug.Assert(CanWrite);

                _crcSizeStream.Flush();
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                ThrowIfDisposed();
                Debug.Assert(CanWrite);

                return _crcSizeStream.FlushAsync(cancellationToken);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing && !_isDisposed)
                {
                    _crcSizeStream.Dispose(); // now we have size/crc info

                    if (!_everWritten)
                    {
                        // write local header, no data, so we use stored
                        _entry.WriteLocalFileHeader(isEmptyFile: true, forceWrite: true);
                    }
                    else
                    {
                        // go back and finish writing
                        if (_entry._archive.ArchiveStream.CanSeek)
                            // finish writing local header if we have seek capabilities
                            _entry.WriteCrcAndSizesInLocalHeader(_usedZip64inLH);
                        else
                            // write out data descriptor if we don't have seek capabilities
                            _entry.WriteDataDescriptor();
                    }
                    _canWrite = false;
                    _isDisposed = true;
                }

                base.Dispose(disposing);
            }

            public override async ValueTask DisposeAsync()
            {
                if (!_isDisposed)
                {
                    await _crcSizeStream.DisposeAsync().ConfigureAwait(false); // now we have size/crc info

                    if (!_everWritten)
                    {
                        // write local header, no data, so we use stored
                        await _entry.WriteLocalFileHeaderAsync(isEmptyFile: true, forceWrite: true, cancellationToken: default).ConfigureAwait(false);
                    }
                    else
                    {
                        // go back and finish writing
                        if (_entry._archive.ArchiveStream.CanSeek)
                            // finish writing local header if we have seek capabilities
                            await _entry.WriteCrcAndSizesInLocalHeaderAsync(_usedZip64inLH, cancellationToken: default).ConfigureAwait(false);
                        else
                            // write out data descriptor if we don't have seek capabilities
                            await _entry.WriteDataDescriptorAsync(cancellationToken: default).ConfigureAwait(false);
                    }
                    _canWrite = false;
                    _isDisposed = true;
                }

                await base.DisposeAsync().ConfigureAwait(false);
            }
        }

        [Flags]
        internal enum BitFlagValues : ushort
        {
            IsEncrypted = 0x1,
            DataDescriptor = 0x8,
            UnicodeFileNameAndComment = 0x800
        }

        internal enum CompressionMethodValues : ushort
        {
            Stored = 0x0,
            Deflate = 0x8,
            Deflate64 = 0x9,
            BZip2 = 0xC,
            LZMA = 0xE
        }

        internal sealed class LocalHeaderOffsetComparer : Comparer<ZipArchiveEntry>
        {
            private static readonly LocalHeaderOffsetComparer s_instance = new LocalHeaderOffsetComparer();

            public static LocalHeaderOffsetComparer Instance => s_instance;

            // Newly added ZipArchiveEntry records should always go to the end of the file.
            public override int Compare(ZipArchiveEntry? x, ZipArchiveEntry? y)
            {
                long xOffset = x != null && !x.OriginallyInArchive ? long.MaxValue : x?.OffsetOfLocalHeader ?? long.MinValue;
                long yOffset = y != null && !y.OriginallyInArchive ? long.MaxValue : y?.OffsetOfLocalHeader ?? long.MinValue;

                return xOffset.CompareTo(yOffset);
            }
        }
    }
}
