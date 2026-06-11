// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Formats.Tar
{
    /// <summary>
    /// Writes a tar archive into a stream.
    /// </summary>
    public sealed partial class TarWriter : IDisposable, IAsyncDisposable
    {
        private bool _wroteEntries;
        private bool _isDisposed;
        private readonly bool _leaveOpen;
        private readonly Stream _archiveStream;
        private readonly TarHardLinkMode _hardLinkMode;
        private int _nextGlobalExtendedAttributesEntryNumber;

        /// <summary>
        /// Initializes a <see cref="TarWriter"/> instance that can write tar entries to the specified stream and closes the <paramref name="archiveStream"/> upon disposal of this instance.
        /// </summary>
        /// <param name="archiveStream">The stream to write to.</param>
        /// <remarks>When using this constructor, <see cref="TarEntryFormat.Pax"/> is used as the default format of the entries written to the archive using the <see cref="WriteEntry(string, string?)"/> method.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="archiveStream"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="archiveStream"/> does not support writing.</exception>
        /// <remarks>The <see cref="TarEntryFormat.Pax"/> format is the default format as it is the most flexible and POSIX compatible. This is the only format with which <see cref="TarWriter"/> reads and stores <c>atime</c> and <c>ctime</c> when creating entries from filesystem entries.</remarks>
        public TarWriter(Stream archiveStream)
            : this(archiveStream, TarEntryFormat.Pax, leaveOpen: false)
        {
        }

        /// <summary>
        /// Initializes a <see cref="TarWriter"/> instance that can write tar entries to the specified stream and optionally leaves the stream open upon disposal of this instance. When using this constructor, the format of the resulting archive is <see cref="TarEntryFormat.Pax"/>.
        /// </summary>
        /// <param name="archiveStream">The stream to write to.</param>
        /// <param name="leaveOpen"><see langword="false"/> to dispose the <paramref name="archiveStream"/> when this instance is disposed; <see langword="true"/> to leave the stream open.</param>
        /// <exception cref="ArgumentNullException"><paramref name="archiveStream"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="archiveStream"/> is unwritable.</exception>
        /// <remarks>The <see cref="TarEntryFormat.Pax"/> format is the default format as it is the most flexible and POSIX compatible. This is the only format with which <see cref="TarWriter"/> reads and stores <c>atime</c> and <c>ctime</c> when creating entries from filesystem entries.</remarks>
        public TarWriter(Stream archiveStream, bool leaveOpen = false)
            : this(archiveStream, TarEntryFormat.Pax, leaveOpen)
        {
        }

        /// <summary>
        /// Initializes a <see cref="TarWriter"/> instance that can write tar entries to the specified stream, optionally leaves the stream open upon disposal of
        /// this instance, and can optionally specify the format when writing entries using the <see cref="WriteEntry(string, string?)"/> method.
        /// </summary>
        /// <param name="archiveStream">The stream to write to.</param>
        /// <param name="format">The format to use when calling <see cref="WriteEntry(string, string?)"/>. The default value is <see cref="TarEntryFormat.Pax"/>.</param>
        /// <param name="leaveOpen"><see langword="false"/> to dispose the <paramref name="archiveStream"/> when this instance is disposed;
        /// <see langword="true"/> to leave the stream open. The default is <see langword="false"/>.</param>
        /// <remarks>The recommended format is <see cref="TarEntryFormat.Pax"/> for its flexibility.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="archiveStream"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="archiveStream"/> is unwritable.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is either <see cref="TarEntryFormat.Unknown"/>, or not one of the other enum values.</exception>
        /// <remarks>The <see cref="TarEntryFormat.Pax"/> format is the default for the other <see cref="TarWriter"/> constructors. This is the recommended format as it is the most flexible and POSIX compatible. This is the only format  with which <see cref="TarWriter"/> reads and stores <c>atime</c> and <c>ctime</c> when creating entries from filesystem entries.</remarks>
        public TarWriter(Stream archiveStream, TarEntryFormat format = TarEntryFormat.Pax, bool leaveOpen = false)
            : this(archiveStream, CreateOptionsForFormat(format), leaveOpen)
        {
        }

        private static TarWriterOptions CreateOptionsForFormat(TarEntryFormat format)
        {
            if (format is not TarEntryFormat.V7 and not TarEntryFormat.Ustar and not TarEntryFormat.Pax and not TarEntryFormat.Gnu)
            {
                throw new ArgumentOutOfRangeException(nameof(format));
            }

            return new TarWriterOptions() { Format = format };
        }

        /// <summary>
        /// Initializes a <see cref="TarWriter"/> instance that can write tar entries to the specified stream using the specified options,
        /// and optionally leaves the stream open upon disposal of this instance.
        /// </summary>
        /// <param name="archiveStream">The stream to write to.</param>
        /// <param name="options">The options that configure the behavior of the writer.</param>
        /// <param name="leaveOpen"><see langword="false"/> to dispose the <paramref name="archiveStream"/> when this instance is disposed;
        /// <see langword="true"/> to leave the stream open. The default is <see langword="false"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="archiveStream"/> or <paramref name="options"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="archiveStream"/> is unwritable.</exception>
        public TarWriter(Stream archiveStream, TarWriterOptions options, bool leaveOpen = false)
        {
            ArgumentNullException.ThrowIfNull(archiveStream);
            ArgumentNullException.ThrowIfNull(options);

            if (!archiveStream.CanWrite)
            {
                throw new ArgumentException(SR.IO_NotSupported_UnwritableStream);
            }

            _archiveStream = archiveStream;
            Format = options.Format;
            _hardLinkMode = options.HardLinkMode;
            _leaveOpen = leaveOpen;
            _isDisposed = false;
            _wroteEntries = false;
            _nextGlobalExtendedAttributesEntryNumber = 1;
        }

        /// <summary>
        /// The format of the entries when writing entries to the archive using the <see cref="WriteEntry(string, string?)"/> method.
        /// </summary>
        public TarEntryFormat Format { get; }

        /// <summary>
        /// Disposes the current <see cref="TarWriter"/> instance, and closes the archive stream if the <c>leaveOpen</c> argument was set to <see langword="false"/> in the constructor.
        /// </summary>
        public void Dispose()
        {
            ValueTask vt = DisposeCoreAsync<SyncReadWriteAdapter>();
            Debug.Assert(vt.IsCompleted, "Synchronous Dispose completed asynchronously.");
            vt.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously disposes the current <see cref="TarWriter"/> instance, and closes the archive stream if the <c>leaveOpen</c> argument was set to <see langword="false"/> in the constructor.
        /// </summary>
        public ValueTask DisposeAsync() => DisposeCoreAsync<AsyncReadWriteAdapter>();

        private async ValueTask DisposeCoreAsync<TAdapter>()
            where TAdapter : IReadWriteAdapter
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                if (_wroteEntries)
                {
                    await WriteFinalRecordsCoreAsync<TAdapter>().ConfigureAwait(false);
                }

                if (!_leaveOpen)
                {
                    await TAdapter.DisposeAsync(_archiveStream).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Writes the specified file into the archive stream as a tar entry.
        /// </summary>
        /// <param name="fileName">The path to the file to write to the archive.</param>
        /// <param name="entryName">The name of the file as it should be represented in the archive. It should include the optional relative path and the filename.</param>
        /// <exception cref="ObjectDisposedException">The archive stream is disposed.</exception>
        /// <exception cref="ArgumentException"><paramref name="fileName"/> or <paramref name="entryName"/> is <see langword="null"/> or empty.</exception>
        /// <exception cref="IOException">An I/O problem occurred.</exception>
        /// <remarks>
        /// <para>The entry will be created using the format specified in the <see cref="TarWriter(Stream, TarEntryFormat, bool)"/> constructor, or will use <see cref="TarEntryFormat.Pax"/> if other constructors are used.</para>
        /// <para>If the format is <see cref="TarEntryFormat.Pax"/>, the <c>atime</c> and <c>ctime</c> from the file will be stored in the <see cref="PaxTarEntry.ExtendedAttributes"/> dictionary. If the format is <see cref="TarEntryFormat.Gnu"/>, this method will not set a value for <see cref="GnuTarEntry.AccessTime"/> and <see cref="GnuTarEntry.ChangeTime"/> because most TAR tools do not support these fields for this format.</para>
        /// </remarks>
        public void WriteEntry(string fileName, string? entryName)
        {
            (string fullPath, string actualEntryName) = ValidateWriteEntryArguments(fileName, entryName);
            ReadFileFromDiskAndWriteToArchiveStreamAsEntry(fullPath, actualEntryName);
        }

        /// <summary>
        /// Asynchronously writes the specified file into the archive stream as a tar entry.
        /// </summary>
        /// <param name="fileName">The path to the file to write to the archive.</param>
        /// <param name="entryName">The name of the file as it should be represented in the archive. It should include the optional relative path and the filename.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        /// <exception cref="ObjectDisposedException">The archive stream is disposed.</exception>
        /// <exception cref="ArgumentException"><paramref name="fileName"/> or <paramref name="entryName"/> is <see langword="null"/> or empty.</exception>
        /// <exception cref="IOException">An I/O problem occurred.</exception>
        /// <remarks>
        /// <para>The entry will be created using the format specified in the <see cref="TarWriter(Stream, TarEntryFormat, bool)"/> constructor, or will use <see cref="TarEntryFormat.Pax"/> if other constructors are used.</para>
        /// <para>If the format is <see cref="TarEntryFormat.Pax"/>, the <c>atime</c> and <c>ctime</c> from the file will be stored in the <see cref="PaxTarEntry.ExtendedAttributes"/> dictionary. If the format is <see cref="TarEntryFormat.Gnu"/>, this method will not set a value for <see cref="GnuTarEntry.AccessTime"/> and <see cref="GnuTarEntry.ChangeTime"/> because most TAR tools do not support these fields for this format.</para>
        /// </remarks>
        public Task WriteEntryAsync(string fileName, string? entryName, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            (string fullPath, string actualEntryName) = ValidateWriteEntryArguments(fileName, entryName);
            return ReadFileFromDiskAndWriteToArchiveStreamAsEntryCoreAsync<AsyncReadWriteAdapter>(fullPath, actualEntryName, FileOptions.Asynchronous, cancellationToken).AsTask();
        }

        // Reads an entry from disk and writes it into the archive stream.
        private void ReadFileFromDiskAndWriteToArchiveStreamAsEntry(string fullPath, string entryName)
        {
            ValueTask vt = ReadFileFromDiskAndWriteToArchiveStreamAsEntryCoreAsync<SyncReadWriteAdapter>(fullPath, entryName, FileOptions.None, CancellationToken.None);
            Debug.Assert(vt.IsCompleted, "Synchronous WriteEntry completed asynchronously.");
            vt.GetAwaiter().GetResult();
        }

        // Reads an entry from disk and writes it into the archive stream.
        private async ValueTask ReadFileFromDiskAndWriteToArchiveStreamAsEntryCoreAsync<TAdapter>(string fullPath, string entryName, FileOptions fileOptions, CancellationToken cancellationToken)
            where TAdapter : IReadWriteAdapter
        {
            TarEntry entry = ConstructEntryForWriting(fullPath, entryName, fileOptions);

            await WriteEntryCoreAsync<TAdapter>(entry, cancellationToken).ConfigureAwait(false);
            if (entry._header._dataStream != null)
            {
                await TAdapter.DisposeAsync(entry._header._dataStream).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Writes the specified entry into the archive stream.
        /// </summary>
        /// <param name="entry">The tar entry to write.</param>
        /// <remarks><para>Before writing an entry to the archive, if you wrote data into the entry's <see cref="TarEntry.DataStream"/>, make sure to rewind it to the desired start position.</para>
        /// <para>These are the entry types supported for writing on each format:</para>
        /// <list type="bullet">
        /// <item>
        /// <para><see cref="TarEntryFormat.V7"/></para>
        /// <list type="bullet">
        /// <item><see cref="TarEntryType.Directory"/></item>
        /// <item><see cref="TarEntryType.HardLink"/></item>
        /// <item><see cref="TarEntryType.SymbolicLink"/></item>
        /// <item><see cref="TarEntryType.V7RegularFile"/></item>
        /// </list>
        /// </item>
        /// <item>
        /// <para><see cref="TarEntryFormat.Ustar"/>, <see cref="TarEntryFormat.Pax"/> and <see cref="TarEntryFormat.Gnu"/></para>
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
        /// <exception cref="ArgumentException">The entry type is <see cref="TarEntryType.HardLink"/> or <see cref="TarEntryType.SymbolicLink"/> and the <see cref="TarEntry.LinkName"/> is <see langword="null"/> or empty.</exception>
        /// <exception cref="ObjectDisposedException">The archive stream is disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="entry"/> is <see langword="null"/>.</exception>
        /// <exception cref="IOException">An I/O problem occurred.</exception>
        /// <remarks>
        /// <para>When writing a <see cref="GnuTarEntry"/> using this method, if <see cref="GnuTarEntry.AccessTime"/> and/or <see cref="GnuTarEntry.ChangeTime" /> are set, they will be preserved in the archive. These fields are unsupported by most TAR tools, so to ensure the archive is readable by other tools, make sure to set <see cref="GnuTarEntry.AccessTime"/> and <see cref="GnuTarEntry.ChangeTime"/> to <see langword="default"/> or <see cref="DateTimeOffset.MinValue"/>.</para>
        /// <para>To ensure an entry preserves the <c>atime</c> and <c>ctime</c> values and it is readable by other tools, it is recommended to convert the entry to <see cref="PaxTarEntry"/> instead. In that format, the two values get stored in the <see cref="PaxTarEntry.ExtendedAttributes"/>. The <see cref="TarEntryFormat.Pax"/> format is used as the default format by <see cref="TarWriter"/> as it is the most flexible and POSIX compatible.</para>
        /// </remarks>
        public void WriteEntry(TarEntry entry)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            ArgumentNullException.ThrowIfNull(entry);
            ValidateEntryLinkName(entry._header._typeFlag, entry._header._linkName);
            ValidateStreamsSeekability(entry);

            ValueTask vt = WriteEntryCoreAsync<SyncReadWriteAdapter>(entry, CancellationToken.None);
            Debug.Assert(vt.IsCompleted, "Synchronous WriteEntry completed asynchronously.");
            vt.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously writes the specified entry into the archive stream.
        /// </summary>
        /// <param name="entry">The tar entry to write.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <remarks><para>Before writing an entry to the archive, if you wrote data into the entry's <see cref="TarEntry.DataStream"/>, make sure to rewind it to the desired start position.</para>
        /// <para>These are the entry types supported for writing on each format:</para>
        /// <list type="bullet">
        /// <item>
        /// <para><see cref="TarEntryFormat.V7"/></para>
        /// <list type="bullet">
        /// <item><see cref="TarEntryType.Directory"/></item>
        /// <item><see cref="TarEntryType.HardLink"/></item>
        /// <item><see cref="TarEntryType.SymbolicLink"/></item>
        /// <item><see cref="TarEntryType.V7RegularFile"/></item>
        /// </list>
        /// </item>
        /// <item>
        /// <para><see cref="TarEntryFormat.Ustar"/>, <see cref="TarEntryFormat.Pax"/> and <see cref="TarEntryFormat.Gnu"/></para>
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
        /// <exception cref="ArgumentException">The entry type is <see cref="TarEntryType.HardLink"/> or <see cref="TarEntryType.SymbolicLink"/> and the <see cref="TarEntry.LinkName"/> is <see langword="null"/> or empty.</exception>
        /// <exception cref="ObjectDisposedException">The archive stream is disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="entry"/> is <see langword="null"/>.</exception>
        /// <exception cref="IOException">An I/O problem occurred.</exception>
        public Task WriteEntryAsync(TarEntry entry, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            ObjectDisposedException.ThrowIf(_isDisposed, this);
            ArgumentNullException.ThrowIfNull(entry);
            ValidateEntryLinkName(entry._header._typeFlag, entry._header._linkName);
            ValidateStreamsSeekability(entry);
            return WriteEntryCoreAsync<AsyncReadWriteAdapter>(entry, cancellationToken).AsTask();
        }

        // Portion of the WriteEntry methods that rents a buffer and writes to the archive.
        private async ValueTask WriteEntryCoreAsync<TAdapter>(TarEntry entry, CancellationToken cancellationToken)
            where TAdapter : IReadWriteAdapter
        {
            byte[] rented = ArrayPool<byte>.Shared.Rent(minimumLength: TarHelpers.RecordSize);
            Memory<byte> buffer = rented.AsMemory(0, TarHelpers.RecordSize); // minimumLength means the array could've been larger
            buffer.Span.Clear(); // Rented arrays aren't clean
            try
            {
                ValueTask task = entry.Format switch
                {
                    TarEntryFormat.V7 => entry._header.WriteAsV7CoreAsync<TAdapter>(_archiveStream, buffer, cancellationToken),
                    TarEntryFormat.Ustar => entry._header.WriteAsUstarCoreAsync<TAdapter>(_archiveStream, buffer, cancellationToken),
                    TarEntryFormat.Pax when entry._header._typeFlag is TarEntryType.GlobalExtendedAttributes => entry._header.WriteAsPaxGlobalExtendedAttributesCoreAsync<TAdapter>(_archiveStream, buffer, _nextGlobalExtendedAttributesEntryNumber++, cancellationToken),
                    TarEntryFormat.Pax => entry._header.WriteAsPaxCoreAsync<TAdapter>(_archiveStream, buffer, cancellationToken),
                    TarEntryFormat.Gnu => entry._header.WriteAsGnuCoreAsync<TAdapter>(_archiveStream, buffer, cancellationToken),
                    _ => throw new InvalidDataException(SR.Format(SR.TarInvalidFormat, Format)),
                };
                await task.ConfigureAwait(false);

                _wroteEntries = true;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }

        // The spec indicates that the end of the archive is indicated
        // by two records consisting entirely of zero bytes.
        // This method is called from Dispose/DisposeAsync, so we don't want to propagate a cancelled CancellationToken.
        private async ValueTask WriteFinalRecordsCoreAsync<TAdapter>()
            where TAdapter : IReadWriteAdapter
        {
            const int TwoRecordSize = TarHelpers.RecordSize * 2;

            byte[] twoEmptyRecords = ArrayPool<byte>.Shared.Rent(TwoRecordSize);
            try
            {
                Array.Clear(twoEmptyRecords, 0, TwoRecordSize);
                await TAdapter.WriteAsync(_archiveStream, twoEmptyRecords.AsMemory(0, TwoRecordSize), CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(twoEmptyRecords);
            }
        }

        private (string, string) ValidateWriteEntryArguments(string fileName, string? entryName)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            ArgumentException.ThrowIfNullOrEmpty(fileName);

            string fullPath = Path.GetFullPath(fileName);
            string? actualEntryName = string.IsNullOrEmpty(entryName) ? Path.GetFileName(fileName) : entryName;

            return (fullPath, actualEntryName);
        }

        private void ValidateStreamsSeekability(TarEntry entry)
        {
            if (!_archiveStream.CanSeek && entry._header._dataStream != null && !entry._header._dataStream.CanSeek)
            {
                throw new IOException(SR.Format(SR.TarStreamSeekabilityUnsupportedCombination, entry.Name));
            }
        }

        private static void ValidateEntryLinkName(TarEntryType entryType, string? linkName)
        {
            if (entryType is TarEntryType.HardLink or TarEntryType.SymbolicLink)
            {
                if (string.IsNullOrEmpty(linkName))
                {
                    throw new ArgumentException(SR.TarEntryHardLinkOrSymlinkLinkNameEmpty, "entry");
                }
            }
        }
    }
}
