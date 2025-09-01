// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression;

public partial class ZipArchive : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Asynchronously initializes and returns a new instance of <see cref="ZipArchive"/> on the given stream in the specified mode, specifying whether to leave the stream open, with an optional encoding and an optional cancellation token.
    /// </summary>
    /// <param name="stream">The input or output stream.</param>
    /// <param name="mode">See the description of the ZipArchiveMode enum. Read requires the stream to support reading, Create requires the stream to support writing, and Update requires the stream to support reading, writing, and seeking.</param>
    /// <param name="leaveOpen">true to leave the stream open upon disposing the ZipArchive, otherwise false.</param>
    /// <param name="entryNameEncoding">The encoding to use when reading or writing entry names and comments in this ZipArchive.
    ///         ///     <para>NOTE: Specifying this parameter to values other than <c>null</c> is discouraged.
    ///         However, this may be necessary for interoperability with ZIP archive tools and libraries that do not correctly support
    ///         UTF-8 encoding for entry names.<br />
    ///         This value is used as follows:</para>
    ///     <para><strong>Reading (opening) ZIP archive files:</strong></para>
    ///     <para>If <c>entryNameEncoding</c> is not specified (<c>== null</c>):</para>
    ///     <list>
    ///         <item>For entries where the language encoding flag (EFS) in the general purpose bit flag of the local file header is <em>not</em> set,
    ///         use the current system default code page (<c>Encoding.Default</c>) in order to decode the entry name and comment.</item>
    ///         <item>For entries where the language encoding flag (EFS) in the general purpose bit flag of the local file header <em>is</em> set,
    ///         use UTF-8 (<c>Encoding.UTF8</c>) in order to decode the entry name and comment.</item>
    ///     </list>
    ///     <para>If <c>entryNameEncoding</c> is specified (<c>!= null</c>):</para>
    ///     <list>
    ///         <item>For entries where the language encoding flag (EFS) in the general purpose bit flag of the local file header is <em>not</em> set,
    ///         use the specified <c>entryNameEncoding</c> in order to decode the entry name and comment.</item>
    ///         <item>For entries where the language encoding flag (EFS) in the general purpose bit flag of the local file header <em>is</em> set,
    ///         use UTF-8 (<c>Encoding.UTF8</c>) in order to decode the entry name and comment.</item>
    ///     </list>
    ///     <para><strong>Writing (saving) ZIP archive files:</strong></para>
    ///     <para>If <c>entryNameEncoding</c> is not specified (<c>== null</c>):</para>
    ///     <list>
    ///         <item>For entry names and comments that contain characters outside the ASCII range,
    ///         the language encoding flag (EFS) will be set in the general purpose bit flag of the local file header,
    ///         and UTF-8 (<c>Encoding.UTF8</c>) will be used in order to encode the entry name and comment into bytes.</item>
    ///         <item>For entry names and comments that do not contain characters outside the ASCII range,
    ///         the language encoding flag (EFS) will not be set in the general purpose bit flag of the local file header,
    ///         and the current system default code page (<c>Encoding.Default</c>) will be used to encode the entry names and comments into bytes.</item>
    ///     </list>
    ///     <para>If <c>entryNameEncoding</c> is specified (<c>!= null</c>):</para>
    ///     <list>
    ///         <item>The specified <c>entryNameEncoding</c> will always be used to encode the entry names and comments into bytes.
    ///         The language encoding flag (EFS) in the general purpose bit flag of the local file header will be set if and only
    ///         if the specified <c>entryNameEncoding</c> is a UTF-8 encoding.</item>
    ///     </list>
    ///     <para>Note that Unicode encodings other than UTF-8 may not be currently used for the <c>entryNameEncoding</c>,
    ///     otherwise an <see cref="ArgumentException"/> is thrown.</para>
    /// </param>
    /// <param name="cancellationToken">The optional cancellation token to monitor.</param>
    /// <exception cref="ArgumentException">The stream is already closed. -or- mode is incompatible with the capabilities of the stream.</exception>
    /// <exception cref="ArgumentNullException">The stream is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">mode specified an invalid value.</exception>
    /// <exception cref="InvalidDataException">The contents of the stream could not be interpreted as a Zip file. -or- mode is Update and an entry is missing from the archive or is corrupt and cannot be read. -or- mode is Update and an entry is too large to fit into memory.</exception>
    /// <exception cref="ArgumentException">If a Unicode encoding other than UTF-8 is specified for the <code>entryNameEncoding</code>.</exception>
    public static async Task<ZipArchive> CreateAsync(Stream stream, ZipArchiveMode mode, bool leaveOpen, Encoding? entryNameEncoding, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(stream);

        Stream? extraTempStream = null;

        try
        {
            Stream? backingStream = null;

            if (ValidateMode(mode, stream))
            {
                backingStream = stream;
                extraTempStream = stream = new MemoryStream();
                await backingStream.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
                stream.Seek(0, SeekOrigin.Begin);
            }

            ZipArchive zipArchive = new(mode, leaveOpen, entryNameEncoding, backingStream, DecideArchiveStream(mode, stream));

            switch (mode)
            {
                case ZipArchiveMode.Create:
                    zipArchive._readEntries = true;
                    break;
                case ZipArchiveMode.Read:
                    await zipArchive.ReadEndOfCentralDirectoryAsync(cancellationToken).ConfigureAwait(false);
                    break;
                case ZipArchiveMode.Update:
                default:
                    Debug.Assert(mode == ZipArchiveMode.Update);
                    if (zipArchive._archiveStream.Length == 0)
                    {
                        zipArchive._readEntries = true;
                    }
                    else
                    {
                        await zipArchive.ReadEndOfCentralDirectoryAsync(cancellationToken).ConfigureAwait(false);
                        await zipArchive.EnsureCentralDirectoryReadAsync(cancellationToken).ConfigureAwait(false);

                        foreach (ZipArchiveEntry entry in zipArchive._entries)
                        {
                            await entry.ThrowIfNotOpenableAsync(needToUncompress: false, needToLoadIntoMemory: true, cancellationToken).ConfigureAwait(false);
                        }
                    }
                    break;
            }

            return zipArchive;
        }
        catch (Exception)
        {
            if (extraTempStream != null)
            {
                await extraTempStream.DisposeAsync().ConfigureAwait(false);
            }

            throw;
        }
    }

    public async ValueTask DisposeAsync() => await DisposeAsyncCore().ConfigureAwait(false);

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (!_isDisposed)
        {
            try
            {
                switch (_mode)
                {
                    case ZipArchiveMode.Read:
                        break;
                    case ZipArchiveMode.Create:
                    case ZipArchiveMode.Update:
                    default:
                        Debug.Assert(_mode == ZipArchiveMode.Update || _mode == ZipArchiveMode.Create);
                        await WriteFileAsync().ConfigureAwait(false);
                        break;
                }
            }
            finally
            {
                await CloseStreamsAsync().ConfigureAwait(false);
                _isDisposed = true;
            }
        }
    }

    private async Task CloseStreamsAsync()
    {
        if (!_leaveOpen)
        {
            await _archiveStream.DisposeAsync().ConfigureAwait(false);
            if (_backingStream != null)
            {
                await _backingStream.DisposeAsync().ConfigureAwait(false);
            }
        }
        else
        {
            // if _backingStream isn't null, that means we assigned the original stream they passed
            // us to _backingStream (which they requested we leave open), and _archiveStream was
            // the temporary copy that we needed
            if (_backingStream != null)
            {
                await _archiveStream.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private async Task EnsureCentralDirectoryReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_readEntries)
        {
            await ReadCentralDirectoryAsync(cancellationToken).ConfigureAwait(false);
            _readEntries = true;
        }
    }

    private async Task ReadCentralDirectoryAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            ReadCentralDirectoryInitialize(out byte[] fileBuffer, out long numberOfEntries, out bool saveExtraFieldsAndComments, out bool continueReadingCentralDirectory, out int bytesRead, out int currPosition, out int bytesConsumed);

            // read the central directory
            while (continueReadingCentralDirectory)
            {
                // the buffer read must always be large enough to fit the constant section size of at least one header
                int currBytesRead = await _archiveStream.ReadAtLeastAsync(fileBuffer, ZipCentralDirectoryFileHeader.BlockConstantSectionSize, throwOnEndOfStream: false, cancellationToken).ConfigureAwait(false);

                byte[] sizedFileBuffer = fileBuffer[0..currBytesRead];
                continueReadingCentralDirectory = currBytesRead >= ZipCentralDirectoryFileHeader.BlockConstantSectionSize;

                while (currPosition + ZipCentralDirectoryFileHeader.BlockConstantSectionSize <= currBytesRead)
                {
                    (bool result, bytesConsumed, ZipCentralDirectoryFileHeader? currentHeader) =
                        await ZipCentralDirectoryFileHeader.TryReadBlockAsync(sizedFileBuffer.AsMemory(currPosition), _archiveStream, saveExtraFieldsAndComments, cancellationToken).ConfigureAwait(false);

                    if (!ReadCentralDirectoryEndOfInnerLoopWork(result, currentHeader, bytesConsumed, ref continueReadingCentralDirectory, ref numberOfEntries, ref currPosition, ref bytesRead))
                    {
                        break;
                    }
                }

                ReadCentralDirectoryEndOfOuterLoopWork(ref currPosition, sizedFileBuffer);
            }

            ReadCentralDirectoryPostOuterLoopWork(numberOfEntries);
        }
        catch (EndOfStreamException ex)
        {
            throw new InvalidDataException(SR.Format(SR.CentralDirectoryInvalid, ex));
        }
    }

    // This function reads all the EOCD stuff it needs to find the offset to the start of the central directory
    // This offset gets put in _centralDirectoryStart and the number of this disk gets put in _numberOfThisDisk
    // Also does some verification that this isn't a split/spanned archive
    // Also checks that offset to CD isn't out of bounds
    private async Task ReadEndOfCentralDirectoryAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // This seeks backwards almost to the beginning of the EOCD, one byte after where the signature would be
            // located if the EOCD had the minimum possible size (no file zip comment)
            _archiveStream.Seek(-ZipEndOfCentralDirectoryBlock.SizeOfBlockWithoutSignature, SeekOrigin.End);

            // If the EOCD has the minimum possible size (no zip file comment), then exactly the previous 4 bytes will contain the signature
            // But if the EOCD has max possible size, the signature should be found somewhere in the previous 64K + 4 bytes
            if (!await ZipHelper.SeekBackwardsToSignatureAsync(_archiveStream,
                    ZipEndOfCentralDirectoryBlock.SignatureConstantBytes,
                    ZipEndOfCentralDirectoryBlock.ZipFileCommentMaxLength + ZipEndOfCentralDirectoryBlock.FieldLengths.Signature,
                    cancellationToken).ConfigureAwait(false))
                throw new InvalidDataException(SR.EOCDNotFound);

            long eocdStart = _archiveStream.Position;

            // read the EOCD
            ZipEndOfCentralDirectoryBlock eocd = await ZipEndOfCentralDirectoryBlock.ReadBlockAsync(_archiveStream, cancellationToken).ConfigureAwait(false);

            ReadEndOfCentralDirectoryInnerWork(eocd);

            await TryReadZip64EndOfCentralDirectoryAsync(eocd, eocdStart, cancellationToken).ConfigureAwait(false);

            if (_centralDirectoryStart > _archiveStream.Length)
            {
                throw new InvalidDataException(SR.FieldTooBigOffsetToCD);
            }
        }
        catch (EndOfStreamException ex)
        {
            throw new InvalidDataException(SR.CDCorrupt, ex);
        }
        catch (IOException ex)
        {
            throw new InvalidDataException(SR.CDCorrupt, ex);
        }
    }

    // Tries to find the Zip64 End of Central Directory Locator, then the Zip64 End of Central Directory, assuming the
    // End of Central Directory block has already been found, as well as the location in the stream where the EOCD starts.
    private async ValueTask TryReadZip64EndOfCentralDirectoryAsync(ZipEndOfCentralDirectoryBlock eocd, long eocdStart, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Only bother looking for the Zip64-EOCD stuff if we suspect it is needed because some value is FFFFFFFFF
        // because these are the only two values we need, we only worry about these
        // if we don't find the Zip64-EOCD, we just give up and try to use the original values
        if (eocd.NumberOfThisDisk == ZipHelper.Mask16Bit ||
            eocd.OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber == ZipHelper.Mask32Bit ||
            eocd.NumberOfEntriesInTheCentralDirectory == ZipHelper.Mask16Bit)
        {
            // Read Zip64 End of Central Directory Locator

            // Check if there's enough space before the EOCD to look for the Zip64 EOCDL
            if (eocdStart < Zip64EndOfCentralDirectoryLocator.TotalSize)
            {
                throw new InvalidDataException(SR.Zip64EOCDNotWhereExpected);
            }

            // This seeks forwards almost to the beginning of the Zip64-EOCDL, one byte after where the signature would be located
            _archiveStream.Seek(eocdStart - Zip64EndOfCentralDirectoryLocator.SizeOfBlockWithoutSignature, SeekOrigin.Begin);

            // Exactly the previous 4 bytes should contain the Zip64-EOCDL signature
            // if we don't find it, assume it doesn't exist and use data from normal EOCD
            if (await ZipHelper.SeekBackwardsToSignatureAsync(_archiveStream,
                    Zip64EndOfCentralDirectoryLocator.SignatureConstantBytes,
                    Zip64EndOfCentralDirectoryLocator.FieldLengths.Signature, cancellationToken).ConfigureAwait(false))
            {
                // use locator to get to Zip64-EOCD
                Zip64EndOfCentralDirectoryLocator locator = await Zip64EndOfCentralDirectoryLocator.TryReadBlockAsync(_archiveStream, cancellationToken).ConfigureAwait(false);
                TryReadZip64EndOfCentralDirectoryInnerInitialWork(locator);

                // Read Zip64 End of Central Directory Record
                Zip64EndOfCentralDirectoryRecord record = await Zip64EndOfCentralDirectoryRecord.TryReadBlockAsync(_archiveStream, cancellationToken).ConfigureAwait(false);

                TryReadZip64EndOfCentralDirectoryInnerFinalWork(record);
            }
        }
    }

    private async ValueTask WriteFileAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // if we are in create mode, we always set readEntries to true in Init
        // if we are in update mode, we call EnsureCentralDirectoryRead, which sets readEntries to true
        Debug.Assert(_readEntries);

        // Entries starting after this offset have had a dynamically-sized change. Everything on or after this point must be rewritten.
        long completeRewriteStartingOffset = 0;
        List<ZipArchiveEntry> entriesToWrite = _entries;

        if (_mode == ZipArchiveMode.Update)
        {
            // Entries starting after this offset have some kind of change made to them. It might just be a fixed-length field though, in which case
            // that single entry's metadata can be rewritten without impacting anything else.
            long startingOffset = _firstDeletedEntryOffset;
            long nextFileOffset = 0;
            completeRewriteStartingOffset = startingOffset;

            entriesToWrite = new(_entries.Count);
            foreach (ZipArchiveEntry entry in _entries)
            {
                if (!entry.OriginallyInArchive)
                {
                    entriesToWrite.Add(entry);
                }
                else
                {

                    WriteFileCalculateOffsets(entry, ref startingOffset, ref nextFileOffset);

                    // We want to re-write entries which are after the starting offset of the first entry which has pending data to write.
                    // NB: the existing ZipArchiveEntries are sorted in _entries by their position ascending.
                    if (entry.OffsetOfLocalHeader >= startingOffset)
                    {
                        WriteFileCheckStartingOffset(entry, ref completeRewriteStartingOffset);

                        await entry.LoadLocalHeaderExtraFieldIfNeededAsync(cancellationToken).ConfigureAwait(false);
                        if (entry.OffsetOfLocalHeader >= completeRewriteStartingOffset)
                        {
                            await entry.LoadCompressedBytesIfNeededAsync(cancellationToken).ConfigureAwait(false);
                        }

                        entriesToWrite.Add(entry);
                    }
                }
            }

            WriteFileUpdateModeFinalWork(startingOffset, nextFileOffset);
        }

        foreach (ZipArchiveEntry entry in entriesToWrite)
        {
            // We don't always need to write the local header entry, ZipArchiveEntry is usually able to work out when it doesn't need to.
            // We want to force this header entry to be written (even for completely untouched entries) if the entry comes after one
            // which had a pending dynamically-sized write.
            bool forceWriteLocalEntry = !entry.OriginallyInArchive || (entry.OriginallyInArchive && entry.OffsetOfLocalHeader >= completeRewriteStartingOffset);

            await entry.WriteAndFinishLocalEntryAsync(forceWriteLocalEntry, cancellationToken).ConfigureAwait(false);
        }

        long plannedCentralDirectoryPosition = _archiveStream.Position;
        // If there are no entries in the archive, we still want to create the archive epilogue.
        bool archiveEpilogueRequiresUpdate = _entries.Count == 0;

        foreach (ZipArchiveEntry entry in _entries)
        {
            // The central directory needs to be rewritten if its position has moved, if there's a new entry in the archive, or if the entry might be different.
            bool centralDirectoryEntryRequiresUpdate = plannedCentralDirectoryPosition != _centralDirectoryStart
                || !entry.OriginallyInArchive || entry.OffsetOfLocalHeader >= completeRewriteStartingOffset;

            await entry.WriteCentralDirectoryFileHeaderAsync(centralDirectoryEntryRequiresUpdate, cancellationToken).ConfigureAwait(false);
            archiveEpilogueRequiresUpdate |= centralDirectoryEntryRequiresUpdate;
        }

        long sizeOfCentralDirectory = _archiveStream.Position - plannedCentralDirectoryPosition;

        await WriteArchiveEpilogueAsync(plannedCentralDirectoryPosition, sizeOfCentralDirectory, archiveEpilogueRequiresUpdate, cancellationToken).ConfigureAwait(false);

        WriteFileFinalWork();
    }

    // writes eocd, and if needed, zip 64 eocd, zip64 eocd locator
    // should only throw an exception in extremely exceptional cases because it is called from dispose
    private async ValueTask WriteArchiveEpilogueAsync(long startOfCentralDirectory, long sizeOfCentralDirectory, bool centralDirectoryChanged, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // determine if we need Zip 64
        if (startOfCentralDirectory >= uint.MaxValue
            || sizeOfCentralDirectory >= uint.MaxValue
            || _entries.Count >= ZipHelper.Mask16Bit
#if DEBUG_FORCE_ZIP64
                || _forceZip64
#endif
            )
        {
            // if we need zip 64, write zip 64 eocd and locator
            long zip64EOCDRecordStart = _archiveStream.Position;

            if (centralDirectoryChanged)
            {
                await Zip64EndOfCentralDirectoryRecord.WriteBlockAsync(_archiveStream, _entries.Count, startOfCentralDirectory, sizeOfCentralDirectory, cancellationToken).ConfigureAwait(false);
                await Zip64EndOfCentralDirectoryLocator.WriteBlockAsync(_archiveStream, zip64EOCDRecordStart, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                WriteArchiveEpilogueNoCDChangesWork();
            }
        }

        // write normal eocd
        if (centralDirectoryChanged || (Changed != ChangeState.Unchanged))
        {
            await ZipEndOfCentralDirectoryBlock.WriteBlockAsync(_archiveStream, _entries.Count, startOfCentralDirectory, sizeOfCentralDirectory, _archiveComment, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _archiveStream.Seek(ZipEndOfCentralDirectoryBlock.TotalSize + _archiveComment.Length, SeekOrigin.Current);
        }
    }
}
