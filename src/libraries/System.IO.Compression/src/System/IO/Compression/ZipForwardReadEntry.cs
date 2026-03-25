// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression;

/// <summary>
/// Represents a single entry read from a ZIP archive by <see cref="ZipStreamReader"/>.
/// Provides metadata from the local file header and a <see cref="DataStream"/> for
/// reading the decompressed entry data.
/// </summary>
/// <remarks>
/// <para>
/// When <c>copyData</c> is <see langword="false"/> (the default), the <see cref="DataStream"/>
/// reads directly from the underlying archive stream. It is invalidated when the reader
/// advances to the next entry via <see cref="ZipStreamReader.GetNextEntry"/>. Any unread
/// data is automatically drained at that point.
/// </para>
/// <para>
/// When <c>copyData</c> is <see langword="true"/>, the decompressed data is copied into a
/// <see cref="MemoryStream"/> and the entry remains valid after the reader advances.
/// </para>
/// </remarks>
public sealed class ZipForwardReadEntry
{
    private uint _crc32;
    private long _compressedLength;
    private long _length;

    internal ZipForwardReadEntry(
        string fullName,
        ZipCompressionMethod compressionMethod,
        DateTimeOffset lastModified,
        uint crc32,
        long compressedLength,
        long length,
        ushort generalPurposeBitFlags,
        ushort versionNeeded,
        bool hasDataDescriptor,
        Stream? dataStream)
    {
        FullName = fullName;
        CompressionMethod = compressionMethod;
        LastModified = lastModified;
        _crc32 = crc32;
        _compressedLength = compressedLength;
        _length = length;
        GeneralPurposeBitFlags = generalPurposeBitFlags;
        VersionNeeded = versionNeeded;
        HasDataDescriptor = hasDataDescriptor;
        DataStream = dataStream;
    }

    /// <summary>
    /// Gets the full name (relative path) of the entry, including any directory path.
    /// </summary>
    public string FullName { get; }

    /// <summary>
    /// Gets the file name portion of the entry (the part after the last directory separator).
    /// </summary>
    public string Name => Path.GetFileName(FullName);

    /// <summary>
    /// Gets the compression method used for this entry.
    /// </summary>
    public ZipCompressionMethod CompressionMethod { get; }

    /// <summary>
    /// Gets the last modification date and time of the entry.
    /// </summary>
    public DateTimeOffset LastModified { get; }

    /// <summary>
    /// Gets the CRC-32 checksum of the uncompressed data.
    /// </summary>
    /// <remarks>
    /// When bit 3 (data descriptor) is set in the local header, this value is initially
    /// zero and is populated after the compressed data has been fully read.
    /// </remarks>
    [CLSCompliant(false)]
    public uint Crc32 => _crc32;

    /// <summary>
    /// Gets the compressed size of the entry in bytes.
    /// </summary>
    /// <remarks>
    /// When bit 3 (data descriptor) is set in the local header, this value is initially
    /// zero and is populated after the compressed data has been fully read.
    /// </remarks>
    public long CompressedLength => _compressedLength;

    /// <summary>
    /// Gets the uncompressed size of the entry in bytes.
    /// </summary>
    /// <remarks>
    /// When bit 3 (data descriptor) is set in the local header, this value is initially
    /// zero and is populated after the compressed data has been fully read.
    /// </remarks>
    public long Length => _length;

    /// <summary>
    /// Gets the raw general purpose bit flags from the local file header.
    /// </summary>
    [CLSCompliant(false)]
    public ushort GeneralPurposeBitFlags { get; }

    /// <summary>
    /// Gets a value indicating whether the entry is encrypted.
    /// </summary>
    public bool IsEncrypted => (GeneralPurposeBitFlags & 1) != 0;

    /// <summary>
    /// Gets a value indicating whether the entry represents a directory.
    /// </summary>
    public bool IsDirectory => FullName.Length > 0 && (FullName[^1] is '/' or '\\');

    /// <summary>
    /// Gets the minimum ZIP specification version needed to extract this entry.
    /// </summary>
    [CLSCompliant(false)]
    public ushort VersionNeeded { get; }

    /// <summary>
    /// Gets the decompressed data stream for this entry, or <see langword="null"/>
    /// if the entry has no data (e.g. a directory entry).
    /// </summary>
    /// <remarks>
    /// When <c>copyData</c> was <see langword="false"/> on the
    /// <see cref="ZipStreamReader.GetNextEntry"/> call that produced this entry,
    /// the stream reads directly from the archive and is invalidated when the reader
    /// advances to the next entry. When <c>copyData</c> was <see langword="true"/>,
    /// the data has been copied into a <see cref="MemoryStream"/> that remains valid
    /// independently.
    /// </remarks>
    public Stream? DataStream { get; internal set; }

    /// <summary>
    /// Extracts the entry to a file on disk.
    /// </summary>
    /// <param name="destinationFileName">The path of the file to create.</param>
    /// <param name="overwrite">
    /// <see langword="true"/> to overwrite an existing file; otherwise <see langword="false"/>.
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="destinationFileName"/> is null or empty.</exception>
    public void ExtractToFile(string destinationFileName, bool overwrite)
    {
        ArgumentException.ThrowIfNullOrEmpty(destinationFileName);

        FileMode mode = overwrite ? FileMode.Create : FileMode.CreateNew;
        using FileStream fs = new(destinationFileName, mode, FileAccess.Write, FileShare.None);
        DataStream?.CopyTo(fs);
    }

    /// <summary>
    /// Asynchronously extracts the entry to a file on disk.
    /// </summary>
    /// <param name="destinationFileName">The path of the file to create.</param>
    /// <param name="overwrite">
    /// <see langword="true"/> to overwrite an existing file; otherwise <see langword="false"/>.
    /// </param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <exception cref="ArgumentException"><paramref name="destinationFileName"/> is null or empty.</exception>
    public async Task ExtractToFileAsync(string destinationFileName, bool overwrite,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(destinationFileName);

        FileMode mode = overwrite ? FileMode.Create : FileMode.CreateNew;
        FileStream fs = new(destinationFileName, mode, FileAccess.Write, FileShare.None,
            bufferSize: 0x1000, useAsync: true);
        await using (fs.ConfigureAwait(false))
        {
            if (DataStream is not null)
            {
                await DataStream.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    internal bool HasDataDescriptor { get; }

    internal void UpdateDataDescriptor(uint crc32, long compressedLength, long length,
        uint runningCrc, long totalBytesRead)
    {
        if (runningCrc != crc32)
        {
            throw new InvalidDataException(SR.CrcMismatch);
        }

        if (totalBytesRead != length)
        {
            throw new InvalidDataException(SR.UnexpectedStreamLength);
        }

        _crc32 = crc32;
        _compressedLength = compressedLength;
        _length = length;
    }
}
