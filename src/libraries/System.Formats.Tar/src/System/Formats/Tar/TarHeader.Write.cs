// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace System.Formats.Tar
{
    // Writes header attributes of a tar archive entry.
    internal partial struct TarHeader
    {
        private static ReadOnlySpan<byte> PaxMagicBytes => "ustar\0"u8;
        private static ReadOnlySpan<byte> PaxVersionBytes => "00"u8;

        private static ReadOnlySpan<byte> GnuMagicBytes => "ustar "u8;
        private static ReadOnlySpan<byte> GnuVersionBytes => " \0"u8;

        // Extended Attribute entries have a special format in the Name field:
        // "{dirName}/PaxHeaders.{processId}/{fileName}{trailingSeparator}"
        private const string PaxHeadersFormat = "{0}/PaxHeaders.{1}/{2}{3}";

        // Global Extended Attribute entries have a special format in the Name field:
        // "{tmpFolder}/GlobalHead.{processId}.1"
        private const string GlobalHeadFormat = "{0}/GlobalHead.{1}.1";

        // Predefined text for the Name field of a GNU long metadata entry. Applies for both LongPath ('L') and LongLink ('K').
        private const string GnuLongMetadataName = "././@LongLink";

        // Creates a PAX Global Extended Attributes header and writes it into the specified archive stream.
        internal static void WriteGlobalExtendedAttributesHeader(Stream archiveStream, Span<byte> buffer, IEnumerable<KeyValuePair<string, string>> globalExtendedAttributes)
        {
            TarHeader geaHeader = default;
            geaHeader._name = GenerateGlobalExtendedAttributeName();
            geaHeader._mode = (int)TarHelpers.DefaultMode;
            geaHeader._typeFlag = TarEntryType.GlobalExtendedAttributes;
            geaHeader._linkName = string.Empty;
            geaHeader._magic = string.Empty;
            geaHeader._version = string.Empty;
            geaHeader._gName = string.Empty;
            geaHeader._uName = string.Empty;
            geaHeader.WriteAsPaxExtendedAttributes(archiveStream, buffer, globalExtendedAttributes, isGea: true);
        }

        // Writes the current header as a V7 entry into the archive stream.
        internal void WriteAsV7(Stream archiveStream, Span<byte> buffer)
        {
            long actualLength = GetTotalDataBytesToWrite();
            TarEntryType actualEntryType = GetCorrectTypeFlagForFormat(TarEntryFormat.V7);

            int checksum = WriteName(buffer, out _);
            checksum += WriteCommonFields(buffer, actualLength, actualEntryType);
            WriteChecksum(checksum, buffer);

            archiveStream.Write(buffer);

            if (_dataStream != null)
            {
                WriteData(archiveStream, _dataStream, actualLength);
            }
        }

        // Writes the current header as a Ustar entry into the archive stream.
        internal void WriteAsUstar(Stream archiveStream, Span<byte> buffer)
        {
            long actualLength = GetTotalDataBytesToWrite();
            TarEntryType actualEntryType = GetCorrectTypeFlagForFormat(TarEntryFormat.Ustar);

            int checksum = WritePosixName(buffer);
            checksum += WriteCommonFields(buffer, actualLength, actualEntryType);
            checksum += WritePosixMagicAndVersion(buffer);
            checksum += WritePosixAndGnuSharedFields(buffer);
            WriteChecksum(checksum, buffer);

            archiveStream.Write(buffer);

            if (_dataStream != null)
            {
                WriteData(archiveStream, _dataStream, actualLength);
            }
        }

        // Writes the current header as a PAX entry into the archive stream.
        // Makes sure to add the preceding exteded attributes entry before the actual entry.
        internal void WriteAsPax(Stream archiveStream, Span<byte> buffer)
        {
            // First, we write the preceding extended attributes header
            TarHeader extendedAttributesHeader = default;
            // Fill the current header's dict
            CollectExtendedAttributesFromStandardFieldsIfNeeded();
            // And pass them to the extended attributes header for writing
            extendedAttributesHeader.WriteAsPaxExtendedAttributes(archiveStream, buffer, _extendedAttributes, isGea: false);

            buffer.Clear(); // Reset it to reuse it
            // Second, we write this header as a normal one
            WriteAsPaxInternal(archiveStream, buffer);
        }

        // Writes the current header as a Gnu entry into the archive stream.
        // Makes sure to add the preceding LongLink and/or LongPath entries if necessary, before the actual entry.
        internal void WriteAsGnu(Stream archiveStream, Span<byte> buffer)
        {
            // First, we determine if we need a preceding LongLink, and write it if needed
            if (_linkName.Length > FieldLengths.LinkName)
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

        // Creates and returns a GNU long metadata header, with the specified long text written into its data stream.
        private static TarHeader GetGnuLongMetadataHeader(TarEntryType entryType, string longText)
        {
            Debug.Assert((entryType is TarEntryType.LongPath && longText.Length > FieldLengths.Name) ||
                         (entryType is TarEntryType.LongLink && longText.Length > FieldLengths.LinkName));

            TarHeader longMetadataHeader = default;

            longMetadataHeader._name = GnuLongMetadataName; // Same name for both longpath or longlink
            longMetadataHeader._mode = (int)TarHelpers.DefaultMode;
            longMetadataHeader._uid = 0;
            longMetadataHeader._gid = 0;
            longMetadataHeader._mTime = DateTimeOffset.MinValue; // 0
            longMetadataHeader._typeFlag = entryType;

            longMetadataHeader._dataStream = new MemoryStream();
            longMetadataHeader._dataStream.Write(Encoding.UTF8.GetBytes(longText));
            longMetadataHeader._dataStream.Seek(0, SeekOrigin.Begin); // Ensure it gets written into the archive from the beginning

            return longMetadataHeader;
        }

        // Writes the current header as a GNU entry into the archive stream.
        internal void WriteAsGnuInternal(Stream archiveStream, Span<byte> buffer)
        {
            // Unused GNU fields: offset, longnames, unused, sparse struct, isextended and realsize
            // If this header came from another archive, it will have a value
            // If it was constructed by the user, it will be an empty array
            _gnuUnusedBytes ??= new byte[FieldLengths.AllGnuUnused];

            long actualLength = GetTotalDataBytesToWrite();
            TarEntryType actualEntryType = GetCorrectTypeFlagForFormat(TarEntryFormat.Gnu);

            int checksum = WriteName(buffer, out _);
            checksum += WriteCommonFields(buffer, actualLength, actualEntryType);
            checksum += WriteGnuMagicAndVersion(buffer);
            checksum += WritePosixAndGnuSharedFields(buffer);
            checksum += WriteGnuFields(buffer);
            WriteChecksum(checksum, buffer);

            archiveStream.Write(buffer);

            if (_dataStream != null)
            {
                WriteData(archiveStream, _dataStream, actualLength);
            }
        }

        // Writes the current header as a PAX Extended Attributes entry into the archive stream.
        private void WriteAsPaxExtendedAttributes(Stream archiveStream, Span<byte> buffer, IEnumerable<KeyValuePair<string, string>> extendedAttributes, bool isGea)
        {
            // The ustar fields (uid, gid, linkName, uname, gname, devmajor, devminor) do not get written.
            // The mode gets the default value.
            _name = GenerateExtendedAttributeName();
            _mode = (int)TarHelpers.DefaultMode;
            _typeFlag = isGea ? TarEntryType.GlobalExtendedAttributes : TarEntryType.ExtendedAttributes;
            _linkName = string.Empty;
            _magic = string.Empty;
            _version = string.Empty;
            _gName = string.Empty;
            _uName = string.Empty;

            _dataStream = GenerateExtendedAttributesDataStream(extendedAttributes);

            WriteAsPaxInternal(archiveStream, buffer);
        }

        // Both the Extended Attributes and Global Extended Attributes entry headers are written in a similar way, just the data changes
        // This method writes an entry as both entries require, using the data from the current header instance.
        private void WriteAsPaxInternal(Stream archiveStream, Span<byte> buffer)
        {
            long actualLength = GetTotalDataBytesToWrite();
            TarEntryType actualEntryType = GetCorrectTypeFlagForFormat(TarEntryFormat.Pax);

            int checksum = WritePosixName(buffer);
            checksum += WriteCommonFields(buffer, actualLength, actualEntryType);
            checksum += WritePosixMagicAndVersion(buffer);
            checksum += WritePosixAndGnuSharedFields(buffer);
            WriteChecksum(checksum, buffer);

            archiveStream.Write(buffer);

            if (_dataStream != null)
            {
                WriteData(archiveStream, _dataStream, actualLength);
            }
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

        // When writing an entry that came from an archive of a different format, if its entry type happens to
        // be an incompatible regular file entry type, convert it to the compatible one.
        // No change for all other entry types.
        private TarEntryType GetCorrectTypeFlagForFormat(TarEntryFormat format)
        {
            if (format is TarEntryFormat.V7)
            {
                if (_typeFlag is TarEntryType.RegularFile)
                {
                    return TarEntryType.V7RegularFile;
                }
            }
            else if (_typeFlag is TarEntryType.V7RegularFile)
            {
                return TarEntryType.RegularFile;
            }

            return _typeFlag;
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

        // Dumps into the archive stream an extended attribute entry containing metadata of the entry it precedes.
        private static Stream? GenerateExtendedAttributesDataStream(IEnumerable<KeyValuePair<string, string>> extendedAttributes)
        {
            MemoryStream? dataStream = null;
            foreach ((string attribute, string value) in extendedAttributes)
            {
                // Need to do this because IEnumerable has no Count property
                dataStream ??= new MemoryStream();

                byte[] entryBytes = GenerateExtendedAttributeKeyValuePairAsByteArray(Encoding.UTF8.GetBytes(attribute), Encoding.UTF8.GetBytes(value));
                dataStream.Write(entryBytes);
            }
            dataStream?.Seek(0, SeekOrigin.Begin); // Ensure it gets written into the archive from the beginning
            return dataStream;
        }

        // Some fields that have a reserved spot in the header, may not fit in such field anymore, but they can fit in the
        // extended attributes. They get collected and saved in that dictionary, with no restrictions.
        private void CollectExtendedAttributesFromStandardFieldsIfNeeded()
        {
            _extendedAttributes.Add(PaxEaName, _name);

            AddTimestampAsUnixSeconds(_extendedAttributes, PaxEaATime, _aTime);
            AddTimestampAsUnixSeconds(_extendedAttributes, PaxEaCTime, _cTime);
            AddTimestampAsUnixSeconds(_extendedAttributes, PaxEaMTime, _mTime);
            TryAddStringField(_extendedAttributes, PaxEaGName, _gName, FieldLengths.GName);
            TryAddStringField(_extendedAttributes, PaxEaUName, _uName, FieldLengths.UName);

            if (!string.IsNullOrEmpty(_linkName))
            {
                _extendedAttributes.Add(PaxEaLinkName, _linkName);
            }

            if (_size > 99_999_999)
            {
                _extendedAttributes.Add(PaxEaSize, _size.ToString());
            }

            // Adds the specified datetime to the dictionary as a decimal number.
            static void AddTimestampAsUnixSeconds(Dictionary<string, string> extendedAttributes, string key, DateTimeOffset value)
            {
                // Avoid overwriting if the user already added it before
                if (!extendedAttributes.ContainsKey(key))
                {
                    double unixTimeSeconds = ((double)(value.UtcDateTime - DateTime.UnixEpoch).Ticks) / TimeSpan.TicksPerSecond;
                    extendedAttributes.Add(key, unixTimeSeconds.ToString("F6", CultureInfo.InvariantCulture)); // 6 decimals, no commas
                }
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
        // At the end, it's saved in the header field.
        internal void WriteChecksum(int checksum, Span<byte> buffer)
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

            _checksum = checksum;
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
        // Format: "%d/GlobalHead.%p/%f"
        // - %d: The path of the $TMPDIR variable, if found. Otherwise, the value is '/tmp'.
        // - %p: The current process ID.
        // - %n: The sequence number of the global extended header record of the archive, starting at 1. In our case, since we only generate one, the value is always 1.
        // If the path of $TMPDIR makes the final string too long to fit in the 'name' field,
        // then the TMPDIR='/tmp' is used.
        private static string GenerateGlobalExtendedAttributeName()
        {
            string? tmpDir = Environment.GetEnvironmentVariable("TMPDIR");
            if (string.IsNullOrWhiteSpace(tmpDir))
            {
                tmpDir = "/tmp";
            }
            else if (Path.EndsInDirectorySeparator(tmpDir))
            {
                tmpDir = Path.TrimEndingDirectorySeparator(tmpDir);
            }
            int processId = Environment.ProcessId;

            string result = string.Format(GlobalHeadFormat, tmpDir, processId);
            if (result.Length >= FieldLengths.Name)
            {
                result = string.Format(GlobalHeadFormat, "/tmp", processId);
            }

            return result;
        }
    }
}
