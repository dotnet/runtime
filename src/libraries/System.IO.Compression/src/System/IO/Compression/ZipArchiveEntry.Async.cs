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
        cancellationToken.ThrowIfCancellationRequested();
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
        cancellationToken.ThrowIfCancellationRequested();
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
        cancellationToken.ThrowIfCancellationRequested();
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
        cancellationToken.ThrowIfCancellationRequested();
        await CloseStreamsAsync().ConfigureAwait(false);
        await WriteLocalFileHeaderAndDataIfNeededAsync(forceWrite, cancellationToken).ConfigureAwait(false);
        await UnloadStreamsAsync().ConfigureAwait(false);
    }

    // should only throw an exception in extremely exceptional cases because it is called from dispose
    internal async Task WriteCentralDirectoryFileHeaderAsync(bool forceWrite, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (WriteCentralDirectoryFileHeaderInitialize(forceWrite, out Zip64ExtraField? zip64ExtraField, out uint compressedSizeTruncated, out uint uncompressedSizeTruncated, out ushort extraFieldLength, out uint offsetOfLocalHeaderTruncated))
        {
            byte[] cdStaticHeader = new byte[ZipCentralDirectoryFileHeader.BlockConstantSectionSize];
            WriteCentralDirectoryFileHeaderPrepare(cdStaticHeader, compressedSizeTruncated, uncompressedSizeTruncated, extraFieldLength, offsetOfLocalHeaderTruncated);

            await _archive.ArchiveStream.WriteAsync(cdStaticHeader, cancellationToken).ConfigureAwait(false);
            await _archive.ArchiveStream.WriteAsync(_storedEntryNameBytes, cancellationToken).ConfigureAwait(false);

            // only write zip64ExtraField if we decided we need it (it's not null)
            if (zip64ExtraField != null)
            {
                await zip64ExtraField.WriteBlockAsync(_archive.ArchiveStream, cancellationToken).ConfigureAwait(false);
            }

            // write extra fields (and any malformed trailing data).
            await ZipGenericExtraField.WriteAllBlocksAsync(_cdUnknownExtraFields, _cdTrailingExtraFieldData ?? Array.Empty<byte>(), _archive.ArchiveStream, cancellationToken).ConfigureAwait(false);

            if (_fileComment.Length > 0)
            {
                await _archive.ArchiveStream.WriteAsync(_fileComment, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    internal async Task LoadLocalHeaderExtraFieldIfNeededAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // we should have made this exact call in _archive.Init through ThrowIfOpenable
        Debug.Assert(await GetIsOpenableAsync(false, true, cancellationToken).ConfigureAwait(false));

        // load local header's extra fields. it will be null if we couldn't read for some reason
        if (_originallyInArchive)
        {
            _archive.ArchiveStream.Seek(_offsetOfLocalHeader, SeekOrigin.Begin);
            (_lhUnknownExtraFields, _lhTrailingExtraFieldData) = await ZipLocalFileHeader.GetExtraFieldsAsync(_archive.ArchiveStream, cancellationToken).ConfigureAwait(false);
        }
    }

    internal async Task LoadCompressedBytesIfNeededAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // we should have made this exact call in _archive.Init through ThrowIfOpenable
        Debug.Assert(await GetIsOpenableAsync(false, true, cancellationToken).ConfigureAwait(false));

        if (!_everOpenedForWrite && _originallyInArchive)
        {
            _compressedBytes = LoadCompressedBytesIfNeededInitialize(out int maxSingleBufferSize);

            _archive.ArchiveStream.Seek(await GetOffsetOfCompressedDataAsync(cancellationToken).ConfigureAwait(false), SeekOrigin.Begin);

            for (int i = 0; i < _compressedBytes.Length - 1; i++)
            {
                await _archive.ArchiveStream.ReadAtLeastAsync(_compressedBytes[i], maxSingleBufferSize, throwOnEndOfStream: true, cancellationToken).ConfigureAwait(false);
            }
            await _archive.ArchiveStream.ReadAtLeastAsync(_compressedBytes[_compressedBytes.Length - 1], (int)(_compressedSize % maxSingleBufferSize), throwOnEndOfStream: true, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<bool> GetIsOpenableAsync(bool needToUncompress, bool needToLoadIntoMemory, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        (bool result, _) = await IsOpenableAsync(needToUncompress, needToLoadIntoMemory, cancellationToken).ConfigureAwait(false);
        return result;
    }

    internal async Task ThrowIfNotOpenableAsync(bool needToUncompress, bool needToLoadIntoMemory, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        (bool openable, string? message) = await IsOpenableAsync(needToUncompress, needToLoadIntoMemory, cancellationToken).ConfigureAwait(false);
        if (!openable)
            throw new InvalidDataException(message);
    }

    private async Task<Stream> OpenInReadModeAsync(bool checkOpenable, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (checkOpenable)
            await ThrowIfNotOpenableAsync(needToUncompress: true, needToLoadIntoMemory: false, cancellationToken).ConfigureAwait(false);

        return OpenInReadModeGetDataCompressor(
            await GetOffsetOfCompressedDataAsync(cancellationToken).ConfigureAwait(false));
    }

    private async Task<WrappedStream> OpenInUpdateModeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
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

    private async Task<(bool, string?)> IsOpenableAsync(bool needToUncompress, bool needToLoadIntoMemory, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string? message = null;

        if (!_originallyInArchive)
        {
            return (true, message);
        }

        if (!IsOpenableInitialVerifications(needToUncompress, out message))
        {
            return (false, message);
        }
        if (!await ZipLocalFileHeader.TrySkipBlockAsync(_archive.ArchiveStream, cancellationToken).ConfigureAwait(false))
        {
            message = SR.LocalFileHeaderCorrupt;
            return (false, message);
        }

        // when this property gets called, some duplicated work
        long offsetOfCompressedData = await GetOffsetOfCompressedDataAsync(cancellationToken).ConfigureAwait(false);
        if (!IsOpenableFinalVerifications(needToLoadIntoMemory, offsetOfCompressedData, out message))
        {
            return (false, message);
        }

        return (true, message);
    }

    // return value is true if we allocated an extra field for 64 bit headers, un/compressed size
    private async Task<bool> WriteLocalFileHeaderAsync(bool isEmptyFile, bool forceWrite, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (WriteLocalFileHeaderInitialize(isEmptyFile, forceWrite, out Zip64ExtraField? zip64ExtraField, out uint compressedSizeTruncated, out uint uncompressedSizeTruncated, out ushort extraFieldLength))
        {
            byte[] lfStaticHeader = new byte[ZipLocalFileHeader.SizeOfLocalHeader];
            WriteLocalFileHeaderPrepare(lfStaticHeader, compressedSizeTruncated, uncompressedSizeTruncated, extraFieldLength);

            // write header
            await _archive.ArchiveStream.WriteAsync(lfStaticHeader, cancellationToken).ConfigureAwait(false);
            await _archive.ArchiveStream.WriteAsync(_storedEntryNameBytes, cancellationToken).ConfigureAwait(false);

            // Only when handling zip64
            if (zip64ExtraField != null)
            {
                await zip64ExtraField.WriteBlockAsync(_archive.ArchiveStream, cancellationToken).ConfigureAwait(false);
            }

            await ZipGenericExtraField.WriteAllBlocksAsync(_lhUnknownExtraFields, _lhTrailingExtraFieldData ?? Array.Empty<byte>(), _archive.ArchiveStream, cancellationToken).ConfigureAwait(false);
        }

        return zip64ExtraField != null;
    }

    private async Task WriteLocalFileHeaderAndDataIfNeededAsync(bool forceWrite, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // _storedUncompressedData gets frozen here, and is what gets written to the file
        if (_storedUncompressedData != null || _compressedBytes != null)
        {
            if (_storedUncompressedData != null)
            {
                _uncompressedSize = _storedUncompressedData.Length;

                //The compressor fills in CRC and sizes
                //The DirectToArchiveWriterStream writes headers and such
                DirectToArchiveWriterStream entryWriter = new(GetDataCompressor(_archive.ArchiveStream, true, null), this);
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
                        await _archive.ArchiveStream.WriteAsync(compressedBytes, cancellationToken).ConfigureAwait(false);
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
        cancellationToken.ThrowIfCancellationRequested();
        // Buffer has been sized to the largest data payload required: the 64-bit data descriptor.
        byte[] writeBuffer = new byte[Zip64DataDescriptorCrcAndSizesBufferLength];

        WriteCrcAndSizesInLocalHeaderInitialize(zip64HeaderUsed, out long finalPosition, out bool pretendStreaming, out uint compressedSizeTruncated, out uint uncompressedSizeTruncated);

        // first step is, if we need zip64, but didn't allocate it, pretend we did a stream write, because
        // we can't go back and give ourselves the space that the extra field needs.
        // we do this by setting the correct property in the bit flag to indicate we have a data descriptor
        // and setting the version to Zip64 to indicate that descriptor contains 64-bit values
        if (pretendStreaming)
        {
            WriteCrcAndSizesInLocalHeaderPrepareForZip64PretendStreaming(writeBuffer);
            await _archive.ArchiveStream.WriteAsync(writeBuffer.AsMemory(0, MetadataBufferLength), cancellationToken).ConfigureAwait(false);
        }

        // next step is fill out the 32-bit size values in the normal header. we can't assume that
        // they are correct. we also write the CRC
        WriteCrcAndSizesInLocalHeaderPrepareFor32bitValuesWriting(pretendStreaming, writeBuffer, compressedSizeTruncated, uncompressedSizeTruncated);
        await _archive.ArchiveStream.WriteAsync(writeBuffer.AsMemory(0, CrcAndSizesBufferLength), cancellationToken).ConfigureAwait(false);

        // next step: if we wrote the 64 bit header initially, a different implementation might
        // try to read it, even if the 32-bit size values aren't masked. thus, we should always put the
        // correct size information in there. note that order of uncomp/comp is switched, and these are
        // 64-bit values
        // also, note that in order for this to be correct, we have to ensure that the zip64 extra field
        // is always the first extra field that is written
        if (zip64HeaderUsed)
        {
            WriteCrcAndSizesInLocalHeaderPrepareForWritingWhenZip64HeaderUsed(writeBuffer);
            await _archive.ArchiveStream.WriteAsync(writeBuffer.AsMemory(0, Zip64SizesBufferLength), cancellationToken).ConfigureAwait(false);
        }

        // now go to the where we were. assume that this is the end of the data
        _archive.ArchiveStream.Seek(finalPosition, SeekOrigin.Begin);

        // if we are pretending we did a stream write, we want to write the data descriptor out
        // the data descriptor can have 32-bit sizes or 64-bit sizes. In this case, we always use
        // 64-bit sizes
        if (pretendStreaming)
        {
            WriteCrcAndSizesInLocalHeaderPrepareForWritingDataDescriptor(writeBuffer);
            await _archive.ArchiveStream.WriteAsync(writeBuffer.AsMemory(0, Zip64DataDescriptorCrcAndSizesBufferLength), cancellationToken).ConfigureAwait(false);
        }
    }

    private ValueTask WriteDataDescriptorAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byte[] dataDescriptor = new byte[MaxSizeOfDataDescriptor];
        int bytesToWrite = PrepareToWriteDataDescriptor(dataDescriptor);
        return _archive.ArchiveStream.WriteAsync(dataDescriptor.AsMemory(0, bytesToWrite), cancellationToken);
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

    private async Task CloseStreamsAsync()
    {
        // if the user left the stream open, close the underlying stream for them
        if (_outstandingWriteStream != null)
        {
            await _outstandingWriteStream.DisposeAsync().ConfigureAwait(false);
        }
    }
}
