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
    /// Reads a tar archive from a stream.
    /// </summary>
    public sealed class TarReader : IDisposable, IAsyncDisposable
    {
        private bool _isDisposed;
        private readonly bool _leaveOpen;
        private TarEntry? _previouslyReadEntry;
        private List<Stream>? _dataStreamsToDispose;
        private bool _reachedEndMarkers;

        internal Stream _archiveStream;

        /// <summary>
        /// Initializes a <see cref="TarReader"/> instance that can read tar entries from the specified stream, and can optionally leave the stream open upon disposal of this instance.
        /// </summary>
        /// <param name="archiveStream">The stream to read from.</param>
        /// <param name="leaveOpen"><see langword="false"/> to dispose the <paramref name="archiveStream"/> when this instance is disposed, as well as all the non-null <see cref="TarEntry.DataStream"/> instances from the entries that were visited by this reader; <see langword="true"/> to leave all the streams open.</param>
        /// <exception cref="ArgumentException"><paramref name="archiveStream"/> does not support reading.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="archiveStream"/> is <see langword="null"/>.</exception>
        public TarReader(Stream archiveStream, bool leaveOpen = false)
        {
            ArgumentNullException.ThrowIfNull(archiveStream);

            if (!archiveStream.CanRead)
            {
                throw new ArgumentException(SR.IO_NotSupported_UnreadableStream, nameof(archiveStream));
            }

            _archiveStream = archiveStream;
            _leaveOpen = leaveOpen;

            _previouslyReadEntry = null;
            _isDisposed = false;
            _reachedEndMarkers = false;
        }

        /// <summary>
        /// Disposes the current <see cref="TarReader"/> instance, closes the archive stream, and disposes the non-null <see cref="TarEntry.DataStream"/> instances of all the entries that were read from the archive if the <c>leaveOpen</c> argument was set to <see langword="false"/> in the constructor.
        /// </summary>
        /// <remarks>The <see cref="TarEntry.DataStream"/> property of any entry can be replaced with a new stream. If the user decides to replace it on a <see cref="TarEntry"/> instance that was obtained using a <see cref="TarReader"/>, the underlying stream gets disposed immediately, freeing the <see cref="TarReader"/> of origin from the responsibility of having to dispose it.</remarks>
        public void Dispose()
        {
            ValueTask vt = DisposeCoreAsync<SyncReadWriteAdapter>();
            Debug.Assert(vt.IsCompleted, "Synchronous Dispose completed asynchronously.");
            vt.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously disposes the current <see cref="TarReader"/> instance, and disposes the non-null <see cref="TarEntry.DataStream"/> instances of all the entries that were read from the archive.
        /// </summary>
        /// <remarks>The <see cref="TarEntry.DataStream"/> property of any entry can be replaced with a new stream. If the user decides to replace it on a <see cref="TarEntry"/> instance that was obtained using a <see cref="TarReader"/>, the underlying stream gets disposed immediately, freeing the <see cref="TarReader"/> of origin from the responsibility of having to dispose it.</remarks>
        public ValueTask DisposeAsync() => DisposeCoreAsync<AsyncReadWriteAdapter>();

        private async ValueTask DisposeCoreAsync<TAdapter>()
            where TAdapter : IReadWriteAdapter
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                if (!_leaveOpen)
                {
                    if (_dataStreamsToDispose?.Count > 0)
                    {
                        foreach (Stream s in _dataStreamsToDispose)
                        {
                            await TAdapter.DisposeAsync(s).ConfigureAwait(false);
                        }
                    }

                    await TAdapter.DisposeAsync(_archiveStream).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Retrieves the next entry from the archive stream.
        /// </summary>
        /// <param name="copyData"><para>Set it to <see langword="true"/> to copy the data of the entry into a new <see cref="MemoryStream"/>. This is helpful when the underlying archive stream is unseekable, and the data needs to be accessed later.</para>
        /// <para>Set it to <see langword="false"/> if the data should not be copied into a new stream. If the underlying stream is unseekable, the user has the responsibility of reading and processing the <see cref="TarEntry.DataStream"/> immediately after calling this method.</para>
        /// <para>The default value is <see langword="false"/>.</para></param>
        /// <returns>A <see cref="TarEntry"/> instance if a valid entry was found, or <see langword="null"/> if the end of the archive has been reached.</returns>
        /// <exception cref="InvalidDataException"><para>The entry's data is malformed.</para>
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

            ValueTask<TarEntry?> vt = GetNextEntryCoreAsync<SyncReadWriteAdapter>(copyData, CancellationToken.None);
            Debug.Assert(vt.IsCompleted, "Synchronous GetNextEntry completed asynchronously.");
            return vt.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously retrieves the next entry from the archive stream.
        /// </summary>
        /// <param name="copyData"><para>Set it to <see langword="true"/> to copy the data of the entry into a new <see cref="MemoryStream"/>. This is helpful when the underlying archive stream is unseekable, and the data needs to be accessed later.</para>
        /// <para>Set it to <see langword="false"/> if the data should not be copied into a new stream. If the underlying stream is unseekable, the user has the responsibility of reading and processing the <see cref="TarEntry.DataStream"/> immediately after calling this method.</para>
        /// <para>The default value is <see langword="false"/>.</para></param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>A value task containing a <see cref="TarEntry"/> instance if a valid entry was found, or <see langword="null"/> if the end of the archive has been reached.</returns>
        /// <exception cref="InvalidDataException"><para>The archive is malformed.</para>
        /// <para>-or-</para>
        /// <para>The archive contains entries in different formats.</para>
        /// <para>-or-</para>
        /// <para>More than one Global Extended Attributes Entry was found in the current <see cref="TarEntryFormat.Pax"/> archive.</para>
        /// <para>-or-</para>
        /// <para>Two or more Extended Attributes entries were found consecutively in the current <see cref="TarEntryFormat.Pax"/> archive.</para></exception>
        /// <exception cref="IOException">An I/O problem occurred.</exception>
        public ValueTask<TarEntry?> GetNextEntryAsync(bool copyData = false, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<TarEntry?>(cancellationToken);
            }

            if (_reachedEndMarkers)
            {
                // Avoid advancing the stream if we already found the end of the archive.
                return ValueTask.FromResult<TarEntry?>(null);
            }

            Debug.Assert(_archiveStream.CanRead);

            if (_archiveStream.CanSeek && _archiveStream.Length == 0)
            {
                // Attempting to get the next entry on an empty tar stream
                return ValueTask.FromResult<TarEntry?>(null);
            }

            return GetNextEntryCoreAsync<AsyncReadWriteAdapter>(copyData, cancellationToken);
        }

        // Moves the underlying archive stream position pointer to the beginning of the next header.
        internal async ValueTask AdvanceDataStreamIfNeededCoreAsync<TAdapter>(CancellationToken cancellationToken)
            where TAdapter : IReadWriteAdapter
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
                // When working with unseekable streams, every time we return an entry, we avoid advancing the pointer beyond the data section
                // This is so the user can read the data if desired. But if the data was not read by the user, we need to advance the pointer
                // here until it's located at the beginning of the next entry header.
                // This should only be done if the previous entry came from a TarReader and it still had its original SubReadStream.

                if (_previouslyReadEntry._header._dataStream is not SubReadStream dataStream)
                {
                    return;
                }

                // SubReadStream is not available in all assemblies that consume the shared
                // IReadWriteAdapter (e.g. Net.Security, Net.Mail), so AdvanceToEnd cannot
                // live on the adapter interface. Use typeof(TAdapter) to dispatch sync/async;
                // the JIT eliminates the dead branch when the generic is specialized.
                if (typeof(TAdapter) == typeof(SyncReadWriteAdapter))
                {
                    dataStream.AdvanceToEnd();
                }
                else
                {
                    await dataStream.AdvanceToEndAsync(cancellationToken).ConfigureAwait(false);
                }

                await TarHelpers.SkipBlockAlignmentPaddingCoreAsync<TAdapter>(_archiveStream, _previouslyReadEntry._header._size, cancellationToken).ConfigureAwait(false);
            }
        }

        // Retrieves the next entry if one is found.
        private async ValueTask<TarEntry?> GetNextEntryCoreAsync<TAdapter>(bool copyData, CancellationToken cancellationToken)
            where TAdapter : IReadWriteAdapter
        {
            await AdvanceDataStreamIfNeededCoreAsync<TAdapter>(cancellationToken).ConfigureAwait(false);

            TarHeader? header = await TryGetNextEntryHeaderCoreAsync<TAdapter>(copyData, cancellationToken).ConfigureAwait(false);
            if (header != null)
            {
                TarEntry entry = header._format switch
                {
                    TarEntryFormat.Pax => header._typeFlag is TarEntryType.GlobalExtendedAttributes ?
                                          new PaxGlobalExtendedAttributesTarEntry(header, this) : new PaxTarEntry(header, this),
                    TarEntryFormat.Gnu => new GnuTarEntry(header, this),
                    TarEntryFormat.Ustar => new UstarTarEntry(header, this),
                    TarEntryFormat.V7 or TarEntryFormat.Unknown or _ => new V7TarEntry(header, this),
                };

                if (_archiveStream.CanSeek && _archiveStream.Length == _archiveStream.Position)
                {
                    _reachedEndMarkers = true;
                }

                _previouslyReadEntry = entry;
                PreserveDataStreamForDisposalIfNeeded(entry);
                return entry;
            }

            _reachedEndMarkers = true;
            return null;
        }

        // Attempts to read the next tar archive entry header.
        // Returns true if an entry header was collected successfully, false otherwise.
        // An entry header represents any typeflag that is contains metadata.
        // Metadata typeflags: ExtendedAttributes, GlobalExtendedAttributes, LongLink, LongPath.
        // Metadata typeflag entries get handled internally by this method until a valid header entry can be returned.
        private async ValueTask<TarHeader?> TryGetNextEntryHeaderCoreAsync<TAdapter>(bool copyData, CancellationToken cancellationToken)
            where TAdapter : IReadWriteAdapter
        {
            Debug.Assert(!_reachedEndMarkers);

            TarHeader? header = await TarHeader.TryGetNextHeaderCoreAsync<TAdapter>(_archiveStream, copyData, TarEntryFormat.Unknown, processDataBlock: true, cancellationToken).ConfigureAwait(false);
            if (header == null)
            {
                return null;
            }

            // If a metadata typeflag entry is retrieved, handle it here, then read the next entry

            // PAX metadata
            if (header._typeFlag is TarEntryType.ExtendedAttributes)
            {
                TarHeader? mainHeader = await TryProcessExtendedAttributesHeaderCoreAsync<TAdapter>(header, copyData, cancellationToken).ConfigureAwait(false);
                if (mainHeader == null)
                {
                    return null;
                }
                header = mainHeader;
            }
            // GNU metadata
            else if (header._typeFlag is TarEntryType.LongLink or TarEntryType.LongPath)
            {
                TarHeader? mainHeader = await TryProcessGnuMetadataHeaderCoreAsync<TAdapter>(header, copyData, cancellationToken).ConfigureAwait(false);
                if (mainHeader == null)
                {
                    return null;
                }
                header = mainHeader;
            }

            return header;
        }

        // Tries to read the contents of the PAX metadata entry as extended attributes, tries to also read the actual entry that follows,
        // and returns the actual entry with the processed extended attributes saved in the _extendedAttributes dictionary.
        private async ValueTask<TarHeader?> TryProcessExtendedAttributesHeaderCoreAsync<TAdapter>(TarHeader extendedAttributesHeader, bool copyData, CancellationToken cancellationToken)
            where TAdapter : IReadWriteAdapter
        {
            // Don't process the data block of the actual entry just yet, because there's a slim chance
            // that the extended attributes contain a size that we need to override in the header
            TarHeader? actualHeader = await TarHeader.TryGetNextHeaderCoreAsync<TAdapter>(_archiveStream, copyData, TarEntryFormat.Pax, processDataBlock: false, cancellationToken).ConfigureAwait(false);
            if (actualHeader == null)
            {
                return null;
            }

            // We're currently processing an extended attributes header, so we can never have two extended entries in a row
            if (actualHeader._typeFlag is TarEntryType.GlobalExtendedAttributes or
                TarEntryType.ExtendedAttributes or
                TarEntryType.LongLink or
                TarEntryType.LongPath)
            {
                throw new InvalidDataException(SR.Format(SR.TarUnexpectedMetadataEntry, actualHeader._typeFlag, TarEntryType.ExtendedAttributes));
            }

            // Replace all the attributes representing standard fields with the extended ones, if any
            actualHeader.ReplaceNormalAttributesWithExtended(extendedAttributesHeader.ExtendedAttributes);

            // We retrieved the extended attributes, now we can read the data, and always with the right size
            await actualHeader.ProcessDataBlockCoreAsync<TAdapter>(_archiveStream, copyData, cancellationToken).ConfigureAwait(false);

            return actualHeader;
        }

        // Tries to read the contents of the GNU metadata entry, then tries to read the next entry, which could either be another GNU metadata entry
        // or the actual entry. Processes them all and returns the actual entry updating its path and/or linkpath fields as needed.
        private async ValueTask<TarHeader?> TryProcessGnuMetadataHeaderCoreAsync<TAdapter>(TarHeader header, bool copyData, CancellationToken cancellationToken)
            where TAdapter : IReadWriteAdapter
        {
            // Get the second entry, which is the actual entry
            TarHeader? secondHeader = await TarHeader.TryGetNextHeaderCoreAsync<TAdapter>(_archiveStream, copyData, TarEntryFormat.Gnu, processDataBlock: true, cancellationToken).ConfigureAwait(false);
            if (secondHeader == null)
            {
                return null;
            }

            // Can't have two identical metadata entries in a row
            if (secondHeader._typeFlag == header._typeFlag)
            {
                throw new InvalidDataException(SR.Format(SR.TarUnexpectedMetadataEntry, secondHeader._typeFlag, header._typeFlag));
            }

            TarHeader finalHeader;

            // It's possible to have the two different metadata entries in a row
            if ((header._typeFlag is TarEntryType.LongLink && secondHeader._typeFlag is TarEntryType.LongPath) ||
                (header._typeFlag is TarEntryType.LongPath && secondHeader._typeFlag is TarEntryType.LongLink))
            {
                // Get the third entry, which is the actual entry
                TarHeader? thirdHeader = await TarHeader.TryGetNextHeaderCoreAsync<TAdapter>(_archiveStream, copyData, TarEntryFormat.Gnu, processDataBlock: true, cancellationToken).ConfigureAwait(false);
                if (thirdHeader == null)
                {
                    return null;
                }

                // Can't have three GNU metadata entries in a row
                if (thirdHeader._typeFlag is TarEntryType.LongLink or TarEntryType.LongPath)
                {
                    throw new InvalidDataException(SR.Format(SR.TarUnexpectedMetadataEntry, thirdHeader._typeFlag, secondHeader._typeFlag));
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

            return finalHeader;
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
