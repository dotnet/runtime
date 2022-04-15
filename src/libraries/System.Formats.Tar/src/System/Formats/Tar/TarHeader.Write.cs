// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace System.Formats.Tar
{
    // Writes header attributes of a tar archive entry.
    internal partial struct TarHeader
    {
        private const byte SpaceChar = 0x20;
        private const byte EqualsChar = 0x3d;
        private const byte NewLineChar = 0xa;
        private static readonly byte[] s_paxMagic = new byte[] { 0x75, 0x73, 0x74, 0x61, 0x72, 0x0 }; // "ustar\0"
        private static readonly byte[] s_paxVersion = new byte[] { 0x30, 0x30 }; // "00"

        private static readonly byte[] s_gnuMagic = new byte[] { 0x75, 0x73, 0x74, 0x61, 0x72, 0x20 }; // "ustar "
        private static readonly byte[] s_gnuVersion = new byte[] { 0x20, 0x0 }; // " \0"

        // Extended Attribute entries have a special format in the Name field:
        // "{dirName}/PaxHeaders.{processId}/{fileName}{trailingSeparator}"
        private const string PaxHeadersFormat = "{0}/PaxHeaders.{1}/{2}{3}";

        // Global Extended Attribute entries have a special format in the Name field:
        // "{tmpFolder}/GlobalHead.{processId}.1"
        private const string GlobalHeadFormat = "{0}/GlobalHead.{1}.1";

        // Creates a PAX Global Extended Attributes header and writes it into the specified archive stream.
        internal static void WriteGlobalExtendedAttributesHeader(Stream archiveStream, IEnumerable<KeyValuePair<string, string>> globalExtendedAttributes)
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
            geaHeader.WriteAsPaxExtendedAttributes(archiveStream, globalExtendedAttributes, isGea: true);
        }

        // Writes the current header as a V7 entry into the archive stream.
        internal void WriteAsV7(Stream archiveStream)
        {
            byte[] nameBytes = new byte[FieldLengths.Name];
            byte[] modeBytes = new byte[FieldLengths.Mode];
            byte[] uidBytes = new byte[FieldLengths.Uid];
            byte[] gidBytes = new byte[FieldLengths.Gid];
            byte[] sizeBytes = new byte[FieldLengths.Size];
            byte[] mTimeBytes = new byte[FieldLengths.MTime];
            byte[] checksumBytes = new byte[FieldLengths.Checksum];
            byte typeFlagByte = 0;
            byte[] linkNameBytes = new byte[FieldLengths.LinkName];

            int checksum = SaveNameFieldAsBytes(nameBytes, out _);
            checksum += SaveCommonFieldsAsBytes(modeBytes, uidBytes, gidBytes, sizeBytes, mTimeBytes, ref typeFlagByte, linkNameBytes);

            _checksum = SaveChecksumBytes(checksum, checksumBytes);

            archiveStream.Write(nameBytes);
            archiveStream.Write(modeBytes);
            archiveStream.Write(uidBytes);
            archiveStream.Write(gidBytes);
            archiveStream.Write(sizeBytes);
            archiveStream.Write(mTimeBytes);
            archiveStream.Write(checksumBytes);
            archiveStream.WriteByte(typeFlagByte);
            archiveStream.Write(linkNameBytes);
            archiveStream.Write(new byte[FieldLengths.V7Padding]);

            if (_dataStream != null)
            {
                WriteData(archiveStream, _dataStream);
            }
        }

        // Writes the current header as a Ustar entry into the archive stream.
        internal void WriteAsUstar(Stream archiveStream)
        {
            byte[] nameBytes = new byte[FieldLengths.Name];
            byte[] modeBytes = new byte[FieldLengths.Mode];
            byte[] uidBytes = new byte[FieldLengths.Uid];
            byte[] gidBytes = new byte[FieldLengths.Gid];
            byte[] sizeBytes = new byte[FieldLengths.Size];
            byte[] mTimeBytes = new byte[FieldLengths.MTime];
            byte[] checksumBytes = new byte[FieldLengths.Checksum];
            byte typeFlagByte = 0;
            byte[] linkNameBytes = new byte[FieldLengths.LinkName];

            byte[] magicBytes = new byte[FieldLengths.Magic];
            byte[] versionBytes = new byte[FieldLengths.Version];
            byte[] uNameBytes = new byte[FieldLengths.UName];
            byte[] gNameBytes = new byte[FieldLengths.GName];
            byte[] devMajorBytes = new byte[FieldLengths.DevMajor];
            byte[] devMinorBytes = new byte[FieldLengths.DevMinor];
            byte[] prefixBytes = new byte[FieldLengths.Prefix];

            int checksum = SavePosixNameFieldAsBytes(nameBytes, prefixBytes);
            checksum += SaveCommonFieldsAsBytes(modeBytes, uidBytes, gidBytes, sizeBytes, mTimeBytes, ref typeFlagByte, linkNameBytes);
            checksum += SavePosixMagicAndVersionBytes(magicBytes, versionBytes);
            checksum += SavePosixAndGnuSharedBytes(uNameBytes, gNameBytes, devMajorBytes, devMinorBytes);

            _checksum = SaveChecksumBytes(checksum, checksumBytes);

            archiveStream.Write(nameBytes);
            archiveStream.Write(modeBytes);
            archiveStream.Write(uidBytes);
            archiveStream.Write(gidBytes);
            archiveStream.Write(sizeBytes);
            archiveStream.Write(mTimeBytes);
            archiveStream.Write(checksumBytes);
            archiveStream.WriteByte(typeFlagByte);
            archiveStream.Write(linkNameBytes);

            archiveStream.Write(magicBytes);
            archiveStream.Write(versionBytes);
            archiveStream.Write(uNameBytes);
            archiveStream.Write(gNameBytes);
            archiveStream.Write(devMajorBytes);
            archiveStream.Write(devMinorBytes);

            archiveStream.Write(prefixBytes);
            archiveStream.Write(new byte[FieldLengths.PosixPadding]);

            if (_dataStream != null)
            {
                WriteData(archiveStream, _dataStream);
            }
        }

        // Writes the current header as a PAX entry into the archive stream.
        internal void WriteAsPax(Stream archiveStream)
        {
            // First, we write the preceding extended attributes header
            TarHeader extendedAttributesHeader = default;
            // Fill the current header's dict
            CollectExtendedAttributesFromStandardFieldsIfNeeded();
            // And pass them to the extended attributes header for writing
            extendedAttributesHeader.WriteAsPaxExtendedAttributes(archiveStream, _extendedAttributes, isGea: false);

            // Second, we write this header as a normal one
            WriteAsPaxInternal(archiveStream);
        }

        // Writes the current header as a GNU entry into the archive stream.
        internal void WriteAsGnu(Stream archiveStream)
        {
            byte[] nameBytes = new byte[FieldLengths.Name];
            byte[] modeBytes = new byte[FieldLengths.Mode];
            byte[] uidBytes = new byte[FieldLengths.Uid];
            byte[] gidBytes = new byte[FieldLengths.Gid];
            byte[] sizeBytes = new byte[FieldLengths.Size];
            byte[] mTimeBytes = new byte[FieldLengths.MTime];
            byte[] checksumBytes = new byte[FieldLengths.Checksum];
            byte typeFlagByte = 0;
            byte[] linkNameBytes = new byte[FieldLengths.LinkName];

            byte[] magicBytes = new byte[FieldLengths.Magic];
            byte[] versionBytes = new byte[FieldLengths.Version];
            byte[] uNameBytes = new byte[FieldLengths.UName];
            byte[] gNameBytes = new byte[FieldLengths.GName];
            byte[] devMajorBytes = new byte[FieldLengths.DevMajor];
            byte[] devMinorBytes = new byte[FieldLengths.DevMinor];

            byte[] aTimeBytes = new byte[FieldLengths.ATime];
            byte[] cTimeBytes = new byte[FieldLengths.CTime];

            // Unused GNU fields: offset, longnames, unused, sparse struct, isextended and realsize
            // If this header came from another archive, it will have a value
            // If it was constructed by the user, it will be an empty array
            _gnuUnusedBytes ??= new byte[FieldLengths.AllGnuUnused];

            int checksum = SaveNameFieldAsBytes(nameBytes, out _);
            checksum += SaveCommonFieldsAsBytes(modeBytes, uidBytes, gidBytes, sizeBytes, mTimeBytes, ref typeFlagByte, linkNameBytes);
            checksum += SaveGnuMagicAndVersionBytes(magicBytes, versionBytes);
            checksum += SavePosixAndGnuSharedBytes(uNameBytes, gNameBytes, devMajorBytes, devMinorBytes);
            checksum += SaveGnuBytes(aTimeBytes, cTimeBytes);

            _checksum = SaveChecksumBytes(checksum, checksumBytes);

            archiveStream.Write(nameBytes);
            archiveStream.Write(modeBytes);
            archiveStream.Write(uidBytes);
            archiveStream.Write(gidBytes);
            archiveStream.Write(sizeBytes);
            archiveStream.Write(mTimeBytes);
            archiveStream.Write(checksumBytes);
            archiveStream.WriteByte(typeFlagByte);
            archiveStream.Write(linkNameBytes);

            archiveStream.Write(magicBytes);
            archiveStream.Write(versionBytes);
            archiveStream.Write(uNameBytes);
            archiveStream.Write(gNameBytes);
            archiveStream.Write(devMajorBytes);
            archiveStream.Write(devMinorBytes);

            archiveStream.Write(aTimeBytes);
            archiveStream.Write(cTimeBytes);
            archiveStream.Write(_gnuUnusedBytes);

            archiveStream.Write(new byte[FieldLengths.GnuPadding]);

            if (_dataStream != null)
            {
                WriteData(archiveStream, _dataStream);
            }
        }

        // Writes the current header as a PAX Extended Attributes entry into the archive stream.
        private void WriteAsPaxExtendedAttributes(Stream archiveStream, IEnumerable<KeyValuePair<string, string>> extendedAttributes, bool isGea)
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

            WriteAsPaxInternal(archiveStream);
        }

        // Both the Extended Attributes and Global Extended Attributes entry headers are written in a similar way, just the data changes
        // This method writes an entry as both entries require, using the data from the current header instance.
        private void WriteAsPaxInternal(Stream archiveStream)
        {
            byte[] nameBytes = new byte[FieldLengths.Name];
            byte[] modeBytes = new byte[FieldLengths.Mode];
            byte[] uidBytes = new byte[FieldLengths.Uid];
            byte[] gidBytes = new byte[FieldLengths.Gid];
            byte[] sizeBytes = new byte[FieldLengths.Size];
            byte[] mTimeBytes = new byte[FieldLengths.MTime];
            byte[] checksumBytes = new byte[FieldLengths.Checksum];
            byte typeFlagByte = 0;
            byte[] linkNameBytes = new byte[FieldLengths.LinkName];

            byte[] magicBytes = new byte[FieldLengths.Magic];
            byte[] versionBytes = new byte[FieldLengths.Version];
            byte[] uNameBytes = new byte[FieldLengths.UName];
            byte[] gNameBytes = new byte[FieldLengths.GName];
            byte[] devMajorBytes = new byte[FieldLengths.DevMajor];
            byte[] devMinorBytes = new byte[FieldLengths.DevMinor];
            byte[] prefixBytes = new byte[FieldLengths.Prefix];

            int checksum = SavePosixNameFieldAsBytes(nameBytes, prefixBytes);
            checksum += SaveCommonFieldsAsBytes(modeBytes, uidBytes, gidBytes, sizeBytes, mTimeBytes, ref typeFlagByte, linkNameBytes);
            checksum += SavePosixMagicAndVersionBytes(magicBytes, versionBytes);
            checksum += SavePosixAndGnuSharedBytes(uNameBytes, gNameBytes, devMajorBytes, devMinorBytes);

            _checksum = SaveChecksumBytes(checksum, checksumBytes);

            archiveStream.Write(nameBytes);
            archiveStream.Write(modeBytes);
            archiveStream.Write(uidBytes);
            archiveStream.Write(gidBytes);
            archiveStream.Write(sizeBytes);
            archiveStream.Write(mTimeBytes);
            archiveStream.Write(checksumBytes);
            archiveStream.WriteByte(typeFlagByte);
            archiveStream.Write(linkNameBytes);

            archiveStream.Write(magicBytes);
            archiveStream.Write(versionBytes);
            archiveStream.Write(uNameBytes);
            archiveStream.Write(gNameBytes);
            archiveStream.Write(devMajorBytes);
            archiveStream.Write(devMinorBytes);

            archiveStream.Write(prefixBytes);
            archiveStream.Write(new byte[FieldLengths.PosixPadding]);

            if (_dataStream != null)
            {
                WriteData(archiveStream, _dataStream);
            }
        }

        // All formats save in the name byte array only the bytes that fit.
        private int SaveNameFieldAsBytes(Span<byte> nameBytes, out byte[] fullNameBytes)
        {
            fullNameBytes = Encoding.ASCII.GetBytes(_name);
            int nameBytesLength = Math.Min(fullNameBytes.Length, FieldLengths.Name);
            int checksum = WriteLeftAlignedBytesAndGetChecksum(fullNameBytes.AsSpan(0, nameBytesLength), nameBytes);
            return checksum;
        }

        // Ustar and PAX save in the name byte array only the bytes that fit, and the rest of the string  (the bytes that fit) get saved in the prefix byte array.
        private int SavePosixNameFieldAsBytes(Span<byte> nameBytes, Span<byte> prefixBytes)
        {
            int checksum = SaveNameFieldAsBytes(nameBytes, out byte[] fullNameBytes);
            if (fullNameBytes.Length > FieldLengths.Name)
            {
                int prefixBytesLength = Math.Min(fullNameBytes.Length - FieldLengths.Name, FieldLengths.Name);
                checksum += WriteLeftAlignedBytesAndGetChecksum(fullNameBytes.AsSpan(FieldLengths.Name, prefixBytesLength), prefixBytes);
            }
            return checksum;
        }

        // Writes all the common fields shared by all formats into the specified spans.
        private int SaveCommonFieldsAsBytes(Span<byte> _modeBytes, Span<byte> _uidBytes, Span<byte> _gidBytes, Span<byte> sizeBytes, Span<byte> _mTimeBytes, ref byte _typeFlagByte, Span<byte> _linkNameBytes)
        {
            byte[] modeBytes = TarHelpers.GetAsciiBytes(TarHelpers.ConvertDecimalToOctal(_mode));
            int checksum = WriteRightAlignedBytesAndGetChecksum(modeBytes, _modeBytes);

            byte[] uidBytes = TarHelpers.GetAsciiBytes(TarHelpers.ConvertDecimalToOctal(_uid));
            checksum += WriteRightAlignedBytesAndGetChecksum(uidBytes, _uidBytes);

            byte[] gidBytes = TarHelpers.GetAsciiBytes(TarHelpers.ConvertDecimalToOctal(_gid));
            checksum += WriteRightAlignedBytesAndGetChecksum(gidBytes, _gidBytes);

            _size = _dataStream == null ? 0 : _dataStream.Length;

            byte[] tmpSizeBytes = (_size > 0) ?
                TarHelpers.GetAsciiBytes(TarHelpers.ConvertDecimalToOctal(_size)) :
                Array.Empty<byte>();

            checksum += WriteRightAlignedBytesAndGetChecksum(tmpSizeBytes, sizeBytes);

            checksum += WriteTimestampAndGetChecksum(_mTime, _mTimeBytes);

            char typeFlagChar = (char)_typeFlag;
            _typeFlagByte = (byte)typeFlagChar;
            checksum += typeFlagChar;

            if (!string.IsNullOrEmpty(_linkName))
            {
                checksum += WriteLeftAlignedBytesAndGetChecksum(Encoding.UTF8.GetBytes(_linkName), _linkNameBytes);
            }

            return checksum;
        }

        // Writes the magic and version fields of a ustar or pax entry into the specified spans.
        private static int SavePosixMagicAndVersionBytes(Span<byte> magicBytes, Span<byte> versionBytes)
        {
            int checksum = WriteLeftAlignedBytesAndGetChecksum(s_paxMagic, magicBytes);
            checksum += WriteLeftAlignedBytesAndGetChecksum(s_paxVersion, versionBytes);
            return checksum;
        }

        // Writes the magic and vresion fields of a gnu entry into the specified spans.
        private static int SaveGnuMagicAndVersionBytes(Span<byte> magicBytes, Span<byte> versionBytes)
        {
            int checksum = WriteLeftAlignedBytesAndGetChecksum(s_gnuMagic, magicBytes);
            checksum += WriteLeftAlignedBytesAndGetChecksum(s_gnuVersion, versionBytes);
            return checksum;
        }

        // Writes the posix fields shared by ustar, pax and gnu, into the specified spans.
        private int SavePosixAndGnuSharedBytes(Span<byte> uNameBytes, Span<byte> gNameBytes, Span<byte> devMajorBytes, Span<byte> devMinorBytes)
        {
            int checksum = 0;
            if (!string.IsNullOrEmpty(_uName))
            {
                checksum += WriteLeftAlignedBytesAndGetChecksum(Encoding.UTF8.GetBytes(_uName), uNameBytes);
            }
            if (!string.IsNullOrEmpty(_gName))
            {
                checksum += WriteLeftAlignedBytesAndGetChecksum(Encoding.UTF8.GetBytes(_gName), gNameBytes);
            }

            if (_devMajor > 0)
            {
                int octalDevMajor = TarHelpers.ConvertDecimalToOctal(_devMajor);
                checksum += WriteRightAlignedBytesAndGetChecksum(TarHelpers.GetAsciiBytes(octalDevMajor), devMajorBytes);
            }
            if (_devMinor > 0)
            {
                int octalDevMinor = TarHelpers.ConvertDecimalToOctal(_devMinor);
                checksum += WriteRightAlignedBytesAndGetChecksum(TarHelpers.GetAsciiBytes(octalDevMinor), devMinorBytes);
            }

            return checksum;
        }

        // Saves the gnu-specific fields into the specified spans.
        private int SaveGnuBytes(Span<byte> aTimeBytes, Span<byte> cTimeBytes)
        {
            int checksum = WriteTimestampAndGetChecksum(_aTime, aTimeBytes);
            checksum += WriteTimestampAndGetChecksum(_cTime, cTimeBytes);

            // Only need to collect the checksum from these fields
            if (_gnuUnusedBytes != null)
            {
                foreach (byte b in _gnuUnusedBytes)
                {
                    checksum += b;
                }
            }

            return checksum;
        }

        // Writes the current header's data stream into the archive stream.
        private static void WriteData(Stream archiveStream, Stream dataStream)
        {
            if (dataStream.CanSeek)
            {
                // If the user constructed the stream, or it comes from another tar with an underlying
                // seekable stream, then we can do this, otherwise, the user will have to do it
                dataStream.Seek(0, SeekOrigin.Begin);
            }
            dataStream.CopyTo(archiveStream);
            int paddingAfterData = TarHelpers.CalculatePadding(dataStream.Length);
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
                    double unixTimeSeconds = ((double)(value.UtcDateTime - DateTime.UnixEpoch).Ticks)/TimeSpan.TicksPerSecond;
                    extendedAttributes.Add(key, unixTimeSeconds.ToString("F6")); // 6 decimals, no commas
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
            bytesList.Add(SpaceChar);
            bytesList.AddRange(keyBytes);
            bytesList.Add(EqualsChar);
            bytesList.AddRange(valueBytes);
            bytesList.Add(NewLineChar);

            Debug.Assert(bytesList.Count == (realTotalCharCount + suffixByteCount));

            return bytesList.ToArray();
        }

        // The checksum accumulator first adds up the byte values of eight space chars,
        // then the final number is written on top of those spaces on the specified
        // span as ascii, and also returned.
        internal static int SaveChecksumBytes(int checksum, Span<byte> destination)
        {
            // The checksum field is also counted towards the total sum
            // but as an array filled with spaces
            checksum += SpaceChar * 8;

            byte[] converted = TarHelpers.GetAsciiBytes(TarHelpers.ConvertDecimalToOctal(checksum));

            // Checksum field ends with a null and a space
            destination[^1] = SpaceChar; // ' '
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
                    destination[i] = 0x30; // Leading zero chars '0'
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

        // Writes the specified DateTimeOffset instance into the specified destination as Unix time seconds, in ASCII.
        private static int WriteTimestampAndGetChecksum(DateTimeOffset timestamp, Span<byte> destination)
        {
            long unixTimeSeconds = timestamp.ToUnixTimeSeconds();
            long octalSeconds = TarHelpers.ConvertDecimalToOctal(unixTimeSeconds);
            byte[] timestampBytes = TarHelpers.GetAsciiBytes(octalSeconds);
            return WriteRightAlignedBytesAndGetChecksum(timestampBytes, destination);
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
                    destination[i] = ZeroChar; // leading zeros
                }
                checksum += destination[i];
                i--;
            }

            return checksum;
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
