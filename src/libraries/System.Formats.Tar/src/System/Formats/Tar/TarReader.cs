// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Formats.Tar
{
    /// <summary>
    /// Class that can read a tar archive from a stream.
    /// </summary>
    public sealed class TarReader : IDisposable, IAsyncDisposable
    {
        private bool _isDisposed;
        private readonly bool _leaveOpen;
        private TarEntry? _previouslyReadEntry;
        private List<Stream>? _dataStreamsToDispose;
        private bool _readFirstEntry;

        internal Stream _archiveStream;

        /// <summary>
        /// Initializes a <see cref="TarReader"/> instance that can read tar entries from the specified stream, and can optionally leave the stream open upon disposal of this instance.
        /// </summary>
        /// <param name="archiveStream">The stream to read from.</param>
        /// <param name="leaveOpen"><see langword="false"/> to dispose the <paramref name="archiveStream"/> when this instance is disposed; <see langword="true"/> to leave the stream open.</param>
        /// <exception cref="IOException"><paramref name="archiveStream"/> is unreadable.</exception>
        public TarReader(Stream archiveStream!!, bool leaveOpen = false)
        {
            if (!archiveStream.CanRead)
            {
                throw new IOException(SR.IO_NotSupported_UnreadableStream);
            }

            _archiveStream = archiveStream;
            _leaveOpen = leaveOpen;

            _previouslyReadEntry = null;
            GlobalExtendedAttributes = null;
            Format = TarFormat.Unknown;
            _isDisposed = false;
            _readFirstEntry = false;
        }

        /// <summary>
        /// The format of the archive. It is initially <see cref="TarFormat.Unknown"/>. The archive format is detected after the first call to <see cref="GetNextEntry(bool)"/> or <see cref="GetNextEntryAsync(bool, CancellationToken)"/>.
        /// </summary>
        public TarFormat Format { get; private set; }

        /// <summary>
        /// <para>If the archive format is <see cref="TarFormat.Pax"/>, returns a read-only dictionary containing the string key-value pairs of the Global Extended Attributes in the first entry of the archive.</para>
        /// <para>If there is no Global Extended Attributes entry at the beginning of the archive, this returns an empty read-only dictionary.</para>
        /// <para>If the first entry has not been read by calling <see cref="GetNextEntry(bool)"/> or <see cref="GetNextEntryAsync(bool, CancellationToken)"/>, this returns <see langword="null"/>.</para>
        /// </summary>
        public IReadOnlyDictionary<string, string>? GlobalExtendedAttributes { get; private set; }

        /// <summary>
        /// Disposes the current <see cref="TarReader"/> instance, and disposes the streams of all the entries that were read from the archive.
        /// </summary>
        /// <remarks>The <see cref="TarEntry.DataStream"/> property of any entry can be replaced with a new stream. If the user decides to replace it on a <see cref="TarEntry"/> instance that was obtained using a <see cref="TarReader"/>, the underlying stream gets disposed immediately, freeing the <see cref="TarReader"/> of origin from the responsibility of having to dispose it.</remarks>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Asynchronously disposes the current <see cref="TarReader"/> instance, and disposes the streams of all the entries that were read from the archive.
        /// </summary>
        /// <remarks>The <see cref="TarEntry.DataStream"/> property of any entry can be replaced with a new stream. If the user decides to replace it on a <see cref="TarEntry"/> instance that was obtained using a <see cref="TarReader"/>, the underlying stream gets disposed immediately, freeing the <see cref="TarReader"/> of origin from the responsibility of having to dispose it.</remarks>
        public ValueTask DisposeAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retrieves the next entry from the archive stream.
        /// </summary>
        /// <param name="copyData"><para>Set it to <see langword="true"/> to copy the data of the entry into a new <see cref="MemoryStream"/>. This is helpful when the underlying archive stream is unseekable, and the data needs to be accessed later.</para>
        /// <para>Set it to <see langword="false"/> if the data should not be copied into a new stream. If the underlying stream is unseekable, the user has the responsibility of reading and processing the <see cref="TarEntry.DataStream"/> immediately after calling this method.</para>
        /// <para>The default value is <see langword="false"/>.</para></param>
        /// <returns>A <see cref="TarEntry"/> instance if a valid entry was found, or <see langword="null"/> if the end of the archive has been reached.</returns>
        /// <exception cref="FormatException"><para>The archive is malformed.</para>
        /// <para>-or-</para>
        /// <para>The archive contains entries in different formats.</para>
        /// <para>-or-</para>
        /// <para>More than one Global Extended Attributes Entry was found in the current <see cref="TarFormat.Pax"/> archive.</para>
        /// <para>-or-</para>
        /// <para>Two or more Extended Attributes entries were found consecutively in the current <see cref="TarFormat.Pax"/> archive.</para></exception>
        /// <exception cref="IOException">An I/O problem ocurred.</exception>
        public TarEntry? GetNextEntry(bool copyData = false)
        {
            Debug.Assert(_archiveStream.CanRead);

            if (_archiveStream.CanSeek && _archiveStream.Length == 0)
            {
                // Attempting to get the next entry on an empty tar stream
                return null;
            }

            AdvanceDataStreamIfNeeded();

            if (TryGetNextEntryHeader(out TarHeader header, copyData))
            {
                if (!_readFirstEntry)
                {
                    Debug.Assert(Format == TarFormat.Unknown);
                    Format = header._format;
                    _readFirstEntry = true;
                }
                else if (header._format != Format)
                {
                    throw new FormatException(SR.TarEntriesInDifferentFormats);
                }

                TarEntry entry = Format switch
                {
                    TarFormat.Pax => new PaxTarEntry(header, this),
                    TarFormat.Gnu => new GnuTarEntry(header, this),
                    TarFormat.Ustar => new UstarTarEntry(header, this),
                    TarFormat.V7 or TarFormat.Unknown or _ => new V7TarEntry(header, this),
                };

                _previouslyReadEntry = entry;
                PreserveDataStreamForDisposalIfNeeded(entry);
                return entry;
            }

            return null;
        }

        /// <summary>
        /// Asynchronously retrieves the next entry from the archive stream.
        /// </summary>
        /// <param name="copyData"><para>Set it to <see langword="true"/> to copy the data of the entry into a new <see cref="MemoryStream"/>. This is helpful when the underlying archive stream is unseekable, and the data needs to be accessed later.</para>
        /// <para>Set it to <see langword="false"/> if the data should not be copied into a new stream. If the underlying stream is unseekable, the user has the responsibility of reading and processing the <see cref="TarEntry.DataStream"/> immediately after calling this method.</para>
        /// <para>The default value is <see langword="false"/>.</para></param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>A value task containing a <see cref="TarEntry"/> instance if a valid entry was found, or <see langword="null"/> if the end of the archive has been reached.</returns>
        public ValueTask<TarEntry?> GetNextEntryAsync(bool copyData = false, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        // Moves the underlying archive stream position pointer to the beginning of the next header.
        internal void AdvanceDataStreamIfNeeded()
        {
            if (_previouslyReadEntry == null)
            {
                return;
            }

            if (_archiveStream.CanSeek)
            {
                Debug.Assert(_previouslyReadEntry._header._endOfHeaderAndDataAndBlockAlignment > 0);
                _archiveStream.Position = _previouslyReadEntry._header._endOfHeaderAndDataAndBlockAlignment;
            }
            else if (_previouslyReadEntry._header._size > 0)
            {
                // When working with seekable streams, every time we return an entry, we avoid advancing the pointer beyond the data section
                // This is so the user can read the data if desired. But if the data was not read by the user, we need to advance the pointer
                // here until it's located at the beginning of the next entry header.
                // This should only be done if the previous entry came from a TarReader and it still had its original SubReadStream or SeekableSubReadStream.

                if (_previouslyReadEntry._header._dataStream is not SubReadStream dataStream)
                {
                    return;
                }

                if (!dataStream.WasStreamAdvanced)
                {
                    // If the user did not advance the position, we need to make sure the position
                    // pointer is located at the beginning of the next header.
                    if (dataStream.Position < (_previouslyReadEntry._header._size - 1))
                    {
                        long bytesToSkip = _previouslyReadEntry._header._size - dataStream.Position;
                        TarHelpers.AdvanceStream(_archiveStream, bytesToSkip);
                        TarHelpers.SkipBlockAlignmentPadding(_archiveStream, _previouslyReadEntry._header._size);
                        dataStream.WasStreamAdvanced = true; // Now the pointer is beyond the limit, so any read attempts should throw
                    }
                }
            }
        }

        // Disposes the current instance.
        // If 'disposing' is 'false', the method was called from the finalizer.
        private void Dispose(bool disposing)
        {
            if (disposing && !_isDisposed)
            {
                try
                {
                    if (!_leaveOpen && _dataStreamsToDispose?.Count > 0)
                    {
                        foreach (Stream s in _dataStreamsToDispose)
                        {
                            s.Dispose();
                        }
                    }
                }
                finally
                {
                    _isDisposed = true;
                }
            }
        }

        // Attempts to read the next tar archive entry header.
        // Returns true if an entry header was collected successfully, false otherwise.
        // An entry header represents any typeflag that is contains metadata.
        // Metadata typeflags: ExtendedAttributes, GlobalExtendedAttributes, LongLink, LongPath.
        // Metadata typeflag entries get handled internally by this method until a valid header entry can be returned.
        private bool TryGetNextEntryHeader(out TarHeader header, bool copyData)
        {
            header = default;

            // Set the initial format that is expected to be retrieved when calling TarHeader.TryReadAttributes.
            // If the archive format is set to unknown here, it means this is the first entry we read and the value will be changed as fields get discovered.
            // If the archive format is initially detected as pax, then any subsequent entries detected as ustar will be assumed to be pax.
            header._format = Format;

            if (!header.TryGetNextHeader(_archiveStream, copyData))
            {
                return false;
            }

            // Special case: First header. Collect GEA from data section, then get next entry.
            if (header._typeFlag is TarEntryType.GlobalExtendedAttributes)
            {
                if (GlobalExtendedAttributes != null)
                {
                    // We can only have one extended attributes entry.
                    throw new FormatException(SR.TarTooManyGlobalExtendedAttributesEntries);
                }

                GlobalExtendedAttributes = header._extendedAttributes?.AsReadOnly();

                header = default;
                header._format = TarFormat.Pax;
                try
                {
                    if (!header.TryGetNextHeader(_archiveStream, copyData))
                    {
                        return false;
                    }
                }
                catch (EndOfStreamException)
                {
                    // Edge case: The only entry in the archive was a Global Extended Attributes entry
                    Format = TarFormat.Pax;
                    return false;
                }
                if (header._typeFlag == TarEntryType.GlobalExtendedAttributes)
                {
                    throw new FormatException(SR.TarTooManyGlobalExtendedAttributesEntries);
                }
            }

            // If a metadata typeflag entry is retrieved, handle it here, then read the next entry
            if (header._typeFlag is TarEntryType.ExtendedAttributes or TarEntryType.LongLink or TarEntryType.LongPath)
            {
                TarHeader actualEntryHeader = default;

                // We should know by now the format of the archive based on the first retrieved entry
                actualEntryHeader._format = header._format;

                // Now get the actual entry
                if (!actualEntryHeader.TryGetNextHeader(_archiveStream, copyData))
                {
                    return false;
                }

                // Should never read a GEA entry at this point
                if (header._typeFlag == TarEntryType.GlobalExtendedAttributes)
                {
                    throw new FormatException(SR.TarTooManyGlobalExtendedAttributesEntries);
                }

                // Can't have two metadata entries in a row, no matter the archive format
                if (actualEntryHeader._typeFlag is TarEntryType.ExtendedAttributes or TarEntryType.LongLink or TarEntryType.LongPath)
                {
                    throw new FormatException(string.Format(SR.TarUnexpectedMetadataEntry, actualEntryHeader._typeFlag, header._typeFlag));
                }

                // Handle metadata entry types
                switch (header._typeFlag)
                {
                    case TarEntryType.ExtendedAttributes: // pax
                        Debug.Assert(header._extendedAttributes != null);
                        if (GlobalExtendedAttributes != null)
                        {
                            // First, replace some of the entry's standard attributes with the global ones
                            actualEntryHeader.ReplaceNormalAttributesWithGlobalExtended(GlobalExtendedAttributes);
                        }
                        // Then replace all the standard attributes with the extended attributes ones,
                        // overwriting the previous global replacements if needed
                        actualEntryHeader.ReplaceNormalAttributesWithExtended(header._extendedAttributes);
                        break;

                    case TarEntryType.LongLink: // gnu
                        Debug.Assert(header._linkName != null);
                        // Replace with longer, complete path
                        actualEntryHeader._linkName = header._linkName;
                        break;

                    case TarEntryType.LongPath: // gnu
                        Debug.Assert(header._name != null);
                        // Replace with longer, complete path
                        actualEntryHeader._name = header._name;
                        break;
                }

                header = actualEntryHeader;
            }

            // Common fields should always acquire a value
            Debug.Assert(header._name != null);
            Debug.Assert(header._linkName != null);

            // Initialize non-common string fields if necessary
            header._magic ??= string.Empty;
            header._version ??= string.Empty;
            header._gName ??= string.Empty;
            header._uName ??= string.Empty;
            header._prefix ??= string.Empty;

            return true;
        }

        // If the current entry contains a non-null DataStream, that stream gets added to an internal
        // list of streams that need to be disposed when this TarReader instance gets disposed.
        private void PreserveDataStreamForDisposalIfNeeded(TarEntry entry)
        {
            // Only dispose the data stream if it was the original one from the archive
            // The user can substitute it anytime, and the setter disposes the original stream upon substitution
            if (entry._header._dataStream is SubReadStream dataStream)
            {
                _dataStreamsToDispose ??= new List<Stream>();
                _dataStreamsToDispose.Add(dataStream);
            }
        }
    }
}
