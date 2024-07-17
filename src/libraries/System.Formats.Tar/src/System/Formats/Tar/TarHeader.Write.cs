// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Formats.Tar
{
    // Writes header attributes of a tar archive entry.
    internal sealed partial class TarHeader
    {
        private const long Octal12ByteFieldMaxValue = (1L << (3 * 11)) - 1; // Max value of 11 octal digits.
        private const int Octal8ByteFieldMaxValue = (1 << (3 * 7)) - 1;     // Max value of 7 octal digits.

        private static ReadOnlySpan<byte> UstarMagicBytes => "ustar\0"u8;
        private static ReadOnlySpan<byte> UstarVersionBytes => "00"u8;

        private static ReadOnlySpan<byte> GnuMagicBytes => "ustar "u8;
        private static ReadOnlySpan<byte> GnuVersionBytes => " \0"u8;

        // Predefined text for the Name field of a GNU long metadata entry. Applies for both LongPath ('L') and LongLink ('K').
        private const string GnuLongMetadataName = "././@LongLink";
        private const string ArgNameEntry = "entry";

        // Writes the entry in the order required to be able to obtain the seekable data stream size.
        private void WriteWithSeekableDataStream(TarEntryFormat format, Stream archiveStream, Span<byte> buffer)
        {
            Debug.Assert(format is > TarEntryFormat.Unknown and <= TarEntryFormat.Gnu);
            Debug.Assert(_dataStream == null || _dataStream.CanSeek);

            _size = GetTotalDataBytesToWrite();
            WriteFieldsToBuffer(format, buffer);
            archiveStream.Write(buffer);

            if (_dataStream != null)
            {
                WriteData(archiveStream, _dataStream);
            }
        }

        // Asynchronously writes the entry in the order required to be able to obtain the seekable data stream size.
        private async Task WriteWithSeekableDataStreamAsync(TarEntryFormat format, Stream archiveStream, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            Debug.Assert(format is > TarEntryFormat.Unknown and <= TarEntryFormat.Gnu);
            Debug.Assert(_dataStream == null || _dataStream.CanSeek);

            _size = GetTotalDataBytesToWrite();
            WriteFieldsToBuffer(format, buffer.Span);
            await archiveStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);

            if (_dataStream != null)
            {
                await WriteDataAsync(archiveStream, _dataStream, cancellationToken).ConfigureAwait(false);
            }
        }

        // Writes into the specified destination stream the entry in the order required to be able to obtain the unseekable data stream size.
        private void WriteWithUnseekableDataStream(TarEntryFormat format, Stream destinationStream, Span<byte> buffer, bool shouldAdvanceToEnd)
        {
            // When the data stream is unseekable, the order in which we write the entry data changes
            Debug.Assert(destinationStream.CanSeek);
            Debug.Assert(_dataStream != null);
            Debug.Assert(!_dataStream.CanSeek);

            // Store the start of the current entry's header, it'll be used later
            long headerStartPosition = destinationStream.Position;

            ushort dataLocation = format switch
            {
                TarEntryFormat.V7 => FieldLocations.V7Data,
                TarEntryFormat.Ustar or TarEntryFormat.Pax => FieldLocations.PosixData,
                TarEntryFormat.Gnu => FieldLocations.GnuData,
                _ => throw new ArgumentOutOfRangeException(nameof(format))
            };

            // We know the exact location where the data starts depending on the format
            long dataStartPosition = headerStartPosition + dataLocation;

            // Before writing, update the offset field now that the entry belongs to an archive
            _dataOffset = dataStartPosition + 1;

            // Move to the data start location and write the data
            destinationStream.Seek(dataLocation, SeekOrigin.Current);
            _dataStream.CopyTo(destinationStream); // The data gets copied from the current position

            // Get the new archive stream position, and the difference is the size of the data stream
            long dataEndPosition = destinationStream.Position;
            _size = dataEndPosition - dataStartPosition;

            // Write the padding now so that we can go back to writing the entry's header metadata
            WriteEmptyPadding(destinationStream);

            // Store the end of the current header, we will write the next one after this position
            long endOfHeaderPosition = destinationStream.Position;

            // Go back to the start of the entry header to write the rest of the fields
            destinationStream.Position = headerStartPosition;

            WriteFieldsToBuffer(format, buffer);
            destinationStream.Write(buffer);

            if (shouldAdvanceToEnd)
            {
                // Finally, move to the end of the header to continue with the next entry
                destinationStream.Position = endOfHeaderPosition;
            }
        }

        // Asynchronously writes into the destination stream the entry in the order required to be able to obtain the unseekable data stream size.
        private async Task WriteWithUnseekableDataStreamAsync(TarEntryFormat format, Stream destinationStream, Memory<byte> buffer, bool shouldAdvanceToEnd, CancellationToken cancellationToken)
        {
            // When the data stream is unseekable, the order in which we write the entry data changes
            Debug.Assert(destinationStream.CanSeek);
            Debug.Assert(_dataStream != null);
            Debug.Assert(!_dataStream.CanSeek);

            // Store the start of the current entry's header, it'll be used later
            long headerStartPosition = destinationStream.Position;

            ushort dataLocation = format switch
            {
                TarEntryFormat.V7 => FieldLocations.V7Data,
                TarEntryFormat.Ustar or TarEntryFormat.Pax => FieldLocations.PosixData,
                TarEntryFormat.Gnu => FieldLocations.GnuData,
                _ => throw new ArgumentOutOfRangeException(nameof(format))
            };

            // We know the exact location where the data starts depending on the format
            long dataStartPosition = headerStartPosition + dataLocation;

            // Before writing, update the offset field now that the entry belongs to an archive
            _dataOffset = dataStartPosition + 1;

            // Move to the data start location and write the data
            destinationStream.Seek(dataLocation, SeekOrigin.Current);
            await _dataStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(false); // The data gets copied from the current position

            // Get the new archive stream position, and the difference is the size of the data stream
            long dataEndPosition = destinationStream.Position;
            _size = dataEndPosition - dataStartPosition;

            // Write the padding now so that we can go back to writing the entry's header metadata
            await WriteEmptyPaddingAsync(destinationStream, cancellationToken).ConfigureAwait(false);

            // Store the end of the current header, we will write the next one after this position
            long endOfHeaderPosition = destinationStream.Position;

            // Go back to the start of the entry header to write the rest of the fields
            destinationStream.Position = headerStartPosition;

            WriteFieldsToBuffer(format, buffer.Span);
            await destinationStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);

            if (shouldAdvanceToEnd)
            {
                // Finally, move to the end of the header to continue with the next entry
                destinationStream.Position = endOfHeaderPosition;
            }
        }

        // Writes the V7 header fields to the specified buffer, calculates and writes the checksum, then returns the final data length.
        private void WriteV7FieldsToBuffer(Span<byte> buffer)
        {
            TarEntryType actualEntryType = TarHelpers.GetCorrectTypeFlagForFormat(TarEntryFormat.V7, _typeFlag);

            int tmpChecksum = WriteName(buffer);
            tmpChecksum += WriteCommonFields(buffer, actualEntryType);
            _checksum = WriteChecksum(tmpChecksum, buffer);
        }

        // Writes the Ustar header fields to the specified buffer, calculates and writes the checksum, then returns the final data length.
        private void WriteUstarFieldsToBuffer(Span<byte> buffer)
        {
            TarEntryType actualEntryType = TarHelpers.GetCorrectTypeFlagForFormat(TarEntryFormat.Ustar, _typeFlag);

            int tmpChecksum = WriteUstarName(buffer);
            tmpChecksum += WriteCommonFields(buffer, actualEntryType);
            tmpChecksum += WritePosixMagicAndVersion(buffer);
            tmpChecksum += WritePosixAndGnuSharedFields(buffer);

            _checksum = WriteChecksum(tmpChecksum, buffer);
        }

        // Writes the current header as a PAX Global Extended Attributes entry into the archive stream.
        internal void WriteAsPaxGlobalExtendedAttributes(Stream archiveStream, Span<byte> buffer, int globalExtendedAttributesEntryNumber)
        {
            VerifyGlobalExtendedAttributesDataIsValid(globalExtendedAttributesEntryNumber);
            WriteAsPaxExtendedAttributes(archiveStream, buffer, ExtendedAttributes, isGea: true, globalExtendedAttributesEntryNumber);
        }

        // Writes the current header as a PAX Global Extended Attributes entry into the archive stream and returns the value of the final checksum.
        internal Task WriteAsPaxGlobalExtendedAttributesAsync(Stream archiveStream, Memory<byte> buffer, int globalExtendedAttributesEntryNumber, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
            }

            VerifyGlobalExtendedAttributesDataIsValid(globalExtendedAttributesEntryNumber);
            return WriteAsPaxExtendedAttributesAsync(archiveStream, buffer, ExtendedAttributes, isGea: true, globalExtendedAttributesEntryNumber, cancellationToken);
        }

        // Verifies the data is valid for writing a Global Extended Attributes entry.
        private void VerifyGlobalExtendedAttributesDataIsValid(int globalExtendedAttributesEntryNumber)
        {
            Debug.Assert(_typeFlag is TarEntryType.GlobalExtendedAttributes);
            Debug.Assert(globalExtendedAttributesEntryNumber >= 0);
        }

        internal void WriteAsV7(Stream archiveStream, Span<byte> buffer)
        {
            Debug.Assert(archiveStream.CanSeek || _dataStream == null || _dataStream.CanSeek);

            if (archiveStream.CanSeek && _dataStream is { CanSeek: false })
            {
                WriteWithUnseekableDataStream(TarEntryFormat.V7, archiveStream, buffer, shouldAdvanceToEnd: true);
            }
            else // Seek status of archive does not matter
            {
                WriteWithSeekableDataStream(TarEntryFormat.V7, archiveStream, buffer);
            }
        }

        internal Task WriteAsV7Async(Stream archiveStream, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            Debug.Assert(archiveStream.CanSeek || _dataStream == null || _dataStream.CanSeek);

            if (archiveStream.CanSeek && _dataStream is { CanSeek: false })
            {
                return WriteWithUnseekableDataStreamAsync(TarEntryFormat.V7, archiveStream, buffer, shouldAdvanceToEnd: true, cancellationToken);
            }

            // Else: Seek status of archive does not matter
            return WriteWithSeekableDataStreamAsync(TarEntryFormat.V7, archiveStream, buffer, cancellationToken);
        }

        internal void WriteAsUstar(Stream archiveStream, Span<byte> buffer)
        {
            Debug.Assert(archiveStream.CanSeek || _dataStream == null || _dataStream.CanSeek);

            if (archiveStream.CanSeek && _dataStream is { CanSeek: false })
            {
                WriteWithUnseekableDataStream(TarEntryFormat.Ustar, archiveStream, buffer, shouldAdvanceToEnd: true);
            }
            else // Seek status of archive does not matter
            {
                WriteWithSeekableDataStream(TarEntryFormat.Ustar, archiveStream, buffer);
            }
        }

        internal Task WriteAsUstarAsync(Stream archiveStream, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            Debug.Assert(archiveStream.CanSeek || _dataStream == null || _dataStream.CanSeek);

            if (archiveStream.CanSeek && _dataStream is { CanSeek: false })
            {
                return WriteWithUnseekableDataStreamAsync(TarEntryFormat.Ustar, archiveStream, buffer, shouldAdvanceToEnd: true, cancellationToken);
            }

            // Else: Seek status of archive does not matter
            return WriteWithSeekableDataStreamAsync(TarEntryFormat.Ustar, archiveStream, buffer, cancellationToken);
        }

        // Writes the current header as a PAX entry into the archive stream.
        // Makes sure to add the preceding extended attributes entry before the actual entry.
        internal void WriteAsPax(Stream archiveStream, Span<byte> buffer)
        {
            Debug.Assert(archiveStream.CanSeek || _dataStream == null || _dataStream.CanSeek);
            Debug.Assert(_typeFlag is not TarEntryType.GlobalExtendedAttributes);

            // First, we create the preceding extended attributes header
            TarHeader extendedAttributesHeader = new(TarEntryFormat.Pax);

            if (archiveStream.CanSeek && _dataStream is { CanSeek: false })
            {
                // Write the full entry header into a temporary stream, which will also collect the data length in the _size field
                using MemoryStream tempStream = new();
                // Don't advance the tempStream, instead, we will rewind it to the beginning for copying later
                WriteWithUnseekableDataStream(TarEntryFormat.Pax, tempStream, buffer, shouldAdvanceToEnd: false);
                tempStream.Position = 0;
                buffer.Clear();

                // If the data length is larger than it fits in the standard size field, it will get stored as an extended attribute
                CollectExtendedAttributesFromStandardFieldsIfNeeded();

                // Write the extended attributes entry into the archive first
                extendedAttributesHeader.WriteAsPaxExtendedAttributes(archiveStream, buffer, ExtendedAttributes, isGea: false, globalExtendedAttributesEntryNumber: -1);
                buffer.Clear();

                // And then write the stored entry into the archive
                tempStream.CopyTo(archiveStream);
            }
            else // Seek status of archive does not matter
            {
                _size = GetTotalDataBytesToWrite();
                // Fill the current header's dict
                CollectExtendedAttributesFromStandardFieldsIfNeeded();
                // And pass the attributes to the preceding extended attributes header for writing
                extendedAttributesHeader.WriteAsPaxExtendedAttributes(archiveStream, buffer, ExtendedAttributes, isGea: false, globalExtendedAttributesEntryNumber: -1);
                buffer.Clear(); // Reset it to reuse it

                // Second, we write this header as a normal one
                WriteWithSeekableDataStream(TarEntryFormat.Pax, archiveStream, buffer);
            }
        }

        // Asynchronously writes the current header as a PAX entry into the archive stream.
        // Makes sure to add the preceding exteded attributes entry before the actual entry.
        internal async Task WriteAsPaxAsync(Stream archiveStream, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            Debug.Assert(archiveStream.CanSeek || _dataStream == null || _dataStream.CanSeek);
            Debug.Assert(_typeFlag is not TarEntryType.GlobalExtendedAttributes);
            cancellationToken.ThrowIfCancellationRequested();

            // First, we create the preceding extended attributes header
            TarHeader extendedAttributesHeader = new(TarEntryFormat.Pax);

            if (archiveStream.CanSeek && _dataStream is { CanSeek: false })
            {
                // Write the full entry header into a temporary stream, which will also collect the data length in the _size field
                using MemoryStream tempStream = new();
                // Don't advance the tempStream, instead, we will rewind it to the beginning for copying later
                await WriteWithUnseekableDataStreamAsync(TarEntryFormat.Pax, tempStream, buffer, shouldAdvanceToEnd: false, cancellationToken).ConfigureAwait(false);
                tempStream.Position = 0;
                buffer.Span.Clear();

                // If the data length is larger than it fits in the standard size field, it will get stored as an extended attribute
                CollectExtendedAttributesFromStandardFieldsIfNeeded();

                // Write the extended attributes entry into the archive first
                await extendedAttributesHeader.WriteAsPaxExtendedAttributesAsync(archiveStream, buffer, ExtendedAttributes, isGea: false, globalExtendedAttributesEntryNumber: -1, cancellationToken).ConfigureAwait(false);
                buffer.Span.Clear();

                // And then write the stored entry into the archive
                await tempStream.CopyToAsync(archiveStream, cancellationToken).ConfigureAwait(false);
            }
            else // Seek status of archive does not matter
            {
                _size = GetTotalDataBytesToWrite();
                // Fill the current header's dict
                CollectExtendedAttributesFromStandardFieldsIfNeeded();
                // And pass the attributes to the preceding extended attributes header for writing
                await extendedAttributesHeader.WriteAsPaxExtendedAttributesAsync(archiveStream, buffer, ExtendedAttributes, isGea: false, globalExtendedAttributesEntryNumber: -1, cancellationToken).ConfigureAwait(false);
                buffer.Span.Clear(); // Reset it to reuse it

                // Second, we write this header as a normal one
                await WriteWithSeekableDataStreamAsync(TarEntryFormat.Pax, archiveStream, buffer, cancellationToken).ConfigureAwait(false);
            }
        }

        // Writes the current header as a Gnu entry into the archive stream.
        // Makes sure to add the preceding LongLink and/or LongPath entries if necessary, before the actual entry.
        internal void WriteAsGnu(Stream archiveStream, Span<byte> buffer)
        {
            Debug.Assert(archiveStream.CanSeek || _dataStream == null || _dataStream.CanSeek);

            // First, we determine if we need a preceding LongLink, and write it if needed
            if (_linkName != null && Encoding.UTF8.GetByteCount(_linkName) > FieldLengths.LinkName)
            {
                TarHeader longLinkHeader = GetGnuLongMetadataHeader(TarEntryType.LongLink, _linkName);
                Debug.Assert(longLinkHeader._dataStream != null && longLinkHeader._dataStream.CanSeek); // We generate the long metadata data stream, should always be seekable
                longLinkHeader.WriteWithSeekableDataStream(TarEntryFormat.Gnu, archiveStream, buffer);
                buffer.Clear(); // Reset it to reuse it
            }

            // Second, we determine if we need a preceding LongPath, and write it if needed
            if (Encoding.UTF8.GetByteCount(_name) > FieldLengths.Name)
            {
                TarHeader longPathHeader = GetGnuLongMetadataHeader(TarEntryType.LongPath, _name);
                Debug.Assert(longPathHeader._dataStream != null && longPathHeader._dataStream.CanSeek); // We generate the long metadata data stream, should always be seekable
                longPathHeader.WriteWithSeekableDataStream(TarEntryFormat.Gnu, archiveStream, buffer);
                buffer.Clear(); // Reset it to reuse it
            }

            // Third, we write this header as a normal one
            if (archiveStream.CanSeek && _dataStream is { CanSeek: false })
            {
                WriteWithUnseekableDataStream(TarEntryFormat.Gnu, archiveStream, buffer, shouldAdvanceToEnd: true);
            }
            else // Seek status of archive does not matter
            {
                WriteWithSeekableDataStream(TarEntryFormat.Gnu, archiveStream, buffer);
            }
        }

        // Writes the current header as a Gnu entry into the archive stream.
        // Makes sure to add the preceding LongLink and/or LongPath entries if necessary, before the actual entry.
        internal async Task WriteAsGnuAsync(Stream archiveStream, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            Debug.Assert(archiveStream.CanSeek || _dataStream == null || _dataStream.CanSeek);
            cancellationToken.ThrowIfCancellationRequested();

            // First, we determine if we need a preceding LongLink, and write it if needed
            if (_linkName != null && Encoding.UTF8.GetByteCount(_linkName) > FieldLengths.LinkName)
            {
                TarHeader longLinkHeader = GetGnuLongMetadataHeader(TarEntryType.LongLink, _linkName);
                Debug.Assert(longLinkHeader._dataStream != null && longLinkHeader._dataStream.CanSeek); // We generate the long metadata data stream, should always be seekable
                await longLinkHeader.WriteWithSeekableDataStreamAsync(TarEntryFormat.Gnu, archiveStream, buffer, cancellationToken).ConfigureAwait(false);
                buffer.Span.Clear(); // Reset it to reuse it
            }

            // Second, we determine if we need a preceding LongPath, and write it if needed
            if (Encoding.UTF8.GetByteCount(_name) > FieldLengths.Name)
            {
                TarHeader longPathHeader = GetGnuLongMetadataHeader(TarEntryType.LongPath, _name);
                Debug.Assert(longPathHeader._dataStream != null && longPathHeader._dataStream.CanSeek); // We generate the long metadata data stream, should always be seekable
                await longPathHeader.WriteWithSeekableDataStreamAsync(TarEntryFormat.Gnu, archiveStream, buffer, cancellationToken).ConfigureAwait(false);
                buffer.Span.Clear(); // Reset it to reuse it
            }

            // Third, we write this header as a normal one
            if (archiveStream.CanSeek && _dataStream is { CanSeek: false })
            {
                await WriteWithUnseekableDataStreamAsync(TarEntryFormat.Gnu, archiveStream, buffer, shouldAdvanceToEnd: true, cancellationToken).ConfigureAwait(false);
            }
            else // Seek status of archive does not matter
            {
                await WriteWithSeekableDataStreamAsync(TarEntryFormat.Gnu, archiveStream, buffer, cancellationToken).ConfigureAwait(false);
            }
        }

        // Creates and returns a GNU long metadata header, with the specified long text written into its data stream (seekable).
        private static TarHeader GetGnuLongMetadataHeader(TarEntryType entryType, string longText)
        {
            Debug.Assert(entryType is TarEntryType.LongPath or TarEntryType.LongLink);

            return new(TarEntryFormat.Gnu)
            {
                _name = GnuLongMetadataName, // Same name for both longpath or longlink
                _mode = TarHelpers.GetDefaultMode(entryType),
                _uid = 0,
                _gid = 0,
                _mTime = DateTimeOffset.MinValue, // 0
                _typeFlag = entryType,
                _dataStream = new MemoryStream(Encoding.UTF8.GetBytes(longText))
            };
        }

        // Shared checksum and data length calculations for GNU entry writing.
        private void WriteGnuFieldsToBuffer(Span<byte> buffer)
        {
            int tmpChecksum = WriteName(buffer);
            tmpChecksum += WriteCommonFields(buffer, TarHelpers.GetCorrectTypeFlagForFormat(TarEntryFormat.Gnu, _typeFlag));
            tmpChecksum += WriteGnuMagicAndVersion(buffer);
            tmpChecksum += WritePosixAndGnuSharedFields(buffer);
            tmpChecksum += WriteGnuFields(buffer);

            _checksum = WriteChecksum(tmpChecksum, buffer);
        }

        // Writes the current header as a PAX Extended Attributes entry into the archive stream.
        private void WriteAsPaxExtendedAttributes(Stream archiveStream, Span<byte> buffer, Dictionary<string, string> extendedAttributes, bool isGea, int globalExtendedAttributesEntryNumber)
        {
            WriteAsPaxExtendedAttributesShared(isGea, globalExtendedAttributesEntryNumber, extendedAttributes);
            Debug.Assert(_dataStream == null || (extendedAttributes.Count > 0 && _dataStream.CanSeek)); // We generate the extended attributes data stream, should always be seekable
            WriteWithSeekableDataStream(TarEntryFormat.Pax, archiveStream, buffer);
        }

        // Asynchronously writes the current header as a PAX Extended Attributes entry into the archive stream and returns the value of the final checksum.
        private Task WriteAsPaxExtendedAttributesAsync(Stream archiveStream, Memory<byte> buffer, Dictionary<string, string> extendedAttributes, bool isGea, int globalExtendedAttributesEntryNumber, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WriteAsPaxExtendedAttributesShared(isGea, globalExtendedAttributesEntryNumber, extendedAttributes);
            Debug.Assert(_dataStream == null || (extendedAttributes.Count > 0 && _dataStream.CanSeek)); // We generate the extended attributes data stream, should always be seekable
            return WriteWithSeekableDataStreamAsync(TarEntryFormat.Pax, archiveStream, buffer, cancellationToken);
        }

        // Initializes the name, mode and type flag of a PAX extended attributes entry.
        private void WriteAsPaxExtendedAttributesShared(bool isGea, int globalExtendedAttributesEntryNumber, Dictionary<string, string> extendedAttributes)
        {
            Debug.Assert(isGea && globalExtendedAttributesEntryNumber >= 0 || !isGea && globalExtendedAttributesEntryNumber < 0);

            _dataStream = GenerateExtendedAttributesDataStream(extendedAttributes);
            _name = isGea ?
                GenerateGlobalExtendedAttributeName(globalExtendedAttributesEntryNumber) :
                GenerateExtendedAttributeName();

            _mode = TarHelpers.GetDefaultMode(_typeFlag);
            _typeFlag = isGea ? TarEntryType.GlobalExtendedAttributes : TarEntryType.ExtendedAttributes;
        }

        // Shared checksum and data length calculations for PAX entry writing.
        private void WritePaxFieldsToBuffer(Span<byte> buffer)
        {
            int tmpChecksum = WriteName(buffer);
            tmpChecksum += WriteCommonFields(buffer, TarHelpers.GetCorrectTypeFlagForFormat(TarEntryFormat.Pax, _typeFlag));
            tmpChecksum += WritePosixMagicAndVersion(buffer);
            tmpChecksum += WritePosixAndGnuSharedFields(buffer);

            _checksum = WriteChecksum(tmpChecksum, buffer);
        }

        // Writes the format-specific fields of the current entry, as well as the entry data length, into the specified buffer.
        private void WriteFieldsToBuffer(TarEntryFormat format, Span<byte> buffer)
        {
            switch (format)
            {
                case TarEntryFormat.V7:
                    WriteV7FieldsToBuffer(buffer);
                    break;
                case TarEntryFormat.Ustar:
                    WriteUstarFieldsToBuffer(buffer);
                    break;
                case TarEntryFormat.Pax:
                    WritePaxFieldsToBuffer(buffer);
                    break;
                case TarEntryFormat.Gnu:
                    WriteGnuFieldsToBuffer(buffer);
                    break;
            }
        }

        // Gnu and pax save in the name byte array only the UTF8 bytes that fit.
        // V7 does not support more than 100 bytes so it throws.
        private int WriteName(Span<byte> buffer)
        {
            ReadOnlySpan<char> name = _name;
            int encodedLength = GetUtf8TextLength(name);

            if (encodedLength > FieldLengths.Name)
            {
                if (_format is TarEntryFormat.V7)
                {
                    throw new ArgumentException(SR.Format(SR.TarEntryFieldExceedsMaxLength, nameof(TarEntry.Name)), ArgNameEntry);
                }

                int utf16NameTruncatedLength = GetUtf16TruncatedTextLength(name, FieldLengths.Name);
                name = name.Slice(0, utf16NameTruncatedLength);
            }

            return WriteAsUtf8String(name, buffer.Slice(FieldLocations.Name, FieldLengths.Name));
        }

        // 'https://www.freebsd.org/cgi/man.cgi?tar(5)'
        // If the path name is too long to fit in the 100 bytes provided by the standard format,
        // it can be split at any / character with the first portion going into the prefix field.
        private int WriteUstarName(Span<byte> buffer)
        {
            // We can have a path name as big as 256, prefix + '/' + name,
            // the separator in between can be neglected as the reader will append it when it joins both fields.
            const int MaxPathName = FieldLengths.Prefix + 1 + FieldLengths.Name;

            if (GetUtf8TextLength(_name) > MaxPathName)
            {
                throw new ArgumentException(SR.Format(SR.TarEntryFieldExceedsMaxLength, nameof(TarEntry.Name)), ArgNameEntry);
            }

            Span<byte> encodingBuffer = stackalloc byte[MaxPathName];
            int encoded = Encoding.UTF8.GetBytes(_name, encodingBuffer);
            ReadOnlySpan<byte> pathNameBytes = encodingBuffer.Slice(0, encoded);

            // If the pathname is able to fit in Name, we can write it down there and avoid calculating Prefix.
            if (pathNameBytes.Length <= FieldLengths.Name)
            {
                return WriteLeftAlignedBytesAndGetChecksum(pathNameBytes, buffer.Slice(FieldLocations.Name, FieldLengths.Name));
            }

            int lastIdx = pathNameBytes.LastIndexOfAny(PathInternal.Utf8DirectorySeparators);
            scoped ReadOnlySpan<byte> name;
            scoped ReadOnlySpan<byte> prefix;

            if (lastIdx < 1) // splitting at the root is not allowed.
            {
                name = pathNameBytes;
                prefix = default;
            }
            else
            {
                name = pathNameBytes.Slice(lastIdx + 1);
                prefix = pathNameBytes.Slice(0, lastIdx);
            }

            // At this point path name is > 100.
            // Attempt to split it in a way it can use prefix.
            while (prefix.Length - name.Length > FieldLengths.Prefix)
            {
                lastIdx = prefix.LastIndexOfAny(PathInternal.Utf8DirectorySeparators);
                if (lastIdx < 1)
                {
                    break;
                }

                name = pathNameBytes.Slice(lastIdx + 1);
                prefix = pathNameBytes.Slice(0, lastIdx);
            }

            if (prefix.Length <= FieldLengths.Prefix && name.Length <= FieldLengths.Name)
            {
                Debug.Assert(prefix.Length != 1 || !PathInternal.Utf8DirectorySeparators.Contains(prefix[0]));

                int checksum = WriteLeftAlignedBytesAndGetChecksum(prefix, buffer.Slice(FieldLocations.Prefix, FieldLengths.Prefix));
                checksum += WriteLeftAlignedBytesAndGetChecksum(name, buffer.Slice(FieldLocations.Name, FieldLengths.Name));

                return checksum;
            }
            else
            {
                throw new ArgumentException(SR.Format(SR.TarEntryFieldExceedsMaxLength, nameof(TarEntry.Name)), ArgNameEntry);
            }
        }

        // Writes all the common fields shared by all formats into the specified spans.
        private int WriteCommonFields(Span<byte> buffer, TarEntryType actualEntryType)
        {
            // Don't write an empty LinkName if the entry is a hardlink or symlink
            Debug.Assert(!string.IsNullOrEmpty(_linkName) ^ (_typeFlag is not TarEntryType.SymbolicLink and not TarEntryType.HardLink));

            int checksum = 0;

            if (_mode > 0)
            {
                checksum += FormatNumeric(_mode, buffer.Slice(FieldLocations.Mode, FieldLengths.Mode));
            }

            if (_uid > 0)
            {
                checksum += FormatNumeric(_uid, buffer.Slice(FieldLocations.Uid, FieldLengths.Uid));
            }

            if (_gid > 0)
            {
                checksum += FormatNumeric(_gid, buffer.Slice(FieldLocations.Gid, FieldLengths.Gid));
            }

            if (_size > 0)
            {
                checksum += FormatNumeric(_size, buffer.Slice(FieldLocations.Size, FieldLengths.Size));
            }

            checksum += WriteAsTimestamp(_mTime, buffer.Slice(FieldLocations.MTime, FieldLengths.MTime));

            char typeFlagChar = (char)actualEntryType;
            buffer[FieldLocations.TypeFlag] = (byte)typeFlagChar;
            checksum += typeFlagChar;

            if (!string.IsNullOrEmpty(_linkName))
            {
                ReadOnlySpan<char> linkName = _linkName;

                if (GetUtf8TextLength(linkName) > FieldLengths.LinkName)
                {
                    if (_format is not TarEntryFormat.Pax and not TarEntryFormat.Gnu)
                    {
                        throw new ArgumentException(SR.Format(SR.TarEntryFieldExceedsMaxLength, nameof(TarEntry.LinkName)), ArgNameEntry);
                    }

                    int truncatedLength = GetUtf16TruncatedTextLength(linkName, FieldLengths.LinkName);
                    linkName = linkName.Slice(0, truncatedLength);
                }

                checksum += WriteAsUtf8String(linkName, buffer.Slice(FieldLocations.LinkName, FieldLengths.LinkName));
            }

            return checksum;
        }

        // Calculates how many data bytes should be written, depending on the position pointer of the stream.
        // Only works if the stream is seekable.
        public long GetTotalDataBytesToWrite()
        {
            if (_dataStream == null)
            {
                return 0;
            }
            Debug.Assert(_dataStream.CanSeek);

            long length = _dataStream.Length;
            long position = _dataStream.Position;

            return position < length ? length - position : 0;
        }

        // Writes the magic and version fields of a ustar or pax entry into the specified spans.
        private static int WritePosixMagicAndVersion(Span<byte> buffer)
        {
            int checksum = WriteLeftAlignedBytesAndGetChecksum(UstarMagicBytes, buffer.Slice(FieldLocations.Magic, FieldLengths.Magic));
            checksum += WriteLeftAlignedBytesAndGetChecksum(UstarVersionBytes, buffer.Slice(FieldLocations.Version, FieldLengths.Version));
            return checksum;
        }

        // Writes the magic and vresion fields of a gnu entry into the specified spans.
        private static int WriteGnuMagicAndVersion(Span<byte> buffer)
        {
            int checksum = WriteLeftAlignedBytesAndGetChecksum(GnuMagicBytes, buffer.Slice(FieldLocations.Magic, FieldLengths.Magic));
            checksum += WriteLeftAlignedBytesAndGetChecksum(GnuVersionBytes, buffer.Slice(FieldLocations.Version, FieldLengths.Version));
            return checksum;
        }

        // Writes the posix fields shared by ustar, pax and gnu, into the specified spans.
        private int WritePosixAndGnuSharedFields(Span<byte> buffer)
        {
            int checksum = 0;

            if (!string.IsNullOrEmpty(_uName))
            {
                ReadOnlySpan<char> uName = _uName;

                if (GetUtf8TextLength(uName) > FieldLengths.UName)
                {
                    if (_format is not TarEntryFormat.Pax)
                    {
                        throw new ArgumentException(SR.Format(SR.TarEntryFieldExceedsMaxLength, nameof(PaxTarEntry.UserName)), ArgNameEntry);
                    }

                    int truncatedLength = GetUtf16TruncatedTextLength(uName, FieldLengths.UName);
                    uName = uName.Slice(0, truncatedLength);
                }

                checksum += WriteAsUtf8String(uName, buffer.Slice(FieldLocations.UName, FieldLengths.UName));
            }

            if (!string.IsNullOrEmpty(_gName))
            {
                ReadOnlySpan<char> gName = _gName;

                if (GetUtf8TextLength(gName) > FieldLengths.GName)
                {
                    if (_format is not TarEntryFormat.Pax)
                    {
                        throw new ArgumentException(SR.Format(SR.TarEntryFieldExceedsMaxLength, nameof(PaxTarEntry.GroupName)), ArgNameEntry);
                    }

                    int truncatedLength = GetUtf16TruncatedTextLength(gName, FieldLengths.GName);
                    gName = gName.Slice(0, truncatedLength);
                }

                checksum += WriteAsUtf8String(gName, buffer.Slice(FieldLocations.GName, FieldLengths.GName));
            }

            if (_devMajor > 0)
            {
                checksum += FormatNumeric(_devMajor, buffer.Slice(FieldLocations.DevMajor, FieldLengths.DevMajor));
            }

            if (_devMinor > 0)
            {
                checksum += FormatNumeric(_devMinor, buffer.Slice(FieldLocations.DevMinor, FieldLengths.DevMinor));
            }

            return checksum;
        }

        // Saves the gnu-specific fields into the specified spans.
        private int WriteGnuFields(Span<byte> buffer)
        {
            int checksum = WriteAsTimestamp(_aTime, buffer.Slice(FieldLocations.ATime, FieldLengths.ATime));
            checksum += WriteAsTimestamp(_cTime, buffer.Slice(FieldLocations.CTime, FieldLengths.CTime));

            if (_gnuUnusedBytes != null)
            {
                checksum += WriteLeftAlignedBytesAndGetChecksum(_gnuUnusedBytes, buffer.Slice(FieldLocations.GnuUnused, FieldLengths.AllGnuUnused));
            }

            return checksum;
        }

        // Writes the current header's data stream into the archive stream.
        private void WriteData(Stream archiveStream, Stream dataStream)
        {
            // Before writing, update the offset field now that the entry belongs to an archive
            SetDataOffset(this, archiveStream);

            dataStream.CopyTo(archiveStream); // The data gets copied from the current position
            WriteEmptyPadding(archiveStream);
        }

        // Calculates the padding for the current entry and writes it after the data.
        private void WriteEmptyPadding(Stream archiveStream)
        {
            int paddingAfterData = TarHelpers.CalculatePadding(_size);
            if (paddingAfterData != 0)
            {
                Debug.Assert(paddingAfterData <= TarHelpers.RecordSize);

                Span<byte> zeros = stackalloc byte[TarHelpers.RecordSize];
                zeros = zeros.Slice(0, paddingAfterData);
                zeros.Clear();

                archiveStream.Write(zeros);
            }
        }

        // Calculates the padding for the current entry and asynchronously writes it after the data.
        private ValueTask WriteEmptyPaddingAsync(Stream archiveStream, CancellationToken cancellationToken)
        {
            int paddingAfterData = TarHelpers.CalculatePadding(_size);
            if (paddingAfterData != 0)
            {
                Debug.Assert(paddingAfterData <= TarHelpers.RecordSize);

                byte[] zeros = new byte[paddingAfterData];
                return archiveStream.WriteAsync(zeros, cancellationToken);
            }

            return ValueTask.CompletedTask;
        }

        // Asynchronously writes the current header's data stream into the archive stream.
        private async Task WriteDataAsync(Stream archiveStream, Stream dataStream, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Before writing, update the offset field now that the entry belongs to an archive
            SetDataOffset(this, archiveStream);

            await dataStream.CopyToAsync(archiveStream, cancellationToken).ConfigureAwait(false); // The data gets copied from the current position

            int paddingAfterData = TarHelpers.CalculatePadding(_size);
            if (paddingAfterData != 0)
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent(paddingAfterData);
                Array.Clear(buffer, 0, paddingAfterData);

                await archiveStream.WriteAsync(buffer.AsMemory(0, paddingAfterData), cancellationToken).ConfigureAwait(false);

                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        // Generates a data stream (seekable) containing the extended attribute metadata of the entry it precedes.
        // Returns a null stream if the extended attributes dictionary is empty.
        private static MemoryStream? GenerateExtendedAttributesDataStream(Dictionary<string, string> extendedAttributes)
        {
            MemoryStream? dataStream = null;

            byte[]? buffer = null;
            Span<byte> span = stackalloc byte[512];

            if (extendedAttributes.Count > 0)
            {
                dataStream = new MemoryStream();

                foreach ((string attribute, string value) in extendedAttributes)
                {
                    // Generates an extended attribute key value pair string saved into a byte array, following the ISO/IEC 10646-1:2000 standard UTF-8 encoding format.
                    // https://pubs.opengroup.org/onlinepubs/9699919799/utilities/pax.html

                    // The format is:
                    //     "XX attribute=value\n"
                    // where "XX" is the number of characters in the entry, including those required for the count itself.
                    // If prepending the length digits increases the number of digits, we need to expand.
                    int length = 3 + Encoding.UTF8.GetByteCount(attribute) + Encoding.UTF8.GetByteCount(value);
                    int originalDigitCount = CountDigits(length), newDigitCount;
                    length += originalDigitCount;
                    while ((newDigitCount = CountDigits(length)) != originalDigitCount)
                    {
                        length += newDigitCount - originalDigitCount;
                        originalDigitCount = newDigitCount;
                    }
                    Debug.Assert(length == CountDigits(length) + 3 + Encoding.UTF8.GetByteCount(attribute) + Encoding.UTF8.GetByteCount(value));

                    // Get a large enough buffer if we don't already have one.
                    if (span.Length < length)
                    {
                        if (buffer is not null)
                        {
                            ArrayPool<byte>.Shared.Return(buffer);
                        }
                        span = buffer = ArrayPool<byte>.Shared.Rent(length);
                    }

                    // Format the contents.
                    bool formatted = Utf8Formatter.TryFormat(length, span, out int bytesWritten);
                    Debug.Assert(formatted);
                    span[bytesWritten++] = (byte)' ';
                    bytesWritten += Encoding.UTF8.GetBytes(attribute, span.Slice(bytesWritten));
                    span[bytesWritten++] = (byte)'=';
                    bytesWritten += Encoding.UTF8.GetBytes(value, span.Slice(bytesWritten));
                    span[bytesWritten++] = (byte)'\n';

                    // Write it to the stream.
                    dataStream.Write(span.Slice(0, bytesWritten));
                }

                dataStream.Position = 0; // Ensure it gets written into the archive from the beginning
            }

            if (buffer is not null)
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            return dataStream;

            static int CountDigits(int value)
            {
                Debug.Assert(value >= 0);
                int digits = 1;
                while (true)
                {
                    value /= 10;
                    if (value == 0) break;
                    digits++;
                }
                return digits;
            }
        }

        // Some fields that have a reserved spot in the header, may not fit in such field anymore, but they can fit in the
        // extended attributes. They get collected and saved in that dictionary, with no restrictions.
        private void CollectExtendedAttributesFromStandardFieldsIfNeeded()
        {
            ExtendedAttributes[PaxEaName] = _name;
            ExtendedAttributes[PaxEaMTime] = TarHelpers.GetTimestampStringFromDateTimeOffset(_mTime);

            TryAddStringField(ExtendedAttributes, PaxEaGName, _gName, FieldLengths.GName);
            TryAddStringField(ExtendedAttributes, PaxEaUName, _uName, FieldLengths.UName);

            if (!string.IsNullOrEmpty(_linkName))
            {
                Debug.Assert(_typeFlag is TarEntryType.SymbolicLink or TarEntryType.HardLink);
                ExtendedAttributes[PaxEaLinkName] = _linkName;
            }

            if (_size > Octal12ByteFieldMaxValue)
            {
                ExtendedAttributes[PaxEaSize] = _size.ToString();
            }
            else
            {
                ExtendedAttributes.Remove(PaxEaSize);
            }

            if (_uid > Octal8ByteFieldMaxValue)
            {
                ExtendedAttributes[PaxEaUid] = _uid.ToString();
            }
            else
            {
                ExtendedAttributes.Remove(PaxEaUid);
            }

            if (_gid > Octal8ByteFieldMaxValue)
            {
                ExtendedAttributes[PaxEaGid] = _gid.ToString();
            }
            else
            {
                ExtendedAttributes.Remove(PaxEaGid);
            }

            if (_devMajor > Octal8ByteFieldMaxValue)
            {
                ExtendedAttributes[PaxEaDevMajor] = _devMajor.ToString();
            }
            else
            {
                ExtendedAttributes.Remove(PaxEaDevMajor);
            }

            if (_devMinor > Octal8ByteFieldMaxValue)
            {
                ExtendedAttributes[PaxEaDevMinor] = _devMinor.ToString();
            }
            else
            {
                ExtendedAttributes.Remove(PaxEaDevMinor);
            }

            // Sets the specified string to the dictionary if it's longer than the specified max byte length; otherwise, remove it.
            static void TryAddStringField(Dictionary<string, string> extendedAttributes, string key, string? value, int maxLength)
            {
                if (string.IsNullOrEmpty(value) || GetUtf8TextLength(value) <= maxLength)
                {
                    extendedAttributes.Remove(key);
                }
                else
                {
                    extendedAttributes[key] = value;
                }
            }
        }

        // The checksum accumulator first adds up the byte values of eight space chars, then the final number
        // is written on top of those spaces on the specified span as ascii.
        // At the end, it's saved in the header field and the final value returned.
        private static int WriteChecksum(int checksum, Span<byte> buffer)
        {
            // The checksum field is also counted towards the total sum
            // but as an array filled with spaces
            checksum += (byte)' ' * 8;

            Span<byte> converted = stackalloc byte[FieldLengths.Checksum];
            converted.Clear();
            FormatOctal(checksum, converted);

            Span<byte> destination = buffer.Slice(FieldLocations.Checksum, FieldLengths.Checksum);

            // Checksum field ends with a null and a space
            destination[^1] = (byte)' ';
            destination[^2] = (byte)'\0';

            int i = destination.Length - 3;
            int j = converted.Length - 1;

            while (i >= 0)
            {
                if (j >= 0)
                {
                    destination[i] = converted[j];
                    j--;
                }
                else
                {
                    destination[i] = (byte)'0';  // Leading zero chars
                }
                i--;
            }

            return checksum;
        }

        // Writes the specified bytes into the specified destination, aligned to the left. Returns the sum of the value of all the bytes that were written.
        private static int WriteLeftAlignedBytesAndGetChecksum(ReadOnlySpan<byte> bytesToWrite, Span<byte> destination)
        {
            Debug.Assert(destination.Length > 1);

            // Copy as many bytes as will fit
            int numToCopy = Math.Min(bytesToWrite.Length, destination.Length);
            bytesToWrite = bytesToWrite.Slice(0, numToCopy);
            bytesToWrite.CopyTo(destination);

            return Checksum(bytesToWrite);
        }

        // Writes the specified bytes aligned to the right, filling all the leading bytes with the zero char 0x30,
        // ensuring a null terminator is included at the end of the specified span.
        private static int WriteRightAlignedBytesAndGetChecksum(ReadOnlySpan<byte> bytesToWrite, Span<byte> destination)
        {
            Debug.Assert(destination.Length > 1);

            // Null terminated
            destination[^1] = (byte)'\0';

            // Copy as many input bytes as will fit
            int numToCopy = Math.Min(bytesToWrite.Length, destination.Length - 1);
            bytesToWrite = bytesToWrite.Slice(0, numToCopy);
            int copyPos = destination.Length - 1 - bytesToWrite.Length;
            bytesToWrite.CopyTo(destination.Slice(copyPos));

            // Fill all leading bytes with zeros
            destination.Slice(0, copyPos).Fill((byte)'0');

            return Checksum(destination);
        }

        private static int Checksum(ReadOnlySpan<byte> bytes)
        {
            int checksum = 0;
            foreach (byte b in bytes)
            {
                checksum += b;
            }
            return checksum;
        }

        private int FormatNumeric(int value, Span<byte> destination)
        {
            Debug.Assert(destination.Length == 8, "8 byte field expected.");

            bool isOctalRange = value >= 0 && value <= Octal8ByteFieldMaxValue;

            if (isOctalRange || _format == TarEntryFormat.Pax)
            {
                return FormatOctal(value, destination);
            }
            else if (_format == TarEntryFormat.Gnu)
            {
                // GNU format: store negative numbers in big endian format with leading '0xff' byte.
                //             store positive numbers in big endian format with leading '0x80' byte.
                long destinationValue = value;
                destinationValue |= 1L << 63;
                BinaryPrimitives.WriteInt64BigEndian(destination, destinationValue);
                return Checksum(destination);
            }
            else
            {
                throw new ArgumentException(SR.Format(SR.TarFieldTooLargeForEntryFormat, _format));
            }
        }

        private int FormatNumeric(long value, Span<byte> destination)
        {
            Debug.Assert(destination.Length == 12, "12 byte field expected.");
            const int Offset = 4; // 4 bytes before the long.

            bool isOctalRange = value >= 0 && value <= Octal12ByteFieldMaxValue;

            if (isOctalRange || _format == TarEntryFormat.Pax)
            {
                return FormatOctal(value, destination);
            }
            else if (_format == TarEntryFormat.Gnu)
            {
                // GNU format: store negative numbers in big endian format with leading '0xff' byte.
                //             store positive numbers in big endian format with leading '0x80' byte.
                BinaryPrimitives.WriteUInt32BigEndian(destination, value < 0 ? 0xffffffff : 0x80000000);
                BinaryPrimitives.WriteInt64BigEndian(destination.Slice(Offset), value);
                return Checksum(destination);
            }
            else
            {
                throw new ArgumentException(SR.Format(SR.TarFieldTooLargeForEntryFormat, _format));
            }
        }

        // Writes the specified decimal number as a right-aligned octal number and returns its checksum.
        private static int FormatOctal(long value, Span<byte> destination)
        {
            ulong remaining = (ulong)value;
            Span<byte> digits = stackalloc byte[32]; // longer than any possible octal formatting of a ulong

            int i = digits.Length - 1;
            while (true)
            {
                digits[i] = (byte)('0' + (remaining % 8));
                remaining /= 8;
                if (remaining == 0) break;
                i--;
            }

            return WriteRightAlignedBytesAndGetChecksum(digits.Slice(i), destination);
        }

        // Writes the specified DateTimeOffset's Unix time seconds, and returns its checksum.
        private int WriteAsTimestamp(DateTimeOffset timestamp, Span<byte> destination)
        {
            long unixTimeSeconds = timestamp.ToUnixTimeSeconds();
            return FormatNumeric(unixTimeSeconds, destination);
        }

        // Writes the specified text as an UTF8 string aligned to the left, and returns its checksum.
        private static int WriteAsUtf8String(ReadOnlySpan<char> text, Span<byte> buffer)
        {
            int encoded = Encoding.UTF8.GetBytes(text, buffer);
            return WriteLeftAlignedBytesAndGetChecksum(buffer.Slice(0, encoded), buffer);
        }

        // Gets the special name for the 'name' field in an extended attribute entry.
        // Format: "%d/PaxHeaders.%p/%f"
        // - %d: The directory name of the file, equivalent to the result of the dirname utility on the translated pathname.
        // - %p: The current process ID.
        // - %f: The filename of the file, equivalent to the result of the basename utility on the translated pathname.
        private string GenerateExtendedAttributeName()
        {
            ReadOnlySpan<char> dirName = Path.GetDirectoryName(_name.AsSpan());
            dirName = dirName.IsEmpty ? "." : dirName;

            ReadOnlySpan<char> fileName = Path.GetFileName(_name.AsSpan());
            fileName = fileName.IsEmpty ? "." : fileName;

            return _typeFlag is TarEntryType.Directory or TarEntryType.DirectoryList ?
                $"{dirName}/PaxHeaders.{Environment.ProcessId}/{fileName}{Path.DirectorySeparatorChar}" :
                $"{dirName}/PaxHeaders.{Environment.ProcessId}/{fileName}";
        }

        // Gets the special name for the 'name' field in a global extended attribute entry.
        // Format: "%d/GlobalHead.%p.%n"
        // - %d: The path of the $TMPDIR variable, if found. Otherwise, the value is '/tmp'.
        // - %p: The current process ID.
        // - %n: The sequence number of the global extended header record of the archive, starting at 1.
        // If the path of $TMPDIR makes the final string too long to fit in the 'name' field,
        // then the TMPDIR='/tmp' is used.
        private static string GenerateGlobalExtendedAttributeName(int globalExtendedAttributesEntryNumber)
        {
            Debug.Assert(globalExtendedAttributesEntryNumber >= 1);

            ReadOnlySpan<char> tmp = Path.TrimEndingDirectorySeparator(Path.GetTempPath());

            string result = $"{tmp}/GlobalHead.{Environment.ProcessId}.{globalExtendedAttributesEntryNumber}";
            return result.Length >= FieldLengths.Name ?
                string.Concat("/tmp", result.AsSpan(tmp.Length)) :
                result;
        }

        private static int GetUtf8TextLength(ReadOnlySpan<char> text)
            => Encoding.UTF8.GetByteCount(text);

        // Returns the text's utf16 length truncated at the specified utf8 max length.
        private static int GetUtf16TruncatedTextLength(ReadOnlySpan<char> text, int utf8MaxLength)
        {
            Debug.Assert(GetUtf8TextLength(text) > utf8MaxLength);

            int utf8Length = 0;
            int utf16TruncatedLength = 0;

            foreach (Rune rune in text.EnumerateRunes())
            {
                utf8Length += rune.Utf8SequenceLength;
                if (utf8Length <= utf8MaxLength)
                {
                    utf16TruncatedLength += rune.Utf16SequenceLength;
                }
                else
                {
                    break;
                }
            }

            return utf16TruncatedLength;
        }
    }
}
