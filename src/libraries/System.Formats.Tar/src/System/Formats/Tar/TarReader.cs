// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace System.Formats.Tar
{
    /// <summary>
    /// Reads a tar archive from a stream.
    /// </summary>
    public sealed class TarReader : IDisposable
    {
        private bool _isDisposed;
        private readonly bool _leaveOpen;
        private TarEntry? _previouslyReadEntry;
        private List<Stream>? _dataStreamsToDispose;
        private bool _readFirstEntry;
        private bool _reachedEndMarkers;

        internal Stream _archiveStream;

        /// <summary>
        /// Initializes a <see cref="TarReader"/> instance that can read tar entries from the specified stream, and can optionally leave the stream open upon disposal of this instance.
        /// </summary>
        /// <param name="archiveStream">The stream to read from.</param>
        /// <param name="leaveOpen"><see langword="false"/> to dispose the <paramref name="archiveStream"/> when this instance is disposed; <see langword="true"/> to leave the stream open.</param>
        /// <exception cref="IOException"><paramref name="archiveStream"/> is unreadable.</exception>
        public TarReader(Stream archiveStream, bool leaveOpen = false)
        {
            ArgumentNullException.ThrowIfNull(archiveStream);

            if (!archiveStream.CanRead)
            {
                throw new IOException(SR.IO_NotSupported_UnreadableStream);
            }

            _archiveStream = archiveStream;
            _leaveOpen = leaveOpen;

            _previouslyReadEntry = null;
            GlobalExtendedAttributes = null;
            Format = TarEntryFormat.Unknown;
            _isDisposed = false;
            _readFirstEntry = false;
            _reachedEndMarkers = false;
        }

        /// <summary>
        /// The format of the archive. It is initially <see cref="TarEntryFormat.Unknown"/>. The archive format is detected after the first call to <see cref="GetNextEntry(bool)"/>.
        /// </summary>
        public TarEntryFormat Format { get; private set; }

        /// <summary>
        /// <para>If the archive format is <see cref="TarEntryFormat.Pax"/>, returns a read-only dictionary containing the string key-value pairs of the Global Extended Attributes in the first entry of the archive.</para>
        /// <para>If there is no Global Extended Attributes entry at the beginning of the archive, this returns an empty read-only dictionary.</para>
        /// <para>If the first entry has not been read by calling <see cref="GetNextEntry(bool)"/>, this returns <see langword="null"/>.</para>
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

        // /// <summary>
        // /// Asynchronously disposes the current <see cref="TarReader"/> instance, and disposes the streams of all the entries that were read from the archive.
        // /// </summary>
        // /// <remarks>The <see cref="TarEntry.DataStream"/> property of any entry can be replaced with a new stream. If the user decides to replace it on a <see cref="TarEntry"/> instance that was obtained using a <see cref="TarReader"/>, the underlying stream gets disposed immediately, freeing the <see cref="TarReader"/> of origin from the responsibility of having to dispose it.</remarks>
        // public ValueTask DisposeAsync()
        // {
        //     throw new NotImplementedException();
        // }

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
        /// <para>More than one Global Extended Attributes Entry was found in the current <see cref="TarEntryFormat.Pax"/> archive.</para>
        /// <para>-or-</para>
        /// <para>Two or more Extended Attributes entries were found consecutively in the current <see cref="TarEntryFormat.Pax"/> archive.</para></exception>
        /// <exception cref="IOException">An I/O problem occurred.</exception>
        public TarEntry? GetNextEntry(bool copyData = false)
        {
            if (_reachedEndMarkers)
            {
                // Avoid advancing the stream if we already found the end of the archive.
                return null;
            }

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
                    Debug.Assert(Format == TarEntryFormat.Unknown);
                    Format = header._format;
                    _readFirstEntry = true;
                }
                else if (header._format != Format)
                {
                    throw new FormatException(string.Format(SR.TarEntriesInDifferentFormats, header._format, Format));
                }

                TarEntry entry = Format switch
                {
                    TarEntryFormat.Pax => new PaxTarEntry(header, this),
                    TarEntryFormat.Gnu => new GnuTarEntry(header, this),
                    TarEntryFormat.Ustar => new UstarTarEntry(header, this),
                    TarEntryFormat.V7 or TarEntryFormat.Unknown or _ => new V7TarEntry(header, this),
                };

                _previouslyReadEntry = entry;
                PreserveDataStreamForDisposalIfNeeded(entry);
                return entry;
            }

            _reachedEndMarkers = true;
            return null;
        }

        // /// <summary>
        // /// Asynchronously retrieves the next entry from the archive stream.
        // /// </summary>
        // /// <param name="copyData"><para>Set it to <see langword="true"/> to copy the data of the entry into a new <see cref="MemoryStream"/>. This is helpful when the underlying archive stream is unseekable, and the data needs to be accessed later.</para>
        // /// <para>Set it to <see langword="false"/> if the data should not be copied into a new stream. If the underlying stream is unseekable, the user has the responsibility of reading and processing the <see cref="TarEntry.DataStream"/> immediately after calling this method.</para>
        // /// <para>The default value is <see langword="false"/>.</para></param>
        // /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        // /// <returns>A value task containing a <see cref="TarEntry"/> instance if a valid entry was found, or <see langword="null"/> if the end of the archive has been reached.</returns>
        // public ValueTask<TarEntry?> GetNextEntryAsync(bool copyData = false, CancellationToken cancellationToken = default)
        // {
        //     throw new NotImplementedException();
        // }

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

                if (!dataStream.HasReachedEnd)
                {
                    // If the user did not advance the position, we need to make sure the position
                    // pointer is located at the beginning of the next header.
                    if (dataStream.Position < (_previouslyReadEntry._header._size - 1))
                    {
                        long bytesToSkip = _previouslyReadEntry._header._size - dataStream.Position;
                        TarHelpers.AdvanceStream(_archiveStream, bytesToSkip);
                        TarHelpers.SkipBlockAlignmentPadding(_archiveStream, _previouslyReadEntry._header._size);
                        dataStream.HasReachedEnd = true; // Now the pointer is beyond the limit, so any read attempts should throw
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
            Debug.Assert(!_reachedEndMarkers);

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
                header._format = TarEntryFormat.Pax;
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
                    Format = TarEntryFormat.Pax;
                    return false;
                }
                if (header._typeFlag == TarEntryType.GlobalExtendedAttributes)
                {
                    throw new FormatException(SR.TarTooManyGlobalExtendedAttributesEntries);
                }
            }

            // If a metadata typeflag entry is retrieved, handle it here, then read the next entry

            // PAX metadata
            if (header._typeFlag is TarEntryType.ExtendedAttributes)
            {
                if (!TryProcessExtendedAttributesHeader(header, copyData, out TarHeader mainHeader))
                {
                    return false;
                }
                header = mainHeader;
            }
            // GNU metadata
            else if (header._typeFlag is TarEntryType.LongLink or TarEntryType.LongPath)
            {
                if (!TryProcessGnuMetadataHeader(header, copyData, out TarHeader mainHeader))
                {
                    return false;
                }
                header = mainHeader;
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

        private bool TryProcessExtendedAttributesHeader(TarHeader firstHeader, bool copyData, out TarHeader secondHeader)
        {
            secondHeader = default;
            secondHeader._format = TarEntryFormat.Pax;

            // Now get the actual entry
            if (!secondHeader.TryGetNextHeader(_archiveStream, copyData))
            {
                return false;
            }

            // Should never read a GEA entry at this point
            if (secondHeader._typeFlag == TarEntryType.GlobalExtendedAttributes)
            {
                throw new FormatException(SR.TarTooManyGlobalExtendedAttributesEntries);
            }

            // Can't have two metadata entries in a row, no matter the archive format
            if (secondHeader._typeFlag is TarEntryType.ExtendedAttributes)
            {
                throw new FormatException(string.Format(SR.TarUnexpectedMetadataEntry, TarEntryType.ExtendedAttributes, TarEntryType.ExtendedAttributes));
            }

            Debug.Assert(firstHeader._extendedAttributes != null);
            if (GlobalExtendedAttributes != null)
            {
                // First, replace some of the entry's standard attributes with the global ones
                secondHeader.ReplaceNormalAttributesWithGlobalExtended(GlobalExtendedAttributes);
            }
            // Then replace all the standard attributes with the extended attributes ones,
            // overwriting the previous global replacements if needed
            secondHeader.ReplaceNormalAttributesWithExtended(firstHeader._extendedAttributes);

            return true;
        }

        private bool TryProcessGnuMetadataHeader(TarHeader header, bool copyData, out TarHeader finalHeader)
        {
            finalHeader = default;

            TarHeader secondHeader = default;
            secondHeader._format = TarEntryFormat.Gnu;

            // Get the second entry, which is the actual entry
            if (!secondHeader.TryGetNextHeader(_archiveStream, copyData))
            {
                return false;
            }

            // Can't have two identical metadata entries in a row
            if (secondHeader._typeFlag == header._typeFlag)
            {
                throw new FormatException(string.Format(SR.TarUnexpectedMetadataEntry, secondHeader._typeFlag, header._typeFlag));
            }

            // It's possible to have the two different metadata entries in a row
            if ((header._typeFlag is TarEntryType.LongLink && secondHeader._typeFlag is TarEntryType.LongPath) ||
                (header._typeFlag is TarEntryType.LongPath && secondHeader._typeFlag is TarEntryType.LongLink))
            {
                TarHeader thirdHeader = default;
                thirdHeader._format = TarEntryFormat.Gnu;

                // Get the third entry, which is the actual entry
                if (!thirdHeader.TryGetNextHeader(_archiveStream, copyData))
                {
                    return false;
                }

                // Can't have three GNU metadata entries in a row
                if (thirdHeader._typeFlag is TarEntryType.LongLink or TarEntryType.LongPath)
                {
                    throw new FormatException(string.Format(SR.TarUnexpectedMetadataEntry, thirdHeader._typeFlag, secondHeader._typeFlag));
                }

                if (header._typeFlag is TarEntryType.LongLink)
                {
                    Debug.Assert(header._linkName != null);
                    Debug.Assert(secondHeader._name != null);

                    thirdHeader._linkName = header._linkName;
                    thirdHeader._name = secondHeader._name;
                }
                else if (header._typeFlag is TarEntryType.LongPath)
                {
                    Debug.Assert(header._name != null);
                    Debug.Assert(secondHeader._linkName != null);
                    thirdHeader._name = header._name;
                    thirdHeader._linkName = secondHeader._linkName;
                }

                finalHeader = thirdHeader;
            }
            // Only one metadata entry was found
            else
            {
                if (header._typeFlag is TarEntryType.LongLink)
                {
                    Debug.Assert(header._linkName != null);
                    secondHeader._linkName = header._linkName;
                }
                else if (header._typeFlag is TarEntryType.LongPath)
                {
                    Debug.Assert(header._name != null);
                    secondHeader._name = header._name;
                }

                finalHeader = secondHeader;
            }

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
