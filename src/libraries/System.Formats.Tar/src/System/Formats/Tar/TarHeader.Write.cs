// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
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
        private static ReadOnlySpan<byte> UstarMagicBytes => "ustar\0"u8;
        private static ReadOnlySpan<byte> UstarVersionBytes => "00"u8;

        private static ReadOnlySpan<byte> GnuMagicBytes => "ustar "u8;
        private static ReadOnlySpan<byte> GnuVersionBytes => " \0"u8;

        // Predefined text for the Name field of a GNU long metadata entry. Applies for both LongPath ('L') and LongLink ('K').
        private const string GnuLongMetadataName = "././@LongLink";
        private const string ArgNameEntry = "entry";

        // Writes the current header as a V7 entry into the archive stream.
        internal void WriteAsV7(Stream archiveStream, Span<byte> buffer)
        {
            WriteV7FieldsToBuffer(buffer);

            archiveStream.Write(buffer);

            if (_dataStream != null)
            {
                WriteData(archiveStream, _dataStream, _size);
            }
        }

        // Asynchronously writes the current header as a V7 entry into the archive stream and returns the value of the final checksum.
        internal async Task WriteAsV7Async(Stream archiveStream, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            WriteV7FieldsToBuffer(buffer.Span);

            await archiveStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);

            if (_dataStream != null)
            {
                await WriteDataAsync(archiveStream, _dataStream, _size, cancellationToken).ConfigureAwait(false);
            }
        }

        // Writes the V7 header fields to the specified buffer, calculates and writes the checksum, then returns the final data length.
        private void WriteV7FieldsToBuffer(Span<byte> buffer)
        {
            _size = GetTotalDataBytesToWrite();
            TarEntryType actualEntryType = TarHelpers.GetCorrectTypeFlagForFormat(TarEntryFormat.V7, _typeFlag);

            int tmpChecksum = WriteName(buffer);
            tmpChecksum += WriteCommonFields(buffer, actualEntryType);
            _checksum = WriteChecksum(tmpChecksum, buffer);
        }

        // Writes the current header as a Ustar entry into the archive stream.
        internal void WriteAsUstar(Stream archiveStream, Span<byte> buffer)
        {
            WriteUstarFieldsToBuffer(buffer);

            archiveStream.Write(buffer);

            if (_dataStream != null)
            {
                WriteData(archiveStream, _dataStream, _size);
            }
        }

        // Asynchronously rites the current header as a Ustar entry into the archive stream and returns the value of the final checksum.
        internal async Task WriteAsUstarAsync(Stream archiveStream, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            WriteUstarFieldsToBuffer(buffer.Span);

            await archiveStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);

            if (_dataStream != null)
            {
                await WriteDataAsync(archiveStream, _dataStream, _size, cancellationToken).ConfigureAwait(false);
            }
        }

        // Writes the Ustar header fields to the specified buffer, calculates and writes the checksum, then returns the final data length.
        private void WriteUstarFieldsToBuffer(Span<byte> buffer)
        {
            _size = GetTotalDataBytesToWrite();
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

        // Writes the current header as a PAX entry into the archive stream.
        // Makes sure to add the preceding extended attributes entry before the actual entry.
        internal void WriteAsPax(Stream archiveStream, Span<byte> buffer)
        {
            Debug.Assert(_typeFlag is not TarEntryType.GlobalExtendedAttributes);

            // First, we write the preceding extended attributes header
            TarHeader extendedAttributesHeader = new(TarEntryFormat.Pax);
            // Fill the current header's dict
            _size = GetTotalDataBytesToWrite();
            CollectExtendedAttributesFromStandardFieldsIfNeeded();
            // And pass the attributes to the preceding extended attributes header for writing
            extendedAttributesHeader.WriteAsPaxExtendedAttributes(archiveStream, buffer, ExtendedAttributes, isGea: false, globalExtendedAttributesEntryNumber: -1);
            buffer.Clear(); // Reset it to reuse it
            // Second, we write this header as a normal one
            WriteAsPaxInternal(archiveStream, buffer);
        }

        // Asynchronously writes the current header as a PAX entry into the archive stream.
        // Makes sure to add the preceding exteded attributes entry before the actual entry.
        internal async Task WriteAsPaxAsync(Stream archiveStream, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            Debug.Assert(_typeFlag is not TarEntryType.GlobalExtendedAttributes);
            cancellationToken.ThrowIfCancellationRequested();

            // First, we write the preceding extended attributes header
            TarHeader extendedAttributesHeader = new(TarEntryFormat.Pax);
            // Fill the current header's dict
            _size = GetTotalDataBytesToWrite();
            CollectExtendedAttributesFromStandardFieldsIfNeeded();
            // And pass the attributes to the preceding extended attributes header for writing
            await extendedAttributesHeader.WriteAsPaxExtendedAttributesAsync(archiveStream, buffer, ExtendedAttributes, isGea: false, globalExtendedAttributesEntryNumber: -1, cancellationToken).ConfigureAwait(false);

            buffer.Span.Clear(); // Reset it to reuse it
            // Second, we write this header as a normal one
            await WriteAsPaxInternalAsync(archiveStream, buffer, cancellationToken).ConfigureAwait(false);
        }

        // Writes the current header as a Gnu entry into the archive stream.
        // Makes sure to add the preceding LongLink and/or LongPath entries if necessary, before the actual entry.
        internal void WriteAsGnu(Stream archiveStream, Span<byte> buffer)
        {
            // First, we determine if we need a preceding LongLink, and write it if needed
            if (_linkName != null && Encoding.UTF8.GetByteCount(_linkName) > FieldLengths.LinkName)
            {
                TarHeader longLinkHeader = GetGnuLongMetadataHeader(TarEntryType.LongLink, _linkName);
                longLinkHeader.WriteAsGnuInternal(archiveStream, buffer);
                buffer.Clear(); // Reset it to reuse it
            }

            // Second, we determine if we need a preceding LongPath, and write it if needed
            if (Encoding.UTF8.GetByteCount(_name) > FieldLengths.Name)
            {
                TarHeader longPathHeader = GetGnuLongMetadataHeader(TarEntryType.LongPath, _name);
                longPathHeader.WriteAsGnuInternal(archiveStream, buffer);
                buffer.Clear(); // Reset it to reuse it
            }

            // Third, we write this header as a normal one
            WriteAsGnuInternal(archiveStream, buffer);
        }

        // Writes the current header as a Gnu entry into the archive stream.
        // Makes sure to add the preceding LongLink and/or LongPath entries if necessary, before the actual entry.
        internal async Task WriteAsGnuAsync(Stream archiveStream, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // First, we determine if we need a preceding LongLink, and write it if needed
            if (_linkName != null && Encoding.UTF8.GetByteCount(_linkName) > FieldLengths.LinkName)
            {
                TarHeader longLinkHeader = GetGnuLongMetadataHeader(TarEntryType.LongLink, _linkName);
                await longLinkHeader.WriteAsGnuInternalAsync(archiveStream, buffer, cancellationToken).ConfigureAwait(false);
                buffer.Span.Clear(); // Reset it to reuse it
            }

            // Second, we determine if we need a preceding LongPath, and write it if needed
            if (Encoding.UTF8.GetByteCount(_name) > FieldLengths.Name)
            {
                TarHeader longPathHeader = GetGnuLongMetadataHeader(TarEntryType.LongPath, _name);
                await longPathHeader.WriteAsGnuInternalAsync(archiveStream, buffer, cancellationToken).ConfigureAwait(false);
                buffer.Span.Clear(); // Reset it to reuse it
            }

            // Third, we write this header as a normal one
            await WriteAsGnuInternalAsync(archiveStream, buffer, cancellationToken).ConfigureAwait(false);
        }

        // Creates and returns a GNU long metadata header, with the specified long text written into its data stream.
        private static TarHeader GetGnuLongMetadataHeader(TarEntryType entryType, string longText)
        {
            Debug.Assert(entryType is TarEntryType.LongPath or TarEntryType.LongLink);

            TarHeader longMetadataHeader = new(TarEntryFormat.Gnu);

            longMetadataHeader._name = GnuLongMetadataName; // Same name for both longpath or longlink
            longMetadataHeader._mode = TarHelpers.GetDefaultMode(entryType);
            longMetadataHeader._uid = 0;
            longMetadataHeader._gid = 0;
            longMetadataHeader._mTime = DateTimeOffset.MinValue; // 0
            longMetadataHeader._typeFlag = entryType;
            longMetadataHeader._dataStream = new MemoryStream(Encoding.UTF8.GetBytes(longText));

            return longMetadataHeader;
        }

        // Writes the current header as a GNU entry into the archive stream.
        internal void WriteAsGnuInternal(Stream archiveStream, Span<byte> buffer)
        {
            WriteAsGnuSharedInternal(buffer);

            archiveStream.Write(buffer);

            if (_dataStream != null)
            {
                WriteData(archiveStream, _dataStream, _size);
            }
        }

        // Asynchronously writes the current header as a GNU entry into the archive stream.
        internal async Task WriteAsGnuInternalAsync(Stream archiveStream, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            WriteAsGnuSharedInternal(buffer.Span);

            await archiveStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);

            if (_dataStream != null)
            {
                await WriteDataAsync(archiveStream, _dataStream, _size, cancellationToken).ConfigureAwait(false);
            }
        }

        // Shared checksum and data length calculations for GNU entry writing.
        private void WriteAsGnuSharedInternal(Span<byte> buffer)
        {
            _size = GetTotalDataBytesToWrite();

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
            WriteAsPaxInternal(archiveStream, buffer);
        }

        // Asynchronously writes the current header as a PAX Extended Attributes entry into the archive stream and returns the value of the final checksum.
        private Task WriteAsPaxExtendedAttributesAsync(Stream archiveStream, Memory<byte> buffer, Dictionary<string, string> extendedAttributes, bool isGea, int globalExtendedAttributesEntryNumber, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WriteAsPaxExtendedAttributesShared(isGea, globalExtendedAttributesEntryNumber, extendedAttributes);
            return WriteAsPaxInternalAsync(archiveStream, buffer, cancellationToken);
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
            _size = GetTotalDataBytesToWrite();
            _typeFlag = isGea ? TarEntryType.GlobalExtendedAttributes : TarEntryType.ExtendedAttributes;
        }

        // Both the Extended Attributes and Global Extended Attributes entry headers are written in a similar way, just the data changes
        // This method writes an entry as both entries require, using the data from the current header instance.
        private void WriteAsPaxInternal(Stream archiveStream, Span<byte> buffer)
        {
            WriteAsPaxSharedInternal(buffer);

            archiveStream.Write(buffer);

            if (_dataStream != null)
            {
                WriteData(archiveStream, _dataStream, _size);
            }
        }

        // Both the Extended Attributes and Global Extended Attributes entry headers are written in a similar way, just the data changes
        // This method asynchronously writes an entry as both entries require, using the data from the current header instance.
        private async Task WriteAsPaxInternalAsync(Stream archiveStream, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            WriteAsPaxSharedInternal(buffer.Span);

            await archiveStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);

            if (_dataStream != null)
            {
                await WriteDataAsync(archiveStream, _dataStream, _size, cancellationToken).ConfigureAwait(false);
            }
        }

        // Shared checksum and data length calculations for PAX entry writing.
        private void WriteAsPaxSharedInternal(Span<byte> buffer)
        {
            int tmpChecksum = WriteName(buffer);
            tmpChecksum += WriteCommonFields(buffer, TarHelpers.GetCorrectTypeFlagForFormat(TarEntryFormat.Pax, _typeFlag));
            tmpChecksum += WritePosixMagicAndVersion(buffer);
            tmpChecksum += WritePosixAndGnuSharedFields(buffer);

            _checksum = WriteChecksum(tmpChecksum, buffer);
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
                checksum += FormatOctal(_mode, buffer.Slice(FieldLocations.Mode, FieldLengths.Mode));
            }

            if (_uid > 0)
            {
                checksum += FormatOctal(_uid, buffer.Slice(FieldLocations.Uid, FieldLengths.Uid));
            }

            if (_gid > 0)
            {
                checksum += FormatOctal(_gid, buffer.Slice(FieldLocations.Gid, FieldLengths.Gid));
            }

            if (_size > 0)
            {
                if (_size <= TarHelpers.MaxSizeLength)
                {
                    checksum += FormatOctal(_size, buffer.Slice(FieldLocations.Size, FieldLengths.Size));
                }
                else if (_format is not TarEntryFormat.Pax)
                {
                    throw new ArgumentException(SR.Format(SR.TarSizeFieldTooLargeForEntryFormat, _format));
                }
                else
                {
                    Debug.Assert(_typeFlag is not TarEntryType.ExtendedAttributes and not TarEntryType.GlobalExtendedAttributes);
                    Debug.Assert(Convert.ToInt64(ExtendedAttributes[PaxEaSize]) > TarHelpers.MaxSizeLength);
                }
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
        private long GetTotalDataBytesToWrite()
        {
            if (_dataStream != null)
            {
                long length = _dataStream.Length;
                long position = _dataStream.Position;
                if (position < length)
                {
                    return length - position;
                }
            }
            return 0;
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
                checksum += FormatOctal(_devMajor, buffer.Slice(FieldLocations.DevMajor, FieldLengths.DevMajor));
            }

            if (_devMinor > 0)
            {
                checksum += FormatOctal(_devMinor, buffer.Slice(FieldLocations.DevMinor, FieldLengths.DevMinor));
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
        private static void WriteData(Stream archiveStream, Stream dataStream, long actualLength)
        {
            dataStream.CopyTo(archiveStream); // The data gets copied from the current position

            int paddingAfterData = TarHelpers.CalculatePadding(actualLength);
            if (paddingAfterData != 0)
            {
                Debug.Assert(paddingAfterData <= TarHelpers.RecordSize);

                Span<byte> padding = stackalloc byte[TarHelpers.RecordSize];
                padding = padding.Slice(0, paddingAfterData);
                padding.Clear();

                archiveStream.Write(padding);
            }
        }

        // Asynchronously writes the current header's data stream into the archive stream.
        private static async Task WriteDataAsync(Stream archiveStream, Stream dataStream, long actualLength, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await dataStream.CopyToAsync(archiveStream, cancellationToken).ConfigureAwait(false); // The data gets copied from the current position

            int paddingAfterData = TarHelpers.CalculatePadding(actualLength);
            if (paddingAfterData != 0)
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent(paddingAfterData);
                Array.Clear(buffer, 0, paddingAfterData);

                await archiveStream.WriteAsync(buffer.AsMemory(0, paddingAfterData), cancellationToken).ConfigureAwait(false);

                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        // Dumps into the archive stream an extended attribute entry containing metadata of the entry it precedes.
        private static Stream? GenerateExtendedAttributesDataStream(Dictionary<string, string> extendedAttributes)
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

            if (_size > TarHelpers.MaxSizeLength)
            {
                ExtendedAttributes[PaxEaSize] = _size.ToString();
            }
            else
            {
                ExtendedAttributes.Remove(PaxEaSize);
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
        internal static int WriteChecksum(int checksum, Span<byte> buffer)
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

        // Writes the specified decimal number as a right-aligned octal number and returns its checksum.
        internal static int FormatOctal(long value, Span<byte> destination)
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

        // Writes the specified DateTimeOffset's Unix time seconds as a right-aligned octal number, and returns its checksum.
        private static int WriteAsTimestamp(DateTimeOffset timestamp, Span<byte> destination)
        {
            long unixTimeSeconds = timestamp.ToUnixTimeSeconds();
            return FormatOctal(unixTimeSeconds, destination);
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
