// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression;

public partial class ZipArchive
{
    private ZipArchiveEntry? _forwardEntry;
    private byte[]? _forwardBuffer;
    // Keeps repeated GetNextEntry calls returning null without reading the remaining central-directory or end-record fields.
    private bool _forwardEnded;

    /// <summary>Reads the next local entry from an archive opened in <see cref="ZipArchiveMode.ForwardRead"/> mode.</summary>
    /// <returns>The next entry, or <see langword="null"/> at a central directory or end record.</returns>
    /// <remarks>
    /// Unread data from the previous entry is drained before advancing. Opened entries are
    /// checked for length and CRC; unopened entries are skipped without CRC validation.
    /// Entries can be opened only once, before advancing. The central directory is not validated.
    /// Do not continue using the reader after a read or parsing failure. Disposal does not drain input.
    /// Complete each archive or entry-stream operation before starting another or disposing the archive.
    /// </remarks>
    /// <exception cref="NotSupportedException">The archive is not in forward-read mode, or an entry uses an unsupported format.</exception>
    /// <exception cref="InvalidDataException">An entry or local header is corrupt or truncated.</exception>
    /// <exception cref="ObjectDisposedException">The archive has been disposed.</exception>
    public ZipArchiveEntry? GetNextEntry() =>
        GetNextEntryCoreAsync(useAsync: false, CancellationToken.None).GetAwaiter().GetResult();

    /// <summary>Asynchronously reads the next local entry from an archive opened in <see cref="ZipArchiveMode.ForwardRead"/> mode.</summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The next entry, or <see langword="null"/> at a central directory or end record.</returns>
    /// <remarks>
    /// Has the same lifetime and validation behavior as <see cref="GetNextEntry"/>.
    /// Cancellation before processing starts permits retry. If cancellation occurs during processing,
    /// stop using the reader because the input position may have changed.
    /// </remarks>
    /// <exception cref="NotSupportedException">The archive is not in forward-read mode, or an entry uses an unsupported format.</exception>
    /// <exception cref="InvalidDataException">An entry or local header is corrupt or truncated.</exception>
    /// <exception cref="ObjectDisposedException">The archive has been disposed.</exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public ValueTask<ZipArchiveEntry?> GetNextEntryAsync(CancellationToken cancellationToken = default) =>
        GetNextEntryCoreAsync(useAsync: true, cancellationToken);

    private async ValueTask<ZipArchiveEntry?> GetNextEntryCoreAsync(bool useAsync, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (_mode != ZipArchiveMode.ForwardRead)
        {
            throw new NotSupportedException(SR.ForwardReadModeRequired);
        }
        try
        {
            if (_forwardEnded)
            {
                return null;
            }

            // Shared scratch space for headers, extras, and draining; never buffers the archive.
            _forwardBuffer ??= new byte[4096];
            if (_forwardEntry is not null)
            {
                await _forwardEntry.DrainForwardEntryAsync(_forwardBuffer, useAsync, cancellationToken).ConfigureAwait(false);
                _forwardEntry.InvalidateForwardEntry();
                _forwardEntry = null;
            }

            // Read the next record's four-byte signature; premature EOF is a truncated archive.
            await ReadForwardExactlyAsync(_forwardBuffer.AsMemory(0, sizeof(uint)), useAsync, cancellationToken).ConfigureAwait(false);
            // Local entries end at the central directory or an end record (including empty archives).
            // Recognize this boundary without parsing or validating the remaining archive.
            if (_forwardBuffer.AsSpan(0, sizeof(uint)).SequenceEqual(ZipCentralDirectoryFileHeader.SignatureConstantBytes) ||
                _forwardBuffer.AsSpan(0, sizeof(uint)).SequenceEqual(ZipEndOfCentralDirectoryBlock.SignatureConstantBytes) ||
                _forwardBuffer.AsSpan(0, sizeof(uint)).SequenceEqual(Zip64EndOfCentralDirectoryRecord.SignatureConstantBytes))
            {
                _forwardEnded = true; // Subsequent calls return null without reading more input.
                return null;
            }
            if (!_forwardBuffer.AsSpan(0, sizeof(uint)).SequenceEqual(ZipLocalFileHeader.SignatureConstantBytes))
            {
                throw new InvalidDataException(SR.LocalFileHeaderCorrupt);
            }

            await ReadForwardExactlyAsync(
                _forwardBuffer.AsMemory(sizeof(uint), ZipLocalFileHeader.SizeOfLocalHeader - sizeof(uint)),
                useAsync, cancellationToken).ConfigureAwait(false);
            int nameLength = BinaryPrimitives.ReadUInt16LittleEndian(_forwardBuffer.AsSpan(ZipLocalFileHeader.FieldLocations.FilenameLength));
            int extraLength = BinaryPrimitives.ReadUInt16LittleEndian(_forwardBuffer.AsSpan(ZipLocalFileHeader.FieldLocations.ExtraFieldLength));
            byte[] name = new byte[nameLength];
            await ReadForwardExactlyAsync(name, useAsync, cancellationToken).ConfigureAwait(false);
            ZipArchiveEntry entry = new(this, _forwardBuffer, name);
            while (extraLength != 0)
            {
                int length = Math.Min(extraLength, _forwardBuffer.Length);
                await ReadForwardExactlyAsync(_forwardBuffer.AsMemory(0, length), useAsync, cancellationToken).ConfigureAwait(false);
                extraLength -= length;
            }
            _forwardEntry = entry;
            return entry;
        }
        catch (EndOfStreamException e)
        {
            throw new InvalidDataException(SR.LocalFileHeaderCorrupt, e);
        }
    }

    private ValueTask ReadForwardExactlyAsync(Memory<byte> buffer, bool useAsync, CancellationToken cancellationToken)
    {
        if (useAsync)
        {
            return _archiveStream.ReadExactlyAsync(buffer, cancellationToken);
        }
        _archiveStream.ReadExactly(buffer.Span);
        return ValueTask.CompletedTask;
    }

    internal void ThrowIfForwardRead()
    {
        if (_mode == ZipArchiveMode.ForwardRead)
        {
            throw new NotSupportedException(SR.ForwardReadOperationNotSupported);
        }
    }

    internal void EnsureCurrentForwardEntry(ZipArchiveEntry entry)
    {
        ThrowIfDisposed();
        if (_forwardEntry != entry)
        {
            throw new InvalidOperationException(SR.ForwardReadEntryExpired);
        }
    }

    private void DisposeForwardReader()
    {
        _forwardEntry?.InvalidateForwardEntry();
        _forwardEntry = null;
        _forwardBuffer = null;
    }
}
