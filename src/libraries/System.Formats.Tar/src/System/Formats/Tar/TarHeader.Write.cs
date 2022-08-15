// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Formats.Tar
{
    // Writes header attributes of a tar archive entry.
    internal sealed partial class TarHeader
    {
        private static ReadOnlySpan<byte> PaxMagicBytes => "ustar\0"u8;
        private static ReadOnlySpan<byte> PaxVersionBytes => "00"u8;

        private static ReadOnlySpan<byte> GnuMagicBytes => "ustar "u8;
        private static ReadOnlySpan<byte> GnuVersionBytes => " \0"u8;

        // Extended Attribute entries have a special format in the Name field:
        // "{dirName}/PaxHeaders.{processId}/{fileName}{trailingSeparator}"
        private const string PaxHeadersFormat = "{0}/PaxHeaders.{1}/{2}{3}";

        // Predefined text for the Name field of a GNU long metadata entry. Applies for both LongPath ('L') and LongLink ('K').
        private const string GnuLongMetadataName = "././@LongLink";

        // Writes the current header as a V7 entry into the archive stream.
        internal void WriteAsV7(Stream archiveStream, Span<byte> buffer)
        {
            long actualLength = WriteV7FieldsToBuffer(buffer);

            archiveStream.Write(buffer);

            if (_dataStream != null)
            {
                WriteData(archiveStream, _dataStream, actualLength);
            }
        }

        // Asynchronously writes the current header as a V7 entry into the archive stream and returns the value of the final checksum.
        internal async Task WriteAsV7Async(Stream archiveStream, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long actualLength = WriteV7FieldsToBuffer(buffer.Span);

            await archiveStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);

            if (_dataStream != null)
            {
                await WriteDataAsync(archiveStream, _dataStream, actualLength, cancellationToken).ConfigureAwait(false);
            }
        }

        // Writes the V7 header fields to the specified buffer, calculates and writes the checksum, then returns the final data length.
        private long WriteV7FieldsToBuffer(Span<byte> buffer)
        {
            long actualLength = GetTotalDataBytesToWrite();
            TarEntryType actualEntryType = TarHelpers.GetCorrectTypeFlagForFormat(TarEntryFormat.V7, _typeFlag);

            int tmpChecksum = WriteName(buffer, out _);
            tmpChecksum += WriteCommonFields(buffer, actualLength, actualEntryType);
            _checksum = WriteChecksum(tmpChecksum, buffer);

            return actualLength;
        }

        // Writes the current header as a Ustar entry into the archive stream.
        internal void WriteAsUstar(Stream archiveStream, Span<byte> buffer)
        {
            long actualLength = WriteUstarFieldsToBuffer(buffer);

            archiveStream.Write(buffer);

            if (_dataStream != null)
            {
                WriteData(archiveStream, _dataStream, actualLength);
            }
        }

        // Asynchronously rites the current header as a Ustar entry into the archive stream and returns the value of the final checksum.
        internal async Task WriteAsUstarAsync(Stream archiveStream, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long actualLength = WriteUstarFieldsToBuffer(buffer.Span);

            await archiveStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);

            if (_dataStream != null)
            {
                await WriteDataAsync(archiveStream, _dataStream, actualLength, cancellationToken).ConfigureAwait(false);
            }
        }

        // Writes the Ustar header fields to the specified buffer, calculates and writes the checksum, then returns the final data length.
        private long WriteUstarFieldsToBuffer(Span<byte> buffer)
        {
            long actualLength = GetTotalDataBytesToWrite();
            TarEntryType actualEntryType = TarHelpers.GetCorrectTypeFlagForFormat(TarEntryFormat.Ustar, _typeFlag);

            int tmpChecksum = WritePosixName(buffer);
            tmpChecksum += WriteCommonFields(buffer, actualLength, actualEntryType);
            tmpChecksum += WritePosixMagicAndVersion(buffer);
            tmpChecksum += WritePosixAndGnuSharedFields(buffer);
            _checksum = WriteChecksum(tmpChecksum, buffer);

            return actualLength;
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
            if (_linkName?.Length > FieldLengths.LinkName)
            {
                TarHeader longLinkHeader = GetGnuLongMetadataHeader(TarEntryType.LongLink, _linkName);
                longLinkHeader.WriteAsGnuInternal(archiveStream, buffer);
                buffer.Clear(); // Reset it to reuse it
            }

            // Second, we determine if we need a preceding LongPath, and write it if needed
            if (_name.Length > FieldLengths.Name)
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
            if (_linkName?.Length > FieldLengths.LinkName)
            {
                TarHeader longLinkHeader = GetGnuLongMetadataHeader(TarEntryType.LongLink, _linkName);
                await longLinkHeader.WriteAsGnuInternalAsync(archiveStream, buffer, cancellationToken).ConfigureAwait(false);
                buffer.Span.Clear(); // Reset it to reuse it
            }

            // Second, we determine if we need a preceding LongPath, and write it if needed
            if (_name.Length > FieldLengths.Name)
            {
                TarHeader longPathHeader = await GetGnuLongMetadataHeaderAsync(TarEntryType.LongPath, _name, cancellationToken).ConfigureAwait(false);
                await longPathHeader.WriteAsGnuInternalAsync(archiveStream, buffer, cancellationToken).ConfigureAwait(false);
                buffer.Span.Clear(); // Reset it to reuse it
            }

            // Third, we write this header as a normal one
            await WriteAsGnuInternalAsync(archiveStream, buffer, cancellationToken).ConfigureAwait(false);
        }

        // Creates and returns a GNU long metadata header, with the specified long text written into its data stream.
        private static TarHeader GetGnuLongMetadataHeader(TarEntryType entryType, string longText)
        {
            TarHeader longMetadataHeader = GetDefaultGnuLongMetadataHeader(longText.Length, entryType);
            Debug.Assert(longMetadataHeader._dataStream != null);

            longMetadataHeader._dataStream.Write(Encoding.UTF8.GetBytes(longText));
            longMetadataHeader._dataStream.Seek(0, SeekOrigin.Begin); // Ensure it gets written into the archive from the beginning

            return longMetadataHeader;
        }

        // Asynchronously creates and returns a GNU long metadata header, with the specified long text written into its data stream.
        private static async Task<TarHeader> GetGnuLongMetadataHeaderAsync(TarEntryType entryType, string longText, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            TarHeader longMetadataHeader = GetDefaultGnuLongMetadataHeader(longText.Length, entryType);
            Debug.Assert(longMetadataHeader._dataStream != null);

            await longMetadataHeader._dataStream.WriteAsync(Encoding.UTF8.GetBytes(longText), cancellationToken).ConfigureAwait(false);
            longMetadataHeader._dataStream.Seek(0, SeekOrigin.Begin); // Ensure it gets written into the archive from the beginning

            return longMetadataHeader;
        }

        // Constructs a GNU metadata header with default values for the specified entry type.
        private static TarHeader GetDefaultGnuLongMetadataHeader(int longTextLength, TarEntryType entryType)
        {
            Debug.Assert((entryType is TarEntryType.LongPath && longTextLength > FieldLengths.Name) ||
                         (entryType is TarEntryType.LongLink && longTextLength > FieldLengths.LinkName));

            TarHeader longMetadataHeader = new(TarEntryFormat.Gnu);

            longMetadataHeader._name = GnuLongMetadataName; // Same name for both longpath or longlink
            longMetadataHeader._mode = TarHelpers.GetDefaultMode(entryType);
            longMetadataHeader._uid = 0;
            longMetadataHeader._gid = 0;
            longMetadataHeader._mTime = DateTimeOffset.MinValue; // 0
            longMetadataHeader._typeFlag = entryType;
            longMetadataHeader._dataStream = new MemoryStream();

            return longMetadataHeader;
        }

        // Writes the current header as a GNU entry into the archive stream.
        internal void WriteAsGnuInternal(Stream archiveStream, Span<byte> buffer)
        {
            WriteAsGnuSharedInternal(buffer, out long actualLength);

            archiveStream.Write(buffer);

            if (_dataStream != null)
            {
                WriteData(archiveStream, _dataStream, actualLength);
            }
        }

        // Asynchronously writes the current header as a GNU entry into the archive stream.
        internal async Task WriteAsGnuInternalAsync(Stream archiveStream, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            WriteAsGnuSharedInternal(buffer.Span, out long actualLength);

            await archiveStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);

            if (_dataStream != null)
            {
                await WriteDataAsync(archiveStream, _dataStream, actualLength, cancellationToken).ConfigureAwait(false);
            }
        }

        // Shared checksum and data length calculations for GNU entry writing.
        private void WriteAsGnuSharedInternal(Span<byte> buffer, out long actualLength)
        {
            actualLength = GetTotalDataBytesToWrite();

            int tmpChecksum = WriteName(buffer, out _);
            tmpChecksum += WriteCommonFields(buffer, actualLength, TarHelpers.GetCorrectTypeFlagForFormat(TarEntryFormat.Gnu, _typeFlag));
            tmpChecksum += WriteGnuMagicAndVersion(buffer);
            tmpChecksum += WritePosixAndGnuSharedFields(buffer);
            tmpChecksum += WriteGnuFields(buffer);

            _checksum = WriteChecksum(tmpChecksum, buffer);
        }

        // Writes the current header as a PAX Extended Attributes entry into the archive stream.
        private void WriteAsPaxExtendedAttributes(Stream archiveStream, Span<byte> buffer, Dictionary<string, string> extendedAttributes, bool isGea, int globalExtendedAttributesEntryNumber)
        {
            WriteAsPaxExtendedAttributesShared(isGea, globalExtendedAttributesEntryNumber);
            _dataStream = GenerateExtendedAttributesDataStream(extendedAttributes);
            WriteAsPaxInternal(archiveStream, buffer);
        }

        // Asynchronously writes the current header as a PAX Extended Attributes entry into the archive stream and returns the value of the final checksum.
        private async Task WriteAsPaxExtendedAttributesAsync(Stream archiveStream, Memory<byte> buffer, Dictionary<string, string> extendedAttributes, bool isGea, int globalExtendedAttributesEntryNumber, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            WriteAsPaxExtendedAttributesShared(isGea, globalExtendedAttributesEntryNumber);
            _dataStream = await GenerateExtendedAttributesDataStreamAsync(extendedAttributes, cancellationToken).ConfigureAwait(false);
            await WriteAsPaxInternalAsync(archiveStream, buffer, cancellationToken).ConfigureAwait(false);
        }

        // Initializes the name, mode and type flag of a PAX extended attributes entry.
        private void WriteAsPaxExtendedAttributesShared(bool isGea, int globalExtendedAttributesEntryNumber)
        {
            Debug.Assert(isGea && globalExtendedAttributesEntryNumber >= 0 || !isGea && globalExtendedAttributesEntryNumber < 0);

            _name = isGea ?
                GenerateGlobalExtendedAttributeName(globalExtendedAttributesEntryNumber) :
                GenerateExtendedAttributeName();

            _mode = TarHelpers.GetDefaultMode(_typeFlag);
            _typeFlag = isGea ? TarEntryType.GlobalExtendedAttributes : TarEntryType.ExtendedAttributes;
        }

        // Both the Extended Attributes and Global Extended Attributes entry headers are written in a similar way, just the data changes
        // This method writes an entry as both entries require, using the data from the current header instance.
        private void WriteAsPaxInternal(Stream archiveStream, Span<byte> buffer)
        {
            WriteAsPaxSharedInternal(buffer, out long actualLength);

            archiveStream.Write(buffer);

            if (_dataStream != null)
            {
                WriteData(archiveStream, _dataStream, actualLength);
            }
        }

        // Both the Extended Attributes and Global Extended Attributes entry headers are written in a similar way, just the data changes
        // This method asynchronously writes an entry as both entries require, using the data from the current header instance.
        private async Task WriteAsPaxInternalAsync(Stream archiveStream, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            WriteAsPaxSharedInternal(buffer.Span, out long actualLength);

            await archiveStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);

            if (_dataStream != null)
            {
                await WriteDataAsync(archiveStream, _dataStream, actualLength, cancellationToken).ConfigureAwait(false);
            }
        }

        // Shared checksum and data length calculations for PAX entry writing.
        private void WriteAsPaxSharedInternal(Span<byte> buffer, out long actualLength)
        {
            actualLength = GetTotalDataBytesToWrite();

            int tmpChecksum = WritePosixName(buffer);
            tmpChecksum += WriteCommonFields(buffer, actualLength, TarHelpers.GetCorrectTypeFlagForFormat(TarEntryFormat.Pax, _typeFlag));
            tmpChecksum += WritePosixMagicAndVersion(buffer);
            tmpChecksum += WritePosixAndGnuSharedFields(buffer);

            _checksum = WriteChecksum(tmpChecksum, buffer);
        }

        // All formats save in the name byte array only the ASCII bytes that fit. The full string is returned in the out byte array.
        private int WriteName(Span<byte> buffer, out byte[] fullNameBytes)
        {
            fullNameBytes = Encoding.ASCII.GetBytes(_name);
            int nameBytesLength = Math.Min(fullNameBytes.Length, FieldLengths.Name);
            int checksum = WriteLeftAlignedBytesAndGetChecksum(fullNameBytes.AsSpan(0, nameBytesLength), buffer.Slice(FieldLocations.Name, FieldLengths.Name));
            return checksum;
        }

        // Ustar and PAX save in the name byte array only the ASCII bytes that fit, and the rest of that string is saved in the prefix field.
        private int WritePosixName(Span<byte> buffer)
        {
            int checksum = WriteName(buffer, out byte[] fullNameBytes);
            if (fullNameBytes.Length > FieldLengths.Name)
            {
                int prefixBytesLength = Math.Min(fullNameBytes.Length - FieldLengths.Name, FieldLengths.Name);
                checksum += WriteLeftAlignedBytesAndGetChecksum(fullNameBytes.AsSpan(FieldLengths.Name, prefixBytesLength), buffer.Slice(FieldLocations.Prefix, FieldLengths.Prefix));
            }
            return checksum;
        }

        // Writes all the common fields shared by all formats into the specified spans.
        private int WriteCommonFields(Span<byte> buffer, long actualLength, TarEntryType actualEntryType)
        {
            int checksum = 0;

            if (_mode > 0)
            {
                checksum += WriteAsOctal(_mode, buffer, FieldLocations.Mode, FieldLengths.Mode);
            }

            if (_uid > 0)
            {
                checksum += WriteAsOctal(_uid, buffer, FieldLocations.Uid, FieldLengths.Uid);
            }

            if (_gid > 0)
            {
                checksum += WriteAsOctal(_gid, buffer, FieldLocations.Gid, FieldLengths.Gid);
            }

            _size = actualLength;

            if (_size > 0)
            {
                checksum += WriteAsOctal(_size, buffer, FieldLocations.Size, FieldLengths.Size);
            }

            checksum += WriteAsTimestamp(_mTime, buffer, FieldLocations.MTime, FieldLengths.MTime);

            char typeFlagChar = (char)actualEntryType;
            buffer[FieldLocations.TypeFlag] = (byte)typeFlagChar;
            checksum += typeFlagChar;

            if (!string.IsNullOrEmpty(_linkName))
            {
                checksum += WriteAsAsciiString(_linkName, buffer, FieldLocations.LinkName, FieldLengths.LinkName);
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
            int checksum = WriteLeftAlignedBytesAndGetChecksum(PaxMagicBytes, buffer.Slice(FieldLocations.Magic, FieldLengths.Magic));
            checksum += WriteLeftAlignedBytesAndGetChecksum(PaxVersionBytes, buffer.Slice(FieldLocations.Version, FieldLengths.Version));
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
                checksum += WriteAsAsciiString(_uName, buffer, FieldLocations.UName, FieldLengths.UName);
            }

            if (!string.IsNullOrEmpty(_gName))
            {
                checksum += WriteAsAsciiString(_gName, buffer, FieldLocations.GName, FieldLengths.GName);
            }

            if (_devMajor > 0)
            {
                checksum += WriteAsOctal(_devMajor, buffer, FieldLocations.DevMajor, FieldLengths.DevMajor);
            }

            if (_devMinor > 0)
            {
                checksum += WriteAsOctal(_devMinor, buffer, FieldLocations.DevMinor, FieldLengths.DevMinor);
            }

            return checksum;
        }

        // Saves the gnu-specific fields into the specified spans.
        private int WriteGnuFields(Span<byte> buffer)
        {
            int checksum = WriteAsTimestamp(_aTime, buffer, FieldLocations.ATime, FieldLengths.ATime);
            checksum += WriteAsTimestamp(_cTime, buffer, FieldLocations.CTime, FieldLengths.CTime);

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
            archiveStream.Write(new byte[paddingAfterData]);
        }

        // Asynchronously writes the current header's data stream into the archive stream.
        private static async Task WriteDataAsync(Stream archiveStream, Stream dataStream, long actualLength, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await dataStream.CopyToAsync(archiveStream, cancellationToken).ConfigureAwait(false); // The data gets copied from the current position
            int paddingAfterData = TarHelpers.CalculatePadding(actualLength);
            await archiveStream.WriteAsync(new byte[paddingAfterData], cancellationToken).ConfigureAwait(false);
        }

        // Dumps into the archive stream an extended attribute entry containing metadata of the entry it precedes.
        private static Stream? GenerateExtendedAttributesDataStream(Dictionary<string, string> extendedAttributes)
        {
            MemoryStream? dataStream = null;
            if (extendedAttributes.Count > 0)
            {
                dataStream = new MemoryStream();
                foreach ((string attribute, string value) in extendedAttributes)
                {
                    byte[] entryBytes = GenerateExtendedAttributeKeyValuePairAsByteArray(Encoding.UTF8.GetBytes(attribute), Encoding.UTF8.GetBytes(value));
                    dataStream.Write(entryBytes);
                }
                dataStream?.Seek(0, SeekOrigin.Begin); // Ensure it gets written into the archive from the beginning
            }
            return dataStream;
        }

        // Asynchronously dumps into the archive stream an extended attribute entry containing metadata of the entry it precedes.
        private static async Task<Stream?> GenerateExtendedAttributesDataStreamAsync(Dictionary<string, string> extendedAttributes, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            MemoryStream? dataStream = null;
            if (extendedAttributes.Count > 0)
            {
                dataStream = new MemoryStream();
                foreach ((string attribute, string value) in extendedAttributes)
                {
                    byte[] entryBytes = GenerateExtendedAttributeKeyValuePairAsByteArray(Encoding.UTF8.GetBytes(attribute), Encoding.UTF8.GetBytes(value));
                    await dataStream.WriteAsync(entryBytes, cancellationToken).ConfigureAwait(false);
                }
                dataStream?.Seek(0, SeekOrigin.Begin); // Ensure it gets written into the archive from the beginning
            }
            return dataStream;
        }

        // Some fields that have a reserved spot in the header, may not fit in such field anymore, but they can fit in the
        // extended attributes. They get collected and saved in that dictionary, with no restrictions.
        private void CollectExtendedAttributesFromStandardFieldsIfNeeded()
        {
            ExtendedAttributes.Add(PaxEaName, _name);

            if (!ExtendedAttributes.ContainsKey(PaxEaMTime))
            {
                ExtendedAttributes.Add(PaxEaMTime, TarHelpers.GetTimestampStringFromDateTimeOffset(_mTime));
            }
            if (!string.IsNullOrEmpty(_gName))
            {
                TryAddStringField(ExtendedAttributes, PaxEaGName, _gName, FieldLengths.GName);
            }
            if (!string.IsNullOrEmpty(_uName))
            {
                TryAddStringField(ExtendedAttributes, PaxEaUName, _uName, FieldLengths.UName);
            }

            if (!string.IsNullOrEmpty(_linkName))
            {
                ExtendedAttributes.Add(PaxEaLinkName, _linkName);
            }

            if (_size > 99_999_999)
            {
                ExtendedAttributes.Add(PaxEaSize, _size.ToString());
            }


            // Adds the specified string to the dictionary if it's longer than the specified max byte length.
            static void TryAddStringField(Dictionary<string, string> extendedAttributes, string key, string value, int maxLength)
            {
                if (Encoding.UTF8.GetByteCount(value) > maxLength)
                {
                    extendedAttributes.Add(key, value);
                }
            }
        }

        // Generates an extended attribute key value pair string saved into a byte array, following the ISO/IEC 10646-1:2000 standard UTF-8 encoding format.
        // https://pubs.opengroup.org/onlinepubs/9699919799/utilities/pax.html
        private static byte[] GenerateExtendedAttributeKeyValuePairAsByteArray(byte[] keyBytes, byte[] valueBytes)
        {
            // Assuming key="ab" and value="cdef"

            // The " ab=cdef\n" attribute string has a length of 9 chars
            int suffixByteCount = 3 + // leading space, equals sign and trailing newline
                keyBytes.Length + valueBytes.Length;

            // The count string "9" has a length of 1 char
            string suffixByteCountString = suffixByteCount.ToString();
            int firstTotalByteCount = Encoding.ASCII.GetByteCount(suffixByteCountString);

            // If we prepend the count string length to the attribute string,
            // the total length increases to 10, which has one more digit
            // "9 abc=def\n"
            int firstPrefixAndSuffixByteCount = firstTotalByteCount + suffixByteCount;

            // The new count string "10" has an increased length of 2 chars
            string prefixAndSuffixByteCountString = firstPrefixAndSuffixByteCount.ToString();
            int realTotalCharCount = Encoding.ASCII.GetByteCount(prefixAndSuffixByteCountString);

            byte[] finalTotalCharCountBytes = Encoding.ASCII.GetBytes(prefixAndSuffixByteCountString);

            // The final string should contain the correct total length now
            List<byte> bytesList = new();

            bytesList.AddRange(finalTotalCharCountBytes);
            bytesList.Add(TarHelpers.SpaceChar);
            bytesList.AddRange(keyBytes);
            bytesList.Add(TarHelpers.EqualsChar);
            bytesList.AddRange(valueBytes);
            bytesList.Add(TarHelpers.NewLineChar);

            Debug.Assert(bytesList.Count == (realTotalCharCount + suffixByteCount));

            return bytesList.ToArray();
        }

        // The checksum accumulator first adds up the byte values of eight space chars, then the final number
        // is written on top of those spaces on the specified span as ascii.
        // At the end, it's saved in the header field and the final value returned.
        internal int WriteChecksum(int checksum, Span<byte> buffer)
        {
            // The checksum field is also counted towards the total sum
            // but as an array filled with spaces
            checksum += TarHelpers.SpaceChar * 8;

            Span<byte> converted = stackalloc byte[FieldLengths.Checksum];
            WriteAsOctal(checksum, converted, 0, converted.Length);

            Span<byte> destination = buffer.Slice(FieldLocations.Checksum, FieldLengths.Checksum);

            // Checksum field ends with a null and a space
            destination[^1] = TarHelpers.SpaceChar; // ' '
            destination[^2] = 0; // '\0'

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
                    destination[i] = TarHelpers.ZeroChar; // Leading zero chars '0'
                }
                i--;
            }

            return checksum;
        }

        // Writes the specified bytes into the specified destination, aligned to the left. Returns the sum of the value of all the bytes that were written.
        private static int WriteLeftAlignedBytesAndGetChecksum(ReadOnlySpan<byte> bytesToWrite, Span<byte> destination)
        {
            Debug.Assert(destination.Length > 1);

            int checksum = 0;

            for (int i = 0, j = 0; i < destination.Length && j < bytesToWrite.Length; i++, j++)
            {
                destination[i] = bytesToWrite[j];
                checksum += destination[i];
            }

            return checksum;
        }

        // Writes the specified bytes aligned to the right, filling all the leading bytes with the zero char 0x30,
        // ensuring a null terminator is included at the end of the specified span.
        private static int WriteRightAlignedBytesAndGetChecksum(ReadOnlySpan<byte> bytesToWrite, Span<byte> destination)
        {
            int checksum = 0;
            int i = destination.Length - 1;
            int j = bytesToWrite.Length - 1;

            while (i >= 0)
            {
                if (i == destination.Length - 1)
                {
                    destination[i] = 0; // null terminated
                }
                else if (j >= 0)
                {
                    destination[i] = bytesToWrite[j];
                    j--;
                }
                else
                {
                    destination[i] = TarHelpers.ZeroChar; // leading zeros
                }
                checksum += destination[i];
                i--;
            }

            return checksum;
        }

        // Writes the specified decimal number as a right-aligned octal number and returns its checksum.
        internal static int WriteAsOctal(long tenBaseNumber, Span<byte> destination, int location, int length)
        {
            long octal = TarHelpers.ConvertDecimalToOctal(tenBaseNumber);
            byte[] bytes = Encoding.ASCII.GetBytes(octal.ToString());
            return WriteRightAlignedBytesAndGetChecksum(bytes.AsSpan(), destination.Slice(location, length));
        }

        // Writes the specified DateTimeOffset's Unix time seconds as a right-aligned octal number, and returns its checksum.
        private static int WriteAsTimestamp(DateTimeOffset timestamp, Span<byte> destination, int location, int length)
        {
            long unixTimeSeconds = timestamp.ToUnixTimeSeconds();
            return WriteAsOctal(unixTimeSeconds, destination, location, length);
        }

        // Writes the specified text as an ASCII string aligned to the left, and returns its checksum.
        private static int WriteAsAsciiString(string str, Span<byte> buffer, int location, int length)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(str);
            return WriteLeftAlignedBytesAndGetChecksum(bytes.AsSpan(), buffer.Slice(location, length));
        }

        // Gets the special name for the 'name' field in an extended attribute entry.
        // Format: "%d/PaxHeaders.%p/%f"
        // - %d: The directory name of the file, equivalent to the result of the dirname utility on the translated pathname.
        // - %p: The current process ID.
        // - %f: The filename of the file, equivalent to the result of the basename utility on the translated pathname.
        private string GenerateExtendedAttributeName()
        {
            string? dirName = Path.GetDirectoryName(_name);
            dirName = string.IsNullOrEmpty(dirName) ? "." : dirName;

            int processId = Environment.ProcessId;

            string? fileName = Path.GetFileName(_name);
            fileName = string.IsNullOrEmpty(fileName) ? "." : fileName;

            string trailingSeparator = (_typeFlag is TarEntryType.Directory or TarEntryType.DirectoryList) ?
                $"{Path.DirectorySeparatorChar}" : string.Empty;

            return string.Format(PaxHeadersFormat, dirName, processId, fileName, trailingSeparator);
        }

        // Gets the special name for the 'name' field in a global extended attribute entry.
        // Format: "%d/GlobalHead.%p/%n"
        // - %d: The path of the $TMPDIR variable, if found. Otherwise, the value is '/tmp'.
        // - %p: The current process ID.
        // - %n: The sequence number of the global extended header record of the archive, starting at 1. In our case, since we only generate one, the value is always 1.
        // If the path of $TMPDIR makes the final string too long to fit in the 'name' field,
        // then the TMPDIR='/tmp' is used.
        private static string GenerateGlobalExtendedAttributeName(int globalExtendedAttributesEntryNumber)
        {
            Debug.Assert(globalExtendedAttributesEntryNumber >= 1);

            string tmpDir = Path.GetTempPath();
            if (Path.EndsInDirectorySeparator(tmpDir))
            {
                tmpDir = Path.TrimEndingDirectorySeparator(tmpDir);
            }
            int processId = Environment.ProcessId;

            string result = string.Format(GlobalHeadFormatPrefix, tmpDir, processId);
            string suffix = $".{globalExtendedAttributesEntryNumber}"; // GEA sequence number
            if (result.Length + suffix.Length >= FieldLengths.Name)
            {
                result = string.Format(GlobalHeadFormatPrefix, "/tmp", processId);
            }
            result += suffix;

            return result;
        }
    }
}
