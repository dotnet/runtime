// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression;

// The disposable fields that this class owns get disposed when the ZipArchive it belongs to gets disposed
public partial class ZipArchiveEntry
{
    /// <summary>
    /// Asynchronously opens the entry. If the archive that the entry belongs to was opened in Read mode, the returned stream will be readable, and it may or may not be seekable. If Create mode, the returned stream will be writable and not seekable. If Update mode, the returned stream will be readable, writable, seekable, and support SetLength.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A Stream that represents the contents of the entry.</returns>
    /// <exception cref="IOException">The entry is already currently open for writing. -or- The entry has been deleted from the archive. -or- The archive that this entry belongs to was opened in ZipArchiveMode.Create, and this entry has already been written to once.</exception>
    /// <exception cref="InvalidDataException">The entry is missing from the archive or is corrupt and cannot be read. -or- The entry has been compressed using a compression method that is not supported.</exception>
    /// <exception cref="ObjectDisposedException">The ZipArchive that this entry belongs to has been disposed.</exception>
    public async Task<Stream> OpenAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfInvalidArchive();

        switch (_archive.Mode)
        {
            case ZipArchiveMode.Read:
                return await OpenInReadModeAsync(checkOpenable: true, cancellationToken).ConfigureAwait(false);
            case ZipArchiveMode.Create:
                return OpenInWriteMode();
            case ZipArchiveMode.Update:
            default:
                Debug.Assert(_archive.Mode == ZipArchiveMode.Update);
                return await OpenInUpdateModeAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    internal async Task<long> GetOffsetOfCompressedDataAsync(CancellationToken cancellationToken)
    {
        if (_storedOffsetOfCompressedData == null)
        {
            _archive.ArchiveStream.Seek(_offsetOfLocalHeader, SeekOrigin.Begin);
            // by calling this, we are using local header _storedEntryNameBytes.Length and extraFieldLength
            // to find start of data, but still using central directory size information
            if (!await ZipLocalFileHeader.TrySkipBlockAsync(_archive.ArchiveStream, cancellationToken).ConfigureAwait(false))
                throw new InvalidDataException(SR.LocalFileHeaderCorrupt);
            _storedOffsetOfCompressedData = _archive.ArchiveStream.Position;
        }
        return _storedOffsetOfCompressedData.Value;
    }

    private async Task<MemoryStream> GetUncompressedDataAsync(CancellationToken cancellationToken)
    {
        if (_storedUncompressedData == null)
        {
            // this means we have never opened it before

            // if _uncompressedSize > int.MaxValue, it's still okay, because MemoryStream will just
            // grow as data is copied into it
            _storedUncompressedData = new MemoryStream((int)_uncompressedSize);

            if (_originallyInArchive)
            {
                Stream decompressor = await OpenInReadModeAsync(false, cancellationToken).ConfigureAwait(false);
                await using (decompressor)
                {
                    try
                    {
                        await decompressor.CopyToAsync(_storedUncompressedData, cancellationToken).ConfigureAwait(false);
                    }
                    catch (InvalidDataException)
                    {
                        // this is the case where the archive say the entry is deflate, but deflateStream
                        // throws an InvalidDataException. This property should only be getting accessed in
                        // Update mode, so we want to make sure _storedUncompressedData stays null so
                        // that later when we dispose the archive, this entry loads the compressedBytes, and
                        // copies them straight over
                        await _storedUncompressedData.DisposeAsync().ConfigureAwait(false);
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

    private async Task CloseStreamsAsync()
    {
        // if the user left the stream open, close the underlying stream for them
        if (_outstandingWriteStream != null)
        {
            await _outstandingWriteStream.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task UnloadStreamsAsync()
    {
        if (_storedUncompressedData != null)
        {
            await _storedUncompressedData.DisposeAsync().ConfigureAwait(false);
        }
        _compressedBytes = null;
        _outstandingWriteStream = null;
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
    internal async Task WriteAndFinishLocalEntryAsync(bool forceWrite, CancellationToken cancellationToken)
    {
        await CloseStreamsAsync().ConfigureAwait(false);
        await WriteLocalFileHeaderAndDataIfNeededAsync(forceWrite, cancellationToken).ConfigureAwait(false);
        await UnloadStreamsAsync().ConfigureAwait(false);
    }

    // should only throw an exception in extremely exceptional cases because it is called from dispose
    internal async Task WriteCentralDirectoryFileHeaderAsync(bool forceWrite, CancellationToken cancellationToken)
    {
        // This part is simple, because we should definitely know the sizes by this time

        // _storedEntryNameBytes only gets set when we read in or call moveTo. MoveTo does a check, and
        // reading in should not be able to produce an entryname longer than ushort.MaxValue
        // _fileComment only gets set when we read in or set the FileComment property. This performs its own
        // length check.
        Debug.Assert(_storedEntryNameBytes.Length <= ushort.MaxValue);
        Debug.Assert(_fileComment.Length <= ushort.MaxValue);

        // decide if we need the Zip64 extra field:
        Zip64ExtraField? zip64ExtraField = null;
        uint compressedSizeTruncated, uncompressedSizeTruncated, offsetOfLocalHeaderTruncated;

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
        int bigExtraFieldLength = (zip64ExtraField != null ? zip64ExtraField.TotalSize : 0)
                                  + (_cdUnknownExtraFields != null ? ZipGenericExtraField.TotalSize(_cdUnknownExtraFields) : 0);
        ushort extraFieldLength;
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
                + (_cdUnknownExtraFields != null ? ZipGenericExtraField.TotalSize(_cdUnknownExtraFields) : 0)
                + _fileComment.Length;

            _archive.ArchiveStream.Seek(centralDirectoryHeaderLength, SeekOrigin.Current);
        }
        else
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
            byte[] cdStaticHeader = new byte[ZipCentralDirectoryFileHeader.BlockConstantSectionSize];

            ZipCentralDirectoryFileHeader.SignatureConstantBytes.CopyTo(cdStaticHeader.AsSpan(ZipCentralDirectoryFileHeader.FieldLocations.Signature));
            cdStaticHeader[ZipCentralDirectoryFileHeader.FieldLocations.VersionMadeBySpecification] = (byte)_versionMadeBySpecification;
            cdStaticHeader[ZipCentralDirectoryFileHeader.FieldLocations.VersionMadeByCompatibility] = (byte)CurrentZipPlatform;
            BinaryPrimitives.WriteUInt16LittleEndian(cdStaticHeader.AsSpan(ZipCentralDirectoryFileHeader.FieldLocations.VersionNeededToExtract), (ushort)_versionToExtract);
            BinaryPrimitives.WriteUInt16LittleEndian(cdStaticHeader.AsSpan(ZipCentralDirectoryFileHeader.FieldLocations.GeneralPurposeBitFlags), (ushort)_generalPurposeBitFlag);
            BinaryPrimitives.WriteUInt16LittleEndian(cdStaticHeader.AsSpan(ZipCentralDirectoryFileHeader.FieldLocations.CompressionMethod), (ushort)CompressionMethod);
            BinaryPrimitives.WriteUInt32LittleEndian(cdStaticHeader.AsSpan(ZipCentralDirectoryFileHeader.FieldLocations.LastModified), ZipHelper.DateTimeToDosTime(_lastModified.DateTime));
            BinaryPrimitives.WriteUInt32LittleEndian(cdStaticHeader.AsSpan(ZipCentralDirectoryFileHeader.FieldLocations.Crc32), _crc32);
            BinaryPrimitives.WriteUInt32LittleEndian(cdStaticHeader.AsSpan(ZipCentralDirectoryFileHeader.FieldLocations.CompressedSize), compressedSizeTruncated);
            BinaryPrimitives.WriteUInt32LittleEndian(cdStaticHeader.AsSpan(ZipCentralDirectoryFileHeader.FieldLocations.UncompressedSize), uncompressedSizeTruncated);
            BinaryPrimitives.WriteUInt16LittleEndian(cdStaticHeader.AsSpan(ZipCentralDirectoryFileHeader.FieldLocations.FilenameLength), (ushort)_storedEntryNameBytes.Length);
            BinaryPrimitives.WriteUInt16LittleEndian(cdStaticHeader.AsSpan(ZipCentralDirectoryFileHeader.FieldLocations.ExtraFieldLength), extraFieldLength);
            BinaryPrimitives.WriteUInt16LittleEndian(cdStaticHeader.AsSpan(ZipCentralDirectoryFileHeader.FieldLocations.FileCommentLength), (ushort)_fileComment.Length);
            BinaryPrimitives.WriteUInt16LittleEndian(cdStaticHeader.AsSpan(ZipCentralDirectoryFileHeader.FieldLocations.DiskNumberStart), 0);
            BinaryPrimitives.WriteUInt16LittleEndian(cdStaticHeader.AsSpan(ZipCentralDirectoryFileHeader.FieldLocations.InternalFileAttributes), 0);
            BinaryPrimitives.WriteUInt32LittleEndian(cdStaticHeader.AsSpan(ZipCentralDirectoryFileHeader.FieldLocations.ExternalFileAttributes), _externalFileAttr);
            BinaryPrimitives.WriteUInt32LittleEndian(cdStaticHeader.AsSpan(ZipCentralDirectoryFileHeader.FieldLocations.RelativeOffsetOfLocalHeader), offsetOfLocalHeaderTruncated);

            await _archive.ArchiveStream.WriteAsync(cdStaticHeader, cancellationToken).ConfigureAwait(false);
            await _archive.ArchiveStream.WriteAsync(_storedEntryNameBytes, cancellationToken).ConfigureAwait(false);

            // write extra fields, and only write zip64ExtraField if we decided we need it (it's not null)
            if (zip64ExtraField != null)
            {
                await zip64ExtraField.WriteBlockAsync(_archive.ArchiveStream, cancellationToken).ConfigureAwait(false);
            }

            if (_cdUnknownExtraFields != null)
            {
                ZipGenericExtraField.WriteAllBlocks(_cdUnknownExtraFields, _archive.ArchiveStream);
            }

            if (_fileComment.Length > 0)
            {
                await _archive.ArchiveStream.WriteAsync(_fileComment, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    // throws exception if fails, will get called on every relevant entry before closing in update mode
    // can throw InvalidDataException
    internal async Task LoadLocalHeaderExtraFieldAndCompressedBytesIfNeededAsync(CancellationToken cancellationToken)
    {
        // we should have made this exact call in _archive.Init through ThrowIfOpenable
        (bool result, _) = await IsOpenableAsync(false, true, cancellationToken).ConfigureAwait(false);
        Debug.Assert(result);

        // load local header's extra fields. it will be null if we couldn't read for some reason
        if (_originallyInArchive)
        {
            _archive.ArchiveStream.Seek(_offsetOfLocalHeader, SeekOrigin.Begin);
            _lhUnknownExtraFields = await ZipLocalFileHeader.GetExtraFieldsAsync(_archive.ArchiveStream, cancellationToken).ConfigureAwait(false);
        }

        if (!_everOpenedForWrite && _originallyInArchive)
        {
            // we know that it is openable at this point
            int MaxSingleBufferSize = Array.MaxLength;

            _compressedBytes = new byte[(_compressedSize / MaxSingleBufferSize) + 1][];
            for (int i = 0; i < _compressedBytes.Length - 1; i++)
            {
                _compressedBytes[i] = new byte[MaxSingleBufferSize];
            }
            _compressedBytes[_compressedBytes.Length - 1] = new byte[_compressedSize % MaxSingleBufferSize];

            long offsetOfCompressedData = await GetOffsetOfCompressedDataAsync(cancellationToken).ConfigureAwait(false);
            _archive.ArchiveStream.Seek(offsetOfCompressedData, SeekOrigin.Begin);

            for (int i = 0; i < _compressedBytes.Length - 1; i++)
            {
                await ZipHelper.ReadBytesAsync(_archive.ArchiveStream, _compressedBytes[i], MaxSingleBufferSize, cancellationToken).ConfigureAwait(false);
            }
            await ZipHelper.ReadBytesAsync(_archive.ArchiveStream, _compressedBytes[_compressedBytes.Length - 1], (int)(_compressedSize % MaxSingleBufferSize), cancellationToken).ConfigureAwait(false);
        }
    }

    internal async Task ThrowIfNotOpenableAsync(bool needToUncompress, bool needToLoadIntoMemory, CancellationToken cancellationToken)
    {
        (bool openable, string? message) = await IsOpenableAsync(needToUncompress, needToLoadIntoMemory, cancellationToken).ConfigureAwait(false);
        if (!openable)
            throw new InvalidDataException(message);
    }

    private async Task<Stream> OpenInReadModeAsync(bool checkOpenable, CancellationToken cancellationToken)
    {
        if (checkOpenable)
            await ThrowIfNotOpenableAsync(needToUncompress: true, needToLoadIntoMemory: false, cancellationToken).ConfigureAwait(false);

        long offsetOfCompressedData = await GetOffsetOfCompressedDataAsync(cancellationToken).ConfigureAwait(false);
        Stream compressedStream = new SubReadStream(_archive.ArchiveStream, offsetOfCompressedData, _compressedSize);
        return GetDataDecompressor(compressedStream);
    }

    private async Task<WrappedStream> OpenInUpdateModeAsync(CancellationToken cancellationToken)
    {
        if (_currentlyOpenForWrite)
            throw new IOException(SR.UpdateModeOneStream);

        await ThrowIfNotOpenableAsync(needToUncompress: true, needToLoadIntoMemory: true, cancellationToken).ConfigureAwait(false);

        _everOpenedForWrite = true;
        Changes |= ZipArchive.ChangeState.StoredData;
        _currentlyOpenForWrite = true;
        // always put it at the beginning for them
        Stream uncompressedData = await GetUncompressedDataAsync(cancellationToken).ConfigureAwait(false);
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

    private async Task<(bool, string)> IsOpenableAsync(bool needToUncompress, bool needToLoadIntoMemory, CancellationToken cancellationToken)
    {
        string message = string.Empty;

        if (_originallyInArchive)
        {
            if (needToUncompress)
            {
                if (CompressionMethod != CompressionMethodValues.Stored &&
                    CompressionMethod != CompressionMethodValues.Deflate &&
                    CompressionMethod != CompressionMethodValues.Deflate64)
                {
                    switch (CompressionMethod)
                    {
                        case CompressionMethodValues.BZip2:
                        case CompressionMethodValues.LZMA:
                            message = SR.Format(SR.UnsupportedCompressionMethod, CompressionMethod.ToString());
                            break;
                        default:
                            message = SR.UnsupportedCompression;
                            break;
                    }
                    return (false, message);
                }
            }
            if (_diskNumberStart != _archive.NumberOfThisDisk)
            {
                message = SR.SplitSpanned;
                return (false, message);
            }
            if (_offsetOfLocalHeader > _archive.ArchiveStream.Length)
            {
                message = SR.LocalFileHeaderCorrupt;
                return (false, message);
            }
            _archive.ArchiveStream.Seek(_offsetOfLocalHeader, SeekOrigin.Begin);
            if (!await ZipLocalFileHeader.TrySkipBlockAsync(_archive.ArchiveStream, cancellationToken).ConfigureAwait(false))
            {
                message = SR.LocalFileHeaderCorrupt;
                return (false, message);
            }
            // when this property gets called, some duplicated work
            long offsetOfCompressedData = await GetOffsetOfCompressedDataAsync(cancellationToken).ConfigureAwait(false);
            if (offsetOfCompressedData + _compressedSize > _archive.ArchiveStream.Length)
            {
                message = SR.LocalFileHeaderCorrupt;
                return (false, message);
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
                        return (false, message);
                    }
                }
            }
        }

        return (true, message);
    }

    // return value is true if we allocated an extra field for 64 bit headers, un/compressed size
    private async Task<bool> WriteLocalFileHeaderAsync(bool isEmptyFile, bool forceWrite, CancellationToken cancellationToken)
    {
        byte[] lfStaticHeader = new byte[ZipLocalFileHeader.SizeOfLocalHeader];

        // _entryname only gets set when we read in or call moveTo. MoveTo does a check, and
        // reading in should not be able to produce an entryname longer than ushort.MaxValue
        Debug.Assert(_storedEntryNameBytes.Length <= ushort.MaxValue);

        // decide if we need the Zip64 extra field:
        Zip64ExtraField? zip64ExtraField = null;
        uint compressedSizeTruncated, uncompressedSizeTruncated;

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
            if (_archive.Mode == ZipArchiveMode.Create && _archive.ArchiveStream.CanSeek == false)
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
        int bigExtraFieldLength = (zip64ExtraField != null ? zip64ExtraField.TotalSize : 0)
                                  + (_lhUnknownExtraFields != null ? ZipGenericExtraField.TotalSize(_lhUnknownExtraFields) : 0);
        ushort extraFieldLength;
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

            if (_lhUnknownExtraFields != null)
            {
                _archive.ArchiveStream.Seek(ZipGenericExtraField.TotalSize(_lhUnknownExtraFields), SeekOrigin.Current);
            }
        }
        else
        {
            ZipLocalFileHeader.SignatureConstantBytes.CopyTo(lfStaticHeader.AsSpan(ZipLocalFileHeader.FieldLocations.Signature));
            BinaryPrimitives.WriteUInt16LittleEndian(lfStaticHeader.AsSpan(ZipLocalFileHeader.FieldLocations.VersionNeededToExtract), (ushort)_versionToExtract);
            BinaryPrimitives.WriteUInt16LittleEndian(lfStaticHeader.AsSpan(ZipLocalFileHeader.FieldLocations.GeneralPurposeBitFlags), (ushort)_generalPurposeBitFlag);
            BinaryPrimitives.WriteUInt16LittleEndian(lfStaticHeader.AsSpan(ZipLocalFileHeader.FieldLocations.CompressionMethod), (ushort)CompressionMethod);
            BinaryPrimitives.WriteUInt32LittleEndian(lfStaticHeader.AsSpan(ZipLocalFileHeader.FieldLocations.LastModified), ZipHelper.DateTimeToDosTime(_lastModified.DateTime));
            BinaryPrimitives.WriteUInt32LittleEndian(lfStaticHeader.AsSpan(ZipLocalFileHeader.FieldLocations.Crc32), _crc32);
            BinaryPrimitives.WriteUInt32LittleEndian(lfStaticHeader.AsSpan(ZipLocalFileHeader.FieldLocations.CompressedSize), compressedSizeTruncated);
            BinaryPrimitives.WriteUInt32LittleEndian(lfStaticHeader.AsSpan(ZipLocalFileHeader.FieldLocations.UncompressedSize), uncompressedSizeTruncated);
            BinaryPrimitives.WriteUInt16LittleEndian(lfStaticHeader.AsSpan(ZipLocalFileHeader.FieldLocations.FilenameLength), (ushort)_storedEntryNameBytes.Length);
            BinaryPrimitives.WriteUInt16LittleEndian(lfStaticHeader.AsSpan(ZipLocalFileHeader.FieldLocations.ExtraFieldLength), extraFieldLength);

            // write header
            await _archive.ArchiveStream.WriteAsync(lfStaticHeader, cancellationToken).ConfigureAwait(false);

            await _archive.ArchiveStream.WriteAsync(_storedEntryNameBytes, cancellationToken).ConfigureAwait(false);

            if (zip64ExtraField != null)
            {
                await zip64ExtraField.WriteBlockAsync(_archive.ArchiveStream, cancellationToken).ConfigureAwait(false);
            }

            if (_lhUnknownExtraFields != null)
                await ZipGenericExtraField.WriteAllBlocksAsync(_lhUnknownExtraFields, _archive.ArchiveStream, cancellationToken).ConfigureAwait(false);
        }

        return zip64ExtraField != null;
    }

    private async Task WriteLocalFileHeaderAndDataIfNeededAsync(bool forceWrite, CancellationToken cancellationToken)
    {
        // _storedUncompressedData gets frozen here, and is what gets written to the file
        if (_storedUncompressedData != null || _compressedBytes != null)
        {
            if (_storedUncompressedData != null)
            {
                _uncompressedSize = _storedUncompressedData.Length;

                //The compressor fills in CRC and sizes
                //The DirectToArchiveWriterStream writes headers and such
                Stream entryWriter = new DirectToArchiveWriterStream(
                                            GetDataCompressor(_archive.ArchiveStream, true, null),
                                            this);
                await using (entryWriter)
                {
                    _storedUncompressedData.Seek(0, SeekOrigin.Begin);
                    await _storedUncompressedData.CopyToAsync(entryWriter, cancellationToken).ConfigureAwait(false);
                    await _storedUncompressedData.DisposeAsync().ConfigureAwait(false);
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

                await WriteLocalFileHeaderAsync(isEmptyFile: _uncompressedSize == 0, forceWrite: true, cancellationToken).ConfigureAwait(false);

                // according to ZIP specs, zero-byte files MUST NOT include file data
                if (_uncompressedSize != 0)
                {
                    Debug.Assert(_compressedBytes != null);
                    foreach (byte[] compressedBytes in _compressedBytes)
                    {
                        await _archive.ArchiveStream.WriteAsync(compressedBytes.AsMemory(0, compressedBytes.Length), cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }
        else // there is no data in the file (or the data in the file has not been loaded), but if we are in update mode, we may still need to write a header
        {
            if (_archive.Mode == ZipArchiveMode.Update || !_everOpenedForWrite)
            {
                _everOpenedForWrite = true;
                await WriteLocalFileHeaderAsync(isEmptyFile: _uncompressedSize == 0, forceWrite: forceWrite, cancellationToken).ConfigureAwait(false);

                // If we know that we need to update the file header (but don't need to load and update the data itself)
                // then advance the position past it.
                if (_compressedSize != 0)
                {
                    _archive.ArchiveStream.Seek(_compressedSize, SeekOrigin.Current);
                }
            }
        }
    }

    // Using _offsetOfLocalHeader, seeks back to where CRC and sizes should be in the header,
    // writes them, then seeks back to where you started
    // Assumes that the stream is currently at the end of the data
    private async Task WriteCrcAndSizesInLocalHeaderAsync(bool zip64HeaderUsed, CancellationToken cancellationToken)
    {
        const int MetadataBufferLength = ZipLocalFileHeader.FieldLengths.VersionNeededToExtract + ZipLocalFileHeader.FieldLengths.GeneralPurposeBitFlags;
        const int CrcAndSizesBufferLength = ZipLocalFileHeader.FieldLengths.Crc32 + ZipLocalFileHeader.FieldLengths.CompressedSize + ZipLocalFileHeader.FieldLengths.UncompressedSize;
        const int Zip64SizesBufferLength = Zip64ExtraField.FieldLengths.UncompressedSize + Zip64ExtraField.FieldLengths.CompressedSize;
        const int Zip64DataDescriptorCrcAndSizesBufferLength = ZipLocalFileHeader.Zip64DataDescriptor.FieldLengths.Crc32
            + ZipLocalFileHeader.Zip64DataDescriptor.FieldLengths.CompressedSize + ZipLocalFileHeader.Zip64DataDescriptor.FieldLengths.UncompressedSize;

        long finalPosition = _archive.ArchiveStream.Position;
        // Buffer has been sized to the largest data payload required: the 64-bit data descriptor.
        byte[] writeBuffer = new byte[Zip64DataDescriptorCrcAndSizesBufferLength];

        bool zip64Needed = ShouldUseZIP64
#if DEBUG_FORCE_ZIP64
                || _archive._forceZip64
#endif
        ;

        bool pretendStreaming = zip64Needed && !zip64HeaderUsed;

        uint compressedSizeTruncated = zip64Needed ? ZipHelper.Mask32Bit : (uint)_compressedSize;
        uint uncompressedSizeTruncated = zip64Needed ? ZipHelper.Mask32Bit : (uint)_uncompressedSize;

        // first step is, if we need zip64, but didn't allocate it, pretend we did a stream write, because
        // we can't go back and give ourselves the space that the extra field needs.
        // we do this by setting the correct property in the bit flag to indicate we have a data descriptor
        // and setting the version to Zip64 to indicate that descriptor contains 64-bit values
        if (pretendStreaming)
        {
            int relativeVersionToExtractLocation = ZipLocalFileHeader.FieldLocations.VersionNeededToExtract - ZipLocalFileHeader.FieldLocations.VersionNeededToExtract;
            int relativeGeneralPurposeBitFlagsLocation = ZipLocalFileHeader.FieldLocations.GeneralPurposeBitFlags - ZipLocalFileHeader.FieldLocations.VersionNeededToExtract;

            VersionToExtractAtLeast(ZipVersionNeededValues.Zip64);
            _generalPurposeBitFlag |= BitFlagValues.DataDescriptor;

            _archive.ArchiveStream.Seek(_offsetOfLocalHeader + ZipLocalFileHeader.FieldLocations.VersionNeededToExtract,
                                        SeekOrigin.Begin);
            BinaryPrimitives.WriteUInt16LittleEndian(writeBuffer.AsSpan(relativeVersionToExtractLocation), (ushort)_versionToExtract);
            BinaryPrimitives.WriteUInt16LittleEndian(writeBuffer.AsSpan(relativeGeneralPurposeBitFlagsLocation), (ushort)_generalPurposeBitFlag);

            await _archive.ArchiveStream.WriteAsync(writeBuffer.AsMemory(0, MetadataBufferLength), cancellationToken).ConfigureAwait(false);
        }

        // next step is fill out the 32-bit size values in the normal header. we can't assume that
        // they are correct. we also write the CRC
        _archive.ArchiveStream.Seek(_offsetOfLocalHeader + ZipLocalFileHeader.FieldLocations.Crc32,
                                        SeekOrigin.Begin);
        if (!pretendStreaming)
        {
            int relativeCrc32Location = ZipLocalFileHeader.FieldLocations.Crc32 - ZipLocalFileHeader.FieldLocations.Crc32;
            int relativeCompressedSizeLocation = ZipLocalFileHeader.FieldLocations.CompressedSize - ZipLocalFileHeader.FieldLocations.Crc32;
            int relativeUncompressedSizeLocation = ZipLocalFileHeader.FieldLocations.UncompressedSize - ZipLocalFileHeader.FieldLocations.Crc32;

            BinaryPrimitives.WriteUInt32LittleEndian(writeBuffer.AsSpan(relativeCrc32Location), _crc32);
            BinaryPrimitives.WriteUInt32LittleEndian(writeBuffer.AsSpan(relativeCompressedSizeLocation), compressedSizeTruncated);
            BinaryPrimitives.WriteUInt32LittleEndian(writeBuffer.AsSpan(relativeUncompressedSizeLocation), uncompressedSizeTruncated);
        }
        else // but if we are pretending to stream, we want to fill in with zeroes
        {
            writeBuffer.AsSpan(0, CrcAndSizesBufferLength).Clear();
        }
        await _archive.ArchiveStream.WriteAsync(writeBuffer.AsMemory(0, CrcAndSizesBufferLength), cancellationToken).ConfigureAwait(false);

        // next step: if we wrote the 64 bit header initially, a different implementation might
        // try to read it, even if the 32-bit size values aren't masked. thus, we should always put the
        // correct size information in there. note that order of uncomp/comp is switched, and these are
        // 64-bit values
        // also, note that in order for this to be correct, we have to ensure that the zip64 extra field
        // is always the first extra field that is written
        if (zip64HeaderUsed)
        {
            int relativeUncompressedSizeLocation = Zip64ExtraField.FieldLocations.UncompressedSize - Zip64ExtraField.FieldLocations.UncompressedSize;
            int relativeCompressedSizeLocation = Zip64ExtraField.FieldLocations.CompressedSize - Zip64ExtraField.FieldLocations.UncompressedSize;

            _archive.ArchiveStream.Seek(_offsetOfLocalHeader + ZipLocalFileHeader.SizeOfLocalHeader
                                        + _storedEntryNameBytes.Length + Zip64ExtraField.OffsetToFirstField,
                                        SeekOrigin.Begin);
            BinaryPrimitives.WriteInt64LittleEndian(writeBuffer.AsSpan(relativeUncompressedSizeLocation), _uncompressedSize);
            BinaryPrimitives.WriteInt64LittleEndian(writeBuffer.AsSpan(relativeCompressedSizeLocation), _compressedSize);

            await _archive.ArchiveStream.WriteAsync(writeBuffer.AsMemory(0, Zip64SizesBufferLength), cancellationToken).ConfigureAwait(false);
        }

        // now go to the where we were. assume that this is the end of the data
        _archive.ArchiveStream.Seek(finalPosition, SeekOrigin.Begin);

        // if we are pretending we did a stream write, we want to write the data descriptor out
        // the data descriptor can have 32-bit sizes or 64-bit sizes. In this case, we always use
        // 64-bit sizes
        if (pretendStreaming)
        {
            int relativeCrc32Location = ZipLocalFileHeader.Zip64DataDescriptor.FieldLocations.Crc32 - ZipLocalFileHeader.Zip64DataDescriptor.FieldLocations.Crc32;
            int relativeCompressedSizeLocation = ZipLocalFileHeader.Zip64DataDescriptor.FieldLocations.CompressedSize - ZipLocalFileHeader.Zip64DataDescriptor.FieldLocations.Crc32;
            int relativeUncompressedSizeLocation = ZipLocalFileHeader.Zip64DataDescriptor.FieldLocations.UncompressedSize - ZipLocalFileHeader.Zip64DataDescriptor.FieldLocations.Crc32;

            BinaryPrimitives.WriteUInt32LittleEndian(writeBuffer.AsSpan(relativeCrc32Location), _crc32);
            BinaryPrimitives.WriteInt64LittleEndian(writeBuffer.AsSpan(relativeCompressedSizeLocation), _compressedSize);
            BinaryPrimitives.WriteInt64LittleEndian(writeBuffer.AsSpan(relativeUncompressedSizeLocation), _uncompressedSize);

            await _archive.ArchiveStream.WriteAsync(writeBuffer.AsMemory(0, Zip64DataDescriptorCrcAndSizesBufferLength), cancellationToken).ConfigureAwait(false);
        }
    }

    private ValueTask WriteDataDescriptorAsync(CancellationToken cancellationToken)
    {
        // We enter here because we cannot seek, so the data descriptor bit should be on
        Debug.Assert((_generalPurposeBitFlag & BitFlagValues.DataDescriptor) != 0);

        // data descriptor can be 32-bit or 64-bit sizes. 32-bit is more compatible, so use that if possible
        // signature is optional but recommended by the spec
        const int MaxSizeOfDataDescriptor = 24;

        byte[] dataDescriptor = new byte[MaxSizeOfDataDescriptor];
        int bytesToWrite;

        ZipLocalFileHeader.DataDescriptorSignatureConstantBytes.CopyTo(dataDescriptor.AsSpan(ZipLocalFileHeader.ZipDataDescriptor.FieldLocations.Signature));
        BinaryPrimitives.WriteUInt32LittleEndian(dataDescriptor.AsSpan(ZipLocalFileHeader.ZipDataDescriptor.FieldLocations.Crc32), _crc32);

        if (AreSizesTooLarge)
        {
            BinaryPrimitives.WriteInt64LittleEndian(dataDescriptor.AsSpan(ZipLocalFileHeader.Zip64DataDescriptor.FieldLocations.CompressedSize), _compressedSize);
            BinaryPrimitives.WriteInt64LittleEndian(dataDescriptor.AsSpan(ZipLocalFileHeader.Zip64DataDescriptor.FieldLocations.UncompressedSize), _uncompressedSize);

            bytesToWrite = ZipLocalFileHeader.Zip64DataDescriptor.FieldLocations.UncompressedSize + ZipLocalFileHeader.Zip64DataDescriptor.FieldLengths.UncompressedSize;
        }
        else
        {
            BinaryPrimitives.WriteUInt32LittleEndian(dataDescriptor.AsSpan(ZipLocalFileHeader.ZipDataDescriptor.FieldLocations.CompressedSize), (uint)_compressedSize);
            BinaryPrimitives.WriteUInt32LittleEndian(dataDescriptor.AsSpan(ZipLocalFileHeader.ZipDataDescriptor.FieldLocations.UncompressedSize), (uint)_uncompressedSize);

            bytesToWrite = ZipLocalFileHeader.ZipDataDescriptor.FieldLocations.UncompressedSize + ZipLocalFileHeader.ZipDataDescriptor.FieldLengths.UncompressedSize;
        }

        return _archive.ArchiveStream.WriteAsync(dataDescriptor.AsMemory(0, bytesToWrite), cancellationToken);
    }
}
