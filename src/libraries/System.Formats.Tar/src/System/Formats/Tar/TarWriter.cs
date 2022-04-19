// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace System.Formats.Tar
{
    /// <summary>
    /// Writes a tar archive into a stream.
    /// </summary>
    public sealed partial class TarWriter : IDisposable
    {
        private bool _wroteGEA;
        private bool _wroteEntries;
        private bool _isDisposed;
        private readonly bool _leaveOpen;
        private readonly Stream _archiveStream;
        private readonly IEnumerable<KeyValuePair<string, string>>? _globalExtendedAttributes;

        /// <summary>
        /// Initializes a <see cref="TarWriter"/> instance that can write tar entries to the specified stream, optionally leave the stream open upon disposal of this instance, and can optionally add a Global Extended Attributes entry at the beginning of the archive. When using this constructor, the format of the resulting archive is <see cref="TarFormat.Pax"/>.
        /// </summary>
        /// <param name="archiveStream">The stream to write to.</param>
        /// <param name="globalExtendedAttributes">An optional enumeration of string key-value pairs that represent Global Extended Attributes metadata that should apply to all subsquent entries. If <see langword="null"/>, then no Global Extended Attributes entry is written. If an empty instance is passed, a Global Extended Attributes entry is written with default values.</param>
        /// <param name="leaveOpen"><see langword="false"/> to dispose the <paramref name="archiveStream"/> when this instance is disposed; <see langword="true"/> to leave the stream open.</param>
        public TarWriter(Stream archiveStream, IEnumerable<KeyValuePair<string, string>>? globalExtendedAttributes = null, bool leaveOpen = false)
            : this(archiveStream, TarFormat.Pax, leaveOpen)
        {
            _globalExtendedAttributes = globalExtendedAttributes;
        }

        /// <summary>
        /// Initializes a <see cref="TarWriter"/> instance that can write tar entries to the specified stream, optionally leave the stream open upon disposal of this instance, and can specify the format of the underlying archive.
        /// </summary>
        /// <param name="archiveStream">The stream to write to.</param>
        /// <param name="archiveFormat">The format of the archive.</param>
        /// <param name="leaveOpen"><see langword="false"/> to dispose the <paramref name="archiveStream"/> when this instance is disposed; <see langword="true"/> to leave the stream open.</param>
        /// <remarks><para>If the selected <paramref name="archiveFormat"/> is <see cref="TarFormat.Pax"/>, no Global Extended Attributes entry is written. To write a PAX archive with a Global Extended Attributes entry inserted at the beginning of the archive, use the <see cref="TarWriter(Stream, IEnumerable{KeyValuePair{string, string}}?, bool)"/> constructor instead.</para>
        /// <para>The recommended format is <see cref="TarFormat.Pax"/> for its flexibility.</para></remarks>
        /// <exception cref="ArgumentNullException"><paramref name="archiveStream"/> is <see langword="null"/>.</exception>
        /// <exception cref="IOException"><paramref name="archiveStream"/> is unwritable.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="archiveFormat"/> is either <see cref="TarFormat.Unknown"/>, or not one of the other enum values.</exception>
        public TarWriter(Stream archiveStream, TarFormat archiveFormat, bool leaveOpen = false)
        {
            ArgumentNullException.ThrowIfNull(archiveStream);

            if (!archiveStream.CanWrite)
            {
                throw new IOException(SR.IO_NotSupported_UnwritableStream);
            }

            if (archiveFormat is not TarFormat.V7 and not TarFormat.Ustar and not TarFormat.Pax and not TarFormat.Gnu)
            {
                throw new ArgumentOutOfRangeException(nameof(archiveFormat));
            }

            _archiveStream = archiveStream;
            Format = archiveFormat;
            _leaveOpen = leaveOpen;
            _isDisposed = false;
            _wroteEntries = false;
            _wroteGEA = false;
            _globalExtendedAttributes = null;
        }

        /// <summary>
        /// The format of the archive.
        /// </summary>
        public TarFormat Format { get; private set; }

        /// <summary>
        /// Disposes the current <see cref="TarWriter"/> instance, and closes the archive stream if the <c>leaveOpen</c> argument was set to <see langword="false"/> in the constructor.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        // /// <summary>
        // /// Asynchronously disposes the current <see cref="TarWriter"/> instance, and closes the archive stream if the <c>leaveOpen</c> argument was set to <see langword="false"/> in the constructor.
        // /// </summary>
        // public ValueTask DisposeAsync()
        // {
        //     throw new NotImplementedException();
        // }

        /// <summary>
        /// Writes the specified file into the archive stream as a tar entry.
        /// </summary>
        /// <param name="fileName">The path to the file to write to the archive.</param>
        /// <param name="entryName">The name of the file as it should be represented in the archive. It should include the optional relative path and the filename.</param>
        /// <exception cref="ObjectDisposedException">The archive stream is disposed.</exception>
        /// <exception cref="ArgumentException"><paramref name="fileName"/> or <paramref name="entryName"/> is <see langword="null"/> or empty.</exception>
        /// <exception cref="IOException">An I/O problem ocurred.</exception>
        public void WriteEntry(string fileName, string? entryName)
        {
            ThrowIfDisposed();

            ArgumentException.ThrowIfNullOrEmpty(fileName);

            string fullPath = Path.GetFullPath(fileName);

            if (string.IsNullOrEmpty(entryName))
            {
                entryName = Path.GetFileName(fileName);
            }

            if (Format is TarFormat.Pax)
            {
                WriteGlobalExtendedAttributesEntryIfNeeded();
            }

            ReadFileFromDiskAndWriteToArchiveStreamAsEntry(fullPath, entryName);
        }

        // /// <summary>
        // /// Asynchronously writes the specified file into the archive stream as a tar entry.
        // /// </summary>
        // /// <param name="fileName">The path to the file to write to the archive.</param>
        // /// <param name="entryName">The name of the file as it should be represented in the archive. It should include the optional relative path and the filename.</param>
        // /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        // public Task WriteEntryAsync(string fileName, string? entryName, CancellationToken cancellationToken = default)
        // {
        //     throw new NotImplementedException();
        // }

        /// <summary>
        /// Writes the specified entry into the archive stream.
        /// </summary>
        /// <param name="entry">The tar entry to write.</param>
        /// <remarks><para>Before writing an entry to the archive, if you wrote data into the entry's <see cref="TarEntry.DataStream"/>, make sure to rewind it to the desired start position.</para>
        /// <para>These are the entry types supported for writing on each format:</para>
        /// <list type="bullet">
        /// <item>
        /// <para><see cref="TarFormat.V7"/></para>
        /// <list type="bullet">
        /// <item><see cref="TarEntryType.Directory"/></item>
        /// <item><see cref="TarEntryType.HardLink"/></item>
        /// <item><see cref="TarEntryType.SymbolicLink"/></item>
        /// <item><see cref="TarEntryType.V7RegularFile"/></item>
        /// </list>
        /// </item>
        /// <item>
        /// <para><see cref="TarFormat.Ustar"/>, <see cref="TarFormat.Pax"/> and <see cref="TarFormat.Gnu"/></para>
        /// <list type="bullet">
        /// <item><see cref="TarEntryType.BlockDevice"/></item>
        /// <item><see cref="TarEntryType.CharacterDevice"/></item>
        /// <item><see cref="TarEntryType.Directory"/></item>
        /// <item><see cref="TarEntryType.Fifo"/></item>
        /// <item><see cref="TarEntryType.HardLink"/></item>
        /// <item><see cref="TarEntryType.RegularFile"/></item>
        /// <item><see cref="TarEntryType.SymbolicLink"/></item>
        /// </list>
        /// </item>
        /// </list>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The archive stream is disposed.</exception>
        /// <exception cref="InvalidOperationException">The entry type of the <paramref name="entry"/> is not supported for writing.</exception>
        /// <exception cref="IOException">An I/O problem ocurred.</exception>
        public void WriteEntry(TarEntry entry)
        {
            ThrowIfDisposed();

            TarHelpers.VerifyEntryTypeIsSupported(entry.EntryType, Format, forWriting: true);

            WriteGlobalExtendedAttributesEntryIfNeeded();

            byte[] rented = ArrayPool<byte>.Shared.Rent(minimumLength: TarHelpers.RecordSize);
            Span<byte> buffer = rented.AsSpan(0, TarHelpers.RecordSize); // minimumLength means the array could've been larger
            buffer.Clear(); // Rented arrays aren't clean
            try
            {
                switch (Format)
                {
                    case TarFormat.V7:
                        entry._header.WriteAsV7(_archiveStream, buffer);
                        break;
                    case TarFormat.Ustar:
                        entry._header.WriteAsUstar(_archiveStream, buffer);
                        break;
                    case TarFormat.Pax:
                        entry._header.WriteAsPax(_archiveStream, buffer);
                        break;
                    case TarFormat.Gnu:
                        entry._header.WriteAsGnu(_archiveStream, buffer);
                        break;
                    case TarFormat.Unknown:
                    default:
                        throw new FormatException(string.Format(SR.TarInvalidFormat, Format));
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }

            _wroteEntries = true;
        }

        // /// <summary>
        // /// Asynchronously writes the specified entry into the archive stream.
        // /// </summary>
        // /// <param name="entry">The tar entry to write.</param>
        // /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        // /// <remarks><para>Before writing an entry to the archive, if you wrote data into the entry's <see cref="TarEntry.DataStream"/>, make sure to rewind it to the desired start position.</para>
        // /// <para>These are the entry types supported for writing on each format:</para>
        // /// <list type="bullet">
        // /// <item>
        // /// <para><see cref="TarFormat.V7"/></para>
        // /// <list type="bullet">
        // /// <item><see cref="TarEntryType.Directory"/></item>
        // /// <item><see cref="TarEntryType.HardLink"/></item>
        // /// <item><see cref="TarEntryType.SymbolicLink"/></item>
        // /// <item><see cref="TarEntryType.V7RegularFile"/></item>
        // /// </list>
        // /// </item>
        // /// <item>
        // /// <para><see cref="TarFormat.Ustar"/>, <see cref="TarFormat.Pax"/> and <see cref="TarFormat.Gnu"/></para>
        // /// <list type="bullet">
        // /// <item><see cref="TarEntryType.BlockDevice"/></item>
        // /// <item><see cref="TarEntryType.CharacterDevice"/></item>
        // /// <item><see cref="TarEntryType.Directory"/></item>
        // /// <item><see cref="TarEntryType.Fifo"/></item>
        // /// <item><see cref="TarEntryType.HardLink"/></item>
        // /// <item><see cref="TarEntryType.RegularFile"/></item>
        // /// <item><see cref="TarEntryType.SymbolicLink"/></item>
        // /// </list>
        // /// </item>
        // /// </list>
        // /// </remarks>
        // public Task WriteEntryAsync(TarEntry entry, CancellationToken cancellationToken = default)
        // {
        //     throw new NotImplementedException();
        // }

        // Disposes the current instance.
        // If 'disposing' is 'false', the method was called from the finalizer.
        private void Dispose(bool disposing)
        {
            if (disposing && !_isDisposed)
            {
                try
                {
                    WriteGlobalExtendedAttributesEntryIfNeeded();

                    if (_wroteEntries)
                    {
                        WriteFinalRecords();
                    }


                    if (!_leaveOpen)
                    {
                        _archiveStream.Dispose();
                    }
                }
                finally
                {
                    _isDisposed = true;
                }
            }
        }

        // If the underlying archive stream is disposed, throws 'ObjectDisposedException'.
        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().ToString());
            }
        }

        // Writes a Global Extended Attributes entry at the beginning of the archive.
        private void WriteGlobalExtendedAttributesEntryIfNeeded()
        {
            Debug.Assert(!_isDisposed);

            if (_wroteGEA || Format != TarFormat.Pax)
            {
                return;
            }

            Debug.Assert(!_wroteEntries); // The GEA entry can only be the first entry

            if (_globalExtendedAttributes != null)
            {
                byte[] rented = ArrayPool<byte>.Shared.Rent(minimumLength: TarHelpers.RecordSize);
                try
                {
                    Span<byte> buffer = rented.AsSpan(0, TarHelpers.RecordSize);
                    buffer.Clear(); // Rented arrays aren't clean
                    // Write the GEA entry regardless if it has values or not
                    TarHeader.WriteGlobalExtendedAttributesHeader(_archiveStream, buffer, _globalExtendedAttributes);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(rented);
                }
            }
            _wroteGEA = true;
        }

        // The spec indicates that the end of the archive is indicated
        // by two records consisting entirely of zero bytes.
        private void WriteFinalRecords()
        {
            byte[] emptyRecord = new byte[TarHelpers.RecordSize];
            _archiveStream.Write(emptyRecord);
            _archiveStream.Write(emptyRecord);
            _archiveStream.SetLength(_archiveStream.Position);
        }

        // Partial method for reading an entry from disk and writing it into the archive stream.
        partial void ReadFileFromDiskAndWriteToArchiveStreamAsEntry(string fullPath, string entryName);
    }
}
